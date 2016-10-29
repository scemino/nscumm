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
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    // Opcode formats
    internal enum opcode_format
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

    internal struct Register : IComparable<Register>
    {
        /// <summary>
        ///  Special reg_t 'offset' used to indicate an error, or that an operation has
        ///  finished (depending on the case).
        /// </summary>
        public const ushort SIGNAL_OFFSET = ushort.MaxValue;

        public const int Size = 4;

        public static readonly Register NULL_REG = Make(0, 0);
        public static readonly Register SIGNAL_REG = Make(0, SIGNAL_OFFSET);
        public static readonly Register TRUE_REG = Make(0, 1);

        // Segment and offset. These should never be accessed directly
        private BytePtr _data;

        private BytePtr Data => _data == BytePtr.Null ? (_data = new BytePtr(new byte[4])) : _data;

        private ushort _offset
        {
            get { return Data.ToUInt16(); }
            set { Data.WriteUInt16(0, value); }
        }

        private ushort _segment
        {
            get { return Data.ToUInt16(2); }
            set { Data.WriteUInt16(2, value); }
        }

        public Register(BytePtr data)
        {
            _data = data;
        }

        private Register(ushort segment, ushort offset)
            : this()
        {
            Segment = segment;
            Offset = offset;
        }

        public uint Offset
        {
            get
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                {
                    return _offset;
                }
                // Return the lower 16 bits from the offset, and the 17th and 18th bits from the segment
                return (uint)(((Segment & 0xC000) << 2) | _offset);
            }
            set
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                {
                    _offset = (ushort)value;
                }
                else
                {
                    // Store the lower 16 bits in the offset, and the 17th and 18th bits in the segment
                    _offset = (ushort)(value & 0xFFFF);
                    _segment = (ushort)(((value & 0x30000) >> 2) | (_segment & 0x3FFF));
                }
            }
        }

        public ushort Segment
        {
            get
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                {
                    return _segment;
                }
                // Return the lower 14 bits of the segment
                return (ushort)(_segment & 0x3FFF);
            }
            set
            {
                if (ResourceManager.GetSciVersion() < SciVersion.V3)
                {
                    _segment = value;
                }
                else
                {
                    // Set the lower 14 bits of the segment, and preserve the upper 2 ones for the offset
                    _segment = (ushort)((_segment & 0xC000) | (value & 0x3FFF));
                }
            }
        }

        public bool IsNull => (Offset | Segment) == 0;

        public bool IsNumber => Segment == 0;

        public bool IsPointer => Segment != 0 && Segment != 0xFFFF;

        public bool IsInitialized => Segment != 0xFFFF;

        public ushort ToUInt16()
        {
            return (ushort)Offset;
        }

        public short ToInt16()
        {
            return (short)Offset;
        }

        public void CopyTo(byte[] data, int offset)
        {
            Array.Copy(_data.Data, _data.Offset, data, offset, 4);
        }

        public void Set(Register copy)
        {
            Segment = copy.Segment;
            Offset = copy.Offset;
        }

        public static Register Make(ushort segment, ushort offset)
        {
            Register r = new Register(segment, offset);
            return r;
        }

        public static Register Make(ushort segment, bool condition)
        {
            return Make(segment, (ushort)(condition ? 1 : 0));
        }

        public override int GetHashCode()
        {
            return Segment.GetHashCode() ^ Offset.GetHashCode();
        }

        public int CompareTo(Register other)
        {
            return Compare(other, false);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Register)) return false;
            var other = (Register)obj;
            return Offset == other.Offset && Segment == other.Segment;
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
            if (left.IsNumber && right.IsPointer)
            {
                // Adding a pointer to a number, flip the order
                return right + left;
            }
            if (left.IsNumber && right.IsNumber)
            {
                // Normal arithmetics
                return Make(0, (ushort)(left.ToInt16() + right.ToInt16()));
            }
            return left.LookForWorkaround(right, "addition");
        }

        public static Register operator +(Register left, int right)
        {
            return left + Make(0, (ushort)right);
        }

        public static Register operator -(Register left, Register right)
        {
            if (left.Segment == right.Segment)
            {
                // We can subtract numbers, or pointers with the same segment,
                // an operation which will yield a number like in C
                return Make(0, (ushort)(left.ToInt16() - right.ToInt16()));
            }
            return left + Make(right.Segment, (ushort)(-right.ToInt16()));
        }

        public static Register operator -(Register left, int right)
        {
            return left - Make(0, (ushort)right);
        }

        public static Register operator *(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber)
                return Make(0, (ushort)(left.ToInt16() * right.ToInt16()));
            return left.LookForWorkaround(right, "multiplication");
        }

        public static Register operator /(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber && !right.IsNull)
                return Make(0, (ushort)(left.ToInt16() / right.ToInt16()));
            return left.LookForWorkaround(right, "division");
        }

        public static Register operator %(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber && !right.IsNull)
            {
                // Support for negative numbers was added in Iceman, and perhaps in
                // SCI0 0.000.685 and later. Theoretically, this wasn't really used
                // in SCI0, so the result is probably unpredictable. Such a case
                // would indicate either a script bug, or a modulo on an unsigned
                // integer larger than 32767. In any case, such a case should be
                // investigated, instead of being silently accepted.
                if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE && (left.ToInt16() < 0 || right.ToInt16() < 0))
                    Warning("Modulo of a negative number has been requested for SCI0. This *could* lead to issues");
                short value = left.ToInt16();
                short modulo = Math.Abs(right.ToInt16());
                short result = (short)(value % modulo);
                if (result < 0)
                    result += modulo;
                return Make(0, (ushort)result);
            }
            return left.LookForWorkaround(right, "modulo");
        }

