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
using System.Collections.Generic;

namespace NScumm.Sci.Engine
{
    enum ScriptObjectTypes
    {
        TERMINATOR,
        OBJECT,
        CODE,
        SYNONYMS,
        SAID,
        STRINGS,
        CLASS,
        EXPORTS,
        POINTERS,
        PRELOAD_TEXT, /* This is really just a flag. */
        LOCALVARS
    }

    class ObjMap : HashMap<ushort, SciObject>
    {
    }

    internal class Script : SegmentObj
    {
        /// <summary>
        /// Magical object identifier
        /// </summary>
        private const int SCRIPT_OBJECT_MAGIC_NUMBER = 0x1234;

        /// <summary>
        /// Offset of this identifier
        /// </summary>
        public static readonly int SCRIPT_OBJECT_MAGIC_OFFSET = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? -8 : 0;

        /// <summary>
        /// Script number
        /// </summary>
        private int _nr;
        /// <summary>
        /// Static data buffer, or NULL if not used
        /// </summary>
        private byte[] _buf;
        /// <summary>
        /// Start of heap if SCI1.1, NULL otherwise
        /// </summary>
        private ByteAccess _heapStart;

        /// <summary>
        /// Number of classes and objects that require this script
        /// </summary>
        private int _lockers;
        private int _scriptSize;
        private int _heapSize;
        private int _bufSize;

        /// <summary>
        /// Abs. offset of the export table or 0 if not present
        /// </summary>
        private UShortAccess _exportTable;
        /// <summary>
        /// Number of entries in the exports table
        /// </summary>
        private ushort _numExports;
        /// <summary>
        /// Synonyms block or 0 if not present
        /// </summary>
        private ByteAccess _synonyms;

        /// <summary>
        /// Number of entries in the synonyms block
        /// </summary>
        private ushort _numSynonyms;

        private int _localsOffset;
        private ushort _localsCount;

        private bool _markedAsDeleted;

        /// <summary>
        /// The local variable segment
        /// </summary>
        private ushort _localsSegment;
        private LocalVariables _localsBlock;

        /// <summary>
        /// Table for objects, contains property variables
        /// </summary>
        private ObjMap _objects;

        public ObjMap ObjectMap
        {
            get { return _objects; }
        }

        public int BufSize
        {
            get { return _bufSize; }
        }

        public ushort LocalsCount { get { return _localsCount; } }

        public int LocalsOffset { get { return _localsOffset; } }

        public ushort LocalsSegment { get { return _localsSegment; } }

        public StackPtr LocalsBegin { get { return _localsBlock != null ? new StackPtr(_localsBlock._locals, 0) : StackPtr.Null; } }

        /// <summary>
        /// Gets an offset to the beginning of the code block in a SCI3 script
        /// </summary>
        /// <returns></returns>
        public int CodeBlockOffsetSci3 { get { return (int)_buf.ReadSci11EndianUInt32(0); } }

        public int ScriptNumber
        {
            get { return _nr; }
        }

        public Script()
            : base(SegmentType.SCRIPT)
        {
            _lockers = 1;
            _objects = new ObjMap();
        }

        public ushort ValidateExportFunc(ushort pubfunct, bool relocSci3)
        {
            bool exportsAreWide = (SciEngine.Instance.Features.DetectLofsType() == SciVersion.V1_MIDDLE);

            if (_numExports <= pubfunct)
            {
                throw new InvalidOperationException("validateExportFunc(): pubfunct is invalid");
            }

            if (exportsAreWide)
                pubfunct *= 2;

            uint offset;

            if (ResourceManager.GetSciVersion() != SciVersion.V3)
            {
                offset = _exportTable.Data.ReadSci11EndianUInt16(_exportTable.Offset + pubfunct * 2);
            }
            else {
                if (!relocSci3)
                    offset = (uint)(_exportTable.Data.ReadSci11EndianUInt16(_exportTable.Offset + pubfunct * 2) + CodeBlockOffsetSci3);
                else
                    offset = RelocateOffsetSci3(pubfunct * 2 + 22);
            }

            // Check if the offset found points to a second export table (e.g. script 912
            // in Camelot and script 306 in KQ4). Such offsets are usually small (i.e. < 10),
            // thus easily distinguished from actual code offsets.
            // This only makes sense for SCI0-SCI1, as the export table in SCI1.1+ games
            // is located at a specific address, thus findBlockSCI0() won't work.
            // Fixes bugs #3039785 and #3037595.
            if (offset < 10 && ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                var secondExportTable = FindBlockSCI0(ScriptObjectTypes.EXPORTS, 0);

                if (secondExportTable != null)
                {
                    secondExportTable.Offset += 3 * 2; // skip header plus 2 bytes (secondExportTable is a uint16 pointer)
                    offset = secondExportTable.Data.ReadSci11EndianUInt16(secondExportTable.Offset + pubfunct);
                }
            }

            // Note that it's perfectly normal to return a zero offset, especially in
            // SCI1.1 and newer games. Examples include script 64036 in Torin's Passage,
            // script 64908 in the demo of RAMA and script 1013 in KQ6 floppy.

            if (offset >= _bufSize)
                throw new InvalidOperationException("Invalid export function pointer");

            return (ushort)offset;
        }

