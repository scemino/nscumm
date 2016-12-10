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

// FIXME: We don't seem to be sending the polyphony init data, so disable this for now
#define ADLIB_DISABLE_VOICE_MAPPING

using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using NScumm.Core.Audio.SoftSynth;
using System;
using System.Collections.Generic;
using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiDriver_AdLib : EmulatedMidiDriver
    {
        private const bool STEREO = true;

        private const int LeftChannel = 1;
        private const int RightChannel = 2;

        /* Special SCI sound stuff */

        private const int SCI_MIDI_TIME_EXPANSION_PREFIX = 0xF8;
        private const int SCI_MIDI_TIME_EXPANSION_LENGTH = 240;

        private const int SCI_MIDI_EOT = 0xFC;
        private const int SCI_MIDI_SET_SIGNAL = 0xCF;
        private const int SCI_MIDI_SET_POLYPHONY = 0x4B;
        private const int SCI_MIDI_RESET_ON_SUSPEND = 0x4C;
        private const int SCI_MIDI_CHANNEL_MUTE = 0x4E;
        private const int SCI_MIDI_SET_REVERB = 0x50;
        private const int SCI_MIDI_HOLD = 0x52;
        private const int SCI_MIDI_CUMULATIVE_CUE = 0x60;

        private const int SCI_MIDI_SET_SIGNAL_LOOP = 0x7F;

        private class AdLibOperator
        {
            public bool amplitudeMod;
            public bool vibrato;
            public bool envelopeType;
            public bool kbScaleRate;
            public byte frequencyMult;     // (0-15)
            public byte kbScaleLevel;      // (0-3)
            public byte totalLevel;        // (0-63, 0=max, 63=min)
            public byte attackRate;        // (0-15)
            public byte decayRate;         // (0-15)
            public byte sustainLevel;      // (0-15)
            public byte releaseRate;       // (0-15)
            public byte waveForm;          // (0-3)
        }

        private class AdLibModulator
        {
            public byte feedback;          // (0-7)
            public bool algorithm;
        }

        private class AdLibPatch
        {
            public AdLibOperator[] op;
            public AdLibModulator mod;

            public AdLibPatch()
            {
                op = new AdLibOperator[2];
                for (int i = 0; i < op.Length; i++)
                {
                    op[i] = new AdLibOperator();
                }
                mod = new AdLibModulator();
            }
        }

        private class Channel
        {
            public byte patch;            // Patch setting
            public byte volume;           // Channel volume (0-63)
            public byte pan;              // Pan setting (0-127, 64 is center)
            public byte holdPedal;        // Hold pedal setting (0 to 63 is off, 127 to 64 is on)
            public byte extraVoices;      // The number of additional voices this channel optimally needs
            public ushort pitchWheel;      // Pitch wheel setting (0-16383, 8192 is center)
            public byte lastVoice;        // Last voice used for this MIDI channel
            public bool enableVelocity;    // Enable velocity control (SCI0)

            public Channel()
            {
                volume = 63;
                pan = 64;
                pitchWheel = 8192;
            }
        }

        private class AdLibVoice
        {
            public sbyte channel;           // MIDI channel that this voice is assigned to or -1
            public sbyte note;              // Currently playing MIDI note or -1
            public int patch;              // Currently playing patch or -1
            public byte velocity;         // Note velocity
            public bool isSustained;       // Flag indicating a note that is being sustained by the hold pedal
            public ushort age;             // Age of the current note

            public AdLibVoice()
            {
                channel = -1;
                note = -1;
                patch = -1;
            }
        }

        public const int Voices = 9;
        public const int RhythmKeys = 62;

        private bool _stereo;
        private bool _isSCI0;
        private IOpl _opl;
        private bool _playSwitch;
        private int _masterVolume;
        private readonly Channel[] _channels;
        private readonly AdLibVoice[] _voices;
        private byte[] _rhythmKeyMap;
        private List<AdLibPatch> _patches;

        private static readonly byte[] registerOffset = {
            0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x11, 0x12
        };

        private static readonly byte[] velocityMap1 = {
            0x00, 0x0c, 0x0d, 0x0e, 0x0f, 0x11, 0x12, 0x13,
            0x14, 0x16, 0x17, 0x18, 0x1a, 0x1b, 0x1c, 0x1d,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26,
            0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2d, 0x2d, 0x2e,
            0x2f, 0x30, 0x31, 0x32, 0x32, 0x33, 0x34, 0x34,
            0x35, 0x36, 0x36, 0x37, 0x38, 0x38, 0x39, 0x3a,
            0x3b, 0x3b, 0x3b, 0x3c, 0x3c, 0x3c, 0x3d, 0x3d,
            0x3d, 0x3e, 0x3e, 0x3e, 0x3e, 0x3f, 0x3f, 0x3f
        };

        private static readonly byte[] velocityMap2 = {
            0x00, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a,
            0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x21,
            0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
            0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x2f, 0x30,
            0x31, 0x32, 0x32, 0x33, 0x34, 0x34, 0x35, 0x36,
            0x36, 0x37, 0x38, 0x38, 0x39, 0x39, 0x3a, 0x3a,
            0x3b, 0x3b, 0x3b, 0x3c, 0x3c, 0x3c, 0x3d, 0x3d,
            0x3d, 0x3e, 0x3e, 0x3e, 0x3e, 0x3f, 0x3f, 0x3f
        };

        private static readonly int[] ym3812_note = {
            0x157, 0x16b, 0x181, 0x198, 0x1b0, 0x1ca,
            0x1e5, 0x202, 0x220, 0x241, 0x263, 0x287,
            0x2ae
        };

        public override bool IsStereo
        {
            get
            {
                return _stereo;
            }
        }

        public override int Rate
        {
            get
            {
                return _mixer.OutputRate;
            }
        }

        public bool UseRhythmChannel { get { return _rhythmKeyMap != null; } }

        protected override void GenerateSamples(short[] buf, int pos, int len)
        {
            if (IsStereo)
                len <<= 1;
            _opl.ReadBuffer(buf, pos, len);

            // Increase the age of the notes
            for (int i = 0; i < Voices; i++)
            {
                if (_voices[i].note != -1)
                    _voices[i].age++;
            }
        }

        public override int Property(int prop, int param)
        {
            switch (prop)
            {
                case MidiPlayer.MIDI_PROP_MASTER_VOLUME:
                    if (param != 0xffff)
                        _masterVolume = param;
                    return _masterVolume;
            }
            return 0;
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        // MIDI messages can be found at http://www.midi.org/techspecs/midimessages.phps
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
                case 0xb0:
                    switch (op1)
                    {
                        case 0x07:
                            _channels[channel].volume = (byte)(op2 >> 1);
                            RenewNotes(channel, true);
                            break;
                        case 0x0a:
                            _channels[channel].pan = op2;
                            RenewNotes(channel, true);
                            break;
                        case 0x40:
                            _channels[channel].holdPedal = op2;
                            if (op2 == 0)
                            {
                                for (int i = 0; i < Voices; i++)
                                {
                                    if ((_voices[i].channel == channel) && _voices[i].isSustained)
                                        VoiceOff(i);
                                }
                            }
                            break;
                        case 0x4b:
# if !ADLIB_DISABLE_VOICE_MAPPING
                            voiceMapping(channel, op2);
#endif
                            break;
                        case 0x4e:
                            _channels[channel].enableVelocity = op2 != 0;
                            break;
                        case MidiPlayer.SCI_MIDI_CHANNEL_NOTES_OFF:
                            for (int i = 0; i < Voices; i++)
                                if ((_voices[i].channel == channel) && (_voices[i].note != -1))
                                    VoiceOff(i);
                            break;
                        default:
                            Warning($"ADLIB: ignoring MIDI command {command|channel:X2} {op1:X2} {op2:X2}");
                            break;
                    }
                    break;
                case 0xc0:
                    _channels[channel].patch = op1;
                    break;
                // The original AdLib driver from sierra ignores aftertouch completely, so should we
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                    break;
                case 0xe0:
                    _channels[channel].pitchWheel = (ushort)((op1 & 0x7f) | ((op2 & 0x7f) << 7));
                    RenewNotes(channel, true);
                    break;
                default:
                    Warning($"ADLIB: Unknown event {command:X2}");
                    break;
            }
        }

        public MidiDriver_AdLib(IMixer mixer) : base(mixer)
        {
            _playSwitch = true;
            _masterVolume = 15;
            _rhythmKeyMap = null;
            _opl = null;
            _patches = new List<AdLibPatch>();
            _channels = new Channel[MidiPlayer.MIDI_CHANNELS];
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new Channel();
            }
            _voices = new AdLibVoice[Voices];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = new AdLibVoice();
            }
        }

        public int OpenAdLib(bool isSCI0)
        {
            int rate = _mixer.OutputRate;

            _stereo = STEREO;

            Debug(3, "ADLIB: Starting driver in {0} mode", (isSCI0 ? "SCI0" : "SCI1"));
            _isSCI0 = isSCI0;

            _opl = new DosBoxOPL(IsStereo ? OplType.DualOpl2 : OplType.Opl2);

            // Try falling back to mono, thus plain OPL2 emualtor, when no Dual OPL2 is available.
            if (_opl == null && _stereo)
            {
                _stereo = false;
                _opl = new DosBoxOPL(OplType.Opl2);
            }

            if (_opl == null)
                return -1;

            _opl.Init();

            SetRegister(0xBD, 0);
            SetRegister(0x08, 0);
            SetRegister(0x01, 0x20);

            base.Open();

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false);

            return 0;
        }

        public void SetVolume(byte volume)
        {
            _masterVolume = volume;
            RenewNotes(-1, true);
        }

        private void NoteOff(int channel, int note)
        {
            for (int i = 0; i < Voices; i++)
            {
                if ((_voices[i].channel == channel) && (_voices[i].note == note))
                {
                    if (_channels[channel].holdPedal != 0)
                        _voices[i].isSustained = true;
                    else
                        VoiceOff(i);
                    return;
                }
            }
        }

        private void VoiceOn(int voice, int note, int velocity)
        {
            int channel = _voices[voice].channel;
            int patch;

            _voices[voice].age = 0;

            if ((channel == 9) && _rhythmKeyMap != null)
            {
                patch = ScummHelper.Clip(note, 27, 88) + 101;
            }
            else {
                patch = _channels[channel].patch;
            }

            // Set patch if different from current patch
            if (patch != _voices[voice].patch)
                SetPatch(voice, patch);

            _voices[voice].velocity = (byte)velocity;
            SetNote(voice, note, true);
        }

        private void VoiceOff(int voice)
        {
            _voices[voice].isSustained = false;
            SetNote(voice, _voices[voice].note, false);
            _voices[voice].note = -1;
            _voices[voice].age = 0;
        }

        private void SetNote(int voice, int note, bool key)
        {
            int channel = _voices[voice].channel;
            int n, fre, oct;
            float delta;
            int bend = _channels[channel].pitchWheel;

            if ((channel == 9) && _rhythmKeyMap != null)
            {
                note = _rhythmKeyMap[ScummHelper.Clip(note, 27, 88) - 27];
            }

            _voices[voice].note = (sbyte)note;

            n = note % 12;

            if (bend < 8192)
                bend = 8192 - bend;
            delta = (float)Math.Pow(2.0, (bend % 8192) / 8192.0);

            if (bend > 8192)
                fre = (int)(ym3812_note[n] * delta);
            else
                fre = (int)(ym3812_note[n] / delta);

            oct = note / 12 - 1;

            if (oct < 0)
                oct = 0;

            if (oct > 7)
                oct = 7;

            SetRegister(0xA0 + voice, fre & 0xff);
            SetRegister(0xB0 + voice, (key ? 1 << 5 : 0) | (oct << 2) | (fre >> 8));

            SetVelocity(voice);
        }

        private void SetVelocity(int voice)
        {
            AdLibPatch patch = _patches[_voices[voice].patch];
            int pan = _channels[_voices[voice].channel].pan;
            SetVelocityReg(registerOffset[voice] + 3, CalcVelocity(voice, 1), patch.op[1].kbScaleLevel, pan);

            // In AM mode we need to set the level for both operators
            if (_patches[_voices[voice].patch].mod.algorithm)
                SetVelocityReg(registerOffset[voice], CalcVelocity(voice, 0), patch.op[0].kbScaleLevel, pan);
        }

        private int CalcVelocity(int voice, int op)
        {
            if (_isSCI0)
            {
                int velocity = _masterVolume;

                if (velocity > 0)
                    velocity += 3;

                if (velocity > 15)
                    velocity = 15;

                int insVelocity;
                if (_channels[_voices[voice].channel].enableVelocity)
                    insVelocity = _voices[voice].velocity;
                else
                    insVelocity = 63 - _patches[_voices[voice].patch].op[op].totalLevel;

                // Note: Later SCI0 has a static table that is close to this formula, but not exactly the same.
                // Early SCI0 does (velocity * (insVelocity / 15))
                return velocity * insVelocity / 15;
            }
            else {
                AdLibOperator oper = _patches[_voices[voice].patch].op[op];
                int velocity = _channels[_voices[voice].channel].volume + 1;
                velocity = velocity * (velocityMap1[_voices[voice].velocity] + 1) / 64;
                velocity = velocity * (_masterVolume + 1) / 16;

                if (--velocity < 0)
                    velocity = 0;

                return velocityMap2[velocity] * (63 - oper.totalLevel) / 63;
            }
        }

        private void SetVelocityReg(int regOffset, int velocity, int kbScaleLevel, int pan)
        {
            if (!_playSwitch)
                velocity = 0;

            if (IsStereo)
            {
                int velLeft = velocity;
                int velRight = velocity;

                if (pan > 0x40)
                    velLeft = velLeft * (0x7f - pan) / 0x3f;
                else if (pan < 0x40)
                    velRight = velRight * pan / 0x40;

                SetRegister(0x40 + regOffset, (kbScaleLevel << 6) | (63 - velLeft), LeftChannel);
                SetRegister(0x40 + regOffset, (kbScaleLevel << 6) | (63 - velRight), RightChannel);
            }
            else {
                SetRegister(0x40 + regOffset, (kbScaleLevel << 6) | (63 - velocity));
            }
        }

        private void SetPatch(int voice, int patch)
        {
            if ((patch < 0) || (patch >= _patches.Count))
            {
                Warning($"ADLIB: Invalid patch {patch} requested");
                // Substitute instrument 0
                patch = 0;
            }

            _voices[voice].patch = patch;
            AdLibModulator mod = _patches[patch].mod;

            // Set the common settings for both operators
            SetOperator(registerOffset[voice], _patches[patch].op[0]);
            SetOperator(registerOffset[voice] + 3, _patches[patch].op[1]);

            // Set the additional settings for the modulator
            byte algorithm = mod.algorithm ? (byte)1 : (byte)0;
            SetRegister(0xC0 + voice, (mod.feedback << 1) | algorithm);
        }

        private void SetOperator(int reg, AdLibOperator op)
        {
            SetRegister(0x40 + reg, (op.kbScaleLevel << 6) | op.totalLevel);
            SetRegister(0x60 + reg, (op.attackRate << 4) | op.decayRate);
            SetRegister(0x80 + reg, (op.sustainLevel << 4) | op.releaseRate);
            SetRegister(0x20 + reg, (op.amplitudeMod ? 1 << 7 : 0) | (op.vibrato ? 1 << 6 : 0)
                        | (op.envelopeType ? 1 << 5 : 0) | (op.kbScaleRate ? 1 << 4 : 0) | op.frequencyMult);
            SetRegister(0xE0 + reg, op.waveForm);
        }

        private void SetRegister(int reg, int value, int channels = LeftChannel | RightChannel)
        {
            if ((channels & LeftChannel) != 0)
            {
                _opl.Write(0x220, reg);
                _opl.Write(0x221, value);
            }

            if (IsStereo)
            {
                if ((channels & RightChannel) != 0)
                {
                    _opl.Write(0x222, reg);
                    _opl.Write(0x223, value);
                }
            }
        }

        public void PlaySwitch(bool play)
        {
            _playSwitch = play;
            RenewNotes(-1, play);
        }

        public bool LoadResource(byte[] data, int size)
        {
            if ((size != 1344) && (size != 2690) && (size != 5382))
            {
                throw new InvalidOperationException("ADLIB: Unsupported patch format ({size} bytes)");
            }

            for (int i = 0; i < 48; i++)
                LoadInstrument(data, (28 * i));

            if (size == 1344)
            {
                byte[] dummy = new byte[28];

                // Only 48 instruments, add dummies
                for (int i = 0; i < 48; i++)
                    LoadInstrument(dummy, 0);
            }
            else if (size == 2690)
            {
                for (int i = 48; i < 96; i++)
                    LoadInstrument(data, 2 + (28 * i));
            }
            else {
                // SCI1.1 and later
                for (int i = 48; i < 190; i++)
                    LoadInstrument(data, (28 * i));
                _rhythmKeyMap = new byte[RhythmKeys];

                Array.Copy(data, 5320, _rhythmKeyMap, 0, RhythmKeys);
            }

            return true;
        }

        private void LoadInstrument(byte[] data, int offset)
        {
            BytePtr ins = new BytePtr(data, offset);
            AdLibPatch patch = new AdLibPatch();

            // Set data for the operators
            for (int i = 0; i < 2; i++)
            {
                BytePtr op = new BytePtr(ins, i * 13);
                patch.op[i].kbScaleLevel = (byte)(op[0] & 0x3);
                patch.op[i].frequencyMult = (byte)(op[1] & 0xf);
                patch.op[i].attackRate = (byte)(op[3] & 0xf);
                patch.op[i].sustainLevel = (byte)(op[4] & 0xf);
                patch.op[i].envelopeType = op[5] != 0;
                patch.op[i].decayRate = (byte)(op[6] & 0xf);
                patch.op[i].releaseRate = (byte)(op[7] & 0xf);
                patch.op[i].totalLevel = (byte)(op[8] & 0x3f);
                patch.op[i].amplitudeMod = op[9] != 0;
                patch.op[i].vibrato = op[10] != 0;
                patch.op[i].kbScaleRate = op[11] != 0;
            }
            patch.op[0].waveForm = (byte)(ins[26] & 0x3);
            patch.op[1].waveForm = (byte)(ins[27] & 0x3);

            // Set data for the modulator
            patch.mod.feedback = (byte)(ins[2] & 0x7);
            patch.mod.algorithm = ins[12] == 0; // Flag is inverted

            _patches.Add(patch);
        }

        private void VoiceMapping(int channel, int voices)
        {
            int curVoices = 0;

            for (int i = 0; i < Voices; i++)
                if (_voices[i].channel == channel)
                    curVoices++;

            curVoices += _channels[channel].extraVoices;

            if (curVoices < voices)
            {
                Debug(3, "ADLIB: assigning {0} additional voices to channel {1}", voices - curVoices, channel);
                AssignVoices(channel, voices - curVoices);
            }
            else if (curVoices > voices)
            {
                Debug(3, "ADLIB: releasing {0} voices from channel {1}", curVoices - voices, channel);
                ReleaseVoices(channel, curVoices - voices);
                DonateVoices();
            }
        }

        private void AssignVoices(int channel, int voices)
        {
            // assert(voices > 0);

            for (int i = 0; i < Voices; i++)
                if (_voices[i].channel == -1)
                {
                    _voices[i].channel = (sbyte)channel;
                    if (--voices == 0)
                        return;
                }

            _channels[channel].extraVoices += (byte)voices;
        }

        private void ReleaseVoices(int channel, int voices)
        {
            if (_channels[channel].extraVoices >= voices)
            {
                _channels[channel].extraVoices -= (byte)voices;
                return;
            }

            voices -= _channels[channel].extraVoices;
            _channels[channel].extraVoices = 0;

            for (int i = 0; i < Voices; i++)
            {
                if ((_voices[i].channel == channel) && (_voices[i].note == -1))
                {
                    _voices[i].channel = -1;
                    if (--voices == 0)
                        return;
                }
            }

            for (int i = 0; i < Voices; i++)
            {
                if (_voices[i].channel == channel)
                {
                    VoiceOff(i);
                    _voices[i].channel = -1;
                    if (--voices == 0)
                        return;
                }
            }
        }

        private void DonateVoices()
        {
            int freeVoices = 0;

            for (int i = 0; i < Voices; i++)
                if (_voices[i].channel == -1)
                    freeVoices++;

            if (freeVoices == 0)
                return;

            for (int i = 0; i < MidiPlayer.MIDI_CHANNELS; i++)
            {
                if (_channels[i].extraVoices >= freeVoices)
                {
                    AssignVoices(i, freeVoices);
                    _channels[i].extraVoices -= (byte)freeVoices;
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

        private void RenewNotes(int channel, bool key)
        {
            for (int i = 0; i < Voices; i++)
            {
                // Update all notes playing this channel
                if ((channel == -1) || (_voices[i].channel == channel))
                {
                    if (_voices[i].note != -1)
                        SetNote(i, _voices[i].note, key);
                }
            }
        }

        private void NoteOn(int channel, int note, int velocity)
        {
            if (velocity == 0)
            {
                NoteOff(channel, note);
                return;
            }

            velocity >>= 1;

            // Check for playable notes
            if ((note < 12) || (note > 107))
                return;

            for (int i = 0; i < Voices; i++)
            {
                if ((_voices[i].channel == channel) && (_voices[i].note == note))
                {
                    VoiceOff(i);
                    VoiceOn(i, note, velocity);
                    return;
                }
            }

# if ADLIB_DISABLE_VOICE_MAPPING
            int voice = FindVoiceBasic(channel);
#else
            int voice = findVoice(channel);
#endif

            if (voice == -1)
            {
                Debug(3, $"ADLIB: failed to find free voice assigned to channel {channel}");
                return;
            }

            VoiceOn(voice, note, velocity);
        }

        // FIXME: Temporary, see comment at top of file regarding ADLIB_DISABLE_VOICE_MAPPING
        private int FindVoiceBasic(int channel)
        {
            int voice = -1;
            int oldestVoice = -1;
            int oldestAge = -1;

            // Try to find a voice assigned to this channel that is free (round-robin)
            for (int i = 0; i < Voices; i++)
            {
                int v = (_channels[channel].lastVoice + i + 1) % Voices;

                if (_voices[v].note == -1)
                {
                    voice = v;
                    break;
                }

                // We also keep track of the oldest note in case the search fails
                if (_voices[v].age > oldestAge)
                {
                    oldestAge = _voices[v].age;
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

            _voices[voice].channel = (sbyte)channel;
            _channels[channel].lastVoice = (byte)voice;
            return voice;
        }

        private int FindVoice(int channel)
        {
            int voice = -1;
            int oldestVoice = -1;
            uint oldestAge = 0;

            // Try to find a voice assigned to this channel that is free (round-robin)
            for (int i = 0; i < Voices; i++)
            {
                int v = (_channels[channel].lastVoice + i + 1) % Voices;

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
                else {
                    return -1;
                }
            }

            _channels[channel].lastVoice = (byte)voice;
            return voice;
        }
    }

    internal class MidiPlayer_AdLib : MidiPlayer
    {
        public override bool HasRhythmChannel
        {
            get
            {
                return false;
            }
        }

        public override byte PlayId
        {
            get
            {
                switch (_version)
                {
                    case SciVersion.V0_EARLY:
                        return 0x01;
                    case SciVersion.V0_LATE:
                        return 0x04;
                    default:
                        return 0x00;
                }
            }
        }

        public override int Polyphony
        {
            get
            {
                return MidiDriver_AdLib.Voices;
            }
        }

        public override int LastChannel
        {
            get
            {
                return ((MidiDriver_AdLib)_driver).UseRhythmChannel ? 8 : 15;
            }
        }

        public MidiPlayer_AdLib(SciVersion soundVersion)
            : base(soundVersion)
        {
            _driver = new MidiDriver_AdLib(SciEngine.Instance.Mixer);
        }

        public override MidiDriverError Open(ResourceManager resMan)
        {
            // Load up the patch.003 file, parse out the instruments
            var res = resMan.FindResource(new ResourceId(ResourceType.Patch, 3), false);
            bool ok = false;

            if (res != null)
            {
                ok = ((MidiDriver_AdLib)_driver).LoadResource(res.data, res.size);
            }
            else {
                // Early SCI0 games have the sound bank embedded in the AdLib driver

                var path = ScummHelper.LocatePath(SciEngine.Instance.Directory, "ADL.DRV");

                if (path != null)
                {
                    using (Stream f = ServiceLocator.FileStorage.OpenFileRead(path))
                    {
                        var size = (int)f.Length;
                        const int patchSize = 1344;

                        // Note: Funseeker's Guide also has another version of adl.drv, 8803 bytes.
                        // This isn't supported, but it's not really used anywhere, as that demo
                        // doesn't have sound anyway.
                        if ((size == 5684) || (size == 5720) || (size == 5727))
                        {
                            byte[] buf = new byte[patchSize];

                            f.Seek(0x45a, SeekOrigin.Begin);
                            if (f.Read(buf, 0, patchSize) == patchSize)
                                ok = ((MidiDriver_AdLib)_driver).LoadResource(buf, patchSize);
                        }
                    }
                }
            }

            if (!ok)
            {
                Warning("ADLIB: Failed to load patch.003");
                return (MidiDriverError)(-1);
            }

            return (MidiDriverError)((MidiDriver_AdLib)_driver).OpenAdLib(_version <= SciVersion.V0_LATE);
        }

        public override void PlaySwitch(bool play)
        {
            ((MidiDriver_AdLib)_driver).PlaySwitch(play);
        }

        public override byte Volume
        {
            get
            {
                return base.Volume;
            }
            set
            {
                ((MidiDriver_AdLib)_driver).SetVolume(value);
            }
        }
    }
}
