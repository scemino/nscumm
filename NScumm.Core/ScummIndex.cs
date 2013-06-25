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

using NScumm.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NScumm.Core
{
    public class ScummIndex
    {
        #region Resource Structure
        private struct Resource
        {
            public byte RoomNum;
            public int Offset;
        }
        #endregion

        #region Fields
        Resource[] _rooms;
        Resource[] _scripts;
        Resource[] _costumes;
        Dictionary<byte, string> _roomNames;
        string _directory;
        #endregion

        #region Properties
        public string Directory
        {
            get { return _directory; }
        }

        public byte[] ObjectOwnerTable { get; private set; }
        public byte[] ObjectStateTable { get; private set; }
        public uint[] ClassData { get; private set; }
        #endregion

        #region Public Methods
        public void LoadIndex(string path)
        {
            _roomNames = new Dictionary<byte, string>();
            _directory = Path.GetDirectoryName(path);
            using (var file = File.Open(path, FileMode.Open))
            {
                var br1 = new BinaryReader(file);
                var br = new XorReader(br1, 0);
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    br.ReadInt32();
                    var block = br.ReadInt16();
                    switch (block)
                    {
                        case 0x4E52:
                            for (byte room; (room = br.ReadByte()) != 0; )
                            {
                                var dataName = br.ReadBytes(9);
                                var name = new StringBuilder();
                                for (int i = 0; i < 9; i++)
                                {
                                    var b = dataName[i] ^ 0xFF;
                                    name.Append((char)b);
                                }
                                _roomNames.Add(room, name.ToString());
                            }
                            break;
                        case 0x5230:	// 'R0'
                            _rooms = ReadResTypeList(br);
                            break;

                        case 0x5330:	// 'S0'
                            _scripts = ReadResTypeList(br);
                            break;

                        case 0x4E30:	// 'N0'
                            ReadResTypeList(br);
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

        public int GetGlobalScriptOffset(byte scriptNum)
        {
            return 6;
        }

        public byte[] GetScript(byte scriptNum)
        {
            byte[] data = null;
            var offset = _scripts[scriptNum].Offset;
            var disk = OpenRoom(_scripts[scriptNum].RoomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                data = disk.ReadScript(rOffsets[_scripts[scriptNum].RoomNum] + offset);
            }
            return data;
        }

        public XorReader GetCostumeReader(byte scriptNum)
        {
            XorReader reader = null;
            var disk = OpenRoom(_costumes[scriptNum].RoomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                var offset = _costumes[scriptNum].Offset;
                reader = disk.ReadCostume(rOffsets[_costumes[scriptNum].RoomNum] + offset);
            }
            return reader;
        }

        public byte[] GetCharsetData(byte id)
        {
            byte[] charset = null;
            var disk = OpenCharset(id);
            if (disk != null)
            {
                charset = disk.ReadCharsetData();
            }
            return charset;
        }

        public Room GetRoom(byte roomNum)
        {
            Room room = null;
            var disk = OpenRoom(roomNum);
            if (disk != null)
            {
                var rOffsets = disk.ReadRoomOffsets();
                room = disk.ReadRoom(rOffsets[roomNum]);
                room.Name = _roomNames[roomNum];
            }

            return room;
        }
        #endregion

        #region Private Methods
        DiskFile OpenCharset(byte id)
        {
            var diskName = string.Format("{0}.lfl", 900 + id);
            var game1Path = Path.Combine(_directory, diskName);
            var file = new DiskFile(game1Path, 0x0);
            return file;
        }

        DiskFile OpenRoom(byte roomNum)
        {
            var diskNum = _rooms[roomNum].RoomNum;
            var diskName = string.Format("disk{0:00}.lec", diskNum);
            var game1Path = Path.Combine(_directory, diskName);

            DiskFile file = diskNum != 0 ? new DiskFile(game1Path, 0x69) : null;
            return file;
        }

        static Resource[] ReadResTypeList(XorReader br)
        {
            var numEntries = br.ReadInt16();
            var res = new Resource[numEntries];
            for (int i = 0; i < numEntries; i++)
            {
                var roomNum = br.ReadByte();
                var offset = br.ReadInt32();
                res[i] = new Resource { RoomNum = roomNum, Offset = offset };
            }
            return res;
        }

        void ReadDirectoryOfObjects(XorReader br)
        {
            var numEntries = br.ReadInt16();
            ObjectOwnerTable = new byte[numEntries];
            ObjectStateTable = new byte[numEntries];
            ClassData = new uint[numEntries];
            uint bits;
            for (int i = 0; i < numEntries; i++)
            {
                bits = br.ReadByte();
                bits |= (uint)(br.ReadByte() << 8);
                bits |= (uint)(br.ReadByte() << 16);
                ClassData[i] = bits;
                var tmp = br.ReadByte();
                ObjectStateTable[i] = (byte)(tmp >> 4);
                ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
            }
        }
        #endregion
    }
}
