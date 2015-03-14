//
//  ResourceFile8.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core.Graphics;
using System.Linq;

namespace NScumm.Core.IO
{
    class ResourceFile8: ResourceFile7
    {
        public ResourceIndex8 ResourceIndex { get; private set; }

        public ResourceFile8(ResourceIndex8 resourceIndex, string path)
            : base(path)
        {
            ResourceIndex = resourceIndex;
        }

        protected override byte[] ReadVerbScript(ObjectData obj, Chunk chunk)
        {
            return _reader.ReadBytes((int)(chunk.Size - obj.Script.Offset - 8));
        }

        protected override void ReadVerbTable(ObjectData data, int size)
        {
            var pos = _reader.BaseStream.Position - 8;
            int id;
            do
            {
                id = _reader.ReadInt32();
                if (id != 0)
                {
                    var offset = _reader.ReadInt32();
                    if (!data.ScriptOffsets.ContainsKey(id))
                    {
                        data.ScriptOffsets.Add(id, offset);
                    }
                }
            }
            while(id != 0);

            var realCodeOffset = _reader.BaseStream.Position - pos;
            var virtualCodeOffset = (8 * data.ScriptOffsets.Count + 4 + 8);
            var diff = (int)(realCodeOffset - virtualCodeOffset);
            foreach (var key in data.ScriptOffsets.Keys.ToList())
            {
                data.ScriptOffsets[key] -= diff;
            }
            data.Script.Offset = virtualCodeOffset - 8;
        }

        protected override int ReadNumBoxes()
        {
            return _reader.ReadInt32();
        }

        protected override Box ReadBox()
        {
            var box = new Box();
            box.Ulx = _reader.ReadInt32();
            box.Uly = _reader.ReadInt32();
            box.Urx = _reader.ReadInt32();
            box.Ury = _reader.ReadInt32();
            box.Lrx = _reader.ReadInt32();
            box.Lry = _reader.ReadInt32();
            box.Llx = _reader.ReadInt32();
            box.Lly = _reader.ReadInt32();
            box.Mask = _reader.ReadInt32();
            box.Flags = (BoxFlags)_reader.ReadInt32();
            box.ScaleSlot = _reader.ReadInt32();
            box.Scale = _reader.ReadInt32();
            _reader.ReadInt32();
            return box;
        }

        protected override ScaleSlot[] ReadSCAL()
        {
            var scales = new ScaleSlot[4];
            for (int i = 0; i < 4; i++)
            {
                var scale1 = _reader.ReadInt32();
                var y1 = _reader.ReadInt32();
                var scale2 = _reader.ReadInt32();
                var y2 = _reader.ReadInt32();
                scales[i] = new ScaleSlot { Scale1 = scale1, Y1 = y1, Y2 = y2, Scale2 = scale2 };
            }
            return scales;
        }

        public override Room ReadRoom(long offset)
        {
            Room room = null;
            GotoResourceHeader(offset);
            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "LFLF")
                throw new NotSupportedException("LFLF block expected");

            var it = CreateChunkIterator(chunk.Size);
            while (it.MoveNext())
            {
                if (it.Current.Tag == "ROOM")
                {
                    room = base.ReadRoom(offset + 8);
                    room.TransparentColor = (byte)room.Header.Transparency;
                }
                else if (it.Current.Tag == "RMSC")
                {
                    it = CreateChunkIterator(it.Current.Size);
                    while (it.MoveNext())
                    {
                        switch (it.Current.Tag)
                        {
                            case "ENCD":
                                {
                                    // Entry script
                                    room.EntryScript.Data = _reader.ReadBytes((int)(it.Current.Size - 8));
                                }
                                break;
                            case "EXCD":
                                {
                                    // exit script
                                    room.ExitScript.Data = _reader.ReadBytes((int)(it.Current.Size - 8));
                                }
                                break;

                            case "LSCR":
                                // local script
                                var pos = _reader.BaseStream.Position;
                                var index = ReadScriptIndex();
                                var size = 8 + _reader.BaseStream.Position - pos;
                                room.LocalScripts[index - GetNumGlobalScripts()] = new ScriptData
                                {
                                    Data = _reader.ReadBytes((int)(it.Current.Size - size))
                                };
                                break;

                            case "OBCD":
                                {
                                    // object script
                                    var obj = ReadObjectCode(it.Current.Size - 8);
                                    var objImg = room.Objects.FirstOrDefault(o => o.Number == obj.Number);
                                    if (objImg != null)
                                    {
                                        room.Objects.Remove(objImg);
                                        room.Objects.Add(Merge(objImg, obj));
                                    }
                                    else
                                    {
                                        room.Objects.Add(obj);
                                    }
                                }
                                break;      
                            default:
                                UnknownChunk(chunk);
                                break;
                        }
                    }
                    return room;
                }
            }
            return room;
        }

