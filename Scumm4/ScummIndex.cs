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
using System.Linq;
using System.Text;
using Scumm4;
using System.IO;

namespace Scumm4
{
    public class ScummIndex
    {
        private struct Resource
        {
            public byte roomNum;
            public int offset;
        }

        #region Fields
        private Resource[] _rooms;
        private Resource[] _scripts;
        private Resource[] _sounds;
        private Resource[] _costumes;
        private Dictionary<byte, string> _roomNames;
        private Dictionary<byte, Room> _roomsData = new Dictionary<byte, Room>();
        private string m_directory;
        #endregion

        public string Directory
        {
            get { return m_directory; }
        }

        public void LoadIndex(string path)
        {
            this._roomNames = new Dictionary<byte, string>();
            this.m_directory = System.IO.Path.GetDirectoryName(path);
            using (var file = File.Open(path, FileMode.Open))
            {
                BinaryReader br1 = new BinaryReader(file);
                XorReader br = new XorReader(br1, 0);
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var size = br.ReadInt32();
                    var block = br.ReadInt16();
                    switch (block)
                    {
                        case 0x4E52:
                            for (byte room; (room = br.ReadByte()) != 0; )
                            {
                                var dataName = br.ReadBytes(9);
                                StringBuilder name = new StringBuilder();
                                for (int i = 0; i < 9; i++)
                                {
                                    var b = dataName[i] ^ 0xFF;
                                    name.Append((char)b);
                                }
                                this._roomNames.Add(room, name.ToString());
                            }
                            break;
                        case 0x5230:	// 'R0'
                            _rooms = ReadResTypeList(br);
                            break;

                        case 0x5330:	// 'S0'
                            _scripts = ReadResTypeList(br);
                            break;

                        case 0x4E30:	// 'N0'
                            _sounds = ReadResTypeList(br);
                            break;

                        case 0x4330:	// 'C0'
                            _costumes = ReadResTypeList(br);
                            break;

                        case 0x4F30:	// 'O0'
                            ReadDirectoryOfObjects(br);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        public byte[] GetScript(byte scriptNum)
        {
            byte[] data = null;
            var offset = _scripts[scriptNum].offset;
            var disk = OpenRoom(_scripts[scriptNum].roomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                data = disk.ReadScript(rOffsets[_scripts[scriptNum].roomNum] + offset);
            }
            return data;
        }

        public Costume GetCostume(byte scriptNum)
        {
            Costume data = null;
            var disk = OpenRoom(_costumes[scriptNum].roomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                var offset = _costumes[scriptNum].offset;
                data = disk.ReadCostume(_costumes[scriptNum].roomNum, rOffsets[_costumes[scriptNum].roomNum] + offset);
            }
            return data;
        }

        public XorReader GetCostumeReader(byte scriptNum)
        {
            XorReader reader = null;
            var disk = OpenRoom(_costumes[scriptNum].roomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                var offset = _costumes[scriptNum].offset;
                reader = disk.ReadCostume2(_costumes[scriptNum].roomNum, rOffsets[_costumes[scriptNum].roomNum] + offset);
            }
            return reader;
        }

        Dictionary<byte, Charset> _charsets = new Dictionary<byte, Charset>();
        public Charset GetCharset(byte id)
        {
            Charset charset = null;
            if (_charsets.ContainsKey(id) == false)
            {
                var disk = OpenCharset(id);
                if (disk != null)
                {
                    charset = disk.ReadCharset();
                    _charsets.Add(id, charset);
                }
            }
            else { charset = _charsets[id]; }
            return charset;
        }

        public Room GetRoom(byte roomNum)
        {
            Room room = null;
            if (_roomsData.ContainsKey(roomNum) == false)
            {
                var disk = OpenRoom(roomNum);
                if (disk != null)
                {
                    var rOffsets = disk.ReadRoomOffsets();
                    room = disk.ReadRoom(rOffsets[roomNum]);
                    room.Name = _roomNames[roomNum];
                    _roomsData.Add(roomNum, room);
                }
            }
            else
            {
                room = _roomsData[roomNum];
            }

            return room;
        }

        #region Private Methods
        private DiskFile OpenCharset(byte id)
        {
            var diskName = string.Format("{0}.lfl", 900 + id);
            var game1Path = System.IO.Path.Combine(m_directory, diskName);
            DiskFile file = new DiskFile(game1Path, 0x0);
            return file;
        }

        private DiskFile OpenRoom(byte roomNum)
        {
            var diskNum = _rooms[roomNum].roomNum;
            var diskName = string.Format("disk{0:00}.lec", diskNum);
            var game1Path = System.IO.Path.Combine(m_directory, diskName);

            DiskFile file = diskNum != 0 ? new DiskFile(game1Path, 0x69) : null;
            return file;
        }

        private static Resource[] ReadResTypeList(XorReader br)
        {
            var numEntries = br.ReadInt16();
            Resource[] res = new Resource[numEntries];
            for (int i = 0; i < numEntries; i++)
            {
                var roomNum = br.ReadByte();
                var offset = br.ReadInt32();
                res[i] = new Resource { roomNum = roomNum, offset = offset };
            }
            return res;
        }

        private void ReadDirectoryOfObjects(XorReader br)
        {
            var numEntries = br.ReadInt16();
            this.ObjectOwnerTable = new byte[numEntries];
            this.ObjectStateTable = new byte[numEntries];
            this.ClassData = new uint[numEntries];
            uint bits;
            for (int i = 0; i < numEntries; i++)
            {
                bits = br.ReadByte();
                bits |= (uint)(br.ReadByte() << 8);
                bits |= (uint)(br.ReadByte() << 16);
                this.ClassData[i] = bits;
                var tmp = br.ReadByte();
                ObjectStateTable[i] = (byte)(tmp >> 4);
                ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
            }
        }
        #endregion

        public byte[] ObjectOwnerTable { get; private set; }
        public byte[] ObjectStateTable { get; private set; }
        public uint[] ClassData { get; private set; }
    }
}
