//
//  ActorData.cs
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

namespace NScumm.Queen
{
	public class ActorData {
		//! room in which the actor is
		public short room;
		//! bob number associated to this actor
		public short bobNum;
		//! entry in ACTOR_NAME
		public ushort name;
		//! gamestate entry/value, actor is valid if GAMESTATE[slot] == value
		public short gsSlot, gsValue;
		//! spoken text color
		public ushort color;
		//! bank bobframe for standing position of the actor
		public ushort bobFrameStanding;
		//! initial coordinates in the room
		public ushort x, y;
		//! entry in ACTOR_ANIM
		public ushort anim;
		//! bank to use to load the actor file
		public ushort bankNum;
		//! entry in ACTOR_FILE
		public ushort file;

		public void ReadFromBE(byte[] data, ref int ptr) 
		{
			room = data.ToInt16BigEndian(ptr); ptr += 2;
			bobNum = data.ToInt16BigEndian(ptr); ptr += 2;
			name = data.ToUInt16BigEndian(ptr); ptr += 2;
			gsSlot = data.ToInt16BigEndian(ptr); ptr += 2;
			gsValue = data.ToInt16BigEndian(ptr); ptr += 2;
			color = data.ToUInt16BigEndian(ptr); ptr += 2;
			bobFrameStanding = data.ToUInt16BigEndian(ptr); ptr += 2;
			x = data.ToUInt16BigEndian(ptr); ptr += 2;
			y = data.ToUInt16BigEndian(ptr); ptr += 2;
			anim = data.ToUInt16BigEndian(ptr); ptr += 2;
			bankNum = data.ToUInt16BigEndian(ptr); ptr += 2;
			file = data.ToUInt16BigEndian(ptr); ptr += 2;
			// Fix the actor data (see queen.c - l.1518-1519). When there is no
			// valid actor file, we must load the data from the objects room bank.
			// This bank has number 15 (not 10 as in the data files).
			if (file == 0) {
				bankNum = 15;
			}
		}
	}
	
}
