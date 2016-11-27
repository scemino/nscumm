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
using NScumm.Core;

namespace NScumm.Agos
{
    partial class AGOSEngine
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

        protected virtual void UserGame(bool b)
        {
            throw new NotImplementedException();
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

        protected virtual string GenSaveName(int i)
        {
            throw new NotImplementedException();
        }

        protected virtual bool LoadGame(string filename, bool restartMode = false)
        {
            throw new NotImplementedException();
        }

        protected virtual bool SaveGame(int slot, string caption)
        {
            throw new NotImplementedException();
        }
    }
}