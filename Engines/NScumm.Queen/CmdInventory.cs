//
//  CmdInventory.cs
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

	class CmdInventory 
	{
		//! CmdListData number
		public short id;
		//! <0: delete, >0: add
		public short dstItem;
		//! >0: valid
		public short srcItem;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			id = data.ToInt16BigEndian(ptr); ptr += 2;
			dstItem = data.ToInt16BigEndian(ptr); ptr += 2;
			srcItem = data.ToInt16BigEndian(ptr); ptr += 2;
		}
	}

}
