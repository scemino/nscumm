//
//  AGOSGameDescription.cs
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

using NScumm.Core.IO;

namespace NScumm.Agos
{
    class AGOSGameDescription : ADGameDescription
    {
        public SIMONGameType gameType;
        public GameIds gameId;
        public GameFeatures features;

        public AGOSGameDescription(ADGameDescription desc, SIMONGameType gameType,
            GameIds gameId, GameFeatures features)
            : base(desc.gameid, desc.extra, desc.filesDescriptions, desc.language, desc.platform, desc.flags,
                desc.guioptions)
        {
            this.gameType = gameType;
            this.gameId = gameId;
            this.features = features;
        }
    }
}