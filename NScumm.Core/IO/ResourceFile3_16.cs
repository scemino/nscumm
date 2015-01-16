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
using System;
using System.Linq;

namespace NScumm.Core.IO
{
    class ResourceFile3_16: ResourceFile
    {
        const int HeaderSize = 4;

        public ResourceFile3_16(string path, byte encByte)
            : base(path, encByte)
        {
			
        }

        public override Dictionary<byte, long> ReadRoomOffsets()
        {
            return new Dictionary<byte, long>();
        }

        protected virtual Box ReadBox(ref int size)
        {
            var box = new Box()
            {
                Ulx = _reader.ReadInt16(),
                Uly = _reader.ReadInt16(),
                Urx = _reader.ReadInt16(),
                Ury = _reader.ReadInt16(),
                Lrx = _reader.ReadInt16(),
                Lry = _reader.ReadInt16(),
                Llx = _reader.ReadInt16(),
                Lly = _reader.ReadInt16(),
                Mask = _reader.ReadByte(),
                Flags = (BoxFlags)_reader.ReadByte()
            };
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

        public override byte[] ReadScript(long offset)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            var data = _reader.ReadBytes(size - HeaderSize);
            return data;
        }

        public override byte[] ReadCostume(long offset)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            return _reader.ReadBytes(size - HeaderSize);
        }

        public override byte[] ReadSound(long offset)
        {
			_reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            var size = _reader.ReadUInt16(); // wa_size
			_reader.BaseStream.Seek(size-2, System.IO.SeekOrigin.Current);
			size = _reader.ReadUInt16(); // ad_size
			_reader.BaseStream.Seek(2, System.IO.SeekOrigin.Current);
            var data = _reader.ReadBytes(size - HeaderSize);
            return data;
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
            var boxesOffset = _reader.ReadUInt16();
            // 23
            var numSounds = _reader.ReadByte();
            // 24
            var numScripts = _reader.ReadByte();
            // 25
            var exitScriptOffset = _reader.ReadUInt16();
            // 27
            var entryScriptOffset = _reader.ReadUInt16();
            var exitScriptLength = exitScriptOffset > 0 ? entryScriptOffset - exitScriptOffset : 0;
            // 29
            var objImgOffsets = _reader.ReadUInt16s(numObjects);
            var objScriptOffsets = _reader.ReadUInt16s(numObjects);
            _reader.BaseStream.Seek(numSounds + numScripts, System.IO.SeekOrigin.Current);
            var scriptOffsets = ReadLocalScriptOffsets();
            var firstScriptOffset = (scriptOffsets.Count > 0) ? scriptOffsets[scriptOffsets.Keys.First()] : offset + size;
            var entryScriptLength = firstScriptOffset - entryScriptOffset;

            // read obj images
            var objImgSizes = new ushort[objImgOffsets.Length + 1];
            Array.Copy(objImgOffsets, objImgSizes, objScriptOffsets.Length);
            objImgSizes[objImgOffsets.Length] = objScriptOffsets[0];
            Array.Sort(objImgSizes);

            var room = new Room
            {
                Header = header,
                ExitScript =
                {
                    Data = ReadBytes(offset + exitScriptOffset, exitScriptLength)
                },
                EntryScript =
                {
                    Data = ReadBytes(offset + entryScriptOffset, (int)entryScriptLength)
                }
            };

            ReadLocalScripts(offset, size, scriptOffsets, room);
            ReadBoxes(offset + boxesOffset, imgOffset - boxesOffset, room);

            if (exitScriptOffset == 0)
            {
                exitScriptOffset = entryScriptOffset;
            }

            room.Image = new ImageData{ Data = ReadBytes(offset + imgOffset, exitScriptOffset - imgOffset) };

            var objScriptSizes = new ushort[objScriptOffsets.Length + 1];
            Array.Copy(objScriptOffsets, objScriptSizes, objScriptOffsets.Length);
            objScriptSizes[objScriptOffsets.Length] = exitScriptOffset;
            Array.Sort(objScriptSizes);

            for (var i = 0; i < objScriptSizes.Length - 1; i++)
            {
                var objOffset = objScriptSizes[i];
                var objSize = objScriptSizes[i + 1] - objOffset - HeaderSize;
                var obj = ReadObject(offset + objOffset + HeaderSize, objSize);
                room.Objects.Add(obj);
            }

            for (var i = 0; i < objImgSizes.Length - 1; i++)
            {
                var objImgOffset = objImgSizes[i];
                var objImgSize = objImgSizes[i + 1] - objImgOffset;
                room.Objects[i].Images.Add(new ImageData{ Data = ReadBytes(objImgOffset, objImgSize) });
            }

            return room;
        }

