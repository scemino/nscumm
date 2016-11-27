//
//  HitArea.cs
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

namespace NScumm.Agos
{
    [Flags]
    enum BoxFlags
    {
        kBFToggleBox = 0x1, // Elvira 1/2
        kBFTextBox = 0x1, // Others
        kBFBoxSelected = 0x2,
        kBFInvertSelect = 0x4, // Elvira 1/2
        kBFNoTouchName = 0x4, // Others
        kBFInvertTouch = 0x8,
        kBFHyperBox = 0x10, // Feeble Files
        kBFDragBox = 0x10, // Others
        kBFBoxInUse = 0x20,
        kBFBoxDead = 0x40,
        kBFBoxItem = 0x80
    }

    class HitArea : IEquatable<HitArea>
    {
        public ushort x, y;
        public ushort width, height;
        public BoxFlags flags;
        public ushort id;
        public ushort data;
        public WindowBlock window;
        public Item itemPtr;
        public ushort verb;
        public ushort priority;

        // Personal Nightmare specific
        public ushort msg1, msg2;

        public static readonly HitArea None = new HitArea {id = ushort.MaxValue};

        public bool Equals(HitArea other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HitArea) obj);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public static bool operator ==(HitArea left, HitArea right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HitArea left, HitArea right)
        {
            return !Equals(left, right);
        }
    }
}