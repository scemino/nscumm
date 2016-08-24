//
//  MidiDriver_MT32.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio.SoftSynth
{
    class MidiChannel_MT32 : MidiChannel_MPU401
    {
        public override void EffectLevel(byte value) { }
        public override void ChorusLevel(byte value) { }
    }

    class MidiDriver_MT32 : EmulatedMidiDriver
    {
        MidiChannel_MT32[] _midiChannels = new MidiChannel_MT32[16];
        ushort _channelMask;
        Mt32.Synth _synth;
        //MT32Emu::ReportHandlerScummV _reportHandler;
        Mt32.ROMImage _controlROM, _pcmROM;
        Stream _controlFile, _pcmFile;
        int _outputRate;
        bool _initializing;

        public MidiDriver_MT32(IMixer mixer)
            : base(mixer)
        {
            _channelMask = 0xFFFF; // Permit all 16 channels by default
            for (var i = 0; i < _midiChannels.Length; ++i)
            {
                _midiChannels[i] = new MidiChannel_MT32();
                _midiChannels[i].Init(this, (byte)i);
            }
        }

        public override bool IsStereo
        {
            get { return true; }
        }

        public override int Rate
        {
            get { return _outputRate; }
        }

        public override MidiDriverError Open()
        {
            if (_isOpen)
                return MidiDriverError.AlreadyOpen;

            // TODO:
            //_reportHandler = new MT32Emu::ReportHandlerScummVM();
            //_synth = new MT32Emu::Synth(_reportHandler);
            _synth = new Mt32.Synth();

            //    PixelFormat screenFormat = g_system.ScreenFormat;

            //    if (screenFormat.bytesPerPixel == 1)
            //    {
            //        const byte dummy_palette[] = {
            //    0, 0, 0,        // background
            //    0, 171, 0,  // border, font
            //    171, 0, 0   // fill
            //};

            //        g_system.getPaletteManager().setPalette(dummy_palette, 0, 3);
            //    }

            _initializing = true;
            Debug(4, "Initializing MT-32 Emulator");
            _controlFile = Engine.OpenFileRead("CM32L_CONTROL.ROM");
            if (_controlFile == null)
                _controlFile = Engine.OpenFileRead("MT32_CONTROL.ROM");
            if (_controlFile == null)
                Error("Error opening MT32_CONTROL.ROM / CM32L_CONTROL.ROM");
            _pcmFile = Engine.OpenFileRead("CM32L_PCM.ROM");
            if (_pcmFile == null)
                _pcmFile = Engine.OpenFileRead("MT32_PCM.ROM");
            if (_pcmFile == null)
                Error("Error opening MT32_PCM.ROM / CM32L_PCM.ROM");
            _controlROM = Mt32.ROMImage.MakeROMImage(_controlFile);
            _pcmROM = Mt32.ROMImage.MakeROMImage(_pcmFile);
            if (!_synth.Open(_controlROM, _pcmROM))
                return MidiDriverError.DeviceNotAvailable;

            double gain = ConfigManager.Instance.Get<int>("midi_gain") / 100.0;
            _synth.OutputGain = (float)(1.0f * gain);
            _synth.ReverbOutputGain = (float)(0.68f * gain);
            // We let the synthesizer play MIDI messages immediately. Our MIDI
            // handling is synchronous to sample generation. This makes delaying MIDI
            // events result in odd sound output in some cases. For example, the
            // shattering window in the Indiana Jones and the Fate of Atlantis intro
            // will sound like a bell if we use any delay here.
            // Bug #6242 "AUDIO: Built-In MT-32 MUNT Produces Wrong Sounds".
            _synth.MIDIDelayMode = Mt32.MIDIDelayMode.IMMEDIATE;

            // We need to report the sample rate MUNT renders at as sample rate of our
            // AudioStream.
            _outputRate = _synth.StereoOutputSampleRate;
            base.Open();

            _initializing = false;

            //if (screenFormat.bytesPerPixel > 1)
            //    g_system.fillScreen(screenFormat.RGBToColor(0, 0, 0));
            //else
            //    g_system.fillScreen(0);

            //g_system.updateScreen();

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);

            return 0;

        }

        public override MidiChannel AllocateChannel()
        {
            MidiChannel_MT32 chan;

            for (var i = 0; i < _midiChannels.Length; ++i)
            {
                if (i == 9 || (_channelMask & (1 << i)) == 0)
                    continue;
                chan = _midiChannels[i];
                if (chan.Allocate())
                {
                    return chan;
                }
            }
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return _midiChannels[9];
        }

        public override void Send(int b)
        {
            _synth.PlayMsg(b);
        }

        public override void SetPitchBendRange(byte channel, uint range)
        {
            if (range > 24)
            {
                Warning("setPitchBendRange() called with range > 24: %d", range);
            }
            byte[] benderRangeSysex = new byte[9];
            benderRangeSysex[0] = 0x41; // Roland
            benderRangeSysex[1] = channel;
            benderRangeSysex[2] = 0x16; // MT-32
            benderRangeSysex[3] = 0x12; // Write
            benderRangeSysex[4] = 0x00;
            benderRangeSysex[5] = 0x00;
            benderRangeSysex[6] = 0x04;
            benderRangeSysex[7] = (byte)range;
            benderRangeSysex[8] = Mt32.Synth.CalcSysexChecksum(new BytePtr(benderRangeSysex, 4), 4, 0);
            SysEx(benderRangeSysex, 9);
        }

        protected override void GenerateSamples(short[] buf, int pos, int len)
        {
            _synth.Render(buf, pos, len);
        }

        public override void SysEx(BytePtr msg, ushort length)
        {
            if (msg[0] == 0xf0)
            {
                _synth.PlaySysex(msg, length);
            }
            else
            {
                _synth.PlaySysexWithoutFraming(msg, length);
            }
        }

        public override int Property(int prop, int param)
        {
            switch (prop)
            {
                case PROP_CHANNEL_MASK:
                    _channelMask = (ushort)(param & 0xFFFF);
                    return 1;
            }

            return 0;
        }
    }
}