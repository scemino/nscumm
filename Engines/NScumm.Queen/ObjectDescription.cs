//
//  ObjectDescription.cs
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
	public class ObjectDescription {
		//! entry in ObjectData or ItemData
		public ushort @object;
		//! type of the description
		/*!
		refer to select.c l.75-101
		<table>
			<tr>
				<td>value</td>
				<td>description</td>
			</tr>
			<tr>
				<td>0</td>
				<td>random but starts at first description</td>
			<tr>
				<td>1</td>
				<td>random</td>
			</tr>
			<tr>
				<td>2</td>
				<td>sequential with loop</td>
			</tr>
			<tr>
				<td>3</td>
				<td>sequential and set description to last</td>
			</tr>
		</table>
	*/
		public ushort type;
		//! last entry possible in OBJECT_DESCR for this object
		public ushort lastDescription;
		//! last description number used (in order to avoid re-using it)
		public ushort lastSeenNumber;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			@object = data.ToUInt16BigEndian(ptr); ptr += 2;
			type = data.ToUInt16BigEndian(ptr); ptr += 2;
			lastDescription = data.ToUInt16BigEndian(ptr); ptr += 2;
			lastSeenNumber = data.ToUInt16BigEndian(ptr); ptr += 2;
		}

		public void writeToBE(byte[] data, ref int ptr) 
		{
			data.WriteUInt16BigEndian(ptr, @object); ptr += 2;
			data.WriteUInt16BigEndian(ptr, type); ptr += 2;
			data.WriteUInt16BigEndian(ptr, lastDescription); ptr += 2;
			data.WriteUInt16BigEndian(ptr, lastSeenNumber); ptr += 2;
		}
	}
	
}
