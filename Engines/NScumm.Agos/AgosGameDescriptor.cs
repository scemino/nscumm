//
//  AgosGameDescriptor.cs
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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    class AgosGameDescriptor : IGameDescriptor
    {
        public AgosGameDescriptor(string path, AGOSGameDescription desc)
        {
            ADGameDescription = desc;
            Path = path;
            Id = desc.gameid;
            Language = desc.language;
            Platform = desc.platform;
        }

        public AGOSGameDescription ADGameDescription { get; }

        public string Description => "";

        public int Height => 200;

        public string Id
        {
            get; private set;
        }

        public Language Language
        {
            get; private set;
        }

        public string Path
        {
            get; private set;
        }

        public PixelFormat PixelFormat
        {
            get; private set;
        }

        public Platform Platform
        {
            get; private set;
        }

        public int Width => 320;
    }
}