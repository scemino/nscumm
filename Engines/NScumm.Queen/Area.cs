//
//  Area.cs
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
	public class Area
	{
		//! bitmask of connected areas
		public short mapNeighbors;
		//! coordinates defining area limits
		public Box box = new Box ();
		//! scaling factors for bobs actors
		public ushort bottomScaleFactor, topScaleFactor;
		//! entry in ObjectData, object lying in this area
		public ushort @object;

		public void ReadFromBE (byte[] data, ref int ptr)
		{
			mapNeighbors = data.ToInt16BigEndian (ptr);
			ptr += 2;
			box.ReadFromBE (data, ref ptr);
			bottomScaleFactor = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			topScaleFactor = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			@object = data.ToUInt16BigEndian (ptr);
			ptr += 2;
		}

		public void WriteToBE (byte[] data, ref int ptr)
		{
			data.WriteInt16BigEndian (ptr, mapNeighbors);
			ptr += 2;
			box.WriteToBE (data, ref ptr);
			data.WriteUInt16BigEndian (ptr, bottomScaleFactor);
			ptr += 2;
			data.WriteUInt16BigEndian (ptr, topScaleFactor);
			ptr += 2;
			data.WriteUInt16BigEndian (ptr, @object);
			ptr += 2;
		}

		public ushort CalcScale (short y)
		{
			ushort dy = (ushort)box.yDiff;
			short ds = ScaleDiff;
			ushort scale = 0;

			if (dy != 0)	// Prevent division-by-zero
				scale = (ushort)(((((y - box.y1) * 100) / dy) * ds) / 100 + bottomScaleFactor);

			if (scale == 0)
				scale = 100;

			return scale;
		}

		public short ScaleDiff {
			get{ return (short)(topScaleFactor - bottomScaleFactor); }
		}
	}
	
}