        protected override void ReadRoomImages(Room room)
        {
            var chunkWrap = ChunkIterator5.ReadChunk(_reader);
            if (chunkWrap.Tag != "WRAP")
                throw new NotSupportedException("WRAP block was expected.");

            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "OFFS")
                throw new NotSupportedException("OFFS block was expected.");
            _reader.BaseStream.Seek(chunk.Size - 8, SeekOrigin.Current);

            var smapChunk = ChunkIterator5.ReadChunk(_reader);
            if (smapChunk.Tag != "SMAP")
                throw new NotSupportedException("SMAP block was expected.");

            List<Chunk> zplaneChunks = new List<Chunk>();
            var it = CreateChunkIterator(smapChunk.Size - 8);
            while (it.MoveNext())
            {
                switch (it.Current.Tag)
                {
                    case "ZPLN":
                        chunk = ChunkIterator5.ReadChunk(_reader);
                        if (chunk.Tag != "WRAP")
                            throw new NotSupportedException("WRAP block was expected.");
                        it = CreateChunkIterator(chunk.Size - 8);
                        break;
                    case "ZSTR":
                        zplaneChunks.Add(it.Current);
                        break;
                }
            }

            if (zplaneChunks.Count == 0)
            {
                // Read smap
                _reader.BaseStream.Seek(smapChunk.Offset, SeekOrigin.Begin);
                room.Image = new ImageData{ Data = _reader.ReadBytes((int)(smapChunk.Size - 8)) };
            }
            else
            {
                _reader.BaseStream.Seek(smapChunk.Offset, SeekOrigin.Begin);
                var zplanesSize = 0;
                for (int i = 0; i < zplaneChunks.Count; i++)
                {
                    zplanesSize += (int)zplaneChunks[i].Size;
                }
                room.Image = new ImageData{ Data = _reader.ReadBytes((int)(smapChunk.Size - 8 - zplanesSize)) };
                for (int i = 0; i < zplaneChunks.Count; i++)
                {
                    room.Image.ZPlanes.Add(ReadZPlane());
                }
            }
        }

        ZPlane ReadZPlane()
        {
            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "ZSTR")
                throw new NotSupportedException("ZSTR block was expected.");

            var chunkWrap = ChunkIterator5.ReadChunk(_reader);
            if (chunkWrap.Tag != "WRAP")
                throw new NotSupportedException("WRAP block was expected.");

            var chunkOffs = ChunkIterator5.ReadChunk(_reader);
            if (chunkOffs.Tag != "OFFS")
                throw new NotSupportedException("OFFS block was expected.");

            var num = (chunkOffs.Size - 8) / 4;
            var offs = new int?[num];
            for (int i = 0; i < num; i++)
            {
                offs[i] = (int)(_reader.ReadInt32() - chunkOffs.Size);
            }

            var data = _reader.ReadBytes((int)(chunkWrap.Size - 8 - chunkOffs.Size));
            return new ZPlane(data, offs);
        }

        protected override ImageData ReadBomp(long size)
        {
            var width = _reader.ReadUInt32();
            var height = _reader.ReadUInt32();
            var img = new ImageData();
            img.Data = _reader.ReadBytes((int)(size - 8));
            img.IsBomp = true;
            return img;
        }

        protected override RoomHeader ReadRMHD()
        {
            var version = _reader.ReadUInt32();
            var header = new RoomHeader
            {
                Width = _reader.ReadInt32(),
                Height = _reader.ReadInt32(),
                NumObjects = _reader.ReadInt32(),
                NumZBuffer = _reader.ReadInt32(),
                Transparency = _reader.ReadInt32(),
            };
            return header;
        }

        protected override void ReadImageHeader(ObjectData od)
        {
            // image header
            var name = _reader.ReadBytes(40);
            var text = ResourceIndex8.DataToString(name);
            if (ResourceIndex.ObjectIDMap.ContainsKey(text))
            {
                var id = ResourceIndex.ObjectIDMap[text];
                od.Number = (ushort)id;
            }

            od.Name = name;
            var version = _reader.ReadUInt32();
            var numImnn = _reader.ReadInt32();
            od.Position = new Point(_reader.ReadInt32(), _reader.ReadInt32());
            od.Width = (ushort)_reader.ReadInt32();
            od.Height = (ushort)_reader.ReadInt32();
            od.ActorDir = _reader.ReadInt32();
            od.Flags = (DrawBitmaps)_reader.ReadInt32();
            for (int i = 0; i < 15; i++)
            {
                od.Hotspots.Add(new Point(_reader.ReadInt32(), _reader.ReadInt32()));
            }
        }

        protected override ObjectData Merge(ObjectData objImg, ObjectData objCode)
        {
            base.Merge(objImg, objCode);
            objCode.Width = objImg.Width;
            objCode.Height = objImg.Height;
            objCode.Position = objImg.Position;
            objCode.ActorDir = objImg.ActorDir;
            objCode.Flags = objImg.Flags;
            objCode.Hotspots.Clear();
            objCode.Hotspots.AddRange(objImg.Hotspots);
            return objCode;
        }

        protected override int ReadScriptIndex()
        {
            return _reader.ReadInt32();
        }
    }
}
