//
//  MidiDriver_TOWNS.cs
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
using System.Diagnostics;

namespace NScumm.Core.Audio.SoftSynth
{
    class TownsMidiChanState
    {
        public byte get(int type)
        {
            switch (type)
            {
                case 0:
                    return unk1;
                case 1:
                    return mulAmsFms;
                case 2:
                    return tl;
                case 3:
                    return attDec;
                case 4:
                    return sus;
                case 5:
                    return fgAlg;
                case 6:
                    return unk2;
            }
            return 0;
        }

        public byte unk1;
        public byte mulAmsFms;
        public byte tl;
        public byte attDec;
        public byte sus;
        public byte fgAlg;
        public byte unk2;
    }

    class MidiDriver_TOWNS: MidiDriver, ITownsAudioInterfacePluginDriver
    {
        public MidiDriver_TOWNS(IMixer mixer)
        {
            _baseTempo = 10080;
            _rand = 1;

            // We set exteral mutex handling to true to avoid lockups in SCUMM which has its own mutex.
            _intf = new TownsAudioInterface(mixer, this, true);

            _channels = new TownsMidiInputChannel[32];
            for (int i = 0; i < 32; i++)
                _channels[i] = new TownsMidiInputChannel(this, i > 8 ? (i + 1) : i);

            _out = new TownsMidiOutputChannel[6];
            for (int i = 0; i < 6; i++)
            {
                _out[i] = new TownsMidiOutputChannel(this, i);
            }

            _chanState = new TownsMidiChanState[32];
            for (int i = 0; i < _chanState.Length; i++)
            {
                _chanState[i] = new TownsMidiChanState();
            }

            _operatorLevelTable = new byte[2048];
            for (int i = 0; i < 64; i++)
            {
                for (int ii = 0; ii < 32; ii++)
                    _operatorLevelTable[(i << 5) + ii] = (byte)(((i * (ii + 1)) >> 5) & 0xff);
            }
            for (int i = 0; i < 64; i++)
                _operatorLevelTable[i << 5] = 0;
        }

        public override MidiDriverError Open()
        {
            if (_isOpen)
                return MidiDriverError.AlreadyOpen;

            if (!_intf.Init())
                return MidiDriverError.CannotConnect;

            _intf.Callback(0);

            _intf.Callback(21, 255, 1);
            _intf.Callback(21, 0, 1);
            _intf.Callback(22, 255, 221);

            _intf.Callback(33, 8);
            _intf.SetSoundEffectChanMask(~0x3f);

            _allocCurPos = 0;

            _isOpen = true;

            return 0;
        }

        public override int Property(int prop, int param)
        {
            return 0;
        }

        public void TimerCallback(int timerId)
        {
            if (!_isOpen)
                return;

            switch (timerId)
            {
                case 1:
                    UpdateParser();
                    UpdateOutputChannels();
                    break;
            }
        }

        public override void SetTimerCallback(object timerParam, MidiDriver.TimerProc timerProc)
        {
            _timerProc = timerProc;
            _timerProcPara = timerParam;
        }

        public override uint BaseTempo
        {
            get{ return _baseTempo; }
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        public override void Send(int b)
        {
            if (!_isOpen)
                return;

            int param2 = ((b >> 16) & 0xFF);
            byte param1 = (byte)((b >> 8) & 0xFF);
            byte cmd = (byte)(b & 0xF0);

            TownsMidiInputChannel c = _channels[b & 0x0F];

            switch (cmd)
            {
                case 0x80:
                    c.NoteOff(param1);
                    break;
                case 0x90:
                    if (param2 != 0)
                        c.NoteOn(param1, (byte)param2);
                    else
                        c.NoteOff(param1);
                    break;
                case 0xB0:
                    c.ControlChange(param1, (byte)param2);
                    break;
                case 0xC0:
                    c.ProgramChange(param1);
                    break;
                case 0xE0:
                    c.PitchBend((short)((param1 | (param2 << 7)) - 0x2000));
                    break;
                case 0xF0:
                    Debug.WriteLine("MidiDriver_TOWNS: Receiving SysEx command on a send() call");
                    break;
            }
        }

        public override MidiChannel AllocateChannel()
        {
            if (!_isOpen)
                return null;

            for (int i = 0; i < 32; ++i)
            {
                TownsMidiInputChannel chan = _channels[i];
                if (chan.Allocate())
                    return chan;
            }

            return null;
        }

        public TownsMidiOutputChannel AllocateOutputChannel(int pri)
        {
            TownsMidiOutputChannel res = null;

            for (int i = 0; i < 6; i++)
            {
                if (++_allocCurPos == 6)
                    _allocCurPos = 0;

                var s = _out[_allocCurPos].CheckPriority(pri);
                if (s == (int)CheckPriorityStatus.Disconnected)
                    return _out[_allocCurPos];

                if (s != (int)CheckPriorityStatus.HighPriority)
                {
                    pri = (int)s;
                    res = _out[_allocCurPos];
                }
            }

            if (res != null)
                res.Disconnect();

            return res;
        }

        public int RandomValue(int para)
        {
            _rand = (byte)(((_rand & 1) != 0) ? (_rand >> 1) ^ 0xb8 : (_rand >> 1));
            return (_rand * para) >> 8;
        }

        void UpdateParser()
        {
            if (_timerProc != null)
                _timerProc(_timerProcPara);
        }

        void UpdateOutputChannels()
        {
            _tickCounter += _baseTempo;
            while (_tickCounter >= 16667)
            {
                _tickCounter -= 16667;
                for (int i = 0; i < 6; i++)
                {
                    if (_out[i].Update())
                        return;
                }
            }
        }

        TownsMidiInputChannel[] _channels;
        TownsMidiOutputChannel[] _out;
        internal TownsMidiChanState[] _chanState;

        MidiDriver.TimerProc _timerProc;
        object _timerProcPara;

        internal TownsAudioInterface _intf;

        uint _tickCounter;
        byte _allocCurPos;
        byte _rand;

        bool _isOpen;

        internal byte[] _operatorLevelTable;

        readonly ushort _baseTempo;
    }
}

