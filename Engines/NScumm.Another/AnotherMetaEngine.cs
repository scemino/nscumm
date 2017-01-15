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
using NScumm.Core.IO;

namespace NScumm.Another
{
    public class AnotherMetaEngine : AdvancedMetaEngine
    {
        public override string OriginalCopyright => "Another World by Eric Chahi / Delphine Software";

        private static readonly ADGameDescription[] GameDescriptions =
        {
            new ADGameDescription("another", "",
                new[]
                {
                    new ADGameFileDescription("MEMLIST.BIN"),
                })
        };

        public AnotherMetaEngine()
            : base(GameDescriptions)
        {
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return new AnotherEngine(system, settings);
        }

        protected override IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc)
        {
            return new AnotherGameDescriptor(path);
        }
    }
}