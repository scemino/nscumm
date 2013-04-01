/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace Scumm4
{
    public class DiskFile
    {
        #region Fields
        private XorReader _reader; 
        #endregion

        #region Chunk Class
        private sealed class Chunk
        {
            public uint Size { get; set; }
            public ushort Tag { get; set; }
            public long Offset { get; set; }
        }
        #endregion

        #region ChunkIterator Class
        private sealed class ChunkIterator : IEnumerator<Chunk>
        {
            private readonly XorReader _reader;
            private readonly long _position;
            private readonly long _size;

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
                if (this.Current != null)
                {
                    var offset = this.Current.Offset + this.Current.Size - 6;
                    _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                }
                this.Current = null;
                if (_reader.BaseStream.Position < (_position + _size - 6) && _reader.BaseStream.Position < _reader.BaseStream.Length)
                {
                    var size = _reader.ReadUInt32();
                    var tag = _reader.ReadUInt16();
                    this.Current = new Chunk { Offset = _reader.BaseStream.Position, Size = size, Tag = tag };
                }
                return this.Current != null;
            }

            public void Reset()
            {
                _reader.BaseStream.Seek(_position, SeekOrigin.Begin);
                this.Current = null;
            }
        }
        #endregion

        #region Constructor
        public DiskFile(string path, byte encByte)
        {
            var fs = File.OpenRead(path);
            var br2 = new BinaryReader(fs);
            _reader = new XorReader(br2, encByte);
        } 
        #endregion

        #region Public Methods
        public Dictionary<byte, int> ReadRoomOffsets()
        {
            Dictionary<byte, int> roomOffsets = new Dictionary<byte, int>();
            do
            {
                var size = _reader.ReadInt32();
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
                            int offset2 = _reader.ReadInt32();
                            roomOffsets[room] = offset2;
                        }
                        return roomOffsets;
                    // *LFLF* disk block
                    case 0x464C:
                        var roomNum = _reader.ReadUInt16();
                        Console.WriteLine("#Room:" + roomNum);
                        break;
                    default:
                        // skip
                        Console.WriteLine("Skip Block: 0x{0:X2}", blockType);
                        _reader.BaseStream.Seek(size - 6, SeekOrigin.Current);
                        break;
                }
            } while (_reader.BaseStream.Position < _reader.BaseStream.Length);
            return null;
        }

        public Room ReadRoom(int roomOffset)
        {
            Dictionary<ushort, byte[]> stripsDic = new Dictionary<ushort, byte[]>();
            Stack<ChunkIterator> its = new Stack<ChunkIterator>();
            Room room = new Room();
            _reader.BaseStream.Seek(roomOffset, SeekOrigin.Begin);
            var it = new ChunkIterator(_reader, _reader.BaseStream.Length - _reader.BaseStream.Position);
            do
            {
                while (it.MoveNext())
                {
                    switch (it.Current.Tag)
                    {
                        case 0x464C:
                            // *LFLF* disk block
                            var roomNum = _reader.ReadUInt16();
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
                            ReadCYCL();
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
                                    Box box = new Box();
                                    box.ulx = _reader.ReadInt16();
                                    box.uly = _reader.ReadInt16();
                                    box.urx = _reader.ReadInt16();
                                    box.ury = _reader.ReadInt16();
                                    box.lrx = _reader.ReadInt16();
                                    box.lry = _reader.ReadInt16();
                                    box.llx = _reader.ReadInt16();
                                    box.lly = _reader.ReadInt16();
                                    box.mask = _reader.ReadByte();
                                    box.flags = (BoxFlags)_reader.ReadByte();
                                    box.scale = _reader.ReadUInt16();
                                    room.Boxes.Add(box);
                                    size -= 20;
                                }

                                if (size > 0)
                                {
                                    room.BoxMatrix.AddRange(_reader.ReadBytes(size));
                                }
                            }
                            break;
                        case 0x4150:
                            {
                                // CLUT
                                var colors = ReadCLUT();
                                room.Palette.Colors.AddRange(colors);
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
                                room.Data = _reader.ReadBytes((int)(it.Current.Size - 6));
                            }
                            break;
                        case 0x4E45:
                            {
                                // Entry script
                                byte[] entryScript = _reader.ReadBytes((int)(it.Current.Size - 6));
                                if (room.EntryScript.Data == null)
                                {
                                    room.EntryScript.Data = entryScript;
                                }
                                else
                                {
                                    throw new NotSupportedException("Entry script has already been defined.");
                                }
                            }
                            break;
                        case 0x5845:
                            {
                                // Exit script
                                byte[] exitScript = _reader.ReadBytes((int)(it.Current.Size - 6));
                                if (room.ExitScript.Data == null)
                                {
                                    room.ExitScript.Data = exitScript;
                                }
                                else
                                {
                                    throw new NotSupportedException("Exit script has already been defined.");
                                }
                            }
                            break;
                        case 0x4C53:
                            {
                                // *SL* 
                                var num = _reader.ReadByte();
                            }
                            break;
                        case 0x434C: //LC
                            {
                                // *NLSC* number of local scripts
                                var numScripts = _reader.ReadUInt16();
                            }
                            break;
                        case 0x534C:
                            {
                                // local scripts
                                var index = _reader.ReadByte();
                                var pos = _reader.BaseStream.Position;
                                room.LocalScripts[index - 0xC8] = new ScriptData { Offset = pos - roomOffset - 8, Data = _reader.ReadBytes((int)(it.Current.Size - 7)) };
                            }
                            break;
                        case 0x494F:
                            {
                                // Object Image
                                var objId = _reader.ReadUInt16();
                                if (it.Current.Size > 6)
                                {
                                    stripsDic.Add(objId, _reader.ReadBytes((int)(it.Current.Size - 6)));
                                }
                            }
                            break;
                        case 0x434F:
                            {
                                // Object script
                                var objId = _reader.ReadUInt16();
                                var unk = _reader.ReadByte();
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

                                ObjectData data = new ObjectData();
                                data.obj_nr = objId;
                                data.x_pos = (short)(8 * x);
                                data.y_pos = (short)(8 * y);
                                data.width = (ushort)(8 * width);
                                //data.height = (ushort)(8 * height);
                                data.height = height;
                                data.parent = parent;
                                data.parentstate = parentState;
                                data.walk_x = walk_x;
                                data.walk_y = walk_y;
                                data.actordir = actordir;
                                room.Objects.Add(data);

                                var nameOffset = _reader.ReadByte();

                                // read scripts
                                ReadObjectScripts(it, data);
                                data.Name = ReadObjectName(it, nameOffset);
                                ReadObjectImage(stripsDic, data);
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

        public XorReader ReadCostume(byte room, int costOffset)
        {
            _reader.BaseStream.Seek(costOffset + 8, SeekOrigin.Begin);
            var size = _reader.ReadInt32();
            var tag = _reader.ReadInt16();
            if (tag != 0x4F43) throw new NotSupportedException("Invalid costume.");
            return _reader;
        }

        public byte[] ReadScript(int roomOffset)
        {
            _reader.BaseStream.Seek(roomOffset + 8, SeekOrigin.Begin);
            var size = _reader.ReadInt32();
            var tag = _reader.ReadInt16();
            if (tag != 0x4353) throw new NotSupportedException("Expected SC block.");
            var data = _reader.ReadBytes(size - 6);
            return data;
        }

        public byte[] ReadCharsetData()
        {
            var size = _reader.ReadInt32() + 11;
            return _reader.ReadBytes(size);
        } 
        #endregion

        #region Private Methods
        private byte[] ReadObjectName(ChunkIterator it, byte nameOffset)
        {
            _reader.BaseStream.Seek(it.Current.Offset + nameOffset - 6, SeekOrigin.Begin);
            List<byte> name = new List<byte>();
            var c = _reader.ReadByte();
            while (c != 0)
            {
                name.Add(c);
                c = _reader.ReadByte();
            }
            return name.ToArray();
        }

        private void ReadObjectScripts(ChunkIterator it, ObjectData data)
        {
            var tableLength = (_reader.BaseStream.Position - it.Current.Offset) / 3;
            List<Tuple<byte, ushort>> offsets = new List<Tuple<byte, ushort>>();
            for (int i = 0; i < tableLength; i++)
            {
                var id = _reader.ReadByte();
                var offset = _reader.ReadUInt16();
                if (id == 0)
                    break;
                data.ScriptOffsets.Add(id, offset);
                offsets.Add(Tuple.Create(id, offset));
            }
            offsets = (from offset in offsets
                       orderby offset.Item2
                       select offset).ToList();
            if (offsets.Count > 0)
            {
                for (int i = 0; i < offsets.Count - 1; i++)
                {
                    _reader.BaseStream.Seek(it.Current.Offset + offsets[i].Item2 - 6, SeekOrigin.Begin);
                    //var size = offsets[i + 1].Item2 - offsets[i].Item2;
                    var size = (int)(it.Current.Offset + it.Current.Size - 6 - _reader.BaseStream.Position);
                    var script = new ScriptData { Data = _reader.ReadBytes(size) };
                    data.Scripts.Add(offsets[i].Item1, script);
                }
                {
                    int i = offsets.Count - 1;
                    _reader.BaseStream.Seek(it.Current.Offset + offsets[i].Item2 - 6, SeekOrigin.Begin);
                    var size = (int)(it.Current.Offset + it.Current.Size - 6 - _reader.BaseStream.Position);
                    var script = new ScriptData { Data = _reader.ReadBytes(size) };
                    data.Scripts.Add(offsets[i].Item1, script);
                }
            }
        }

        private static void ReadObjectImage(Dictionary<ushort, byte[]> stripsDic, ObjectData obj)
        {
            if (stripsDic.ContainsKey(obj.obj_nr))
            {
                var stripData = stripsDic[obj.obj_nr];
                obj.Image = stripData;
            }
            else
            {
                obj.Image = new byte[0];
            }
        }

        private RoomHeader ReadRMHD()
        {
            RoomHeader header = new RoomHeader();
            header.Width = _reader.ReadUInt16();
            header.Height = _reader.ReadUInt16();
            header.NumObjects = _reader.ReadUInt16();
            return header;
        }

        private void ReadCYCL()
        {
            for (int i = 0; i < 16; i++)
            {
                var freq = _reader.ReadUInt16();
                var start = _reader.ReadByte();
                var end = _reader.ReadByte();
            }
        }

        private Scale[] ReadSCAL()
        {
            Scale[] scales = new Scale[4];
            for (int i = 0; i < 4; i++)
            {
                var scale1 = _reader.ReadUInt16();
                var y1 = _reader.ReadUInt16();
                var scale2 = _reader.ReadUInt16();
                var y2 = _reader.ReadUInt16();
                scales[i] = new Scale { scale1 = scale1, y1 = y1, y2 = y2, scale2 = scale2 };
            }
            return scales;
        }

        private byte[] ReadEPAL()
        {
            return _reader.ReadBytes(256);
        }

        private Color[] ReadCLUT()
        {
            var numColors = _reader.ReadUInt16() / 3;
            var colors = new Color[numColors];
            for (int i = 0; i < numColors; i++)
            {
                colors[i] = Color.FromArgb(255, _reader.ReadByte(), _reader.ReadByte(), _reader.ReadByte());
            }
            return colors;
        }         
        #endregion
    }
}
