//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

namespace NScumm.Sci
{
    internal class FanMadePatchInfo
    {
        public readonly SciGameId GameId;
        public readonly ushort TargetScript;
        public readonly ushort TargetSize;
        public readonly ushort PatchedByteOffset;
        public readonly byte PatchedByte;

        public FanMadePatchInfo(SciGameId gameId, ushort targetScript, ushort targetSize, ushort patchedByteOffset,
            byte patchedByte)
        {
            GameId = gameId;
            TargetScript = targetScript;
            TargetSize = targetSize;
            PatchedByteOffset = patchedByteOffset;
            PatchedByte = patchedByte;
        }
    }
}