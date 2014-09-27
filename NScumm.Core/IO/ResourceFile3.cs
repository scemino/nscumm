//
//  ResourceFile3.cs
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
using NScumm.Core.Graphics;
using NScumm.Core;
using System.IO;
using System;

namespace NScumm.Core.IO
{
    public class ResourceFile3: ResourceFile
    {
        public ResourceFile3(string path, byte encByte)
            : base(path, encByte)
        {
        }

        public override Dictionary<byte, long> ReadRoomOffsets()
        {
            return new Dictionary<byte, long>();
        }

        protected virtual Box ReadBox(ref int size)
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
            size -= 18;
            return box;
        }

        protected virtual Color[] ReadCLUT()
        {
            var numColors = _reader.ReadUInt16();
            var colors = new Color[numColors];
            for (var i = 0; i < numColors; i++)
            {
                colors[i] = Color.FromRgb(_reader.ReadByte(), _reader.ReadByte(), _reader.ReadByte());
            }
            return colors;
        }

        #region Chunk Class

        sealed class Chunk
        {
            public long Size { get; set; }

            public ushort Tag { get; set; }

            public long Offset { get; set; }
        }

        #endregion

        #region ChunkIterator Class

        sealed class ChunkIterator : IEnumerator<Chunk>
        {
            readonly XorReader _reader;
            readonly long _position;
            readonly long _size;

            public ChunkIterator(XorReader reader, long size)
            {
                _reader = reader;
                _position = reader.BaseStream.Position;
                _size = size;
            }

            public Chunk Current
            {
                get;
                private set;
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (Current != null)
                {
                    var offset = Current.Offset + Current.Size - 6;
                    _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                }
                Current = null;
                if (_reader.BaseStream.Position < (_position + _size - 6) && _reader.BaseStream.Position < _reader.BaseStream.Length)
                {
                    var size = _reader.ReadUInt32();
                    var tag = _reader.ReadUInt16();
                    Current = new Chunk { Offset = _reader.BaseStream.Position, Size = size, Tag = tag };
                }
                return Current != null;
            }

            public void Reset()
            {
                _reader.BaseStream.Seek(_position, SeekOrigin.Begin);
                Current = null;
            }
        }

        #endregion

