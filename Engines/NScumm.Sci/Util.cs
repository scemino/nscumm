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
using NScumm.Core.IO;

namespace NScumm.Sci
{
    internal static class Util
    {
        public static uint ReadSci11EndianUInt32(this byte[] ptr, int offset)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                return ptr.ToUInt32BigEndian(offset);
            else
                return ptr.ToUInt32(offset);
        }

        public static ushort ReadSci11EndianUInt16(this byte[] ptr, int offset = 0)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                return ptr.ToUInt16BigEndian(offset);
            else
                return ptr.ToUInt16(offset);
        }

        public static void WriteSciEndianUInt16(this byte[] ptr, int offset, ushort val)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                ptr.WriteUInt16BigEndian(offset, val);
            else
                ptr.WriteUInt16(offset, val);
        }
    }
}
