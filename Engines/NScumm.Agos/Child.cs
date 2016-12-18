//
//  Child.cs
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
using System.Collections.Generic;

namespace NScumm.Agos
{
    enum ChildType
    {
        kRoomType = 1,
        kObjectType = 2,
        kPlayerType = 3,
        kGenExitType = 4, // Elvira 1 specific
        kSuperRoomType = 4, // Elvira 2 specific

        kContainerType = 7,
        kChainType = 8,
        kUserFlagType = 9,

        kInheritType = 255
    }

    class Child
    {
        public Child next;
        public ChildType type;
    }

    class SubPlayer : Child
    {
        public short userKey;
        public short size;
        public short weight;
        public short strength;
        public short flags;
        public short level;
        public int score;
    }

    class SubUserFlag : Child
    {
        public ushort subroutine_id;
        public ushort[] userFlags = new ushort[8];
        public ushort[] userItems = new ushort[1];
    }

    class SubInherit : Child
    {
        public ushort subroutine_id;
        public ushort inMaster;
    }

    class SubRoom : Child
    {
        public ushort subroutine_id;
        public ushort roomExitStates;
        public ushort[] roomExit = new ushort[3];

        public ushort roomShort
        {
            get { return roomExit[1]; }
            set { roomExit[1] = value; }
        }

        public ushort roomLong
        {
            get { return roomExit[2]; }
            set { roomExit[2] = value; }
        }

        public ushort flags;
    }

    class SubSuperRoom : Child
    {
        public ushort subroutine_id;
        public ushort roomX;
        public ushort roomY;
        public ushort roomZ;
        public ushort[] roomExitStates = new ushort[1];
    }

    [Flags]
    enum SubObjectFlags
    {
        kOFText = 0x1,
        kOFSize = 0x2,
        kOFWorn = 0x4, // Elvira 1
        kOFWeight = 0x4, // Others
        kOFVolume = 0x8,
        kOFIcon = 0x10,
        kOFKeyColor1 = 0x20,
        kOFKeyColor2 = 0x40,
        kOFMenu = 0x80,
        kOFNumber = 0x100,
        kOFSoft = 0x200, // Waxworks
        kOFVoice = 0x200 // Others
    }

    class SubObject : Child
    {
        public ushort objectName;
        public ushort objectSize;
        public ushort objectWeight;
        public SubObjectFlags objectFlags;
        public List<short> objectFlagValue = new List<short>();
    }

    class SubGenExit : Child
    {
        public ushort subroutine_id;
        public ushort[] dest = new ushort[6];
    }

    class SubContainer : Child
    {
        public ushort subroutine_id;
        public ushort volume;
        public ushort flags;
    }

    class SubChain : Child
    {
        public ushort subroutine_id;
        public ushort chChained;
    }
}