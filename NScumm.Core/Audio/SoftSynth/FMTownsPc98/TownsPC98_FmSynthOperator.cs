//
//  TownsPC98_FmSynthOperator.cs
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

namespace NScumm.Core
{
    enum EnvelopeState
    {
        Ready = 0,
        Attacking,
        Decaying,
        Sustaining,
        Releasing
    }

    public class TownsPC98_FmSynthOperator
    {
        public TownsPC98_FmSynthOperator(uint timerbase, uint rtt,
                                         byte[] rateTable, byte[] shiftTable, byte[] attackDecayTable,
                                         uint[] frqTable, uint[] sineTable, int[] tlevelOut, int[][] detuneTable)
        {
            _rtt = rtt;
            _rateTbl = rateTable;
            _rshiftTbl = shiftTable;
            _adTbl = attackDecayTable;
            _fTbl = frqTable;
            _sinTbl = sineTable;
            _tLvlTbl = tlevelOut;
            _detnTbl = detuneTable;
            _tickLength = timerbase * 2;

        
            _state = EnvelopeState.Ready;
            _currentLevel = 1023;

            fs_a = new EvpState();
            fs_d = new EvpState();
            fs_r = new EvpState();
            fs_s = new EvpState();
            fs_a.rate = fs_a.shift = fs_d.rate = fs_d.shift = fs_s.rate = fs_s.shift = fs_r.rate = fs_r.shift = 0;

            Reset();
        }

        public void Reset()
        {
            KeyOff();
            _timer = 0;
            _keyScale2 = 0;
            _currentLevel = 1023;

            Frequency(0);
            Detune(0);
            ScaleRate(0);
            Multiple(0);
            UpdatePhaseIncrement();
            AttackRate(0);
            DecayRate(0);
            ReleaseRate(0);
            SustainRate(0);
            FeedbackLevel(0);
            TotalLevel(127);
            AmpModulation(false);
        }

        public void KeyOn()
        {
            if (_holdKey)
                return;

            _holdKey = true;
            _state = EnvelopeState.Attacking;
            _phase = 0;
        }

        public void KeyOff()
        {
            if (!_holdKey)
                return;

            _holdKey = false;
            if (_state != EnvelopeState.Ready)
                _state = EnvelopeState.Releasing;
        }

        public void TotalLevel(uint value)
        {
            _totalLevel = value << 3;
        }

        public void AmpModulation(bool enable)
        {
            _ampMod = enable;
        }

        public void DecayRate(uint value)
        {
            _specifiedDecayRate = value;
            RecalculateRates();
        }

        public void SustainRate(uint value)
        {
            _specifiedSustainRate = value;
            RecalculateRates();
        }

        public void SustainLevel(uint value)
        {
            _sustainLevel = (value == 0x0f) ? 0x3e0 : value << 5;
        }

        public void ReleaseRate(uint value)
        {
            _specifiedReleaseRate = value;
            RecalculateRates();
        }

        public bool ScaleRate(byte value)
        {
            value = (byte)(3 - value);
            if (_keyScale1 != value)
            {
                _keyScale1 = value;
                return true;
            }

            int k = _keyScale2;
            int r = _specifiedAttackRate != 0 ? (int)((_specifiedAttackRate << 1) + 0x20) : 0;
            fs_a.rate = ((r + k) < 94) ? _rateTbl[r + k] : (byte)136;
            fs_a.shift = ((r + k) < 94) ? _rshiftTbl[r + k] : (byte)0;
            return false;
        }

        public void Frequency(int freq)
        {
            byte block = (byte)(freq >> 11);
            ushort pos = (ushort)(freq & 0x7ff);
            byte c = (byte)(pos >> 7);

            _kcode = (byte)((block << 2) | ((c < 7) ? 0 : ((c > 8) ? 3 : c - 6)));
            _frequency = _fTbl[pos << 1] >> (7 - block);
        }

        public void FeedbackLevel(int level)
        {
            _feedbackLevel = level != 0 ? (uint)(level + 6) : 0;
        }

        public void Detune(int value)
        {
            _detn = _detnTbl[value << 5];
        }

        public void Multiple(uint value)
        {
            _multiple = value != 0 ? (value << 1) : 1;
        }

        public void AttackRate(uint value)
        {
            _specifiedAttackRate = value;
        }

        void RecalculateRates()
        {
            int k = _keyScale2;
            int r = (_specifiedAttackRate != 0) ? (int)((_specifiedAttackRate << 1) + 0x20) : 0;
            fs_a.rate = ((r + k) < 94) ? _rateTbl[r + k] : (byte)136;
            fs_a.shift = ((r + k) < 94) ? _rshiftTbl[r + k] : (byte)0;

            r = (_specifiedDecayRate != 0) ? (int)((_specifiedDecayRate << 1) + 0x20) : 0;
            fs_d.rate = _rateTbl[r + k];
            fs_d.shift = _rshiftTbl[r + k];

            r = (_specifiedSustainRate != 0) ? (int)((_specifiedSustainRate << 1) + 0x20) : 0;
            fs_s.rate = _rateTbl[r + k];
            fs_s.shift = _rshiftTbl[r + k];

            r = (int)((_specifiedReleaseRate << 2) + 0x22);
            fs_r.rate = _rateTbl[r + k];
            fs_r.shift = _rshiftTbl[r + k];
        }

