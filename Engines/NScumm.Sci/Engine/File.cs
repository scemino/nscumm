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

using NScumm.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace NScumm.Sci.Engine
{
    internal class SavegameDesc
    {
        public short id;
        public int virtualId; // straight numbered, according to id but w/o gaps
        public int date;
        public int time;
        public int version;
        public string name;
    }

    internal class FileHandle : IDisposable
    {
        public bool IsOpen { get { return _in != null || _out != null; } }

        public string _name;
        public Stream _in;
        public Stream _out;

        ~FileHandle()
        {
            GC.SuppressFinalize(this);
            Close();
        }

        public void Close()
        {
            if (_in != null)
            {
                _in.Dispose();
                _in = null;
            }
            if (_out != null)
            {
                _out.Dispose();
                _out = null;
            }
            _name = string.Empty;
        }

        void IDisposable.Dispose()
        {
            Close();
        }
    }

    internal class DirSeeker
    {
        internal Register NextFile(SegManager _segMan)
        {
            throw new NotImplementedException();
        }

        internal string GetVirtualFilename(uint _chosenQfGImportItem)
        {
            throw new NotImplementedException();
        }
    }

    internal static class File
    {
        // Create a sorted array containing all found savedgames
        public static IList<SavegameDesc> ListSavegames()
        {
            List<SavegameDesc> saves = new List<SavegameDesc>();
            ISaveFileManager saveFileMan = SciEngine.Instance.SaveFileManager;

            // Load all saves
            var saveNames = saveFileMan.ListSavefiles(SciEngine.Instance.SavegamePattern);

            foreach (var filename in saveNames)
            {
                SavegameMetadata meta;
                using (Stream @in = saveFileMan.OpenForLoading(filename))
                {
                    if (!Savegame.get_savegame_metadata(@in, out meta) || string.IsNullOrEmpty(meta.name))
                    {
                        // invalid
                        continue;
                    }
                }

                SavegameDesc desc = new SavegameDesc();
                desc.id = short.Parse(filename.Substring(filename.Length - 3, 3));
                desc.date = meta.saveDate;
                // We need to fix date in here, because we save DDMMYYYY instead of
                // YYYYMMDD, so sorting wouldn't work
                desc.date = (int)(((desc.date & 0xFFFF) << 16) | ((desc.date & 0xFF0000) >> 8) | ((desc.date & 0xFF000000) >> 24));
                desc.time = meta.saveTime;
                desc.version = meta.version;

                if (meta.name[meta.name.Length - 1] == '\n')
                    meta.name = meta.name.Remove(meta.name.Length - 1);

                desc.name = meta.name;

                // TODO: debug(3, "Savegame in file %s ok, id %d", filename.c_str(), desc.id);

                saves.Add(desc);
            }

            // Sort the list by creation date of the saves
            saves.Sort(new Comparison<SavegameDesc>((d1, d2) =>
            {
                // sort by date
                var ret = d1.date.CompareTo(d2.date);
                if (ret == 0)
                {
                    ret = d1.time.CompareTo(d2.time);
                }
                return ret;
            }));

            return saves;
        }

        // Find a savedgame according to virtualId and return the position within our array
        public static int FindSavegame(IList<SavegameDesc> saves, short savegameId)
        {
            for (var saveNr = 0; saveNr < saves.Count; saveNr++)
            {
                if (saves[saveNr].id == savegameId)
                    return saveNr;
            }
            return -1;
        }
    }
}
