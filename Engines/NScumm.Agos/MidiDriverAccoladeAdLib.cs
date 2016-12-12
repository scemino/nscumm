//
//  MidiDriverAccoladeAdLib.cs
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

using System;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal class InstrumentEntry
    {
        public const int Size = 9;

        public byte reg20op1;
        // Amplitude Modulation / Vibrato / Envelope Generator Type / Keyboard Scaling Rate / Modulator Frequency Multiple

        public byte reg40op1; // Level Key Scaling / Total Level
        public byte reg60op1; // Attack Rate / Decay Rate
        public byte reg80op1; // Sustain Level / Release Rate

        public byte reg20op2;
        // Amplitude Modulation / Vibrato / Envelope Generator Type / Keyboard Scaling Rate / Modulator Frequency Multiple

        public byte reg40op2; // Level Key Scaling / Total Level
        public byte reg60op2; // Attack Rate / Decay Rate
        public byte reg80op2; // Sustain Level / Release Rate
        public byte regC0; // Feedback / Algorithm, bit 0 - set . both operators in use

        public InstrumentEntry(BytePtr data)
        {
            reg20op1 = data[0];

            reg40op1 = data[1];
            reg60op1 = data[2];
            reg80op1 = data[3];

            reg20op2 = data[4];

            reg40op2 = data[5];
            reg60op2 = data[6];
            reg80op2 = data[7];
            regC0 = data[8];
        }
    }

    struct ChannelEntry
    {
        public byte currentNote;
        public byte currentA0hReg;
        public byte currentB0hReg;
        public short volumeAdjust;
        public Ptr<InstrumentEntry> currentInstrumentPtr;
    }

    /// <summary>
    /// Accolade adlib music driver.
    /// </summary>
    /// <remarks>
    /// There are at least 2 variants of this sound system.
    /// One for the games Elvira 1 + Elvira 2
    /// It seems it was also used for the game "Altered Destiny"
    /// Another one for the games Waxworks + Simon, the Sorcerer 1 Demo
    ///
    /// First one uses the file INSTR.DAT for instrument data, channel mapping etc.
    /// Second one uses the file MUSIC.DRV, which actually contains driver code + instrument data + channel mapping, etc.
    ///
    /// The second variant supported dynamic channel allocation for the FM voice channels, but this
    /// feature was at least definitely disabled for Simon, the Sorcerer 1 demo and for the Waxworks demo too.
    ///
    /// I have currently not implemented dynamic channel allocation.
    /// </remarks>
    internal class MidiDriverAccoladeAdLib : MidiDriver
    {
        // 5 instruments on top of the regular MIDI ones
        // used by the MUSIC.DRV variant for percussion instruments
        private const int AGOS_ADLIB_EXTRA_INSTRUMENT_COUNT = 5;

        private const int AGOS_ADLIB_VOICES_COUNT = 11;
        private const int AGOS_ADLIB_VOICES_MELODIC_COUNT = 6;
        private const int AGOS_ADLIB_VOICES_PERCUSSION_START = 6;
        private const int AGOS_ADLIB_VOICES_PERCUSSION_COUNT = 5;
        private const int AGOS_ADLIB_VOICES_PERCUSSION_CYMBAL = 9;

        private bool _musicDrvMode;

        // from INSTR.DAT/MUSIC.DRV - simple mapping between MIDI channel and MT32 channel
        private byte[] _channelMapping = new byte[Accolade.AGOS_MIDI_CHANNEL_COUNT];
        // from INSTR.DAT/MUSIC.DRV - simple mapping between MIDI instruments and MT32 instruments
        private byte[] _instrumentMapping = new byte[Accolade.AGOS_MIDI_INSTRUMENT_COUNT];
        // from INSTR.DAT/MUSIC.DRV - volume adjustment per instrument
        private sbyte[] _instrumentVolumeAdjust = new sbyte[Accolade.AGOS_MIDI_INSTRUMENT_COUNT];
        // simple mapping between MIDI key notes and MT32 key notes
        private byte[] _percussionKeyNoteMapping = new byte[Accolade.AGOS_MIDI_KEYNOTE_COUNT];

        // from INSTR.DAT/MUSIC.DRV - adlib instrument data
        private InstrumentEntry[] _instrumentTable;
        private byte _instrumentCount;

        private byte _percussionReg;

        private IOpl _opl;
        private int _masterVolume;

        private TimerProc _adlibTimerProc;
        private object _adlibTimerParam;

        private bool _isOpen;

        // stores information about all FM voice channels
        private ChannelEntry[] _channels = new ChannelEntry[AGOS_ADLIB_VOICES_COUNT];

        private static readonly byte[] operator1Register =
        {
            0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x14, 0x12, 0x15, 0x11
        };

        private static readonly byte[] operator2Register =
        {
            0x03, 0x04, 0x05, 0x0B, 0x0C, 0x0D, 0x13, 0xFF, 0xFF, 0xFF, 0xFF
        };

        // percussion:
        //  voice  6 - base drum - also uses operator 13h
        //  voice  7 - snare drum
        //  voice  8 - tom tom
        //  voice  9 - cymbal
        //  voice 10 - hi hat
        private static readonly byte[] percussionBits =
        {
            0x10, 0x08, 0x04, 0x02, 0x01
        };

        // hardcoded, dumped from Accolade music system
        // same for INSTR.DAT + MUSIC.DRV, except that MUSIC.DRV does the lookup differently
        private static readonly byte[] percussionKeyNoteChannelTable =
        {
            0x06, 0x07, 0x07, 0x07, 0x07, 0x08, 0x0A, 0x08, 0x0A, 0x08,
            0x0A, 0x08, 0x08, 0x09, 0x08, 0x09, 0x0F, 0x0F, 0x0A, 0x0F,
            0x0A, 0x0F, 0x0F, 0x0F, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x0A, 0x0F, 0x0F, 0x08, 0x0F, 0x08
        };

        // hardcoded, dumped from Accolade music system (INSTR.DAT variant)
        private static readonly ushort[] frequencyLookUpTable =
        {
            0x02B2, 0x02DB, 0x0306, 0x0334, 0x0365, 0x0399, 0x03CF,
            0xFE05, 0xFE23, 0xFE44, 0xFE67, 0xFE8B
        };

        // hardcoded, dumped from Accolade music system (MUSIC.DRV variant)
        private static readonly ushort[] frequencyLookUpTableMusicDrv =
        {
            0x0205, 0x0223, 0x0244, 0x0267, 0x028B, 0x02B2, 0x02DB,
            0x0306, 0x0334, 0x0365, 0x0399, 0x03CF
        };

        public MidiDriverAccoladeAdLib()
        {
            _masterVolume = 15;
            _percussionReg = 0x20;
        }

        /// <summary>
        /// MIDI messages can be found at http://www.midi.org/techspecs/midimessages.php
        /// </summary>
        /// <param name="b"></param>
        public override void Send(int b)
        {
            byte command = (byte) (b & 0xf0);
            byte channel = (byte) (b & 0xf);
            byte op1 = (byte) ((b >> 8) & 0xff);
            byte op2 = (byte) ((b >> 16) & 0xff);

            byte mappedChannel = _channelMapping[channel];

            // Ignore everything that is outside of our channel range
            if (mappedChannel >= AGOS_ADLIB_VOICES_COUNT)
                return;

            switch (command)
            {
                case 0x80:
                    NoteOff(mappedChannel, op1, false);
                    break;
                case 0x90:
                    // Convert noteOn with velocity 0 to a noteOff
                    if (op2 == 0)
                    {
                        NoteOff(mappedChannel, op1, false);
                        return;
                    }

                    NoteOn(mappedChannel, op1, op2);
                    break;
                case 0xb0: // Control change
                    // Doesn't seem to be implemented
                    break;
                case 0xc0: // Program Change
                    var mappedInstrument = _instrumentMapping[op1];
                    ProgramChange(mappedChannel, mappedInstrument, op1);
                    break;
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                    // Aftertouch doesn't seem to be implemented
                    break;
                case 0xe0:
                    // No pitch bend change
                    break;
                case 0xf0: // SysEx
                    Warning("ADLIB: SysEx: {0:X}", b);
                    break;
                default:
                    Warning("ADLIB: Unknown event {0:X2}", command);
                    break;
            }
        }

        public override MidiDriverError Open()
        {
            //	debugC(kDebugLevelAdLibDriver, "AdLib: starting driver");

            _opl = new DosBoxOPL(OplType.Opl2);

            if (_opl == null)
                return MidiDriverError.UnknownError;

            _opl.Init();

            _isOpen = true;

            _opl.Start(OnTimer);

            ResetAdLib();

            // Finally set up default instruments
            for (byte FMvoiceNr = 0; FMvoiceNr < AGOS_ADLIB_VOICES_COUNT; FMvoiceNr++)
            {
                if (FMvoiceNr < AGOS_ADLIB_VOICES_PERCUSSION_START)
                {
                    // Regular FM voices with instrument 0
                    ProgramChangeSetInstrument(FMvoiceNr, 0, 0);
                }
                else
                {
                    byte percussionInstrument;
                    if (!_musicDrvMode)
                    {
                        // INSTR.DAT: percussion voices with instrument 1, 2, 3, 4 and 5
                        percussionInstrument = (byte) (FMvoiceNr - AGOS_ADLIB_VOICES_PERCUSSION_START + 1);
                    }
                    else
                    {
                        // MUSIC.DRV: percussion voices with instrument 0x80, 0x81, 0x82, 0x83 and 0x84
                        percussionInstrument = (byte) (FMvoiceNr - AGOS_ADLIB_VOICES_PERCUSSION_START + 0x80);
                    }
                    ProgramChangeSetInstrument(FMvoiceNr, percussionInstrument, percussionInstrument);
                }
            }

            // driver initialization does this here:
            // INSTR.DAT
            // noteOn(9, 0x29, 0);
            // noteOff(9, 0x26, false);
            // MUSIC.DRV
            // noteOn(9, 0x26, 0);
            // noteOff(9, 0x26, false);

            return 0;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _adlibTimerProc = timerProc;
            _adlibTimerParam = timerParam;
        }

        public override uint BaseTempo => 1000000 / Opl.DefaultCallbackFrequency;

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        public void SetVolume(byte volume)
        {
            _masterVolume = volume;
            //renewNotes(-1, true);
        }

        void ProgramChange(byte FMvoiceChannel, byte mappedInstrumentNr, byte MIDIinstrumentNr)
        {
            if (mappedInstrumentNr >= _instrumentCount)
            {
                Warning("ADLIB: tried to set non-existent instrument");
                return; // out of range
            }

            // setup instrument
            //warning("ADLIB: program change for FM voice channel {0}, instrument id {1}", FMvoiceChannel, mappedInstrumentNr);

            if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_START)
            {
                // Regular FM voice
                ProgramChangeSetInstrument(FMvoiceChannel, mappedInstrumentNr, MIDIinstrumentNr);
            }
            else
            {
                // Percussion
                // set default instrument (again)
                byte percussionInstrumentNr = 0;

                if (!_musicDrvMode)
                {
                    // INSTR.DAT: percussion default instruments start at instrument 1
                    percussionInstrumentNr = (byte) (FMvoiceChannel - AGOS_ADLIB_VOICES_PERCUSSION_START + 1);
                }
                else
                {
                    // MUSIC.DRV: percussion default instruments start at instrument 0x80
                    percussionInstrumentNr = (byte) (FMvoiceChannel - AGOS_ADLIB_VOICES_PERCUSSION_START + 0x80);
                }
                if (percussionInstrumentNr >= _instrumentCount)
                {
                    Warning("ADLIB: tried to set non-existent instrument");
                    return;
                }
                var instrumentPtr = new Ptr<InstrumentEntry>(_instrumentTable, percussionInstrumentNr);
                _channels[FMvoiceChannel].currentInstrumentPtr = instrumentPtr;
                _channels[FMvoiceChannel].volumeAdjust = _instrumentVolumeAdjust[percussionInstrumentNr];
            }
        }


        private void NoteOn(byte FMvoiceChannel, byte note, byte velocity)
        {
            byte adjustedNote = note;
            byte adjustedVelocity = velocity;

            // adjust velocity
            short channelVolumeAdjust = _channels[FMvoiceChannel].volumeAdjust;
            channelVolumeAdjust += adjustedVelocity;
            channelVolumeAdjust = (short) ScummHelper.Clip(channelVolumeAdjust, 0, 0x7F);

            // TODO: adjust to global volume
            // original drivers had a global volume variable, which was 0 for full volume, -64 for half volume
            // and -128 for mute

            adjustedVelocity = (byte) channelVolumeAdjust;

            if (!_musicDrvMode)
            {
                // INSTR.DAT
                // force note-off
                NoteOff(FMvoiceChannel, note, true);
            }
            else
            {
                // MUSIC.DRV
                if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_START)
                {
                    // force note-off, but only for actual FM voice channels
                    NoteOff(FMvoiceChannel, note, true);
                }
            }

            if (FMvoiceChannel != 9)
            {
                // regular FM voice

                if (!_musicDrvMode)
                {
                    // INSTR.DAT: adjust key note
                    while (adjustedNote < 24)
                        adjustedNote += 12;
                    adjustedNote -= 12;
                }
            }
            else
            {
                // percussion channel
                // MUSIC.DRV variant didn't do this adjustment, it directly used a pointer
                adjustedNote -= 36;
                if (adjustedNote > 40)
                {
                    // Security check
                    Warning("ADLIB: bad percussion channel note");
                    return;
                }

                byte percussionChannel = percussionKeyNoteChannelTable[adjustedNote];
                if (percussionChannel >= AGOS_ADLIB_VOICES_COUNT)
                    return; // INSTR.DAT variant checked for ">" instead of ">=", which seems to have been a bug

                // Map the keynote accordingly
                adjustedNote = _percussionKeyNoteMapping[adjustedNote];
                // Now overwrite the FM voice channel
                FMvoiceChannel = percussionChannel;
            }

            if (!_musicDrvMode)
            {
                // INSTR.DAT

                // Save this key note
                _channels[FMvoiceChannel].currentNote = adjustedNote;

                adjustedVelocity += 24;
                if (adjustedVelocity > 120)
                    adjustedVelocity = 120;
                adjustedVelocity = (byte) (adjustedVelocity >> 1); // divide by 2
            }
            else
            {
                // MUSIC.DRV
                adjustedVelocity = (byte) (adjustedVelocity >> 1); // divide by 2
            }

            // Set volume of voice channel
            NoteOnSetVolume(FMvoiceChannel, 1, adjustedVelocity);
            if (FMvoiceChannel <= AGOS_ADLIB_VOICES_PERCUSSION_START)
            {
                // Set second operator for FM voices + first percussion
                NoteOnSetVolume(FMvoiceChannel, 2, adjustedVelocity);
            }

            if (FMvoiceChannel >= AGOS_ADLIB_VOICES_PERCUSSION_START)
            {
                // Percussion
                byte percussionIdx = (byte) (FMvoiceChannel - AGOS_ADLIB_VOICES_PERCUSSION_START);

                // Enable bit of the requested percussion type
                System.Diagnostics.Debug.Assert(percussionIdx < AGOS_ADLIB_VOICES_PERCUSSION_COUNT);
                _percussionReg |= percussionBits[percussionIdx];
                SetRegister(0xBD, _percussionReg);
            }

            if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_CYMBAL)
            {
                // FM voice, Base Drum, Snare Drum + Tom Tom
                byte adlibNote = adjustedNote;
                byte adlibOctave = 0;
                byte adlibFrequencyIdx = 0;
                ushort adlibFrequency = 0;

                if (!_musicDrvMode)
                {
                    // INSTR.DAT
                    if (adlibNote >= 0x60)
                        adlibNote = 0x5F;

                    adlibOctave = (byte) ((adlibNote / 12) - 1);
                    adlibFrequencyIdx = (byte) (adlibNote % 12);
                    adlibFrequency = frequencyLookUpTable[adlibFrequencyIdx];

                    if ((adlibFrequency & 0x8000) != 0)
                        adlibOctave++;
                    if ((adlibOctave & 0x80) != 0)
                    {
                        adlibOctave++;
                        adlibFrequency = (ushort) (adlibFrequency >> 1);
                    }
                }
                else
                {
                    // MUSIC.DRV variant
                    if (adlibNote >= 19)
                        adlibNote -= 19;

                    adlibOctave = (byte) (adlibNote / 12);
                    adlibFrequencyIdx = (byte) (adlibNote % 12);
                    // additional code, that will lookup octave and do a multiplication with it
                    // noteOn however calls the frequency calculation in a way that it multiplies with 0
                    adlibFrequency = frequencyLookUpTableMusicDrv[adlibFrequencyIdx];
                }

                var regValueA0h = (byte) (adlibFrequency & 0xFF);
                var regValueB0h = (byte) (((adlibFrequency & 0x300) >> 8) | (adlibOctave << 2));
                if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_START)
                {
                    // set Key-On flag for regular FM voices, but not for percussion
                    regValueB0h |= 0x20;
                }

                SetRegister(0xA0 + FMvoiceChannel, regValueA0h);
                SetRegister(0xB0 + FMvoiceChannel, regValueB0h);
                _channels[FMvoiceChannel].currentA0hReg = regValueA0h;
                _channels[FMvoiceChannel].currentB0hReg = regValueB0h;

                if (_musicDrvMode)
                {
                    // MUSIC.DRV
                    if (FMvoiceChannel < AGOS_ADLIB_VOICES_MELODIC_COUNT)
                    {
                        _channels[FMvoiceChannel].currentNote = adjustedNote;
                    }
                }
            }
        }

        // 100% the same for INSTR.DAT and MUSIC.DRV variants
        // except for a bug, that was introduced for MUSIC.DRV
        private void NoteOnSetVolume(byte FMvoiceChannel, byte operatorNr, byte adjustedVelocity)
        {
            byte operatorReg = 0;
            byte regValue40h = 0;

            regValue40h = (byte) ((63 - adjustedVelocity) & 0x3F);

            if ((operatorNr == 1) && (FMvoiceChannel <= AGOS_ADLIB_VOICES_PERCUSSION_START))
            {
                // first operator of FM voice channels or first percussion channel
                var curInstrument = _channels[FMvoiceChannel].currentInstrumentPtr;
                if ((curInstrument.Value.regC0 & 0x01) == 0)
                {
                    // check, if both operators produce sound
                    // only one does, instrument wants fixed volume
                    if (operatorNr == 1)
                    {
                        regValue40h = curInstrument.Value.reg40op1;
                    }
                    else
                    {
                        regValue40h = curInstrument.Value.reg40op2;
                    }

                    // not sure, if we are supposed to implement these bugs, or not
#if Undefined
			if (!_musicDrvMode) {
				// Table is 16 bytes instead of 18 bytes
				if ((FMvoiceChannel == 7) || (FMvoiceChannel == 9)) {
					regValue40h = 0;
					warning("volume set bug (original)");
				}
			}
			if (_musicDrvMode) {
				// MUSIC.DRV variant has a bug, which will overwrite these registers
				// for all operators above 11 / 0Bh, which means percussion will always
				// get a value of 0 (the table holding those bytes was 12 bytes instead of 18
				if (FMvoiceChannel >= AGOS_ADLIB_VOICES_PERCUSSION_START) {
					regValue40h = 0;
					warning("volume set bug (original)");
				}
			}
#endif
                }
            }

            if (operatorNr == 1)
            {
                operatorReg = operator1Register[FMvoiceChannel];
            }
            else
            {
                operatorReg = operator2Register[FMvoiceChannel];
            }
            System.Diagnostics.Debug.Assert(operatorReg != 0xFF); // Security check
            SetRegister(0x40 + operatorReg, regValue40h);
        }

        private void NoteOff(byte FMvoiceChannel, byte note, bool dontCheckNote)
        {
            byte adjustedNote = note;

            if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_START)
            {
                // regular FM voice

                if (!_musicDrvMode)
                {
                    // INSTR.DAT: adjust key note
                    while (adjustedNote < 24)
                        adjustedNote += 12;
                    adjustedNote -= 12;
                }

                if (!dontCheckNote)
                {
                    // check, if current note is also the current actually playing channel note
                    if (_channels[FMvoiceChannel].currentNote != adjustedNote)
                        return; // not the same . ignore this note off command
                }

                byte regValueB0h = (byte) (_channels[FMvoiceChannel].currentB0hReg & 0xDF);
                SetRegister(0xB0 + FMvoiceChannel, regValueB0h);
            }
            else
            {
                // percussion
                adjustedNote -= 36;
                if (adjustedNote > 40)
                {
                    // Security check
                    Warning("ADLIB: bad percussion channel note");
                    return;
                }

                byte percussionChannel = percussionKeyNoteChannelTable[adjustedNote];
                if (percussionChannel > AGOS_ADLIB_VOICES_COUNT)
                    return;

                byte percussionIdx = (byte) (percussionChannel - AGOS_ADLIB_VOICES_PERCUSSION_START);

                // Disable bit of the requested percussion type
                System.Diagnostics.Debug.Assert(percussionIdx < AGOS_ADLIB_VOICES_PERCUSSION_COUNT);
                _percussionReg = (byte) (_percussionReg & ~percussionBits[percussionIdx]);
                SetRegister(0xBD, _percussionReg);
            }
        }


        private void OnTimer()
        {
            _adlibTimerProc?.Invoke(_adlibTimerParam);
        }

        private void ResetAdLib()
        {
            // The original driver sent 0x00 to register 0x00 up to 0xF5
            SetRegister(0xBD, 0x00); // Disable rhythm

            // reset FM voice instrument data
            ResetAdLibOperatorRegisters(0x20, 0);
            ResetAdLibOperatorRegisters(0x60, 0);
            ResetAdLibOperatorRegisters(0x80, 0);
            ResetAdLibFMVoiceChannelRegisters(0xA0, 0);
            ResetAdLibFMVoiceChannelRegisters(0xB0, 0);
            ResetAdLibFMVoiceChannelRegisters(0xC0, 0);
            ResetAdLibOperatorRegisters(0xE0, 0);
            ResetAdLibOperatorRegisters(0x40, 0x3F); // original driver sent 0x00

            SetRegister(0x01, 0x20); // enable waveform control on both operators
            SetRegister(0x04, 0x60); // Timer control

            SetRegister(0x08, 0); // select FM music mode
            SetRegister(0xBD, 0x20); // Enable rhythm

            // reset our percussion register
            _percussionReg = 0x20;
        }

        private void SetRegister(int reg, int value)
        {
            _opl.WriteReg(reg, value);
            //warning("OPL {0:x} {1:x} ({2})", reg, value, value);
        }

        private void ResetAdLibOperatorRegisters(byte baseRegister, byte value)
        {
            byte operatorIndex;

            for (operatorIndex = 0; operatorIndex < 0x16; operatorIndex++)
            {
                switch (operatorIndex)
                {
                    case 0x06:
                    case 0x07:
                    case 0x0E:
                    case 0x0F:
                        break;
                    default:
                        SetRegister(baseRegister + operatorIndex, value);
                        break;
                }
            }
        }

        private void ResetAdLibFMVoiceChannelRegisters(byte baseRegister, byte value)
        {
            byte FMvoiceChannel;

            for (FMvoiceChannel = 0; FMvoiceChannel < AGOS_ADLIB_VOICES_COUNT; FMvoiceChannel++)
            {
                SetRegister(baseRegister + FMvoiceChannel, value);
            }
        }

        private void ProgramChangeSetInstrument(byte FMvoiceChannel, byte mappedInstrumentNr, byte MIDIinstrumentNr)
        {
            byte op1Reg = 0;
            byte op2Reg = 0;

            if (mappedInstrumentNr >= _instrumentCount)
            {
                Warning("ADLIB: tried to set non-existent instrument");
                return; // out of range
            }

            // setup instrument
            var instrumentPtr = new Ptr<InstrumentEntry>(_instrumentTable, mappedInstrumentNr);
            //warning("set instrument for FM voice channel {0}, instrument id {1}", FMvoiceChannel, mappedInstrumentNr);

            op1Reg = operator1Register[FMvoiceChannel];
            op2Reg = operator2Register[FMvoiceChannel];

            SetRegister(0x20 + op1Reg, instrumentPtr.Value.reg20op1);
            SetRegister(0x40 + op1Reg, instrumentPtr.Value.reg40op1);
            SetRegister(0x60 + op1Reg, instrumentPtr.Value.reg60op1);
            SetRegister(0x80 + op1Reg, instrumentPtr.Value.reg80op1);

            if (FMvoiceChannel <= AGOS_ADLIB_VOICES_PERCUSSION_START)
            {
                // set 2nd operator as well for FM voices and first percussion voice
                SetRegister(0x20 + op2Reg, instrumentPtr.Value.reg20op2);
                SetRegister(0x40 + op2Reg, instrumentPtr.Value.reg40op2);
                SetRegister(0x60 + op2Reg, instrumentPtr.Value.reg60op2);
                SetRegister(0x80 + op2Reg, instrumentPtr.Value.reg80op2);

                if (!_musicDrvMode)
                {
                    // set Feedback / Algorithm as well
                    SetRegister(0xC0 + FMvoiceChannel, instrumentPtr.Value.regC0);
                }
                else
                {
                    if (FMvoiceChannel < AGOS_ADLIB_VOICES_PERCUSSION_START)
                    {
                        // set Feedback / Algorithm as well for regular FM voices only
                        SetRegister(0xC0 + FMvoiceChannel, instrumentPtr.Value.regC0);
                    }
                }
            }

            // Remember instrument
            _channels[FMvoiceChannel].currentInstrumentPtr = instrumentPtr;
            _channels[FMvoiceChannel].volumeAdjust = _instrumentVolumeAdjust[MIDIinstrumentNr];
        }

