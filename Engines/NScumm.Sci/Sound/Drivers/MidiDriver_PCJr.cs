//
//  MidiDriver_PCJr.cs
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
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiDriver_PCJr : EmulatedMidiDriver
    {
        private const int MaxChannels = 3;
        private const int VOLUME_SHIFT = 3;

        private const int BASE_NOTE = 129;  // A10
        private const int BASE_OCTAVE = 10; // A10, as I said


        private int _channels_nr;
        internal int _global_volume; // Base volume
        private int[] _volumes = new int[MaxChannels];
        private int[] _notes = new int[MaxChannels]; // Current halftone, or 0 if off
        private int[] _freq_count = new int[MaxChannels];
        private int _channel_assigner;
        private int _channels_assigned;
        private int[] _chan_nrs = new int[MaxChannels];

        public override bool IsStereo
        {
            get
            {
                return false;
            }
        }

        public override int Rate
        {
            get
            {
                return _mixer.OutputRate;
            }
        }

        public MidiDriver_PCJr(IMixer mixer)
            : base(mixer)
        {
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        public override MidiDriverError Open()
        {
            return Open(MaxChannels);
        }

        public MidiDriverError Open(int channels)
        {
            if (_isOpen)
                return MidiDriverError.AlreadyOpen;

            if (channels > MaxChannels)
                return (MidiDriverError)(-1);

            _channels_nr = channels;
            _global_volume = 100;
            for (int i = 0; i < _channels_nr; i++)
            {
                _volumes[i] = 100;
                _notes[i] = 0;
                _freq_count[i] = 0;
                _chan_nrs[i] = -1;
            }
            _channel_assigner = 0;
            _channels_assigned = 0;

            base.Open();

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false);

            return 0;
        }

        public override void Dispose()
        {
            _mixer.StopHandle(_mixerSoundHandle);
            base.Dispose();
        }

        public override void Send(int b)
        {
            byte command = (byte)(b & 0xff);
            byte op1 = (byte)((b >> 8) & 0xff);
            byte op2 = (byte)((b >> 16) & 0xff);
            int i;
            int mapped_chan = -1;
            int chan_nr = command & 0xf;

            // First, test for channel having been assigned already
            if ((_channels_assigned & (1 << chan_nr)) != 0)
            {
                // Already assigned this channel number:
                for (i = 0; i < _channels_nr; i++)
                    if (_chan_nrs[i] == chan_nr)
                    {
                        mapped_chan = i;
                        break;
                    }
            }
            else if ((command & 0xe0) == 0x80)
            {
                // Assign new channel round-robin

                // Mark channel as unused:
                if (_chan_nrs[_channel_assigner] >= 0)
                    _channels_assigned &= ~(1 << _chan_nrs[_channel_assigner]);

                // Remember channel:
                _chan_nrs[_channel_assigner] = chan_nr;
                // Mark channel as used
                _channels_assigned |= (1 << _chan_nrs[_channel_assigner]);

                // Save channel for use later in this call:
                mapped_chan = _channel_assigner;
                // Round-ropin iterate channel assigner:
                _channel_assigner = (_channel_assigner + 1) % _channels_nr;
            }

            if (mapped_chan == -1)
                return;

            switch (command & 0xf0)
            {
                case 0x80:
                    if (op1 == _notes[mapped_chan])
                        _notes[mapped_chan] = 0;
                    break;

                case 0x90:
                    if (op2 == 0)
                    {
                        if (op1 == _notes[mapped_chan])
                            _notes[mapped_chan] = 0;
                    }
                    else {
                        _notes[mapped_chan] = op1;
                        _volumes[mapped_chan] = op2;
                    }
                    break;

                case 0xb0:
                    if ((op1 == MidiPlayer.SCI_MIDI_CHANNEL_NOTES_OFF) || (op1 == MidiPlayer.SCI_MIDI_CHANNEL_SOUND_OFF))
                        _notes[mapped_chan] = 0;
                    break;

                default:
                    Debug(2, $"Unused MIDI command {command:X2} {op1:X2} {op2:X2}");
                    break; /* ignore */
            }
        }

        protected override void GenerateSamples(short[] buf, int pos, int len)
        {
            int i;
            int chan;
            int[] freq = new int[MaxChannels];
            int frequency = Rate;

            for (chan = 0; chan < _channels_nr; chan++)
                freq[chan] = get_freq(_notes[chan]);

            for (i = 0; i < len; i++)
            {
                short result = 0;

                for (chan = 0; chan < _channels_nr; chan++)
                {
                    if (_notes[chan] != 0)
                    {
                        int volume = (_global_volume * _volumes[chan])
                                     >> VOLUME_SHIFT;

                        _freq_count[chan] += freq[chan];
                        while (_freq_count[chan] >= (frequency << 1))
                            _freq_count[chan] -= (frequency << 1);

                        if (_freq_count[chan] - freq[chan] < 0)
                        {
                            /* Unclean rising edge */
                            int l = volume << 1;
                            result = (short)(result - volume + (l * _freq_count[chan]) / freq[chan]);
                        }
                        else if (_freq_count[chan] >= frequency
                                 && _freq_count[chan] - freq[chan] < frequency)
                        {
                            /* Unclean falling edge */
                            int l = volume << 1;
                            result = (short)(result + volume - (l * (_freq_count[chan] - frequency)) / freq[chan]);
                        }
                        else {
                            if (_freq_count[chan] < frequency)
                                result = (short)(result + volume);
                            else
                                result = (short)(result - volume);
                        }
                    }
                }
                buf[pos + i] = result;
            }
        }

        static readonly int[] freq_table = { // A4 is 440Hz, halftone map is x |-> ** 2^(x/12)
            28160, // A10
            29834,
            31608,
            33488,
            35479,
            37589,
            39824,
            42192,
            44701,
            47359,
            50175,
            53159
        };

        private static int get_freq(int note)
        {
            int halftone_delta = note - BASE_NOTE;
            int oct_diff = ((halftone_delta + BASE_OCTAVE * 12) / 12) - BASE_OCTAVE;
            int halftone_index = (halftone_delta + (12 * 100)) % 12;
            int freq = (note == 0) ? 0 : freq_table[halftone_index] / (1 << (-oct_diff));

            return freq;
        }
    }

    internal class MidiPlayer_PCJr : MidiPlayer
    {
        public override int Polyphony
        {
            get
            {
                return 3;
            }
        }

        public override bool HasRhythmChannel
        {
            get
            {
                return false;
            }
        }

        public MidiPlayer_PCJr(SciVersion version)
            : base(version)
        {
            _driver = new MidiDriver_PCJr(SciEngine.Instance.Mixer);
        }

        public override MidiDriverError Open(ResourceManager resMan)
        {
            return ((MidiDriver_PCJr)_driver).Open(Polyphony);
        }

        public override byte Volume
        {
            get
            {
                return (byte)((MidiDriver_PCJr)_driver)._global_volume;
            }
            set
            {
                ((MidiDriver_PCJr)_driver)._global_volume = value;
            }
        }

        public override byte PlayId
        {
            get
            {
                switch (_version)
                {
                    case SciVersion.V0_EARLY:
                        return 0x02;
                    case SciVersion.V0_LATE:
                        return 0x10;
                    default:
                        return 0x13;
                }
            }
        }
    }

    internal class MidiPlayer_PCSpeaker : MidiPlayer_PCJr
    {
        public override int Polyphony
        {
            get { return 1; }
        }

        public override byte PlayId
        {
            get
            {
                switch (_version)
                {
                    case SciVersion.V0_EARLY:
                        return 0x04;
                    case SciVersion.V0_LATE:
                        return 0x20;
                    default:
                        return 0x12;
                }
            }
        }

        public MidiPlayer_PCSpeaker(SciVersion version)
            : base(version)
        {
        }
    }
}
