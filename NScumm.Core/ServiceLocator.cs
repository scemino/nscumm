//
//  ServiceLocator.cs
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
using System.Collections.Generic;

namespace NScumm.Core
{
    public enum SearchOption
    {
        TopDirectoryOnly,
        AllDirectories
    }

    public interface IFileStorage
    {
        IEnumerable<string> EnumerateFiles(string path);
        IEnumerable<string> EnumerateFiles(string path, string prefix);
        IEnumerable<string> EnumerateFiles(string path, string prefix, SearchOption option);

        string Combine(string path1, string path2);

        string GetDirectoryName(string path);
        string GetFileName(string path);
        string GetFileNameWithoutExtension(string path);
        string GetExtension(string path);
        string ChangeExtension(string path, string newExtension);

        bool FileExists(string path);

        Stream OpenFileRead(string path);
        Stream OpenFileWrite(string path);

        byte[] ReadAllBytes(string filename);
        string GetSignature(string path);
    }

    public interface IPlatform
    {
        void Sleep(int timeInMs);
        object ToStructure(byte[] data, int offset, Type type);
    }

    public static class ServiceLocator
    {
        public static IFileStorage FileStorage { get; set; }

        public static IPlatform Platform { get; set; }
    }
}

