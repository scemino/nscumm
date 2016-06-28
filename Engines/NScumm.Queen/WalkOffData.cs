//
//  WalkOffData.cs
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

using System;
using NScumm.Core;
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;

namespace NScumm.Queen
{
	public class WalkOffData 
	{
		/// <summary>
		/// Entry in ObjectData.
		/// </summary>
		public short entryObj;
		/// <summary>
		/// Coordinates to reach
		/// </summary>
		public ushort x, y;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			entryObj = data.ToInt16BigEndian(ptr); ptr += 2;
			x = data.ToUInt16BigEndian(ptr); ptr += 2;
			y = data.ToUInt16BigEndian(ptr); ptr += 2;
		}

		public void WriteToBE(byte[] data, ref int ptr) 
		{
			data.WriteInt16BigEndian(ptr, entryObj); ptr += 2;
			data.WriteUInt16BigEndian(ptr, x); ptr += 2;
			data.WriteUInt16BigEndian(ptr, y); ptr += 2;
		}
	}
	
}
