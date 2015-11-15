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
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm.IO
{
    class ResourceFile3_16: ResourceFile
    {
        const int HeaderSize = 4;

        GameInfo _game;

        public ResourceFile3_16(GameInfo game, Stream stream)
            : base(stream)
        {
            _game = game;
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
            _reader.BaseStream.Seek(offset, SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            var data = _reader.ReadBytes(size - HeaderSize);
            return data;
        }

        public override byte[] ReadCostume(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Current);
            var size = _reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);

            return _reader.ReadBytes(size - HeaderSize);
        }

        public byte[] ReadAmigaSound(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = _reader.ReadUInt16();
            _reader.BaseStream.Seek(-2, SeekOrigin.Current);
            return _reader.ReadBytes(size);
        }

        public override byte[] ReadSound(MusicDriverTypes music, long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var wa_offs = offset;
            var wa_size = _reader.ReadUInt16();
            int ad_size = 0;
            int ad_offs = 0;
            _reader.BaseStream.Seek(wa_size - 2, SeekOrigin.Current);
            if (!(_game.Platform == Platform.AtariST || _game.Platform == Platform.Macintosh))
            {
                ad_offs = (int)_reader.BaseStream.Position;
                ad_size = _reader.ReadUInt16();
            }
            _reader.BaseStream.Seek(wa_offs, SeekOrigin.Begin);
            var data = _reader.ReadBytes(wa_size);
            return data;
        }

        public override Room ReadRoom(long offset)
        {
            var size = (int)_reader.ReadUInt16();
            var tmp = _reader.ReadBytes(2);
            var header = ReadRMHD();

            var room = new Room { Header = header, Size = size };

            // 10
            var imgOffset = _reader.ReadUInt16();
            // 12
            _reader.BaseStream.Seek(8, SeekOrigin.Current);
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
            var exitScriptLength = entryScriptOffset - exitScriptOffset;
            // 29
            var objImgOffsets = _reader.ReadUInt16s(numObjects);
            var objScriptOffsets = _reader.ReadUInt16s(numObjects);
            // load sounds
            var soundIds = _reader.ReadBytes(numSounds);
            // load scripts
            var scriptIds = _reader.ReadBytes(numScripts);

            // determine the entry script size
            _reader.ReadByte();
            var firstLocalScriptOffset = _reader.ReadUInt16();
            _reader.BaseStream.Seek(-3, SeekOrigin.Current);
            var entryScriptLength = firstLocalScriptOffset - entryScriptOffset;

            ReadLocalScripts(offset, size, room);

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
            var imgLength = GetBlockSize(offset + imgOffset);
            room.Image = new ImageData{ Data = ReadBytes(offset + imgOffset, imgLength) };

            // read boxes
            ReadBoxes(offset + boxesOffset, imgOffset - boxesOffset, room);

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
                        var objImgSize = GetBlockSize(offset + objImgOffset);
                        if (objImgSize > 0)
                        {
                            room.Objects[i].Images.Add(new ImageData{ Data = ReadBytes(offset + objImgOffset, objImgSize) });
                        }
                    }
                }
            }

            return room;
        }

        void ReadLocalScripts(long offset, int size, Room room)
        {
            // local script offsets
            byte id;
            var localScriptOffsets = new List<Tuple<int, int>>();
            while ((id = _reader.ReadByte()) != 0)
            {
                localScriptOffsets.Add(Tuple.Create(id - 200, (int)_reader.ReadUInt16()));
            }
            // local script data
            if (localScriptOffsets.Count > 0)
            {
                int i;
                for (i = 0; i < localScriptOffsets.Count - 1; i++)
                {
                    var localScriptInfo = localScriptOffsets[i];
                    var localScriptId = localScriptInfo.Item1;
                    var localScriptOffset = localScriptInfo.Item2;
                    var nextScriptOffset = localScriptOffsets[i + 1].Item2;
                    room.LocalScripts[localScriptId] = new ScriptData
                    {
                        Data = ReadBytes(offset + localScriptOffset, nextScriptOffset - localScriptOffset + HeaderSize),
                        Offset = offset + localScriptOffset
                    };
                }
                room.LocalScripts[localScriptOffsets[i].Item1] = new ScriptData
                {
                    Data = ReadBytes(offset + localScriptOffsets[i].Item2, size - localScriptOffsets[i].Item2),
                    Offset = offset + localScriptOffsets[i].Item2
                };
            }
        }

        ObjectData ReadObject(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = (int)_reader.ReadUInt16();
            var tmp = _reader.ReadUInt16();
            var id = _reader.ReadUInt16();
            var @class = _reader.ReadByte();
            var x = _reader.ReadByte() * 8;
            var tmpY = _reader.ReadByte();
            var y = (tmpY & 0x7F) * 8;
            var parentState = ((tmpY & 0x80) == 0x80) ? 1 : 0;
            var width = _reader.ReadByte() * 8;
            var parent = _reader.ReadByte();
            var walkX = _reader.ReadUInt16();
            var walkY = _reader.ReadUInt16();
            var tmpActor = _reader.ReadByte();
            var actor = tmpActor & 0x7;
            var height = tmpActor & 0xF8;
            var obj = new ObjectData(id)
            {
                Position = new Point(x, y),
                ParentState = (byte)parentState,
                Width = (ushort)width,
                Height = (ushort)height,
                Parent = parent,
                Walk = new Point(walkX, walkY),
                ActorDir = actor
            };
            var nameOffset = _reader.ReadByte();
            var read = 17;
            ReadObjectScriptOffsets(obj);
            read += (3 * obj.ScriptOffsets.Count + 1);
            ReadName(obj);
            read += (obj.Name.Length + 1);
            size -= read;
            obj.Script.Data = _reader.ReadBytes(size);
            obj.Script.Offset = read;
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
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _reader.ReadBytes(length);
        }

        void ReadBoxes(long offset, int size, Room room)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var numBoxes = _reader.ReadByte();
            size--;
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

        int GetBlockSize(long offset)
        {
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var size = (int)_reader.ReadUInt16();
            return size;
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