        ObjectData ReadObject(long offset, int size)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            var id = _reader.ReadUInt16();
            _reader.ReadByte();
            var x = _reader.ReadByte() * 8;
            var tmpY = _reader.ReadByte() * 8;
            var y = tmpY & 0x7F * 8;
            var parentState = ((tmpY & 0x80) == 0x80) ? 1 : 0;
            var width = _reader.ReadByte() * 8;
            var parent = _reader.ReadByte();
            var walkX = _reader.ReadUInt16();
            var walkY = _reader.ReadUInt16();
            var tmpActor = _reader.ReadByte();
            var actor = tmpActor & 0x7;
            var height = tmpActor & 0xF8;
            var obj = new ObjectData
            {
                Number = id,
                Position = new Point((short)x, (short)y),
                ParentState = (byte)parentState,
                Width = (ushort)width,
                Height = (ushort)height,
                Parent = parent,
                Walk = new Point((short)walkX, (short)walkY),
                ActorDir = (byte)actor
            };
            _reader.ReadByte();
            size -= 13;
            ReadObjectScriptOffsets(obj);
            size -= (3 * obj.ScriptOffsets.Count + 1);
            ReadName(obj);
            size -= (obj.Name.Length + 1);
            obj.Script.Data = _reader.ReadBytes(size);
            return obj;
        }

        void ReadObjectScriptOffsets(ObjectData obj)
        {
            byte entry;
            while ((entry = _reader.ReadByte()) != 0)
            {
                obj.ScriptOffsets.Add(entry, _reader.ReadUInt16());
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

        void ReadBoxes(long offset, int size, Room room)
        {
            _reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
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

        Dictionary<byte, ushort> ReadLocalScriptOffsets()
        {
            byte id;
            var scriptOffsets = new Dictionary<byte, ushort>();
            while ((id = _reader.ReadByte()) != 0)
            {
                scriptOffsets[id] = _reader.ReadUInt16();
            }
            return scriptOffsets;
        }

        void ReadLocalScripts(long offset, ushort size, Dictionary<byte, ushort> scriptOffsets, Room room)
        {
            if (scriptOffsets.Count > 0)
            {
                var enumScriptOffset = scriptOffsets.GetEnumerator();
                enumScriptOffset.MoveNext();
                for (var i = 0; i < scriptOffsets.Count - 1; i++)
                {
                    var script = enumScriptOffset.Current;
                    enumScriptOffset.MoveNext();
                    var scriptLen = enumScriptOffset.Current.Value - script.Value;
                    _reader.BaseStream.Seek(offset + script.Value, System.IO.SeekOrigin.Begin);
                    room.LocalScripts[script.Key - 200] = new ScriptData
                    {
                        Offset = offset + script.Value,
                        Data = _reader.ReadBytes(scriptLen)
                    };
                }
                {
                    var script = enumScriptOffset.Current;
                    var scriptLen = size - script.Value;
                    _reader.BaseStream.Seek(offset + script.Value, System.IO.SeekOrigin.Begin);
                    room.LocalScripts[script.Key - 200] = new ScriptData
                    {
                        Offset = offset + script.Value - HeaderSize,
                        Data = _reader.ReadBytes(scriptLen)
                    };
                }
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
    }
	
}
