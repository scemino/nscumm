//
//  CmdGameState.cs
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

	class CmdGameState {
		//! CmdListData number
		public short id;
		public short gameStateSlot;
		public short gameStateValue;
		public ushort speakValue;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			id = data.ToInt16BigEndian(ptr); ptr += 2;
			gameStateSlot = data.ToInt16BigEndian(ptr); ptr += 2;
			gameStateValue = data.ToInt16BigEndian(ptr); ptr += 2;
			speakValue = data.ToUInt16BigEndian(ptr); ptr += 2;
		}
	}

}
