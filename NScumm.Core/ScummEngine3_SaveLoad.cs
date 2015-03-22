//
//  ScummEngine3_SaveLoad.cs
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
using System.IO;
using System.Text;
using NScumm.Core.IO;

namespace NScumm.Core
{
    partial class ScummEngine3
    {
        string _saveLoadVarsFilename;

        void SaveLoad()
        {
            if (_saveLoadFlag != 0)
            {
                if (_savegame == null)
                {
                    var dir = Path.GetDirectoryName(Game.Path);
                    _savegame = Path.Combine(dir, string.Format("{0}_{1}{2}.sav", Game.Id, _saveTemporaryState ? 'c' : 's', (_saveLoadSlot + 1)));
                }
                if (_saveLoadFlag == 2)
                {
                    if (File.Exists(_savegame))
                    {
                        LoadState(_savegame);
                        if (_saveTemporaryState && Game.Version <= 7)
                        {
                            Variables[VariableGameLoaded.Value] = (Game.Version == 8) ? 1 : 203;
                        }
                    }
                }
                else if (_saveLoadFlag == 1)
                {
                    SaveState(_savegame, Path.GetFileNameWithoutExtension(_savegame));
                    if (_saveTemporaryState)
                    {
                        Variables[VariableGameLoaded.Value] = 201;
                    }
                }

                // update IQ points after loading
                if (_saveLoadFlag == 2)
                {
                    if (Game.GameId == GameId.Indy4)
                        RunScript(145, false, false, new int[0]);
                }

                _saveLoadFlag = 0;
            }
        }

        void SaveLoadVars()
        {
            if (ReadByte() == 1)
            {
                SaveVars();
            }
            else
            {
                LoadVars();
            }
        }

        protected void LoadVars()
        {
            int a, b;

            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01: // read a range of variables
                        GetResult();
                        a = _resultVarIndex;
                        GetResult();
                        b = _resultVarIndex;
                        //debug(0, "stub loadVars: vars %d -> %d", a, b);
                        break;
                    case 0x02: // read a range of string variables
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);

                        int slot;
                        int savegameId;

                        if (a == StringIdIqSeries && b == StringIdIqSeries)
                        {
                            // Zak256 loads the IQ script-slot but does not use it -> ignore it
                            if (Game.GameId == GameId.Indy3)
                            {
                                var ptr = _strings[StringIdIqSeries];
                                if (ptr != null)
                                {
                                    LoadIqPoints(ptr);
                                }
                            }
                            break;
                        }

                        var avail_saves = ListSavegames(100);
                        for (slot = a; slot <= b; ++slot)
                        {
                            var slotContent = _strings[slot];

                            // load savegame names
                            savegameId = slot - a + 1;
                            string name;
                            if (avail_saves[savegameId] && GetSavegameName(savegameId, out name))
                            {
                                int pos;
                                var ptr = name;
                                // slotContent ends with {'\0','@'} -> max. length = slotSize-2
                                for (pos = 0; pos < name.Length - 2; ++pos)
                                {
                                    if (ptr[pos] == 0)
                                        break;
                                    // replace special characters
                                    if (ptr[pos] >= 32 && ptr[pos] <= 122 && ptr[pos] != 64)
                                        slotContent[pos] = (byte)ptr[pos];
                                    else
                                        slotContent[pos] = (byte)'_';
                                }
                                slotContent[pos] = 0;
                            }
                            else
                            {
                                slotContent[0] = 0;
                            }
                        }
                        break;
                    case 0x03: // open file
                        _saveLoadVarsFilename = ReadString();
                        break;
                    case 0x04:
                        return;
                    case 0x1F: // close file
                        _saveLoadVarsFilename = null;
                        return;
                }
            }
        }

        string ReadString()
        {
            var sb = new StringBuilder();
            var chr = (char)_currentScriptData[CurrentPos++];
            while (chr != 0)
            {
                sb.Append(chr);
                chr = (char)_currentScriptData[CurrentPos++];
            }
            return sb.ToString();
        }

        protected void SaveVars()
        {
            int a, b;

            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01: // write a range of variables
                        GetResult();
                        a = _resultVarIndex;
                        GetResult();
                        b = _resultVarIndex;
                        //debug(0, "stub saveVars: vars %d -> %d", a, b);
                        break;
                    case 0x02: // write a range of string variables
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);

                        if (a == StringIdIqEpisode && b == StringIdIqEpisode)
                        {
                            if (Game.GameId == GameId.Indy3)
                            {
                                SaveIQPoints();
                            }
                            break;
                        }
                        // FIXME: changing savegame-names not supported
                        break;
                    case 0x03: // open file
                        _saveLoadVarsFilename = ReadString();
                        break;
                    case 0x04:
                        return;
                    case 0x1F: // close file
                        _saveLoadVarsFilename = null;
                        return;
                }
            }
        }

        void LoadIqPoints(byte[] ptr)
        {
            // load Indy3 IQ-points
            var filename = GetIqPointsFilename();
            if (File.Exists(filename))
            {
                using (var file = File.OpenRead(filename))
                {
                    file.Read(ptr, 0, ptr.Length);
                }
            }
        }

        void SaveIQPoints()
        {
            var filename = GetIqPointsFilename();
            using (var file = File.OpenWrite(filename))
            {
                var data = _strings[StringIdIqEpisode];
                file.Write(data, 0, data.Length);
            }
        }

        string GetIqPointsFilename()
        {
            var filename = Path.Combine(Path.GetDirectoryName(Game.Path), Game.Id + ".iq");
            return filename;
        }

        void SaveLoadGame()
        {
            GetResult();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var result = 0;

            var slot = a & 0x1F;
            // Slot numbers in older games start with 0, in newer games with 1
            if (Game.Version <= 2)
                slot++;
            _opCode = (byte)(a & 0xE0);

            switch (_opCode)
            {
                case 0x00: // num slots available
                    result = 100;
                    break;
                case 0x20: // drive
                    if (Game.Version <= 3)
                    {
                        // 0 = ???
                        // [1,2] = disk drive [A:,B:]
                        // 3 = hard drive
                        result = 3;
                    }
                    else
                    {
                        // set current drive
                        result = 1;
                    }
                    break;
                case 0x40: // load
                    if (LoadState(slot, false))
                        result = 3; // sucess
                                        else
                        result = 5; // failed to load
                    break;
                case 0x80: // save
                    if (Game.Version <= 3)
                    {
                        string name;
                        if (Game.Version <= 2)
                        {
                            // use generic name
                            name = string.Format("Game {0}", (char)('A' + slot - 1));
                        }
                        else
                        {
                            // use name entered by the user
                            var firstSlot = StringIdSavename1;
                            name = Encoding.ASCII.GetString(_strings[slot + firstSlot - 1]);
                        }

                        if (SavePreparedSavegame(slot, name))
                            result = 0;
                        else
                            result = 2;
                    }
                    else
                    {
                        result = 2; // failed to save
                    }
                    break;
                case 0xC0: // test if save exists
                    {
                        var availSaves = ListSavegames(100);
                        var filename = MakeSavegameName(slot, false);
                        var directory = Path.GetDirectoryName(Game.Path);
                        if (availSaves[slot] && (File.Exists(Path.Combine(directory, filename))))
                        {
                            result = 6; // save file exists
                        }
                        else
                        {
                            result = 7; // save file does not exist
                        }
                    }
                    break;
            //                default:
            //                    error("o4_saveLoadGame: unknown subopcode %d", _opcode);
            }

            SetResult(result);
        }
    }
}

