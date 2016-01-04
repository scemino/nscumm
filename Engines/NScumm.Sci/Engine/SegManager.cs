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
using NScumm.Core.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        private Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();

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

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return _items.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return _items.Values;
            }
        }

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
        private ResourceManager _resMan;
        private Register _saveDirPtr;
        private ScriptPatcher _scriptPatcher;
        /// <summary>
        /// Table of all classes
        /// </summary>
        private Class[] _classTable;
        private HashMap<int, ushort> _scriptSegMap;
        private List<SegmentObj> _heap;
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

        public int ClassTableSize { get { return _classTable.Length; } }

        public List<SegmentObj> Segments { get { return _heap; } }

        public Register SaveDirPtr { get { return _saveDirPtr; } }

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
                else {
                    scr.FreeScript();
                }
            }
            else {
                scr = AllocateScript(scriptNum, out segmentId);
            }

            scr.Load(scriptNum, _resMan, _scriptPatcher);
            scr.InitializeLocals(this);
            scr.InitializeClasses(this);
            scr.InitializeObjects(this, segmentId);

            return segmentId;
        }

        public void UninstantiateScript(int script_nr)
        {
            ushort segmentId = GetScriptSegment(script_nr);
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
                UninstantiateScriptSci0(script_nr);
            // FIXME: Add proper script uninstantiation for SCI 1.1

            if (scr.Lockers == 0)
            {
                // The actual script deletion seems to be done by SCI scripts themselves
                scr.MarkDeleted();
                // TODO: debugC(kDebugLevelScripts, "Unloaded script 0x%x.", script_nr);
            }
        }

        public SegmentObjTable<SciObject>.Entry AllocateClone(out Register addr)
        {
            CloneTable table;
            int offset;

            if (_clonesSegId==0)
                table = (CloneTable)AllocSegment(new CloneTable(), ref _clonesSegId);
            else
                table = (CloneTable)_heap[_clonesSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_clonesSegId, (ushort)offset);
            return table._table[offset];
        }

        public bool IsHeapObject(Register pos)
        {
            SciObject obj = GetObject(pos);
            if (obj == null || (obj != null && obj.IsFreed))
                return false;
            Script scr = GetScriptIfLoaded(pos.Segment);
            return !(scr != null && scr.IsMarkedAsDeleted);
        }

        public Class GetClass(int i)
        {
            return _classTable[i];
        }

        private void UninstantiateScriptSci0(int script_nr)
        {
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);
            ushort segmentId = GetScriptSegment(script_nr);
            Script scr = GetScript(segmentId);
            Register reg = Register.Make(segmentId, oldScriptHeader ? (ushort)2 : (ushort)0);
            ScriptObjectTypes objType, objLength = 0;

            // Make a pass over the object in order to uninstantiate all superclasses

            do
            {
                reg.IncOffset((short)objLength); // Step over the last checked object

                var tmp = scr.GetBuf((int)(reg.Offset));
                objType = (ScriptObjectTypes)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);
                if (objType == 0)
                    break;
                tmp = scr.GetBuf((int)(reg.Offset + 2));
                objLength = (ScriptObjectTypes)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);

                reg.IncOffset(4); // Step over header

                if ((objType == ScriptObjectTypes.OBJECT) || (objType == ScriptObjectTypes.CLASS))
                { // object or class?
                    reg.IncOffset(8);   // magic offset (SCRIPT_OBJECT_MAGIC_OFFSET)
                    tmp = scr.GetBuf((int)(reg.Offset + 2));
                    short superclass = (short)tmp.Data.ReadSci11EndianUInt16(tmp.Offset);

                    if (superclass >= 0)
                    {
                        int superclass_script = GetClass(superclass).script;

                        if (superclass_script == script_nr)
                        {
                            if (scr.Lockers != 0)
                                scr.DecrementLockers();  // Decrease lockers if this is us ourselves
                        }
                        else {
                            UninstantiateScript(superclass_script);
                        }
                        // Recurse to assure that the superclass lockers number gets decreased
                    }

                    reg.IncOffset((short)Script.SCRIPT_OBJECT_MAGIC_OFFSET);
                } // if object or class

                reg.IncOffset(-4); // Step back on header

            } while (objType != 0);
        }

        private Script AllocateScript(int script_nr, out ushort segid)
        {
            // Check if the script already has an allocated segment. If it
            // does, return that segment.
            segid = _scriptSegMap.GetValue(script_nr, 0);
            if (segid > 0)
            {
                return (Script)_heap[segid];
            }

            // allocate the SegmentObj
            var mem = AllocSegment(new Script(), ref segid);

            // Add the script to the "script id . segment id" hashmap
            _scriptSegMap[script_nr] = segid;

            return (Script)mem;
        }

        public SegmentObj AllocSegment(SegmentObj mem, ref ushort segid)
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
                SciString* saveDirString = allocateString(&_saveDirPtr);
                saveDirString.setSize(256);
                saveDirString.setValue(0, 0);

                _parserPtr = NULL_REG;  // no SCI2 game had a parser
