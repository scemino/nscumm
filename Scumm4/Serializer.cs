/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Scumm4
{
    internal class LoadAndSaveEntry
    {
        private Action action;
        private uint minVersion;
        private uint maxVersion = uint.MaxValue;

        public static LoadAndSaveEntry Create(Action action, uint minVersion, uint maxVersion = uint.MaxValue)
        {
            return new LoadAndSaveEntry { action = action, minVersion = minVersion, maxVersion = maxVersion };
        }

        public void Execute(uint version)
        {
            if (version >= minVersion && version <= maxVersion)
            {
                action();
            }
        }
    }

    internal class SaveGameHeader
    {
        public uint type;
        public uint size;
        public uint ver;
        public string name;
    }

    internal class SaveStateMetaInfos
    {
        public uint date;
        public ushort time;
        public uint playtime;
    }

    internal class ThumbnailHeader
    {
        public uint type;
        public uint size;
        public byte version;
        public ushort width, height;
        public byte bpp;
    }

    internal class SaveInfoSection
    {
        public uint type;
        public uint version;
        public uint size;

        public uint timeTValue;  // Obsolete since version 2, but kept for compatibility
        public uint playtime;

        public uint date;
        public ushort time;
    }
}
