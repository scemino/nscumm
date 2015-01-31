//
//  IMuseDigital_Music.cs
//
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
using System.Diagnostics;

namespace NScumm.Core.Audio.IMuse
{
    partial class IMuseDigital
    {
        public void SetAudioNames(string[] names)
        {
            _numAudioNames = names.Length;
            _audioNames = names;
        }

        void SetFtMusicState(int stateId)
        {
            if (stateId > 48)
                return;

            Debug.WriteLine("State music: {0}, {1}", _ftStateMusicTable[stateId].name, _ftStateMusicTable[stateId].audioName);

            if (_curMusicState == stateId)
                return;

            if (_curMusicSeq == 0)
            {
                if (stateId == 0)
                    PlayFtMusic(null, 0, 0);
                else
                    PlayFtMusic(_ftStateMusicTable[stateId].audioName, _ftStateMusicTable[stateId].transitionType, _ftStateMusicTable[stateId].volume);
            }

            _curMusicState = stateId;
        }

        void SetFtMusicSequence(int seqId)
        {
            if (seqId > 52)
                return;

            Debug.WriteLine("Sequence music: {0}", _ftSeqNames[seqId]);

            if (_curMusicSeq == seqId)
                return;

            if (seqId == 0)
            {
                if (_curMusicState == 0)
                    PlayFtMusic(null, 0, 0);
                else
                {
                    PlayFtMusic(_ftStateMusicTable[_curMusicState].audioName, _ftStateMusicTable[_curMusicState].transitionType, _ftStateMusicTable[_curMusicState].volume);
                }
            }
            else
            {
                int seq = (seqId - 1) * 4;
                PlayFtMusic(_ftSeqMusicTable[seq].audioName, _ftSeqMusicTable[seq].transitionType, _ftSeqMusicTable[seq].volume);
            }

            _curMusicSeq = seqId;
            _curMusicCue = 0;
        }

        void SetFtMusicCuePoint(int cueId)
        {
            if (cueId > 3)
                return;

            Debug.WriteLine("Cue point sequence: {0}", cueId);

            if (_curMusicSeq == 0)
                return;

            if (_curMusicCue == cueId)
                return;

            if (cueId == 0)
                PlayFtMusic(null, 0, 0);
            else
            {
                int seq = ((_curMusicSeq - 1) * 4) + cueId;
                PlayFtMusic(_ftSeqMusicTable[seq].audioName, _ftSeqMusicTable[seq].transitionType, _ftSeqMusicTable[seq].volume);
            }

            _curMusicCue = cueId;
        }

        void PlayFtMusic(string songName, int opcode, int volume)
        {
            FadeOutMusic(200);

            switch (opcode)
            {
                case 0:
                case 4:
                    break;
                case 1:
                case 2:
                case 3:
                    {
                        int soundId = GetSoundIdByName(songName);
                        if (soundId != -1)
                        {
                            StartMusic(soundId, volume);
                        }
                    }
                    break;
            }
        }

        int GetSoundIdByName(string soundName)
        {
            if (!string.IsNullOrEmpty(soundName))
            {
                for (int r = 0; r < _numAudioNames; r++)
                {
                    if (soundName == _audioNames[r])
                    {
                        return r;
                    }
                }
            }

            return -1;
        }
    }
}

