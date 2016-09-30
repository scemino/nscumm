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
        // Wrappers for reading/writing 16-bit values in the endianness
        // of the original game platform.
        public static ushort ReadSciEndianUInt16(this byte[] ptr, int offset = 0)
        {
            if (SciEngine.Instance.IsBe)
                return ptr.ToUInt16BigEndian(offset);
            return ptr.ToUInt16(offset);
        }

        public static void WriteSciEndianUInt16(this byte[] ptr, int offset, ushort val)
        {
            if (SciEngine.Instance.IsBe)
                ptr.WriteUInt16BigEndian(offset, val);
            else
                ptr.WriteUInt16(offset, val);
        }

        // Wrappers for reading integer values for SCI1.1+.
        // Mac versions have big endian data for some fields.
        public static ushort ReadSci11EndianUInt16(this byte[] ptr, int offset = 0)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                return ptr.ToUInt16BigEndian(offset);
            return ptr.ToUInt16(offset);
        }

        public static uint ReadSci11EndianUInt32(this byte[] ptr, int offset = 0)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                return ptr.ToUInt32BigEndian(offset);
            return ptr.ToUInt32(offset);
        }

        public static void WriteSci11EndianUInt16(this byte[] ptr, int offset, ushort val)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                ptr.WriteUInt16BigEndian(offset, val);
            else
                ptr.WriteUInt16(offset, val);
        }

        public static void WriteSci11EndianUInt32(this byte[] ptr, int offset, uint val)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                ptr.WriteUInt32BigEndian(offset, val);
            else
                ptr.WriteUInt32(offset, val);
        }

        // Wrappers for reading integer values in resources that are
        // LE in SCI1.1 Mac, but BE in SCI32 Mac
        public static ushort ReadSci32EndianUInt16(this byte[] ptr, int offset = 0)
        {
            if (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY)
                return ptr.ToUInt16BigEndian(offset);
            return ptr.ToUInt16(offset);
        }
    }
}
