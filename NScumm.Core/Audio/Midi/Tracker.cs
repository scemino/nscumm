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
    public class Tracker
    {
        /// <summary>
        /// A pointer to the next event to be parsed
        /// </summary>
        public BytePtr PlayPos;

        /// <summary>
        /// Current time in microseconds; may be in between event times
        /// </summary>
        public int PlayTime;

        /// <summary>
        /// Current MIDI tick; may be in between event ticks
        /// </summary>
        public int PlayTick;

        /// <summary>
        /// The time, in microseconds, of the last event that was parsed
        /// </summary>
        public int LastEventTime;

        /// <summary>
        /// The tick at which the last parsed event occurs
        /// </summary>
        public long LastEventTick;

        /// <summary>
        /// Cached MIDI command, for MIDI streams that rely on implied event codes
        /// </summary>
        public int RunningStatus;

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
            PlayPos = BytePtr.Null;
            PlayTime = 0;
            PlayTick = 0;
            LastEventTime = 0;
            LastEventTick = 0;
            RunningStatus = 0;
        }
    }
    
}
