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
using NScumm.Core;
using System;
using NScumm.Core.Graphics;

namespace NScumm.Core.IO
{
    public class ResourceFile2: ResourceFile
    {
        const int HeaderSize = 4;

        public ResourceFile2(string path, byte encByte)
            : base(path, encByte)
        {
        }

        public override Dictionary<byte, long> ReadRoomOffsets()
        {
            return new Dictionary<byte, long>();
        }

        public override Room ReadRoom(long offset)
        {
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            var header = ReadRMHD();

            // 10
            var imgOffset = _reader.ReadUInt16();
            // 12
            _reader.BaseStream.Seek(8, System.IO.SeekOrigin.Current);
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
            };

            // read exit script
            if (exitScriptOffset > 0)
            {
                room.ExitScript.Data = ReadBytes(offset + exitScriptOffset, exitScriptLength);
            }

            // read entry script
            if (entryScriptOffset > 0)
            {
                room.EntryScript.Data = ReadBytes(offset + entryScriptOffset, entryScriptLength);
            }

            // read room image
            int imgLength;
            if (exitScriptOffset > 0)
            {
                imgLength = exitScriptOffset;
            }
            else if (entryScriptOffset > 0)
            {
                imgLength = entryScriptOffset;
            }
            else
            {
                imgLength = size;
            }
            imgLength -= imgOffset;

            room.Image = new ImageData { Data = ReadBytes(offset + imgOffset, imgLength) };

            // read boxes
            ReadBoxes(offset + boxesOffset, room);

            var objImgSizes = new ushort[objImgOffsets.Length + 1];
            Array.Copy(objImgOffsets, objImgSizes, objScriptOffsets.Length);
            objImgSizes[objImgOffsets.Length] = objScriptOffsets[0];
            Array.Sort(objImgSizes);

            var objScriptSizes = new ushort[objScriptOffsets.Length + 1];
            Array.Copy(objScriptOffsets, objScriptSizes, objScriptOffsets.Length);
            objScriptSizes[objScriptOffsets.Length] = exitScriptOffset;
            Array.Sort(objScriptSizes);

            // read objects
            for (var i = 0; i < objScriptSizes.Length - 1; i++)
            {
                var objOffset = objScriptSizes[i];
                var objSize = objScriptSizes[i + 1] - objOffset - HeaderSize;
                var obj = ReadObject(offset + objOffset + HeaderSize, objSize);
                room.Objects.Add(obj);
            }

            // read obj images
            for (var i = 0; i < objImgSizes.Length - 1; i++)
            {
                var objImgOffset = objImgSizes[i];
                var objImgSize = objImgSizes[i + 1] - objImgOffset;
                room.Objects[i].Images.Add(new ImageData{ Data = ReadBytes(objImgOffset, objImgSize) });
            }

            return room;
        }

        void ReadBoxes(long offset, Room room)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
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
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            return _reader.ReadBytes(size - HeaderSize);
        }

        public override byte[] ReadScript(long offset)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            var data = _reader.ReadBytes(size - HeaderSize);
            return data;
        }

        public override byte[] ReadSound(NScumm.Core.Audio.MusicDriverTypes music, long offset)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            var size = _reader.ReadUInt16(); // wa_size
            _reader.BaseStream.Seek(2, System.IO.SeekOrigin.Current);
            var data = _reader.ReadBytes(size - HeaderSize);
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

        ObjectData ReadObject(long offset, int size)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            var id = _reader.ReadUInt16();
            _reader.ReadByte();
            var x = _reader.ReadByte() * 8;
            var tmpY = _reader.ReadByte();
            var y = (tmpY & 0x7F) * 8;
            var parentState = ((tmpY & 0x80) == 0x80) ? 8 : 0;
            var width = _reader.ReadByte() * 8;
            var parent = _reader.ReadByte();
            var walkX = _reader.ReadByte() * 8;
            var walkY = _reader.ReadByte() * 8;
            var tmpActor = _reader.ReadByte();
            var actor = tmpActor & 0x7;
            var height = tmpActor & 0xF8;
            var obj = new ObjectData
            {
                Number = id,
                Position = new Point(x, y),
                ParentState = (byte)parentState,
                Width = (ushort)width,
                Height = (ushort)height,
                Parent = parent,
                Walk = new Point(walkX, walkY),
                ActorDir = actor
            };
            _reader.ReadByte();
            size -= 11;
            ReadObjectScriptOffsets(obj);
            size -= (2 * obj.ScriptOffsets.Count + 1);
            ReadName(obj);
            size -= (obj.Name.Length + 1);
            obj.Script.Data = _reader.ReadBytes(size);
            obj.Script.Offset = 11 + 2 * obj.ScriptOffsets.Count + 1 + obj.Name.Length + 1 + HeaderSize;
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
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            return _reader.ReadBytes(length);
        }
    }
}
