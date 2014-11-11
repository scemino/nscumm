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

using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core.Graphics;
using System.Linq;

namespace NScumm.Core.IO
{
    class ResourceFile6: ResourceFile5
    {
        public ResourceFile6(string path, byte encByte)
            : base(path, encByte)
        {

        }

        public override Room ReadRoom(long offset)
        {
            GotoResourceHeader(offset);
            var chunk = ChunkIterator5.ReadChunk(_reader);

            if (chunk.Tag != "ROOM")
            {
                throw new NotSupportedException("Room cblock was expected.");
            }

            var room = ReadRoomCore(offset, chunk.Size);
            return room;
        }

        Room ReadRoomCore(long offset, long chunkSize)
        {
            var stripsDic = new Dictionary<ushort, byte[]>();
            var room = new Room();
            var it = new ChunkIterator5(_reader, chunkSize);
            var images = new Dictionary<ushort, List<ImageData>>();
            while (it.MoveNext())
            {
                switch (it.Current.Tag)
                {
                    case "TRNS":
                        {
                            // transparent color
                            room.TransparentColor = _reader.ReadByte();
                            var unknown = _reader.ReadByte();
                        }
                        break;

                    case "RMHD":
                                    // ROOM Header
                        room.Header = ReadRMHD();
                        break;

                    case "RMIM":
                                    // room image
                        ReadRoomImage(room);
                        break;
                                
                    case "CYCL":
                                    // CYCL
                        room.ColorCycle = ReadCYCL();
                        break;
                    case "EPAL":
                                    // EPAL
                        ReadEPAL();
                        break;
                    case "BOXD":
                                    // box data
                        {
                            var numBoxes = _reader.ReadUInt16();
                            for (int i = 0; i < numBoxes; i++)
                            {
                                var box = ReadBox();
                                room.Boxes.Add(box);
                            }
                        }
                        break;
                    case "BOXM":
                                    // box matrix
                        {
                            var size = (int)(it.Current.Size - 8);
                            room.BoxMatrix.Clear();
                            room.BoxMatrix.AddRange(_reader.ReadBytes(size));
                        }
                        break;
                    case "PALS":
                        {
                            room.HasPalette = true;
                            var palettes = ReadPalettes().ToArray();
                            room.Palettes.Clear();
                            room.Palettes.AddRange(palettes);
                        }
                        break;
                    case "SCAL":
                                    // SCAL
                        if (it.Current.Size > 8)
                        {
                            room.Scales = ReadSCAL();
                        }
                        break;
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
                    case "NLSC":
                        {
                            // number of local scripts
                            var numLocalScripts = _reader.ReadUInt16();
                        }
                        break;
                    case "LSCR":
                        {
                            // local script
                            var index = _reader.ReadByte();
                            room.LocalScripts[index - 200] = new ScriptData
                            {
                                Data = _reader.ReadBytes((int)(it.Current.Size - 9))
                            };
                        }
                        break;
                    case "OBIM":
                        {
                            // Object Image
                            var imgObj = ReadObjectImages(it.Current.Size - 8);
                            images[imgObj.Item1] = imgObj.Item2;
                        }
                        break;
                    case "OBCD":
                        {
                            // object script
                            var obj = ReadObjectCode(it.Current.Size - 8);
                            if (images.ContainsKey(obj.Number))
                            {
                                obj.Images.AddRange(images[obj.Number]);
                            }
                            room.Objects.Add(obj);
                        }
                        break;                        

                    default:
                        UnknownChunk(it.Current);
                        break;
                }
            }

            return room;
        }

        void ReadRoomImage(Room room)
        {
            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "RMIH")
                throw new NotSupportedException("Room Header block was expected.");
            // number of z buffers
            room.NumZBuffer = _reader.ReadUInt16() + 1;

            chunk = ChunkIterator5.ReadChunk(_reader);
            if (!chunk.Tag.StartsWith("IM", StringComparison.InvariantCulture))
                throw new NotSupportedException("Image block was expected.");

            room.Image = ReadImage(chunk.Size - 8, room.Header.Width / 8);
        }

        IEnumerable<Palette> ReadPalettes()
        {
            var chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "WRAP")
                throw new NotSupportedException("WRAP block was expected.");

            chunk = ChunkIterator5.ReadChunk(_reader);
            if (chunk.Tag != "OFFS")
                throw new NotSupportedException("OFFS block was expected.");
            var num = (int)(chunk.Size - 8) / 4;
            var offsets = _reader.ReadUInt32s(num);
            for (int i = 0; i < num; i++)
            {
                chunk = ChunkIterator5.ReadChunk(_reader);
                if (chunk.Tag != "APAL")
                    throw new NotSupportedException("APAL block was expected.");
                yield return new Palette(ReadCLUT());
            }
        }

        protected override ObjectData ReadCDHD()
        {
            var obj = new ObjectData
            {
                Number = _reader.ReadUInt16(),
                Position = new Point(_reader.ReadInt16(), _reader.ReadInt16()),
                Width = _reader.ReadUInt16(),
                Height = _reader.ReadUInt16(),
                Flags = (DrawBitmaps)_reader.ReadByte(),
                Parent = _reader.ReadByte(),
                Walk = new Point(_reader.ReadInt16(), _reader.ReadInt16()),
                ActorDir = _reader.ReadByte()
            };
            obj.ParentState = (obj.Flags == (DrawBitmaps)0x80) ? (byte)1 : (byte)((int)obj.Flags & 0xF);
            return obj;
        }
    }
 
}
