//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    /// <summary>
    /// Parameters for getScriptSegment().
    /// </summary>
    [Flags]
    internal enum ScriptLoadType
    {
        /// <summary>
        /// Fail if not loaded
        /// </summary>
        DONT_LOAD = 0,
        /// <summary>
        /// Load, if neccessary
        /// </summary>
        LOAD = 1,
        /// <summary>
        /// Load, if neccessary, and lock
        /// </summary>
        LOCK = 3
    }

    internal class Class
    {
        /// <summary>
        /// number of the script the class is in, -1 for non-existing
        /// </summary>
        public int script;
        /// <summary>
        /// offset; script-relative offset, segment: 0 if not instantiated
        /// </summary>
        public Register reg;
    }

    internal class HashMap<TKey, TValue> : IDictionary<TKey, TValue> where TValue : new()
    {
        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get
            {
                if (!_items.ContainsKey(key))
                {
                    _items.Add(key, new TValue());
                }
                return _items[key];
            }

            set
            {
                _items[key] = value;
            }
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => _items.Keys;

        public ICollection<TValue> Values => _items.Values;

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_items).Add(item);
        }

        public void Add(TKey key, TValue value)
        {
            _items.Add(key, value);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _items.ContainsKey(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_items).GetEnumerator();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_items).Remove(item);
        }

        public bool Remove(TKey key)
        {
            return _items.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _items.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public TValue GetValue(TKey id, TValue defaultValue)
        {
            return ContainsKey(id) ? this[id] : defaultValue;
        }
    }

    internal class SegManager
    {
        private Register _parserPtr;
        private readonly ResourceManager _resMan;
        private Register _saveDirPtr;
        private readonly ScriptPatcher _scriptPatcher;
        /// <summary>
        /// Table of all classes
        /// </summary>
        private Class[] _classTable;
        private readonly HashMap<int, ushort> _scriptSegMap;
        private readonly List<SegmentObj> _heap;
        /// <summary>
        /// ID of the (a) clones segment
        /// </summary>
        private ushort _clonesSegId;
        /// <summary>
        /// ID of the (a) list segment
        /// </summary>
        private ushort _listsSegId;
        /// <summary>
        /// ID of the (a) node segment
        /// </summary>
        private ushort _nodesSegId;
        /// <summary>
        /// ID of the (a) hunk segment
        /// </summary>
        private ushort _hunksSegId;

#if ENABLE_SCI32
        private ushort _arraysSegId;
        private ushort _stringSegId;
#endif

        public int ClassTableSize => _classTable.Length;

        public List<SegmentObj> Segments => _heap;

        public Register SaveDirPtr => _saveDirPtr;

        public Register ParserPtr => _parserPtr;

        public SegManager(ResourceManager resMan, ScriptPatcher scriptPatcher)
        {
            _resMan = resMan;
            _scriptPatcher = scriptPatcher;
            _scriptSegMap = new HashMap<int, ushort>();
            _heap = new List<SegmentObj>();

            _saveDirPtr = Register.NULL_REG;
            _parserPtr = Register.NULL_REG;

            CreateClassTable();
        }

        /**
         * Checks whether a heap address contains an object
         * @parm obj The address to check
         * @return True if it is an object, false otherwise
         */
        public bool IsObject(Register obj)
        {
            return GetObject(obj) != null;
        }

        public int InstantiateScript(int scriptNum)
        {
            var segmentId = GetScriptSegment(scriptNum);
            var scr = GetScriptIfLoaded(segmentId);
            if (scr != null)
            {
                if (!scr.IsMarkedAsDeleted)
                {
                    scr.IncrementLockers();
                    return segmentId;
                }
                else
                {
                    scr.FreeScript();
                }
            }
            else
            {
                scr = AllocateScript(scriptNum, out segmentId);
            }

            scr.Load(scriptNum, _resMan, _scriptPatcher);
            scr.InitializeLocals(this);
            scr.InitializeClasses(this);
            scr.InitializeObjects(this, segmentId);

            return segmentId;
        }

        public ByteAccess DerefBulkPtr(Register pointer, int entries)
        {
            return (BytePtr)DerefPtr(this, pointer, entries, true);
        }

        public void UninstantiateScript(int scriptNr)
        {
            ushort segmentId = GetScriptSegment(scriptNr);
            Script scr = GetScriptIfLoaded(segmentId);

            if (scr == null || scr.IsMarkedAsDeleted)
            {   // Is it already unloaded?
                //warning("unloading script 0x%x requested although not loaded", script_nr);
                // This is perfectly valid SCI behavior
                return;
            }

            scr.DecrementLockers();   // One less locker

            if (scr.Lockers > 0)
                return;

            // Free all classtable references to this script
            for (var i = 0; i < ClassTableSize; i++)
                if (GetClass(i).reg.Segment == segmentId)
                    SetClassOffset(i, Register.NULL_REG);

            if (ResourceManager.GetSciVersion() < SciVersion.V1_1)
                UninstantiateScriptSci0(scriptNr);
            // FIXME: Add proper script uninstantiation for SCI 1.1

            if (scr.Lockers == 0)
            {
                // The actual script deletion seems to be done by SCI scripts themselves
                scr.MarkDeleted();
                DebugC(DebugLevels.Scripts, "Unloaded script 0x{0:X}.", scriptNr);
            }
        }

        public void SaveLoadWithSerializer(Serializer s)
        {
            throw new NotImplementedException();
//            if (s.IsLoading)
//            {
//                ResetSegMan();

//                // Reset _scriptSegMap, to be restored below
//                _scriptSegMap.Clear();
//            }

//            s.Skip(4, 14, 18);        // OBSOLETE: Used to be _exportsAreWide

//            uint sync_heap_size = (uint)_heap.Count;
//            s.SyncAsUint32LE(ref sync_heap_size);
//            _heap.Resize(sync_heap_size);
//            for (uint i = 0; i < sync_heap_size; ++i)
//            {
//                SegmentObj mobj = _heap[i];

//                // Sync the segment type
//                uint type = (uint)((s.IsSaving && mobj!=null) ? mobj.Type : SegmentType.INVALID);
//                s.SyncAsUint32LE(ref type);

//                if (type == (uint)SegmentType.HUNK)
//                {
//                    // Don't save or load HunkTable segments
//                    continue;
//                }
//                else if (type == (uint)SegmentType.INVALID)
//                {
//                    // If we were saving and mobj == 0, or if we are loading and this is an
//                    // entry marked as empty . skip to next
//                    continue;
//                }
//                else if (type == 5)
//                {
//                    // Don't save or load the obsolete system string segments
//                    if (s.IsSaving)
//                    {
//                        continue;
//                    }
//                    else
//                    {
//                        // Old saved game. Skip the data.
//                        string tmp;
//                        for (int j = 0; j < 4; j++)
//                        {
//                            s.SyncString(tmp);  // OBSOLETE: name
//                            s.Skip(4);          // OBSOLETE: maxSize
//                            s.SyncString(tmp);  // OBSOLETE: value
//                        }
//                        _heap[i] = null;    // set as freed
//                        continue;
//                    }
//# if ENABLE_SCI32
//                }
//                else if (type == SEG_TYPE_ARRAY)
//                {
//                    // Set the correct segment for SCI32 arrays
//                    _arraysSegId = i;
//                }
//                else if (type == SEG_TYPE_STRING)
//                {
//                    // Set the correct segment for SCI32 strings
//                    _stringSegId = i;
//#endif
//                }

//                if (s.IsLoading)
//                    mobj = SegmentObj.CreateSegmentObj(type);

//                //assert(mobj);

//                // Let the object sync custom data. Scripts are loaded at this point.
//                mobj.SaveLoadWithSerializer(s);

//                if (type == (uint)SegmentType.SCRIPT)
//                {
//                    Script scr = (Script)mobj;

//                    // If we are loading a script, perform some extra steps
//                    if (s.IsLoading)
//                    {
//                        // Hook the script up in the script.segment map
//                        _scriptSegMap[scr.ScriptNumber] = (ushort)i;

//                        ObjMap objects = scr.GetObjectMap();
//                        for (ObjMap::iterator it = objects.begin(); it != objects.end(); ++it)
//                            it._value.syncBaseObject(scr.getBuf(it._value.getPos().getOffset()));

//                    }

//                    // Sync the script's string heap
//                    if (s.Version >= 28)
//                        scr.SyncStringHeap(s);
//                }
//            }

//            s.SyncAsSint32LE(_clonesSegId);
//            s.SyncAsSint32LE(_listsSegId);
//            s.SyncAsSint32LE(_nodesSegId);

//            syncArray<Class>(s, _classTable);

//            // Now that all scripts are loaded, init their objects.
//            // Just like in Script::initializeObjectsSci0, we do two passes
//            // in case an object is loaded before its base.
//            int passes = ResourceManager.GetSciVersion() < SciVersion.V1_1 ? 2 : 1;
//            for (int pass = 1; pass <= passes; ++pass)
//            {
//                for (uint i = 0; i < _heap.size(); i++)
//                {
//                    if (!_heap[i] || _heap[i].getType() != SegmentType.SCRIPT)
//                        continue;

//                    Script scr = (Script)_heap[i];
//                    scr.SyncLocalsBlock(this);

//                    ObjMap objects = scr.GetObjectMap();
//                    for (ObjMap::iterator it = objects.begin(); it != objects.end(); ++it)
//                    {
//                        reg_t addr = it._value.getPos();
//                        Object* obj = scr.scriptObjInit(addr, false);

//                        if (pass == 2)
//                        {
//                            if (!obj.InitBaseObject(this, addr, false))
//                            {
//                                // TODO/FIXME: This should not be happening at all. It might indicate a possible issue
//                                // with the garbage collector. It happens for example in LSL5 (German, perhaps English too).
//                                Warning("Failed to locate base object for object at %04X:%04X; skipping", PRINT_REG(addr));
//                                objects.erase(addr.toUint16());
//                            }
//                        }
//                    }
//                }
//            }
        }

        public bool FreeDynmem(Register addr)
        {
            if (addr.Segment < 1 || addr.Segment >= _heap.Count || _heap[addr.Segment] == null || _heap[addr.Segment].Type != SegmentType.DYNMEM)
                return false; // error

            Deallocate(addr.Segment);

            return true; // OK
        }

#if ENABLE_SCI32
        public SciArray<Register> AllocateArray(out Register addr)
        {
            ArrayTable table;
            int offset;

            if (_arraysSegId == 0)
            {
                table = (ArrayTable) AllocSegment(new ArrayTable(), out _arraysSegId);
            }
            else
                table = (ArrayTable) _heap[_arraysSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_arraysSegId, (ushort) offset);
            return table[offset];
        }

        public SciArray<Register> LookupArray(Register addr)
        {
            if (_heap[addr.Segment].Type != SegmentType.ARRAY)
                Error("Attempt to use non-array {0} as array", addr);

            ArrayTable arrayTable = (ArrayTable) _heap[addr.Segment];

            if (!arrayTable.IsValidEntry((int) addr.Offset))
                Error("Attempt to use non-array {0} as array", addr);

            return arrayTable[(int) addr.Offset];
        }
#endif

        public SegmentObjTable<SciObject>.Entry AllocateClone(out Register addr)
        {
            CloneTable table;
            int offset;

            if (_clonesSegId == 0)
                table = (CloneTable)AllocSegment(new CloneTable(), out _clonesSegId);
            else
                table = (CloneTable)_heap[_clonesSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_clonesSegId, (ushort)offset);
            return table._table[offset];
        }

        public bool IsHeapObject(Register pos)
        {
            SciObject obj = GetObject(pos);
            if (obj == null || obj.IsFreed)
                return false;
            Script scr = GetScriptIfLoaded(pos.Segment);
            return !(scr != null && scr.IsMarkedAsDeleted);
        }

        public Class GetClass(int i)
        {
            return _classTable[i];
        }

        private void UninstantiateScriptSci0(int scriptNr)
        {
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);
            ushort segmentId = GetScriptSegment(scriptNr);
            Script scr = GetScript(segmentId);
            Register reg = Register.Make(segmentId, oldScriptHeader ? (ushort)2 : (ushort)0);
            ScriptObjectTypes objType, objLength = 0;

            // Make a pass over the object in order to uninstantiate all superclasses

            do
            {
                reg = Register.IncOffset(reg, (short)objLength); // Step over the last checked object

                var tmp = scr.GetBuf((int)(reg.Offset));
                objType = (ScriptObjectTypes)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);
                if (objType == 0)
                    break;
                tmp = scr.GetBuf((int)(reg.Offset + 2));
                objLength = (ScriptObjectTypes)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);

                reg = Register.IncOffset(reg, 4); // Step over header

                if ((objType == ScriptObjectTypes.OBJECT) || (objType == ScriptObjectTypes.CLASS))
                { // object or class?
                    reg = Register.IncOffset(reg, 8);   // magic offset (SCRIPT_OBJECT_MAGIC_OFFSET)
                    tmp = scr.GetBuf((int)(reg.Offset + 2));
                    short superclass = (short)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);

                    if (superclass >= 0)
                    {
                        int superclassScript = GetClass(superclass).script;

                        if (superclassScript == scriptNr)
                        {
                            if (scr.Lockers != 0)
                                scr.DecrementLockers();  // Decrease lockers if this is us ourselves
                        }
                        else
                        {
                            UninstantiateScript(superclassScript);
                        }
                        // Recurse to assure that the superclass lockers number gets decreased
                    }

                    reg = Register.IncOffset(reg, (short)Script.SCRIPT_OBJECT_MAGIC_OFFSET);
                } // if object or class

                reg = Register.IncOffset(reg, -4); // Step back on header

            } while (objType != 0);
        }

        private Script AllocateScript(int scriptNr, out ushort segid)
        {
            // Check if the script already has an allocated segment. If it
            // does, return that segment.
            segid = _scriptSegMap.GetValue(scriptNr, 0);
            if (segid > 0)
            {
                return (Script)_heap[segid];
            }

            // allocate the SegmentObj
            var mem = AllocSegment(new Script(), out segid);

            // Add the script to the "script id . segment id" hashmap
            _scriptSegMap[scriptNr] = segid;

            return (Script)mem;
        }

        public SegmentObj AllocSegment(SegmentObj mem, out ushort segid)
        {
            // Find a free segment
            var id = FindFreeSegment();
            segid = id;

            if (mem == null)
                throw new InvalidOperationException("SegManager: invalid mobj");

            // ... and put it into the (formerly) free segment.
            if (id >= _heap.Count)
            {
                for (int i = 0; i <= id - _heap.Count + 1; i++)
                {
                    _heap.Add(null);
                }
            }
            _heap[id] = mem;

            return mem;
        }

        public void ResizeClassTable(int size)
        {
            Array.Resize(ref _classTable, size);
        }

        private ushort FindFreeSegment()
        {
            // The following is a very crude approach: We find a free segment id by
            // scanning from the start. This can be slow if the number of segments
            // becomes large. Optimizations are possible and easy, but I'll refrain
            // from attempting any until we determine we actually need it.
            int seg = 1;
            while (seg < _heap.Count && _heap[seg] != null)
            {
                ++seg;
            }

            if (seg >= 65536) throw new InvalidOperationException("No space left in heap");

            return (ushort)seg;
        }

        public void InitSysStrings()
        {
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                // We need to allocate system strings in one segment, for compatibility reasons
                AllocDynmem(512, "system strings", out _saveDirPtr);
                _parserPtr = Register.Make(_saveDirPtr.Segment, (ushort)(_saveDirPtr.Offset + 256));
# if ENABLE_SCI32
            }
            else {
                var saveDirString = AllocateString(out _saveDirPtr);
                saveDirString.SetSize(256);
                saveDirString.SetValue(0, 0);

                _parserPtr = Register.NULL_REG;  // no SCI2 game had a parser
#endif
            }
        }

        public void SetClassOffset(int index, Register offset)
        {
            _classTable[index].reg = offset;
        }

        public StackPtr? DerefRegPtr(Register pointer, int entries)
        {
            return (StackPtr?)DerefPtr(this, pointer, 2 * entries, false);
        }

        public Register GetClassAddress(int classnr, ScriptLoadType loadType, ushort callerSegment)
        {
            if (classnr == 0xffff)
                return Register.NULL_REG;

            if (classnr < 0 || _classTable.Length <= classnr || _classTable[classnr].script < 0)
            {
                Error($"[VM] Attempt to dereference class {classnr:x}, which doesn't exist (max {_classTable.Length:x})");
                return Register.NULL_REG;
            }

            Class theClass = _classTable[classnr];
            if (theClass.reg.Segment == 0)
            {
                GetScriptSegment(theClass.script, loadType);

                if (theClass.reg.Segment == 0)
                {
                    throw new InvalidOperationException($"[VM] Trying to instantiate class {classnr:x} by instantiating script 0x{theClass.script:x} ({theClass.script}) failed;");
                }
            }
            else
            if (callerSegment != theClass.reg.Segment)
                GetScript(theClass.reg.Segment).IncrementLockers();

            return theClass.reg;
        }

        public byte[] AllocDynmem(int size, string descr, out Register addr)
        {
            ushort seg;
            SegmentObj mobj = AllocSegment(new DynMem(), out seg);
            addr = Register.Make(seg, 0);

            DynMem d = (DynMem)mobj;

            d._size = size;

            if (size == 0)
                d._buf = null;
            else
                d._buf = new byte[size];

            d._description = descr;

            return d._buf;
        }

        public Script GetScriptIfLoaded(ushort seg)
        {
            if (seg < 1 || seg >= _heap.Count || _heap[seg] == null || _heap[seg].Type != SegmentType.SCRIPT)
                return null;
            return (Script)_heap[seg];
        }


        private void CreateClassTable()
        {
            var vocab996 = _resMan.FindResource(new ResourceId(ResourceType.Vocab, 996), true);

            if (vocab996 == null)
                throw new InvalidOperationException("SegManager: failed to open vocab 996");

            int totalClasses = vocab996.size >> 2;
            _classTable = new Class[totalClasses];

            for (ushort classNr = 0; classNr < totalClasses; classNr++)
            {
                ushort scriptNr = vocab996.data.ReadSci11EndianUInt16(classNr * 4 + 2);

                _classTable[classNr] = new Class();
                _classTable[classNr].reg = Register.NULL_REG;
                _classTable[classNr].script = scriptNr;
            }

            _resMan.UnlockResource(vocab996);
        }

        public void ResetSegMan()
        {
            // Free memory
            for (int i = 0; i < _heap.Count; i++)
            {
                if (_heap[i] != null)
                    Deallocate((ushort)i);
            }

            _heap.Clear();

            // And reinitialize
            _heap.Add(null);

            _clonesSegId = 0;
            _listsSegId = 0;
            _nodesSegId = 0;
            _hunksSegId = 0;

# if ENABLE_SCI32
            _arraysSegId = 0;
            _stringSegId = 0;
#endif

            // Reinitialize class table
            Array.Clear(_classTable, 0, _classTable.Length);
            CreateClassTable();
        }

        /// <summary>
        /// Find the address of an object by its name. In case multiple objects
        /// with the same name occur, the optional index parameter can be used
        /// to distinguish between them. If index is -1, then if there is a
        /// unique object with the specified name, its address is returned;
        /// if there are multiple matches, or none, then NULL_REG is returned.
        /// </summary>
        /// <param name="name">the name of the object we are looking for</param>
        /// <param name="index">the index of the object in case there are multiple</param>
        /// <returns>the address of the object, or NULL_REG</returns>
        public Register FindObjectByName(string name, int index = -1)
        {
            List<Register> result = new List<Register>();

            // Now all values are available; iterate over all objects.
            for (var i = 0; i < _heap.Count; i++)
            {
                var mobj = _heap[i];

                if (mobj == null)
                    continue;

                var objpos = Register.Make((ushort)i, 0);

                if (mobj.Type == SegmentType.SCRIPT)
                {
                    // It's a script, scan all objects in it
                    var scr = (Script)mobj;
                    var objects = scr.ObjectMap;
                    foreach (var it in objects)
                    {
                        objpos.Offset = (ushort)it.Value.Pos.Offset;
                        if (name == GetObjectName(objpos))
                            result.Add(objpos);
                    }
                }
                else if (mobj.Type == SegmentType.CLONES)
                {
                    // It's clone table, scan all objects in it
                    var ct = (CloneTable)mobj;
                    for (uint idx = 0; idx < ct._table.Length; ++idx)
                    {
                        if (!ct.IsValidEntry((ushort)idx))
                            continue;

                        objpos.Offset = (ushort)idx;
                        if (name == GetObjectName(objpos))
                            result.Add(objpos);
                    }
                }
            }

            if (result.Count == 0)
                return Register.NULL_REG;

            if (result.Count > 1 && index < 0)
            {
                Debug($"findObjectByName({name}): multiple matches:");
                for (var i = 0; i < result.Count; i++)
                    Debug($"  {i:X3}: [{result[i]}]");
                return Register.NULL_REG; // Ambiguous
            }

            if (index < 0)
                return result[0];
            else if (result.Count <= (uint)index)
                return Register.NULL_REG; // Not found
            return result[index];
        }

        public DataStack AllocateStack(int size, out ushort segid)
        {
            var mobj = AllocSegment(new DataStack(), out segid);
            DataStack retval = (DataStack)mobj;

            retval._entries = new Register[size];
            retval._capacity = size;

            // SSCI initializes the stack with "S" characters (uppercase S in SCI0-SCI1,
            // lowercase s in SCI0 and SCI11) - probably stands for "stack"
            byte filler = (ResourceManager.GetSciVersion() >= SciVersion.V01 && ResourceManager.GetSciVersion() <= SciVersion.V1_LATE) ? (byte)'S' : (byte)'s';
            for (int i = 0; i < size; i++)
                retval._entries[i] = Register.Make(0, filler);

            return retval;
        }