#if ENABLE_SCI32
        public static Register operator &(Register left, int right)
        {
            return left & Make(0, (ushort)right);
        }

        public static Register operator |(Register left, int right)
        {
            return left | Make(0, (ushort)right);
        }

        public static Register operator ^(Register left, int right)
        {
            return left ^ Make(0, (ushort)right);
        }

        //        public Register operator&=(short right) { this = this & right; }
        //        void operator|=(int16 right) { *this = *this | right; }
        //        void operator^=(int16 right) { *this = *this ^ right; }
#endif

        public Register ShiftRight(Register right)
        {
            if (IsNumber && right.IsNumber)
                return Make(0, (ushort)(ToUInt16() >> right.ToUInt16()));
            return LookForWorkaround(right, "shift right");
        }

        public Register ShiftLeft(Register right)
        {
            if (IsNumber && right.IsNumber)
                return Make(0, (ushort)(ToUInt16() << right.ToUInt16()));
            return LookForWorkaround(right, "shift left");
        }

        public static Register IncOffset(Register reg, short offset)
        {
            return Make(reg.Segment, (ushort)(reg.Offset + offset));
        }

        public static Register operator &(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber)
                return Make(0, (ushort)(left.ToUInt16() & right.ToUInt16()));
            return left.LookForWorkaround(right, "bitwise AND");
        }

        public static Register operator |(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber)
                return Make(0, (ushort)(left.ToUInt16() | right.ToUInt16()));
            return left.LookForWorkaround(right, "bitwise OR");
        }

        public static Register operator ^(Register left, Register right)
        {
            if (left.IsNumber && right.IsNumber)
                return Make(0, (ushort)(left.ToUInt16() ^ right.ToUInt16()));
            return left.LookForWorkaround(right, "bitwise XOR");
        }

        public static bool operator >(Register left, Register right)
        {
            return left.Compare(right, false) > 0;
        }

        public static bool operator >=(Register left, Register right)
        {
            return left.Compare(right, false) >= 0;
        }

        public static bool operator <(Register left, Register right)
        {
            return left.Compare(right, false) < 0;
        }

        public static bool operator <=(Register left, Register right)
        {
            return left.Compare(right, false) <= 0;
        }

        // Same as the normal operators, but perform unsigned
        // integer checking
        public bool GreaterThanUnsigned(Register right)
        {
            return Compare(right, true) > 0;
        }

        public bool GreaterOrEqualsUnsigned(Register right)
        {
            return Compare(right, true) >= 0;
        }

        public bool LowerThanUnsigned(Register right)
        {
            return Compare(right, true) < 0;
        }

        public bool LowerOrEqualsUnsigned(Register right)
        {
            return Compare(right, true) <= 0;
        }

        public override string ToString()
        {
            return $"{Segment:x4}:{Offset:x4}";
        }

        public ushort RequireUInt16()
        {
            if (IsNumber)
                return ToUInt16();
            return LookForWorkaround(NULL_REG, "require unsigned number").ToUInt16();
        }

        public short RequireInt16()
        {
            if (IsNumber)
                return ToInt16();
            return LookForWorkaround(NULL_REG, "require signed number").ToInt16();
        }

        private Register LookForWorkaround(Register right, string operation)
        {
            SciTrackOriginReply originReply;
            SciWorkaroundSolution solution = Workarounds.TrackOriginAndFindWorkaround(0,
                Workarounds.ArithmeticWorkarounds, out originReply);
            if (solution.type == SciWorkaroundType.NONE)
            {
                throw new InvalidOperationException(
                    $"Invalid arithmetic operation ({operation} - params: {this} and {right}) from method {originReply.objectName}::{originReply.methodName} (room {SciEngine.Instance.EngineState.CurrentRoomNumber}, script {originReply.scriptNr}, localCall {originReply.localCallOffset:X})");
            }

            // assert(solution.type == WORKAROUND_FAKE);
            return Make(0, solution.value);
        }

        private int Compare(Register right, bool treatAsUnsigned)
        {
            if (Segment == right.Segment)
            {
                // can compare things in the same segment
                if (treatAsUnsigned || !IsNumber)
                    return ToUInt16() - right.ToUInt16();
                return ToInt16() - right.ToInt16();
            }
#if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                return Sci32Comparison(right);
#endif
            if (PointerComparisonWithInteger(right))
            {
                return 1;
            }
            if (right.PointerComparisonWithInteger(this))
            {
                return -1;
            }
            return LookForWorkaround(right, "comparison").ToInt16();
        }

