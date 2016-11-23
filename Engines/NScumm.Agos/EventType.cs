//
//  EventType.cs
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

namespace NScumm.Agos
{
    enum EventType
    {
        ANIMATE_INT = 1 << 1,
        ANIMATE_EVENT = 1 << 2,
        SCROLL_EVENT = 1 << 3,
        PLAYER_DAMAGE_EVENT = 1 << 4,
        MONSTER_DAMAGE_EVENT = 1 << 5
    }
}