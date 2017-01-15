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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;

namespace NScumm.Another
{
    public class AnotherGameDescriptor : IGameDescriptor
    {
        public AnotherGameDescriptor(ADGameDescription desc, string path)
        {
            Language = desc.language;
            Platform = desc.platform;
            Path = path;
        }

        public string Id => "another";
        public string Description => "Another World";
        public Language Language { get; }
        public Platform Platform { get; }
        public int Width => Video.ScreenWidth;
        public int Height => Video.ScreenHeight;
        public PixelFormat PixelFormat => PixelFormat.Indexed8;
        public string Path { get; }
    }
}