//
//  Box.cs
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

using NScumm.Core;

namespace NScumm.Queen
{
    public class Box
	{
        public short x1, y1, x2, y2;

        public Box()
        {
        }

        public Box(Box copy)
            :this(copy.x1,copy.y1,copy.x2,copy.y2)
        {
        }

        public Box (short xx1, short yy1, short xx2, short yy2)
		{
			x1 = xx1;
			y1 = yy1;
			x2 = xx2;
			y2 = yy2;
		}

		public short xDiff 
		{
			get { return (short)(x2 - x1); }
		}

		public short yDiff 
		{
			get { return (short)(y2 - y1); }
		}

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			x1 = data.ToInt16BigEndian(ptr); ptr += 2;
			y1 = data.ToInt16BigEndian(ptr); ptr += 2;
			x2 = data.ToInt16BigEndian(ptr); ptr += 2;
			y2 = data.ToInt16BigEndian(ptr); ptr += 2;
		}

		public void WriteToBE(byte[] data, ref int ptr) 
		{
			data.WriteInt16BigEndian(ptr, x1); ptr += 2;
			data.WriteInt16BigEndian(ptr, y1); ptr += 2;
			data.WriteInt16BigEndian(ptr, x2); ptr += 2;
			data.WriteInt16BigEndian(ptr, y2); ptr += 2;
		}

		public bool Intersects (short x, short y, ushort w, ushort h)
		{
			return (x + w > x1) && (y + h > y1) && (x <= x2) && (y <= y2);
		}

		public bool Contains (short x, short y)
		{
			return (x >= x1) && (x <= x2) && (y >= y1) && (y <= y2);
		}

		public static bool operator== (Box b1, Box b2)
		{
			if (ReferenceEquals (b1, null) && ReferenceEquals (b2, null))
				return true;
			if (ReferenceEquals (b1, null) || ReferenceEquals (b2, null))
				return false;
			
			return b1.Equals (b2);
		}

		public static bool operator!= (Box b1, Box b2)
		{
			return !(b1 == b2);
		}

		public override bool Equals (object obj)
		{
            if (!(obj is Box)) return false;

            var box = (Box)obj;
			return (x1 == box.x1) && (x2 == box.x2) && (y1 == box.y1) && (y2 == box.y2);
		}

		public override int GetHashCode ()
		{
			return x1 ^ x2 ^ y1 ^ y2;
		}
	}
	
}
