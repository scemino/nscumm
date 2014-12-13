//
//  ResourceIndex6.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System;

namespace NScumm.Core
{
    public class ArrayDefinition
    {
        public int Index{ get; set; }

        public int Type{ get; set; }

        public int Dim1{ get; set; }

        public int Dim2{ get; set; }
    }

    class ResourceIndex6 : ResourceIndex5
    {
        int numVerbs;
        int numInventory;
        int numVariables;
        int numBitVariables;
        int numLocalObjects;

        public override int NumVerbs { get { return numVerbs; } }

        public override int NumInventory { get { return numInventory; } }

        public override int NumVariables { get { return numVariables; } }

        public override int NumBitVariables { get { return numBitVariables; } }

        public override int NumLocalObjects { get { return numLocalObjects; } }

        #region implemented abstract members of ResourceIndex

        protected override void LoadIndex(GameInfo game)
        {
            Directory = Path.GetDirectoryName(game.Path);
            using (var file = File.Open(game.Path, FileMode.Open))
            {
                var br1 = new BinaryReader(file);
                var br = new XorReader(br1, 0x69);
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var tag = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4));
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

                        default:
                            Console.Error.WriteLine("Unknown tag {0} found in index file directory", tag);
                            break;
                    }
                }
            }
        }

        #endregion

        void ReadMaxSizes(XorReader reader)
        {
            numVariables = reader.ReadUInt16();      // 800
            reader.ReadUInt16();                      // 16
            numBitVariables = reader.ReadUInt16();   // 2048
            numLocalObjects = reader.ReadUInt16();   // 200
            var numArray = reader.ReadUInt16();                      // 50
            reader.ReadUInt16();
            numVerbs = reader.ReadUInt16();                      // 100
            var numFlObject = reader.ReadUInt16();                      // 50
            numInventory = reader.ReadUInt16();      // 80
            var numRooms = reader.ReadUInt16();
            var numScripts = reader.ReadUInt16();
            var numSounds = reader.ReadUInt16();
            var numCharsets = reader.ReadUInt16();
            var numCostumes = reader.ReadUInt16();
            var numGlobalObjects = reader.ReadUInt16();
        }

        void ReadArrayFromIndexFile(XorReader br)
        {
            int num;
            while ((num = br.ReadUInt16()) != 0)
            {
                var a = br.ReadUInt16();
                var b = br.ReadUInt16();
                var c = br.ReadUInt16();
                ArrayDefinitions.Add(new ArrayDefinition{ Index = num, Type = c, Dim2 = a, Dim1 = b });
            }
        }
    }
}

