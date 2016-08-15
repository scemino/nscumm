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
using static NScumm.Core.DebugHelper;
using System.Linq;

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
                _out.Flush();
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
        protected Register _outbuffer;
        protected List<string> _files;
        protected List<string> _virtualFiles;
        protected int _iter;

        public DirSeeker()
        {
            _outbuffer = Register.NULL_REG;
            _files = new List<string>();
            _virtualFiles = new List<string>();
        }

        public Register NextFile(SegManager segMan)
        {
            if (_iter == _files.Count)
            {
                return Register.NULL_REG;
            }

            string @string;

            if (_virtualFiles.Count == 0)
            {
                // Strip the prefix, if we don't got a virtual filelisting
                string wrappedString = _files[_iter];
                @string = SciEngine.Instance.UnwrapFilename(wrappedString);
            }
            else
            {
                @string = _files[_iter];
            }

            if (@string.Length > 12)
                @string = @string.Substring(0, 12);
            segMan.Strcpy(_outbuffer, @string);

            // Return the result and advance the list iterator :)
            ++_iter;
            return _outbuffer;
        }

        public Register FirstFile(string mask, Register buffer, SegManager segMan)
        {
            // Verify that we are given a valid buffer
            if (buffer.Segment == 0)
            {
                Error($"DirSeeker::firstFile('{mask}') invoked with invalid buffer");
                return Register.NULL_REG;
            }
            _outbuffer = buffer;
            _files.Clear();
            _virtualFiles.Clear();

            int QfGImport = SciEngine.Instance.InQfGImportRoom;
            if (QfGImport != 0)
            {
                _files.Clear();
                AddAsVirtualFiles("-QfG1-", "qfg1-*");
                AddAsVirtualFiles("-QfG1VGA-", "qfg1vga-*");
                if (QfGImport > 2)
                    AddAsVirtualFiles("-QfG2-", "qfg2-*");
                if (QfGImport > 3)
                    AddAsVirtualFiles("-QfG3-", "qfg3-*");

                if (QfGImport == 3)
                {
                    // QfG3 sorts the filelisting itself, we can't let that happen otherwise our
                    //  virtual list would go out-of-sync
                    Register savedHeros = segMan.FindObjectByName("savedHeros");
                    if (!savedHeros.IsNull)
                        SciEngine.WriteSelectorValue(segMan, savedHeros, o => o.sort, 0);
                }

            }
            else
            {
                // Prefix the mask
                string wrappedMask = SciEngine.Instance.WrapFilename(mask);

                // Obtain a list of all files matching the given mask
                var saveFileMan = SciEngine.Instance.SaveFileManager;
                _files = saveFileMan.ListSavefiles(wrappedMask).ToList();
            }

            // Reset the list iterator and write the first match to the output buffer,
            // if any.
            _iter = 0;
            return NextFile(segMan);
        }

        private void AddAsVirtualFiles(string title, string fileMask)
        {
            var saveFileMan = SciEngine.Instance.SaveFileManager;
            var foundFiles = saveFileMan.ListSavefiles(fileMask);
            if (foundFiles.Length > 0)
            {
                // Sort all filenames alphabetically
                Array.Sort(foundFiles, 0, foundFiles.Length);

                _files.Add(title);
                _virtualFiles.Add("");

                foreach (var regularFilename in foundFiles)
                {
                    string wrappedFilename = regularFilename.Substring(fileMask.Length - 1);

                    var testfile = saveFileMan.OpenForLoading(regularFilename);
                    int testfileSize = (int)testfile.Length;
                    if (testfileSize > 1024) // check, if larger than 1k. in that case its a saved game.
                        continue; // and we dont want to have those in the list
                                  // We need to remove the prefix for display purposes
                    _files.Add(wrappedFilename);
                    // but remember the actual name as well
                    _virtualFiles.Add(regularFilename);
                }
            }
        }


        public string GetVirtualFilename(int fileNumber)
        {
            return _virtualFiles[fileNumber];
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

                Debug(3, $"Savegame in file {filename} ok, id {desc.id}");

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
