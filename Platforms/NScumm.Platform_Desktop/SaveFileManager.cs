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

using System;
using System.IO;
using System.Linq;
using NScumm.Core;

namespace NScumm
{
    public class SaveFileManager : ISaveFileManager
    {
        private IFileStorage _fileStorage;

        public SaveFileManager(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public Stream OpenForLoading(string fileName)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            var fullPath = Path.Combine(path, fileName);
            return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        }

        public Stream OpenForSaving(string fileName, bool compress = true)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            return File.OpenWrite(Path.Combine(path, fileName));
        }

        public string[] ListSavefiles(string pattern)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            return Directory.EnumerateFiles(path, pattern).Select(Path.GetFileName).ToArray();
        }

        public void RemoveSavefile(string filename)
        {
            string path = GetSavePath();
            var fullPath = Path.Combine(path, filename);
            File.Delete(fullPath);
        }

        private static void EnsureSavePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nScumm",
                "SaveGames");
        }
    }
}
