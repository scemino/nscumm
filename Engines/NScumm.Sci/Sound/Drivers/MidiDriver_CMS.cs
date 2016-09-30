//
//  MidiDriver_CMS.cs
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

// FIXME: We don't seem to be sending the polyphony init data, so disable this for now
#define CMS_DISABLE_VOICE_MAPPING

using System;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiDriver_CMS : EmulatedMidiDriver
    {
        private const int _timerFreq = 60;
        
        public bool PlaySwitch
        {
            get { return _playSwitch; }
            set { _playSwitch = value; }
        }

        public override bool IsStereo
        {
            get { return true; }
        }

        public override int Rate
        {
            get { return _rate; }
        }

        private static readonly int[] _frequencyTable = {
              3,  10,  17,  24,
             31,  38,  46,  51,
             58,  64,  71,  77,
             83,  89,  95, 101,
            107, 113, 119, 124,
            130, 135, 141, 146,
            151, 156, 162, 167,
            172, 177, 182, 186,
            191, 196, 200, 205,
            209, 213, 217, 222,
            226, 230, 234, 238,
            242, 246, 250, 253
        };

        private static readonly int[] _velocityTable = {
             1,  3,  6,  8,  9, 10, 11, 12,
            12, 13, 13, 14, 14, 14, 15, 15,
             0,  1,  2,  2,  3,  4,  4,  5,
             6,  6,  7,  8,  8,  9, 10, 10
        };

        private readonly byte[,] _octaveRegs = new byte[2, 3];

        private ResourceManager _resMan;
        private CMSEmulator _cms;

        private int _samplesPerCallback;
        private int _samplesPerCallbackRemainder;
        private int _samplesTillCallback;
        private int _samplesTillCallbackRemainder;

        private int _rate;
        private bool _playSwitch;
        private ushort _masterVolume;

        private byte[] _patchData;

        private class Channel
        {
            public Channel()
            {
                pan = 0x40;
                pitchWheel = 0x2000;
            }

            public byte patch;
            public byte volume;
            public byte pan;
            public byte hold;
            public byte extraVoices;
            public ushort pitchWheel;
            public byte pitchModifier;
            public bool pitchAdditive;
            public byte lastVoiceUsed;
        }

        private readonly Channel[] _channel = new Channel[16];

        private class Voice
        {
            public Voice()
            {
                channel = 0xFF;
                note = 0xFF;
                sustained = 0xFF;
            }

            public byte channel;
            public byte note;
            public byte sustained;
            public ushort ticks;
            public ushort turnOffTicks;
            public BytePtr patchDataPtr;
            public byte patchDataIndex;
            public byte amplitudeTimer;
            public byte amplitudeModifier;
            public bool turnOff;
            public byte velocity;
        }

        private readonly Voice[] _voice = new Voice[12];

        public MidiDriver_CMS(IMixer mixer, ResourceManager resMan)
            : base(mixer)
        {
            _resMan = resMan;
            _playSwitch = true;


        }

        public override MidiDriverError Open()
        {
            if (_cms != null)
                return MidiDriverError.AlreadyOpen;

            System.Diagnostics.Debug.Assert(_resMan != null);
            ResourceManager.ResourceSource.Resource res = _resMan.FindResource(new ResourceId(ResourceType.Patch, 101), false);
            if (res == null)
                return (MidiDriverError)(-1);

            _patchData = new byte[res.size];
            Array.Copy(res.data, _patchData, res.size);

            for (int i = 0; i < _channel.Length; ++i)
                _channel[i] = new Channel();

            for (int i = 0; i < _voice.Length; ++i)
                _voice[i] = new Voice();

            _rate = _mixer.OutputRate;
            _cms = new CMSEmulator(_rate);
            System.Diagnostics.Debug.Assert(_cms != null);
            _playSwitch = true;
            _masterVolume = 0;

            for (int i = 0; i < 31; ++i)
            {
                WriteToChip1(i, 0);
                WriteToChip2(i, 0);
            }

            WriteToChip1(0x14, 0xFF);
            WriteToChip2(0x14, 0xFF);

            WriteToChip1(0x1C, 1);
            WriteToChip2(0x1C, 1);

            _samplesPerCallback = Rate / _timerFreq;
            _samplesPerCallbackRemainder = Rate % _timerFreq;
            _samplesTillCallback = 0;
            _samplesTillCallbackRemainder = 0;

            var retVal = base.Open();
            if (retVal != MidiDriverError.None)
                return retVal;

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false);
            return 0;
        }

        public override void Dispose()
        {
            _mixer.StopHandle(_mixerSoundHandle);

            _patchData = null;
            _cms = null;
            base.Dispose();
        }

        public override void Send(int b)
        {
            byte command = (byte)(b & 0xf0);
            byte channel = (byte)(b & 0xf);
            byte op1 = (byte)((b >> 8) & 0xff);
            byte op2 = (byte)((b >> 16) & 0xff);

            switch (command)
            {
                case 0x80:
                    NoteOff(channel, op1);
                    break;

                case 0x90:
                    NoteOn(channel, op1, op2);
                    break;

                case 0xB0:
                    ControlChange(channel, op1, op2);
                    break;

                case 0xC0:
                    _channel[channel].patch = op1;
                    break;

                case 0xE0:
                    PitchWheel(channel, (op1 & 0x7f) | ((op2 & 0x7f) << 7));
                    break;
            }
        }

        public override int Property(int prop, int param)
        {
            switch (prop)
            {
                case MidiPlayer.MIDI_PROP_MASTER_VOLUME:
                    if (param != 0xffff)
                        _masterVolume = (ushort)param;
                    return _masterVolume;

                default:
                    return base.Property(prop, param);
            }
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        private void WriteToChip1(int address, int data)
        {
            _cms.PortWrite(0x221, address);
            _cms.PortWrite(0x220, data);

            if (address >= 16 && address <= 18)
                _octaveRegs[0, address - 16] = (byte)data;
        }

        private void WriteToChip2(int address, int data)
        {
            _cms.PortWrite(0x223, address);
            _cms.PortWrite(0x222, data);

            if (address >= 16 && address <= 18)
                _octaveRegs[1, address - 16] = (byte)data;
        }

        private void VoiceOn(int voiceNr, int note, int velocity)
        {
            Voice voice = _voice[voiceNr];
            voice.note = (byte)note;
            voice.turnOff = false;
            voice.patchDataIndex = 0;
            voice.amplitudeTimer = 0;
            voice.ticks = 0;
            voice.turnOffTicks = 0;
            voice.patchDataPtr = new BytePtr(_patchData, _patchData.ToUInt16(_channel[voice.channel].patch * 2));
            if (velocity != 0)
                velocity = _velocityTable[(velocity >> 3)];
            voice.velocity = (byte)velocity;
            NoteSend(voiceNr);
        }

        private void VoiceOff(int voiceNr)
        {
            Voice voice = _voice[voiceNr];
            voice.velocity = 0;
            voice.note = 0xFF;
            voice.sustained = 0;
            voice.turnOff = false;
            voice.patchDataIndex = 0;
            voice.amplitudeTimer = 0;
            voice.amplitudeModifier = 0;
            voice.ticks = 0;
            voice.turnOffTicks = 0;

            SetupVoiceAmplitude(voiceNr);
        }

        private void NoteSend(int voiceNr)
        {
            Voice voice = _voice[voiceNr];

            int frequency = (ScummHelper.Clip(voice.note, 21, 116) - 21) * 4;
            if (_channel[voice.channel].pitchModifier != 0)
            {
                int modifier = _channel[voice.channel].pitchModifier;

                if (!_channel[voice.channel].pitchAdditive)
                {
                    if (frequency > modifier)
                        frequency -= modifier;
                    else
                        frequency = 0;
                }
                else {
                    int tempFrequency = 384 - frequency;
                    if (modifier < tempFrequency)
                        frequency += modifier;
                    else
                        frequency = 383;
                }
            }

            int chipNumber = 0;
            if (voiceNr >= 6)
            {
                voiceNr -= 6;
                chipNumber = 1;
            }

            int octave = 0;
            while (frequency >= 48)
            {
                frequency -= 48;
                ++octave;
            }

            frequency = _frequencyTable[frequency];

            if (chipNumber == 1)
                WriteToChip2(8 + voiceNr, frequency);
            else
                WriteToChip1(8 + voiceNr, frequency);

            byte octaveData = _octaveRegs[chipNumber, voiceNr >> 1];

            if ((voiceNr & 1) != 0)
            {
                octaveData &= 0x0F;
                octaveData = (byte)(octaveData | (octave << 4));
            }
            else {
                octaveData &= 0xF0;
                octaveData = (byte)(octaveData | octave);
            }

            if (chipNumber == 1)
                WriteToChip2(0x10 + (voiceNr >> 1), octaveData);
            else
                WriteToChip1(0x10 + (voiceNr >> 1), octaveData);
        }

        private void NoteOn(int channel, int note, int velocity)
        {
            if (note < 21 || note > 116)
                return;

            if (velocity == 0)
            {
                NoteOff(channel, note);
                return;
            }

            for (int i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == channel && _voice[i].note == note)
                {
                    _voice[i].sustained = 0;
                    VoiceOff(i);
                    VoiceOn(i, note, velocity);
                    return;
                }
            }

# if CMS_DISABLE_VOICE_MAPPING
            int voice = FindVoiceBasic(channel);
#else
            int voice = FindVoice(channel);
#endif
            if (voice != -1)
                VoiceOn(voice, note, velocity);
        }

        private int FindVoiceBasic(int channel)
        {
            int voice = -1;
            int oldestVoice = -1;
            int oldestAge = -1;

            // Try to find a voice assigned to this channel that is free (round-robin)
            for (int i = 0; i < _voice.Length; i++)
            {
                int v = (_channel[channel].lastVoiceUsed + i + 1) % _voice.Length;

                if (_voice[v].note == 0xFF)
                {
                    voice = v;
                    break;
                }

                // We also keep track of the oldest note in case the search fails
                if (_voice[v].ticks > oldestAge)
                {
                    oldestAge = _voice[v].ticks;
                    oldestVoice = v;
                }
            }

            if (voice == -1)
            {
                if (oldestVoice >= 0)
                {
                    VoiceOff(oldestVoice);
                    voice = oldestVoice;
                }
                else {
                    return -1;
                }
            }

            _voice[voice].channel = (byte)channel;
            _channel[channel].lastVoiceUsed = (byte)voice;
            return voice;
        }

        private void NoteOff(int channel, int note)
        {
            for (uint i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == channel && _voice[i].note == note)
                {
                    if (_channel[channel].hold != 0)
                        _voice[i].sustained = 1;
                    else
                        _voice[i].turnOff = true;
                }
            }
        }

        private void ControlChange(int channel, int control, int value)
        {
            switch (control)
            {
                case 7:
                    if (value != 0)
                    {
                        value >>= 3;
                        if (value == 0)
                            ++value;
                    }

                    _channel[channel].volume = (byte)value;
                    break;

                case 10:
                    _channel[channel].pan = (byte)value;
                    break;

                case 64:
                    _channel[channel].hold = (byte)value;

                    if (value == 0)
                    {
                        for (uint i = 0; i < _voice.Length; ++i)
                        {
                            if (_voice[i].channel == channel && _voice[i].sustained != 0)
                            {
                                _voice[i].sustained = 0;
                                _voice[i].turnOff = true;
                            }
                        }
                    }
                    break;

                case 75:
# if !CMS_DISABLE_VOICE_MAPPING
                    VoiceMapping(channel, value);
#endif
                    break;

                case 123:
                    for (int i = 0; i < _voice.Length; ++i)
                    {
                        if (_voice[i].channel == channel && _voice[i].note != 0xFF)
                            VoiceOff(i);
                    }
                    break;
            }
        }

        private void PitchWheel(int channelNr, int value)
        {
            Channel channel = _channel[channelNr];
            channel.pitchWheel = (ushort)value;
            channel.pitchAdditive = false;
            channel.pitchModifier = 0;

            if (value < 0x2000)
            {
                channel.pitchModifier = (byte)((0x2000 - value) / 170);
            }
            else if (value > 0x2000)
            {
                channel.pitchModifier = (byte)((value - 0x2000) / 170);
                channel.pitchAdditive = true;
            }

            for (int i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == channelNr && _voice[i].note != 0xFF)
                    NoteSend(i);
            }
        }

        private void VoiceMapping(int channelNr, int value)
        {
            int curVoices = 0;

            for (int i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == channelNr)
                    ++curVoices;
            }

            curVoices += _channel[channelNr].extraVoices;

            if (curVoices == value)
            {
                return;
            }
            if (curVoices < value)
            {
                BindVoices(channelNr, value - curVoices);
            }
            else {
                UnbindVoices(channelNr, curVoices - value);
                DonateVoices();
            }
        }

        private void BindVoices(int channel, int voices)
        {
            for (int i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == 0xFF)
                    continue;

                Voice voice = _voice[i];
                voice.channel = (byte)channel;

                if (voice.note != 0xFF)
                    VoiceOff(i);

                --voices;
                if (voices == 0)
                    break;
            }

            _channel[channel].extraVoices = (byte)(_channel[channel].extraVoices + voices);

            // The original called "PatchChange" here, since this just
            // copies the value of _channel[channel].patch to itself
            // it is left out here though.
        }

        private void UnbindVoices(int channelNr, int voices)
        {
            Channel channel = _channel[channelNr];

            if (channel.extraVoices >= voices)
            {
                channel.extraVoices = (byte)(channel.extraVoices - voices);
            }
            else {
                voices -= channel.extraVoices;
                channel.extraVoices = 0;

                for (int i = 0; i < _voice.Length; ++i)
                {
                    if (_voice[i].channel == channelNr
                        && _voice[i].note == 0xFF)
                    {
                        --voices;
                        if (voices == 0)
                            return;
                    }
                }

                do
                {
                    ushort voiceTime = 0;
                    uint voiceNr = 0;

                    for (int i = 0; i < _voice.Length; ++i)
                    {
                        if (_voice[i].channel != channelNr)
                            continue;

                        ushort curTime = _voice[i].turnOffTicks;
                        if (curTime != 0)
                            curTime += 0x8000;
                        else
                            curTime = _voice[i].ticks;

                        if (curTime >= voiceTime)
                        {
                            voiceNr = (uint)i;
                            voiceTime = curTime;
                        }
                    }

                    _voice[voiceNr].sustained = 0;
                    VoiceOff((int)voiceNr);
                    _voice[voiceNr].channel = 0xFF;
                    --voices;
                } while (voices != 0);
            }
        }

        private void DonateVoices()
        {
            int freeVoices = 0;

            for (int i = 0; i < _voice.Length; ++i)
            {
                if (_voice[i].channel == 0xFF)
                    ++freeVoices;
            }

            if (freeVoices == 0)
                return;

            for (int i = 0; i < _channel.Length; ++i)
            {
                Channel channel = _channel[i];

                if (channel.extraVoices == 0)
                {
                    continue;
                }
                if (channel.extraVoices < freeVoices)
                {
                    freeVoices -= channel.extraVoices;
                    channel.extraVoices = 0;
                    BindVoices(i, channel.extraVoices);
                }
                else
                {
                    channel.extraVoices = (byte)(channel.extraVoices - freeVoices);
                    BindVoices(i, freeVoices);
                    return;
                }
            }
        }

        private int FindVoice(int channelNr)
        {
            Channel channel = _channel[channelNr];
            int voiceNr = channel.lastVoiceUsed;

            int newVoice = 0;
            ushort newVoiceTime = 0;

            bool loopDone = false;
            do
            {
                ++voiceNr;

                if (voiceNr == 12)
                    voiceNr = 0;

                Voice voice = _voice[voiceNr];

                if (voiceNr == channel.lastVoiceUsed)
                    loopDone = true;

                if (voice.channel == channelNr)
                {
                    if (voice.note == 0xFF)
                    {
                        channel.lastVoiceUsed = (byte)voiceNr;
                        return voiceNr;
                    }

                    ushort curTime = voice.turnOffTicks;
                    if (curTime != 0)
                        curTime += 0x8000;
                    else
                        curTime = voice.ticks;

                    if (curTime >= newVoiceTime)
                    {
                        newVoice = voiceNr;
                        newVoiceTime = curTime;
                    }
                }
            } while (!loopDone);

            if (newVoiceTime > 0)
            {
                voiceNr = newVoice;
                _voice[voiceNr].sustained = 0;
                VoiceOff(voiceNr);
                channel.lastVoiceUsed = (byte)voiceNr;
                return voiceNr;
            }
            return -1;
        }

        private void UpdateVoiceAmplitude(int voiceNr)
        {
            Voice voice = _voice[voiceNr];

            if (voice.amplitudeTimer != 0 && voice.amplitudeTimer != 254)
            {
                --voice.amplitudeTimer;
                return;
            }

            if (voice.amplitudeTimer == 254)
            {
                if (!voice.turnOff)
                    return;

                voice.amplitudeTimer = 0;
            }

            int nextDataIndex = voice.patchDataIndex;
            byte timerData = 0;
            byte amplitudeData = voice.patchDataPtr[nextDataIndex];

            if (amplitudeData == 255)
            {
                timerData = amplitudeData = 0;
                VoiceOff(voiceNr);
            }
            else 
            {
                timerData = voice.patchDataPtr[nextDataIndex + 1];
                nextDataIndex += 2;
            }

            voice.patchDataIndex = (byte)nextDataIndex;
            voice.amplitudeTimer = timerData;
            voice.amplitudeModifier = amplitudeData;
        }


        private void SetupVoiceAmplitude(int voiceNr)
        {
            Voice voice = _voice[voiceNr];
            uint amplitude = 0;

            if (_channel[voice.channel].volume != 0 && voice.velocity != 0
                && voice.amplitudeModifier != 0 && _masterVolume != 0)
            {
                amplitude = (uint)(_channel[voice.channel].volume * voice.velocity);
                amplitude /= 0x0F;
                amplitude *= voice.amplitudeModifier;
                amplitude /= 0x0F;
                amplitude *= _masterVolume;
                amplitude /= 0x0F;

                if (amplitude == 0)
                    ++amplitude;
            }

            byte amplitudeData = 0;
            int pan = _channel[voice.channel].pan >> 2;
            if (pan >= 16)
            {
                amplitudeData = (byte)((amplitude * (31 - pan) / 0x0F) & 0x0F);
                amplitudeData = (byte)(amplitudeData | (amplitude << 4));
            }
            else {
                amplitudeData = (byte)((amplitude * pan / 0x0F) & 0x0F);
                amplitudeData <<= 4;
                amplitudeData = (byte)(amplitudeData | amplitude);
            }

            if (!_playSwitch)
                amplitudeData = 0;

            if (voiceNr >= 6)
                WriteToChip2(voiceNr - 6, amplitudeData);
            else
                WriteToChip1(voiceNr, amplitudeData);
        }

        protected override void GenerateSamples(short[] buf, int pos, int len)
        {
            while (len != 0)
            {
                if (_samplesTillCallback == 0)
                {
                    for (int i = 0; i < _voice.Length; ++i)
                    {
                        if (_voice[i].note == 0xFF)
                            continue;

                        ++_voice[i].ticks;
                        if (_voice[i].turnOff)
                            ++_voice[i].turnOffTicks;

                        UpdateVoiceAmplitude(i);
                        SetupVoiceAmplitude(i);
                    }

                    _samplesTillCallback = _samplesPerCallback;
                    _samplesTillCallbackRemainder += _samplesPerCallbackRemainder;
                    if (_samplesTillCallbackRemainder >= _timerFreq)
                    {
                        _samplesTillCallback++;
                        _samplesTillCallbackRemainder -= _timerFreq;
                    }
                }

                int render = Math.Min(len, _samplesTillCallback);
                len -= render;
                _samplesTillCallback -= render;
                _cms.ReadBuffer(buf, pos, render);
                pos += render * 2;
            }
        }
    }

    internal class MidiPlayer_CMS : MidiPlayer
    {
        public MidiPlayer_CMS(SciVersion version) : base(version)
        {
        }

        public override MidiDriverError Open(ResourceManager resMan)
        {
            if (_driver != null)
                return MidiDriverError.AlreadyOpen;

            _driver = new MidiDriver_CMS(SciEngine.Instance.Mixer, resMan);
            var driverRetVal = _driver.Open();
            if (driverRetVal != MidiDriverError.None)
                return driverRetVal;

            return 0;
        }

        public override void Close()
        {
            _driver.SetTimerCallback(0, null);
            _driver.Dispose();
            _driver = null;
        }

        public override bool HasRhythmChannel
        {
            get { return false; }
        }

        public override byte PlayId
        {
            get { return 9; }
        }

        public override int Polyphony
        {
            get { return 12; }
        }

        public override void PlaySwitch(bool play)
        {
            ((MidiDriver_CMS)_driver).PlaySwitch = play;
        }
    }
}
