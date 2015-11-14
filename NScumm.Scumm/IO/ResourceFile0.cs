//
//  ResourceFile0.cs
//
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

using System.Collections.Generic;
using System.IO;
using NScumm.Core.Graphics;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile0 : ResourceFile
    {
        const int HeaderSize = 4;

        public ResourceFile0(Stream stream)
            : base(stream)
        {
        }

        public override Room ReadRoom(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            var header = ReadRMHD();

            var room = new Room { Header = header, Size = size };

            // 6
            var img = new ImageData1();
            room.Image = img;
            img.Colors = _reader.ReadBytes(4);
            var charmapOffset = _reader.ReadUInt16();
            var picMapOffset = _reader.ReadUInt16();
            var colorMapOffset = _reader.ReadUInt16();
            var maskMapOffset = _reader.ReadUInt16();
            var maskOffset = _reader.ReadUInt16();

            // 20
            var numObjects = _reader.ReadByte();
            // 21
            var boxesOffset = _reader.ReadByte();
            // 22
            var numSounds = _reader.ReadByte();
            // 23
            var numScripts = _reader.ReadByte();
            // 24
            var exitScriptOffset = _reader.ReadUInt16();
            // 26
            var entryScriptOffset = _reader.ReadUInt16();
            var exitScriptLength = entryScriptOffset - entryScriptOffset;
            var entryScriptLength = size - entryScriptOffset;
            // 28
            var objImgOffsets = _reader.ReadUInt16s(numObjects);
            var objScriptOffsets = _reader.ReadUInt16s(numObjects);
            // load sounds
            var soundIds = _reader.ReadBytes(numSounds);
            // load scripts
            var scriptIds = _reader.ReadBytes(numScripts);

            var width = header.Width / 8;
            var height = header.Height / 8;
            DecodeV1Gfx(offset + charmapOffset, img.CharMap, 2048);
            DecodeV1Gfx(offset + picMapOffset, img.PicMap, width * height);
            DecodeV1Gfx(offset + colorMapOffset, img.ColorMap, width * height);
            DecodeV1Gfx(offset + maskMapOffset, img.MaskMap, width * height);

            // Read the mask data. The 16bit length value seems to always be 8 too big.
            // See bug #1837375 for details on this.
            _reader.BaseStream.Seek(offset + maskOffset, SeekOrigin.Begin);
            DecodeV1Gfx(offset + maskOffset + 2, img.MaskChar, _reader.ReadUInt16() - 8);

            // read exit script
            if (exitScriptOffset > 0)
            {
                room.ExitScript.Data = ReadBytes(offset + exitScriptOffset, exitScriptLength);
            }
            else
            {
                room.ExitScript.Data = new byte[0];
            }

            // read entry script
            if (entryScriptOffset > 0)
            {
                room.EntryScript.Data = ReadBytes(offset + entryScriptOffset, entryScriptLength);
            }
            else
            {
                room.EntryScript.Data = new byte[0];
            }

            // read boxes
            ReadBoxes(offset + boxesOffset, room);

            // read objects
            if (numObjects > 0)
            {
                var firstOffset = objScriptOffsets.Min();
                for (var i = 0; i < numObjects; i++)
                {
                    var objOffset = objScriptOffsets[i];
                    var obj = ReadObject(offset + objOffset);
                    room.Objects.Add(obj);
                }

                for (var i = 0; i < numObjects; i++)
                {
                    var objImgOffset = objImgOffsets[i];
                    if (firstOffset != objImgOffset)
                    {
                        img = new ImageData1();
                        var obj = room.Objects[i];
                        DecodeV1Gfx(offset + objImgOffset, img.ObjectMap, (obj.Width / 8) * (obj.Height / 8) * 3);
                        obj.Images.Add(img);
                    }
                }
            }

            return room;
        }

        public override byte[] ReadCostume(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _reader.ReadBytes(size);
        }

        public override byte[] ReadScript(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            var data = _reader.ReadBytes(size - HeaderSize);
            return data;
        }

        public override byte[] ReadSound(MusicDriverTypes music, long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16(); // wa_size
            _reader.BaseStream.Seek(-2, SeekOrigin.Current);
            var data = _reader.ReadBytes(size);
            return data;
        }

        protected RoomHeader ReadRMHD()
        {
            var header = new RoomHeader
            {
                Width = _reader.ReadByte() * 8,
                Height = _reader.ReadByte() * 8,
            };
            return header;
        }

        byte[] ReadBytes(long offset, int length)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _reader.ReadBytes(length);
        }

        void DecodeV1Gfx(long offset, byte[] dst, int size)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            int x, z;
            byte color, run;
            byte[] common = new byte[4];

            for (z = 0; z < 4; z++)
            {
                common[z] = _reader.ReadByte();
            }

            x = 0;
            while (x < size)
            {
                run = _reader.ReadByte();
                if ((run & 0x80) != 0)
                {
                    color = common[(run >> 5) & 3];
                    run &= 0x1F;
                    for (z = 0; z <= run; z++)
                    {
                        dst[x++] = color;
                    }
                }
                else if ((run & 0x40) != 0)
                {
                    run &= 0x3F;
                    color = _reader.ReadByte();
                    for (z = 0; z <= run; z++)
                    {
                        dst[x++] = color;
                    }
                }
                else
                {
                    for (z = 0; z <= run; z++)
                    {
                        dst[x++] = _reader.ReadByte();
                    }
                }
            }
        }

        long GetImageSize(long offset, int width, int height)
        {
            height &= 0xFFF8;
            byte data;
            int y;
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            int run = 1;
            for (var x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    if (--run == 0)
                    {
                        data = _reader.ReadByte();
                        if ((data & 0x80) != 0)
                        {
                            run = data & 0x7f;
                        }
                        else
                        {
                            run = data >> 4;
                        }
                        if (run == 0)
                        {
                            run = _reader.ReadByte();
                        }
                    }
                }
            }
            y = 0;
            run = _reader.ReadByte();
            for (var x = 0; x < width;)
            {
                byte runFlag = (byte)(run & 0x80);
                if (runFlag != 0)
                {
                    run &= 0x7f;
                    data = _reader.ReadByte();
                }
                do
                {
                    if (runFlag == 0)
                        data = _reader.ReadByte();
                    y++;
                    if (y >= height)
                    {
                        y = 0;
                        x += 8;
                        if (x >= width)
                            break;
                    }
                } while ((--run) != 0);
                run = _reader.ReadByte();
            }
            return _reader.BaseStream.Position - offset;
        }

        ObjectData ReadObject(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadUInt16();
            var id = _reader.ReadUInt16();
            var x = _reader.ReadByte() * 8;
            var tmpY = _reader.ReadByte();
            var y = (tmpY & 0x7F) * 8;
            var parentState = ((tmpY & 0x80) == 0x80) ? 8 : 0;
            var width = _reader.ReadByte() * 8;
            var parent = _reader.ReadByte();
            var walkX = _reader.ReadByte() * 8;
            tmpY = _reader.ReadByte();
            var walkY = (tmpY & 0x1F) * 8;
            var preposition = tmpY >> 5;
            var tmpActor = _reader.ReadByte();
            var actor = tmpActor & 0x3;
            var height = tmpActor & 0xF8;
            var obj = new ObjectData(id)
            {
                Position = new Point(x, y),
                ParentState = (byte)parentState,
                Width = (ushort)width,
                Height = (ushort)height,
                Parent = parent,
                Walk = new Point(walkX, walkY),
                ActorDir = actor,
                Preposition = preposition
            };
            var nameOffset = _reader.ReadByte();
            ReadObjectScriptOffsets(obj);
            ReadName(obj);
            obj.Script.Offset = 10 + 2 * obj.ScriptOffsets.Count + 1 + obj.Name.Length + 1 + HeaderSize;
            obj.Script.Data = _reader.ReadBytes((int)(size - obj.Script.Offset));

            return obj;
        }

        void ReadObjectScriptOffsets(ObjectData obj)
        {
            byte entry;
            while ((entry = _reader.ReadByte()) != 0)
            {
                obj.ScriptOffsets.Add(entry == 0xF ? 0xFF : entry, _reader.ReadByte());
            }
        }

        void ReadName(ObjectData obj)
        {
            byte entry;
            var name = new List<byte>();
            while ((entry = _reader.ReadByte()) != 0)
            {
                name.Add(entry);
            }
            obj.Name = name.ToArray();
        }

        void ReadBoxes(long offset, Room room)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte x1;
            while ((x1 = _reader.ReadByte()) != 0xFF)
            {
                byte x2 = _reader.ReadByte();
                byte y1 = _reader.ReadByte();
                byte y2 = _reader.ReadByte();
                byte mask = _reader.ReadByte();
                var box = new Box
                {
                    Ulx = x1,
                    Uly = y1,
                    Urx = x2,
                    Ury = y1,
                    Llx = x1,
                    Lly = y2,
                    Lrx = x2,
                    Lry = y2,
                    Mask = mask,
                };

                if ((mask & 0x88) == 0x88)
                {
                    // walkbox for (right/left) corner
                    if ((mask & 0x04) != 0)
                    {
                        box.Urx = box.Ulx;
                        box.Ury = box.Uly;
                    }
                    else
                    {
                        box.Ulx = box.Urx;
                        box.Uly = box.Ury;
                    }
                }
                room.Boxes.Add(box);
            }

            var size = 0;
            var pos = _reader.BaseStream.Position;
            // Compute matrix size
            for (var i = 0; i < room.Boxes.Count; i++)
            {
                while (_reader.ReadByte() != 0xFF)
                {
                    size++;
                }
                size++;
            }

            _reader.BaseStream.Position = pos;
            room.BoxMatrix.AddRange(_reader.ReadBytes(size));
        }
    }
}

