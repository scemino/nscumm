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

using System;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal class SciObject: IDisposable
    {
        public const int InfoFlagClone = 0x0001;
#if ENABLE_SCI32
        /**
         * When set, indicates to game scripts that a screen
         * item can be updated.
         */
        public const int InfoFlagViewVisible = 0x0008; // TODO: "dirty" ?

        /**
         * When set, the object has an associated screen item in
         * the rendering tree.
         */
        public const int InfoFlagViewInserted = 0x0010;
#endif
        public const int InfoFlagClass = 0x8000;

        public const int OffsetLocalVariables = -6;
        public const int OffsetFunctionArea = -4;
        public const int OffsetSelectorCounter = -2;

        public const int OffsetSelectorSegment = 0;
        public const int OffsetInfoSelectorSci0 = 4;
        public const int OffsetNamePointerSci0 = 6;
        public const int OffsetInfoSelectorSci11 = 14;
        public const int OffsetNamePointerSci11 = 16;

        private const int OBJECT_FLAG_FREED = (1 << 0);

        private const int EXTRA_GROUPS = 3;

        /// <summary>
        /// Object offset within its script; for clones, this is their base
        /// </summary>
        private Register _pos;

        private Register[] _variables;
        private ushort _offset;

        /// <summary>
        /// Register pointing to superclass for SCI3
        /// </summary>
        private Register _superClassPosSci3;

        /// <summary>
        /// Register containing species "selector" for SCI3
        /// </summary>
        private Register _speciesSelectorSci3;

        /// <summary>
        /// Register containing info "selector" for SCI3
        /// </summary>
        private StackPtr _infoSelectorSci3 = new StackPtr(new Register[1], 0);

        /// <summary>
        /// base + object offset within base
        /// </summary>
        private ByteAccess _baseObj;

        /// <summary>
        /// Pointer to the varselector area for this object
        /// </summary>
        private UShortAccess _baseVars;

        private ushort _methodCount;

        /// <summary>
        /// Pointer to the method selector area for this object
        /// </summary>
        private List<ushort> _baseMethod;

        /// <summary>
        /// This is used to enable relocation of property valuesa in SCI3.
        /// </summary>
        private uint[] _propertyOffsetsSci3;

        private int _flags;
        private bool[] _mustSetViewVisible;

        public Register Pos => _pos;

        public Register SpeciesSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    return _variables[_offset];
                return _speciesSelectorSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    _variables[_offset] = value;
                else // SCI3
                    _speciesSelectorSci3 = value;
            }
        }

        public Register SuperClassSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    return _variables[_offset + 1];
                return _superClassPosSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    _variables[_offset + 1] = value;
                else // SCI3
                    _superClassPosSci3 = value;
            }
        }

        public Register ClassScriptSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                    return _variables[4];
                return Register.Make(0, _baseObj.Data.ReadSci11EndianUInt16(_baseObj.Offset + 6));
            }
            set
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                    _variables[4] = value;
                else // SCI3
                    // This should never occur, this is called from a SCI1.1 - SCI2.1 only function
                    Error("setClassScriptSelector called for SCI3");
            }
        }

        public Register PropDictSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                    return _variables[2];
                Error("getPropDictSelector called for SCI3");
                return Register.NULL_REG;
            }
            set
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                    _variables[2] = value;
                else
                    // This should never occur, this is called from a SCI1.1 - SCI2.1 only function
                    Error("setPropDictSelector called for SCI3");
            }
        }

        public StackPtr InfoSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    return new StackPtr(_variables, _offset + 2);
                return _infoSelectorSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    _variables[_offset + 2] = value[0];
                else // SCI3
                    _infoSelectorSci3 = value;
            }
        }

        public StackPtr NameSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V3)
                    return _offset + 3 < (ushort) _variables.Length
                        ? new StackPtr(_variables, _offset + 3)
                        : StackPtr.Null;
                return _variables.Length != 0 ? new StackPtr(_variables, 0) : StackPtr.Null;
            }
        }


        public bool IsClass => (InfoSelector[0].Offset & InfoFlagClass) != 0;

        public bool IsFreed => (_flags & OBJECT_FLAG_FREED) != 0;

        public int VarCount => _variables.Length;

        public uint MethodCount => _methodCount;

        public SciObject()
        {
            _offset = ResourceManager.GetSciVersion() < SciVersion.V1_1 ? (ushort) 0 : (ushort) 5;
            _baseMethod = new List<ushort>();
        }

        public void Dispose()
        {
            if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                // FIXME: memory leak! Commented out because of reported heap
                // corruption by MSVC (e.g. in LSL7, when it starts)
                //free(_baseVars);
                //_baseVars = 0;
                //free(_propertyOffsetsSci3);
                //_propertyOffsetsSci3 = 0;
            }
        }

        public bool RelocateSci3(int segment, uint location, int offset, int scriptSize)
        {
            System.Diagnostics.Debug.Assert(_propertyOffsetsSci3 != null);

            for (int i = 0; i < _variables.Length; ++i)
            {
                if (location == _propertyOffsetsSci3[i])
                {
                    _variables[i] = Register.Make((ushort) segment, (ushort) (_variables[i].Offset + (short) offset));
                    return true;
                }
            }

            return false;
        }

        public int GetVarSelector(ushort i)
        {
            return _baseVars.Data.ReadSci11EndianUInt16(_baseVars.Offset + i);
        }

        public void MarkAsFreed()
        {
            _flags |= OBJECT_FLAG_FREED;
        }

        public Register GetVariable(int var)
        {
            return _variables[var];
        }

        public StackPtr GetVariableRef(int var)
        {
            return new StackPtr(_variables, var);
        }

        public SciObject GetClass(SegManager segMan)
        {
            return IsClass ? this : segMan.GetObject(SuperClassSelector);
        }

        public int LocateVarSelector(SegManager segMan, int slc)
        {
            ByteAccess buf = null;
            int varnum = 0;

            if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_LATE)
            {
                var obj = GetClass(segMan);
                varnum = ResourceManager.GetSciVersion() <= SciVersion.V1_LATE
                    ? VarCount
                    : obj.GetVariable(1).ToUInt16();
                buf = obj._baseVars.ToByteAccess();
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                varnum = _variables.Length;
                buf = _baseVars.ToByteAccess();
            }

            for (int i = 0; i < varnum; i++)
                if (buf.Data.ReadSci11EndianUInt16(buf.Offset + (i << 1)) == slc) // Found it?
                    return i; // report success

            return -1; // Failed
        }

        public void Init(byte[] buf, Register obj_pos, bool initVariables)
        {
            ByteAccess data = new ByteAccess(buf, (int) obj_pos.Offset);
            _baseObj = data;
            _pos = obj_pos;

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                Array.Resize(ref _variables, data.ToUInt16(OffsetSelectorCounter));
                _baseVars = new UShortAccess(_baseObj, _variables.Length * 2);
                _methodCount = data.ToUInt16(data.ToUInt16(OffsetFunctionArea) - 2);
                for (int i = 0; i < _methodCount * 2 + 2; ++i)
                {
                    _baseMethod.Add(
                        data.Data.ReadSci11EndianUInt16(data.Offset + data.ToUInt16(OffsetFunctionArea) + i * 2));
                }
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 &&
                     ResourceManager.GetSciVersion() <= SciVersion.V2_1_LATE)
            {
                Array.Resize(ref _variables, data.Data.ReadSci11EndianUInt16(data.Offset + 2));
                _baseVars = new UShortAccess(buf, data.Data.ReadSci11EndianUInt16(data.Offset + 4));
                _methodCount = buf.ReadSci11EndianUInt16(data.Data.ReadSci11EndianUInt16(data.Offset + 6));
                for (int i = 0; i < _methodCount * 2 + 3; ++i)
                {
                    _baseMethod.Add(buf.ReadSci11EndianUInt16(data.Data.ReadSci11EndianUInt16(data.Offset + 6) + i * 2));
                }
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                InitSelectorsSci3(buf);
            }

            if (initVariables)
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_LATE)
                {
                    for (var i = 0; i < _variables.Length; i++)
                        _variables[i] = Register.Make(0, data.Data.ReadSci11EndianUInt16(data.Offset + (i * 2)));
                }
                else
                {
                    _infoSelectorSci3[0] = Register.Make(0, _baseObj.Data.ReadSci11EndianUInt16(_baseObj.Offset + 10));
                }
            }
        }

        public SciObject Clone()
        {
            var clone = new SciObject
            {
                _pos = _pos,
                _offset = _offset,
                _superClassPosSci3 = _superClassPosSci3,
                _speciesSelectorSci3 = _speciesSelectorSci3,
                _infoSelectorSci3 = _infoSelectorSci3,
                _baseObj = new ByteAccess(_baseObj),
                _baseVars = new UShortAccess(_baseVars),
                _methodCount = _methodCount,
                _flags = _flags
            };
            clone._variables = _variables.ToArray();
            clone._baseMethod.AddRange(_baseMethod);
            return clone;
        }

        private void InitSelectorsSci3(byte[] buf)
        {
            BytePtr groupInfo = new BytePtr(_baseObj, 16);
            BytePtr selectorBase = new BytePtr(groupInfo, EXTRA_GROUPS * 32 * 2);
            int groups = SciEngine.Instance.Kernel.SelectorNamesSize / 32;
            int methods, properties;

            if ((SciEngine.Instance.Kernel.SelectorNamesSize % 32) != 0)
                ++groups;

            _mustSetViewVisible = new bool[groups];

            methods = properties = 0;

            // Selectors are divided into groups of 32, of which the first
            // two selectors are always reserved (because their storage
            // space is used by the typeMask).
            // We don't know beforehand how many methods and properties
            // there are, so we count them first.
            for (int groupNr = 0; groupNr < groups; ++groupNr)
            {
                byte groupLocation = groupInfo[groupNr];
                BytePtr seeker = new BytePtr(selectorBase, groupLocation * 32 * 2);

                if (groupLocation != 0)
                {
                    // This object actually has selectors belonging to this group
                    int typeMask = (int) seeker.Data.ReadSci11EndianUInt32(seeker.Offset);

                    _mustSetViewVisible[groupNr] = (typeMask & 1) != 0;

                    for (int bit = 2; bit < 32; ++bit)
                    {
                        int value = seeker.Data.ReadSci11EndianUInt16(seeker.Offset + bit * 2);
                        if ((typeMask & (1 << bit)) != 0)
                        {
                            // Property
                            ++properties;
                        }
                        else if (value != 0xffff)
                        {
                            // Method
                            ++methods;
                        }
                        else
                        {
                            // Undefined selector
                        }
                    }
                }
                else
                    _mustSetViewVisible[groupNr] = false;
            }

            _variables = new Register[properties];
            byte[] propertyIds = new byte[properties * sizeof(ushort)];
            //  uint16 *methodOffsets = (uint16 *)malloc(sizeof(uint16) * 2 * methods);
            uint[] propertyOffsets = new uint[properties];
            int propertyCounter = 0;
            int methodCounter = 0;

            // Go through the whole thing again to get the property values
            // and method pointers
            for (int groupNr = 0; groupNr < groups; ++groupNr)
            {
                byte groupLocation = groupInfo[groupNr];
                BytePtr seeker = new BytePtr(selectorBase, groupLocation * 32 * 2);

                if (groupLocation != 0)
                {
                    // This object actually has selectors belonging to this group
                    int typeMask = (int) seeker.Data.ReadSci11EndianUInt32(seeker.Offset);
                    int groupBaseId = groupNr * 32;

                    for (int bit = 2; bit < 32; ++bit)
                    {
                        int value = seeker.Data.ReadSci11EndianUInt16(seeker.Offset + bit * 2);
                        if ((typeMask & (1 << bit)) != 0)
                        {
                            // Property

                            // FIXME: We really shouldn't be doing endianness
                            // conversion here; instead, propertyIds should be converted
                            // to a Common::Array, like _baseMethod already is
                            // This interim solution fixes playing SCI3 PC games
                            // on Big Endian platforms

                            propertyIds.WriteSci11EndianUInt16(propertyCounter * sizeof(ushort),
                                (ushort) (groupBaseId + bit));
                            _variables[propertyCounter] = Register.Make(0, (ushort) value);
                            uint propertyOffset = ((uint) (seeker.Offset + bit * 2));
                            propertyOffsets[propertyCounter] = propertyOffset;
                            ++propertyCounter;
                        }
                        else if (value != 0xffff)
                        {
                            // Method
                            _baseMethod.Add((ushort) (groupBaseId + bit));
                            _baseMethod.Add((ushort) (value + buf.ReadSci11EndianUInt32()));
                            //                  methodOffsets[methodCounter] = (seeker + bit * 2) - buf;
                            ++methodCounter;
                        }
                        else
                        {
                            // Undefined selector
                        }
                    }
                }
            }

            _speciesSelectorSci3 = Register.Make(0, _baseObj.Data.ReadSci11EndianUInt16(_baseObj.Offset + 4));
            _superClassPosSci3 = Register.Make(0, _baseObj.Data.ReadSci11EndianUInt16(_baseObj.Offset + 8));

            _baseVars = new UShortAccess(propertyIds);
            _methodCount = (ushort) methods;
            _propertyOffsetsSci3 = propertyOffsets;
            //_methodOffsetsSci3 = methodOffsets;
        }

        public void InitSpecies(SegManager segMan, Register addr)
        {
            ushort speciesOffset = (ushort) SpeciesSelector.Offset;

            if (speciesOffset == 0xffff) // -1
                SpeciesSelector = Register.NULL_REG; // no species
            else
                SpeciesSelector = segMan.GetClassAddress(speciesOffset, ScriptLoadType.LOCK, addr.Segment);
        }

        public bool InitBaseObject(SegManager segMan, Register addr, bool doInitSuperClass = true)
        {
            var baseObj = segMan.GetObject(SpeciesSelector);

            if (baseObj != null)
            {
                int originalVarCount = _variables.Length;

                if (_variables.Length != baseObj.VarCount)
                    Array.Resize(ref _variables, baseObj.VarCount);
                // Copy base from species class, as we need its selector IDs
                _baseObj = baseObj._baseObj;
                if (doInitSuperClass)
                    InitSuperClass(segMan, addr);

                if (_variables.Length != originalVarCount)
                {
                    // These objects are probably broken.
                    // An example is 'witchCage' in script 200 in KQ5 (#3034714),
                    // but also 'girl' in script 216 and 'door' in script 22.
                    // In LSL3 a number of sound objects trigger this right away.
                    // SQ4-floppy's bug #3037938 also seems related.

                    // The effect is that a number of its method selectors may be
                    // treated as variable selectors, causing unpredictable effects.
                    int objScript = segMan.GetScript(_pos.Segment).ScriptNumber;

                    // We have to do a little bit of work to get the name of the object
                    // before any relocations are done.
                    StackPtr nameReg = NameSelector;
                    string name;
                    if (nameReg[0].IsNull)
                    {
                        name = "<no name>";
                    }
                    else
                    {
                        nameReg.SetSegment(0, _pos.Segment);
                        name = segMan.DerefString(nameReg[0]);
                        if (name == null)
                            name = "<invalid name>";
                    }

                    DebugC(DebugLevels.VM, "Object {0} (name {1}, script {2}) "
                                           + "varnum doesn't match baseObj's: obj {3}, base {4}",
                        _pos, name, objScript,
                        originalVarCount, baseObj.VarCount);

#if None
// We enumerate the methods selectors which could be hidden here
			if (getSciVersion() <= SCI_VERSION_2_1) {
				const SegmentRef objRef = segMan.dereference(baseObj._pos);
				uint segBound = objRef.maxSize/2 - baseObj.getVarCount();
				const byte* buf = (const byte *)baseObj._baseVars;
				if (!buf) {
					// While loading this may happen due to objects being loaded
					// out of order, and we can't proceed then, unfortunately.
					segBound = 0;
				}
				for (uint i = baseObj.getVarCount();
				         i < originalVarCount && i < segBound; ++i) {
					uint16 slc = READ_SCI11ENDIAN_UINT16(buf + 2*i);
					// Skip any numbers which happen to be varselectors too
					bool found = false;
					for (uint j = 0; j < baseObj.getVarCount() && !found; ++j)
						found = READ_SCI11ENDIAN_UINT16(buf + 2*j) == slc;
					if (found) continue;
					// Skip any selectors which aren't method selectors,
					// so couldn't be mistaken for varselectors
					if (lookupSelector(segMan, _pos, slc, 0, 0) != kSelectorMethod) continue;
					warning("    Possibly affected selector: %02x (%s)", slc,
					        g_sci.getKernel().getSelectorName(slc).c_str());
				}
			}
#endif
                }

                return true;
            }

            return false;
        }

        private void InitSuperClass(SegManager segMan, Register addr)
        {
            ushort superClassOffset = (ushort) SuperClassSelector.Offset;

            if (superClassOffset == 0xffff) // -1
                SuperClassSelector = Register.NULL_REG; // no superclass
            else
                SuperClassSelector = segMan.GetClassAddress(superClassOffset, ScriptLoadType.LOCK, addr.Segment);
        }

        public bool RelocateSci0Sci21(ushort segment, int location, int scriptSize)
        {
            return RelocateBlock(_variables, (int) Pos.Offset, segment, location, scriptSize);
        }

        // This helper function is used by Script::relocateLocal and Object::relocate
        // Duplicate in segment.cpp and script.cpp
        private static bool RelocateBlock(Register[] block, int block_location, ushort segment, int location,
            int scriptSize)
        {
            int rel = location - block_location;

            if (rel < 0)
                return false;

            int idx = rel >> 1;

            if (idx >= block.Length)
                return false;

            if ((rel & 1) != 0)
            {
                throw new InvalidOperationException(
                    $"Attempt to relocate odd variable #{idx}.5e (relative to {block_location:X4})");
            }
            block[idx] = Register.Make(segment, (ushort) block[idx].Offset); // Perform relocation
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 &&
                ResourceManager.GetSciVersion() <= SciVersion.V2_1_LATE)
                block[idx] = Register.IncOffset(block[idx], (short) scriptSize);

            return true;
        }

        public Register GetFunction(int i)
        {
            var offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? _methodCount + 1 + i : i * 2 + 2;
            if (ResourceManager.GetSciVersion() == SciVersion.V3)
                offset--;
            return Register.Make(_pos.Segment, _baseMethod[offset]);
        }

        /// <summary>
        /// Determines if this object is a class and explicitly defines the
        /// selector as a funcselector. Does NOT say anything about the object's
        /// superclasses, i.e. failure may be returned even if one of the
        /// superclasses defines the funcselector
        /// </summary>
        /// <param name="selectorId"></param>
        /// <returns></returns>
        public int FuncSelectorPosition(int sel)
        {
            for (var i = 0; i < _methodCount; i++)
                if (GetFuncSelector(i) == sel)
                    return i;

            return -1;
        }

        public int GetFuncSelector(int i)
        {
            int offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? i : i * 2 + 1;
            if (ResourceManager.GetSciVersion() == SciVersion.V3)
                offset--;
            return _baseMethod[offset];
        }
#if ENABLE_SCI32
        public void SetInfoSelectorFlag(int flag)
        {
            if (ResourceManager.GetSciVersion() < SciVersion.V3)
            {
                _variables[_offset + 2] |= flag;
            }
            else
            {
                _infoSelectorSci3[0] |= flag;
            }
        }

        public void ClearInfoSelectorFlag(int flag)
        {
            if (ResourceManager.GetSciVersion() < SciVersion.V3)
            {
                _variables[_offset + 2] = _variables[_offset + 2] & ~flag;
            }
            else
            {
                _infoSelectorSci3[0] = _infoSelectorSci3[0] & ~flag;
            }
        }
#endif
    }
}