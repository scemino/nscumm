//
//  ConfigManager.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using System.Collections.Generic;
using System.IO;

namespace NScumm.Core.Common
{
    public class SearchManager
    {
        private readonly Dictionary<string, string> _files;

        public static readonly SearchManager Instance =new SearchManager();

        private SearchManager()
        {
            _files=new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddDirectory(params string[] directories)
        {
            AddDirectory(ServiceLocator.FileStorage.Combine(directories));
        }

        public void AddDirectory(string directory)
        {
            if (!ServiceLocator.FileStorage.DirectoryExists(directory))
                return;

            var files = ServiceLocator.FileStorage.EnumerateFiles(directory);
            foreach (var file in files)
            {
                var name = ServiceLocator.FileStorage.GetFileName(file);
                _files[name] = file;
            }
        }

        public bool HasFile(string file)
        {
            return _files.ContainsKey(file);
        }

        public Stream CreateReadStreamForMember(string name)
        {
            if (!HasFile(name)) return null;

            var path = _files[name];
            return ServiceLocator.FileStorage.OpenFileRead(path);
        }
    }
}