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

namespace NScumm.Core
{
    partial class ScummEngine3
    {
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
                    if (Game.GameId == NScumm.Core.IO.GameId.Indy4)
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