// Called right at the start, we get an INSTR.DAT entry
        public bool SetupInstruments(byte[] driverData, ushort driverDataSize, bool useMusicDrvFile)
        {
            ushort channelMappingOffset = 0;
            ushort channelMappingSize = 0;
            ushort instrumentMappingOffset = 0;
            ushort instrumentMappingSize = 0;
            ushort instrumentVolumeAdjustOffset = 0;
            ushort instrumentVolumeAdjustSize = 0;
            ushort keyNoteMappingOffset = 0;
            ushort keyNoteMappingSize = 0;
            ushort instrumentCount = 0;
            ushort instrumentDataOffset = 0;
            ushort instrumentDataSize = 0;
            ushort instrumentEntrySize = 0;

            if (!useMusicDrvFile)
            {
                // INSTR.DAT: we expect at least 354 bytes
                if (driverDataSize < 354)
                    return false;

                // Data is like this:
                // 128 bytes  instrument mapping
                // 128 bytes  instrument volume adjust (signed!)
                //  16 bytes  unknown
                //  16 bytes  channel mapping
                //  64 bytes  key note mapping (not used for MT32)
                //   1 byte   instrument count
                //   1 byte   bytes per instrument
                //   x bytes  no instruments used for MT32

                channelMappingOffset = 256 + 16;
                channelMappingSize = 16;
                instrumentMappingOffset = 0;
                instrumentMappingSize = 128;
                instrumentVolumeAdjustOffset = 128;
                instrumentVolumeAdjustSize = 128;
                keyNoteMappingOffset = 256 + 16 + 16;
                keyNoteMappingSize = 64;

                byte instrDatInstrumentCount = driverData[256 + 16 + 16 + 64];
                byte instrDatBytesPerInstrument = driverData[256 + 16 + 16 + 64 + 1];

                // We expect 9 bytes per instrument
                if (instrDatBytesPerInstrument != 9)
                    return false;
                // And we also expect at least one adlib instrument
                if (instrDatInstrumentCount == 0)
                    return false;

                instrumentCount = instrDatInstrumentCount;
                instrumentDataOffset = 256 + 16 + 16 + 64 + 2;
                instrumentDataSize = (ushort) (instrDatBytesPerInstrument * instrDatInstrumentCount);
                instrumentEntrySize = instrDatBytesPerInstrument;
            }
            else
            {
                // MUSIC.DRV: we expect at least 468 bytes
                if (driverDataSize < 468)
                    return false;

                // music.drv is basically a driver, but with a few fixed locations for certain data

                channelMappingOffset = 396;
                channelMappingSize = 16;
                instrumentMappingOffset = 140;
                instrumentMappingSize = 128;
                instrumentVolumeAdjustOffset = 140 + 128;
                instrumentVolumeAdjustSize = 128;
                keyNoteMappingOffset = 376 + 36; // adjust by 36, because we adjust keyNote before mapping (see noteOn)
                keyNoteMappingSize = 64;

                // seems to have used 128 + 5 instruments
                // 128 regular ones and an additional 5 for percussion
                instrumentCount = 128 + AGOS_ADLIB_EXTRA_INSTRUMENT_COUNT;
                instrumentDataOffset = 722;
                instrumentEntrySize = 9;
                instrumentDataSize = (ushort) (instrumentCount * instrumentEntrySize);
            }

            // Channel mapping
            if (channelMappingSize != 0)
            {
                // Get these 16 bytes for MIDI channel mapping
                if (channelMappingSize != _channelMapping.Length)
                    return false;

                Array.Copy(driverData, channelMappingOffset, _channelMapping, 0, _channelMapping.Length);
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
                if (instrumentMappingSize > _channelMapping.Length)
                    return false;

                Array.Copy(driverData, instrumentMappingOffset, _instrumentMapping, 0, instrumentMappingSize);
            }
            // Set up straight mapping for the remaining data
            for (ushort instrumentNr = instrumentMappingSize; instrumentNr < _instrumentMapping.Length; instrumentNr++)
            {
                _instrumentMapping[instrumentNr] = (byte) instrumentNr;
            }

            if (instrumentVolumeAdjustSize != 0)
            {
                if (instrumentVolumeAdjustSize != _instrumentVolumeAdjust.Length)
                    return false;

                Array.Copy(driverData, instrumentVolumeAdjustOffset, _instrumentVolumeAdjust, 0,
                    instrumentVolumeAdjustSize);
            }

            // Get key note mapping, if available
            if (keyNoteMappingSize != 0)
            {
                if (keyNoteMappingSize != _percussionKeyNoteMapping.Length)
                    return false;

                Array.Copy(driverData, keyNoteMappingOffset, _percussionKeyNoteMapping, 0, keyNoteMappingSize);
            }

            // Check, if there are enough bytes left to hold all instrument data
            if (driverDataSize < (instrumentDataOffset + instrumentDataSize))
                return false;

            // We release previous instrument data, just in case

            _instrumentTable = new InstrumentEntry[instrumentCount];
            _instrumentCount = (byte) instrumentCount;

            BytePtr instrDATReadPtr = new BytePtr(driverData, instrumentDataOffset);
            var instrumentWritePtr = _instrumentTable;

            for (ushort instrumentNr = 0; instrumentNr < _instrumentCount; instrumentNr++)
            {
                instrumentWritePtr[instrumentNr]=new InstrumentEntry(instrDATReadPtr);
                instrDATReadPtr += instrumentEntrySize;
            }

            // Enable MUSIC.DRV-Mode (slightly different behaviour)
            if (useMusicDrvFile)
                _musicDrvMode = true;

            if (_musicDrvMode)
            {
                // Extra code for MUSIC.DRV

                // This was done during "programChange" in the original driver
                instrumentWritePtr = _instrumentTable;
                for (ushort instrumentNr = 0; instrumentNr < _instrumentCount; instrumentNr++)
                {
                    instrumentWritePtr[instrumentNr].reg80op1 =(byte) (instrumentWritePtr[instrumentNr].reg80op1| 0x03); // set release rate
                    instrumentWritePtr[instrumentNr].reg80op2 =(byte) (instrumentWritePtr[instrumentNr].reg80op2 |0x03);
                }
            }
            return true;
        }
    }
}