//
//  FurnitureData.cs
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
	class FurnitureData {
		//! room in which the furniture are
		public short room;
		//! furniture object number
		/*!
		<table>
			<tr>
				<td>range</td>
				<td>type</td>
			</tr>
			<tr>
				<td>]0..5000]</td>
				<td>static or animated</td>
			</tr>
			<tr>
				<td>]5000..[</td>
				<td>paste down</td>
			</tr>
		</table>
	*/
		public short objNum;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			room = data.ToInt16BigEndian(ptr); ptr += 2;
			objNum = data.ToInt16BigEndian(ptr); ptr += 2;
		}
	}
	
}
