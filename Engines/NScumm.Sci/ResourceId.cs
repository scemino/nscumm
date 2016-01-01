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

namespace NScumm.Sci
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class ResourceId
    {
        private ResourceType _type;
        private ushort _number;
        private uint _tuple; // Only used for audio36 and sync36

        public ResourceType Type { get { return _type; } }
        public ushort Number { get { return _number; } }
        public uint Tuple { get { return _tuple; } }

        public ResourceId()
        {
            _type = ResourceType.Invalid;
        }

        public ResourceId(ResourceType type, ushort number, uint tuple = 0)
        {
            _type = FixupType(type);
            _number = number;
            _tuple = tuple;
        }

        public ResourceId(ResourceType type, ushort number, byte noun, byte verb, byte cond, byte seq)
        {
            _type = FixupType(type);
            _number = number;
            _tuple = (uint)((noun << 24) | (verb << 16) | (cond << 8) | seq);
        }

        private static ResourceType FixupType(ResourceType type)
        {
            if (type >= ResourceType.Invalid)
                return ResourceType.Invalid;
            return type;
        }

        internal string DebuggerDisplay
        {
            get
            {
                return $"({Type}, {Number}, {Tuple})";
            }
        }

        public override int GetHashCode()
        {
            return (int)((((int)_type << 16) | _number) ^ _tuple);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            ResourceId other = obj as ResourceId;
            if (ReferenceEquals(other, null)) return false;

            return (_type == other._type) && (_number == other._number) && (_tuple == other._tuple);
        }

        public static bool operator ==(ResourceId id1, ResourceId id2)
        {
            if (ReferenceEquals(id1, null) && ReferenceEquals(id2, null))
                return true;

            if (ReferenceEquals(id1, null) && !ReferenceEquals(id2, null))
                return false;

            return id1.Equals(id2);
        }

        public static bool operator !=(ResourceId id1, ResourceId id2)
        {
            return !(id1 == id2);
        }
    }
}