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

using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using NScumm.Core;

namespace NScumm.Scumm.IO
{
    class ResourceIndex5: ResourceIndex4
    {
        int numInventory;
        int numVariables;
        int numBitVariables;
        int numLocalObjects;

        public ReadOnlyCollection<Resource> CharsetResources
        {
            get;
            protected set;
        }

        public override int NumInventory { get { return numInventory; } }

        public override int NumVariables { get { return numVariables; } }

        public override int NumBitVariables { get { return numBitVariables; } }

        public override int NumLocalObjects { get { return numLocalObjects; } }


        protected override Resource[] ReadResTypeList(BinaryReader br)
        {
            var numEntries = br.ReadUInt16();
            var res = new Resource[numEntries];
            var roomNumbers = br.ReadBytes(numEntries);
            for (int i = 0; i < numEntries; i++)
            {
                res[i] = new Resource { RoomNum = roomNumbers[i], Offset = br.ReadUInt32() };
            }
            return res;
        }

        static string ToTag(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        protected override void LoadIndex(GameInfo game)
        {
            const byte encByte = 0x69;
            Directory = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            using (var file = ServiceLocator.FileStorage.OpenFileRead(game.Path))
            {
                var br = new BinaryReader(new XorStream(file,encByte));
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var block = ToTag(br.ReadBytes(4));
                    br.ReadUInt32BigEndian(); // size
                    switch (block)
                    {
                        case "RNAM":
                            ReadRoomNames(br);
                            break;

                        case "MAXS":
                            ReadMaxSizes(br);
                            break;

                        case "DROO":
                            var rooms = ReadResTypeList(br);
                            RoomResources = new ReadOnlyCollection<Resource>(rooms);
                            break;

                        case "DSCR":
                            var scripts = ReadResTypeList(br);
                            ScriptResources = new ReadOnlyCollection<Resource>(scripts);
                            break;

                        case "DSOU":
                            var sounds = ReadResTypeList(br);
                            SoundResources = new ReadOnlyCollection<Resource>(sounds);
                            break;

                        case "DCOS":
                            var costumes = ReadResTypeList(br);
                            CostumeResources = new ReadOnlyCollection<Resource>(costumes);
                            break;

                        case "DCHR":
                            var charset = ReadResTypeList(br);
                            CharsetResources = new ReadOnlyCollection<Resource>(charset);
                            break;

                        case "DOBJ":
                            ReadDirectoryOfObjects(br);
                            break;
                        default:
//                            Console.Error.WriteLine("Unknown block {0}", block);
                            break;
                    }
                }
            }
        }

        protected override void ReadDirectoryOfObjects(BinaryReader br)
        {
            var numEntries = br.ReadUInt16();
            ObjectOwnerTable = new byte[numEntries];
            ObjectStateTable = new byte[numEntries];
            for (int i = 0; i < ObjectOwnerTable.Length; i++)
            {
                var tmp = br.ReadByte();
                ObjectStateTable[i] = (byte)(tmp >> 4);
                ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
            }
            ClassData = br.ReadUInt32s(numEntries);
        }

        void ReadMaxSizes(BinaryReader reader)
        {
            numVariables = reader.ReadUInt16();      // 800
            reader.ReadUInt16();                      // 16
            numBitVariables = reader.ReadUInt16();   // 2048
            numLocalObjects = reader.ReadUInt16();   // 200

            reader.ReadUInt16();                      // 50
            var numCharsets = reader.ReadUInt16();       // 9
            reader.ReadUInt16();                      // 100
            reader.ReadUInt16();                      // 50
            numInventory = reader.ReadUInt16();      // 80
        }
    }
}
