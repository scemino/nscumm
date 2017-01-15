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

namespace NScumm.Another
{
    internal static class DebugLevels
    {
        public const int DbgRes = 1 << 0;
        public const int DbgVideo = 1 << 1;
        public const int DbgBank = 1 << 2;
        public const int DbgVm = 1 << 3;
        public const int DbgSnd = 1 << 4;
        public const int DbgInfo = 1 << 5;
        public const int DbgSer = 1 << 6;
    }
}