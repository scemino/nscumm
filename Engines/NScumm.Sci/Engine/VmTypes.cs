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
    // Opcode formats
    enum opcode_format
    {
        Script_Invalid = -1,
        Script_None = 0,
        Script_Byte,
        Script_SByte,
        Script_Word,
        Script_SWord,
        Script_Variable,
        Script_SVariable,
        Script_SRelative,
        Script_Property,
        Script_Global,
        Script_Local,
        Script_Temp,
        Script_Param,
        Script_Offset,
        Script_End
    }

    internal class Register
    {
        /// <summary>
        ///  Special reg_t 'offset' used to indicate an error, or that an operation has
        ///  finished (depending on the case).
        /// </summary>
        const ushort SIGNAL_OFFSET = ushort.MaxValue;

        public static readonly Register NULL_REG = Make(0, 0);
        public static readonly Register SIGNAL_REG = Make(0, SIGNAL_OFFSET);
        public static readonly Register TRUE_REG = Make(0, 1);

        // Segment and offset. These should never be accessed directly
        ushort _segment;
        ushort _offset;

        public uint Offset
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                {
                    return _offset;
                }
                else {
                    // Return the lower 16 bits from the offset, and the 17th and 18th bits from the segment
                    return (uint)(((_segment & 0xC000) << 2) | _offset);
                }
            }
        }

        public ushort Segment
        {
            get
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                {
                    return _segment;
                }
                else {
                    // Return the lower 14 bits of the segment
                    return (ushort)(_segment & 0x3FFF);
                }
            }
        }

        public bool IsNull
        {
            get
            {
                return (Offset | Segment) == 0;
            }
        }

        public bool IsNumber
        {
            get
            {
                return Segment == 0;
            }
        }

        public bool IsPointer
        {
            get { return Segment != 0 && Segment != 0xFFFF; }
        }

        public void SetSegment(ushort segment)
        {
            if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                _segment = segment;
            }
            else {
                // Set the lower 14 bits of the segment, and preserve the upper 2 ones for the offset
                _segment = (ushort)((_segment & 0xC000) | (segment & 0x3FFF));
            }
        }

        public ushort ToUInt16()
        {
            return (ushort)Offset;
        }

        public short ToInt16()
        {
            return (short)Offset;
        }

        public void SetOffset(ushort offset)
        {
            if (ResourceManager.GetSciVersion() <= SciVersion.V2_1)
            {
                _offset = offset;
            }
            else {
                // Store the lower 16 bits in the offset, and the 17th and 18th bits in the segment
                _offset = (ushort)(offset & 0xFFFF);
                _segment = (ushort)(((offset & 0x30000) >> 2) | (_segment & 0x3FFF));
            }
        }

        public void Set(Register other)
        {
            _segment = other._segment;
            _offset = other._offset;
        }

        public static Register Make(ushort segment, ushort offset)
        {
            Register r = new Register();
            r.SetSegment(segment);
            r.SetOffset(offset);
            return r;
        }

        public override int GetHashCode()
        {
            return _segment ^ _offset;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Register)) return false;
            var other = (Register)obj;
            return _offset == other._offset && _segment == other._segment;
        }

        public static bool operator ==(Register r1, Register r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(Register r1, Register r2)
        {
            return !(r1 == r2);
        }

        public static Register operator +(Register left, Register right)
        {
            if (left.IsPointer && right.IsNumber)
            {
                // Pointer arithmetics. Only some pointer types make sense here
                SegmentObj mobj = SciEngine.Instance.EngineState._segMan.GetSegmentObj(left.Segment);

                if (mobj == null)
                    throw new InvalidOperationException($"[VM]: Attempt to add {right.Offset} to invalid pointer {left}");

                switch (mobj.Type)
                {
                    case SegmentType.LOCALS:
                    case SegmentType.SCRIPT:
                    case SegmentType.STACK:
                    case SegmentType.DYNMEM:
                        return Make(left.Segment, (ushort)(left.Offset + right.ToInt16()));
                    default:
                        return left.LookForWorkaround(right, "addition");
                }
            }
            else if (left.IsNumber && right.IsPointer)
            {
                // Adding a pointer to a number, flip the order
                return right + left;
            }
            else if (left.IsNumber && right.IsNumber)
            {
                // Normal arithmetics
                return Make(0, (ushort)(left.ToInt16() + right.ToInt16()));
            }
            else {
                return left.LookForWorkaround(right, "addition");
            }
        }

        public static Register operator -(Register left, Register right)
        {
            if (left.Segment == right.Segment)
            {
                // We can subtract numbers, or pointers with the same segment,
                // an operation which will yield a number like in C
                return Make(0, (ushort)(left.ToInt16() - right.ToInt16()));
            }
            else {
                return left + Make(right.Segment, (ushort)(-right.ToInt16()));
            }
        }

        public void IncOffset(short offset)
        {
            SetOffset((ushort)(Offset + offset));
        }

        public override string ToString()
        {
            return $"{_segment:X4}:{_offset:X4}";
        }

        public ushort RequireUInt16()
        {
            if (IsNumber)
                return ToUInt16();
            else
                // The right parameter is NULL_REG because
                // we're not comparing *this with anything here.
                return LookForWorkaround(NULL_REG, "require unsigned number").ToUInt16();
        }

        public short RequireInt16()
        {
            if (IsNumber)
                return ToInt16();
            else
                // The right parameter is NULL_REG because
                // we're not comparing *this with anything here.
                return LookForWorkaround(NULL_REG, "require signed number").ToInt16();
        }

        private Register LookForWorkaround(Register right, string operation)
        {
            SciTrackOriginReply originReply;
            SciWorkaroundSolution solution = Workarounds.TrackOriginAndFindWorkaround(0, Workarounds.ArithmeticWorkarounds, out originReply);
            if (solution.type == SciWorkaroundType.NONE)
            {
                throw new InvalidOperationException($"Invalid arithmetic operation ({operation} - params: {this} and {right}) from method {originReply.objectName}::{originReply.methodName} (room {SciEngine.Instance.EngineState.CurrentRoomNumber}, script {originReply.scriptNr}, localCall {originReply.localCallOffset:X})");
            }

            // assert(solution.type == WORKAROUND_FAKE);
            return Make(0, solution.value);
        }
    }

    /// <summary>
    /// A true 32-bit Register
    /// </summary>
    internal class Register32
    {
        // Segment and offset. These should never be accessed directly
        public int Segment { get; set; }
        public int Offset { get; set; }

        public void IncOffset(int offset)
        {
            Offset += offset;
        }

        public override int GetHashCode()
        {
            return Segment ^ Offset;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Register32)) return false;
            var other = (Register32)obj;
            return Offset == other.Offset && Segment == other.Segment;
        }

        public static bool operator ==(Register32 r1, Register32 r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(Register32 r1, Register32 r2)
        {
            return !(r1 == r2);
        }

        public static Register32 Make(int segment, int offset)
        {
            return new Register32 { Segment = segment, Offset = offset };
        }
    }
}
