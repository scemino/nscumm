//
//  ResourceIndex7.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core.IO;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace NScumm.Core
{
    class ResourceIndex8 : ResourceIndex7
    {
        public Dictionary<string,int> ObjectIDMap { get; private set; }

        public ReadOnlyCollection<Resource> RoomScriptResources { get; private set; }

        public ResourceIndex8()
        {
            AudioNames = new string[0];
        }

        protected override void LoadIndex(GameInfo game)
        {
            Directory = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            using (var file = ServiceLocator.FileStorage.OpenFileRead(game.Path))
            {
                var br = new BinaryReader(file);
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var tag = System.Text.Encoding.UTF8.GetString(br.ReadBytes(4));
                    br.ReadUInt32BigEndian();

                    switch (tag)
                    {
                        case "DCHR":
                        case "DIRF":
                            var charset = ReadResTypeList(br);
                            CharsetResources = new ReadOnlyCollection<Resource>(charset);
                            break;

                        case "DOBJ":
                            ReadDirectoryOfObjects(br);
                            break;

                        case "RNAM":
                            ReadRoomNames(br);
                            break;

                        case "DROO":
                        case "DIRR":
                            var rooms = ReadResTypeList(br);
                            RoomResources = new ReadOnlyCollection<Resource>(rooms);
                            break;

                        case "DSCR":
                        case "DIRS":
                            var scripts = ReadResTypeList(br);
                            ScriptResources = new ReadOnlyCollection<Resource>(scripts);
                            break;

                        case "DRSC":
                            var roomScripts = ReadResTypeList(br);
                            RoomScriptResources = new ReadOnlyCollection<Resource>(roomScripts);
                            break;

                        case "DCOS":
                        case "DIRC":
                            var costumes = ReadResTypeList(br);
                            CostumeResources = new ReadOnlyCollection<Resource>(costumes);
                            break;

                        case "MAXS":
                            ReadMaxSizes(br);
                            break;

                        case "DIRN":
                        case "DSOU":
                            var sounds = ReadResTypeList(br);
                            SoundResources = new ReadOnlyCollection<Resource>(sounds);
                            break;

                        case "AARY":
                            ReadArrayFromIndexFile(br);
                            break;

                        case "ANAM":        // Used by: The Dig, FT
                            {
                                var num = br.ReadUInt16();
                                AudioNames = new string[num];
                                for (int i = 0; i < num; i++)
                                {
                                    AudioNames[i] = System.Text.Encoding.UTF8.GetString(br.ReadBytes(9));
                                }
                            }
                            break;

//                        default:
//                            Console.Error.WriteLine("Unknown tag {0} found in index file directory", tag);
//                            break;
                    }
                }
            }
        }

        protected override void ReadMaxSizes(BinaryReader reader)
        {
            reader.BaseStream.Seek(50, SeekOrigin.Current);  // Skip over SCUMM engine version
            reader.BaseStream.Seek(50, SeekOrigin.Current);  // Skip over data file version
            numVariables = reader.ReadInt32();
            numBitVariables = reader.ReadInt32();
            reader.ReadInt32();
            var numScripts = reader.ReadInt32();
            var numSounds = reader.ReadInt32();
            var numCharsets = reader.ReadInt32();
            var numCostumes = reader.ReadInt32();
            var numRooms = reader.ReadInt32();
            reader.ReadInt32();
            var numGlobalObjects = reader.ReadInt32();
            reader.ReadInt32();
            numLocalObjects = reader.ReadInt32();
            var numNewNames = reader.ReadInt32();
            var numFlObject = reader.ReadInt32();
            numInventory = reader.ReadInt32();
            numArray = reader.ReadInt32();
            numVerbs = reader.ReadInt32();

            numGlobalScripts = 2000;
        }

        protected override void ReadDirectoryOfObjects(BinaryReader br)
        {
            var num = br.ReadInt32();

            ObjectIDMap = new Dictionary<string, int>();
            ObjectStateTable = new byte[num];
            objectRoomTable = new byte[num];
            ClassData = new uint[num];
            ObjectOwnerTable = new byte[num];
            for (var i = 0; i < num; i++)
            {
                // Add to object name-to-id map
                var name = DataToString(br.ReadBytes(40));
                ObjectIDMap[name] = i;

                ObjectStateTable[i] = br.ReadByte();
                ObjectRoomTable[i] = br.ReadByte();
                ClassData[i] = br.ReadUInt32();
                ObjectOwnerTable[i] = 0xFF;
            }
        }

        public static string DataToString(byte[] data)
        {
            var sb = new List<byte>();
            int i = 0;
            while (i < data.Length && data[i] != 0)
            {
                sb.Add(data[i]);
                i++;
            }
            return System.Text.Encoding.UTF8.GetString(sb.ToArray());
        }

        protected override Resource[] ReadResTypeList(BinaryReader br)
        {
            var numEntries = br.ReadInt32();
            var res = new Resource[numEntries];
            var roomNumbers = br.ReadBytes(numEntries);
            for (int i = 0; i < numEntries; i++)
            {
                res[i] = new Resource { RoomNum = roomNumbers[i], Offset = br.ReadUInt32() };
            }
            return res;
        }

        protected override void ReadArrayFromIndexFile(BinaryReader br)
        {
            uint num;
            while ((num = br.ReadUInt32()) != 0)
            {
                var a = br.ReadInt32();
                var b = br.ReadInt32();

                if (b != 0)
                {
                    ArrayDefinitions.Add(new ArrayDefinition{ Index = num, Type = (int)ArrayType.IntArray, Dim2 = b, Dim1 = a });
                }
                else
                {
                    ArrayDefinitions.Add(new ArrayDefinition{ Index = num, Type = (int)ArrayType.IntArray, Dim2 = a, Dim1 = b });
                }
            }
        }
    }
}
