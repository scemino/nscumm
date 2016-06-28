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
using System.Globalization;
using NScumm.Core.Graphics;
using NScumm.Core.Audio;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Sky
{
    class SkyGameDescriptor : IGameDescriptor
    {
        private readonly Language _language;
        private string _path;

        public string Description
        {
            get
            {
                return "Beneath a Steel Sky";
            }
        }

        public string Id
        {
            get
            {
                return "sky";
            }
        }

        public Language Language
        {
            get
            {
                return _language;
            }
        }

        public Platform Platform
        {
            get
            {
                return Platform.Unknown;
            }
        }

        public int Width
        {
            get
            {
                return 320;
            }
        }

        public int Height
        {
            get
            {
                return 200;
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {
                return PixelFormat.Indexed8;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public SkyGameDescriptor(string path)
        {
            _path = path;
            // The game detector uses US English by default. We want British
            // English to match the recorded voices better.
            _language = Language.UNK_LANG;
        }
    }

    public class SkyMetaEngine : IMetaEngine
    {
        public IEngine Create(GameSettings settings, ISystem system)
        {
            return new SkyEngine(settings, system);
        }

        public GameDetected DetectGame(string path)
        {
            var fileName = ServiceLocator.FileStorage.GetFileName(path);
            if (string.Equals(fileName, "sky.dnr", StringComparison.OrdinalIgnoreCase))
            {
                var directory = ServiceLocator.FileStorage.GetDirectoryName(path);
                using (var disk = new Disk(directory))
                {
                    var version = disk.DetermineGameVersion();
                    return new GameDetected(new SkyGameDescriptor(path), this);
                }
            }
            return null;
        }
    }
}