        protected virtual void GotoResourceHeader(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public override XorReader ReadCostume(long offset)
        {
            GotoResourceHeader(offset + 4);
            var tag = _reader.ReadInt16();
            if (tag != 0x4F43)
                throw new NotSupportedException("Invalid costume.");
            return _reader;
        }

        public override byte[] ReadScript(long offset)
        {
            GotoResourceHeader(offset);
            long size = _reader.ReadUInt32();
            var tag = _reader.ReadInt16();
            if (tag != 0x4353)
                throw new NotSupportedException("Expected SC block.");
            var data = _reader.ReadBytes((int)(size - 6));
            return data;
        }

        public override byte[] ReadSound(long offset)
        {
            GotoResourceHeader(offset);
            long size = _reader.ReadUInt32();
            var tag = _reader.ReadInt16();
            if (tag != 0x4F53)
                throw new NotSupportedException("Expected SO block.");
            var totalSize = size - 6;
            while (totalSize > 0)
            {
                size = _reader.ReadUInt32();
                tag = _reader.ReadInt16();
                if (tag == 0x4F53)
                {
                    totalSize -= 6;
                }
                else if (tag == 0x4441)
                {
                    _reader.BaseStream.Seek(-6, SeekOrigin.Current);
                    return _reader.ReadBytes((int)size);
                }
                else
                {
                    totalSize -= size;
                    _reader.BaseStream.Seek(size - 6, SeekOrigin.Current);
                }

            }
            return null;
        }

        public override Room ReadRoom(long offset)
        {
            var objImages = new Dictionary<ushort, ImageData>();
            var its = new Stack<ChunkIterator>();
            var room = new Room();
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var it = new ChunkIterator(_reader, _reader.BaseStream.Length - _reader.BaseStream.Position);
            do
            {
                while (it.MoveNext())
                {
                    switch (it.Current.Tag)
                    {
                        case 0x464C:
                            // *LFLF* disk block
                            // room number
                            _reader.ReadUInt16();
                            //its.Push(it);
                            it = new ChunkIterator(_reader, it.Current.Size - 2);
                            break;
                        case 0x4F52:
                            // ROOM
                            its.Push(it);
                            it = new ChunkIterator(_reader, it.Current.Size);
                            break;
                        case 0x4448:
                            // ROOM Header
                            room.Header = ReadRMHD();
                            break;
                        case 0x4343:
                            // CYCL
                            room.ColorCycle = ReadCYCL();
                            break;
                        case 0x5053:
                            // EPAL
                            ReadEPAL();
                            break;
                        case 0x5842:
                            // BOXD
                            {
                                int size = (int)(it.Current.Size - 6);
                                var numBoxes = _reader.ReadByte();
                                for (int i = 0; i < numBoxes; i++)
                                {
                                    var box = ReadBox(ref size);
                                    room.Boxes.Add(box);
                                }

                                if (size > 0)
                                {
                                    room.BoxMatrix.Clear();
                                    room.BoxMatrix.AddRange(_reader.ReadBytes(size));
                                }
                            }
                            break;
                        case 0x4150:
                            {
                                // CLUT
                                var colors = ReadCLUT();
                                room.HasPalette = true;
                                Array.Copy(colors, room.Palette.Colors, colors.Length);
                            }
                            break;
                        case 0x4153:
                            // SCAL
                            if (it.Current.Size > 6)
                            {
                                room.Scales = ReadSCAL();
                            }
                            break;
                        case 0x4D42:
                            // BM (IM00)
                            if (it.Current.Size > 8)
                            {
                                using (var ms = new MemoryStream(_reader.ReadBytes((int)(it.Current.Size - 6))))
                                {
                                    room.Image = ReadImage(ms, room.Header.Width / 8);
                                }
                            }
                            break;
                        case 0x4E45:
                            {
                                // Entry script
                                room.EntryScript.Data = _reader.ReadBytes((int)(it.Current.Size - 6));
                            }
                            break;
                        case 0x5845:
                            {
                                // Exit script
                                room.ExitScript.Data = _reader.ReadBytes((int)(it.Current.Size - 6));
                            }
                            break;
                        case 0x4C53:
                            {
                                // *SL* 
                                _reader.ReadByte();
                            }
                            break;
                        case 0x434C: //LC
                            {
                                // *NLSC* number of local scripts
                                _reader.ReadUInt16();
                            }
                            break;
                        case 0x534C:
                            {
                                // local scripts
                                var index = _reader.ReadByte();
                                var pos = _reader.BaseStream.Position;
                                room.LocalScripts[index - 0xC8] = new ScriptData
                                {
                                    Offset = pos - offset - 8,
                                    Data = _reader.ReadBytes((int)(it.Current.Size - 7))
                                };
                            }
                            break;
                        case 0x494F:
                            {
                                // Object Image
                                var objId = _reader.ReadUInt16();
                                if (it.Current.Size > 8)
                                {
                                    var img = new ImageData{ Data = _reader.ReadBytes((int)(it.Current.Size - 6)) };
                                    objImages.Add(objId, img);
                                }
                            }
                            break;
                        case 0x434F:
                            {
                                // Object script
                                var objId = _reader.ReadUInt16();
                                _reader.ReadByte();
                                var x = _reader.ReadByte();
                                var tmp = _reader.ReadByte();
                                var y = tmp & 0x7F;
                                byte parentState = (byte)(((tmp & 0x80) != 0) ? 1 : 0);
                                var width = _reader.ReadByte();
                                var parent = _reader.ReadByte();
                                var walk_x = _reader.ReadInt16();
                                var walk_y = _reader.ReadInt16();
                                tmp = _reader.ReadByte();
                                byte height = (byte)(tmp & 0xF8);
                                byte actordir = (byte)(tmp & 0x07);

                                var data = new ObjectData();
                                data.Number = objId;
                                data.Position = new Point((short)(8 * x), (short)(8 * y));
                                data.Width = (ushort)(8 * width);
                                data.Height = height;
                                data.Parent = parent;
                                data.ParentState = parentState;
                                data.Walk = new Point(walk_x, walk_y);
                                data.ActorDir = actordir;
                                room.Objects.Add(data);

                                var nameOffset = _reader.ReadByte();
                                var size = nameOffset - 6 - 13;
                                ReadVerbTable(data, size);
                                data.Name = ReadObjectName(it, nameOffset);
                                // read script
                                size = (int)(it.Current.Offset + it.Current.Size - 6 - _reader.BaseStream.Position);
                                data.Script.Data = _reader.ReadBytes(size);
                                data.Script.Offset = nameOffset + data.Name.Length + 1;

                                SetObjectImage(objImages, data);
                            }
                            break;
                    //case 0x4F53:
                    //    {
                    //        // SO
                    //        its.Push(it);
                    //        it = new ChunkIterator(_reader, it.Current.Size);
                    //    }
                    //    break;
                        default:
                            System.Diagnostics.Debug.WriteLine("Ignoring Resource Tag: {0:X2} ({2}{3}), Size: {1:X4}",
                                it.Current.Tag, it.Current.Size, (char)(it.Current.Tag & 0x00FF), (char)(it.Current.Tag >> 8));
                            break;
                    }
                }
                it = its.Pop();
            } while (its.Count > 0);

            return room;
        }

        ImageData ReadImage(Stream stream, int numStrips)
        {
            var br = new BinaryReader(stream);
            var img = new ImageData();
            var size = br.ReadInt32();
            br.BaseStream.Seek(-4, SeekOrigin.Current);
            if (size > 0)
            {
                img.Data = br.ReadBytes(size + 2);
                br.BaseStream.Seek(-2, SeekOrigin.Current);
                size = br.ReadUInt16();
            }
            while (size != 0 && img.ZPlanes.Count < 3)
            {
                var zPlane = ReadZPlane(br, size, numStrips);
                img.ZPlanes.Add(zPlane);
                size = 0;
                if ((br.BaseStream.Position + 2) < br.BaseStream.Length)
                {
                    size = br.ReadUInt16();
                }
            }
            return img;
        }

        protected virtual ZPlane ReadZPlane(BinaryReader b, int size, int numStrips)
        {
            var zPlaneData = b.ReadBytes(size);
            byte[] strips = null;
            var offsets = new List<int>();
            using (var ms = new MemoryStream(zPlaneData))
            {
                var br = new BinaryReader(ms);
                var tableSize = 4 + numStrips * 2;
                br.BaseStream.Seek(2, SeekOrigin.Begin);
                // read table offsets
                for (int i = 0; i < numStrips; i++)
                {
                    var offset = br.ReadUInt16();
                    if (offset > 0)
                    {
                        offsets.Add(offset - tableSize);
                    }
                    else
                    {
                        offsets.Add(-1);
                    }
                }
                strips = br.ReadBytes(size - tableSize);
            }
            var zPlane = new ZPlane(0, strips, offsets);
            return zPlane;
        }

        #region Protected Methods

        byte[] ReadObjectName(IEnumerator<Chunk> it, byte nameOffset)
        {
            _reader.BaseStream.Seek(it.Current.Offset + nameOffset - 6, SeekOrigin.Begin);
            var name = new List<byte>();
            var c = _reader.ReadByte();
            while (c != 0)
            {
                name.Add(c);
                c = _reader.ReadByte();
            }
            return name.ToArray();
        }

        protected void ReadVerbTable(ObjectData data, int size)
        {
            var tableLength = (size - 1) / 3;
            for (int i = 0; i < tableLength; i++)
            {
                var id = _reader.ReadByte();
                var offset = _reader.ReadUInt16();
                data.ScriptOffsets.Add(id, offset);
            }
            _reader.ReadByte();
        }

        static void SetObjectImage(IDictionary<ushort, ImageData> stripsDic, ObjectData obj)
        {
            if (stripsDic.ContainsKey(obj.Number))
            {
                obj.Images.Add(stripsDic[obj.Number]);
            }
        }

        protected RoomHeader ReadRMHD()
        {
            var header = new RoomHeader
            {
                Width = _reader.ReadUInt16(),
                Height = _reader.ReadUInt16(),
                NumObjects = _reader.ReadUInt16()
            };
            return header;
        }

        protected virtual ColorCycle[] ReadCYCL()
        {
            var colorCycle = new ColorCycle[16];
            for (int i = 0; i < 16; i++)
            {
                var delay = ScummHelper.SwapBytes(_reader.ReadUInt16());
                var start = _reader.ReadByte();
                var end = _reader.ReadByte();

                colorCycle[i] = new ColorCycle();

                if (delay == 0 || delay == 0x0aaa || start >= end)
                    continue;

                colorCycle[i].Counter = 0;
                colorCycle[i].Delay = (ushort)(16384 / delay);
                colorCycle[i].Flags = 2;
                colorCycle[i].Start = start;
                colorCycle[i].End = end;
            }

            return colorCycle;
        }

        protected ScaleSlot[] ReadSCAL()
        {
            var scales = new ScaleSlot[4];
            for (int i = 0; i < 4; i++)
            {
                var scale1 = _reader.ReadUInt16();
                var y1 = _reader.ReadUInt16();
                var scale2 = _reader.ReadUInt16();
                var y2 = _reader.ReadUInt16();
                scales[i] = new ScaleSlot { Scale1 = scale1, Y1 = y1, Y2 = y2, Scale2 = scale2 };
            }
            return scales;
        }

        protected byte[] ReadEPAL()
        {
            return _reader.ReadBytes(256);
        }

        #endregion
    }
	
}
