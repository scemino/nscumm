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

using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    internal abstract class Track : ITrack
    {
        private bool _paused;

        public abstract TrackType TrackType { get; }

        public abstract bool EndOfTrack { get; }

        public virtual bool IsRewindable => IsSeekable;

        public virtual bool Rewind()
        {
            return Seek(new Timestamp(0, 1000));
        }

        public virtual bool IsSeekable => false;

        public virtual bool Seek(Timestamp time)
        {
            return false;
        }

        public void Pause(bool shouldPause)
        {
            _paused = shouldPause;
            PauseIntern(shouldPause);
        }

        public bool IsPaused => _paused;

        public virtual Timestamp Duration => new Timestamp(0, 1000);

        protected virtual void PauseIntern(bool shouldPause) { }
    }
}