        public override bool IsValidOffset(ushort offset)
        {
            return offset < _bufSize;
        }

        public override SegmentRef Dereference(Register pointer)
        {
            if (pointer.Offset > _bufSize)
            {
                throw new InvalidOperationException($"Script::dereference(): Attempt to dereference invalid pointer {pointer} into script segment (script size={_bufSize})");
            }

            SegmentRef ret = new SegmentRef();
            ret.isRaw = true;
            ret.maxSize = (int)(_bufSize - pointer.Offset);
            ret.raw = new ByteAccess(_buf, (int)pointer.Offset);
            return ret;
        }

        public void DecrementLockers()
        {
            if (_lockers > 0)
                _lockers--;
        }

        public void InitializeClasses(SegManager segMan)
        {
            ushort mult = 0;
            ByteAccess seeker = null;

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                seeker = FindBlockSCI0(ScriptObjectTypes.CLASS);
                mult = 1;
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                seeker = new ByteAccess(_heapStart.Data, _heapStart.Offset + 4 + _heapStart.Data.ReadSci11EndianUInt16(_heapStart.Offset + 2) * 2);
                mult = 2;
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                seeker = GetSci3ObjectsPointer();
                mult = 1;
            }

            if (seeker == null)
                return;

            ushort marker;
            bool isClass = false;
            int classpos;
            short species = 0;

            while (true)
            {
                // In SCI0-SCI1, this is the segment type. In SCI11, it's a marker (0x1234)
                marker = seeker.Data.ReadSci11EndianUInt16(seeker.Offset);
                classpos = seeker.Offset;

                if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE && marker == 0)
                    break;

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && marker != SCRIPT_OBJECT_MAGIC_NUMBER)
                    break;

