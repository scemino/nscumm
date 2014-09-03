//
//  ResourceFile.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Linq;

namespace NScumm.Core.IO
{
    public abstract class ResourceFile
    {
        #region Fields

        protected readonly XorReader _reader;

        #endregion

        #region Constructor

        protected ResourceFile(string path, byte encByte)
        {
            var dir = Path.GetDirectoryName(path);
            var realPath = (from file in Directory.EnumerateFiles(dir)
                                     where string.Equals(file, path, StringComparison.OrdinalIgnoreCase)
                                     select file).FirstOrDefault();
            var fs = File.OpenRead(realPath);
            var br2 = new BinaryReader(fs);
            _reader = new XorReader(br2, encByte);
        }

        #endregion

        #region Methods

        public abstract Dictionary<byte, long> ReadRoomOffsets();

        public abstract Room ReadRoom(long offset);

        public abstract XorReader ReadCostume(long offset);

        public abstract byte[] ReadScript(long offset);

        public abstract byte[] ReadSound(long offset);

        #endregion

    }
}
