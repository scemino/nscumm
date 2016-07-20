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

using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace NScumm.Core
{
    public enum SearchOption
    {
        TopDirectoryOnly,
        AllDirectories
    }

    public interface IFileStorage
    {
        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption option = SearchOption.TopDirectoryOnly);

        string Combine(string path1, string path2);

        string GetPath(Stream file);

        string GetDirectoryName(string path);

        string GetFileName(string path);

        string GetFileNameWithoutExtension(string path);

        string GetExtension(string path);

        string ChangeExtension(string path, string newExtension);

        bool FileExists(string path);

        Stream OpenFileRead(string path);

        Stream OpenFileWrite(string path);

        byte[] ReadAllBytes(string filename);

        long GetSize(string filePath);

        string GetSignature(string path, int size = 1024 * 1024);

        Stream OpenContent(string path);

        // warning: don't remove this or you will have a TypeInitializationException
        XDocument LoadDocument(Stream stream);
    }

    public static class FileStorageExtension
    {
        public static string Combine(this IFileStorage fileStorage, params string[] paths)
        {
            var path = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                path = fileStorage.Combine(path, paths[i]);
            }
            return path;
        }
    }
}