using NScumm.Core;

namespace NScumm.Agos
{
    // Elvira 1/2 and Waxworks
    internal class ImageHeaderWw
    {
        public const int Size = 8;

        public ushort id => Pointer.ToUInt16();
        public ushort color => Pointer.ToUInt16(2);
        public ushort x_2 => Pointer.ToUInt16(4);
        public ushort scriptOffs => Pointer.ToUInt16(6);

        public BytePtr Pointer;

        public ImageHeaderWw(BytePtr pointer)
        {
            Pointer = pointer;
        }
    }
}