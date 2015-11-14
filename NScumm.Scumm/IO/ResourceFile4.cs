//
//  ResourceFile4.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile4: ResourceFile3
    {
        public ResourceFile4(Stream stream)
            : base(stream)
        {

        }

        public override long GetRoomOffset(byte roomNum)
        {
            var rOffsets = ReadRoomOffsets();
            return rOffsets.ContainsKey(roomNum) ? rOffsets[roomNum] : 0;
        }

        public override byte[] ReadAmigaSound(long offset)
        {
            GotoResourceHeader(offset);
            var size = _reader.ReadInt32();
            _reader.BaseStream.Seek(-4, SeekOrigin.Current);
            return _reader.ReadBytes(size);
        }

        protected virtual Dictionary<byte, long> ReadRoomOffsets()
        {
            var roomOffsets = new Dictionary<byte, long>();
            do
            {
                var size = _reader.ReadUInt32();
                var blockType = _reader.ReadUInt16();

                switch (blockType)
                {
                    // *LECF* main container
                    case 0x454C:
                        break;
                    // *LOFF* room offset table
                    case 0x4F46:
                        var numRooms = _reader.ReadByte();
                        while (numRooms-- != 0)
                        {
                            var room = _reader.ReadByte();
                            var offset = _reader.ReadUInt32();
                            roomOffsets[room] = offset;
                        }
                        return roomOffsets;
                    default:
                        // skip
                        //                        Console.WriteLine("Skip Block: 0x{0:X2}", blockType);
                        _reader.BaseStream.Seek(size - 6, SeekOrigin.Current);
                        break;
                }
            } while (_reader.BaseStream.Position < _reader.BaseStream.Length);
            return null;
        }

        protected override ZPlane ReadZPlane(BinaryReader b, int size, int numStrips)
        {
            var zPlaneData = b.ReadBytes(size);
            byte[] strips = null;
            var offsets = new List<int?>();
            using (var ms = new MemoryStream(zPlaneData))
            {
                var br = new BinaryReader(ms);
                var tableSize = 2 + numStrips * 2;
                // read table offsets
                for (int i = 0; i < numStrips; i++)
                {
                    var offset = br.ReadUInt16();
                    if (offset != 0)
                    {
                        offsets.Add(offset - tableSize);
                    }
                    else
                    {
                        offsets.Add(null);
                    }
                }
                strips = br.ReadBytes(size - tableSize);
            }
            var zPlane = new ZPlane(strips, offsets);
            return zPlane;
        }

        protected override void GotoResourceHeader(long offset)
        {
            _reader.BaseStream.Seek(offset + 8, SeekOrigin.Begin);
        }

        protected override Box ReadBox()
        {
            var box = new Box();
            box.Ulx = _reader.ReadInt16();
            box.Uly = _reader.ReadInt16();
            box.Urx = _reader.ReadInt16();
            box.Ury = _reader.ReadInt16();
            box.Lrx = _reader.ReadInt16();
            box.Lry = _reader.ReadInt16();
            box.Llx = _reader.ReadInt16();
            box.Lly = _reader.ReadInt16();
            box.Mask = _reader.ReadByte();
            box.Flags = (BoxFlags)_reader.ReadByte();
            box.Scale = _reader.ReadUInt16();
            return box;
        }

        protected override bool ReadBlockSound()
        {
            return false;
        }

        protected override Color[] ReadCLUT()
        {
            var numColors = _reader.ReadUInt16() / 3;
            var colors = new Color[numColors];
            for (var i = 0; i < numColors; i++)
            {
                colors[i] = Color.FromRgb(_reader.ReadByte(), _reader.ReadByte(), _reader.ReadByte());
            }
            return colors;
        }
    }
	
}
