//
//  ResourceFile2.cs
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
using System.Linq;
using NScumm.Core.Graphics;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile2: ResourceFile
    {
        const int HeaderSize = 4;

        public ResourceFile2(Stream stream)
            : base(stream)
        {
        }

        public override Room ReadRoom(long offset)
        {
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            var header = ReadRMHD();

            // 10
            var imgOffset = _reader.ReadUInt16();
            // 12
            _reader.BaseStream.Seek(8, SeekOrigin.Current);
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

            var room = new Room
            {
                Header = header,
                Size = size
            };

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

            // read room image
            var imgLength = GetImageSize(offset + imgOffset, room.Header.Width, room.Header.Height);
            room.Image = new ImageData { Data = ReadBytes(offset + imgOffset, (int)imgLength) };

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

                for (var i = 0; i < numObjects - 1; i++)
                {
                    var objImgOffset = objImgOffsets[i];
                    if (firstOffset != objImgOffset)
                    {
                        var objImgSize = GetImageSize(offset + objImgOffset, room.Objects[i].Width, room.Objects[i].Height);
                        if (objImgSize > 0)
                        {
                            room.Objects[i].Images.Add(new ImageData{ Data = ReadBytes(offset + objImgOffset, (int)objImgSize) });
                        }
                    }
                }
            }

            return room;
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

        void ReadBoxes(long offset, Room room)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var numBoxes = _reader.ReadByte();
            for (int i = 0; i < numBoxes; i++)
            {
                var uy = _reader.ReadByte();
                var ly = _reader.ReadByte();
                var ulx = _reader.ReadByte();
                var urx = _reader.ReadByte();
                var llx = _reader.ReadByte();
                var lrx = _reader.ReadByte();
                var mask = _reader.ReadByte();
                var flags = _reader.ReadByte();
                room.Boxes.Add(new Box
                    {
                        Ulx = ulx,
                        Uly = uy,
                        Urx = urx,
                        Ury = uy,
                        Llx = llx,
                        Lly = ly,
                        Lrx = lrx,
                        Lry = ly,
                        Mask = mask,
                        Flags = (BoxFlags)flags
                    });
            }
            var size = numBoxes * (numBoxes + 1);
            room.BoxMatrix.AddRange(_reader.ReadBytes(size));
        }

        public override byte[] ReadCostume(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            return _reader.ReadBytes(size - HeaderSize);
        }

        public override byte[] ReadScript(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Current);
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
                Width = _reader.ReadUInt16(),
                Height = _reader.ReadUInt16(),
                NumObjects = _reader.ReadUInt16()
            };
            return header;
        }

        ObjectData ReadObject(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadUInt16();
            var id = _reader.ReadUInt16();
            var @class = _reader.ReadByte();
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
            obj.Script.Offset = 11 + 2 * obj.ScriptOffsets.Count + 1 + obj.Name.Length + 1 + HeaderSize;
            obj.Script.Data = _reader.ReadBytes((int)(size - obj.Script.Offset));
            return obj;
        }

        void ReadObjectScriptOffsets(ObjectData obj)
        {
            byte entry;
            while ((entry = _reader.ReadByte()) != 0)
            {
                obj.ScriptOffsets.Add(entry, _reader.ReadByte());
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

        byte[] ReadBytes(long offset, int length)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _reader.ReadBytes(length);
        }
    }
}
