//
//  TownsAudioInterface.cs
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

namespace NScumm.Core.Audio.SoftSynth
{
    class TownsAudio_PcmChannel
    {
        public void Clear()
        {
            _curInstrument = null;
            _note = _tl = _level = _velo = 0;

            _data = null;
            _dataEnd = 0;
            _loopLen = 0;

            _pos = 0;
            _loopEnd = 0;

            _step = 0;
            _stepNote = 0x4000;
            _stepPitch = 0x4000;

            _panLeft = _panRight = 7;

            _envTotalLevel = _envAttackRate = _envDecayRate = _envSustainLevel = _envSustainRate = _envReleaseRate = 0;
            _envStep = _envCurrentLevel = 0;

            _envState = EnvelopeState.Ready;

            _activeKey = _activeEffect = _activeOutput = _keyPressed = _reserved = false;

            _extData = null;
        }

        public void LoadData(TownsAudio_WaveTable w)
        {
            _data = w.data;
            _dataEnd = w.size;
        }

        public void LoadData(byte[] buffer, int offset, int size)
        {
            _extData = new sbyte[size];
            for (var i = 0; i < size; i++)
                _extData[i] = ((buffer[offset + i] & 0x80) != 0) ? (sbyte)(buffer[offset + i] & 0x7f) : (sbyte)-buffer[offset + i];

            _data = _extData;
            _dataEnd = size;
            _pos = 0;
        }

        public int InitInstrument(ref byte note, TownsAudio_WaveTable[] tables, int numTables, out TownsAudio_WaveTable table)
        {
            int i = 0;
            table = null;
            for (; i < 8; i++)
            {
                if (note <= _curInstrument[16 + 2 * i])
                    break;
            }

            if (i == 8)
                return 8;

            var d = (i << 3) + 64;
            _envTotalLevel = _curInstrument[d];
            _envAttackRate = _curInstrument[d + 1];
            _envDecayRate = _curInstrument[d + 2];
            _envSustainLevel = _curInstrument[d + 3];
            _envSustainRate = _curInstrument[d + 4];
            _envReleaseRate = _curInstrument[d + 5];
            _envStep = 0;
            note += _curInstrument[d + 6];

            int id = _curInstrument.ToInt32(i * 4 + 32);

            for (i = 0; i < numTables; i++)
            {
                if (id == tables[i].id)
                    break;
            }

            if (i == numTables)
                return 9;

            table = tables[i];
            return 0;
        }

        public void KeyOn(byte note, byte velo, TownsAudio_WaveTable w)
        {
            SetupLoop(w.loopStart, w.loopLen);
            SetNote(note, w, _reserved);
            SetVelo(velo);

            if (_reserved)
                _activeEffect = true;
            else
                _keyPressed = _activeKey = true;

            _activeOutput = true;
        }

        public void KeyOff()
        {
            _keyPressed = false;
            EnvRelease();
        }

        public void UpdateEnvelopeGenerator()
        {
            if (_envCurrentLevel == 0)
            {
                _activeKey = false;
                _envState = EnvelopeState.Ready;
            }

            if (!_activeKey)
                return;

            switch (_envState)
            {
                case EnvelopeState.Attacking:
                    if (((_envCurrentLevel + _envStep) >> 8) > _envTotalLevel)
                    {
                        EnvDecay();
                        return;
                    }
                    else
                    {
                        _envCurrentLevel += _envStep;
                    }
                    break;

                case EnvelopeState.Decaying:
                    if (((_envCurrentLevel - _envStep) >> 8) < _envSustainLevel)
                    {
                        EnvSustain();
                        return;
                    }
                    else
                    {
                        _envCurrentLevel -= _envStep;
                    }
                    break;

                case EnvelopeState.Sustaining:
                case EnvelopeState.Releasing:
                    _envCurrentLevel -= _envStep;
                    if (_envCurrentLevel <= 0)
                        _envCurrentLevel = 0;
                    break;
            }
            _tl = (byte)((_envCurrentLevel >> 8) << 1);
        }

        public void SetInstrument(byte[] instr)
        {
            _curInstrument = instr;
        }

        public void SetLevel(int lvl)
        {
            if (_reserved)
            {
                _velo = (byte)lvl;
                _tl = (byte)(lvl << 1);
            }
            else
            {
                int t = _envStep * lvl;
                if (_level != 0)
                    t /= _level;
                _envStep = (short)t;
                t = _envCurrentLevel * lvl;
                if (_level != 0)
                    t /= _level;
                _envCurrentLevel = (short)t;
                _level = (byte)lvl;
                _tl = (byte)(_envCurrentLevel >> 8);
            }
        }

        public void SetPitch(uint pt)
        {
            _stepPitch = (ushort)(pt & 0xffff);
            _step = (ushort)((_stepNote * _stepPitch) >> 14);

            //  if (_pcmChanUnkFlag & _chanFlags[chan])
            //       unk[chan] = (((p->step * 1000) << 11) / 98) / 20833;

            /*else*/
            if (_activeEffect && (_step > 2048))
                _step = 2048;
        }

        public void SetBalance(byte blc)
        {
            _panLeft = (byte)(blc & 0x0f);
            _panRight = (byte)(blc >> 4);
        }

