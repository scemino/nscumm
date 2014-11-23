//
//  Tracker.cs
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

namespace NScumm.Core.Audio.Midi
{
    class Tracker
    {
        /// <summary>
        /// A pointer to the next event to be parsed
        /// </summary>
        public long PlayPos { get; set; }

        /// <summary>
        /// Current time in microseconds; may be in between event times
        /// </summary>
        public int PlayTime { get; set; }

        /// <summary>
        /// Current MIDI tick; may be in between event ticks
        /// </summary>
        public int PlayTick { get; set; }

        /// <summary>
        /// The time, in microseconds, of the last event that was parsed
        /// </summary>
        public int LastEventTime{ get; set; }

        /// <summary>
        /// The tick at which the last parsed event occurs
        /// </summary>
        public long LastEventTick { get; set; }

        /// <summary>
        /// Cached MIDI command, for MIDI streams that rely on implied event codes
        /// </summary>
        public int RunningStatus { get; set; }

        public Tracker()
        {
            Clear();
        }

        /// Copy constructor for each duplication of Tracker information.
        public Tracker(Tracker copy)
        {
            PlayPos = copy.PlayPos;
            PlayTime = copy.PlayTime;
            PlayTick = copy.PlayTick;
            LastEventTime = copy.LastEventTime;
            LastEventTick = copy.LastEventTick;
            RunningStatus = copy.RunningStatus;
        }

        /// Clears all data; used by the constructor for initialization.
        public void Clear()
        {
            PlayPos = 0;
            PlayTime = 0;
            PlayTick = 0;
            LastEventTime = 0;
            LastEventTick = 0;
            RunningStatus = 0;
        }
    }
    
}
