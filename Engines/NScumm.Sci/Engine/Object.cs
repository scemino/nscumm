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

using NScumm.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NScumm.Sci.Engine
{
    internal class SciObject
    {
        public const int InfoFlagClone = 0x0001;
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
        private Register _infoSelectorSci3;
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
        private int _flags;

        public Register Pos { get { return _pos; } }

        public Register SpeciesSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    return _variables[_offset];
                else    // SCI3
                    return _speciesSelectorSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    _variables[_offset] = value;
                else    // SCI3
                    _speciesSelectorSci3 = value;
            }
        }

        public Register SuperClassSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    return _variables[_offset + 1];
                else    // SCI3
                    return _superClassPosSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    _variables[_offset + 1] = value;
                else    // SCI3
                    _superClassPosSci3 = value;
            }
        }

        public Register InfoSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    return _variables[_offset + 2];
                else    // SCI3
                    return _infoSelectorSci3;
            }
            set
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    _variables[_offset + 2] = value;
                else    // SCI3
                    _infoSelectorSci3 = value;
            }
        }

        public Register NameSelector
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                    return _offset + 3 < (ushort)_variables.Length ? _variables[_offset + 3] : Register.NULL_REG;
                else    // SCI3
                    return _variables.Length != 0 ? _variables[0] : Register.NULL_REG;
            }
        }



        public bool IsClass { get { return (InfoSelector.Offset & InfoFlagClass) != 0; } }

        public bool IsFreed { get { return (_flags & OBJECT_FLAG_FREED) != 0; } }

        public int VarCount { get { return _variables.Length; } }

        public uint MethodCount { get { return _methodCount; } }

        public SciObject()
        {
            _offset = ResourceManager.GetSciVersion() < SciVersion.V1_1 ? (ushort)0 : (ushort)5;
            _baseMethod = new List<ushort>();
        }

        public void MarkAsFreed()
        {
            _flags |= SciObject.OBJECT_FLAG_FREED;
        }

        public Register GetVariable(int var) { return _variables[var]; }

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

            if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                var obj = GetClass(segMan);
                varnum = ResourceManager.GetSciVersion() <= SciVersion.V1_LATE ? VarCount : obj.GetVariable(1).ToUInt16();
                buf = obj._baseVars.ToByte();
            }
            else if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                varnum = _variables.Length;
                buf = _baseVars.ToByte();
            }

            for (int i = 0; i < varnum; i++)
                if (buf.Data.ReadSci11EndianUInt16(buf.Offset + (i << 1)) == slc) // Found it?
                    return i; // report success

            return -1; // Failed
        }

        public void Init(byte[] buf, Register obj_pos, bool initVariables)
        {
            ByteAccess data = new ByteAccess(buf, (int)obj_pos.Offset);
            _baseObj = data;
            _pos = obj_pos;

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                Array.Resize(ref _variables, data.ReadUInt16(OffsetSelectorCounter));
                _baseVars = new UShortAccess(_baseObj, _variables.Length * 2);
                _methodCount = data.ReadUInt16(data.ReadUInt16(OffsetFunctionArea) - 2);
                for (int i = 0; i < _methodCount * 2 + 2; ++i)
                {
                    _baseMethod.Add(data.Data.ReadSci11EndianUInt16(data.Offset + data.ReadUInt16(OffsetFunctionArea) + i * 2));
                }
            }
            else if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
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
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                {
                    for (var i = 0; i < _variables.Length; i++)
                        _variables[i] = Register.Make(0, data.Data.ReadSci11EndianUInt16(data.Offset + (i * 2)));
                }
                else {
                    _infoSelectorSci3 = Register.Make(0, _baseObj.Data.ReadSci11EndianUInt16(_baseObj.Offset + 10));
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
            throw new NotImplementedException();
        }

        public void InitSpecies(SegManager segMan, Register addr)
        {
            ushort speciesOffset = (ushort)SpeciesSelector.Offset;

            if (speciesOffset == 0xffff)        // -1
                SpeciesSelector = Register.NULL_REG;   // no species
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
                    Register nameReg = NameSelector;
                    string name;
                    if (nameReg.IsNull)
                    {
                        name = "<no name>";
                    }
                    else {
                        nameReg = Register.SetSegment(nameReg, _pos.Segment);
                        name = segMan.DerefString(nameReg);
                        if (name == null)
                            name = "<invalid name>";
                    }

                    // TODO: debugC(kDebugLevelVM, "Object %04x:%04x (name %s, script %d) "

                    //"varnum doesn't match baseObj's: obj %d, base %d",
                    //PRINT_REG(_pos), name, objScript,
                    //originalVarCount, baseObj.getVarCount());

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
            ushort superClassOffset = (ushort)SuperClassSelector.Offset;

            if (superClassOffset == 0xffff)         // -1
                SuperClassSelector = Register.NULL_REG;    // no superclass
            else
                SuperClassSelector = segMan.GetClassAddress(superClassOffset, ScriptLoadType.LOCK, addr.Segment);
        }

        public bool RelocateSci0Sci21(ushort segment, int location, int scriptSize)
        {
            return RelocateBlock(_variables, (int)Pos.Offset, segment, location, scriptSize);
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
            block[idx]= Register.SetSegment(block[idx], segment); // Perform relocation
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                block[idx] = Register.IncOffset(block[idx], (short)scriptSize);

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
    }
}
