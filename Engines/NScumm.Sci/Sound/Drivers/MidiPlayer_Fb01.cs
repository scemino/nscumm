//
//  MidiPlayer_Midi.cs
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

using NScumm.Core;
using NScumm.Core.Audio;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiPlayer_Fb01 : MidiPlayer
    {
        private const int Voices = 8;
        private const int MaxSysExSize = 264;

        private class Channel
        {
            public byte patch; // Patch setting
            public byte volume; // Channel volume (0-63)
            public byte pan; // Pan setting (0-127, 64 is center)
            public byte holdPedal; // Hold pedal setting (0 to 63 is off, 127 to 64 is on)
            public byte extraVoices; // The number of additional voices this channel optimally needs
            public ushort pitchWheel; // Pitch wheel setting (0-16383, 8192 is center)
            public byte lastVoice; // Last voice used for this MIDI channel
            public bool enableVelocity; // Enable velocity control (SCI0)

            public Channel()
            {
                volume = 127;
                pan = 64;
                pitchWheel = 8192;
            }
        }

        private class Voice
        {
            public sbyte channel; // MIDI channel that this voice is assigned to or -1
            public sbyte note; // Currently playing MIDI note or -1
            public int bank; // Current bank setting or -1
            public int patch; // Currently playing patch or -1
            public byte velocity; // Note velocity
            public bool isSustained; // Flag indicating a note that is being sustained by the hold pedal
            public ushort age; // Age of the current note

            public Voice()
            {
                channel = -1;
                note = -1;
                bank = -1;
                patch = -1;
            }
        }

        private static readonly byte[] VolumeTable =
        {
            0x00, 0x10, 0x14, 0x18, 0x1f, 0x26, 0x2a, 0x2e,
            0x2f, 0x32, 0x33, 0x33, 0x34, 0x35, 0x35, 0x36,
            0x36, 0x37, 0x37, 0x38, 0x38, 0x38, 0x39, 0x39,
            0x39, 0x3a, 0x3a, 0x3a, 0x3a, 0x3a, 0x3b, 0x3b,
            0x3b, 0x3b, 0x3b, 0x3c, 0x3c, 0x3c, 0x3c, 0x3c,
            0x3d, 0x3d, 0x3d, 0x3d, 0x3d, 0x3e, 0x3e, 0x3e,
            0x3e, 0x3e, 0x3e, 0x3f, 0x3f, 0x3f, 0x3f, 0x3f,
            0x3f, 0x3f, 0x3f, 0x3f, 0x3f, 0x3f, 0x3f, 0x3f
        };

        private readonly bool _playSwitch;
        private int _masterVolume;

        private readonly Channel[] _channels = new Channel[16];
        private readonly Voice[] _voices = new Voice[Voices];
        private readonly byte[] _sysExBuf = new byte[MaxSysExSize];
        private MidiDriver.TimerProc _timerProc;
        private object _timerParam;

        public override byte Volume
        {
            get { return (byte) _masterVolume; }
            set
            {
                _masterVolume = value;

                for (var i = 0; i < MIDI_CHANNELS; i++)
                    ControlChange(i, 0x07, _channels[i].volume & 0x7f);
            }
        }

        public override bool HasRhythmChannel => false;

        public override int Polyphony => Voices; // 9 in SCI1?

        public override byte PlayId
        {
            get
            {
                switch (_version)
                {
                    case SciVersion.V0_EARLY:
                        return 0x01;
                    case SciVersion.V0_LATE:
                        return 0x02;
                    default:
                        return 0x00;
                }
            }
        }

        public MidiPlayer_Fb01(SciVersion version)
            : base(version)
        {
            _playSwitch = true;
            _masterVolume = 15;
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new Channel();
            }
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = new Voice();
            }

            var dev = MidiDriver.DetectDevice(MusicDriverTypes.Midi, SciEngine.Instance.Settings.AudioDevice);
            _driver = (MidiDriver) MidiDriver.CreateMidi(SciEngine.Instance.Mixer, dev);

            _sysExBuf[0] = 0x43;
            _sysExBuf[1] = 0x75;
        }

        public override void PlaySwitch(bool play)
        {
        }

        public override MidiDriverError Open(ResourceManager resMan)
        {
            System.Diagnostics.Debug.Assert(resMan != null);

            var retval = _driver.Open();
            if (retval != 0)
            {
                Warning("Failed to open MIDI driver");
                return retval;
            }

            // Set system channel to 0. We send this command over all 16 system channels
            for (var i = 0; i < 16; i++)
                SetSystemParam((byte) i, 0x20, 0);

            // Turn off memory protection
            SetSystemParam(0, 0x21, 0);

            var res = resMan.FindResource(new ResourceId(ResourceType.Patch, 2), false);

            if (res != null)
            {
                SendBanks(res.data, res.size);
            }
            else
            {
                // Early SCI0 games have the sound bank embedded in the IMF driver.
                // Note that these games didn't actually support the FB-01 as a device,
                // but the IMF, which is the same device on an ISA card. Check:
                // http://wiki.vintage-computer.com/index.php/IBM_Music_feature_card

                Warning("FB-01 patch file not found, attempting to load sound bank from IMF.DRV");
                // Try to load sound bank from IMF.DRV
                var f = Core.Engine.OpenFileRead("IMF.DRV");

                if (f != null)
                {
                    var size = f.Length;
                    var buf = new byte[size];

                    f.Read(buf, 0, (int) size);

                    // Search for start of sound bank
                    int offset;
                    for (offset = 0; offset < size; ++offset)
                    {
                        if (string.Equals(buf.GetRawText(offset), "SIERRA "))
                            break;
                    }

                    // Skip to voice data
                    offset += 0x20;

                    if (offset >= size)
                        Error("Failed to locate start of FB-01 sound bank");

                    SendBanks(new BytePtr(buf , offset), (int) (size - offset));
                }
                else
                    Error("Failed to open IMF.DRV");
            }

            // Set up voices to use MIDI channels 0 - 7
            for (var i = 0; i < Voices; i++)
                SetVoiceParam((byte) i, 1, (byte) i);

            InitVoices();

            // Set master volume
            SetSystemParam(0, 0x24, 0x7f);

            return 0;
        }

        public override void Close()
        {
            _driver.Dispose();
        }

        public override void SetTimerCallback(object timerParam, MidiDriver.TimerProc timerProc)
        {
            _driver.SetTimerCallback(null, null);

            _timerParam = timerParam;
            _timerProc = timerProc;

            _driver.SetTimerCallback(this, MidiTimerCallback);
        }

        public override void SysEx(BytePtr msg, ushort length)
        {
            _driver.SysEx(msg, length);

            // Wait the time it takes to send the SysEx data
            var delay = (length + 2) * 1000 / 3125;

            delay += 10;

            ServiceLocator.Platform.Sleep(delay);
            SciEngine.Instance.System.GraphicsManager.UpdateScreen();
        }

        public override void Send(int b)
        {
            var command = (byte) (b & 0xf0);
            var channel = (byte) (b & 0xf);
            var op1 = (byte) ((b >> 8) & 0x7f);
            var op2 = (byte) ((b >> 16) & 0x7f);

            switch (command)
            {
                case 0x80:
                    NoteOff(channel, op1);
                    break;
                case 0x90:
                    NoteOn(channel, op1, op2);
                    break;
                case 0xb0:
                    ControlChange(channel, op1, op2);
                    break;
                case 0xc0:
                    SetPatch(channel, op1);
                    break;
                case 0xe0:
                    _channels[channel].pitchWheel = (ushort) ((op1 & 0x7f) | ((op2 & 0x7f) << 7));
                    SendToChannel(channel, command, op1, op2);
                    break;
                default:
                    Warning("FB-01: Ignoring MIDI event {0:X2} {1:X2} {2:X2}", command | channel, op1, op2);
                    break;
            }
        }

        private static void MidiTimerCallback(object p)
        {
            var m = (MidiPlayer_Fb01) p;

            // Increase the age of the notes
            for (var i = 0; i < Voices; i++)
            {
                if (m._voices[i].note != -1)
                    m._voices[i].age++;
            }

            m._timerProc?.Invoke(m._timerParam);
        }

        private void SetVoiceParam(byte voice, byte param, byte value)
        {
            _sysExBuf[2] = 0x00;
            _sysExBuf[3] = (byte) (0x18 | voice);
            _sysExBuf[4] = param;
            _sysExBuf[5] = value;

            _driver.SysEx(_sysExBuf, 6);
        }

        private void SetSystemParam(byte sysChan, byte param, byte value)
        {
            _sysExBuf[2] = sysChan;
            _sysExBuf[3] = 0x10;
            _sysExBuf[4] = param;
            _sysExBuf[5] = value;

            SysEx(_sysExBuf, 6);
        }

        private void SendVoiceData(byte instrument, BytePtr data)
        {
            _sysExBuf[2] = 0x00;
            _sysExBuf[3] = (byte) (0x08 | instrument);
            _sysExBuf[4] = 0x00;
            _sysExBuf[5] = 0x00;
            _sysExBuf[6] = 0x01;
            _sysExBuf[7] = 0x00;

            for (var i = 0; i < 64; i++)
            {
                _sysExBuf[8 + i * 2] = (byte) (data[i] & 0xf);
                _sysExBuf[8 + i * 2 + 1] = (byte) (data[i] >> 4);
            }

            byte checksum = 0;
            for (var i = 8; i < 136; i++)
                checksum += _sysExBuf[i];

            _sysExBuf[136] = (byte) ((-checksum) & 0x7f);

            SysEx(_sysExBuf, 137);
        }

        private void StoreVoiceData(byte instrument, byte bank, byte index)
        {
            _sysExBuf[2] = 0x00;
            _sysExBuf[3] = (byte) (0x28 | instrument);
            _sysExBuf[4] = 0x40;
            _sysExBuf[5] = (byte) ((bank > 0 ? 48 : 0) + index);

            SysEx(_sysExBuf, 6);
        }

        private void SendBanks(BytePtr data, int size)
        {
            if (size < 3072)
                Error("Failed to read FB-01 patch");

            // SSCI sends bank dumps containing 48 instruments at once. We cannot do that
            // due to the limited maximum SysEx length. Instead we send the instruments
            // one by one and store them in the banks.
            for (var i = 0; i < 48; i++)
            {
                SendVoiceData(0, new BytePtr(data, i * 64));
                StoreVoiceData(0, 0, (byte) i);
            }

            // Send second bank if available
            if ((size >= 6146) && (data.ToUInt16BigEndian(3072) == 0xabcd))
            {
                for (var i = 0; i < 48; i++)
                {
                    SendVoiceData(0, new BytePtr(data, 3074 + i * 64));
                    StoreVoiceData(0, 1, (byte) i);
                }
            }
        }

        private void InitVoices()
        {
            var i = 2;
            _sysExBuf[i++] = 0x70;

            // Set all MIDI channels to 0 voices
            for (var j = 0; j < MIDI_CHANNELS; j++)
            {
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x00;
                _sysExBuf[i++] = 0x00;
            }

            // Set up the 8 MIDI channels we will be using
            for (var j = 0; j < 8; j++)
            {
                // One voice
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x00;
                _sysExBuf[i++] = 0x01;

                // Full range of keys
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x02;
                _sysExBuf[i++] = 0x7f;
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x03;
                _sysExBuf[i++] = 0x00;

                // Voice bank 0
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x04;
                _sysExBuf[i++] = 0x00;

                // Voice 10
                _sysExBuf[i++] = (byte) (0x70 | j);
                _sysExBuf[i++] = 0x05;
                _sysExBuf[i++] = 0x0a;
            }

            SysEx(_sysExBuf, (ushort) i);
        }

        private void VoiceMapping(int channel, int voices)
        {
            var curVoices = 0;

            for (var i = 0; i < Voices; i++)
                if (_voices[i].channel == channel)
                    curVoices++;

            curVoices += _channels[channel].extraVoices;

            if (curVoices < voices)
            {
                Debug(3, "FB-01: assigning {0} additional voices to channel {1}", voices - curVoices, channel);
                AssignVoices(channel, voices - curVoices);
            }
            else if (curVoices > voices)
            {
                Debug(3, "FB-01: releasing {0} voices from channel {1}", curVoices - voices, channel);
                ReleaseVoices(channel, curVoices - voices);
                DonateVoices();
            }
        }

        private void AssignVoices(int channel, int voices)
        {
            System.Diagnostics.Debug.Assert(voices > 0);

            for (var i = 0; i < Voices; i++)
            {
                if (_voices[i].channel != -1) continue;

                _voices[i].channel = (sbyte) channel;
                if (--voices == 0)
                    break;
            }

            _channels[channel].extraVoices = (byte) (_channels[channel].extraVoices + voices);
            SetPatch(channel, _channels[channel].patch);
            SendToChannel((byte) channel, 0xe0, (byte) (_channels[channel].pitchWheel & 0x7f),
                (byte) (_channels[channel].pitchWheel >> 7));
            ControlChange(channel, 0x07, _channels[channel].volume);
            ControlChange(channel, 0x0a, _channels[channel].pan);
            ControlChange(channel, 0x40, _channels[channel].holdPedal);
        }

        private void ReleaseVoices(int channel, int voices)
        {
            if (_channels[channel].extraVoices >= voices)
            {
                _channels[channel].extraVoices = (byte) (_channels[channel].extraVoices - voices);
                return;
            }

            voices -= _channels[channel].extraVoices;
            _channels[channel].extraVoices = 0;

            for (var i = 0; i < Voices; i++)
            {
                if ((_voices[i].channel != channel) || (_voices[i].note != -1)) continue;

                _voices[i].channel = -1;
                if (--voices == 0)
                    return;
            }

            for (var i = 0; i < Voices; i++)
            {
                if (_voices[i].channel != channel) continue;

                VoiceOff(i);
                _voices[i].channel = -1;
                if (--voices == 0)
                    return;
            }
        }

        private void DonateVoices()
        {
            var freeVoices = 0;

            for (var i = 0; i < Voices; i++)
                if (_voices[i].channel == -1)
                    freeVoices++;

            if (freeVoices == 0)
                return;

            for (var i = 0; i < MIDI_CHANNELS; i++)
            {
                if (_channels[i].extraVoices >= freeVoices)
                {
                    AssignVoices(i, freeVoices);
                    _channels[i].extraVoices = (byte) (_channels[i].extraVoices - freeVoices);
                    return;
                }
                if (_channels[i].extraVoices > 0)
                {
                    AssignVoices(i, _channels[i].extraVoices);
                    freeVoices -= _channels[i].extraVoices;
                    _channels[i].extraVoices = 0;
                }
            }
        }

        private int FindVoice(int channel)
        {
            var voice = -1;
            var oldestVoice = -1;
            uint oldestAge = 0;

            // Try to find a voice assigned to this channel that is free (round-robin)
            for (var i = 0; i < Voices; i++)
            {
                var v = (_channels[channel].lastVoice + i + 1) % Voices;

                if (_voices[v].channel == channel)
                {
                    if (_voices[v].note == -1)
                    {
                        voice = v;
                        break;
                    }

                    // We also keep track of the oldest note in case the search fails
                    // Notes started in the current time slice will not be selected
                    if (_voices[v].age > oldestAge)
                    {
                        oldestAge = _voices[v].age;
                        oldestVoice = v;
                    }
                }
            }

            if (voice == -1)
            {
                if (oldestVoice >= 0)
                {
                    VoiceOff(oldestVoice);
                    voice = oldestVoice;
                }
                else
                {
                    return -1;
                }
            }

            _channels[channel].lastVoice = (byte) voice;
            return voice;
        }

        private void SendToChannel(byte channel, byte command, byte op1, byte op2)
        {
            for (var i = 0; i < Voices; i++)
            {
                // Send command to all voices assigned to this channel
                if (_voices[i].channel == channel)
                    _driver.Send((byte) (command | i), op1, op2);
            }
        }

        private void SetPatch(int channel, int patch)
        {
            var bank = 0;

            _channels[channel].patch = (byte) patch;

            if (patch >= 48)
            {
                patch -= 48;
                bank = 1;
            }

            for (var voice = 0; voice < Voices; voice++)
            {
                if (_voices[voice].channel == channel)
                {
                    if (_voices[voice].bank != bank)
                    {
                        _voices[voice].bank = bank;
                        SetVoiceParam((byte) voice, 4, (byte) bank);
                    }
                    _driver.Send((byte) (0xc0 | voice), (byte) patch, 0);
                }
            }
        }

        private void VoiceOn(int voice, int note, int velocity)
        {
            if (_playSwitch)
            {
                _voices[voice].note = (sbyte) note;
                _voices[voice].age = 0;
                _driver.Send((byte) (0x90 | voice), (byte) note, (byte) velocity);
            }
        }

        private void VoiceOff(int voice)
        {
            _voices[voice].note = -1;
            _driver.Send((byte) (0xb0 | voice), 0x7b, 0x00);
        }

        private void NoteOff(int channel, int note)
        {
            int voice;
            for (voice = 0; voice < Voices; voice++)
            {
                if ((_voices[voice].channel != channel) || (_voices[voice].note != note)) continue;

                VoiceOff(voice);
                return;
            }
        }

        private void NoteOn(int channel, int note, int velocity)
        {
            if (velocity == 0)
            {
                NoteOff(channel, note);
                return;
            }

            if (_version > SciVersion.V0_LATE)
                velocity = VolumeTable[velocity >> 1] << 1;

            int voice;
            for (voice = 0; voice < Voices; voice++)
            {
                if ((_voices[voice].channel != channel) || (_voices[voice].note != note)) continue;

                VoiceOff(voice);
                VoiceOn(voice, note, velocity);
                return;
            }

            voice = FindVoice(channel);

            if (voice == -1)
            {
                Debug(3, "FB-01: failed to find free voice assigned to channel %i", channel);
                return;
            }

            VoiceOn(voice, note, velocity);
        }

        private void ControlChange(int channel, int control, int value)
        {
            switch (control)
            {
                case 0x07:
                {
                    _channels[channel].volume = (byte) value;

                    if (_version > SciVersion.V0_LATE)
                        value = VolumeTable[value >> 1] << 1;

                    var vol = (byte) _masterVolume;

                    if (vol > 0)
                        vol = (byte) ScummHelper.Clip(vol + 3, 0, 15);

                    SendToChannel((byte) channel, 0xb0, (byte) control, (byte) ((value * vol / 15) & 0x7f));
                    break;
                }
                case 0x0a:
                    _channels[channel].pan = (byte) value;
                    SendToChannel((byte) channel, 0xb0, (byte) control, (byte) value);
                    break;
                case 0x40:
                    _channels[channel].holdPedal = (byte) value;
                    SendToChannel((byte) channel, 0xb0, (byte) control, (byte) value);
                    break;
                case 0x4b:
                    // In early SCI0, voice count 15 signifies that the channel should be ignored
                    // for this song. Assuming that there are no embedded voice count commands in
                    // the MIDI stream, we should be able to get away with simply setting the voice
                    // count for this channel to 0.
                    VoiceMapping(channel, (value != 15 ? value : 0));
                    break;
                case 0x7b:
                    for (var i = 0; i < Voices; i++)
                        if ((_voices[i].channel == channel) && (_voices[i].note != -1))
                            VoiceOff(i);
                    break;
            }
        }
    }
}