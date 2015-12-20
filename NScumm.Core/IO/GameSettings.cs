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

namespace NScumm.Core.IO
{
    public class GameSettings
    {
        public IGameDescriptor Game { get; private set; }

        public IMetaEngine MetaEngine { get; private set; }

        public string AudioDevice { get; set; }

        public bool CopyProtection { get; set; }

        public int BootParam { get; set; }

        public GameSettings(IGameDescriptor game, IMetaEngine engine)
        {
            Game = game;
            AudioDevice = "adlib";
            MetaEngine = engine;
        }
    }
}