#if ENABLE_SCI32
        private SciString AllocateString(out Register addr)
        {
            StringTable table;
            int offset;

            if (_stringSegId == 0)
            {
                table = (StringTable) AllocSegment(new StringTable(), out _stringSegId);
            }
            else
                table = (StringTable) _heap[_stringSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_stringSegId, (ushort) offset);
            return table[offset];
        }

        public SciString LookupString(Register addr)
        {
            if (_heap[addr.Segment].Type != SegmentType.STRING)
                Error("lookupString: Attempt to use non-string {0} as string", addr);

            StringTable stringTable = (StringTable) _heap[addr.Segment];

            if (!stringTable.IsValidEntry((int)addr.Offset))
                Error("lookupString: Attempt to use non-string {0} as string", addr);

            return stringTable[(int)addr.Offset];
        }
#endif

        public void Memcpy(Register dest, Register src, int n)
        {
            SegmentRef destR = Dereference(dest);
            SegmentRef srcR = Dereference(src);
            if (!destR.IsValid)
            {
                Warning($"Attempt to memcpy to invalid pointer {dest}");
                return;
            }
            if (n > destR.maxSize)
            {
                Warning($"Trying to dereference pointer {dest} beyond end of segment");
                return;
            }
            if (!srcR.IsValid)
            {
                Warning($"Attempt to memcpy from invalid pointer {src}");
                return;
            }
            if (n > srcR.maxSize)
            {
                Warning($"Trying to dereference pointer {src} beyond end of segment");
                return;
            }

            if (srcR.isRaw)
            {
                // raw . *
                Memcpy(dest, srcR.raw, n);
            }
            else if (destR.isRaw)
            {
                // * . raw
                Memcpy(destR.raw, src, n);
            }
            else
            {
                // non-raw . non-raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(srcR, i);
                    SetChar(destR, (uint)i, (byte)c);
                }
            }
        }

        public void Memcpy(Register dest, ByteAccess src, int n)
        {
            SegmentRef destR = Dereference(dest);
            if (!destR.IsValid)
            {
                Warning($"Attempt to memcpy to invalid pointer {dest}");
                return;
            }
            if (n > destR.maxSize)
            {
                Warning($"Trying to dereference pointer {dest} beyond end of segment");
                return;
            }

            if (destR.isRaw)
            {
                // raw . raw
                Array.Copy(src.Data, src.Offset, destR.raw.Data, destR.raw.Offset, n);
            }
            else
            {
                // raw . non-raw
                for (var i = 0; i < n; i++)
                    SetChar(destR, (uint)i, src[i]);
            }
        }

        public void Memcpy(ByteAccess dest, Register src, int n)
        {
            SegmentRef srcR = Dereference(src);
            if (!srcR.IsValid)
            {
                Warning($"Attempt to memcpy from invalid pointer {src}");
                return;
            }
            if (n > srcR.maxSize)
            {
                Warning($"Trying to dereference pointer {src} beyond end of segment");
                return;
            }

            if (srcR.isRaw)
            {
                // raw . raw
                //::memcpy(dest, src_r.raw, n);
                Array.Copy(srcR.raw.Data, srcR.raw.Offset, dest.Data, dest.Offset, n);
            }
            else
            {
                // non-raw . raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(srcR, i);
                    dest[i] = (byte)c;
                }
            }
        }

        /// <summary>
        /// Determines the segment occupied by a certain script, if any.
        /// </summary>
        /// <param name="scriptId">Number of the script to look up</param>
        /// <returns>The script's segment ID, or 0 on failure</returns>
        public ushort GetScriptSegment(int scriptId)
        {
            return _scriptSegMap.GetValue(scriptId, 0);
        }

        public ushort GetScriptSegment(int scriptNr, ScriptLoadType load)
        {
            ushort segment;

            if (load.HasFlag(ScriptLoadType.LOAD))
                InstantiateScript(scriptNr);

            segment = GetScriptSegment(scriptNr);

            if (segment > 0)
            {
                if (load.HasFlag(ScriptLoadType.LOCK))
                    GetScript(segment).IncrementLockers();
            }
            return segment;
        }

        public Script GetScript(int seg)
        {
            if (seg < 1 || seg >= _heap.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(seg), $"SegManager::getScript(): seg id {seg:X} out of bounds");
            }
            if (_heap[seg] == null)
            {
                throw new InvalidOperationException($"SegManager::getScript(): seg id {seg:X} is not in memory");
            }
            if (_heap[seg].Type != SegmentType.SCRIPT)
            {
                throw new InvalidOperationException($"SegManager::getScript(): seg id {seg:X} refers to type {_heap[seg].Type} != SEG_TYPE_SCRIPT");
            }
            return (Script)_heap[seg];
        }

        public int FindSegmentByType(SegmentType type)
        {
            var obj = _heap.FirstOrDefault(o => o != null && o.Type == type);
            return (obj != null) ? _heap.IndexOf(obj) : 0;
        }

        public string GetObjectName(Register pos)
        {
            var obj = GetObject(pos);
            if (obj == null)
                return "<no such object>";

            var nameReg = obj.NameSelector;
            if (nameReg[0].IsNull)
                return "<no name>";

            string name = null;
            if (nameReg[0].Segment != 0)
                name = DerefString(nameReg[0]);
            if (name == null)
            {
                // Crazy Nick Laura Bow is missing some object names needed for the static
                // selector vocabulary
                if (SciEngine.Instance.GameId == SciGameId.LAURABOW && pos == Register.Make(1, 0x2267))
                    return "Character";
                return "<invalid name>";
            }

            return name;
        }

        /// <summary>
        /// Dereferences a heap pointer pointing to raw memory.
        /// </summary>
        /// <param name="pointer">The pointer to dereference</param>
        /// <param name="entries">The number of values expected (for checking)</param>
        /// <returns>A physical reference to the address pointed to, or NULL on error or if not enough entries were available.</returns>
        public string DerefString(Register pointer, int entries = 0)
        {
            var data = (BytePtr)DerefPtr(this, pointer, entries, true);
            return ScummHelper.GetText(data.Data, data.Offset);
        }

        public SegmentRef Dereference(Register pointer)
        {
            SegmentRef ret = new SegmentRef();

            if (pointer.Segment == 0 || (pointer.Segment >= _heap.Count) || _heap[pointer.Segment] == null)
            {
                // This occurs in KQ5CD when interacting with certain objects
                Warning($"SegManager::dereference(): Attempt to dereference invalid pointer {pointer}");
                return ret; /* Invalid */
            }

            var mobj = _heap[pointer.Segment];
            return mobj.Dereference(pointer);
        }

        private static object DerefPtr(SegManager segMan, Register pointer, int entries, bool wantRaw)
        {
            var ret = segMan.Dereference(pointer);

            if (!ret.IsValid)
                return null;

            if (ret.isRaw != wantRaw)
            {
                var isRaw = ret.isRaw ? "raw" : "not raw";
                var wr = wantRaw ? "raw" : "not raw";
                Warning($"Dereferencing pointer {pointer} (type {segMan.GetSegmentType(pointer.Segment)}) which is {isRaw}, but expected {wr}");
            }

            if (!wantRaw && ret.skipByte)
            {
                Warning($"Unaligned pointer read: {pointer} expected with word alignment");
                return null;
            }

            if (entries > ret.maxSize)
            {
                Warning($"Trying to dereference pointer {pointer} beyond end of segment");
                return null;
            }

            if (ret.isRaw)
                return ret.raw;
            else
                return ret.reg;
        }

        public SegmentObj GetSegmentObj(ushort seg)
        {
            if (seg < 1 || seg >= _heap.Count || _heap[seg] == null)
                return null;
            return _heap[seg];
        }

        public SegmentObj GetSegment(ushort seg, SegmentType type)
        {
            return GetSegmentType(seg) == type ? _heap[seg] : null;
        }

        public SegmentType GetSegmentType(ushort seg)
        {
            if (seg < 1 || seg >= _heap.Count || _heap[seg] == null)
                return SegmentType.INVALID;
            return _heap[seg].Type;
        }

        public SciObject GetObject(Register pos)
        {
            var mobj = GetSegmentObj(pos.Segment);
            SciObject obj = null;

            if (mobj != null)
            {
                if (mobj.Type == SegmentType.CLONES)
                {
                    var ct = (CloneTable)mobj;
                    if (ct.IsValidEntry((int)pos.Offset))
                        obj = ct._table[(int)pos.Offset].Item;
                    else
                    {
                        Warning("getObject(): Trying to get an invalid object");
                    }
                }
                else if (mobj.Type == SegmentType.SCRIPT)
                {
                    var scr = (Script)mobj;
                    if (pos.Offset <= scr.BufSize && pos.Offset >= (uint)-Script.SCRIPT_OBJECT_MAGIC_OFFSET
                            && scr.OffsetIsObject((int)pos.Offset))
                    {
                        obj = scr.GetObject((ushort)pos.Offset);
                    }
                }
            }

            return obj;
        }

        public Register AllocateHunkEntry(string hunkType, int size)
        {
            HunkTable table;

            if (_hunksSegId == 0)
                AllocSegment(new HunkTable(), out _hunksSegId);
            table = (HunkTable)_heap[_hunksSegId];

            ushort offset = (ushort)table.AllocEntry();

            Register addr = Register.Make(_hunksSegId, offset);
            Hunk h = table._table[offset].Item;

            if (h == null)
                return Register.NULL_REG;

            h.mem = new byte[size];
            h.size = size;
            h.type = hunkType;

            return addr;
        }

        public void FreeHunkEntry(Register addr)
        {
            if (addr.IsNull)
            {
                Warning("Attempt to free a Hunk from a null address");
                return;
            }

            HunkTable ht = (HunkTable)GetSegment(addr.Segment, SegmentType.HUNK);

            if (ht == null)
            {
                Warning($"Attempt to free Hunk from address {addr}: Invalid segment type {GetSegmentType(addr.Segment)}");
                return;
            }

            ht.FreeEntryContents((int)addr.Offset);
        }

        public byte[] GetHunkPointer(Register addr)
        {
            HunkTable ht = (HunkTable)GetSegment(addr.Segment, SegmentType.HUNK);

            if (ht == null || !ht.IsValidEntry((int)addr.Offset))
            {
                // Valid SCI behavior, e.g. when loading/quitting
                return null;
            }

            return ht._table[addr.Offset].Item.mem;
        }

        public void Strcpy(Register dest, string src)
        {
            Strncpy(dest, src, 0xFFFFFFFFU);
        }

        public void Strcpy(Register dest, Register src)
        {
            Strncpy(dest, src, 0xFFFFFFFFU);
        }

        public void Strncpy(Register dest, string src, uint n)
        {
            SegmentRef destR = Dereference(dest);
            if (!destR.IsValid)
            {
                Warning($"Attempt to strncpy to invalid pointer {dest}");
                return;
            }

            if (destR.isRaw)
            {
                // raw . raw
                if (n == 0xFFFFFFFFU)
                {
                    Array.Copy(src.ToCharArray().Select(c => (byte)c).ToArray(), 0, destR.raw.Data, destR.raw.Offset, src.Length);
                    destR.raw.Data[destR.raw.Offset + src.Length] = 0;
                }
                else
                {
                    Array.Copy(src.ToCharArray().Select(c => (byte)c).ToArray(), 0, destR.raw.Data, destR.raw.Offset, (int)n);
                    destR.raw.Data[destR.raw.Offset + n] = 0;
                }
            }
            else
            {
                int i;
                // raw . non-raw
                for (i = 0; i < n && i < src.Length; i++)
                {
                    SetChar(destR, (uint)i, (byte)src[i]);
                    if (src[i] == 0)
                        break;
                }
                if (i == src.Length)
                {
                    SetChar(destR, (uint)i, 0);
                }
                // Put an ending NUL to terminate the string
                if (destR.maxSize > n)
                    SetChar(destR, n, 0);
            }
        }

        public void Strncpy(Register dest, Register src, uint n)
        {
            if (src.IsNull)
            {
                // Clear target string instead.
                if (n > 0)
                    Strcpy(dest, "");

                return; // empty text
            }

            SegmentRef destR = Dereference(dest);
            SegmentRef srcR = Dereference(src);
            if (!srcR.IsValid)
            {
                Warning($"Attempt to strncpy from invalid pointer {src}");

                // Clear target string instead.
                if (n > 0)
                    Strcpy(dest, "");
                return;
            }

            if (!destR.IsValid)
            {
                Warning($"Attempt to strncpy to invalid pointer {dest}");
                return;
            }


            if (srcR.isRaw)
            {
                // raw . *
                Strncpy(dest, ScummHelper.GetText(srcR.raw.Data, srcR.raw.Offset), n);
            }
            else if (destR.isRaw && !srcR.isRaw)
            {
                // non-raw . raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(srcR, i);
                    destR.raw[i] = (byte)c;
                    if (c == 0)
                        break;
                }
            }
            else
            {
                // non-raw . non-raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(srcR, i);
                    SetChar(destR, (uint)i, (byte)c);
                    if (c == 0)
                        break;
                }
            }
        }

        // Helper functions for getting/setting characters in string fragments
        private static char GetChar(SegmentRef @ref, int offset)
        {
            if (@ref.skipByte)
                offset++;

            Register val = @ref.reg.Value[offset / 2];

            // segment 0xFFFF means that the scripts are using uninitialized temp-variable space
            //  we can safely ignore this, if it isn't one of the first 2 chars.
            //  foreign lsl3 uses kFileIO(readraw) and then immediately uses kReadNumber right at the start
            if (val.Segment != 0)
                if (!((val.Segment == 0xFFFF) && (offset > 1)))
                    Warning("Attempt to read character from non-raw data");

            bool oddOffset = (offset & 1) != 0;
            if (SciEngine.Instance.IsBe)
                oddOffset = !oddOffset;

            return (char)(oddOffset ? val.Offset >> 8 : val.Offset & 0xff);
        }

        private static void SetChar(SegmentRef @ref, uint offset, byte value)
        {
            if (@ref.skipByte)
                offset++;

            StackPtr val = @ref.reg.Value + (int)offset / 2;

            val.SetSegment(0, 0);

            bool oddOffset = (offset & 1) != 0;
            if (SciEngine.Instance.IsBe)
                oddOffset = !oddOffset;

            if (oddOffset)
                val.SetOffset(0, (ushort)((val[0].Offset & 0x00ff) | (value << 8)));
            else
                val.SetOffset(0, (ushort)((val[0].Offset & 0xff00) | value));
        }

        public List AllocateList(out Register addr)
        {
            ListTable table;
            int offset;

            if (_listsSegId == 0)
                AllocSegment(new ListTable(), out _listsSegId);
            table = (ListTable)_heap[_listsSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_listsSegId, (ushort)offset);
            return table._table[offset].Item;
        }

        public int Strlen(Register str)
        {
            if (str.IsNull)
                return 0;   // empty text

            SegmentRef strR = Dereference(str);
            if (!strR.IsValid)
            {
                Warning($"Attempt to call strlen on invalid pointer {str}");
                return 0;
            }

            if (strR.isRaw)
            {
                return ScummHelper.GetTextLength(strR.raw.Data, strR.raw.Offset);
            }
            else
            {
                int i = 0;
                while (GetChar(strR, i) != 0)
                    i++;
                return i;
            }
        }

        /// <summary>
        /// Return the string referenced by pointer.
        /// pointer can point to either a raw or non-raw segment.
        /// </summary>
        /// <param name="pointer">pointer The pointer to dereference</param>
        /// <param name="entries">The number of values expected (for checking)</param>
        /// <returns>The string referenced, or an empty string if not enough 
        /// entries were available.</returns>
        public string GetString(Register pointer, int entries = 0)
        {
            string ret = string.Empty;
            if (pointer.IsNull)
                return ret; // empty text

            SegmentRef srcR = Dereference(pointer);
            if (!srcR.IsValid)
            {
                Warning($"SegManager::getString(): Attempt to dereference invalid pointer {pointer}");
                return ret;
            }
            if (entries > srcR.maxSize)
            {
                Warning($"Trying to dereference pointer {pointer} beyond end of segment");
                return ret;
            }
            if (srcR.isRaw)
                ret = ScummHelper.GetText(srcR.raw.Data, srcR.raw.Offset);
            else
            {
                var i = 0;
                for (;;)
                {
                    char c = GetChar(srcR, i);

                    if (c == 0)
                        break;

                    i++;
                    ret += c;
                }
            }
            return ret;
        }

        public List LookupList(Register addr)
        {
            if (GetSegmentType(addr.Segment) != SegmentType.LISTS)
            {
                throw new InvalidOperationException($"Attempt to use non-list {addr} as list");
            }

            ListTable lt = (ListTable)_heap[addr.Segment];

            if (!lt.IsValidEntry((int)addr.Offset))
            {
                throw new InvalidOperationException($"Attempt to use non-list {addr} as list");
            }

            return lt._table[addr.Offset].Item;
        }

        internal Node LookupNode(Register addr, bool stopOnDiscarded = true)
        {
            if (addr.IsNull)
                return null; // Non-error null

            SegmentType type = GetSegmentType(addr.Segment);

            if (type != SegmentType.NODES)
            {
                throw new InvalidOperationException($"Attempt to use non-node {addr} (type {type}) as list node");
            }

            NodeTable nt = (NodeTable)_heap[addr.Segment];

            if (!nt.IsValidEntry((int)addr.Offset))
            {
                if (!stopOnDiscarded)
                    return null;

                throw new InvalidOperationException($"Attempt to use invalid or discarded reference {addr} as list node");
            }

            return nt._table[addr.Offset].Item;
        }

        public void DeallocateScript(int scriptNr)
        {
            Deallocate(GetScriptSegment(scriptNr));
        }

        private void Deallocate(ushort seg)
        {
            if (seg < 1 || seg >= _heap.Count)
                throw new ArgumentOutOfRangeException(nameof(seg), "Attempt to deallocate an invalid segment ID");

            SegmentObj mobj = _heap[seg];
            if (mobj == null)
                throw new InvalidOperationException("Attempt to deallocate an already freed segment");

            if (mobj.Type == SegmentType.SCRIPT)
            {
                Script scr = (Script)mobj;
                _scriptSegMap.Remove(scr.ScriptNumber);
                if (scr.LocalsSegment != 0)
                {
                    // Check if the locals segment has already been deallocated.
                    // If the locals block has been stored in a segment with an ID
                    // smaller than the segment ID of the script itself, it will be
                    // already freed at this point. This can happen when scripts are
                    // uninstantiated and instantiated again: they retain their own
                    // segment ID, but are allocated a new locals segment, which can
                    // have an ID smaller than the segment of the script itself.
                    if (_heap[scr.LocalsSegment] != null)
                        Deallocate(scr.LocalsSegment);
                }
            }

            _heap[seg] = null;
        }

        public Register NewNode(Register value, Register key)
        {
            Register nodeRef;
            Node n = AllocateNode(out nodeRef);
            n.pred = n.succ = Register.NULL_REG;
            n.key = key;
            n.value = value;

            return nodeRef;
        }

        private Node AllocateNode(out Register addr)
        {
            NodeTable table;
            int offset;

            if (_nodesSegId == 0)
                AllocSegment(new NodeTable(), out _nodesSegId);
            table = (NodeTable)_heap[_nodesSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_nodesSegId, (ushort)offset);
            return table._table[offset].Item;
        }
    }
}
