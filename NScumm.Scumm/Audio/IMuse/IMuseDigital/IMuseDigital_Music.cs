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

using NScumm.Core;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    partial class IMuseDigital: IEnableTrace
    {
        const int DigStateOffset = 11;
        const int DigSequenceOffset = (DigStateOffset + 65);
        const int ComiStateOffset = 3;

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

            for (l = 0; _comiStateMusicTable[l].SoundId != -1; l++)
            {
                if ((_comiStateMusicTable[l].SoundId == stateId))
                {
                    this.Trace().Write(TraceSwitches.Music, "Set music state: {0}, {1}", _comiStateMusicTable[l].Name, _comiStateMusicTable[l].Filename);
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
                    PlayComiMusic(_comiStateMusicTable[num].Name, _comiStateMusicTable[num], num, false);
            }

            _curMusicState = num;
        }

        void SetComiMusicSequence(int seqId)
        {
            int l, num = -1;

            if (seqId == 0)
                seqId = 2000;

            for (l = 0; _comiSeqMusicTable[l].SoundId != -1; l++)
            {
                if ((_comiSeqMusicTable[l].SoundId == seqId))
                {
                    this.Trace().Write(TraceSwitches.Music, "Set music sequence: {0}, {1}", _comiSeqMusicTable[l].Name, _comiSeqMusicTable[l].Filename);
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
                if (_curMusicSeq != 0 && ((_comiSeqMusicTable[_curMusicSeq].TransitionType == 4)
                    || (_comiSeqMusicTable[_curMusicSeq].TransitionType == 6)))
                {
                    _nextSeqToPlay = num;
                    return;
                }
                else
                {
                    PlayComiMusic(_comiSeqMusicTable[num].Name, _comiSeqMusicTable[num], 0, true);
                    _nextSeqToPlay = 0;
                }
            }
            else
            {
                if (_nextSeqToPlay != 0)
                {
                    PlayComiMusic(_comiSeqMusicTable[_nextSeqToPlay].Name, _comiSeqMusicTable[_nextSeqToPlay], 0, true);
                    num = _nextSeqToPlay;
                    _nextSeqToPlay = 0;
                }
                else
                {
                    if (_curMusicState != 0)
                    {
                        PlayComiMusic(_comiStateMusicTable[_curMusicState].Name, _comiStateMusicTable[_curMusicState], _curMusicState, true);
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
                if (table.AttribPos != 0)
                    attribPos = table.AttribPos;
                hookId = _attributes[ComiStateOffset + attribPos];
                if (table.HookId != 0)
                {
                    if ((hookId != 0) && (table.HookId > 1))
                    {
                        _attributes[ComiStateOffset + attribPos] = 2;
                    }
                    else
                    {
                        _attributes[ComiStateOffset + attribPos] = hookId + 1;
                        if (table.HookId < hookId + 1)
                            _attributes[ComiStateOffset + attribPos] = 1;
                    }
                }
            }

            if (string.IsNullOrEmpty(songName))
            {
                FadeOutMusic(120);
                return;
            }

            switch (table.TransitionType)
            {
                case 0:
                    break;
                case 8:
                    SetHookIdForMusic(table.HookId);
                    break;
                case 9:
                    _stopingSequence = 1;
                    SetHookIdForMusic(table.HookId);
                    break;
                case 2:
                case 3:
                case 4:
                case 12:
                    if (table.Filename[0] == 0)
                    {
                        FadeOutMusic(60);
                        return;
                    }
                    if (GetCurMusicSoundId() == table.SoundId)
                        return;
                    if (table.TransitionType == 4)
                        _stopingSequence = 1;
                    if (table.TransitionType == 2)
                    {
                        FadeOutMusic(table.FadeOutDelay);
                        StartMusic(table.Filename, table.SoundId, table.HookId, 127);
                        return;
                    }
                    if ((!sequence) && (table.AttribPos != 0) &&
                        (table.AttribPos == _comiStateMusicTable[_curMusicState].AttribPos))
                    {
                        FadeOutMusicAndStartNew(table.FadeOutDelay, table.Filename, table.SoundId);
                    }
                    else if (table.TransitionType == 12)
                    {
                        TriggerParams trigger;
                        trigger.Marker = "exit";
                        trigger.FadeOutDelay = table.FadeOutDelay;
                        trigger.Filename = table.Filename;
                        trigger.SoundId = table.SoundId;
                        trigger.HookId = table.HookId;
                        trigger.Volume = 127;
                        SetTrigger(trigger);
                    }
                    else
                    {
                        FadeOutMusic(table.FadeOutDelay);
                        StartMusic(table.Filename, table.SoundId, hookId, 127);
                    }
                    break;
            }
        }

        void SetDigMusicState(int stateId)
        {
            int l, num = -1;

            for (l = 0; _digStateMusicTable[l].SoundId != -1; l++)
            {
                if ((_digStateMusicTable[l].SoundId == stateId))
                {
                    this.Trace().Write(TraceSwitches.Music, "Set music state: {0}, {1}", _digStateMusicTable[l].Name, _digStateMusicTable[l].Filename);
                    num = l;
                    break;
                }
            }

            if (num == -1)
            {
                for (l = 0; _digStateMusicMap[l].RoomId != -1; l++)
                {
                    if ((_digStateMusicMap[l].RoomId == stateId))
                    {
                        break;
                    }
                }
                num = l;

                int offset = _attributes[_digStateMusicMap[num].Offset];
                if (offset == 0)
                {
                    if (_attributes[_digStateMusicMap[num].AttribPos] != 0)
                    {
                        num = _digStateMusicMap[num].StateIndex3;
                    }
                    else
                    {
                        num = _digStateMusicMap[num].StateIndex1;
                    }
                }
                else
                {
                    int stateIndex2 = _digStateMusicMap[num].StateIndex2;
                    if (stateIndex2 == 0)
                    {
                        num = _digStateMusicMap[num].StateIndex1 + offset;
                    }
                    else
                    {
                        num = stateIndex2;
                    }
                }
            }

            this.Trace().Write(TraceSwitches.Music, "Set music state: {0}, {1}", _digStateMusicTable[num].Name, _digStateMusicTable[num].Filename);

            if (_curMusicState == num)
                return;

            if (_curMusicSeq == 0)
            {
                if (num == 0)
                    PlayDigMusic(null, _digStateMusicTable[0], num, false);
                else
                    PlayDigMusic(_digStateMusicTable[num].Name, _digStateMusicTable[num], num, false);
            }

            _curMusicState = num;
        }

        void SetDigMusicSequence(int seqId)
        {
            int num = -1;

            if (seqId == 0)
                seqId = 2000;

            for (var l = 0; _digSeqMusicTable[l].SoundId != -1; l++)
            {
                if ((_digSeqMusicTable[l].SoundId == seqId))
                {
                    this.Trace().Write(TraceSwitches.Music, "Set music sequence: {0}, {1}", _digSeqMusicTable[l].Name, _digSeqMusicTable[l].Filename);
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
                if (_curMusicSeq != 0 && ((_digSeqMusicTable[_curMusicSeq].TransitionType == 4)
                    || (_digSeqMusicTable[_curMusicSeq].TransitionType == 6)))
                {
                    _nextSeqToPlay = num;
                    return;
                }
                else
                {
                    PlayDigMusic(_digSeqMusicTable[num].Name, _digSeqMusicTable[num], 0, true);
                    _nextSeqToPlay = 0;
                    _attributes[DigSequenceOffset + num] = 1; // _attributes[COMI_SEQ_OFFSET] in Comi are not used as it doesn't have 'room' attributes table
                }
            }
            else
            {
                if (_nextSeqToPlay != 0)
                {
                    PlayDigMusic(_digSeqMusicTable[_nextSeqToPlay].Name, _digSeqMusicTable[_nextSeqToPlay], 0, true);
                    _attributes[DigSequenceOffset + _nextSeqToPlay] = 1; // _attributes[COMI_SEQ_OFFSET] in Comi are not used as it doesn't have 'room' attributes table
                    num = _nextSeqToPlay;
                    _nextSeqToPlay = 0;
                }
                else
                {
                    if (_curMusicState != 0)
                    {
                        PlayDigMusic(_digStateMusicTable[_curMusicState].Name, _digStateMusicTable[_curMusicState], _curMusicState, true);
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
                if ((_attributes[DigSequenceOffset + 38] != 0) && (_attributes[DigSequenceOffset + 41] == 0))
                {
                    if ((attribPos == 43) || (attribPos == 44))
                        hookId = 3;
                }

                if ((_attributes[DigSequenceOffset + 46] != 0) && (_attributes[DigSequenceOffset + 48] == 0))
                {
                    if ((attribPos == 38) || (attribPos == 39))
                        hookId = 3;
                }

                if ((_attributes[DigSequenceOffset + 53] != 0))
                {
                    if ((attribPos == 50) || (attribPos == 51))
                        hookId = 3;
                }

                if ((attribPos != 0) && (hookId == 0))
                {
                    if (table.AttribPos != 0)
                        attribPos = table.AttribPos;
                    hookId = _attributes[DigStateOffset + attribPos];
                    if (table.HookId != 0)
                    {
                        if ((hookId != 0) && (table.HookId > 1))
                        {
                            _attributes[DigStateOffset + attribPos] = 2;
                        }
                        else
                        {
                            _attributes[DigStateOffset + attribPos] = hookId + 1;
                            if (table.HookId < hookId + 1)
                                _attributes[DigStateOffset + attribPos] = 1;
                        }
                    }
                }
            }

            if (songName == null)
            {
                FadeOutMusic(120);
                return;
            }

            switch (table.TransitionType)
            {
                case 0:
                case 5:
                    break;
                case 3:
                case 4:
                    if (table.Filename[0] == 0)
                    {
                        FadeOutMusic(60);
                        return;
                    }
                    if (table.TransitionType == 4)
                        _stopingSequence = 1;
                    if ((!sequence) && (table.AttribPos != 0) &&
                        (table.AttribPos == _digStateMusicTable[_curMusicState].AttribPos))
                    {
                        FadeOutMusicAndStartNew(108, table.Filename, table.SoundId);
                    }
                    else
                    {
                        FadeOutMusic(108);
                        StartMusic(table.Filename, table.SoundId, hookId, 127);
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

            this.Trace().Write(TraceSwitches.Music, "State music: {0}, {1}", _ftStateMusicTable[stateId].Name, _ftStateMusicTable[stateId].AudioName);

            if (_curMusicState == stateId)
                return;

            if (_curMusicSeq == 0)
            {
                if (stateId == 0)
                    PlayFtMusic(null, 0, 0);
                else
                    PlayFtMusic(_ftStateMusicTable[stateId].AudioName, _ftStateMusicTable[stateId].TransitionType, _ftStateMusicTable[stateId].Volume);
            }

            _curMusicState = stateId;
        }

        void SetFtMusicSequence(int seqId)
        {
            if (seqId > 52)
                return;

            this.Trace().Write(TraceSwitches.Music, "Sequence music: {0}", _ftSeqNames[seqId]);

            if (_curMusicSeq == seqId)
                return;

            if (seqId == 0)
            {
                if (_curMusicState == 0)
                    PlayFtMusic(null, 0, 0);
                else
                {
                    PlayFtMusic(_ftStateMusicTable[_curMusicState].AudioName, _ftStateMusicTable[_curMusicState].TransitionType, _ftStateMusicTable[_curMusicState].Volume);
                }
            }
            else
            {
                int seq = (seqId - 1) * 4;
                PlayFtMusic(_ftSeqMusicTable[seq].AudioName, _ftSeqMusicTable[seq].TransitionType, _ftSeqMusicTable[seq].Volume);
            }

            _curMusicSeq = seqId;
            _curMusicCue = 0;
        }

        void SetFtMusicCuePoint(int cueId)
        {
            if (cueId > 3)
                return;

            this.Trace().Write(TraceSwitches.Music, "Cue point sequence: {0}", cueId);

            if (_curMusicSeq == 0)
                return;

            if (_curMusicCue == cueId)
                return;

            if (cueId == 0)
                PlayFtMusic(null, 0, 0);
            else
            {
                int seq = ((_curMusicSeq - 1) * 4) + cueId;
                PlayFtMusic(_ftSeqMusicTable[seq].AudioName, _ftSeqMusicTable[seq].TransitionType, _ftSeqMusicTable[seq].Volume);
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