        public void UpdateOutput()
        {
            if (_activeKey || _activeEffect)
            {
                _pos += _step;

                if ((_pos >> 11) >= _loopEnd)
                {
                    if (_loopLen != 0)
                    {
                        _pos -= _loopLen;
                    }
                    else
                    {
                        _pos = 0;
                        _activeKey = _activeEffect = false;
                    }
                }
            }
        }

        public int CurrentSampleLeft()
        {
            return (_activeOutput && _panLeft != 0) ? (((_data[_pos >> 11] * _tl) * _panLeft) >> 3) : 0;
        }

        public int CurrentSampleRight()
        {
            return (_activeOutput && _panRight != 0) ? (((_data[_pos >> 11] * _tl) * _panRight) >> 3) : 0;
        }

        void EnvAttack()
        {
            _envState = EnvelopeState.Attacking;
            short t = (short)(_envTotalLevel << 8);
            if (_envAttackRate == 127)
            {
                _envCurrentLevel = _envStep = 0;
            }
            else if (_envAttackRate != 0)
            {
                _envStep = (short)(t / _envAttackRate);
                _envCurrentLevel = 1;
            }
            else
            {
                _envCurrentLevel = t;
                EnvDecay();
            }
        }

        void EnvDecay()
        {
            _envState = EnvelopeState.Decaying;
            short t = (short)(_envTotalLevel - _envSustainLevel);
            if (t < 0 || _envDecayRate == 127)
            {
                _envStep = 0;
            }
            else if (_envDecayRate != 0)
            {
                _envStep = (short)((t << 8) / _envDecayRate);
            }
            else
            {
                _envCurrentLevel = (short)(_envSustainLevel << 8);
                EnvSustain();
            }
        }

        void EnvSustain()
        {
            _envState = EnvelopeState.Sustaining;
            if (_envSustainLevel != 0 && _envSustainRate != 0)
                _envStep = (short)((_envSustainRate == 127) ? 0 : (_envCurrentLevel / _envSustainRate) >> 1);
            else
                _envStep = _envCurrentLevel = 1;
        }

        void EnvRelease()
        {
            _envState = EnvelopeState.Releasing;
            if (_envReleaseRate == 127)
                _envStep = 0;
            else if (_envReleaseRate != 0)
                _envStep = (short)(_envCurrentLevel / _envReleaseRate);
            else
                _envStep = _envCurrentLevel = 1;
        }

        void SetupLoop(uint loopStart, uint len)
        {
            _loopLen = len << 11;
            _loopEnd = ((_loopLen == 0) ? _dataEnd : (int)(loopStart + _loopLen) >> 11);
            _pos = loopStart;
        }

        void SetNote(byte note, TownsAudio_WaveTable w, bool stepLimit)
        {
            _note = note;
            sbyte diff = (sbyte)(_note - w.baseNote);
            ushort r = (ushort)(w.rate + w.rateOffs);
            ushort bl = 0;
            uint s = 0;

            if (diff < 0)
            {
                diff *= -1;
                bl = (ushort)(diff % 12);
                diff /= 12;
                s = (uint)(r >> diff);
                if (bl != 0)
                    s = (s * _pcmPhase2[bl]) >> 16;

            }
            else if (diff > 0)
            {
                bl = (ushort)(diff % 12);
                diff /= 12;
                s = (uint)(r << diff);
                if (bl != 0)
                    s += ((s * _pcmPhase1[bl]) >> 16);

            }
            else
            {
                s = r;
            }

            _stepNote = (ushort)(s & 0xffff);
            _step = (ushort)((s * _stepPitch) >> 14);

            if (stepLimit && _step > 2048)
                _step = 2048;
        }

        void SetVelo(byte velo)
        {
            if (_reserved)
            {
                _velo = velo;
                _tl = (byte)(velo << 1);
            }
            else
            {
                _velo = velo;
                uint lvl = (uint)(_level * _velo);
                _envTotalLevel = (byte)(((_envTotalLevel * lvl) >> 14) & 0xff);
                _envSustainLevel = (byte)(((_envSustainLevel * lvl) >> 14) & 0xff);
                EnvAttack();
                _tl = (byte)((_envCurrentLevel >> 8) << 1);
            }
        }

        public bool _keyPressed;
        public bool _reserved;
        public bool _activeKey;
        public bool _activeEffect;
        public bool _activeOutput;

        byte[] _curInstrument;

        byte _note;

        byte _velo;
        byte _level;
        byte _tl;

        byte _panLeft;
        byte _panRight;

        sbyte[] _data;
        int _dataEnd;

        int _loopEnd;
        uint _loopLen;

        ushort _stepNote;
        ushort _stepPitch;
        ushort _step;

        uint _pos;

        int _envTotalLevel;
        int _envAttackRate;
        int _envDecayRate;
        int _envSustainLevel;
        int _envSustainRate;
        int _envReleaseRate;
        short _envStep;
        short _envCurrentLevel;

        EnvelopeState _envState;

        sbyte[] _extData;

        static readonly ushort[] _pcmPhase1 =
            {
                0x879B, 0x0F37, 0x1F58, 0x306E, 0x4288, 0x55B6, 0x6A08, 0x7F8F, 0x965E, 0xAE88, 0xC882, 0xE341
            };

        static readonly ushort[] _pcmPhase2 =
            {
                0xFEFE, 0xF1A0, 0xE411, 0xD744, 0xCB2F, 0xBFC7, 0xB504, 0xAAE2, 0xA144, 0x9827, 0x8FAC
            };

    }
}