# if ENABLE_SCI32
        private int Sci32Comparison(Register right)
        {
            // In SCI32, MemIDs are normally indexes into the memory manager's handle
            // list, but the engine reserves indexes at and above 20000 for objects
            // that were created inside the engine (as opposed to inside the VM). The
            // engine compares these as a tiebreaker for graphics objects that are at
            // the same priority, and it is necessary to at least minimally handle
            // this situation.
            // This is obviously a bogus comparision, but then, this entire thing is
            // bogus. For the moment, it just needs to be deterministic.
            if (IsNumber && !right.IsNumber)
            {
                return 1;
            }
            else if (right.IsNumber && !IsNumber)
            {
                return -1;
            }

            return (int)(Offset - right.Offset);
        }
#endif

        private bool PointerComparisonWithInteger(Register right)
        {
            // This function handles the case where a script tries to compare a pointer
            // to a number. Normally, we would not want to allow that. However, SCI0 -
            // SCI1.1 scripts do this in order to distinguish references to
            // external resources (which are numbers) from pointers. In
            // our SCI implementation, such a check may seem pointless, as
            // one can simply use the segment value to achieve this goal.
            // But Sierra's SCI did not have the notion of segment IDs, so
            // both pointer and numbers were simple integers.
            //
            // But for some things, scripts had (and have) to distinguish between
            // numbers and pointers. Lacking the segment information, Sierra's
            // developers resorted to a hack: If an integer is smaller than a certain
            // bound, it can be assumed to be a number, otherwise it is assumed to be a
            // pointer. This allowed them to implement polymorphic functions, such as
            // the Print function, which can be called in two different ways, with a
            // pointer or a far text reference:
            //
            // (Print "foo") // Pointer to a string
            // (Print 420 5) // Reference to the fifth message in text resource 420
            // It works because in those games, the maximum resource number is 999,
            // so any parameter value above that threshold must be a pointer.
            // PQ2 japanese compares pointers to 2000 to find out if its a pointer
            // or a resource ID. Thus, we check for all integers <= 2000.
            //
            // Some examples where game scripts check for arbitrary numbers against
            // pointers:
            // Hoyle 3, Pachisi, when any opponent is about to talk
            // SQ1, room 28, when throwing water at the Orat
            // SQ1, room 58, when giving the ID card to the robot
            // SQ4 CD, at the first game screen, when the narrator is about to speak
            return (IsPointer && right.IsNumber && right.Offset <= 2000 &&
                    ResourceManager.GetSciVersion() <= SciVersion.V1_1);
        }
    }

    internal static class RegisterExtension
    {
        public static Register ToRegister(this BytePtr ptr, int offset = 0)
        {
            return new Register(new BytePtr(ptr.Data, ptr.Offset + offset));
        }

        public static Register ToRegister(this byte[] data, int offset = 0)
        {
            return new Register(new BytePtr(data, offset));
        }

        public static void WriteRegister(this byte[] data, int offset, Register register)
        {
            register.CopyTo(data, offset);
        }

        public static void WriteRegister(this BytePtr ptr, int offset, Register register)
        {
            register.CopyTo(ptr.Data, ptr.Offset + offset);
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