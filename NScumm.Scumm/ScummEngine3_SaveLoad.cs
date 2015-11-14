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

using System.Text;
using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
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
                    var dir = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
                    _savegame = ServiceLocator.FileStorage.Combine(dir, string.Format("{0}_{1}{2}.sav", Game.Id, _saveTemporaryState ? 'c' : 's', (_saveLoadSlot + 1)));
                }
                if (_saveLoadFlag == 2)
                {
                    if (ServiceLocator.FileStorage.FileExists(_savegame))
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
                    SaveState(_savegame, ServiceLocator.FileStorage.GetFileNameWithoutExtension(_savegame));
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
            if (ServiceLocator.FileStorage.FileExists(filename))
            {
                using (var file = ServiceLocator.FileStorage.OpenFileRead(filename))
                {
                    file.Read(ptr, 0, ptr.Length);
                }
            }
        }

        void SaveIQPoints()
        {
            var filename = GetIqPointsFilename();
            using (var file = ServiceLocator.FileStorage.OpenFileWrite(filename))
            {
                var data = _strings[StringIdIqEpisode];
                file.Write(data, 0, data.Length);
            }
        }

        string GetIqPointsFilename()
        {
            var filename = ServiceLocator.FileStorage.Combine(ServiceLocator.FileStorage.GetDirectoryName(Game.Path), Game.Id + ".iq");
            return filename;
        }


    }
}

