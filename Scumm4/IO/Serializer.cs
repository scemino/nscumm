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
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Scumm4.IO
{
    public class Serializer
    {
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }
        public uint Version { get; private set; }

        public bool IsLoading { get; private set; }

        public static Serializer CreateReader(BinaryReader reader, uint version)
        {
            return new Serializer { Reader = reader, IsLoading = true, Version = version };
        }

        public static Serializer CreateWriter(BinaryWriter writer, uint version)
        {
            return new Serializer { Writer = writer, IsLoading = false, Version = version };
        }
    }


    internal class LoadAndSaveEntry
    {
        private Action<BinaryReader> _load;
        private Action<BinaryWriter> _save;
        private uint _minVersion;
        private uint _maxVersion = uint.MaxValue;

        public static LoadAndSaveEntry Create(Action<BinaryReader> load, uint minVersion=0, uint maxVersion = uint.MaxValue)
        {
            return new LoadAndSaveEntry { _load = load, _minVersion = minVersion, _maxVersion = maxVersion };
        }

        public static LoadAndSaveEntry Create(Action<BinaryReader> load, Action<BinaryWriter> save, uint minVersion=0, uint maxVersion = uint.MaxValue)
        {
            return new LoadAndSaveEntry { _load = load, _save = save, _minVersion = minVersion, _maxVersion = maxVersion };
        }

        public void Execute(Serializer serializer)
        {
            if (serializer.Version >= _minVersion && serializer.Version <= _maxVersion)
            {
                if (serializer.IsLoading)
                {
                    _load(serializer.Reader);
                }
                else
                {
                    _save(serializer.Writer);
                }
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
