//
//  MidiDriverAccoladeMt32.cs
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

namespace NScumm.Agos
{
    internal static class Accolade
    {
        public const int AGOS_MIDI_CHANNEL_COUNT = 16;
        public const int AGOS_MIDI_INSTRUMENT_COUNT = 128;
        public const int AGOS_MIDI_KEYNOTE_COUNT = 64;
    }

    internal class MidiDriverAccoladeMt32 : MidiDriver
    {
        protected object _mutex = new object();
        protected MidiDriver _driver;
        protected bool _nativeMT32; // native MT32, may also be our MUNT, or MUNT over MIDI

        protected bool _isOpen;
        protected int _baseFreq;

        // simple mapping between MIDI channel and MT32 channel
        private readonly byte[] _channelMapping = new byte[Accolade.AGOS_MIDI_CHANNEL_COUNT];
        // simple mapping between MIDI instruments and MT32 instruments
        private readonly byte[] _instrumentMapping = new byte[Accolade.AGOS_MIDI_INSTRUMENT_COUNT];

        public override uint BaseTempo
        {
            get
            {
                if (_driver != null)
                {
                    return _driver.BaseTempo;
                }
                return (uint) (1000000 / _baseFreq);
            }
        }

        public MidiDriverAccoladeMt32()
        {
            _baseFreq = 250;
        }

        public override void Dispose()
        {
            base.Dispose();

            _driver?.Dispose();
        }

        // MIDI messages can be found at http://www.midi.org/techspecs/midimessages.php
        public override void Send(int b)
        {
            byte command = (byte) (b & 0xf0);
            byte channel = (byte) (b & 0xf);

            if (command == 0xF0)
            {
                _driver?.Send(b);
                return;
            }

            byte mappedChannel = _channelMapping[channel];

            if (mappedChannel < Accolade.AGOS_MIDI_CHANNEL_COUNT)
            {
                // channel mapped to an actual MIDI channel, so use that one
                b = (int) ((b & 0xFFFFFFF0) | mappedChannel);
                if (command == 0xC0)
                {
                    // Program change
                    // Figure out the requested instrument
                    byte midiInstrument = (byte) ((b >> 8) & 0xFF);
                    byte mappedInstrument = _instrumentMapping[midiInstrument];

                    // If there is no actual MT32 (or MUNT), we make a second mapping to General MIDI instruments
                    if (!_nativeMT32)
                    {
                        mappedInstrument = (Mt32ToGm[mappedInstrument]);
                    }
                    // And replace it
                    b = (int) ((b & 0xFFFF00FF) | (mappedInstrument << 8));
                }

                _driver?.Send(b);
            }
        }

        public override MidiDriverError Open()
        {
            System.Diagnostics.Debug.Assert(_driver == null);

            //	debugC(kDebugLevelMT32Driver, "MT32: starting driver");

            // Setup midi driver
            var dev = MidiDriver.DetectDevice(MusicDriverTypes.Midi | MusicDriverTypes.PreferMt32);
            MusicType musicType = GetMusicType(dev);

            // check, if we got a real MT32 (or MUNT, or MUNT over MIDI)
            switch (musicType)
            {
                case MusicType.MT32:
                    _nativeMT32 = true;
                    break;
                case MusicType.GeneralMidi:
                    if (ConfigManager.Instance.Get<bool>("native_mt32"))
                    {
                        _nativeMT32 = true;
                    }
                    break;
            }

            _driver = (MidiDriver) CreateMidi(Engine.Instance.Mixer, dev);
            if (_driver == null)
                return (MidiDriverError) 255;

            MidiDriverError ret = _driver.Open();
            if (ret != MidiDriverError.None)
                return ret;

            if (_nativeMT32)
                _driver.SendMt32Reset();
            else
                _driver.SendGmReset();

            return 0;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _driver?.SetTimerCallback(timerParam, timerProc);
        }

        public override MidiChannel AllocateChannel()
        {
            return _driver?.AllocateChannel();
        }

        public override MidiChannel GetPercussionChannel()
        {
            return _driver?.GetPercussionChannel();
        }

        // Called right at the start, we get an INSTR.DAT entry
        public bool SetupInstruments(BytePtr driverData, ushort driverDataSize, bool useMusicDrvFile)
        {
            ushort channelMappingOffset = 0;
            ushort channelMappingSize = 0;
            ushort instrumentMappingOffset = 0;
            ushort instrumentMappingSize = 0;

            if (!useMusicDrvFile)
            {
                // INSTR.DAT: we expect at least 354 bytes
                if (driverDataSize < 354)
                    return false;

                // Data is like this:
                // 128 bytes  instrument mapping
                // 128 bytes  instrument volume adjust (signed!) (not used for MT32)
                //  16 bytes  unknown
                //  16 bytes  channel mapping
                //  64 bytes  key note mapping (not really used for MT32)
                //   1 byte   instrument count
                //   1 byte   bytes per instrument
                //   x bytes  no instruments used for MT32

                channelMappingOffset = 256 + 16;
                channelMappingSize = 16;
                instrumentMappingOffset = 0;
                instrumentMappingSize = 128;
            }
            else
            {
                // MUSIC.DRV: we expect at least 468 bytes
                if (driverDataSize < 468)
                    return false;

                channelMappingOffset = 396;
                channelMappingSize = 16;
                instrumentMappingOffset = 140;
                instrumentMappingSize = 128;
            }

            // Channel mapping
            if (channelMappingSize != 0)
            {
                // Get these 16 bytes for MIDI channel mapping
                if (channelMappingSize != _channelMapping.Length)
                    return false;

                driverData.Copy(channelMappingOffset, _channelMapping, 0, _channelMapping.Length);
            }
            else
            {
                // Set up straight mapping
                for (ushort channelNr = 0; channelNr < _channelMapping.Length; channelNr++)
                {
                    _channelMapping[channelNr] = (byte) channelNr;
                }
            }

            if (instrumentMappingSize != 0)
            {
                // And these for instrument mapping
                if (instrumentMappingSize > _instrumentMapping.Length)
                    return false;

                driverData.Copy(instrumentMappingOffset, _instrumentMapping, 0, instrumentMappingSize);
            }
            // Set up straight mapping for the remaining data
            for (ushort instrumentNr = instrumentMappingSize; instrumentNr < _instrumentMapping.Length; instrumentNr++)
            {
                _instrumentMapping[instrumentNr] = (byte) instrumentNr;
            }
            return true;
        }
    }
}