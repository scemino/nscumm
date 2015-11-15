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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    class ResourceIndex3 : ResourceIndex3_16
    {
        protected override void LoadIndex(GameInfo game)
        {
            var encByte = GetEncodingByte(game);
            Directory = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            using (var file = ServiceLocator.FileStorage.OpenFileRead(game.Path))
            {
                var br = new BinaryReader(new XorStream(file, encByte));
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    br.ReadUInt32();
                    var block = br.ReadInt16();
                    switch (block)
                    {
                        case 0x4E52:
                            ReadRoomNames(br);
                            break;
                        case 0x5230:    // 'R0'
                            var rooms = ReadResTypeList(br);
                            RoomResources = new ReadOnlyCollection<Resource>(rooms);
                            break;

                        case 0x5330:    // 'S0'
                            var scripts = ReadResTypeList(br);
                            ScriptResources = new ReadOnlyCollection<Resource>(scripts);
                            break;

                        case 0x4E30:    // 'N0'
                            var sounds = ReadResTypeList(br);
                            SoundResources = new ReadOnlyCollection<Resource>(sounds);
                            break;

                        case 0x4330:    // 'C0'
                            var costumes = ReadResTypeList(br);
                            CostumeResources = new ReadOnlyCollection<Resource>(costumes);
                            break;

                        case 0x4F30:    // 'O0'
                            ReadDirectoryOfObjects(br);

                            // Indy3 FM-TOWNS has 32 extra bytes of unknown meaning
                            if (Game.GameId == GameId.Indy3 && Game.Platform == Platform.FMTowns)
                                br.BaseStream.Seek(32, SeekOrigin.Current);
                            break;
                        default:
                            Debug.WriteLine("Unknown block {0:X2}", block);
                            break;
                    }
                }
            }
        }

        #region Protected/Private Methods

        protected void ReadRoomNames(BinaryReader reader)
        {
            var roomNames = new Dictionary<byte, string>();
            for (byte room; (room = reader.ReadByte()) != 0;)
            {
                var dataName = reader.ReadBytes(9);
                var name = new StringBuilder();
                for (int i = 0; i < 9; i++)
                {
                    var b = dataName[i] ^ 0xFF;
                    if (b == 0)
                        continue;
                    name.Append((char)b);
                }
                roomNames[room] = name.ToString();
            }
            RoomNames = roomNames;
        }

        protected virtual byte GetEncodingByte(GameInfo game)
        {
            byte encByte = 0;
            if (!game.Features.HasFlag(GameFeatures.Old256))
            {
                encByte = 0xFF;
            }
            return encByte;
        }

        protected override Resource[] ReadResTypeList(BinaryReader br)
        {
            var numEntries = br.ReadUInt16();
            var res = new Resource[numEntries];
            for (int i = 0; i < numEntries; i++)
            {
                var roomNum = br.ReadByte();
                var offset = br.ReadUInt32();
                res[i] = new Resource { RoomNum = roomNum, Offset = offset };
            }
            return res;
        }

        #endregion

    }
}
