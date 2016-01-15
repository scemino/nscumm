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

using System;
using NScumm.Core.Audio;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Sound
{
    enum AudioSyncCommands
    {
        Start = 0,
        Next = 1,
        Stop = 2
    }

    internal class AudioPlayer
    {
        private int _audioRate;
        private IMixer _mixer;
        private ResourceManager _resMan;
        private bool _wPlayFlag;
        /// <summary>
        /// Used by kDoSync for speech syncing in CD talkie games
        /// </summary>
        private ResourceManager.ResourceSource.Resource _syncResource;
        private int _syncOffset;
        private uint _audioCdStart;
        private SoundHandle _audioHandle;

        public AudioPlayer(ResourceManager resMan)
        {
            _resMan = resMan;
            _audioRate = 11025;

            _mixer = SciEngine.Instance.Mixer;
            _wPlayFlag = false;
        }

        internal IRewindableAudioStream GetAudioStream(ushort resourceId, int v, out int sampleLen)
        {
            throw new NotImplementedException();
        }

        public void StopSoundSync()
        {
            if (_syncResource != null)
            {
                _resMan.UnlockResource(_syncResource);
                _syncResource = null;
            }
        }

        public void SetSoundSync(ResourceId id, Register syncObjAddr, SegManager segMan)
        {
            _syncResource = _resMan.FindResource(id, true);
            _syncOffset = 0;

            if (_syncResource != null)
            {
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, 0);
            }
            else {
                // TODO: warning("setSoundSync: failed to find resource %s", id.toString().c_str());
                // Notify the scripts to stop sound sync
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, Register.SIGNAL_OFFSET);
            }
        }

        public void DoSoundSync(Register syncObjAddr, SegManager segMan)
        {
            if (_syncResource != null && (_syncOffset < _syncResource.size - 1))
            {
                short syncCue = -1;
                short syncTime = (short)_syncResource.data.ReadSci11EndianUInt16(_syncOffset);

                _syncOffset += 2;

                if ((syncTime != -1) && (_syncOffset < _syncResource.size - 1))
                {
                    syncCue = (short)_syncResource.data.ReadSci11EndianUInt16(_syncOffset);
                    _syncOffset += 2;
                }

                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncTime, (ushort)syncTime);
                SciEngine.WriteSelectorValue(segMan, syncObjAddr, o => o.syncCue, (ushort)syncCue);
            }
        }

        public void StopAllAudio()
        {
            StopSoundSync();
            StopAudio();
            if (_audioCdStart > 0)
                AudioCdStop();
        }

        private void AudioCdStop()
        {
            throw new NotImplementedException();
            //_audioCdStart = 0;
            //g_system->getAudioCDManager()->stop();
        }

        private void StopAudio()
        {
            _mixer.StopHandle(_audioHandle);
        }
    }
}
