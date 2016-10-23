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

using System.Text;

namespace NScumm.Sci
{
    internal class ResourceId
    {
        private readonly ResourceType _type;
        private readonly ushort _number;
        private readonly uint _tuple; // Only used for audio36 and sync36

        public ResourceType Type => _type;
        public ushort Number => _number;
        public uint Tuple => _tuple;

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

        public override int GetHashCode()
        {
            return _type.GetHashCode() ^ _number.GetHashCode() ^ _tuple.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            ResourceId other = obj as ResourceId;
            if (ReferenceEquals(other, null)) return false;

            return (_type == other._type) && (_number == other._number) && (_tuple == other._tuple);
        }

        // Convert from a resource ID to a base36 patch name
        public string ToPatchNameBase36()
        {
            var output = new StringBuilder();

            output.Append(Type == ResourceType.Audio36 ? '@' : '#'); // Identifier
            output.Append(IntToBase36(Number, 3));                     // Map
            output.Append(IntToBase36((int)(Tuple >> 24), 2));                // Noun
            output.Append(IntToBase36((int)((Tuple >> 16) & 0xff), 2));       // Verb
            output.Append('.');                                                   // Separator
            output.Append(IntToBase36((int)((Tuple >> 8) & 0xff), 2));        // Cond
            output.Append(IntToBase36((int)(Tuple & 0xff), 1));               // Seq

            System.Diagnostics.Debug.Assert(output.Length == 12); // We should always get 12 characters in the end
            return output.ToString();
        }

        private static string IntToBase36(int number, int minChar)
        {
            // Convert from an integer to a base36 string
            string @string = string.Empty;

            while (minChar--!=0)
            {
                int character = number % 36;
                @string = ((character < 10) ? (character + '0') : (character + 'A' - 10)) + @string;
                number /= 36;
            }

            return @string;
        }

        public static bool operator ==(ResourceId id1, ResourceId id2)
        {
            if (ReferenceEquals(id1, null) && ReferenceEquals(id2, null))
                return true;

            if (ReferenceEquals(id1, null))
                return false;

            return id1.Equals(id2);
        }

        public static bool operator !=(ResourceId id1, ResourceId id2)
        {
            return !(id1 == id2);
        }

        public override string ToString()
        {
            return $"({Type}, {Number}, {Tuple})";
        }
    }
}