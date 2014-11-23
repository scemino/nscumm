//
//  NoteTimer.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core
{
    class NoteTimer
    {
        /// <summary>
        /// Gets or sets the MIDI channel on which the note was played.
        /// </summary>
        /// <value>The channel.</value>
        public int Channel { get; set; }

        /// <summary>
        /// Gets or sets the note number for the active note.
        /// </summary>
        /// <value>The note.</value>
        public int Note { get; set; }

        /// <summary>
        /// Gets or sets the time, in microseconds, remaining before the note should be turned off
        /// </summary>
        /// <value>The time left.</value>
        public int TimeLeft { get; set; }
    }
    
}
