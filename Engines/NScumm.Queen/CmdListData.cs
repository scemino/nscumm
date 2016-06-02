//
//  CmdListData.cs
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

	class CmdListData
	{
		//! action to perform
		Verb verb;
		//! first object used in the action
		short nounObj1;
		//! second object used in the action
		short nounObj2;
		//! song to play (>0: playbefore, <0: playafter)
		short song;
		//! if set, P2_SET_AREAS must be called (using CmdArea)
		bool setAreas;
		//! if set, P3_SET_OBJECTS must be called (using CmdObject)
		bool setObjects;
		//! if set, P4_SET_ITEMS must be called (using CmdInventory)
		bool setItems;
		//! if set, P1_SET_CONDITIONS must be called (using CmdGameState)
		bool setConditions;
		//! graphic image order
		short imageOrder;
		//! special section to execute (refer to execute.c l.423-451)
		short specialSection;

		public void ReadFromBE (byte[] data, ref int ptr)
		{
			verb = (Verb)data.ToUInt16BigEndian (ptr);
			ptr += 2;
			nounObj1 = data.ToInt16BigEndian (ptr);
			ptr += 2;
			nounObj2 = data.ToInt16BigEndian (ptr);
			ptr += 2;
			song = data.ToInt16BigEndian (ptr);
			ptr += 2;
			setAreas = data.ToUInt16BigEndian (ptr) != 0;
			ptr += 2;
			setObjects = data.ToUInt16BigEndian (ptr) != 0;
			ptr += 2;
			setItems = data.ToUInt16BigEndian (ptr) != 0;
			ptr += 2;
			setConditions = data.ToUInt16BigEndian (ptr) != 0;
			ptr += 2;
			imageOrder = data.ToInt16BigEndian (ptr);
			ptr += 2;
			specialSection = data.ToInt16BigEndian (ptr);
			ptr += 2;
		}

		public bool Match (Verb v, short obj1, short obj2)
		{
			return verb == v && nounObj1 == obj1 && nounObj2 == obj2;
		}
	}

}
