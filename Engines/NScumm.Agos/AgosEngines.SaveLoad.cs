//
//  AgosEngine.SaveLoad.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using System;
using System.IO;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Input;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        // FIXME: This code counts savegames, but callers in many cases assume
        // that the return value + 1 indicates an empty slot.
        protected int CountSaveGames()
        {
            bool[] marks = new bool[256];

            // Get the name of (possibly non-existent) savegame slot 998, and replace
            // the extension by * to get a pattern.
            string tmp = GenSaveName(998);
            System.Diagnostics.Debug.Assert(tmp.Length >= 4 && tmp[tmp.Length - 4] == '.');
            var prefix = tmp.Substring(tmp.Length - 3) + "*";

            var filenames = OSystem.SaveFileManager.ListSavefiles(prefix);

            foreach (var file in filenames)
            {
                //Obtain the last 3 digits of the filename, since they correspond to the save slot
                System.Diagnostics.Debug.Assert(file.Length >= 4);
                var slotNum = int.Parse(file.Substring(file.Length - 3));
                if (slotNum >= 0 && slotNum < 256)
                    marks[slotNum] = true; //mark this slot as valid
            }

            // locate first empty slot
            int numSaveGames = 1;
            for (var s = 1; s < 256; s++)
            {
                if (marks[s])
                    numSaveGames++;
            }

            return numSaveGames;
        }

        protected virtual void UserGame(bool load)
        {
            var window = _windowArray[4];
            int i = 0, numSaveGames;
            Array.Clear(SaveBuf, 0, SaveBuf.Length);

            numSaveGames = CountSaveGames();

            uint saveTime = GetTime();
            HaltAnimation();

            restart:
            PrintScroll();
            window.textColumn = 0;
            window.textRow = 0;
            window.textColumnOffset = 0;
            window.textLength = 0; // Difference

            string message1;
            switch (_language)
            {
                case Language.FR_FRA:
                    message1 = "\rIns/rez disquette de\rsauvegarde de jeux &\rentrez nom de fichier:\r\r   ";
                    break;
                case Language.DE_DEU:
                    message1 = "\rLege Spielstandsdiskette ein. Dateinamen eingeben:\r\r   ";
                    break;
                default:
                    message1 = "\r Insert savegame data disk & enter filename:\r\r   ";
                    break;
            }

            foreach (var c in message1)
                WindowPutChar(window, (byte) c);

            Array.Clear(SaveBuf, 0, 10);
            var name = SaveBuf;
            _saveGameNameLen = 0;

            while (!HasToQuit)
            {
                WindowPutChar(window, 128);
                _keyPressed = new ScummInputState();

                while (!HasToQuit)
                {
                    Delay(10);
                    var key = ToChar(_keyPressed.GetKeys().FirstOrDefault());
                    if (key != 0 && key < 128)
                    {
                        i = key;
                        break;
                    }
                }

                UserGameBackSpace(_windowArray[4], 8);
                if (i == 10 || i == 13)
                {
                    break;
                }
                else if (i == 8)
                {
                    // do_backspace
                    if (_saveGameNameLen != 0)
                    {
                        _saveGameNameLen--;
                        name[_saveGameNameLen] = 0;
                        UserGameBackSpace(_windowArray[4], 8);
                    }
                }
                else if (i >= 32 && _saveGameNameLen != 8)
                {
                    name[_saveGameNameLen++] = (byte) i;
                    WindowPutChar(_windowArray[4], (byte) i);
                }
            }

            if (_saveGameNameLen != 0)
            {
                short slot = MatchSaveGame(name.GetRawText(), (ushort) numSaveGames);
                if (!load)
                {
                    if (slot >= 0 && !ConfirmOverWrite(window))
                        goto restart;

                    if (slot < 0)
                        slot = (short) numSaveGames;

                    if (!SaveGame(slot, name.GetRawText()))
                        FileError(_windowArray[4], true);
                }
                else
                {
                    if (slot < 0)
                    {
                        FileError(_windowArray[4], false);
                    }
                    else
                    {
                        if (!LoadGame(GenSaveName(slot)))
                            FileError(_windowArray[4], false);
                    }
                }

                PrintStats();
            }

            RestartAnimation();
            _gameStoppedClock = GetTime() - saveTime + _gameStoppedClock;
        }

        protected void DisableFileBoxes()
        {
            if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
            {
                for (var i = 208; i != 214; i++)
                    DisableBox(i);
            }
            else
            {
                for (var i = 200; i != 224; i++)
                    DisableBox(i);
            }
        }

        protected void UserGameBackSpace(WindowBlock window, int x, byte b = 0)
        {
            WindowPutChar(window, (byte) x, b);
            byte oldTextColor = window.textColor;
            window.textColor = window.fillColor;

            if (_language == Language.HE_ISR)
            {
                x = 128;
            }
            else
            {
                x += 120;
                if (x != 128)
                    x = 129;
            }

            WindowPutChar(window, (byte) x);

            window.textColor = oldTextColor;
            WindowPutChar(window, 8);
        }

        protected void FileError(WindowBlock window, bool saveError)
        {
            string message1, message2;

            if (saveError)
            {
                switch (_language)
                {
                    case Language.RU_RUS:
                        if (GameType == SIMONGameType.GType_SIMON2)
                        {
                            message1 = "\r   Mf sowrap+fts+.";
                            message2 = "\r  Nzjb#a ejs#a.";
                        }
                        else
                        {
                            message1 = "\r   Mf sowrap]fts].";
                            message2 = "\r   Nzjb_a ejs_a.";
                        }
                        break;
                    case Language.PL_POL:
                        message1 = "\r      Blad zapisu.    ";
                        message2 = "\rBlad dysku.                       ";
                        break;
                    case Language.ES_ESP:
                        message1 = "\r     Error al salvar";
                        message2 = "\r  Intenta con otro disco";
                        break;
                    case Language.IT_ITA:
                        message1 = "\r  Salvataggio non riuscito";
                        message2 = "\r    Prova un\x27altro disco";
                        break;
                    case Language.FR_FRA:
                        message1 = "\r    Echec sauvegarde";
                        message2 = "\rEssayez une autre disquette";
                        break;
                    case Language.DE_DEU:
                        message1 = "\r  Sicherung erfolglos.";
                        message2 = "\rVersuche eine andere     Diskette.";
                        break;
                    default:
                        message1 = "\r       Save failed.";
                        message2 = "\r       Disk error.";
                        break;
                }
            }
            else
            {
                switch (_language)
                {
                    case Language.RU_RUS:
                        if (GameType == SIMONGameType.GType_SIMON2)
                        {
                            message1 = "\r  Mf ^adruhafts+.";
                            message2 = "\r   Takm pf pakefp.";
                        }
                        else
                        {
                            message1 = "\r   Mf ^adruhafts].";
                            message2 = "\r   Takm pf pakefp.";
                        }
                        break;
                    case Language.PL_POL:
                        message1 = "\r   Blad odczytu.    ";
                        message2 = "\r  Nie znaleziono pliku.";
                        break;
                    case Language.ES_ESP:
                        message1 = "\r     Error al cargar";
                        message2 = "\r  Archivo no encontrado";
                        break;
                    case Language.IT_ITA:
                        message1 = "\r  Caricamento non riuscito";
                        message2 = "\r      File non trovato";
                        break;
                    case Language.FR_FRA:
                        message1 = "\r    Echec chargement";
                        message2 = "\r  Fichier introuvable";
                        break;
                    case Language.DE_DEU:
                        message1 = "\r    Laden erfolglos.";
                        message2 = "\r  Datei nicht gefunden.";
                        break;
                    default:
                        message1 = "\r       Load failed.";
                        message2 = "\r     File not found.";
                        break;
                }
            }

            if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                PrintScroll();
                window.textColumn = 0;
                window.textRow = 0;
                window.textColumnOffset = 0;
                window.textLength = 0; // Difference
            }
            else
            {
                WindowPutChar(window, 12);
            }

            foreach (var c in message1)
                WindowPutChar(window, (byte) c);
            foreach (var c in message2)
                WindowPutChar(window, (byte) c);

            WaitWindow(window);
        }

        protected ushort ReadItemID(BinaryReader f)
        {
            uint val = f.ReadUInt32BigEndian();
            if (val == 0xFFFFFFFF)
                return 0;
            return (ushort) (val + 1);
        }

        protected void WriteItemID(BinaryWriter f, ushort val)
        {
            if (val == 0)
                f.WriteUInt32BigEndian(0xFFFFFFFF);
            else
                f.WriteUInt32BigEndian((uint) (val - 1));
        }

        protected virtual string GenSaveName(int slot)
        {
            return $"pn.{slot:D3}";
        }

        protected virtual bool LoadGame(string filename, bool restartMode = false)
        {
            Stream f;
            uint num, item_index, i;

            _videoLockOut |= 0x100;

            if (restartMode)
            {
                // Load restart state
                var file = OpenFileRead(filename);
                f = file;
            }
            else
            {
                f = OSystem.SaveFileManager.OpenForLoading(filename);
            }

            if (f == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            var br = new BinaryReader(f);
            byte[] ident = new byte[100];
            if (!restartMode)
            {
                f.Read(ident, 0, 8);
            }

            num = br.ReadUInt32BigEndian();

            if (br.ReadUInt32BigEndian() != 0xFFFFFFFF || num != _itemArrayInited - 1)
            {
                f.Dispose();
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            br.ReadUInt32BigEndian();
            br.ReadUInt32BigEndian();
            _noParentNotify = true;

            // add all timers
            KillAllTimers();
            for (num = br.ReadUInt32BigEndian(); num != 0; num--)
            {
                uint timeout = br.ReadUInt32BigEndian();
                ushort subroutine_id = br.ReadUInt16BigEndian();
                AddTimeEvent((ushort) timeout, subroutine_id);
            }

            item_index = 1;
            for (num = (uint) (_itemArrayInited - 1); num != 0; num--)
            {
                Item item = _itemArrayPtr[item_index++];

                Item parent_item = DerefItem(ReadItemID(br));
                SetItemParent(item, parent_item);

                item.state = (short) br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    o.objectSize = br.ReadUInt16BigEndian();
                    o.objectWeight = br.ReadUInt16BigEndian();
                }

                var p = (SubPlayer) FindChildOfType(item, ChildType.kPlayerType);
                if (p != null)
                {
                    p.score = br.ReadInt32BigEndian();
                    p.level = br.ReadInt16BigEndian();
                    p.size = br.ReadInt16BigEndian();
                    p.weight = br.ReadInt16BigEndian();
                    p.strength = br.ReadInt16BigEndian();
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 8; i++)
                    {
                        u.userFlags[i] = br.ReadUInt16BigEndian();
                    }
                    u.userItems[0] = ReadItemID(br);
                }
            }

            // read the variables
            for (i = 0; i != _numVars; i++)
            {
                WriteVariable((ushort) i, br.ReadUInt16BigEndian());
            }

            f.Dispose();

            _noParentNotify = false;

            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            return true;
        }

        protected virtual bool SaveGame(int slot, string caption)
        {
            int item_index, num_item, i;
            uint curTime = GetTime();
            uint gsc = _gameStoppedClock;

            _videoLockOut |= 0x100;

            var stream = OSystem.SaveFileManager.OpenForSaving(GenSaveName(slot));
            if (stream == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            var f = new BinaryWriter(stream);
            f.WriteString(caption, 8);

            f.WriteUInt32BigEndian((uint) (_itemArrayInited - 1));
            f.WriteUInt32BigEndian(0xFFFFFFFF);
            f.WriteUInt32BigEndian(curTime);
            f.WriteUInt32BigEndian(0);

            i = 0;
            for (var te = _firstTimeStruct; te != null; te = te.next)
                i++;
            f.WriteUInt32BigEndian((uint) i);

            for (var te = _firstTimeStruct; te != null; te = te.next)
            {
                f.WriteUInt32BigEndian(te.time - curTime + gsc);
                f.WriteUInt16BigEndian(te.subroutine_id);
            }

            item_index = 1;
            for (num_item = _itemArrayInited - 1; num_item != 0; num_item--)
            {
                Item item = _itemArrayPtr[item_index++];

                WriteItemID(f, item.parent);

                f.WriteUInt16BigEndian((ushort) item.state);
                f.WriteUInt16BigEndian(item.classFlags);

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    f.WriteUInt16BigEndian(o.objectSize);
                    f.WriteUInt16BigEndian(o.objectWeight);
                }

                var p = (SubPlayer) FindChildOfType(item, ChildType.kPlayerType);
                if (p != null)
                {
                    f.WriteUInt32BigEndian((uint) p.score);
                    f.WriteUInt16BigEndian((ushort) p.level);
                    f.WriteUInt16BigEndian((ushort) p.size);
                    f.WriteUInt16BigEndian((ushort) p.weight);
                    f.WriteUInt16BigEndian((ushort) p.strength);
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 8; i++)
                    {
                        f.WriteUInt16BigEndian(u.userFlags[i]);
                    }
                    WriteItemID(f, u.userItems[0]);
                }
            }

            // write the variables
            for (i = 0; i != _numVars; i++)
            {
                f.WriteUInt16BigEndian((ushort) ReadVariable((ushort) i));
            }

            f.Dispose();
            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            return true;
        }

        protected virtual void PrintStats()
        {
            WindowBlock window = DummyWindow;
            int val;

            window.flags = 1;

            MouseOff();

            // Strength
            val = _variableArray[0];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 5, 133, 6, val);

            // Resolution
            val = _variableArray[1];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 11, 133, 6, val);

            // Dexterity
            val = _variableArray[2];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 18, 133, 0, val);

            // Skill
            val = _variableArray[3];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 24, 133, 0, val);

            // Life
            val = _variableArray[5];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 30, 133, 2, val);

            // Experience
            val = _variableArray[6];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 36, 133, 4, val);

            MouseOn();
        }

        protected virtual bool ConfirmOverWrite(WindowBlock window)
        {
            string message1, message2, message3;

            switch (_language)
            {
                case Language.FR_FRA:
                    message1 = "\rFichier d/j; existant.\r\r";
                    message2 = "  Ecrire pardessus ?\r\r";
                    message3 = "     Oui      Non";
                    break;
                case Language.DE_DEU:
                    message1 = "\rDatei existiert bereits.\r\r";
                    message2 = "   Ueberschreiben ?\r\r";
                    message3 = "     Ja        Nein";
                    break;
                default:
                    message1 = "\r File already exists.\r\r";
                    message2 = "    Overwrite it ?\r\r";
                    message3 = "     Yes       No";
                    break;
            }

            PrintScroll();
            window.textColumn = 0;
            window.textRow = 0;
            window.textColumnOffset = 0;
            window.textLength = 0; // Difference

            foreach (var c in message1)
                WindowPutChar(window, (byte) c);
            foreach (var c in message2)
                WindowPutChar(window, (byte) c);
            foreach (var c in message3)
                WindowPutChar(window, (byte) c);

            if (ConfirmYesOrNo(120, 78) == 0x7FFF)
                return true;

            return false;
        }

        private uint ConfirmYesOrNo(ushort x, ushort y)
        {
            var ha = FindEmptyHitArea().Value;
            ha.x = x;
            ha.y = y;
            ha.width = 30;
            ha.height = 12;
            ha.flags = BoxFlags.kBFBoxInUse;
            ha.id = 0x7FFF;
            ha.priority = 999;
            ha.window = null;

            ha = FindEmptyHitArea().Value;
            ha.x = (ushort) (x + 60);
            ha.y = y;
            ha.width = 24;
            ha.height = 12;
            ha.flags = BoxFlags.kBFBoxInUse;
            ha.id = 0x7FFE;
            ha.priority = 999;
            ha.window = null;

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit)
                {
                    if (_lastHitArea3 != null)
                        break;
                    Delay(1);
                }

                ha = _lastHitArea;

                if (ha == null)
                {
                }
                else if (ha.id == 0x7FFE)
                {
                    break;
                }
                else if (ha.id == 0x7FFF)
                {
                    break;
                }
            }

            UndefineBox(0x7FFF);
            UndefineBox(0x7FFE);

            return ha.id;
        }

        protected short MatchSaveGame(string name, ushort max)
        {
            byte[] dst = new byte[10];

            for (var slot = 0; slot < max; slot++)
            {
                Stream @in;
                if ((@in = OSystem.SaveFileManager.OpenForLoading(GenSaveName(slot))) == null) continue;

                @in.Read(dst, 0, 8);
                @in.Dispose();

                if (StringComparer.OrdinalIgnoreCase.Equals(name, dst.GetRawText()))
                {
                    return (short) slot;
                }
            }

            return -1;
        }

        // The function uses segments of code from the original game scripts
        // to allow quick loading and saving, but isn't perfect.
        //
        // Unfortuntely this allows loading and saving in locations,
        // which aren't supported, and will not restore correctly:
        // Various locations in Elvira 1/2 and Waxworks where saving
        // was disabled
        private void QuickLoadOrSave()
        {
            bool success;
            string buf;

            // Disable loading and saving when it was not possible in the original:
            // In overhead maps areas in Simon the Sorcerer 2
            // In the floppy disk demo of Simon the Sorcerer 1
            // In copy protection, conversations and cut scenes
            if ((GameType == SIMONGameType.GType_SIMON2 && _boxStarHeight == 200) ||
                (GameType == SIMONGameType.GType_SIMON1 && (Features.HasFlag(GameFeatures.GF_DEMO))) ||
                _mouseHideCount != 0 || _showPreposition)
            {
                // TODO: MessageDialog
                buf = "Quick load or save game isn't supported in this location";
//                GUI::MessageDialog dialog(buf, "OK");
//                dialog.runModal();
                return;
            }

            // Check if Simon is walking, and stop when required
            if (GameType == SIMONGameType.GType_SIMON1 && GetBitFlag(11))
            {
                VcStopAnimation(11, 1122);
                Animate(4, 11, 1122, 0, 0, 2);
                WaitForSync(1122);
            }
            else if (GameType == SIMONGameType.GType_SIMON2 && GetBitFlag(11))
            {
                VcStopAnimation(11, 232);
                Animate(4, 11, 232, 0, 0, 2);
                WaitForSync(1122);
            }

            string filename = GenSaveName(_saveLoadSlot);
            if (_saveLoadType == 2)
            {
                Subroutine sub;
                success = LoadGame(GenSaveName(_saveLoadSlot));
                if (!success)
                {
                    buf = $"Failed to load saved game from file:\n\n{filename}";
                }
                else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
                {
                    DrawIconArray(2, Me(), 0, 0);
                    SetBitFlag(97, true);
                    sub = GetSubroutineByID(100);
                    StartSubroutine(sub);
                }
                else if (GameType == SIMONGameType.GType_WW)
                {
                    sub = GetSubroutineByID(66);
                    StartSubroutine(sub);
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    sub = GetSubroutineByID(87);
                    StartSubroutine(sub);
                    SetBitFlag(7, false);
                    sub = GetSubroutineByID(19);
                    StartSubroutine(sub);
                    PrintStats();
                    sub = GetSubroutineByID(28);
                    StartSubroutine(sub);
                    SetBitFlag(17, false);
                    sub = GetSubroutineByID(207);
                    StartSubroutine(sub);
                    sub = GetSubroutineByID(71);
                    StartSubroutine(sub);
                }
                else if (GameType == SIMONGameType.GType_ELVIRA1)
                {
                    DrawIconArray(2, Me(), 0, 0);
                    sub = GetSubroutineByID(265);
                    StartSubroutine(sub);
                    sub = GetSubroutineByID(129);
                    StartSubroutine(sub);
                    sub = GetSubroutineByID(131);
                    StartSubroutine(sub);
                }
            }
            else
            {
                success = SaveGame(_saveLoadSlot, _saveLoadName);
                if (!success)
                    buf = $"Failed to save game to file:\n\n{filename}";
            }

            if (!success)
            {
                // TODO: MessageDialog
//                GUI::MessageDialog dialog(buf, "OK");
//                dialog.runModal();
            }
            else if (_saveLoadType == 1)
            {
                // TODO: MessageDialog
                buf = $"Successfully saved game in file:\n\n{filename}";
//                GUI::TimedMessageDialog dialog(buf, 1500);
//                dialog.runModal();
            }

            _saveLoadType = 0;
        }
    }
}