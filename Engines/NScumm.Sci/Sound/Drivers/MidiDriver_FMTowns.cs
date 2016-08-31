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
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiDriver_FMTowns : MidiDriver, ITownsAudioInterfacePluginDriver
    {
        private class TownsMidiPart
        {
            public TownsMidiPart(MidiDriver_FMTowns driver, byte id)
            {
                _drv = driver;
                _id = id;
                _volume = 0x3f;
                _pitchBend = 0x2000;
            }

            public void NoteOff(byte note)
            {
                for (var i = 0; i < 6; i++)
                {
                    if ((_drv._out[i]._assign != _id && _drv._version != SciVersion.V1_EARLY) ||
                        _drv._out[i]._note != note)
                        continue;
                    if (_sustain != 0)
                        _drv._out[i]._sustain = 1;
                    else
                        _drv._out[i].NoteOff();
                    return;
                }
            }

            public void NoteOn(byte note, byte velo)
            {
                if (note < 12 || note > 107)
                    return;

                if (velo == 0)
                {
                    NoteOff(note);
                    return;
                }

                if (_drv._version != SciVersion.V1_EARLY)
                    velo >>= 1;

                for (var i = 0; i < 6; i++)
                {
                    if ((_drv._out[i]._assign != _id && _drv._version != SciVersion.V1_EARLY) ||
                        _drv._out[i]._note != note)
                        continue;
                    _drv._out[i]._sustain = 0;
                    _drv._out[i].NoteOff();
                    _drv._out[i].NoteOn(note, velo);
                    return;
                }

                var chan = AllocateChannel();
                if (chan != -1)
                    _drv._out[chan].NoteOn(note, velo);
            }

            public void ControlChangeVolume(byte vol)
            {
                if (_drv._version == SciVersion.V1_EARLY)
                    return;

                _volume = (byte) (vol >> 1);
                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign == _id)
                        _drv._out[i].UpdateVolume();
                }
            }

            public void ControlChangeSustain(byte sus)
            {
                if (_drv._version == SciVersion.V1_EARLY)
                    return;

                _sustain = sus;
                if (_sustain != 0)
                    return;

                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign == _id && _drv._out[i]._sustain != 0)
                    {
                        _drv._out[i]._sustain = 0;
                        _drv._out[i].NoteOff();
                    }
                }
            }

            public void ControlChangePolyphony(byte numChan)
            {
                if (_drv._version == SciVersion.V1_EARLY)
                    return;

                byte numAssigned = 0;
                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign == _id)
                        numAssigned++;
                }

                numAssigned += _chanMissing;
                if (numAssigned < numChan)
                {
                    AddChannels(numChan - numAssigned);
                }
                else if (numAssigned > numChan)
                {
                    DropChannels(numAssigned - numChan);
                    _drv.AddMissingChannels();
                }
            }

            public void ControlChangeAllNotesOff()
            {
                for (var i = 0; i < 6; i++)
                {
                    if ((_drv._out[i]._assign == _id || _drv._version == SciVersion.V1_EARLY) &&
                        _drv._out[i]._note != 0xff)
                        _drv._out[i].NoteOff();
                }
            }

            public void ProgramChange(byte prg)
            {
                _program = prg;
            }

            public void PitchBend(short val)
            {
                _pitchBend = val;
                val -= 0x2000;
                for (var i = 0; i < 6; i++)
                {
                    // Strangely, the early version driver applies the setting to channel 0 only.
                    if (_drv._out[i]._assign == _id || (_drv._version == SciVersion.V1_EARLY && i == 0))
                        _drv._out[i].PitchBend(val);
                }
            }

            public void AddChannels(int num)
            {
                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign != 0xff)
                        continue;

                    _drv._out[i]._assign = _id;
                    _drv._out[i].UpdateVolume();

                    if (_drv._out[i]._note != 0xff)
                        _drv._out[i].NoteOff();

                    if (--num == 0)
                        break;
                }
            }

            public void DropChannels(int num)
            {
                if (_chanMissing == num)
                {
                    _chanMissing = 0;
                    return;
                }
                else if (_chanMissing > num)
                {
                    _chanMissing = (byte) (_chanMissing - num);
                    return;
                }

                num -= _chanMissing;
                _chanMissing = 0;

                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign != _id || _drv._out[i]._note != 0xff)
                        continue;
                    _drv._out[i]._assign = 0xff;
                    if (--num == 0)
                        return;
                }

                for (var i = 0; i < 6; i++)
                {
                    if (_drv._out[i]._assign != _id)
                        continue;
                    _drv._out[i]._sustain = 0;
                    _drv._out[i].NoteOff();
                    _drv._out[i]._assign = 0xff;
                    if (--num == 0)
                        return;
                }
            }

            public byte CurrentProgram()
            {
                return _program;
            }

            private int AllocateChannel()
            {
                int chan = _outChan;
                var ovrChan = 0;
                var ld = 0;
                var found = false;

                for (var loop = true; loop;)
                {
                    if (++chan == 6)
                        chan = 0;

                    if (chan == _outChan)
                        loop = false;

                    if (_id == _drv._out[chan]._assign || _drv._version == SciVersion.V1_EARLY)
                    {
                        if (_drv._out[chan]._note == 0xff)
                        {
                            found = true;
                            break;
                        }

                        if (_drv._out[chan]._duration >= ld)
                        {
                            ld = _drv._out[chan]._duration;
                            ovrChan = chan;
                        }
                    }
                }

                if (!found)
                {
                    if (ld == 0)
                        return -1;
                    chan = ovrChan;
                    _drv._out[chan]._sustain = 0;
                    _drv._out[chan].NoteOff();
                }

                _outChan = (byte) chan;
                return chan;
            }

            private readonly byte _id;
            private byte _program;
            internal byte _volume;
            private byte _sustain;
            internal byte _chanMissing;
            private short _pitchBend;
            private byte _outChan;

            private readonly MidiDriver_FMTowns _drv;
        }

        private class TownsChannel
        {
            public TownsChannel(MidiDriver_FMTowns driver, byte id)
            {
                _drv = driver;
                _id = id;
                _assign = 0xff;
                _note = 0xff;
                _program = 0xff;
            }

            public void NoteOff()
            {
                if (_sustain != 0)
                    return;

                _drv._intf.Callback(2, _id);
                _note = 0xff;
                _duration = 0;
            }

            public void NoteOn(byte note, byte velo)
            {
                _duration = 0;

                if (_drv._version != SciVersion.V1_EARLY)
                {
                    if (_program != _drv._parts[_assign].CurrentProgram() && _drv._soundOn)
                    {
                        _program = _drv._parts[_assign].CurrentProgram();
                        _drv._intf.Callback(4, _id, _program);
                    }
                }

                _note = note;
                _velo = velo;
                _drv._intf.Callback(1, _id, _note, _velo);
            }

            public void PitchBend(short val)
            {
                _drv._intf.Callback(7, _id, val);
            }

            public void UpdateVolume()
            {
                if (_assign > 15 && _drv._version != SciVersion.V1_EARLY)
                    return;
                _drv._intf.Callback(8, _id,
                    _drv.GetChannelVolume((byte)(_drv._version == SciVersion.V1_EARLY ? 0 : _assign)));
            }

            public void UpdateDuration()
            {
                if (_note != 0xff)
                    _duration++;
            }

            internal byte _assign;
            internal byte _note;
            internal byte _sustain;
            internal ushort _duration;

            private readonly byte _id;
            private byte _velo;
            private byte _program;

            private readonly MidiDriver_FMTowns _drv;
        }

        private TimerProc _timerProc;
        private object _timerProcPara;

        private readonly TownsMidiPart[] _parts;
        private readonly TownsChannel[] _out;

        private byte _masterVolume;

        private bool _soundOn;

        private bool _isOpen;
        private bool _ready;

        private readonly ushort _baseTempo;
        private readonly SciVersion _version;

        private readonly TownsAudioInterface _intf;

        private static readonly byte[] VolumeTable =
        {
            0x00,
            0x0D,
            0x1B,
            0x28,
            0x36,
            0x43,
            0x51,
            0x5F,
            0x63,
            0x67,
            0x6B,
            0x6F,
            0x73,
            0x77,
            0x7B,
            0x7F
        };

        public override uint BaseTempo => _baseTempo;

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        public MidiDriver_FMTowns(IMixer mixer, SciVersion version)
        {
            _version = version;
            _baseTempo = 10080;
            _masterVolume = 0x0f;

            _soundOn = true;
            _intf = new TownsAudioInterface(mixer, this, true);
            _out = new TownsChannel[6];
            for (var i = 0; i < 6; i++)
                _out[i] = new TownsChannel(this, (byte) i);
            _parts = new TownsMidiPart[16];
            for (var i = 0; i < 16; i++)
                _parts[i] = new TownsMidiPart(this, (byte) i);
        }

        public override MidiDriverError Open()
        {
            if (_isOpen)
                return MidiDriverError.AlreadyOpen;

            if (!_ready)
            {
                if (!_intf.Init())
                    return MidiDriverError.CannotConnect;

                _intf.Callback(0);

                _intf.Callback(21, 255, 1);
                _intf.Callback(21, 0, 1);
                _intf.Callback(22, 255, 221);

                _intf.Callback(33, 8);
                _intf.SetSoundEffectChanMask(~0x3f);

                _ready = true;
            }

            _isOpen = true;

            return 0;
        }

        private void Close()
        {
            _isOpen = false;
        }

        public void SetSoundOn(bool toggle)
        {
            _soundOn = toggle;
        }

        public void LoadInstruments(BytePtr data)
        {
            if (data != BytePtr.Null)
            {
                data.Offset += 6;
                for (var i = 0; i < 128; i++)
                {
                    _intf.Callback(5, 0, i, data);
                    data.Offset += 48;
                }
            }
            _intf.Callback(70, 3);

            Property(MidiPlayer.MIDI_PROP_MASTER_VOLUME, _masterVolume);
        }

        public override void Send(int b)
        {
            if (!_isOpen)
                return;

            var para2 = (byte) ((b >> 16) & 0xFF);
            var para1 = (byte) ((b >> 8) & 0xFF);
            var cmd = (byte) (b & 0xF0);

            var chan = _parts[b & 0x0F];

            switch (cmd)
            {
                case 0x80:
                    chan.NoteOff(para1);
                    break;
                case 0x90:
                    chan.NoteOn(para1, para2);
                    break;
                case 0xb0:
                    switch (para1)
                    {
                        case 7:
                            chan.ControlChangeVolume(para2);
                            break;
                        case 64:
                            chan.ControlChangeSustain(para2);
                            break;
                        case MidiPlayer. SCI_MIDI_SET_POLYPHONY:
                            chan.ControlChangePolyphony(para2);
                            break;
                        case MidiPlayer.SCI_MIDI_CHANNEL_NOTES_OFF:
                            chan.ControlChangeAllNotesOff();
                            break;
                    }
                    break;
                case 0xc0:
                    chan.ProgramChange(para1);
                    break;
                case 0xe0:
                    chan.PitchBend((short) (para1 | (para2 << 7)));
                    break;
            }
        }

        private uint Property(int prop, uint param)
        {
            switch (prop)
            {
                case MidiPlayer.MIDI_PROP_MASTER_VOLUME:
                    if (param != 0xffff)
                    {
                        _masterVolume = (byte) param;
                        for (var i = 0; i < 6; i++)
                            _out[i].UpdateVolume();
                    }
                    return _masterVolume;
            }
            return 0;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _timerProc = timerProc;
            _timerProcPara = timerParam;
        }

        public void TimerCallback(int timerId)
        {
            if (!_isOpen)
                return;

            switch (timerId)
            {
                case 1:
                    UpdateParser();
                    UpdateChannels();
                    break;
            }
        }

        public int GetChannelVolume(byte midiPart)
        {
            var tableIndex = (_version == SciVersion.V1_EARLY)
                ? _masterVolume
                : (_parts[midiPart]._volume * (_masterVolume + 1)) >> 6;
            System.Diagnostics.Debug.Assert(tableIndex < 16);
            return VolumeTable[tableIndex];
        }

        private void AddMissingChannels()
        {
            byte avlChan = 0;
            for (var i = 0; i < 6; i++)
            {
                if (_out[i]._assign == 0xff)
                    avlChan++;
            }

            if (avlChan==0)
                return;

            for (var i = 0; i < 16; i++)
            {
                if (_parts[i]._chanMissing==0)
                    continue;

                if (_parts[i]._chanMissing < avlChan)
                {
                    avlChan -= _parts[i]._chanMissing;
                    var m = _parts[i]._chanMissing;
                    _parts[i]._chanMissing = 0;
                    _parts[i].AddChannels(m);
                }
                else
                {
                    _parts[i]._chanMissing -= avlChan;
                    _parts[i].AddChannels(avlChan);
                    return;
                }
            }
        }

        private void UpdateParser()
        {
            _timerProc?.Invoke(_timerProcPara);
        }

        private void UpdateChannels()
        {
            for (var i = 0; i < 6; i++)
                _out[i].UpdateDuration();
        }
    }
}