        public void UpdatePhaseIncrement()
        {
            _phaseIncrement = (uint)(((_frequency + _detn[_kcode]) * _multiple) >> 1);
            byte keyscale = (byte)(_kcode >> _keyScale1);
            if (_keyScale2 != keyscale)
            {
                _keyScale2 = keyscale;
                RecalculateRates();
            }
        }

        public void GenerateOutput(int phasebuf, int[] feed, ref int @out)
        {
            if (_state == EnvelopeState.Ready)
                return;

            _timer += _tickLength;
            while (_timer > _rtt)
            {
                _timer -= _rtt;
                ++_tickCount;

                int levelIncrement = 0;
                uint targetTime = 0;
                int targetLevel = 0;
                EnvelopeState nextState = EnvelopeState.Ready;

                for (bool loop = true; loop;)
                {
                    switch (_state)
                    {
                        case EnvelopeState.Ready:
                            return;
                        case EnvelopeState.Attacking:
                            targetLevel = 0;
                            nextState = _sustainLevel != 0 ? EnvelopeState.Decaying : EnvelopeState.Sustaining;
                            if ((_specifiedAttackRate << 1) + _keyScale2 < 62)
                            {
                                targetTime = (uint)((1 << fs_a.shift) - 1);
                                levelIncrement = (~_currentLevel * _adTbl[fs_a.rate + ((_tickCount >> fs_a.shift) & 7)]) >> 4;
                            }
                            else
                            {
                                _currentLevel = targetLevel;
                                _state = nextState;
                                continue;
                            }
                            break;
                        case EnvelopeState.Decaying:
                            targetTime = (uint)((1 << fs_d.shift) - 1);
                            nextState = EnvelopeState.Sustaining;
                            targetLevel = (int)_sustainLevel;
                            levelIncrement = _adTbl[fs_d.rate + ((_tickCount >> fs_d.shift) & 7)];
                            break;
                        case EnvelopeState.Sustaining:
                            targetTime = (uint)((1 << fs_s.shift) - 1);
                            nextState = EnvelopeState.Sustaining;
                            targetLevel = 1023;
                            levelIncrement = _adTbl[fs_s.rate + ((_tickCount >> fs_s.shift) & 7)];
                            break;
                        case EnvelopeState.Releasing:
                            targetTime = (uint)((1 << fs_r.shift) - 1);
                            nextState = EnvelopeState.Ready;
                            targetLevel = 1023;
                            levelIncrement = _adTbl[fs_r.rate + ((_tickCount >> fs_r.shift) & 7)];
                            break;
                    }
                    loop = false;
                }

                if ((_tickCount & targetTime) == 0)
                {
                    _currentLevel += levelIncrement;
                    if ((_state == EnvelopeState.Attacking && _currentLevel <= targetLevel) || (_state != EnvelopeState.Attacking && _currentLevel >= targetLevel))
                    {
                        if (_state != EnvelopeState.Decaying)
                            _currentLevel = targetLevel;
                        _state = nextState;
                    }
                }
            }

            uint lvlout = _totalLevel + (uint)_currentLevel;


            int outp = 0;
            Func<int> ir = () => outp;
            Action<int> iw = v => outp = v;
            Func<int> or = () => outp;
            Action<int> ow = v => outp = v;
            int phaseShift = 0;

            if (feed != null)
            {
                or = () => feed[0];
                ow = v => feed[0] = v;
                ir = () => feed[1];
                iw = v => feed[1] = v;
                phaseShift = _feedbackLevel != 0 ? ((or() + ir()) << (int)_feedbackLevel) : 0;
                ow(ir());
            }
            else
            {
                phaseShift = phasebuf << 15;
            }

            if (lvlout < 832)
            {
                uint index = (lvlout << 3) + _sinTbl[(((int)((_phase & 0xffff0000)
                                 + phaseShift)) >> 16) & 0x3ff];
                iw(((index < 6656) ? _tLvlTbl[index] : 0));
            }
            else
            {
                iw(0);
            }

            _phase += _phaseIncrement;
            @out += or();
        }

        EnvelopeState _state;
        bool _holdKey;
        uint _feedbackLevel;
        uint _multiple;
        uint _totalLevel;
        byte _keyScale1;
        byte _keyScale2;
        uint _specifiedAttackRate;
        uint _specifiedDecayRate;
        uint _specifiedSustainRate;
        uint _specifiedReleaseRate;
        uint _tickCount;
        uint _sustainLevel;

        bool _ampMod;
        uint _frequency;
        byte _kcode;
        uint _phase;
        uint _phaseIncrement;
        int[] _detn;

        readonly byte[] _rateTbl;
        readonly byte[] _rshiftTbl;
        readonly byte[] _adTbl;
        readonly uint[] _fTbl;
        readonly uint[] _sinTbl;
        readonly int[] _tLvlTbl;
        readonly int[][] _detnTbl;

        readonly uint _tickLength;
        uint _timer;
        readonly uint _rtt;
        int _currentLevel;

        class EvpState
        {
            public byte rate;
            public byte shift;
        }

        EvpState fs_a, fs_d, fs_s, fs_r;
    }
}

