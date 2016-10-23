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

using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    internal enum AudioSyncCommands
    {
        Start = 0,
        Next = 1,
        Stop = 2
    }

    /// <summary>
    /// Sync class, kDoSync and relevant functions for SCI games.
    /// Provides AV synchronization for animations.
    /// </summary>
    internal class Sync
    {
        private readonly SegManager _segMan;
        private readonly ResourceManager _resMan;
        private ResourceManager.ResourceSource.Resource _resource;
        private int _offset;

        public Sync(ResourceManager resMan, SegManager segMan)
        {
            _resMan = resMan;
            _segMan = segMan;
        }

        public void Start(ResourceId id, Register syncObjAddr)
        {
            _resource = _resMan.FindResource(id, true);
            _offset = 0;

            if (_resource != null)
            {
                SciEngine.WriteSelectorValue(_segMan, syncObjAddr, o => o.syncCue, 0);
            }
            else
            {
                Warning("Sync::start: failed to find resource {0}", id);
                // Notify the scripts to stop sound sync
                SciEngine.WriteSelectorValue(_segMan, syncObjAddr, o => o.syncCue, Register.SIGNAL_OFFSET);
            }
        }

        public void Next(Register syncObjAddr)
        {
            if (_resource == null || (_offset >= _resource.size - 1)) return;

            short syncCue = -1;
            short syncTime = (short) _resource.data.ReadSci11EndianUInt16(_offset);

            _offset += 2;

            if ((syncTime != -1) && (_offset < _resource.size - 1))
            {
                syncCue = (short) _resource.data.ReadSci11EndianUInt16(_offset);
                _offset += 2;
            }

            SciEngine.WriteSelectorValue(_segMan, syncObjAddr, o => o.syncTime, (ushort) syncTime);
            SciEngine.WriteSelectorValue(_segMan, syncObjAddr, o => o.syncCue, (ushort) syncCue);
        }

        public void Stop()
        {
            if (_resource == null) return;

            _resMan.UnlockResource(_resource);
            _resource = null;
        }
    }
}