using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// A convenience class for creating and modifying in-memory
    /// bitmaps.
    /// </summary>
    internal class SciBitmap
    {
        private const int kBitmapRemap = 2;

        byte[] _data;
        int _dataSize;
        Buffer _buffer;

        public bool ShouldGc { get; private set; }

        public ushort Width
        {
            get { return _data.ReadSci11EndianUInt16(); }
            set { _data.WriteSci11EndianUInt16(0, value); }
        }

        public ushort Height
        {
            get { return _data.ReadSci11EndianUInt16(2); }
            set { _data.WriteSci11EndianUInt16(2, value); }
        }

        public Point Origin
        {
            get
            {
                return new Point((short)_data.ReadSci11EndianUInt16(4),
                    (short)_data.ReadSci11EndianUInt16(6));
            }
            set
            {
                _data.WriteSci11EndianUInt16(4, (ushort)value.X);
                _data.WriteSci11EndianUInt16(6, (ushort)value.Y);
            }
        }

        public byte SkipColor
        {
            get { return _data[8]; }
            set { _data[8] = value; }
        }

        public bool Remap
        {
            get { return (_data.ReadSci11EndianUInt16(10) & kBitmapRemap) != 0; }
            set
            {
                var flags = _data.ReadSci11EndianUInt16(10);
                if (value)
                {
                    flags |= kBitmapRemap;
                }
                else
                {
                    flags = (ushort)(flags & ~kBitmapRemap);
                }
                _data.WriteSci11EndianUInt16(10, flags);
            }
        }

        public BytePtr RawData => _data;

        public int DataSize
        {
            get { return (int)_data.ReadSci11EndianUInt32(12); }
            set { _data.WriteSci11EndianUInt32(12, (uint)value); }
        }

        public int RawSize => _dataSize;

        public int HunkPaletteOffset
        {
            get { return (int)_data.ReadSci11EndianUInt32(20); }
            set
            {
                if (value != 0)
                {
                    value += BitmapHeaderSize;
                }
                _data.WriteSci11EndianUInt32(20, (uint)value);
            }
        }

        public int DataOffset
        {
            get { return (int)_data.ReadSci11EndianUInt32(24); }
            set { _data.WriteSci11EndianUInt32(24, (uint)value); }
        }

        // NOTE: This property is used as a "magic number" for
        // validating that a block of memory is a valid bitmap,
        // and so is always set to the size of the header.
        public int UncompressedDataOffset
        {
            get { return (int)_data.ReadSci11EndianUInt32(28); }
            set { _data.WriteSci11EndianUInt32(28, (uint)value); }
        }

        // NOTE: This property always seems to be zero
        public int ControlOffset
        {
            get { return (int)_data.ReadSci11EndianUInt32(32); }
            set { _data.WriteSci11EndianUInt32(32, (uint)value); }
        }

        public ushort XResolution
        {
            get
            {
                if (DataOffset >= 40)
                {
                    return _data.ReadSci11EndianUInt16(36);
                }

                // SCI2 bitmaps did not have scaling ability
                return 320;
            }
            set
            {
                if (DataOffset >= 40)
                {
                    _data.WriteSci11EndianUInt16(36, value);
                }
            }
        }

        public ushort YResolution
        {
            get
            {
                if (DataOffset >= 40)
                {
                    return _data.ReadSci11EndianUInt16(38);
                }

                // SCI2 bitmaps did not have scaling ability
                return 200;
            }
            set
            {
                if (DataOffset >= 40)
                {
                    _data.WriteSci11EndianUInt16(38, value);
                }
            }
        }

        public BytePtr Pixels => new BytePtr(_data, UncompressedDataOffset);

        public BytePtr HunkPalette
        {
            get
            {
                if (HunkPaletteOffset == 0)
                {
                    return BytePtr.Null;
                }
                return new BytePtr(_data, HunkPaletteOffset);
            }
        }

        public Register Object { get; }

        public Buffer Buffer { get; }

        /// <summary>
        /// Gets the size of the bitmap header for the current
        /// engine version.
        /// </summary>
        public static ushort BitmapHeaderSize
        {
            get
            {
                // TODO: These values are accurate for each engine, but there may be no reason
                // to not simply just always use size 40, since SCI2.1mid does not seem to
                // actually store any data above byte 40, and SCI2 did not allow bitmaps with
                // scaling resolutions other than the default (320x200). Perhaps SCI3 used
                // the extra bytes, or there is some reason why they tried to align the header
                // size with other headers like pic headers?
                //		uint32 bitmapHeaderSize;
                //		if (getSciVersion() >= SCI_VERSION_2_1_MIDDLE) {
                //			bitmapHeaderSize = 46;
                //		} else if (getSciVersion() == SCI_VERSION_2_1_EARLY) {
                //			bitmapHeaderSize = 40;
                //		} else {
                //			bitmapHeaderSize = 36;
                //		}
                //		return bitmapHeaderSize;
                return 46;
            }
        }

        /// <summary>
        /// Gets the byte size of a bitmap with the given width
        /// and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int GetBitmapSize(ushort width, ushort height)
        {
            return width * height + BitmapHeaderSize;
        }

        public SciBitmap()
        {
            _data = new byte[0];
        }

        private SciBitmap(SciBitmap other)
        {
            _dataSize = other._dataSize;
            _data = new byte[other._dataSize];
            Array.Copy(other._data, _data, other._dataSize);
            if (_dataSize != 0)
            {
                _buffer = new Buffer(Width, Height, Pixels);
            }
            ShouldGc = other.ShouldGc;
        }

        /// <summary>
        /// Allocates and initialises a new bitmap.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="skipColor"></param>
        /// <param name="displaceX"></param>
        /// <param name="displaceY"></param>
        /// <param name="scaledWidth"></param>
        /// <param name="scaledHeight"></param>
        /// <param name="paletteSize"></param>
        /// <param name="remap"></param>
        /// <param name="gc"></param>
        public void Create(short width, short height, byte skipColor, short displaceX, short displaceY,
            short scaledWidth, short scaledHeight, int paletteSize, bool remap, bool gc)
        {
            _dataSize = GetBitmapSize((ushort)width, (ushort)height) + paletteSize;
            Array.Resize(ref _data, _dataSize);
            ShouldGc = gc;

            short bitmapHeaderSize = (short)BitmapHeaderSize;

            Width = (ushort)width;
            Height = (ushort)height;
            Origin = new Point(displaceX, displaceY);
            SkipColor = skipColor;
            _data[9] = 0;
            _data.WriteSci11EndianUInt16(10, 0);
            Remap = remap;
            DataSize = width * height;
            _data.WriteSci11EndianUInt32(16, 0);
            HunkPaletteOffset = paletteSize > 0 ? (width * height) : 0;
            DataOffset = bitmapHeaderSize;
            UncompressedDataOffset = bitmapHeaderSize;
            ControlOffset = 0;
            XResolution = (ushort)scaledWidth;
            YResolution = (ushort)scaledHeight;

            _buffer = new Buffer(Width, Height, Pixels);
        }
    }
}