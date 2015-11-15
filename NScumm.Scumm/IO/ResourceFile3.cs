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
using System.IO;
using System;
using System.Text;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile3: ResourceFile
    {
        public ResourceFile3(Stream stream)
            : base(stream)
        {
        }

        protected virtual Box ReadBox()
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

        static Chunk ReadChunk(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var tag = Encoding.UTF8.GetString(reader.ReadBytes(2));
            return new Chunk { Offset = reader.BaseStream.Position, Size = size, Tag = tag };
        }

        protected virtual void GotoResourceHeader(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public override byte[] ReadCostume(long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ReadChunk(_reader);
            if (chunk.Tag != "CO")
                throw new NotSupportedException("Expected costume block.");
            return _reader.ReadBytes((int)chunk.Size - 6);
        }

        public override byte[] ReadScript(long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ReadChunk(_reader);
            if (chunk.Tag != "SC")
                throw new NotSupportedException("Expected SC block.");
            var data = _reader.ReadBytes((int)(chunk.Size - 6));
            return data;
        }

        protected virtual bool ReadBlockSound()
        {
            return true;
        }

        public override byte[] ReadSound(MusicDriverTypes music, long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ReadChunk(_reader);
            if (chunk.Tag != "SO")
                throw new NotSupportedException("Expected sound block.");
            var totalSize = chunk.Size - 6;
            Dictionary<string,Chunk> offsets = new Dictionary<string, Chunk>();
            while (totalSize > 0)
            {
                chunk = ReadChunk(_reader);
                if (chunk.Tag == "SO")
                {
                    totalSize -= 6;
                    if (ReadBlockSound())
                    {
                        continue;
                    }
                }
                else if (chunk.Tag == "WA")
                {
                    offsets[chunk.Tag] = chunk;
                }
                else if (chunk.Tag == "AD")
                {
                    offsets[chunk.Tag] = chunk;
                }
                else if (chunk.Tag == "AD")
                {
                    offsets[chunk.Tag] = chunk;
                }
                totalSize -= chunk.Size;
                _reader.BaseStream.Seek(chunk.Size - 6, SeekOrigin.Current);

            }

            if (music == MusicDriverTypes.PCSpeaker || music == MusicDriverTypes.PCjr)
            {
                if (offsets.ContainsKey("WA"))
                {
                    _reader.BaseStream.Seek(offsets["WA"].Offset - 6, SeekOrigin.Begin);
                    return _reader.ReadBytes((int)offsets["WA"].Size + 6);
                }
            }
            else if (music == MusicDriverTypes.CMS)
            {
                bool hasAdLibMusicTrack = false;

                if (offsets.ContainsKey("AD"))
                {
                    _reader.BaseStream.Seek(offsets["AD"].Offset + 2, SeekOrigin.Begin);
                    hasAdLibMusicTrack = (_reader.PeekChar() == 0x80);
                }

                if (hasAdLibMusicTrack)
                {
                    _reader.BaseStream.Seek(offsets["AD"].Offset - 4, SeekOrigin.Begin);
                    return _reader.ReadBytes((int)offsets["AD"].Offset + 4);
                }
                else if (offsets.ContainsKey("WA"))
                {
                    _reader.BaseStream.Seek(offsets["WA"].Offset - 6, SeekOrigin.Begin);
                    return _reader.ReadBytes((int)offsets["WA"].Offset + 6);
                }
            }
            else if (music == MusicDriverTypes.AdLib)
            {
                if (offsets.ContainsKey("AD"))
                {
                    _reader.BaseStream.Seek(offsets["AD"].Offset, SeekOrigin.Begin);
                    return _reader.ReadBytes((int)offsets["AD"].Size - 6);
                }
            }

            return null;
        }

        public virtual byte[] ReadAmigaSound(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadInt32();
            _reader.BaseStream.Seek(-4, SeekOrigin.Current);
            return _reader.ReadBytes(size);
        }

        public override Room ReadRoom(long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ReadChunk(_reader);

            if (chunk.Tag != "RO")
            {
                throw new NotSupportedException("Invalid room.");
            }

            var room = ReadRoomCore(offset, chunk.Size);
            return room;
        }

        Room ReadRoomCore(long offset, long chunkSize)
        {
            var objImages = new Dictionary<ushort, byte[]>();
            var room = new Room { Size = (int)chunkSize };
            var it = new ChunkIterator(_reader, chunkSize);

            while (it.MoveNext())
            {
                switch (it.Current.Tag)
                {
                    case "HD":
                        // Room Header
                        room.Header = ReadRMHD();
                        break;
                    case "CC":
                        // Color Cylcle
                        room.ColorCycle = ReadCYCL();
                        break;
                    case "SP":
                        // EPAL
                        ReadEPAL();
                        break;
                    case "BX":
                        // BOXD
                        {
                            var size = (int)(it.Current.Size - 6);
                            var numBoxes = _reader.ReadByte();
                            var pos = _reader.BaseStream.Position;
                            for (var i = 0; i < numBoxes; i++)
                            {
                                var box = ReadBox();
                                room.Boxes.Add(box);
                            }
                            size -= (int)(_reader.BaseStream.Position - pos);

                            if (size > 0)
                            {
                                room.BoxMatrix.Clear();
                                room.BoxMatrix.AddRange(_reader.ReadBytes(size));
                            }
                        }
                        break;
                    case "PA":
                        {
                            // Palette
                            var colors = ReadCLUT();
                            room.HasPalette = true;
                            Array.Copy(colors, room.Palette.Colors, colors.Length);
                        }
                        break;
                    case "SA":
                        // Scale
                        if (it.Current.Size > 6)
                        {
                            room.Scales = ReadSCAL();
                        }
                        break;
                    case "BM":
                        // Bitmap
                        if (it.Current.Size > 6)
                        {
                            using (var ms = new MemoryStream(_reader.ReadBytes((int)(it.Current.Size - 6))))
                            {
                                room.Image = ReadImage(ms, room.Header.Width / 8);
                            }
                        }
                        break;
                    case "EN":
                        {
                            // Entry script
                            room.EntryScript.Data = _reader.ReadBytes((int)(it.Current.Size - 6));
                        }
                        break;
                    case "EX":
                        {
                            // Exit script
                            room.ExitScript.Data = _reader.ReadBytes((int)(it.Current.Size - 6));
                        }
                        break;
                    case "LC": //LC
                        {
                            // *NLSC* number of local scripts
                            var num = _reader.ReadUInt16();
                        }
                        break;
                    case "LS":
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
                    case "OI":
                        {
                            // Object Image
                            var objId = _reader.ReadUInt16();
                            if (it.Current.Size > 8)
                            {
                                var img = _reader.ReadBytes((int)(it.Current.Size - 6));
                                objImages.Add(objId, img);
                            }
                        }
                        break;
                    case "OC":
                        {
                            // Object script
                            var objId = _reader.ReadUInt16();
                            var t = _reader.ReadByte();
                            System.Diagnostics.Debug.WriteLine("objId={0}: {1}", objId, t);
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

                            var data = new ObjectData(objId);
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

                            SetObjectImage(room.Image.ZPlanes.Count, objImages, data);
                        }
                        break;

                    default:
                        {
                            var data = _reader.ReadBytes((int)it.Current.Size - 6);
                            System.Diagnostics.Debug.WriteLine("Ignoring Resource Tag: {0} (0x{1:X2}{2:X2}) [{3}]", 
                                it.Current.Tag, (int)it.Current.Tag[0], (int)it.Current.Tag[1], 
                                string.Join(",", data.Select(b => b.ToString("X2"))));
                        }
                        break;
                }
            }

            return room;
        }

        ImageData ReadImage(Stream stream, int numStrips)
        {
            var br = new BinaryReader(stream);
            var img = new ImageData();
            var size = br.ReadInt32();
            br.BaseStream.Seek(-4, SeekOrigin.Current);
            if (size != 0)
            {
                img.Data = br.ReadBytes(size + 2);
                br.BaseStream.Seek(-2, SeekOrigin.Current);
                size = br.ReadUInt16();
                if (br.BaseStream.Position == br.BaseStream.Length)
                {
                    size = 0;
                }
            }
            while (size != 0 && img.ZPlanes.Count < 3)
            {
                var zPlane = ReadZPlane(br, size, numStrips);
                img.ZPlanes.Add(zPlane);
                size = 0;
                br.BaseStream.Seek(-2, SeekOrigin.Current);
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
            var offsets = new List<int?>();
            using (var ms = new MemoryStream(zPlaneData))
            {
                var br = new BinaryReader(ms);
                var tableSize = 4 + numStrips * 2;
                ms.Seek(2, SeekOrigin.Current);
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

        protected virtual void ReadVerbTable(ObjectData data, int size)
        {
            var tableLength = (size - 1) / 3;
            for (var i = 0; i < tableLength; i++)
            {
                var id = _reader.ReadByte();
                var offset = _reader.ReadUInt16();
                data.ScriptOffsets.Add(id, offset);
            }
            _reader.ReadByte();
        }

        void SetObjectImage(int numZBuffer, IDictionary<ushort, byte[]> stripsDic, ObjectData obj)
        {
            if (stripsDic.ContainsKey(obj.Number))
            {
                using (var ms = new MemoryStream(stripsDic[obj.Number]))
                {
                    //                    Console.Write("obj {0}: ", obj.DebuggerDisplay);
                    obj.Images.Add(ReadImage(ms, obj.Width / 8));
                }
                //                obj.Images.Add(new ImageData{ Data = stripsDic[obj.Number] });

            }
        }

        protected virtual RoomHeader ReadRMHD()
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

        protected virtual ScaleSlot[] ReadSCAL()
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
    }
}
