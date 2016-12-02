using NScumm.Core;

namespace NScumm.Agos
{
    internal class AnimationHeaderFeeble
    {
        public const int Size = 6;

        public ushort scriptOffs => Pointer.ToUInt16();

        public ushort x_2 => Pointer.ToUInt16(2);

        public ushort id => Pointer.ToUInt16(4);

        public BytePtr Pointer;

        public AnimationHeaderFeeble(BytePtr pointer)
        {
            Pointer = pointer;
        }
    }
}