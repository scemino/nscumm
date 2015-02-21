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
        const int DIG_STATE_OFFSET = 11;
        const int DIG_SEQ_OFFSET = (DIG_STATE_OFFSET + 65);
        const int COMI_STATE_OFFSET = 3;

        public void SetAudioNames(string[] names)
        {
            _numAudioNames = names.Length;
            _audioNames = names;
        }

        void SetComiMusicState(int stateId)
        {
            int l, num = -1;

            if (stateId == 4) // look into #1881415 bug, ignore stateId == 4 it's seems needed after all
                return;

            if (stateId == 0)
                stateId = 1000;

            for (l = 0; _comiStateMusicTable[l].soundId != -1; l++)
            {
                if ((_comiStateMusicTable[l].soundId == stateId))
                {
                    Debug.WriteLine("Set music state: {0}, {1}", _comiStateMusicTable[l].name, _comiStateMusicTable[l].filename);
                    num = l;
                    break;
                }
            }

            if (num == -1)
                return;

            if (_curMusicState == num)
                return;

            if (_curMusicSeq == 0)
            {
                if (num == 0)
                    PlayComiMusic(null, _comiStateMusicTable[0], num, false);
                else
                    PlayComiMusic(_comiStateMusicTable[num].name, _comiStateMusicTable[num], num, false);
            }

            _curMusicState = num;
        }

        void SetComiMusicSequence(int seqId)
        {
            int l, num = -1;

            if (seqId == 0)
                seqId = 2000;

            for (l = 0; _comiSeqMusicTable[l].soundId != -1; l++)
            {
                if ((_comiSeqMusicTable[l].soundId == seqId))
                {
                    Debug.WriteLine("Set music sequence: {0}, {1}", _comiSeqMusicTable[l].name, _comiSeqMusicTable[l].filename);
                    num = l;
                    break;
                }
            }

            if (num == -1)
                return;

            if (_curMusicSeq == num)
                return;

            if (num != 0)
            {
                if (_curMusicSeq!=0 && ((_comiSeqMusicTable[_curMusicSeq].transitionType == 4)
                    || (_comiSeqMusicTable[_curMusicSeq].transitionType == 6)))
                {
                    _nextSeqToPlay = num;
                    return;
                }
                else
                {
                    PlayComiMusic(_comiSeqMusicTable[num].name, _comiSeqMusicTable[num], 0, true);
                    _nextSeqToPlay = 0;
                }
            }
            else
            {
                if (_nextSeqToPlay != 0)
                {
                    PlayComiMusic(_comiSeqMusicTable[_nextSeqToPlay].name, _comiSeqMusicTable[_nextSeqToPlay], 0, true);
                    num = _nextSeqToPlay;
                    _nextSeqToPlay = 0;
                }
                else
                {
                    if (_curMusicState != 0)
                    {
                        PlayComiMusic(_comiStateMusicTable[_curMusicState].name, _comiStateMusicTable[_curMusicState], _curMusicState, true);
                    }
                    else
                        PlayComiMusic(null, _comiStateMusicTable[0], _curMusicState, true);
                    num = 0;
                }
            }

            _curMusicSeq = num;
        }

        void PlayComiMusic(string songName, ImuseComiTable table, int attribPos, bool sequence)
        {
            int hookId = 0;

            if ((songName != null) && (attribPos != 0))
            {
                if (table.attribPos != 0)
                    attribPos = table.attribPos;
                hookId = _attributes[COMI_STATE_OFFSET + attribPos];
                if (table.hookId != 0)
                {
                    if ((hookId != 0) && (table.hookId > 1))
                    {
                        _attributes[COMI_STATE_OFFSET + attribPos] = 2;
                    }
                    else
                    {
                        _attributes[COMI_STATE_OFFSET + attribPos] = hookId + 1;
                        if (table.hookId < hookId + 1)
                            _attributes[COMI_STATE_OFFSET + attribPos] = 1;
                    }
                }
            }

            if (string.IsNullOrEmpty(songName))
            {
                FadeOutMusic(120);
                return;
            }

            switch (table.transitionType)
            {
                case 0:
                    break;
                case 8:
                    SetHookIdForMusic(table.hookId);
                    break;
                case 9:
                    _stopingSequence = 1;
                    SetHookIdForMusic(table.hookId);
                    break;
                case 2:
                case 3:
                case 4:
                case 12:
                    if (table.filename[0] == 0)
                    {
                        FadeOutMusic(60);
                        return;
                    }
                    if (GetCurMusicSoundId() == table.soundId)
                        return;
                    if (table.transitionType == 4)
                        _stopingSequence = 1;
                    if (table.transitionType == 2)
                    {
                        FadeOutMusic(table.fadeOutDelay);
                        StartMusic(table.filename, table.soundId, table.hookId, 127);
                        return;
                    }
                    if ((!sequence) && (table.attribPos != 0) &&
                        (table.attribPos == _comiStateMusicTable[_curMusicState].attribPos))
                    {
                        FadeOutMusicAndStartNew(table.fadeOutDelay, table.filename, table.soundId);
                    }
                    else if (table.transitionType == 12)
                    {
                        TriggerParams trigger;
                        trigger.marker = "exit";
                        trigger.fadeOutDelay = table.fadeOutDelay;
                        trigger.filename = table.filename;
                        trigger.soundId = table.soundId;
                        trigger.hookId = table.hookId;
                        trigger.volume = 127;
                        SetTrigger(trigger);
                    }
                    else
                    {
                        FadeOutMusic(table.fadeOutDelay);
                        StartMusic(table.filename, table.soundId, hookId, 127);
                    }
                    break;
            }
        }

        void SetDigMusicState(int stateId)
        {
            int l, num = -1;

            for (l = 0; _digStateMusicTable[l].soundId != -1; l++)
            {
                if ((_digStateMusicTable[l].soundId == stateId))
                {
                    Debug.WriteLine("Set music state: {0}, {1}", _digStateMusicTable[l].name, _digStateMusicTable[l].filename);
                    num = l;
                    break;
                }
            }

            if (num == -1)
            {
                for (l = 0; _digStateMusicMap[l].roomId != -1; l++)
                {
                    if ((_digStateMusicMap[l].roomId == stateId))
                    {
                        break;
                    }
                }
                num = l;

                int offset = _attributes[_digStateMusicMap[num].offset];
                if (offset == 0)
                {
                    if (_attributes[_digStateMusicMap[num].attribPos] != 0)
                    {
                        num = _digStateMusicMap[num].stateIndex3;
                    }
                    else
                    {
                        num = _digStateMusicMap[num].stateIndex1;
                    }
                }
                else
                {
                    int stateIndex2 = _digStateMusicMap[num].stateIndex2;
                    if (stateIndex2 == 0)
                    {
                        num = _digStateMusicMap[num].stateIndex1 + offset;
                    }
                    else
                    {
                        num = stateIndex2;
                    }
                }
            }

            Debug.WriteLine("Set music state: {0}, {1}", _digStateMusicTable[num].name, _digStateMusicTable[num].filename);

            if (_curMusicState == num)
                return;

            if (_curMusicSeq == 0)
            {
                if (num == 0)
                    PlayDigMusic(null, _digStateMusicTable[0], num, false);
                else
                    PlayDigMusic(_digStateMusicTable[num].name, _digStateMusicTable[num], num, false);
            }

            _curMusicState = num;
        }

        void SetDigMusicSequence(int seqId)
        {
            int num = -1;

            if (seqId == 0)
                seqId = 2000;

            for (var l = 0; _digSeqMusicTable[l].soundId != -1; l++)
            {
                if ((_digSeqMusicTable[l].soundId == seqId))
                {
                    Debug.WriteLine("Set music sequence: {0}, {1}", _digSeqMusicTable[l].name, _digSeqMusicTable[l].filename);
                    num = l;
                    break;
                }
            }

            if (num == -1)
                return;

            if (_curMusicSeq == num)
                return;

            if (num != 0)
            {
                if (_curMusicSeq != 0 && ((_digSeqMusicTable[_curMusicSeq].transitionType == 4)
                    || (_digSeqMusicTable[_curMusicSeq].transitionType == 6)))
                {
                    _nextSeqToPlay = num;
                    return;
                }
                else
                {
                    PlayDigMusic(_digSeqMusicTable[num].name, _digSeqMusicTable[num], 0, true);
                    _nextSeqToPlay = 0;
                    _attributes[DIG_SEQ_OFFSET + num] = 1; // _attributes[COMI_SEQ_OFFSET] in Comi are not used as it doesn't have 'room' attributes table
                }
            }
            else
            {
                if (_nextSeqToPlay != 0)
                {
                    PlayDigMusic(_digSeqMusicTable[_nextSeqToPlay].name, _digSeqMusicTable[_nextSeqToPlay], 0, true);
                    _attributes[DIG_SEQ_OFFSET + _nextSeqToPlay] = 1; // _attributes[COMI_SEQ_OFFSET] in Comi are not used as it doesn't have 'room' attributes table
                    num = _nextSeqToPlay;
                    _nextSeqToPlay = 0;
                }
                else
                {
                    if (_curMusicState != 0)
                    {
                        PlayDigMusic(_digStateMusicTable[_curMusicState].name, _digStateMusicTable[_curMusicState], _curMusicState, true);
                    }
                    else
                        PlayDigMusic(null, _digStateMusicTable[0], _curMusicState, true);
                    num = 0;
                }
            }

            _curMusicSeq = num;
        }

        void PlayDigMusic(string songName, ImuseDigTable table, int attribPos, bool sequence)
        {
            int hookId = 0;

            if (songName != null)
            {
                if ((_attributes[DIG_SEQ_OFFSET + 38] != 0) && (_attributes[DIG_SEQ_OFFSET + 41] == 0))
                {
                    if ((attribPos == 43) || (attribPos == 44))
                        hookId = 3;
                }

                if ((_attributes[DIG_SEQ_OFFSET + 46] != 0) && (_attributes[DIG_SEQ_OFFSET + 48] == 0))
                {
                    if ((attribPos == 38) || (attribPos == 39))
                        hookId = 3;
                }

                if ((_attributes[DIG_SEQ_OFFSET + 53] != 0))
                {
                    if ((attribPos == 50) || (attribPos == 51))
                        hookId = 3;
                }

                if ((attribPos != 0) && (hookId == 0))
                {
                    if (table.attribPos != 0)
                        attribPos = table.attribPos;
                    hookId = _attributes[DIG_STATE_OFFSET + attribPos];
                    if (table.hookId != 0)
                    {
                        if ((hookId != 0) && (table.hookId > 1))
                        {
                            _attributes[DIG_STATE_OFFSET + attribPos] = 2;
                        }
                        else
                        {
                            _attributes[DIG_STATE_OFFSET + attribPos] = hookId + 1;
                            if (table.hookId < hookId + 1)
                                _attributes[DIG_STATE_OFFSET + attribPos] = 1;
                        }
                    }
                }
            }

            if (songName == null)
            {
                FadeOutMusic(120);
                return;
            }

            switch (table.transitionType)
            {
                case 0:
                case 5:
                    break;
                case 3:
                case 4:
                    if (table.filename[0] == 0)
                    {
                        FadeOutMusic(60);
                        return;
                    }
                    if (table.transitionType == 4)
                        _stopingSequence = 1;
                    if ((!sequence) && (table.attribPos != 0) &&
                        (table.attribPos == _digStateMusicTable[_curMusicState].attribPos))
                    {
                        FadeOutMusicAndStartNew(108, table.filename, table.soundId);
                    }
                    else
                    {
                        FadeOutMusic(108);
                        StartMusic(table.filename, table.soundId, hookId, 127);
                    }
                    break;
                case 6:
                    _stopingSequence = 1;
                    break;
            }
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

