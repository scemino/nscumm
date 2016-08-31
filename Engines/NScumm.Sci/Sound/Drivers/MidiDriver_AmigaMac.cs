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
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiDriver_AmigaMac : EmulatedMidiDriver
    {
        public const int Voices = 4;

        const int kModeLoop = 1 << 0; // Instrument looping flag
        const int kModePitch = 1 << 1; // Instrument pitch changes flag

        const int Channels = 10;
        const int BaseFreq = 20000; // Samplerate of the instrument bank
        const int PanLeft = 91;
        const int PanRight = 164;

        class Channel
        {
            public int instrument;
            public int volume;
            public int pan;
            public ushort pitch;
        }

        class Envelope
        {
            public int length; // Phase period length in samples
            public int delta; // Velocity delta per period
            public int target; // Target velocity
        }

        class Voice
        {
            public int instrument;
            public int note;
            public int note_velocity;
            public int velocity;
            public int envelope;
            public int envelope_samples; // Number of samples till next envelope event
            public int decay;
            public int looping;
            public int hw_channel;
            public int offset;
            public int rate;
        }

        class InstrumentSample
        {
            public string name;
            public int mode;
            public int size; // Size of non-looping part in bytes
            public int loop_size; // Starting offset and size of loop in bytes
            public int transpose; // Transpose value in semitones
            public Envelope[] envelope = new Envelope[4]; // Envelope
            public sbyte[] samples;
            public sbyte[] loop;
            public short startNote;
            public short endNote;
            public bool isUnsigned;
            public ushort baseFreq;
            public ushort baseNote;
            public short fixedNote;
        }

        class Instrument : List<InstrumentSample>
        {
            public string name;
        }

        class Bank
        {
            public string name;
            public int size;
            public Instrument[] instruments;
        }

        bool _isSci1;
        bool _isSci1Early; // KQ1/MUMG Amiga, patch 5
        bool _playSwitch;
        int _masterVolume;
        int _frequency;
        Envelope _envDecay = new Envelope();
        Bank _bank; // Instrument bank
        double[] _freqTable = new double[48];

        Channel[] _channels = new Channel[MidiPlayer.MIDI_CHANNELS];
        /* Internal channels */
        Voice[] _voices = new Voice[Channels];


        public MidiDriver_AmigaMac(IMixer mixer) : base(mixer)
        {
            _playSwitch = true;
            _masterVolume = 15;
        }

        public override bool IsStereo { get; }
        public override int Rate { get; }

        private void SetEnvelope(Voice channel, Envelope[] envelope, int phase)
        {
            channel.envelope = phase;
            channel.envelope_samples = envelope[phase].length;

            if (phase == 0)
                channel.velocity = channel.note_velocity / 2;
            else
                channel.velocity = envelope[phase - 1].target;
        }

        private int Interpolate(sbyte[] samples, int offset, bool isUnsigned)
        {
            int x = FixedPointFractionHelper.FracToInt(offset);
            int diff;

            if (isUnsigned)
            {
                int s1 = (byte)samples[x] - 0x80;
                int s2 = (byte)samples[x + 1] - 0x80;
                diff = (s2 - s1) << 8;
                return (s1 << 8) +
                       FixedPointFractionHelper.FracToInt(
                           (int)(diff * (offset & FixedPointFractionHelper.FRAC_LO_MASK)));
            }

            diff = (samples[x + 1] - samples[x]) << 8;
            return (samples[x] << 8) +
                   FixedPointFractionHelper.FracToInt((int)(diff * (offset & FixedPointFractionHelper.FRAC_LO_MASK)));
        }

        private void PlayInstrument(Ptr<short> dest, Voice channel, int count)
        {
            int index = 0;
            int vol = _channels[channel.hw_channel].volume;
            InstrumentSample instrument = FindInstrument(channel.instrument, channel.note);

            while (true)
            {
                /* Available source samples until end of segment */
                int lin_avail;
                int seg_end, rem, i, amount;
                sbyte[] samples;

                if (channel.looping != 0 && instrument.loop != null)
                {
                    samples = instrument.loop;
                    seg_end = instrument.loop_size;
                }
                else
                {
                    samples = instrument.samples;
                    seg_end = instrument.size;
                }

                lin_avail = FixedPointFractionHelper.IntToFrac((short)seg_end) - channel.offset;

                rem = count - index;

                /* Amount of destination samples that we will compute this iteration */
                amount = lin_avail / channel.rate;

                if ((lin_avail % channel.rate) != 0)
                    amount++;

                if (amount > rem)
                    amount = rem;

                /* Stop at next envelope event */
                if ((channel.envelope_samples != -1) && (amount > (int)channel.envelope_samples))
                    amount = channel.envelope_samples;

                for (i = 0; i < amount; i++)
                {
                    dest[index++] =
                        (short)(Interpolate(samples, channel.offset, instrument.isUnsigned) * channel.velocity /
                                 64 * channel.note_velocity * vol / (127 * 127));
                    channel.offset += channel.rate;
                }

                if (channel.envelope_samples != -1)
                    channel.envelope_samples -= amount;

                if (channel.envelope_samples == 0)
                {
                    Envelope envelope;
                    int delta, target, velocity;

                    if (channel.decay != 0)
                        envelope = _envDecay;
                    else
                        envelope = instrument.envelope[channel.envelope];

                    delta = envelope.delta;
                    target = envelope.target;
                    velocity = channel.velocity - envelope.delta;

                    /* Check whether we have reached the velocity target for the current phase */
                    if ((delta >= 0 && velocity <= target) || (delta < 0 && velocity >= target))
                    {
                        channel.velocity = target;

                        /* Stop note after velocity has dropped to 0 */
                        if (target == 0)
                        {
                            channel.note = -1;
                            break;
                        }
                        else
                            switch (channel.envelope)
                            {
                                case 0:
                                case 2:
                                    /* Go to next phase */
                                    SetEnvelope(channel, instrument.envelope, channel.envelope + 1);
                                    break;
                                case 1:
                                case 3:
                                    /* Stop envelope */
                                    channel.envelope_samples = -1;
                                    break;
                            }
                    }
                    else
                    {
                        /* We haven't reached the target yet */
                        channel.envelope_samples = envelope.length;
                        channel.velocity = velocity;
                    }
                }

                if (index == count)
                    break;

                if (FixedPointFractionHelper.FracToInt(channel.offset) >= seg_end)
                {
                    if ((instrument.mode & kModeLoop) != 0)
                    {
                        /* Loop the samples */
                        channel.offset -= FixedPointFractionHelper.IntToFrac((short)seg_end);
                        channel.looping = 1;
                    }
                    else
                    {
                        /* All samples have been played */
                        channel.note = -1;
                        break;
                    }
                }
            }
        }

        private void ChangeInstrument(int channel, int instrument)
        {
            if (((uint)instrument < _bank.instruments.Length) && (_bank.instruments[instrument].Count > 0))
                DebugC(1, DebugLevels.Sound, "Amiga/Mac driver: Setting channel {0} to \"{1}\" ({2})", channel,
                    _bank.instruments[instrument].name, instrument);
            else
                DebugC(DebugLevels.Sound, "Amiga/Mac driver: instrument {0} does not exist (channel {1})", instrument,
                    channel);
            _channels[channel].instrument = instrument;
        }

        private void StopChannel(int ch)
        {
            int i;

            /* Start decay phase for note on this hw channel, if any */
            for (i = 0; i < Channels; i++)
                if (_voices[i].note != -1 && _voices[i].hw_channel == ch && _voices[i].decay == 0)
                {
                    /* Trigger fast decay envelope */
                    _voices[i].decay = 1;
                    _voices[i].envelope_samples = _envDecay.length;
                    break;
                }
        }

        private void PitchWheel(int ch, ushort pitch)
        {
            _channels[ch].pitch = pitch;

            for (int i = 0; i < Channels; i++)
                if (_voices[i].note != -1 && _voices[i].hw_channel == ch)
                    SetOutputFrac(i);
        }

        private void StopNote(int ch, int note)
        {
            int channel;

            for (channel = 0; channel < Channels; channel++)
                if (_voices[channel].note == note && _voices[channel].hw_channel == ch && _voices[channel].decay == 0)
                    break;

            if (channel == Channels)
            {
                DebugC(1, DebugLevels.Sound, "Amiga/Mac driver: cannot stop note {0} on channel {1}", note, ch);
                return;
            }

            InstrumentSample instrument = FindInstrument(_voices[channel].instrument, note);

            // FIXME: SCI1 envelope support is not perfect yet

            /* Start the envelope phases for note-off if looping is on and envelope is enabled */
            if (((instrument.mode & kModeLoop) != 0) && (instrument.envelope[0].length != 0))
                SetEnvelope(_voices[channel], instrument.envelope, 2);
        }

        private InstrumentSample FindInstrument(int instrument, int note)
        {
            if ((uint)instrument >= _bank.instruments.Length)
                return null;

            for (var i = 0; i < _bank.instruments[instrument].Count; i++)
            {
                InstrumentSample sample = _bank.instruments[instrument][i];
                if (note >= sample.startNote && note <= sample.endNote)
                    return sample;
            }

            return null;
        }

        private void SetOutputFrac(int voice)
        {
            InstrumentSample instrument = FindInstrument(_voices[voice].instrument, _voices[voice].note);

            int fnote = 0;

            if (instrument.fixedNote == -1)
            {
                fnote = _voices[voice].note;

                // Handle SCI0-style transposing here
                if (!_isSci1)
                    fnote += instrument.transpose;

                if (fnote < 0 || fnote > 127)
                {
                    Warning("Amiga/Mac driver: illegal note %i", fnote);
                    return;
                }
            }
            else
                fnote = instrument.fixedNote;

            // Compute rate for note
            int mulFact = 1, divFact = 1;

            fnote -= instrument.baseNote;
            fnote *= 4;
            // FIXME: check how SSCI maps this
            fnote += (_channels[_voices[voice].hw_channel].pitch - 0x2000) / 169;

            while (fnote < 0)
            {
                divFact *= 2;
                fnote += 12 * 4;
            }

            while (fnote >= 12 * 4)
            {
                mulFact *= 2;
                fnote -= 12 * 4;
            }

            double freq = _freqTable[fnote] * instrument.baseFreq * mulFact / divFact;

            // Handle SCI1-style transposing here
            if (instrument.transpose != 0 && _isSci1)
                freq = freq + ((_freqTable[4] - 1.0) * freq * (double)instrument.transpose / (double)16);

            _voices[voice].rate = FixedPointFractionHelper.DoubleToFrac(freq / _frequency);
        }

        private void StartNote(int ch, int note, int velocity)
        {
            int channel;

            if (_channels[ch].instrument < 0 || _channels[ch].instrument > 255)
            {
                Warning("Amiga/Mac driver: invalid instrument %i on channel %i", _channels[ch].instrument, ch);
                return;
            }

            InstrumentSample instrument = FindInstrument(_channels[ch].instrument, note);

            if (instrument == null)
            {
                Warning("Amiga/Mac driver: instrument %i does not exist", _channels[ch].instrument);
                return;
            }

            for (channel = 0; channel < Channels; channel++)
                if (_voices[channel].note == -1)
                    break;

            if (channel == Channels)
            {
                Warning("Amiga/Mac driver: could not find a free channel");
                return;
            }

            StopChannel(ch);

            _voices[channel].instrument = _channels[ch].instrument;
            _voices[channel].note = note;
            _voices[channel].note_velocity = velocity;

            if (((instrument.mode & kModeLoop) != 0) && (instrument.envelope[0].length != 0))
                SetEnvelope(_voices[channel], instrument.envelope, 0);
            else
            {
                _voices[channel].velocity = 64;
                _voices[channel].envelope_samples = -1;
            }

            _voices[channel].offset = 0;
            _voices[channel].hw_channel = ch;
            _voices[channel].decay = 0;
            _voices[channel].looping = 0;
            SetOutputFrac(channel);
        }

        private InstrumentSample ReadInstrumentSCI0(Stream file, out int id)
        {
            byte[] header = new byte[61];
            var br = new BinaryReader(file);

            if (file.Read(header, 0, 61) < 61)
            {
                Warning("Amiga/Mac driver: failed to read instrument header");
                id = 0;
                return null;
            }

            int[] seg_size =
            {
                (short) header.ToInt16BigEndian(35) * 2,
                (short) header.ToInt16BigEndian(41) * 2,
                (short) header.ToInt16BigEndian(47) * 2
            };

            InstrumentSample instrument = new InstrumentSample();

            instrument.startNote = 0;
            instrument.endNote = 127;
            instrument.isUnsigned = false;
            instrument.baseFreq = BaseFreq;
            instrument.baseNote = 101;
            instrument.fixedNote = 101;

            instrument.mode = header[33];
            instrument.transpose = (sbyte)header[34];
            for (int i = 0; i < 4; i++)
            {
                int length = (sbyte)header[49 + i];

                if (length == 0 && i > 0)
                    length = 256;

                instrument.envelope[i].length = length * _frequency / 60;
                instrument.envelope[i].delta = (sbyte)header[53 + i];
                instrument.envelope[i].target = header[57 + i];
            }
            /* Final target must be 0 */
            instrument.envelope[3].target = 0;

            int loop_offset = header.ToInt32BigEndian(37) & ~1;
            int size = seg_size[0] + seg_size[1] + seg_size[2];

            id = header.ToUInt16BigEndian();

            instrument.name = header.GetRawText(2, 29);

            if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.Sound))
            {
                Debug("Amiga/Mac driver: Reading instrument {0}: \"{1}\" ({2} bytes)",
                    id, instrument.name, size);
                DebugN("    Mode: {0:X2} (", header[33]);
                DebugN("looping: {0}, ", (header[33] & kModeLoop) != 0 ? "on" : "off");
                Debug("pitch changes: {0})", (header[33] & kModePitch) != 0 ? "on" : "off");
                Debug("    Transpose: {0}", (sbyte)header[34]);
                for (int i = 0; i < 3; i++)
                    Debug("    Segment {0}: {1} words @ offset {2}", i, (short)header.ToInt16BigEndian(35 + 6 * i),
                        (i == 0 ? 0 : (int)header.ToInt32BigEndian(31 + 6 * i)));
                for (int i = 0; i < 4; i++)
                    Debug("    Envelope {0}: period {1} / delta {2} / target {3}", i, header[49 + i],
                        (sbyte)header[53 + i],
                        header[57 + i]);
            }

            instrument.samples = br.ReadSBytes(size + 1);

            if ((instrument.mode & kModePitch) == 0)
                instrument.fixedNote = -1;

            if ((instrument.mode & kModeLoop) != 0)
            {
                if (loop_offset + seg_size[1] > size)
                {
                    DebugC(DebugLevels.Sound,
                        "Amiga/Mac driver: looping samples extend %i bytes past end of sample block",
                        loop_offset + seg_size[1] - size);
                    seg_size[1] = size - loop_offset;
                }

                if (seg_size[1] < 0)
                {
                    Warning("Amiga/Mac driver: invalid looping point");
                    instrument.samples = null;
                    return null;
                }

                instrument.size = seg_size[0];
                instrument.loop_size = seg_size[1];

                instrument.loop = new sbyte[instrument.loop_size + 1];
                Array.Copy(instrument.samples, loop_offset, instrument.loop, 0, instrument.loop_size);

                instrument.samples[instrument.size] = instrument.loop[0];
                instrument.loop[instrument.loop_size] = instrument.loop[0];
            }
            else
            {
                instrument.loop = null;
                instrument.loop_size = 0;
                instrument.size = size;
                instrument.samples[instrument.size] = 0;
            }

            return instrument;
        }

        public override int Property(int prop, int param)
        {
            if (prop == MidiPlayer.MIDI_PROP_MASTER_VOLUME)
            {
                if (param != 0xffff)
                    _masterVolume = param;
                return _masterVolume;
            }
            return 0;
        }

        public override MidiDriverError Open()
        {
            _isSci1 = false;
            _isSci1Early = false;

            for (int i = 0; i < 48; i++)
                _freqTable[i] = Math.Pow(2, i / (double)48);

            _frequency = _mixer.OutputRate;
            _envDecay.length = _frequency / (32 * 64);
            _envDecay.delta = 1;
            _envDecay.target = 0;

            for (int i = 0; i < Channels; i++)
            {
                _voices[i].note = -1;
                _voices[i].hw_channel = 0;
            }

            for (int i = 0; i < MidiPlayer.MIDI_CHANNELS; i++)
            {
                _channels[i].instrument = -1;
                _channels[i].volume = 127;
                _channels[i].pan = (i % 4 == 0 || i % 4 == 3 ? PanLeft : PanRight);
                _channels[i].pitch = 0x2000;
            }

            var file = Core.Engine.OpenFileRead("bank.001");

            if (file != null)
            {
                if (!LoadInstrumentsSCI0(file))
                {
                    file.Dispose();
                    return MidiDriverError.UnknownError;
                }
                file.Dispose();
            }
            else
            {
                ResourceManager resMan = SciEngine.Instance.ResMan;

                ResourceManager.ResourceSource.Resource resource =
                    resMan.FindResource(new ResourceId(ResourceType.Patch, 7), false); // Mac
                if (resource == null)
                    resource = resMan.FindResource(new ResourceId(ResourceType.Patch, 9), false); // Amiga

                if (resource == null)
                {
                    resource = resMan.FindResource(new ResourceId(ResourceType.Patch, 5), false); // KQ1/MUMG Amiga
                    if (resource != null)
                        _isSci1Early = true;
                }

                // If we have a patch by this point, it's SCI1
                if (resource != null)
                    _isSci1 = true;

                // Check for the SCI0 Mac patch
                if (resource == null)
                    resource = resMan.FindResource(new ResourceId(ResourceType.Patch, 200), false);

                if (resource == null)
                {
                    Warning("Could not open patch for Amiga sound driver");
                    return MidiDriverError.UnknownError;
                }

                var stream = new MemoryStream(resource.data, 0, resource.size);

                if (_isSci1)
                {
                    if (!LoadInstrumentsSCI1(stream))
                        return MidiDriverError.UnknownError;
                }
                else if (!LoadInstrumentsSCI0Mac(stream))
                    return MidiDriverError.UnknownError;
            }

            base.Open();

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false);

            return MidiDriverError.None;
        }

        private void Close()
        {
            _mixer.StopHandle(_mixerSoundHandle);

            for (var i = 0; i < _bank.size; i++)
            {
                for (var j = 0; j < _bank.instruments[i].Count; j++)
                {
                    if (_bank.instruments[i][j] != null)
                    {
                        _bank.instruments[i][j].loop = null;
                        _bank.instruments[i][j].samples = null;
                    }
                }
            }
        }

        public void PlaySwitch(bool play)
        {
            _playSwitch = play;
        }

        public void SetVolume(byte volume_)
        {
            _masterVolume = volume_;
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
                    StopNote(channel, op1);
                    break;
                case 0x90:
                    if (op2 > 0)
                        StartNote(channel, op1, op2);
                    else
                        StopNote(channel, op1);
                    break;
                case 0xb0:
                    switch (op1)
                    {
                        case 0x07:
                            _channels[channel].volume = op2;
                            break;
                        case 0x0a: // pan
                            DebugC(1, DebugLevels.Sound, "Amiga/Mac driver: ignoring pan 0x{0:X2} event for channel {1}",
                                op2, channel);
                            break;
                        case 0x40: // hold
                            DebugC(1, DebugLevels.Sound, "Amiga/Mac driver: ignoring hold 0x{0:X2} event for channel {1}",
                                op2, channel);
                            break;
                        case 0x4b: // voice mapping
                            break;
                        case 0x4e: // velocity
                            break;
                        case 0x7b:
                            StopChannel(channel);
                            break;
                        default:
                            //warning("Amiga/Mac driver: unknown control event 0x%02x", op1);
                            break;
                    }
                    break;
                case 0xc0:
                    ChangeInstrument(channel, op1);
                    break;
                // The original MIDI driver from sierra ignores aftertouch completely, so should we
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                    break;
                case 0xe0:
                    PitchWheel(channel, (ushort)((op2 << 7) | op1));
                    break;
                default:
                    Warning("Amiga/Mac driver: unknown event {0:X2}", command);
                    break;
            }
        }

        protected override void GenerateSamples(short[] data, int pos, int len)
        {
            if (len == 0)
                return;

            short[] buffers = new short[len * 2 * Channels];

            /* Generate samples for all notes */
            for (int i = 0; i < Channels; i++)
                if (_voices[i].note >= 0)
                    PlayInstrument(new Ptr<short>(buffers, i * len*2), _voices[i], len);

            if (IsStereo)
            {
                for (int j = 0; j < len; j++)
                {
                    int mixedl = 0, mixedr = 0;

                    /* Mix and pan */
                    for (int i = 0; i < Channels; i++)
                    {
                        mixedl += buffers[i * len + j] * (256 - _channels[_voices[i].hw_channel].pan);
                        mixedr += buffers[i * len + j] * _channels[_voices[i].hw_channel].pan;
                    }

                    /* Adjust volume */
                    data[pos + 2 * j] = (short)(mixedl * _masterVolume >> 13);
                    data[pos + 2 * j + 1] = (short)(mixedr * _masterVolume >> 13);
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    int mixed = 0;

                    /* Mix */
                    for (int i = 0; i < Channels; i++)
                        mixed += buffers[i * len + j];

                    /* Adjust volume */
                    data[pos + j] = (short)(mixed * _masterVolume >> 6);
                }
            }
        }

        private bool LoadInstrumentsSCI0(Stream file)
        {
            _isSci1 = false;

            byte[] header = new byte[40];

            if (file.Read(header, 0, 40) < 40)
            {
                Warning("Amiga/Mac driver: failed to read header of file bank.001");
                return false;
            }

            _bank.size = header.ToUInt16BigEndian(38);
            _bank.name = header.GetRawText(8, 29);
            DebugC(DebugLevels.Sound, "Amiga/Mac driver: Reading %i instruments from bank \"%s\"", _bank.size,
                _bank.name);

            for (var i = 0; i < _bank.size; i++)
            {
                int id;
                InstrumentSample instrument = ReadInstrumentSCI0(file, out id);

                if (instrument == null)
                {
                    Warning("Amiga/Mac driver: failed to read bank.001");
                    return false;
                }

                if (id < 0 || id > 255)
                {
                    Warning("Amiga/Mac driver: Error: instrument ID out of bounds");
                    return false;
                }

                if ((uint)id >= _bank.instruments.Length)
                    Array.Resize(ref _bank.instruments, id + 1);

                _bank.instruments[id].Add(instrument);
                _bank.instruments[id].name = instrument.name;
            }

            return true;
        }

        private bool LoadInstrumentsSCI0Mac(Stream file)
        {
            byte[] header = new byte[40];
            var br = new BinaryReader(file);

            if (file.Read(header, 0, 40) < 40)
            {
                Warning("Amiga/Mac driver: failed to read header of file patch.200");
                return false;
            }

            _bank.size = 128;
            _bank.name = header.GetRawText(8, 29);
            DebugC(DebugLevels.Sound, "Amiga/Mac driver: Reading %i instruments from bank \"%s\"", _bank.size, _bank.name);

            uint[] instrumentOffsets = new uint[_bank.size];
            Array.Resize(ref _bank.instruments, _bank.size);

            for (var i = 0; i < _bank.size; i++)
                instrumentOffsets[i] = br.ReadUInt32BigEndian();

            for (uint i = 0; i < _bank.size; i++)
            {
                // 0 signifies it doesn't exist
                if (instrumentOffsets[i] == 0)
                    continue;

                file.Seek(instrumentOffsets[i], SeekOrigin.Begin);

                ushort id = br.ReadUInt16BigEndian();
                if (id != i)
                    Error("Instrument number mismatch");

                InstrumentSample instrument = new InstrumentSample();

                instrument.startNote = 0;
                instrument.endNote = 127;
                instrument.isUnsigned = true;
                instrument.baseFreq = BaseFreq;
                instrument.baseNote = 101;
                instrument.fixedNote = 101;
                instrument.mode = br.ReadUInt16BigEndian();

                // Read in the offsets
                int[] seg_size ={
                     br.ReadInt32BigEndian(),
                     br.ReadInt32BigEndian(),
                     br.ReadInt32BigEndian()
                };
                instrument.transpose = br.ReadUInt16BigEndian();

                for (byte j = 0; j < 4; j++)
                {
                    int length = (sbyte)br.ReadByte();

                    if (length == 0 && j > 0)
                        length = 256;

                    instrument.envelope[j].length = length * _frequency / 60;
                    instrument.envelope[j].delta = (sbyte)br.ReadByte();
                    instrument.envelope[j].target = br.ReadByte();
                }

                // Final target must be 0
                instrument.envelope[3].target = 0;

                instrument.name = br.ReadBytes(30).GetRawText();

                if ((instrument.mode & kModePitch) != 0)
                    instrument.fixedNote = -1;

                int size = seg_size[2];
                int loop_offset = seg_size[0];

                instrument.samples = new sbyte[size + 1];
                Array.Copy(br.ReadSBytes(size), instrument.samples, size);

                if ((instrument.mode & kModeLoop) != 0)
                {
                    instrument.size = seg_size[0];
                    instrument.loop_size = seg_size[1] - seg_size[0];

                    instrument.loop = new sbyte[instrument.loop_size + 1];
                    Array.Copy(instrument.samples, loop_offset, instrument.loop, 0, instrument.loop_size);

                    instrument.samples[instrument.size] = instrument.loop[0];
                    instrument.loop[instrument.loop_size] = instrument.loop[0];
                }
                else
                {
                    instrument.loop = null;
                    instrument.loop_size = 0;
                    instrument.size = size;
                    instrument.samples[instrument.size] = (sbyte)-1;
                }

                _bank.instruments[id].Add(instrument);
                _bank.instruments[id].name = instrument.name;
            }

            return true;
        }

        private bool LoadInstrumentsSCI1(Stream file)
        {
            var br = new BinaryReader(file);
            _bank.size = 128;

            if (_isSci1Early)
                br.ReadUInt32BigEndian(); // Skip size of bank

            List<uint> instrumentOffsets = new List<uint>((int)_bank.size);
            Array.Resize(ref _bank.instruments, _bank.size);

            for (var i = 0; i < _bank.size; i++)
                instrumentOffsets[i] = br.ReadUInt32BigEndian();

            for (var i = 0; i < _bank.size; i++)
            {
                // 0 signifies it doesn't exist
                if (instrumentOffsets[i] == 0)
                    continue;

                file.Seek(instrumentOffsets[i] + (_isSci1Early ? 4 : 0), SeekOrigin.Begin);

                // Read in the instrument name
                _bank.instruments[i].name = br.ReadBytes(10).GetRawText(); // last two bytes are always 0

                for (var j = 0; ; j++)
                {
                    InstrumentSample sample = new InstrumentSample();

                    sample.startNote = br.ReadInt16BigEndian();

                    // startNote being -1 signifies we're done with this instrument
                    if (sample.startNote == -1)
                    {
                        break;
                    }

                    sample.endNote = br.ReadInt16BigEndian();
                    uint samplePtr = br.ReadUInt32BigEndian();
                    sample.transpose = br.ReadInt16BigEndian();
                    for (int env = 0; env < 3; env++)
                    {
                        sample.envelope[env].length = br.ReadByte() * _frequency / 60;
                        sample.envelope[env].delta = (env == 0 ? 10 : -10);
                        sample.envelope[env].target = br.ReadByte();
                    }

                    sample.envelope[3].length = 0;
                    sample.fixedNote = br.ReadInt16BigEndian();
                    short loop = br.ReadInt16BigEndian();
                    var nextSamplePos = file.Position;

                    file.Seek(samplePtr + (_isSci1Early ? 4 : 0), SeekOrigin.Begin);
                    sample.name = br.ReadBytes(8).GetRawText();

                    ushort phase1Offset, phase1End;
                    ushort phase2Offset, phase2End;

                    if (_isSci1Early)
                    {
                        sample.isUnsigned = false;
                        br.ReadUInt32BigEndian(); // skip total sample size
                        phase2Offset = br.ReadUInt16BigEndian();
                        phase2End = br.ReadUInt16BigEndian();
                        sample.baseNote = br.ReadUInt16BigEndian();
                        phase1Offset = br.ReadUInt16BigEndian();
                        phase1End = br.ReadUInt16BigEndian();
                    }
                    else
                    {
                        sample.isUnsigned = br.ReadUInt16BigEndian() == 0;
                        phase1Offset = br.ReadUInt16BigEndian();
                        phase1End = br.ReadUInt16BigEndian();
                        phase2Offset = br.ReadUInt16BigEndian();
                        phase2End = br.ReadUInt16BigEndian();
                        sample.baseNote = br.ReadUInt16BigEndian();
                    }

                    uint periodTableOffset = _isSci1Early ? 0 : br.ReadUInt32BigEndian();
                    var sampleDataPos = file.Position;

                    sample.size = phase1End - phase1Offset + 1;
                    sample.loop_size = phase2End - phase2Offset + 1;

                    sample.samples = new sbyte[sample.size + 1];
                    file.Seek(phase1Offset + sampleDataPos, SeekOrigin.Begin);
                    Array.Copy(br.ReadSBytes(sample.size), sample.samples, sample.size);
                    sample.samples[sample.size] = (sbyte)(sample.isUnsigned ? -1 : 0);

                    if (loop == 0 && sample.loop_size > 1)
                    {
                        sample.loop = new sbyte[sample.loop_size + 1];
                        file.Seek(phase2Offset + sampleDataPos, SeekOrigin.Begin);
                        Array.Copy(br.ReadSBytes(sample.loop_size), sample.loop, sample.loop_size);
                        sample.mode |= kModeLoop;
                        sample.samples[sample.size] = sample.loop[0];
                        sample.loop[sample.loop_size] = sample.loop[0];
                    }

                    _bank.instruments[i].Add(sample);

                    if (_isSci1Early)
                    {
                        // There's no frequency specified by the sample and is hardcoded like in SCI0
                        sample.baseFreq = 11000;
                    }
                    else
                    {
                        file.Seek(periodTableOffset + 0xe0, SeekOrigin.Begin);
                        sample.baseFreq = br.ReadUInt16BigEndian();
                    }

                    file.Seek(nextSamplePos, SeekOrigin.Begin);
                }
            }

            return true;
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }
    }

    class MidiPlayer_AmigaMac : MidiPlayer
    {
        public MidiPlayer_AmigaMac(SciVersion version) : base(version)
        {
            _driver = new MidiDriver_AmigaMac(SciEngine.Instance.Mixer);
        }

        public override int Polyphony
        {
            get
            {
                return MidiDriver_AmigaMac.Voices;
            }
        }

        public override byte PlayId
        {
            get
            {
                if (_version > SciVersion.V0_LATE)
                    return 0x06;

                return 0x40;
            }
        }

        public override bool HasRhythmChannel
        {
            get
            {
                return false;
            }
        }

        //byte getPlayId() const;
        public void SetVolume(byte volume) { ((MidiDriver_AmigaMac)_driver).SetVolume(volume); }
        public void playSwitch(bool play)
        {
            ((MidiDriver_AmigaMac)_driver).PlaySwitch(play);
        }
    }
}