#endif
            }
        }

        public void SetClassOffset(int index, Register offset)
        {
            _classTable[index].reg = offset;
        }

        public Register GetClassAddress(ushort classnr, ScriptLoadType loadType, ushort callerSegment)
        {
            if (classnr == 0xffff)
                return Register.NULL_REG;

            if (classnr < 0 || _classTable.Length <= classnr || _classTable[classnr].script < 0)
            {
                throw new InvalidOperationException($"[VM] Attempt to dereference class {classnr:x}, which doesn't exist (max {_classTable.Length:x})");
            }
            else {
                Class the_class = _classTable[classnr];
                if (the_class.reg.Segment == 0)
                {
                    GetScriptSegment(the_class.script, loadType);

                    if (the_class.reg.Segment == 0)
                    {
                        throw new InvalidOperationException($"[VM] Trying to instantiate class {classnr:x} by instantiating script 0x{the_class.script:x} ({the_class.script}) failed;");
                    }
                }
                else
                    if (callerSegment != the_class.reg.Segment)
                    GetScript(the_class.reg.Segment).IncrementLockers();

                return the_class.reg;
            }
        }

        private byte[] AllocDynmem(int size, string descr, out Register addr)
        {
            ushort seg = 0;
            SegmentObj mobj = AllocSegment(new DynMem(), ref seg);
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

        internal void ResetSegMan()
        {
            throw new NotImplementedException();
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
                        objpos.SetOffset((ushort)it.Value.Pos.Offset);
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

                        objpos.SetOffset((ushort)idx);
                        if (name == GetObjectName(objpos))
                            result.Add(objpos);
                    }
                }
            }

            if (result.Count == 0)
                return Register.NULL_REG;

            if (result.Count > 1 && index < 0)
            {
                // TODO: debug("findObjectByName(%s): multiple matches:", name.c_str());
                //for (i = 0; i < result.size(); i++)
                //    debug("  %3x: [%04x:%04x]", i, PRINT_REG(result[i]));
                return Register.NULL_REG; // Ambiguous
            }

            if (index < 0)
                return result[0];
            else if (result.Count <= (uint)index)
                return Register.NULL_REG; // Not found
            return result[index];
        }

        public DataStack AllocateStack(int size, ref ushort segid)
        {
            var mobj = AllocSegment(new DataStack(), ref segid);
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

        public void Memcpy(Register dest, byte[] src, ushort n)
        {
            SegmentRef dest_r = Dereference(dest);
            if (!dest_r.IsValid)
            {
                // TODO: warning("Attempt to memcpy to invalid pointer %04x:%04x", PRINT_REG(dest));
                return;
            }
            if (n > dest_r.maxSize)
            {
                // TODO: warning("Trying to dereference pointer %04x:%04x beyond end of segment", PRINT_REG(dest));
                return;
            }

            if (dest_r.isRaw)
            {
                // raw . raw
                Array.Copy(src, 0, dest_r.raw.Data, dest_r.raw.Offset, n);
            }
            else {
                // raw . non-raw
                for (uint i = 0; i < n; i++)
                    SetChar(dest_r, i, src[i]);
            }
        }

        /// <summary>
        /// Determines the segment occupied by a certain script, if any.
        /// </summary>
        /// <param name="script_id">Number of the script to look up</param>
        /// <returns>The script's segment ID, or 0 on failure</returns>
        public ushort GetScriptSegment(int script_id)
        {
            return _scriptSegMap.GetValue(script_id, 0);
        }

        public ushort GetScriptSegment(int script_nr, ScriptLoadType load)
        {
            ushort segment;

            if (load.HasFlag(ScriptLoadType.LOAD))
                InstantiateScript(script_nr);

            segment = GetScriptSegment(script_nr);

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
                throw new ArgumentOutOfRangeException("seg", $"SegManager::getScript(): seg id {seg:X} out of bounds");
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
            if (nameReg.IsNull)
                return "<no name>";

            string name = null;
            if (nameReg.Segment != 0)
                name = DerefString(nameReg);
            if (name == null)
            {
                // Crazy Nick Laura Bow is missing some object names needed for the static
                // selector vocabulary
                if (SciEngine.Instance.GameId == SciGameId.LAURABOW && pos == Register.Make(1, 0x2267))
                    return "Character";
                else
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
            var data = (ByteAccess)DerefPtr(this, pointer, entries, true);
            return ScummHelper.GetText(data.Data, data.Offset);
        }

        private SegmentRef Dereference(Register pointer)
        {
            SegmentRef ret = new SegmentRef();

            if (pointer.Segment == 0 || (pointer.Segment >= _heap.Count) || _heap[pointer.Segment] == null)
            {
                // This occurs in KQ5CD when interacting with certain objects
                // TODO: warning("SegManager::dereference(): Attempt to dereference invalid pointer %04x:%04x", PRINT_REG(pointer));
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
                // TODO: warning("Dereferencing pointer %04x:%04x (type %d) which is %s, but expected %s", PRINT_REG(pointer),
                //segMan.getSegmentType(pointer.getSegment()),
                //ret.isRaw ? "raw" : "not raw",
                //wantRaw ? "raw" : "not raw");
            }

            if (!wantRaw && ret.skipByte)
            {
                // TODO: warning("Unaligned pointer read: %04x:%04x expected with word alignment", PRINT_REG(pointer));
                return null;
            }

            if (entries > ret.maxSize)
            {
                // TODO: warning("Trying to dereference pointer %04x:%04x beyond end of segment", PRINT_REG(pointer));
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

        private SegmentType GetSegmentType(ushort seg)
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
                    else {
                        // TODO: warning("getObject(): Trying to get an invalid object");
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

        public Register AllocateHunkEntry(string hunk_type, int size)
        {
            HunkTable table;

            if (_hunksSegId == 0)
                AllocSegment(new HunkTable(), ref _hunksSegId);
            table = (HunkTable)_heap[_hunksSegId];

            ushort offset = (ushort)table.AllocEntry();

            Register addr = Register.Make(_hunksSegId, offset);
            Hunk h = table._table[offset].Item;

            if (h == null)
                return Register.NULL_REG;

            h.mem = new byte[size];
            h.size = size;
            h.type = hunk_type;

            return addr;
        }

        public void FreeHunkEntry(Register addr)
        {
            if (addr.IsNull)
            {
                // TODO: warning("Attempt to free a Hunk from a null address");
                return;
            }

            HunkTable ht = (HunkTable)GetSegment(addr.Segment, SegmentType.HUNK);

            if (ht == null)
            {
                // TODO: warning("Attempt to free Hunk from address %04x:%04x: Invalid segment type %d", PRINT_REG(addr), getSegmentType(addr.getSegment()));
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

        public void Strncpy(Register dest, string src, uint n)
        {
            SegmentRef dest_r = Dereference(dest);
            if (!dest_r.IsValid)
            {
                // TODO: warning("Attempt to strncpy to invalid pointer %04x:%04x", PRINT_REG(dest));
                return;
            }


            if (dest_r.isRaw)
            {
                // raw . raw
                if (n == 0xFFFFFFFFU)
                {
                    Array.Copy(src.ToCharArray().Select(c => (byte)c).ToArray(), 0, dest_r.raw.Data, dest_r.raw.Offset, src.Length);
                }
                else {
                    Array.Copy(src.ToCharArray().Select(c => (byte)c).ToArray(), 0, dest_r.raw.Data, dest_r.raw.Offset, (int)n);
                }
            }
            else {
                // raw . non-raw
                for (var i = 0; i < n && i < src.Length; i++)
                {
                    SetChar(dest_r, (uint)i, (byte)src[i]);
                    if (src[i] == 0)
                        break;
                }
                // Put an ending NUL to terminate the string
                if (dest_r.maxSize > n)
                    SetChar(dest_r, n, 0);
            }
        }

        private void Strncpy(Register dest, Register src, uint n)
        {
            if (src.IsNull)
            {
                // Clear target string instead.
                if (n > 0)
                    Strcpy(dest, "");

                return; // empty text
            }

            SegmentRef dest_r = Dereference(dest);
            SegmentRef src_r = Dereference(src);
            if (!src_r.IsValid)
            {
                // TODO: warning("Attempt to strncpy from invalid pointer %04x:%04x", PRINT_REG(src));

                // Clear target string instead.
                if (n > 0)
                    Strcpy(dest, "");
                return;
            }

            if (!dest_r.IsValid)
            {
                // TODO: warning("Attempt to strncpy to invalid pointer %04x:%04x", PRINT_REG(dest));
                return;
            }


            if (src_r.isRaw)
            {
                // raw . *
                Strncpy(dest, ScummHelper.GetText(src_r.raw.Data, src_r.raw.Offset), n);
            }
            else if (dest_r.isRaw && !src_r.isRaw)
            {
                // non-raw . raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(src_r, i);
                    dest_r.raw[i] = (byte)c;
                    if (c == 0)
                        break;
                }
            }
            else {
                // non-raw . non-raw
                for (var i = 0; i < n; i++)
                {
                    char c = GetChar(src_r, i);
                    SetChar(dest_r, (uint)i, (byte)c);
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

            throw new NotImplementedException();
            //Register val = @ref.reg[offset / 2];

            //// segment 0xFFFF means that the scripts are using uninitialized temp-variable space
            ////  we can safely ignore this, if it isn't one of the first 2 chars.
            ////  foreign lsl3 uses kFileIO(readraw) and then immediately uses kReadNumber right at the start
            //// TODO: 
            ////if (val.Segment != 0)
            ////    if (!((val.Segment == 0xFFFF) && (offset > 1)))
            ////        warning("Attempt to read character from non-raw data");

            //bool oddOffset = (offset & 1) != 0;
            //if (SciEngine.Instance.IsBE)
            //    oddOffset = !oddOffset;

            //return (char)(oddOffset ? val.Offset >> 8 : val.Offset & 0xff);
        }

        private static void SetChar(SegmentRef @ref, uint offset, byte value)
        {
            if (@ref.skipByte)
                offset++;

            StackPtr val = @ref.reg + (int)offset / 2;

            val[0].SetSegment(0);

            bool oddOffset = (offset & 1) != 0;
            if (SciEngine.Instance.IsBE)
                oddOffset = !oddOffset;

            if (oddOffset)
                val[0].SetOffset((ushort)((val[0].Offset & 0x00ff) | (value << 8)));
            else
                val[0].SetOffset((ushort)((val[0].Offset & 0xff00) | value));
        }

        public List AllocateList(out Register addr)
        {
            ListTable table;
            int offset;

            if (_listsSegId == 0)
                AllocSegment(new ListTable(), ref _listsSegId);
            table = (ListTable)_heap[_listsSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_listsSegId, (ushort)offset);
            return table._table[offset].Item;
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

            SegmentRef src_r = Dereference(pointer);
            if (!src_r.IsValid)
            {
                // TODO: warning("SegManager::getString(): Attempt to dereference invalid pointer %04x:%04x", PRINT_REG(pointer));
                return ret;
            }
            if (entries > src_r.maxSize)
            {
                // TODO: warning("Trying to dereference pointer %04x:%04x beyond end of segment", PRINT_REG(pointer));
                return ret;
            }
            if (src_r.isRaw)
                ret = ScummHelper.GetText(src_r.raw.Data, src_r.raw.Offset);
            else {
                var i = 0;
                for (;;)
                {
                    char c = GetChar(src_r, i);

                    if (c == 0)
                        break;

                    i++;
                    ret += c;
                };
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
                throw new InvalidOperationException("Attempt to use non-list {addr} as list");
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

        internal void DeallocateScript(int _nr)
        {
            throw new NotImplementedException();
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
                AllocSegment(new NodeTable(), ref _nodesSegId);
            table = (NodeTable)_heap[_nodesSegId];

            offset = table.AllocEntry();

            addr = Register.Make(_nodesSegId, (ushort)offset);
            return table._table[offset].Item;
        }
    }
}
