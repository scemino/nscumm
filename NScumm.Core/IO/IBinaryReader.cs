using System;
using System.IO;

namespace NScumm.Core.IO
{
    public interface IBinaryReader : IDisposable
    {
        Stream BaseStream { get; }

        byte ReadByte();
        sbyte ReadSByte();

        uint ReadUInt32();
        uint ReadUInt32BigEndian();
        int ReadInt32();
        int ReadInt32BigEndian();
        ushort ReadUInt16();
        ushort ReadUInt16BigEndian();
        short ReadInt16();
        short ReadInt16BigEndian();
    }

    public class EndianBinaryReader : IBinaryReader
    {
        private readonly IBinaryReader _br;

        public Stream BaseStream => _br.BaseStream;

        public EndianBinaryReader(bool bigEndian, Stream stream)
        {
            _br = bigEndian
                ? (IBinaryReader) new BigEndianBinaryReaderImpl(stream)
                : new BinaryReaderImpl(stream);
        }

        public void Dispose()
        {
            _br.Dispose();
        }

        public byte ReadByte()
        {
            return _br.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return _br.ReadSByte();
        }

        public uint ReadUInt32()
        {
            return _br.ReadUInt32();
        }

        public uint ReadUInt32BigEndian()
        {
            return _br.ReadUInt32BigEndian();
        }

        public int ReadInt32()
        {
            return _br.ReadInt32();
        }

        public int ReadInt32BigEndian()
        {
            return _br.ReadInt32BigEndian();
        }

        public ushort ReadUInt16()
        {
            return _br.ReadUInt16();
        }

        public ushort ReadUInt16BigEndian()
        {
            return _br.ReadUInt16BigEndian();
        }

        public short ReadInt16()
        {
            return _br.ReadInt16();
        }

        public short ReadInt16BigEndian()
        {
            return _br.ReadInt16BigEndian();
        }
    }

    public class BinaryReaderImpl : IBinaryReader
    {
        private readonly BinaryReader _br;

        public Stream BaseStream => _br.BaseStream;

        public BinaryReaderImpl(Stream stream)
        {
            _br = new BinaryReader(stream);
        }

        public void Dispose()
        {
            _br.Dispose();
        }

        public byte ReadByte()
        {
            return _br.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return _br.ReadSByte();
        }

        public uint ReadUInt32()
        {
            return _br.ReadUInt32();
        }

        public uint ReadUInt32BigEndian()
        {
            return _br.ReadUInt32BigEndian();
        }

        public int ReadInt32()
        {
            return _br.ReadInt32();
        }

        public int ReadInt32BigEndian()
        {
            return _br.ReadInt32BigEndian();
        }

        public ushort ReadUInt16()
        {
            return _br.ReadUInt16();
        }

        public ushort ReadUInt16BigEndian()
        {
            return _br.ReadUInt16BigEndian();
        }

        public short ReadInt16()
        {
            return _br.ReadInt16();
        }

        public short ReadInt16BigEndian()
        {
            return _br.ReadInt16BigEndian();
        }
    }

    public class BigEndianBinaryReaderImpl : IBinaryReader
    {
        private readonly BinaryReader _br;

        public Stream BaseStream => _br.BaseStream;

        public BigEndianBinaryReaderImpl(Stream stream)
        {
            _br = new BinaryReader(stream);
        }

        public void Dispose()
        {
            _br.Dispose();
        }

        public byte ReadByte()
        {
            return _br.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return _br.ReadSByte();
        }

        public uint ReadUInt32()
        {
            return _br.ReadUInt32BigEndian();
        }

        public uint ReadUInt32BigEndian()
        {
            return _br.ReadUInt32();
        }

        public int ReadInt32()
        {
            return _br.ReadInt32BigEndian();
        }

        public int ReadInt32BigEndian()
        {
            return _br.ReadInt32();
        }

        public ushort ReadUInt16()
        {
            return _br.ReadUInt16BigEndian();
        }

        public ushort ReadUInt16BigEndian()
        {
            return _br.ReadUInt16();
        }

        public short ReadInt16()
        {
            return _br.ReadInt16BigEndian();
        }

        public short ReadInt16BigEndian()
        {
            return _br.ReadInt16();
        }
    }
}