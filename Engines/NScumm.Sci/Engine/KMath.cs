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

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kAbs(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, (ushort)Math.Abs(argv.Value[0].ToInt16()));
        }

        public bool SignatureMatch(ushort[] signature, int argc, StackPtr? argv)
        {
            var sig = 0;
            var nextSig = 0;
            var curSig = nextSig;
            while (nextSig < signature.Length && argc != 0)
            {
                curSig = nextSig;
                int type = FindRegType(argv.Value[0]);

                if ((type & SIG_IS_INVALID) != 0 && (0 == (signature[curSig] & SIG_IS_INVALID)))
                    return false; // pointer is invalid and signature doesn't allow that?

                if (0 == ((type & ~SIG_IS_INVALID) & signature[curSig]))
                {
                    if ((type & ~SIG_IS_INVALID) == SIG_TYPE_ERROR && (signature[curSig] & SIG_IS_INVALID) != 0)
                    {
                        // Type is unknown (error - usually because of a deallocated object or
                        // stale pointer) and the signature allows invalid pointers. In this case,
                        // ignore the invalid pointer.
                    }
                    else {
                        return false; // type mismatch
                    }
                }

                if (0 == (signature[curSig] & SIG_MORE_MAY_FOLLOW))
                {
                    sig++;
                    nextSig = sig;
                }
                else {
                    signature[nextSig] |= SIG_IS_OPTIONAL; // more may follow . assumes followers are optional
                }
                argv++;
                argc--;
            }

            // Too many arguments?
            if (argc != 0)
                return false;
            // Signature end reached?
            if (signature[nextSig] == 0)
                return true;
            // current parameter is optional?
            if ((signature[curSig] & SIG_IS_OPTIONAL) != 0)
            {
                // yes, check if nothing more is required
                if (0 == (signature[curSig] & SIG_NEEDS_MORE))
                    return true;
            }
            else {
                // no, check if next parameter is optional
                if ((signature[nextSig] & SIG_IS_OPTIONAL) != 0)
                    return true;
            }
            // Too few arguments or more optional arguments required
            return false;
        }

        private int FindRegType(Register reg)
        {
            // No segment? Must be integer
            if (reg.Segment == 0)
                return SIG_TYPE_INTEGER | (reg.Offset != 0 ? 0 : SIG_TYPE_NULL);

            if (reg.Segment == 0xFFFF)
                return SIG_TYPE_UNINITIALIZED;

            // Otherwise it's an object
            SegmentObj mobj = _segMan.GetSegmentObj(reg.Segment);
            if (mobj == null)
                return SIG_TYPE_ERROR;

            var result = 0;
            if (!mobj.IsValidOffset((ushort)reg.Offset))
                result |= SIG_IS_INVALID;

            switch (mobj.Type)
            {
                case SegmentType.SCRIPT:
                    if (reg.Offset <= ((Script)mobj).BufSize &&
                        reg.Offset >= (uint)-Script.SCRIPT_OBJECT_MAGIC_OFFSET &&
                        ((Script)mobj).OffsetIsObject((int)reg.Offset))
                    {
                        result |= ((Script)mobj).GetObject((ushort)reg.Offset) != null ? SIG_TYPE_OBJECT : SIG_TYPE_REFERENCE;
                    }
                    else
                        result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.CLONES:
                    result |= SIG_TYPE_OBJECT;
                    break;
                case SegmentType.LOCALS:
                case SegmentType.STACK:
                case SegmentType.DYNMEM:
                case SegmentType.HUNK:
# if ENABLE_SCI32
                case SegmentType.ARRAY:
                case SegmentType.STRING:
#endif
                    result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.LISTS:
                    result |= SIG_TYPE_LIST;
                    break;
                case SegmentType.NODES:
                    result |= SIG_TYPE_NODE;
                    break;
                default:
                    return SIG_TYPE_ERROR;
            }
            return result;
        }
    }
}
