//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using System.IO;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    enum kMilesAdLibUpdateFlags
    {
        kMilesAdLibUpdateFlags_None = 0,
        kMilesAdLibUpdateFlags_Reg_20 = 1 << 0,
        kMilesAdLibUpdateFlags_Reg_40 = 1 << 1,
        kMilesAdLibUpdateFlags_Reg_60 = 1 << 2, // register 0x6x + 0x8x
        kMilesAdLibUpdateFlags_Reg_C0 = 1 << 3,
        kMilesAdLibUpdateFlags_Reg_E0 = 1 << 4,
        kMilesAdLibUpdateFlags_Reg_A0 = 1 << 5, // register 0xAx + 0xBx
        kMilesAdLibUpdateFlags_Reg_All = 0x3F
    }

    public class InstrumentEntry
    {
        public byte bankId;
        public byte patchId;
        public short transposition;
        public byte reg20op1;
        public byte reg40op1;
        public byte reg60op1;
        public byte reg80op1;
        public byte regE0op1;
        public byte reg20op2;
        public byte reg40op2;
        public byte reg60op2;
        public byte reg80op2;
        public byte regE0op2;
        public byte regC0;
    }

    // Structure to hold information about current status of MIDI Channels
    class MidiChannelEntry
    {
        public byte currentPatchBank;
        public Ptr<InstrumentEntry> currentInstrumentPtr;
        public ushort currentPitchBender = MidiDriverMilesAdLib.MILES_PITCHBENDER_DEFAULT;
        public byte currentPitchRange;
        public byte currentVoiceProtection;

        public byte currentVolume;
        public byte currentVolumeExpression;

        public byte currentPanning;

        public byte currentModulation;
        public byte currentSustain;

        public byte currentActiveVoicesCount;
    }

    // Structure to hold information about current status of virtual FM Voices
    class VirtualFmVoiceEntry
    {
        public bool inUse;
        public byte actualMidiChannel;

        public Ptr<InstrumentEntry> currentInstrumentPtr;

        public bool isPhysical;
        public byte physicalFmVoice;

        public ushort currentPriority;

        public byte currentOriginalMidiNote;
        public byte currentNote;
        public short currentTransposition;
        public byte currentVelocity;

        public bool sustained;
    }

    // Structure to hold information about current status of physical FM Voices
    struct PhysicalFmVoiceEntry
    {
        public bool inUse;
        public byte virtualFmVoice;

        public byte currentB0hReg;
    }

    /// <summary>
    /// Miles Audio AdLib/OPL3 driver.
    /// </summary>
    /// <remarks>
    /// TODO: currently missing: OPL3 4-op voices
    /// // Special cases (great for testing):
    /// - sustain feature is used by Return To Zork (demo) right at the start
    /// - sherlock holmes 2 does lots of priority sorts right at the start of the intro
    /// </remarks>
    public class MidiDriverMilesAdLib : MidiDriver
    {
        public const int MILES_MIDI_CHANNEL_COUNT = 16;

        // Miles Audio supported controllers for control change messages
        public const int MILES_CONTROLLER_SELECT_PATCH_BANK = 114;
        public const int MILES_CONTROLLER_PROTECT_VOICE = 112;
        public const int MILES_CONTROLLER_PROTECT_TIMBRE = 113;
        public const int MILES_CONTROLLER_MODULATION = 1;
        public const int MILES_CONTROLLER_VOLUME = 7;
        public const int MILES_CONTROLLER_EXPRESSION = 11;
        public const int MILES_CONTROLLER_PANNING = 10;
        public const int MILES_CONTROLLER_SUSTAIN = 64;
        public const int MILES_CONTROLLER_PITCH_RANGE = 6;
        public const int MILES_CONTROLLER_RESET_ALL = 121;
        public const int MILES_CONTROLLER_ALL_NOTES_OFF = 123;
        public const int MILES_CONTROLLER_PATCH_REVERB = 59;
        public const int MILES_CONTROLLER_PATCH_BENDER = 60;
        public const int MILES_CONTROLLER_REVERB_MODE = 61;
        public const int MILES_CONTROLLER_REVERB_TIME = 62;
        public const int MILES_CONTROLLER_REVERB_LEVEL = 63;
        public const int MILES_CONTROLLER_RHYTHM_KEY_TIMBRE = 58;

        // 3 SysEx controllers, each range 5
        // 32-36 for 1st queue
        // 37-41 for 2nd queue
        // 42-46 for 3rd queue
        public const int MILES_CONTROLLER_SYSEX_RANGE_BEGIN = 32;
        public const int MILES_CONTROLLER_SYSEX_RANGE_END = 46;

        public const int MILES_CONTROLLER_SYSEX_QUEUE_COUNT = 3;
        public const int MILES_CONTROLLER_SYSEX_QUEUE_SIZE = 32;

        public const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS1 = 0;
        public const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS2 = 1;
        public const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS3 = 2;
        public const int MILES_CONTROLLER_SYSEX_COMMAND_DATA = 3;
        public const int MILES_CONTROLLER_SYSEX_COMMAND_SEND = 4;

        public const int MILES_CONTROLLER_XMIDI_RANGE_BEGIN = 110;
        public const int MILES_CONTROLLER_XMIDI_RANGE_END = 120;

        // Miles Audio actually used 0x4000, because they didn't shift the 2 bytes properly
        public const int MILES_PITCHBENDER_DEFAULT = 0x2000;

        private const int MILES_ADLIB_VIRTUAL_FMVOICES_COUNT_MAX = 20;
        private const int MILES_ADLIB_PHYSICAL_FMVOICES_COUNT_MAX = 18;

        private const int MILES_ADLIB_PERCUSSION_BANK = 127;

        private const int MILES_ADLIB_STEREO_PANNING_THRESHOLD_LEFT = 27;
        private const int MILES_ADLIB_STEREO_PANNING_THRESHOLD_RIGHT = 100;

        private int _masterVolume;
        private readonly Ptr<InstrumentEntry> _instrumentTablePtr;
        private readonly ushort _instrumentTableCount;
        private bool _modeOPL3;
        private int _modeVirtualFmVoicesCount;
        private int _modePhysicalFmVoicesCount;
        private bool _modeStereo;
        private readonly bool circularPhysicalAssignment;
        private byte circularPhysicalAssignmentFmVoice;

        // stores information about all MIDI channels (not the actual OPL FM voice channels!)
        readonly MidiChannelEntry[] _midiChannels = ScummHelper.CreateArray<MidiChannelEntry>(MILES_MIDI_CHANNEL_COUNT);

        // stores information about all virtual OPL FM voices
        readonly VirtualFmVoiceEntry[] _virtualFmVoices =
            ScummHelper.CreateArray<VirtualFmVoiceEntry>(MILES_ADLIB_VIRTUAL_FMVOICES_COUNT_MAX);

        // stores information about all physical OPL FM voices
        readonly PhysicalFmVoiceEntry[] _physicalFmVoices =
            ScummHelper.CreateArray<PhysicalFmVoiceEntry>(MILES_ADLIB_PHYSICAL_FMVOICES_COUNT_MAX);

        private IOpl _opl;
        private bool _isOpen;

        private TimerProc _adlibTimerProc;
        private object _adlibTimerParam;

        private static readonly short[] milesAdLibOperator1Register =
        {
            0x0000, 0x0001, 0x0002, 0x0008, 0x0009, 0x000A, 0x0010, 0x0011, 0x0012,
            0x0100, 0x0101, 0x0102, 0x0108, 0x0109, 0x010A, 0x0110, 0x0111, 0x0112
        };

        private static readonly ushort[] milesAdLibOperator2Register =
        {
            0x0003, 0x0004, 0x0005, 0x000B, 0x000C, 0x000D, 0x0013, 0x0014, 0x0015,
            0x0103, 0x0104, 0x0105, 0x010B, 0x010C, 0x010D, 0x0113, 0x0114, 0x0115
        };

        private static readonly ushort[] milesAdLibChannelRegister =
        {
            0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008,
            0x0100, 0x0101, 0x0102, 0x0103, 0x0104, 0x0105, 0x0106, 0x0107, 0x0108
        };

        // hardcoded, dumped from ADLIB.MDI
        private static readonly ushort[] milesAdLibFrequencyLookUpTable =
        {
            0x02B2, 0x02B4, 0x02B7, 0x02B9, 0x02BC, 0x02BE, 0x02C1, 0x02C3, 0x02C6, 0x02C9, 0x02CB, 0x02CE,
            0x02D0, 0x02D3, 0x02D6, 0x02D8, 0x02DB, 0x02DD, 0x02E0, 0x02E3, 0x02E5, 0x02E8, 0x02EB, 0x02ED,
            0x02F0, 0x02F3, 0x02F6, 0x02F8, 0x02FB, 0x02FE, 0x0301, 0x0303, 0x0306, 0x0309, 0x030C, 0x030F,
            0x0311, 0x0314, 0x0317, 0x031A, 0x031D, 0x0320, 0x0323, 0x0326, 0x0329, 0x032B, 0x032E, 0x0331,
            0x0334, 0x0337, 0x033A, 0x033D, 0x0340, 0x0343, 0x0346, 0x0349, 0x034C, 0x034F, 0x0352, 0x0356,
            0x0359, 0x035C, 0x035F, 0x0362, 0x0365, 0x0368, 0x036B, 0x036F, 0x0372, 0x0375, 0x0378, 0x037B,
            0x037F, 0x0382, 0x0385, 0x0388, 0x038C, 0x038F, 0x0392, 0x0395, 0x0399, 0x039C, 0x039F, 0x03A3,
            0x03A6, 0x03A9, 0x03AD, 0x03B0, 0x03B4, 0x03B7, 0x03BB, 0x03BE, 0x03C1, 0x03C5, 0x03C8, 0x03CC,
            0x03CF, 0x03D3, 0x03D7, 0x03DA, 0x03DE, 0x03E1, 0x03E5, 0x03E8, 0x03EC, 0x03F0, 0x03F3, 0x03F7,
            0x03FB, 0x03FE, 0xFE01, 0xFE03, 0xFE05, 0xFE07, 0xFE08, 0xFE0A, 0xFE0C, 0xFE0E, 0xFE10, 0xFE12,
            0xFE14, 0xFE16, 0xFE18, 0xFE1A, 0xFE1C, 0xFE1E, 0xFE20, 0xFE21, 0xFE23, 0xFE25, 0xFE27, 0xFE29,
            0xFE2B, 0xFE2D, 0xFE2F, 0xFE31, 0xFE34, 0xFE36, 0xFE38, 0xFE3A, 0xFE3C, 0xFE3E, 0xFE40, 0xFE42,
            0xFE44, 0xFE46, 0xFE48, 0xFE4A, 0xFE4C, 0xFE4F, 0xFE51, 0xFE53, 0xFE55, 0xFE57, 0xFE59, 0xFE5C,
            0xFE5E, 0xFE60, 0xFE62, 0xFE64, 0xFE67, 0xFE69, 0xFE6B, 0xFE6D, 0xFE6F, 0xFE72, 0xFE74, 0xFE76,
            0xFE79, 0xFE7B, 0xFE7D, 0xFE7F, 0xFE82, 0xFE84, 0xFE86, 0xFE89, 0xFE8B, 0xFE8D, 0xFE90, 0xFE92,
            0xFE95, 0xFE97, 0xFE99, 0xFE9C, 0xFE9E, 0xFEA1, 0xFEA3, 0xFEA5, 0xFEA8, 0xFEAA, 0xFEAD, 0xFEAF
        };

        // hardcoded, dumped from ADLIB.MDI
        private static readonly ushort[] milesAdLibVolumeSensitivityTable =
        {
            82, 85, 88, 91, 94, 97, 100, 103, 106, 109, 112, 115, 118, 121, 124, 127
        };

        public override uint BaseTempo => 1000000 / Opl.DefaultCallbackFrequency;

        public MidiDriverMilesAdLib(Ptr<InstrumentEntry> instrumentTablePtr, ushort instrumentTableCount)
        {
            _masterVolume = 15;

            _instrumentTablePtr = instrumentTablePtr;
            _instrumentTableCount = instrumentTableCount;

            // Set up for OPL3, we will downgrade in case we can't create OPL3 emulator
            // regular AdLib (OPL2) card
            _modeOPL3 = true;
            _modeVirtualFmVoicesCount = 20;
            _modePhysicalFmVoicesCount = 18;
            _modeStereo = true;

            // Older Miles Audio drivers did not do a circular assign for physical FM-voices
            // Sherlock Holmes 2 used the circular assign
            circularPhysicalAssignment = true;
            // this way the first circular physical FM-voice search will start at FM-voice 0
            circularPhysicalAssignmentFmVoice = MILES_ADLIB_PHYSICAL_FMVOICES_COUNT_MAX;

            ResetData();
        }

        public static MidiDriver Create(string filenameAdLib, string filenameOPL3,
            Stream streamAdLib = null, Stream streamOPL3 = null)
        {
            // Load adlib instrument data from file SAMPLE.AD (OPL3: SAMPLE.OPL)
            string timbreFilename = null;
            Stream timbreStream = null;

            bool preferOPL3 = false;

            int fileSize = 0;
            int fileDataOffset;
            int fileDataLeft;

            int streamSize = 0;
            Stream fileStream = null;
            byte[] streamDataPtr = null;

            byte curBankId;
            byte curPatchId;

            ushort instrumentTableCount = 0;

            // Logic:
            // We prefer OPL3 timbre data in case OPL3 is available in ScummVM
            // If it's not or OPL3 timbre data is not available, we go for AdLib timbre data
            // And if OPL3 is not available in ScummVM and also AdLib timbre data is not available,
            // we then still go for OPL3 timbre data.
            //
            // Note: for most games OPL3 timbre data + AdLib timbre data is the same.
            //       And at least in theory we should still be able to use OPL3 timbre data even for AdLib.
            //       However there is a special OPL3-specific timbre format, which is currently not supported.
            //       In this case the error message "unsupported instrument size" should appear. I haven't found
            //       a game that uses it, which is why I haven't implemented it yet.

            // TODO: if (OPL::Config::detect(OPL::Config::kOpl3) >= 0) {
            if (false)
            {
                // OPL3 available, prefer OPL3 timbre data because of this
                preferOPL3 = true;
            }

            // Check if streams were passed to us and select one of them
            if ((streamAdLib != null) || (streamOPL3 != null))
            {
                // At least one stream was passed by caller
                if (preferOPL3)
                {
                    // Prefer OPL3 timbre stream in case OPL3 is available
                    timbreStream = streamOPL3;
                }
                if (timbreStream == null)
                {
                    // Otherwise prefer AdLib timbre stream first
                    if (streamAdLib != null)
                    {
                        timbreStream = streamAdLib;
                    }
                    else
                    {
                        // If not available, use OPL3 timbre stream
                        if (streamOPL3 != null)
                        {
                            timbreStream = streamOPL3;
                        }
                    }
                }
            }

            // Now check if any filename was passed to us
            if ((!string.IsNullOrEmpty(filenameAdLib)) || (!string.IsNullOrEmpty(filenameOPL3)))
            {
                // If that's the case, check if one of those exists
                if (preferOPL3)
                {
                    // OPL3 available
                    if (!string.IsNullOrEmpty(filenameOPL3))
                    {
                        if (Engine.FileExists(filenameOPL3))
                        {
                            // If OPL3 available, prefer OPL3 timbre file in case file exists
                            timbreFilename = filenameOPL3;
                        }
                    }
                    if (string.IsNullOrEmpty(timbreFilename))
                    {
                        if (!string.IsNullOrEmpty(filenameAdLib))
                        {
                            if (Engine.FileExists(filenameAdLib))
                            {
                                // otherwise use AdLib timbre file, if it exists
                                timbreFilename = filenameAdLib;
                            }
                        }
                    }
                }
                else
                {
                    // OPL3 not available
                    // Prefer the AdLib one for now
                    if (!string.IsNullOrEmpty(filenameAdLib))
                    {
                        if (Engine.FileExists(filenameAdLib))
                        {
                            // if AdLib file exists, use it
                            timbreFilename = filenameAdLib;
                        }
                    }
                    if (string.IsNullOrEmpty(timbreFilename))
                    {
                        if (!string.IsNullOrEmpty(filenameOPL3))
                        {
                            if (Engine.FileExists(filenameOPL3))
                            {
                                // if OPL3 file exists, use it
                                timbreFilename = filenameOPL3;
                            }
                        }
                    }
                }
                if (string.IsNullOrEmpty(timbreFilename) && (timbreStream == null))
                {
                    // If none of them exists and also no stream was passed, we can't do anything about it
                    if (!string.IsNullOrEmpty(filenameAdLib))
                    {
                        if (!string.IsNullOrEmpty(filenameOPL3))
                        {
                            Error("MILES-ADLIB: could not open timbre file ({0} or {1})", filenameAdLib, filenameOPL3);
                        }
                        else
                        {
                            Error("MILES-ADLIB: could not open timbre file ({0})", filenameAdLib);
                        }
                    }
                    else
                    {
                        Error("MILES-ADLIB: could not open timbre file ({0})", filenameOPL3);
                    }
                }
            }

            if (!string.IsNullOrEmpty(timbreFilename))
            {
                // Filename was passed to us and file exists (this is the common case for most games)
                // We prefer this situation

                fileStream = Engine.OpenFileRead(timbreFilename);
                if (fileStream == null)
                    Error("MILES-ADLIB: could not open timbre file ({0})", timbreFilename);

                streamSize = (int) fileStream.Length;

                streamDataPtr = new byte[streamSize];

                if (fileStream.Read(streamDataPtr, 0, streamSize) != streamSize)
                    Error("MILES-ADLIB: error while reading timbre file ({0})", timbreFilename);
                fileStream.Dispose();
            }
            else if (timbreStream != null)
            {
                // Timbre data was passed directly (possibly read from resource file by caller)
                // Currently used by "Amazon Guardians of Eden", "Simon 2" and "Return To Zork"
                streamSize = (int) timbreStream.Length;

                streamDataPtr = new byte[streamSize];

                if (timbreStream.Read(streamDataPtr, 0, streamSize) != streamSize)
                    Error("MILES-ADLIB: error while reading timbre stream");
            }
            else
            {
                Error("MILES-ADLIB: timbre filenames nor timbre stream were passed");
            }

            fileStream.Dispose();

            // File is like this:
            // [patch:BYTE] [bank:BYTE] [patchoffset:UINT32]
            // ...
            // until patch + bank are both 0xFF, which signals end of header

            // First we check how many entries there are
            fileDataOffset = 0;
            fileDataLeft = streamSize;
            while (true)
            {
                if (fileDataLeft < 6)
                    Error("MILES-ADLIB: unexpected EOF in instrument file");

                curPatchId = streamDataPtr[fileDataOffset++];
                curBankId = streamDataPtr[fileDataOffset++];

                if ((curBankId == 0xFF) && (curPatchId == 0xFF))
                    break;

                fileDataOffset += 4; // skip over offset
                instrumentTableCount++;
            }

            if (instrumentTableCount == 0)
                Error("MILES-ADLIB: no instruments in instrument file");

            // Allocate space for instruments
            var instrumentTablePtr = new InstrumentEntry[instrumentTableCount];

            // Now actually read all entries
            fileDataOffset = 0;
            fileDataLeft = fileSize;
            foreach (var instrumentPtr in instrumentTablePtr)
            {
                curPatchId = streamDataPtr[fileDataOffset++];
                curBankId = streamDataPtr[fileDataOffset++];

                if ((curBankId == 0xFF) && (curPatchId == 0xFF))
                    break;

                int instrumentOffset = streamDataPtr.ToInt32(fileDataOffset);
                fileDataOffset += 4;

                instrumentPtr.bankId = curBankId;
                instrumentPtr.patchId = curPatchId;

                ushort instrumentDataSize = streamDataPtr.ToUInt16(instrumentOffset);
                if (instrumentDataSize != 14)
                    Error("MILES-ADLIB: unsupported instrument size");

                instrumentPtr.transposition = (sbyte) streamDataPtr[instrumentOffset + 2];
                instrumentPtr.reg20op1 = streamDataPtr[instrumentOffset + 3];
                instrumentPtr.reg40op1 = streamDataPtr[instrumentOffset + 4];
                instrumentPtr.reg60op1 = streamDataPtr[instrumentOffset + 5];
                instrumentPtr.reg80op1 = streamDataPtr[instrumentOffset + 6];
                instrumentPtr.regE0op1 = streamDataPtr[instrumentOffset + 7];
                instrumentPtr.regC0 = streamDataPtr[instrumentOffset + 8];
                instrumentPtr.reg20op2 = streamDataPtr[instrumentOffset + 9];
                instrumentPtr.reg40op2 = streamDataPtr[instrumentOffset + 10];
                instrumentPtr.reg60op2 = streamDataPtr[instrumentOffset + 11];
                instrumentPtr.reg80op2 = streamDataPtr[instrumentOffset + 12];
                instrumentPtr.regE0op2 = streamDataPtr[instrumentOffset + 13];
            }

            return new MidiDriverMilesAdLib(instrumentTablePtr, instrumentTableCount);
        }

        public override int Property(int prop, int param)
        {
            return 0;
        }

        private void ResetData()
        {
            Array.Clear(_midiChannels, 0, _midiChannels.Length);
            Array.Clear(_virtualFmVoices, 0, _virtualFmVoices.Length);
            Array.Clear(_physicalFmVoices, 0, _physicalFmVoices.Length);

            for (byte midiChannel = 0; midiChannel < MILES_MIDI_CHANNEL_COUNT; midiChannel++)
            {
                // defaults, were sent to driver during driver initialization
                _midiChannels[midiChannel].currentVolume = 0x7F;
                _midiChannels[midiChannel].currentPanning = 0x40; // center
                _midiChannels[midiChannel].currentVolumeExpression = 127;

                // Miles Audio 2: hardcoded pitch range as a global (not channel specific), set to 12
                // Miles Audio 3: pitch range per MIDI channel
                _midiChannels[midiChannel].currentPitchBender = MILES_PITCHBENDER_DEFAULT;
                _midiChannels[midiChannel].currentPitchRange = 12;
            }
        }

        public void SetVolume(byte volume)
        {
            _masterVolume = volume;
            //renewNotes(-1, true);
        }

        // MIDI messages can be found at http://www.midi.org/techspecs/midimessages.php
        public override void Send(int b)
        {
            byte command = (byte) (b & 0xf0);
            byte channel = (byte) (b & 0xf);
            byte op1 = (byte) ((b >> 8) & 0xff);
            byte op2 = (byte) ((b >> 16) & 0xff);

            switch (command)
            {
                case 0x80:
                    NoteOff(channel, op1);
                    break;
                case 0x90:
                    NoteOn(channel, op1, op2);
                    break;
                case 0xb0: // Control change
                    ControlChange(channel, op1, op2);
                    break;
                case 0xc0: // Program Change
                    ProgramChange(channel, op1);
                    break;
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                    // Aftertouch doesn't seem to be implemented in the Miles Audio AdLib driver
                    break;
                case 0xe0:
                    PitchBendChange(channel, op1, op2);
                    break;
                case 0xf0: // SysEx
                    Warning("MILES-ADLIB: SysEx: {0:X}", b);
                    break;
                default:
                    Warning("MILES-ADLIB: Unknown event {0:X2}", command);
                    break;
            }
        }

        public override MidiDriverError Open()
        {
            if (_modeOPL3)
            {
                // TODO: Try to create OPL3 first
                _opl = null;
            }
            if (_opl == null)
            {
                // not created yet, downgrade to OPL2
                _modeOPL3 = false;
                _modeVirtualFmVoicesCount = 16;
                _modePhysicalFmVoicesCount = 9;
                _modeStereo = false;

                // TODO: _opl = OPL::Config::create(OPL::Config::kOpl2);
                _opl = new DosBoxOPL(OplType.Opl2);
            }

            if (_opl == null)
            {
                // We still got nothing . can't do anything anymore
                return (MidiDriverError) (-1);
            }

            _opl.Init();

            _isOpen = true;

            _opl.Start(OnTimer);

            ResetAdLib();

            return 0;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _adlibTimerProc = timerProc;
            _adlibTimerParam = timerParam;
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        private void PitchBendChange(byte midiChannel, byte parameter1, byte parameter2)
        {
            // Miles Audio actually didn't shift parameter 2 1 down in here
            // which means in memory it used a 15-bit pitch bender, which also means the default was 0x4000
            if (((parameter1 & 0x80) != 0) || ((parameter2 & 0x80) != 0))
            {
                Warning("MILES-ADLIB: invalid pitch bend change");
                return;
            }
            _midiChannels[midiChannel].currentPitchBender = (ushort) (parameter1 | (parameter2 << 7));
        }

        private void ResetAdLib()
        {
            if (_modeOPL3)
            {
                SetRegister(0x105, 1); // enable OPL3
                SetRegister(0x104, 0); // activate 18 2-operator FM-voices
            }

            SetRegister(0x01, 0x20); // enable waveform control on both operators
            SetRegister(0x04, 0xE0); // Timer control

            SetRegister(0x08, 0); // select FM music mode
            SetRegister(0xBD, 0); // disable Rhythm

            // reset FM voice instrument data
            ResetAdLibOperatorRegisters(0x20, 0);
            ResetAdLibOperatorRegisters(0x60, 0);
            ResetAdLibOperatorRegisters(0x80, 0);
            ResetAdLibFmVoiceChannelRegisters(0xA0, 0);
            ResetAdLibFmVoiceChannelRegisters(0xB0, 0);
            ResetAdLibFmVoiceChannelRegisters(0xC0, 0);
            ResetAdLibOperatorRegisters(0xE0, 0);
            ResetAdLibOperatorRegisters(0x40, 0x3F);
        }

        private void SetRegister(int reg, int value)
        {
            if ((reg & 0x100) == 0)
            {
                _opl.Write(0x220, reg);
                _opl.Write(0x221, value);
                //warning("OPL write %x %x (%d)", reg, value, value);
            }
            else
            {
                _opl.Write(0x222, reg & 0xFF);
                _opl.Write(0x223, value);
                //warning("OPL3 write %x %x (%d)", reg & 0xFF, value, value);
            }
        }

        private void ResetAdLibOperatorRegisters(byte baseRegister, byte value)
        {
            byte physicalFmVoice = 0;

            for (physicalFmVoice = 0; physicalFmVoice < _modePhysicalFmVoicesCount; physicalFmVoice++)
            {
                SetRegister(baseRegister + milesAdLibOperator1Register[physicalFmVoice], value);
                SetRegister(baseRegister + milesAdLibOperator2Register[physicalFmVoice], value);
            }
        }

        private void ResetAdLibFmVoiceChannelRegisters(byte baseRegister, byte value)
        {
            byte physicalFmVoice = 0;

            for (physicalFmVoice = 0; physicalFmVoice < _modePhysicalFmVoicesCount; physicalFmVoice++)
            {
                SetRegister(baseRegister + milesAdLibChannelRegister[physicalFmVoice], value);
            }
        }

        private void OnTimer()
        {
            _adlibTimerProc?.Invoke(_adlibTimerParam);
        }

        private void NoteOn(byte midiChannel, byte note, byte velocity)
        {
            Ptr<InstrumentEntry> instrumentPtr;

            if (velocity == 0)
            {
                NoteOff(midiChannel, note);
                return;
            }

            if (midiChannel == 9)
            {
                // percussion channel
                // search for instrument according to given note
                instrumentPtr = SearchInstrument(MILES_ADLIB_PERCUSSION_BANK, note);
            }
            else
            {
                // directly get instrument of channel
                instrumentPtr = _midiChannels[midiChannel].currentInstrumentPtr;
            }
            if (instrumentPtr == Ptr<InstrumentEntry>.Null)
            {
                Warning("MILES-ADLIB: noteOn: invalid instrument");
                return;
            }

            //warning("Note On: channel %d, note %d, velocity %d, instrument %d/%d", midiChannel, note, velocity, instrumentPtr.bankId, instrumentPtr.patchId);

            // look for free virtual FM voice
            short virtualFmVoice = SearchFreeVirtualFmVoiceChannel();

            if (virtualFmVoice == -1)
            {
                // Out of virtual voices,  can't do anything about it
                return;
            }

            // Scale back velocity
            velocity = (byte) ((velocity & 0x7F) >> 3);
            velocity = (byte) milesAdLibVolumeSensitivityTable[velocity];

            if (midiChannel != 9)
            {
                _virtualFmVoices[virtualFmVoice].currentNote = note;
                _virtualFmVoices[virtualFmVoice].currentTransposition = instrumentPtr.Value.transposition;
            }
            else
            {
                // Percussion channel
                _virtualFmVoices[virtualFmVoice].currentNote = (byte) instrumentPtr.Value.transposition;
                _virtualFmVoices[virtualFmVoice].currentTransposition = 0;
            }

            _virtualFmVoices[virtualFmVoice].inUse = true;
            _virtualFmVoices[virtualFmVoice].actualMidiChannel = midiChannel;
            _virtualFmVoices[virtualFmVoice].currentOriginalMidiNote = note;
            _virtualFmVoices[virtualFmVoice].currentInstrumentPtr = instrumentPtr;
            _virtualFmVoices[virtualFmVoice].currentVelocity = velocity;
            _virtualFmVoices[virtualFmVoice].isPhysical = false;
            _virtualFmVoices[virtualFmVoice].sustained = false;
            _virtualFmVoices[virtualFmVoice].currentPriority = 32767;

            short physicalFmVoice = SearchFreePhysicalFmVoiceChannel();
            if (physicalFmVoice == -1)
            {
                // None found
                // go through priorities and reshuffle voices
                PrioritySort();
                return;
            }

            // Another voice active on this MIDI channel
            _midiChannels[midiChannel].currentActiveVoicesCount++;

            // Mark virtual FM-Voice as being connected to physical FM-Voice
            _virtualFmVoices[virtualFmVoice].isPhysical = true;
            _virtualFmVoices[virtualFmVoice].physicalFmVoice = (byte) physicalFmVoice;

            // Mark physical FM-Voice as being connected to virtual FM-Voice
            _physicalFmVoices[physicalFmVoice].inUse = true;
            _physicalFmVoices[physicalFmVoice].virtualFmVoice = (byte) virtualFmVoice;

            // Update the physical FM-Voice
            UpdatePhysicalFmVoice((byte) virtualFmVoice, true, kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_All);
        }

        private void PrioritySort()
        {
            byte virtualFmVoice;
            ushort virtualPriority;
            ushort[] virtualPriorities = new ushort[MILES_ADLIB_VIRTUAL_FMVOICES_COUNT_MAX];
            ushort virtualFmVoicesCount = 0;
            byte midiChannel;

            //warning("prioritysort");

            // First calculate priorities for all virtual FM voices, that are in use
            for (virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
            {
                if (_virtualFmVoices[virtualFmVoice].inUse)
                {
                    virtualFmVoicesCount++;

                    midiChannel = _virtualFmVoices[virtualFmVoice].actualMidiChannel;
                    if (_midiChannels[midiChannel].currentVoiceProtection >= 64)
                    {
                        // Voice protection enabled
                        virtualPriority = 0xFFFF;
                    }
                    else
                    {
                        virtualPriority = _virtualFmVoices[virtualFmVoice].currentPriority;
                    }
                    byte currentActiveVoicesCount = _midiChannels[midiChannel].currentActiveVoicesCount;
                    if (virtualPriority >= currentActiveVoicesCount)
                    {
                        virtualPriority -= _midiChannels[midiChannel].currentActiveVoicesCount;
                    }
                    else
                    {
                        virtualPriority = 0; // overflow, should never happen
                    }
                    virtualPriorities[virtualFmVoice] = virtualPriority;
                }
            }

            //
            while (virtualFmVoicesCount != 0)
            {
                ushort unvoicedHighestPriority = 0;
                byte unvoicedHighestFmVoice = 0;
                ushort voicedLowestPriority = 65535;
                byte voicedLowestFmVoice = 0;

                for (virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
                {
                    if (_virtualFmVoices[virtualFmVoice].inUse)
                    {
                        virtualPriority = virtualPriorities[virtualFmVoice];
                        if (!_virtualFmVoices[virtualFmVoice].isPhysical)
                        {
                            // currently not physical, so unvoiced
                            if (virtualPriority >= unvoicedHighestPriority)
                            {
                                unvoicedHighestPriority = virtualPriority;
                                unvoicedHighestFmVoice = virtualFmVoice;
                            }
                        }
                        else
                        {
                            // currently physical, so voiced
                            if (virtualPriority <= voicedLowestPriority)
                            {
                                voicedLowestPriority = virtualPriority;
                                voicedLowestFmVoice = virtualFmVoice;
                            }
                        }
                    }
                }

                if (unvoicedHighestPriority < voicedLowestPriority)
                    break; // We are done

                if (unvoicedHighestPriority == 0)
                    break;

                // Safety checks
                System.Diagnostics.Debug.Assert(_virtualFmVoices[voicedLowestFmVoice].isPhysical);
                System.Diagnostics.Debug.Assert(!_virtualFmVoices[unvoicedHighestFmVoice].isPhysical);

                // Steal this physical voice
                byte physicalFmVoice = _virtualFmVoices[voicedLowestFmVoice].physicalFmVoice;

                //warning("MILES-ADLIB: stealing physical FM-Voice %d from virtual FM-Voice %d for virtual FM-Voice %d", physicalFmVoice, voicedLowestFmVoice, unvoicedHighestFmVoice);
                //warning("priority old %d, priority new %d", unvoicedHighestPriority, voicedLowestPriority);

                ReleaseFmVoice(voicedLowestFmVoice);

                // Get some data of the unvoiced highest priority virtual FM Voice
                midiChannel = _virtualFmVoices[unvoicedHighestFmVoice].actualMidiChannel;

                // Another voice active on this MIDI channel
                _midiChannels[midiChannel].currentActiveVoicesCount++;

                // Mark virtual FM-Voice as being connected to physical FM-Voice
                _virtualFmVoices[unvoicedHighestFmVoice].isPhysical = true;
                _virtualFmVoices[unvoicedHighestFmVoice].physicalFmVoice = physicalFmVoice;

                // Mark physical FM-Voice as being connected to virtual FM-Voice
                _physicalFmVoices[physicalFmVoice].inUse = true;
                _physicalFmVoices[physicalFmVoice].virtualFmVoice = unvoicedHighestFmVoice;

                // Update the physical FM-Voice
                UpdatePhysicalFmVoice(unvoicedHighestFmVoice, true,
                    kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_All);

                virtualFmVoicesCount--;
            }
        }


        private short SearchFreeVirtualFmVoiceChannel()
        {
            for (byte virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
            {
                if (!_virtualFmVoices[virtualFmVoice].inUse)
                    return virtualFmVoice;
            }
            return -1;
        }

        private Ptr<InstrumentEntry> SearchInstrument(byte bankId, byte patchId)
        {
            var instrumentPtr = _instrumentTablePtr;

            for (var instrumentNr = 0; instrumentNr < _instrumentTableCount; instrumentNr++)
            {
                if ((instrumentPtr.Value.bankId == bankId) && (instrumentPtr.Value.patchId == patchId))
                {
                    return instrumentPtr;
                }
                instrumentPtr.Offset++;
            }

            return Ptr<InstrumentEntry>.Null;
        }

        private short SearchFreePhysicalFmVoiceChannel()
        {
            if (!circularPhysicalAssignment)
            {
                // Older assign logic
                for (byte physicalFmVoice = 0; physicalFmVoice < _modePhysicalFmVoicesCount; physicalFmVoice++)
                {
                    if (!_physicalFmVoices[physicalFmVoice].inUse)
                        return physicalFmVoice;
                }
            }
            else
            {
                // Newer one
                // Remembers last physical FM-voice and searches from that spot
                byte physicalFmVoice = circularPhysicalAssignmentFmVoice;
                for (byte physicalFmVoiceCount = 0;
                    physicalFmVoiceCount < _modePhysicalFmVoicesCount;
                    physicalFmVoiceCount++)
                {
                    physicalFmVoice++;
                    if (physicalFmVoice >= _modePhysicalFmVoicesCount)
                        physicalFmVoice = 0;
                    if (!_physicalFmVoices[physicalFmVoice].inUse)
                    {
                        circularPhysicalAssignmentFmVoice = physicalFmVoice;
                        return physicalFmVoice;
                    }
                }
            }
            return -1;
        }

        private void NoteOff(byte midiChannel, byte note)
        {
            //warning("Note Off: channel %d, note %d", midiChannel, note);

            // Search through all virtual FM-Voices for current midiChannel + note
            for (byte virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
            {
                if (_virtualFmVoices[virtualFmVoice].inUse)
                {
                    if ((_virtualFmVoices[virtualFmVoice].actualMidiChannel == midiChannel) &&
                        (_virtualFmVoices[virtualFmVoice].currentOriginalMidiNote == note))
                    {
                        // found one
                        if (_midiChannels[midiChannel].currentSustain >= 64)
                        {
                            _virtualFmVoices[virtualFmVoice].sustained = true;
                            continue;
                        }
                        //
                        ReleaseFmVoice(virtualFmVoice);
                    }
                }
            }
        }

        private void ReleaseFmVoice(byte virtualFmVoice)
        {
            // virtual Voice not actually played? . exit
            if (!_virtualFmVoices[virtualFmVoice].isPhysical)
            {
                _virtualFmVoices[virtualFmVoice].inUse = false;
                return;
            }

            byte midiChannel = _virtualFmVoices[virtualFmVoice].actualMidiChannel;
            byte physicalFmVoice = _virtualFmVoices[virtualFmVoice].physicalFmVoice;

            // stop note from playing
            UpdatePhysicalFmVoice(virtualFmVoice, false, kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_A0);

            // this virtual FM voice isn't physical anymore
            _virtualFmVoices[virtualFmVoice].isPhysical = false;
            _virtualFmVoices[virtualFmVoice].inUse = false;

            // Remove physical FM-Voice from being active
            _physicalFmVoices[physicalFmVoice].inUse = false;

            // One less voice active on this MIDI channel
            System.Diagnostics.Debug.Assert(_midiChannels[midiChannel].currentActiveVoicesCount != 0);
            _midiChannels[midiChannel].currentActiveVoicesCount--;
        }

        private void ControlChange(byte midiChannel, byte controllerNumber, byte controllerValue)
        {
            kMilesAdLibUpdateFlags registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_None;

            switch (controllerNumber)
            {
                case MILES_CONTROLLER_SELECT_PATCH_BANK:
                    //warning("patch bank channel %d, bank %x", midiChannel, controllerValue);
                    _midiChannels[midiChannel].currentPatchBank = controllerValue;
                    break;

                case MILES_CONTROLLER_PROTECT_VOICE:
                    _midiChannels[midiChannel].currentVoiceProtection = controllerValue;
                    break;

                case MILES_CONTROLLER_PROTECT_TIMBRE:
                    // It seems that this can get ignored, because we don't cache timbres at all
                    break;

                case MILES_CONTROLLER_MODULATION:
                    _midiChannels[midiChannel].currentModulation = controllerValue;
                    registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_20;
                    break;

                case MILES_CONTROLLER_VOLUME:
                    _midiChannels[midiChannel].currentVolume = controllerValue;
                    registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_40;
                    break;

                case MILES_CONTROLLER_EXPRESSION:
                    _midiChannels[midiChannel].currentVolumeExpression = controllerValue;
                    registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_40;
                    break;

                case MILES_CONTROLLER_PANNING:
                    _midiChannels[midiChannel].currentPanning = controllerValue;
                    if (_modeStereo)
                    {
                        // Update register only in case we are in stereo mode
                        registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_C0;
                    }
                    break;

                case MILES_CONTROLLER_SUSTAIN:
                    _midiChannels[midiChannel].currentSustain = controllerValue;
                    if (controllerValue < 64)
                    {
                        ReleaseSustain(midiChannel);
                    }
                    break;

                case MILES_CONTROLLER_PITCH_RANGE:
                    // Miles Audio 3 feature
                    _midiChannels[midiChannel].currentPitchRange = controllerValue;
                    break;

                case MILES_CONTROLLER_RESET_ALL:
                    _midiChannels[midiChannel].currentSustain = 0;
                    ReleaseSustain(midiChannel);
                    _midiChannels[midiChannel].currentModulation = 0;
                    _midiChannels[midiChannel].currentVolumeExpression = 127;
                    _midiChannels[midiChannel].currentPitchBender = MILES_PITCHBENDER_DEFAULT;
                    registerUpdateFlags = kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_20 |
                                          kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_40 |
                                          kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_A0;
                    break;

                case MILES_CONTROLLER_ALL_NOTES_OFF:
                    for (byte virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
                    {
                        if (_virtualFmVoices[virtualFmVoice].inUse)
                        {
                            // used
                            if (_virtualFmVoices[virtualFmVoice].actualMidiChannel == midiChannel)
                            {
                                // by our current MIDI channel . noteOff
                                NoteOff(midiChannel, _virtualFmVoices[virtualFmVoice].currentNote);
                            }
                        }
                    }
                    break;

                default:
                    //warning("MILES-ADLIB: Unsupported control change %d", controllerNumber);
                    break;
            }

            if (registerUpdateFlags != kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_None)
            {
                for (byte virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
                {
                    if (_virtualFmVoices[virtualFmVoice].inUse)
                    {
                        // used
                        if (_virtualFmVoices[virtualFmVoice].actualMidiChannel == midiChannel)
                        {
                            // by our current MIDI channel . update
                            UpdatePhysicalFmVoice(virtualFmVoice, true, registerUpdateFlags);
                        }
                    }
                }
            }
        }

        private void ReleaseSustain(byte midiChannel)
        {
            // Search through all virtual FM-Voices for currently sustained notes and call noteOff on them
            for (byte virtualFmVoice = 0; virtualFmVoice < _modeVirtualFmVoicesCount; virtualFmVoice++)
            {
                if (_virtualFmVoices[virtualFmVoice].inUse)
                {
                    if ((_virtualFmVoices[virtualFmVoice].actualMidiChannel == midiChannel) &&
                        (_virtualFmVoices[virtualFmVoice].sustained))
                    {
                        // is currently sustained
                        // so do a noteOff (which will check current sustain controller)
                        NoteOff(midiChannel, _virtualFmVoices[virtualFmVoice].currentOriginalMidiNote);
                    }
                }
            }
        }

        private void ProgramChange(byte midiChannel, byte patchId)
        {
            byte patchBank = _midiChannels[midiChannel].currentPatchBank;

            //warning("patch channel %d, patch %x, bank %x", midiChannel, patchId, patchBank);

            // we check, if we actually have data for the requested instrument...
            var instrumentPtr = SearchInstrument(patchBank, patchId);
            if (instrumentPtr != Ptr<InstrumentEntry>.Null)
            {
                Warning("MILES-ADLIB: unknown instrument requested ({0}, {1})", patchBank, patchId);
                return;
            }

            // and remember it in that case for the current MIDI-channel
            _midiChannels[midiChannel].currentInstrumentPtr = instrumentPtr;
        }


        private void UpdatePhysicalFmVoice(byte virtualFmVoice, bool keyOn, kMilesAdLibUpdateFlags registerUpdateFlags)
        {
            byte midiChannel = _virtualFmVoices[virtualFmVoice].actualMidiChannel;

            if (!_virtualFmVoices[virtualFmVoice].isPhysical)
            {
                // virtual FM-Voice has no physical FM-Voice assigned? . ignore
                return;
            }

            byte physicalFmVoice = _virtualFmVoices[virtualFmVoice].physicalFmVoice;
            Ptr<InstrumentEntry> instrumentPtr = _virtualFmVoices[virtualFmVoice].currentInstrumentPtr;

            ushort op1Reg = (ushort) milesAdLibOperator1Register[physicalFmVoice];
            ushort op2Reg = milesAdLibOperator2Register[physicalFmVoice];
            ushort channelReg = milesAdLibChannelRegister[physicalFmVoice];

            ushort compositeVolume = 0;

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_40))
            {
                // Calculate new volume
                byte midiVolume = _midiChannels[midiChannel].currentVolume;
                byte midiVolumeExpression = _midiChannels[midiChannel].currentVolumeExpression;
                compositeVolume = (ushort) (midiVolume * midiVolumeExpression * 2);

                compositeVolume = (ushort) (compositeVolume >> 8); // get upmost 8 bits
                if (compositeVolume != 0)
                    compositeVolume++; // round up in case result wasn't 0

                compositeVolume = (ushort) (compositeVolume * _virtualFmVoices[virtualFmVoice].currentVelocity * 2);
                compositeVolume = (ushort) (compositeVolume >> 8); // get upmost 8 bits
                if (compositeVolume != 0)
                    compositeVolume++; // round up in case result wasn't 0
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_20))
            {
                // Amplitude Modulation / Vibrato / Envelope Generator Type / Keyboard Scaling Rate / Modulator Frequency Multiple
                byte reg20op1 = instrumentPtr.Value.reg20op1;
                byte reg20op2 = instrumentPtr.Value.reg20op2;

                if (_midiChannels[midiChannel].currentModulation >= 64)
                {
                    // set bit 6 (Vibrato)
                    reg20op1 |= 0x40;
                    reg20op2 |= 0x40;
                }
                SetRegister(0x20 + op1Reg, reg20op1);
                SetRegister(0x20 + op2Reg, reg20op2);
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_40))
            {
                // Volume (Level Key Scaling / Total Level)
                byte reg40op1 = instrumentPtr.Value.reg40op1;
                byte reg40op2 = instrumentPtr.Value.reg40op2;

                ushort volumeOp1 = (ushort) ((~reg40op1) & 0x3F);
                ushort volumeOp2 = (ushort) ((~reg40op2) & 0x3F);

                if ((instrumentPtr.Value.regC0 & 1) != 0)
                {
                    // operator 2 enabled
                    // scale volume factor
                    volumeOp1 = (ushort) ((volumeOp1 * compositeVolume) / 127);
                    // 2nd operator always scaled
                }

                volumeOp2 = (ushort) ((volumeOp2 * compositeVolume) / 127);

                volumeOp1 = (ushort) ((~volumeOp1) & 0x3F); // negate it, so we get the proper value for the register
                volumeOp2 = (ushort) ((~volumeOp2) & 0x3F); // ditto
                reg40op1 = (byte) ((reg40op1 & 0xC0) | volumeOp1); // keep "scaling level" and merge in our volume
                reg40op2 = (byte) ((reg40op2 & 0xC0) | volumeOp2);

                SetRegister(0x40 + op1Reg, reg40op1);
                SetRegister(0x40 + op2Reg, reg40op2);
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_60))
            {
                // Attack Rate / Decay Rate
                // Sustain Level / Release Rate
                byte reg60op1 = instrumentPtr.Value.reg60op1;
                byte reg60op2 = instrumentPtr.Value.reg60op2;
                byte reg80op1 = instrumentPtr.Value.reg80op1;
                byte reg80op2 = instrumentPtr.Value.reg80op2;

                SetRegister(0x60 + op1Reg, reg60op1);
                SetRegister(0x60 + op2Reg, reg60op2);
                SetRegister(0x80 + op1Reg, reg80op1);
                SetRegister(0x80 + op2Reg, reg80op2);
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_E0))
            {
                // Waveform Select
                byte regE0op1 = instrumentPtr.Value.regE0op1;
                byte regE0op2 = instrumentPtr.Value.regE0op2;

                SetRegister(0xE0 + op1Reg, regE0op1);
                SetRegister(0xE0 + op2Reg, regE0op2);
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_C0))
            {
                // Feedback / Algorithm
                byte regC0 = instrumentPtr.Value.regC0;

                if (_modeOPL3)
                {
                    // Panning for OPL3
                    byte panning = _midiChannels[midiChannel].currentPanning;

                    if (panning <= MILES_ADLIB_STEREO_PANNING_THRESHOLD_LEFT)
                    {
                        regC0 |= 0x20; // left speaker only
                    }
                    else if (panning >= MILES_ADLIB_STEREO_PANNING_THRESHOLD_RIGHT)
                    {
                        regC0 |= 0x10; // right speaker only
                    }
                    else
                    {
                        regC0 |= 0x30; // center
                    }
                }

                SetRegister(0xC0 + channelReg, regC0);
            }

            if (registerUpdateFlags.HasFlag(kMilesAdLibUpdateFlags.kMilesAdLibUpdateFlags_Reg_A0))
            {
                // Frequency / Key-On
                // Octave / F-Number / Key-On
                if (!keyOn)
                {
                    // turn off note
                    byte regB0 = (byte) (_physicalFmVoices[physicalFmVoice].currentB0hReg & 0x1F);
                    // remove bit 5 "key on"
                    SetRegister(0xB0 + channelReg, regB0);
                }
                else
                {
                    // turn on note, calculate frequency, octave...
                    short pitchBender = (short) _midiChannels[midiChannel].currentPitchBender;
                    byte pitchRange = _midiChannels[midiChannel].currentPitchRange;
                    short currentNote = _virtualFmVoices[virtualFmVoice].currentNote;
                    short physicalNote = 0;
                    short halfTone = 0;
                    ushort frequency = 0;
                    ushort frequencyIdx = 0;
                    byte octave = 0;

                    pitchBender -= 0x2000;
                    pitchBender = (short) (pitchBender >> 5); // divide by 32
                    pitchBender = (short) (pitchBender * pitchRange); // pitchrange 12: now +0x0C00 to -0xC00
                    // difference between Miles Audio 2 + 3
                    // Miles Audio 2 used a pitch range of 12, which was basically hardcoded
                    // Miles Audio 3 used an array, which got set by control change events

                    currentNote += _virtualFmVoices[0].currentTransposition;

                    // Normalize note
                    currentNote -= 24;
                    do
                    {
                        currentNote += 12;
                    } while (currentNote < 0);
                    currentNote += 12;

                    do
                    {
                        currentNote -= 12;
                    } while (currentNote > 95);

                    // combine note + pitchbender, also adjust by 8 for rounding
                    currentNote = (short) ((currentNote << 8) + pitchBender + 8);

                    currentNote = (short) (currentNote >> 4); // get actual note

                    // Normalize
                    currentNote -= (12 * 16);
                    do
                    {
                        currentNote += (12 * 16);
                    } while (currentNote < 0);

                    currentNote += (12 * 16);
                    do
                    {
                        currentNote -= (12 * 16);
                    } while (currentNote > ((96 * 16) - 1));

                    physicalNote = (short) (currentNote >> 4);

                    halfTone = (short) (physicalNote % 12); // remainder of physicalNote / 12

                    frequencyIdx = (ushort) ((halfTone << 4) + (currentNote & 0x0F));
                    System.Diagnostics.Debug.Assert(frequencyIdx < milesAdLibFrequencyLookUpTable.Length);
                    frequency = milesAdLibFrequencyLookUpTable[frequencyIdx];

                    octave = (byte) ((physicalNote / 12) - 1);

                    if ((frequency & 0x8000) != 0)
                        octave++;

                    if ((octave & 0x80) != 0)
                    {
                        octave++;
                        frequency = (ushort) (frequency >> 1);
                    }

                    byte regA0 = (byte) (frequency & 0xFF);
                    byte regB0 = (byte) (((frequency >> 8) & 0x03) | (octave << 2) | 0x20);

                    SetRegister(0xA0 + channelReg, regA0);
                    SetRegister(0xB0 + channelReg, regB0);

                    _physicalFmVoices[physicalFmVoice].currentB0hReg = regB0;
                }
            }

            //warning("end of update voice");
        }
    }
}