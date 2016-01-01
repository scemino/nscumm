//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using NScumm.Core.Graphics;
using System.Collections.Generic;

namespace NScumm.Sci
{
    internal class SciEvent
    {
        public short type;
        public short data;
        public short modifiers;
        /**
         * For keyboard events: 'data' after applying
         * the effects of 'modifiers', e.g. if
         *   type == SCI_EVT_KEYBOARD
         *   data == 'a'
         *   buckybits == SCI_EVM_LSHIFT
         * then
         *   character == 'A'
         * For 'Alt', characters are interpreted by their
         * PC keyboard scancodes.
         */
        public short character;

        /**
         * The mouse position at the time the event was created.
         *
         * These are display coordinates!
         */
        public Point mousePos;
    }

    internal class EventManager
    {
        private readonly bool _fontIsExtended;
        private List<SciEvent> _events;

        public EventManager(bool fontIsExtended)
        {
            _fontIsExtended = fontIsExtended;
            _events = new List<SciEvent>();
        }
    }
}