                if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
                {
                    isClass = (marker == (ushort)ScriptObjectTypes.CLASS);
                    if (isClass)
                        species = (short)seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 12);
                    classpos += 12;
                }
                else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                {
                    isClass = (seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 14) & SciObject.InfoFlagClass) != 0;  // -info- selector
                    species = (short)seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 10);
                }
                else if (ResourceManager.GetSciVersion() == SciVersion.V3)
                {
                    isClass = (seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 10) & SciObject.InfoFlagClass) != 0;
                    species = (short)seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 4);
                }

                if (isClass)
                {
                    // WORKAROUNDs for off-by-one script errors
                    if (species == segMan.ClassTableSize)
                    {
                        if (SciEngine.Instance.GameId == SciGameId.LSL2 && SciEngine.Instance.IsDemo)
                            segMan.ResizeClassTable(species + 1);
                        else if (SciEngine.Instance.GameId == SciGameId.LSL3 && !SciEngine.Instance.IsDemo && _nr == 500)
                            segMan.ResizeClassTable(species + 1);
                        else if (SciEngine.Instance.GameId == SciGameId.SQ3 && !SciEngine.Instance.IsDemo && _nr == 93)
                            segMan.ResizeClassTable(species + 1);
                        else if (SciEngine.Instance.GameId == SciGameId.SQ3 && !SciEngine.Instance.IsDemo && _nr == 99)
                            segMan.ResizeClassTable(species + 1);
                    }

                    if (species < 0 || species >= segMan.ClassTableSize)
                        throw new InvalidOperationException($"Invalid species {species}(0x{species:X}) unknown max {segMan.ClassTableSize}(0x{segMan.ClassTableSize:X}) while instantiating script {_nr}");

                    var segmentId = segMan.GetScriptSegment(_nr);
                    segMan.SetClassOffset(species, Register.Make((ushort)segmentId, (ushort)classpos));
                }

                seeker.Offset += seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 2) * mult;
            }
        }

        public List<Register> ListObjectReferences()
        {
            List<Register> tmp = new List<Register>();

            // Locals, if present
            if (_localsSegment != 0)
                tmp.Add(Register.Make(_localsSegment, 0));

            // All objects (may be classes, may be indirectly reachable)
            foreach (var obj in _objects)
            {
                tmp.Add(obj.Value.Pos);
            }

            return tmp;
        }

        public ByteAccess GetBuf(int offset = 0)
        {
            return new ByteAccess(_buf, offset);
        }

        public void InitializeObjects(SegManager segMan, ushort segmentId)
        {
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
                InitializeObjectsSci0(segMan, segmentId);
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                InitializeObjectsSci11(segMan, segmentId);
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
                InitializeObjectsSci3(segMan, segmentId);
        }

        private void InitializeObjectsSci3(object segMan, ushort segmentId)
        {
            throw new NotImplementedException();
        }

        private void InitializeObjectsSci11(object segMan, ushort segmentId)
        {
            throw new NotImplementedException();
        }

        private void InitializeObjectsSci0(SegManager segMan, ushort segmentId)
        {
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);

            // We need to make two passes, as the objects in the script might be in the
            // wrong order (e.g. in the demo of Iceman) - refer to bug #3034713
            for (int pass = 1; pass <= 2; pass++)
            {
                var seeker = new ByteAccess(_buf, (oldScriptHeader ? 2 : 0));

                do
                {
                    var objType = seeker.Data.ReadSci11EndianUInt16(seeker.Offset);
                    if (objType == 0)
                        break;

                    switch ((ScriptObjectTypes)objType)
                    {
                        case ScriptObjectTypes.OBJECT:
                        case ScriptObjectTypes.CLASS:
                            {
                                Register addr = Register.Make(segmentId, (ushort)(seeker.Offset + 4));
                                SciObject obj = ScriptObjInit(addr);
                                obj.InitSpecies(segMan, addr);

                                if (pass == 2)
                                {
                                    if (!obj.InitBaseObject(segMan, addr))
                                    {
                                        if ((_nr == 202 || _nr == 764) && SciEngine.Instance.GameId == SciGameId.KQ5)
                                        {
                                            // WORKAROUND: Script 202 of KQ5 French and German
                                            // (perhaps Spanish too?) has an invalid object.
                                            // This is non-fatal. Refer to bugs #3035396 and
                                            // #3150767.
                                            // Same happens with script 764, it seems to
                                            // contain junk towards its end.
                                            _objects.Remove((ushort)(addr.ToUInt16() - SCRIPT_OBJECT_MAGIC_OFFSET));
                                        }
                                        else {
                                            throw new InvalidOperationException("Failed to locate base object for object at {addr}");
                                        }
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    seeker.Offset += seeker.Data.ReadSci11EndianUInt16(seeker.Offset + 2);
                } while (seeker.Offset < _scriptSize - 2);
            }

            var relocationBlock = FindBlockSCI0(ScriptObjectTypes.POINTERS);
            if (relocationBlock != null)
                RelocateSci0Sci21(Register.Make(segmentId, (ushort)(relocationBlock.Offset + 4)));
        }

        private void RelocateSci0Sci21(Register block)
        {
            var heap = new ByteAccess(_buf);
            ushort heapSize = (ushort)_bufSize;
            ushort heapOffset = 0;

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                heap = _heapStart;
                heapSize = (ushort)_heapSize;
                heapOffset = (ushort)_scriptSize;
            }

            if (block.Offset >= heapSize ||
                heap.Data.ReadSci11EndianUInt16((int)(heap.Offset + block.Offset)) * 2 + block.Offset >= heapSize)
                throw new InvalidOperationException("Relocation block outside of script");

            int count = heap.Data.ReadSci11EndianUInt16((int)(heap.Offset + block.Offset));
            int exportIndex = 0;
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                pos = heap.Data.ReadSci11EndianUInt16((int)(heap.Offset + block.Offset + 2 + (exportIndex * 2))) + heapOffset;
                // This occurs in SCI01/SCI1 games where usually one export value is
                // zero. It seems that in this situation, we should skip the export and
                // move to the next one, though the total count of valid exports remains
                // the same
                if (pos == 0)
                {
                    exportIndex++;
                    pos = heap.Data.ReadSci11EndianUInt16((int)(heap.Offset + block.Offset + 2 + (exportIndex * 2))) + heapOffset;
                    if (pos == 0)
                        throw new InvalidOperationException("Script::relocate(): Consecutive zero exports found");
                }

                // In SCI0-SCI1, script local variables, objects and code are relocated.
                // We only relocate locals and objects here, and ignore relocation of
                // code blocks. In SCI1.1 and newer versions, only locals and objects
                // are relocated.
                if (!RelocateLocal(block.Segment, pos))
                {
                    // Not a local? It's probably an object or code block. If it's an
                    // object, relocate it.
                    foreach (var obj in _objects)
                    {
                        if (obj.Value.RelocateSci0Sci21(block.Segment, pos, _scriptSize))
                            break;
                    }
                }

                exportIndex++;
            }
        }

        private bool RelocateLocal(ushort segment, int location)
        {
            if (_localsBlock != null)
                return RelocateBlock(_localsBlock._locals, _localsOffset, segment, location, _scriptSize);
            else
                return false;
        }

        // This helper function is used by Script::relocateLocal and Object::relocate
        // Duplicate in segment.cpp and script.cpp
        private static bool RelocateBlock(Register[] block, int block_location, ushort segment, int location, int scriptSize)
        {
            int rel = location - block_location;

            if (rel < 0)
                return false;

            int idx = rel >> 1;

            if (idx >= block.Length)
                return false;

            if ((rel & 1) != 0)
            {
                throw new InvalidOperationException($"Attempt to relocate odd variable #{idx}.5e (relative to {block_location:X4})");
            }
            block[idx] = Register.SetSegment(block[idx], segment); // Perform relocation
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                block[idx] = Register.IncOffset(block[idx], (short)scriptSize);

            return true;
        }

        public void Load(int script_nr, ResourceManager resMan, ScriptPatcher scriptPatcher)
        {
            FreeScript();

            var script = resMan.FindResource(new ResourceId(ResourceType.Script, (ushort)script_nr), false);
            if (script == null)
                throw new InvalidOperationException("Script {script_nr} not found");

            _nr = script_nr;
            _bufSize = _scriptSize = script.size;

            if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
            {
                _bufSize += script.data.ToUInt16() * 2;
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                // In SCI1.1 - SCI2.1, the heap was in a separate space from the script. We append
                // it to the end of the script, and adjust addressing accordingly.
                // However, since we address the heap with a 16-bit pointer, the
                // combined size of the stack and the heap must be 64KB. So far this has
                // worked for SCI11, SCI2 and SCI21 games. SCI3 games use a different
                // script format, and theoretically they can exceed the 64KB boundary
                // using relocation.
                var heap = resMan.FindResource(new ResourceId(ResourceType.Heap, (ushort)script_nr), false);
                _bufSize += heap.size;
                _heapSize = heap.size;

                // Ensure that the start of the heap resource can be word-aligned.
                if ((script.size & 2) != 0)
                {
                    _bufSize++;
                    _scriptSize++;
                }

                // As mentioned above, the script and the heap together should not exceed 64KB
                if (script.size + heap.size > 65535)
                    throw new InvalidOperationException("Script and heap sizes combined exceed 64K. This means a fundamental " +
                            "design bug was made regarding SCI1.1 and newer games.\n" +
                            "Please report this error to the ScummVM team");
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                // Check for scripts over 64KB. These won't work with the current 16-bit address
                // scheme. We need an overlaying mechanism, or a mechanism to split script parts
                // in different segments to handle these. For now, simply stop when such a script
                // is found.
                //
                // Known large SCI 3 scripts are:
                // Lighthouse: 9, 220, 270, 351, 360, 490, 760, 765, 800
                // LSL7: 240, 511, 550
                // Phantasmagoria 2: none (hooray!)
                // RAMA: 70
                //
                // TODO: Remove this once such a mechanism is in place
                if (script.size > 65535)
                    throw new InvalidOperationException("TODO: SCI script {script_nr} is over 64KB - it's {script.size} bytes long. This can't " +
                          "be handled at the moment, thus stopping");
            }

            int extraLocalsWorkaround = 0;
            if (SciEngine.Instance.GameId == SciGameId.FANMADE && _nr == 1 && script.size == 11140)
            {
                // WORKAROUND: Script 1 in Ocean Battle doesn't have enough locals to
                // fit the string showing how many shots are left (a nasty script bug,
                // corrupting heap memory). We add 10 more locals so that it has enough
                // space to use as the target for its kFormat operation. Fixes bug
                // #3059871.
                extraLocalsWorkaround = 10;
            }
            _bufSize += extraLocalsWorkaround * 2;

            _buf = new byte[_bufSize];

            Array.Copy(script.data, _buf, script.size);

            // Check scripts for matching signatures and patch those, if found
            scriptPatcher.ProcessScript((ushort)_nr, _buf, script.size);

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                var heap = resMan.FindResource(new ResourceId(ResourceType.Heap, (ushort)_nr), false);

                _heapStart = new ByteAccess(_buf, _scriptSize);

                Array.Copy(heap.data, 0, _heapStart.Data, _heapStart.Offset, heap.size);
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                _exportTable = FindBlockSCI0(ScriptObjectTypes.EXPORTS)?.ToUInt16();
                if (_exportTable != null)
                {
                    _numExports = _exportTable.Data.ReadSci11EndianUInt16(_exportTable.Offset + 1 * 2);
                    _exportTable.Offset += 3 * 2;  // skip header plus 2 bytes (_exportTable is a uint16 pointer)
                }
                _synonyms = FindBlockSCI0(ScriptObjectTypes.SYNONYMS);
                if (_synonyms != null)
                {
                    _numSynonyms = (ushort)(_synonyms.Data.ReadSci11EndianUInt16(_synonyms.Offset + 2) / 4);
                    _synonyms.Offset += 4; // skip header
                }
                var localsBlock = FindBlockSCI0(ScriptObjectTypes.LOCALVARS);
                if (localsBlock != null)
                {
                    _localsOffset = localsBlock.Offset + 4;
                    _localsCount = (ushort)((_buf.ToUInt16(_localsOffset - 2) - 4) >> 1); // half block size
                }
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                if (_buf.ToUInt16(1 + 5) > 0)
                {   // does the script have an export table?
                    _exportTable = new UShortAccess(_buf, 1 + 5 + 2);
                    _numExports = _exportTable.Data.ReadSci11EndianUInt16(-1 * 2);
                }
                _localsOffset = _scriptSize + 4;
                _localsCount = _buf.ReadSci11EndianUInt16(_localsOffset - 2);
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                _localsCount = _buf.ToUInt16(12);
                _exportTable = new UShortAccess(_buf, 22);
                _numExports = _buf.ToUInt16(20);
                // SCI3 local variables always start dword-aligned
                if ((_numExports % 2) != 0)
                    _localsOffset = 22 + _numExports * 2;
                else
                    _localsOffset = 24 + _numExports * 2;
            }

            // WORKAROUND: Increase locals, if needed (check above)
            _localsCount = (ushort)(_localsCount + extraLocalsWorkaround);

            if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
            {
                // SCI0 early
                // Old script block. There won't be a localvar block in this case.
                // Instead, the script starts with a 16 bit int specifying the
                // number of locals we need; these are then allocated and zeroed.
                _localsCount = _buf.ToUInt16();
                _localsOffset = -_localsCount * 2; // Make sure it's invalid
            }
            else {
                // SCI0 late and newer
                // Does the script actually have locals? If not, set the locals offset to 0
                if (_localsCount == 0)
                    _localsOffset = 0;

                if (_localsOffset + _localsCount * 2 + 1 >= _bufSize)
                {
                    throw new InvalidOperationException($"Locals extend beyond end of script: offset {_localsOffset:4X}, count {_localsCount} vs size {_bufSize}");
                    //_localsCount = (_bufSize - _localsOffset) >> 1;
                }
            }
        }

        /// <summary>
        /// Initializes an object within the segment manager
        /// </summary>
        /// <param name="obj_pos">
        /// Location (segment, offset) of the object. It must
        /// point to the beginning of the script/class block
        /// (as opposed to what the VM considers to be the
        /// object location)
        /// </param>
        /// <param name="fullObjectInit"></param>
        /// <returns>
        /// A newly created Object describing the object,
        /// stored within the relevant script
        /// </returns>
        private SciObject ScriptObjInit(Register obj_pos, bool fullObjectInit = true)
        {
            if (ResourceManager.GetSciVersion() < SciVersion.V1_1 && fullObjectInit)
                obj_pos = Register.IncOffset(obj_pos, 8);   // magic offset (SCRIPT_OBJECT_MAGIC_OFFSET)

            if (obj_pos.Offset >= _bufSize)
                throw new InvalidOperationException("Attempt to initialize object beyond end of script");

            // Get the object at the specified position and init it. This will
            // automatically "allocate" space for it in the _objects map if necessary.
            SciObject obj = _objects[(ushort)obj_pos.Offset];
            obj.Init(_buf, obj_pos, fullObjectInit);

            return obj;
        }

        private ByteAccess GetSci3ObjectsPointer()
        {
            ByteAccess ptr;

            // SCI3 local variables always start dword-aligned
            if ((_numExports % 2) != 0)
                ptr = new ByteAccess(_buf, 22 + _numExports * 2);
            else
                ptr = new ByteAccess(_buf, 24 + _numExports * 2);

            // SCI3 object structures always start dword-aligned
            if ((_localsCount % 2) != 0)
                ptr.Offset += 2 + _localsCount * 2;
            else
                ptr.Offset += _localsCount * 2;

            return ptr;
        }

        /// <summary>
        /// Finds the pointer where a block of a specific type starts from,
        /// in SCI0 - SCI1 games
        /// </summary>
        /// <param name="type"></param>
        /// <param name="startBlockIndex"></param>
        /// <returns></returns>
        private ByteAccess FindBlockSCI0(ScriptObjectTypes type, int startBlockIndex = -1)
        {
            var buf = new ByteAccess(_buf);
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);
            int blockIndex = 0;

            if (oldScriptHeader)
                buf.Offset += 2;

            do
            {
                var blockType = (ScriptObjectTypes)buf.ReadUInt16();

                if (blockType == 0)
                    break;
                if (blockType == type && blockIndex > startBlockIndex)
                    return buf;

                int blockSize = buf.ReadUInt16(2);
                buf.Offset += blockSize;
                blockIndex++;
            } while (true);

            return null;
        }

        public SciObject GetObject(ushort offset)
        {
            if (_objects.ContainsKey(offset))
                return _objects[offset];
            else
                return null;
        }

        public void InitializeLocals(SegManager segMan)
        {
            LocalVariables locals = AllocLocalsSegment(segMan);
            if (locals != null)
            {
                if (ResourceManager.GetSciVersion() > SciVersion.V0_EARLY)
                {
                    var @base = LocalsOffset;

                    for (var i = 0; i < LocalsCount; i++)
                        locals._locals[i] = Register.Make(0, _buf.ReadSci11EndianUInt16(@base + i * 2));
                }
                else {
                    // In SCI0 early, locals are set at run time, thus zero them all here
                    for (var i = 0; i < LocalsCount; i++)
                        locals._locals[i] = Register.NULL_REG;
                }
            }
        }

        public bool OffsetIsObject(int offset)
        {
            return _buf.ReadSci11EndianUInt16(offset + SCRIPT_OBJECT_MAGIC_OFFSET) == SCRIPT_OBJECT_MAGIC_NUMBER;
        }

        /// <summary>
        /// Gets a value indicationg whether the script is marked as being deleted.
        /// </summary>
        public bool IsMarkedAsDeleted { get; private set; }

        /// <summary>
        /// Retrieves the number of exports of script.
        /// the number of exports of this script
        /// </summary>
        public ushort ExportsNr { get { return _numExports; } }

        public ushort ScriptSize
        {
            get { return (ushort)_scriptSize; }
        }

        public int Lockers
        {
            get { return _lockers; }
            set { _lockers = value; }
        }

        public ByteAccess Synonyms { get { return new ByteAccess(_synonyms); } }

        /// <summary>
        /// Retrieves the number of synonyms associated with this script.
        /// </summary>
        public int SynonymsNr { get { return _numSynonyms; } }

        /// <summary>
        /// Marks the script as deleted.
        /// This will not actually delete the script.  If references remain present on the
        /// heap or the stack, the script will stay in memory in a quasi-deleted state until
        /// either unreachable (resulting in its eventual deletion) or reloaded (resulting
        /// in its data being updated).
        /// </summary>
        public void MarkDeleted()
        {
            _markedAsDeleted = true;
        }

        public void IncrementLockers()
        {
            _lockers++;
        }

        public void FreeScript()
        {
            _nr = 0;

            _buf = null;
            _bufSize = 0;
            _scriptSize = 0;
            _heapStart = null;
            _heapSize = 0;

            _exportTable = null;
            _numExports = 0;
            _synonyms = null;
            _numSynonyms = 0;

            _localsOffset = 0;
            _localsSegment = 0;
            _localsBlock = null;
            _localsCount = 0;

            _lockers = 1;
            _markedAsDeleted = false;
            _objects.Clear();
        }

        private LocalVariables AllocLocalsSegment(SegManager segMan)
        {
            if (LocalsCount == 0)
            { // No locals
                return null;
            }
            else {
                LocalVariables locals;

                if (_localsSegment != 0)
                {
                    locals = (LocalVariables)segMan.GetSegment(_localsSegment, SegmentType.LOCALS);
                    if (locals == null || locals.Type != SegmentType.LOCALS || locals.script_id != ScriptNumber)
                        throw new InvalidOperationException("Invalid script locals segment while allocating locals");
                }
                else
                    locals = (LocalVariables)segMan.AllocSegment(new LocalVariables(), ref _localsSegment);

                _localsBlock = locals;
                locals.script_id = ScriptNumber;
                locals._locals = new Register[LocalsCount];

                return locals;
            }
        }

        public override List<Register> ListAllOutgoingReferences(Register addr)
        {
            List<Register> tmp = new List<Register>();
            if (addr.Offset <= _bufSize && addr.Offset >= (uint)-SCRIPT_OBJECT_MAGIC_OFFSET && OffsetIsObject((int)addr.Offset))
            {
                SciObject obj = GetObject((ushort)addr.Offset);
                if (obj != null)
                {
                    // Note all local variables, if we have a local variable environment
                    if (_localsSegment != 0)
                        tmp.Add(Register.Make(_localsSegment, 0));

                    for (var i = 0; i < obj.VarCount; i++)
                        tmp.Add(obj.GetVariable(i));
                }
                else {
                    throw new InvalidOperationException("Request for outgoing script-object reference at {addr} failed");
                }
            }
            else {
                /*		warning("Unexpected request for outgoing script-object references at %04x:%04x", PRINT_REG(addr));*/
                /* Happens e.g. when we're looking into strings */
            }
            return tmp;
        }

        public override List<Register> ListAllDeallocatable(ushort segId)
        {
            Register r = Register.Make(segId, 0);
            return new List<Register> { r };
        }

        public override Register FindCanonicAddress(SegManager segMan, Register addr)
        {
            addr = Register.SetOffset(addr, 0);
            return addr;
        }

        public override void FreeAtAddress(SegManager segMan, Register addr)
        {
            /*
		debugC(kDebugLevelGC, "[GC] Freeing script %04x:%04x", PRINT_REG(addr));
		if (_localsSegment)
			debugC(kDebugLevelGC, "[GC] Freeing locals %04x:0000", _localsSegment);
	*/

            if (_markedAsDeleted)
                segMan.DeallocateScript(_nr);
        }

        internal ushort RelocateOffsetSci3(int v)
        {
            throw new NotImplementedException();
        }
    }
}