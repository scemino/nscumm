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
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    public class MilesMT32InstrumentEntry
    {
        public byte bankId;
        public byte patchId;
        public readonly byte[] commonParameter;
        public readonly byte[][] partialParameters;

        public MilesMT32InstrumentEntry()
        {
            commonParameter = new byte[MidiDriverMilesMt32.MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE + 1];
            for (int i = 0; i < MidiDriverMilesMt32.MILES_MT32_PATCHDATA_PARTIALPARAMETERS_COUNT; i++)
            {
                partialParameters[i] = new byte[MidiDriverMilesMt32.MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE + 1];
            }
        }
    }

    public class MidiDriverMilesMt32 : MidiDriver
    {
        // Miles Audio supported controllers for control change messages
        private const int MILES_CONTROLLER_SELECT_PATCH_BANK = 114;
        private const int MILES_CONTROLLER_PROTECT_VOICE = 112;
        private const int MILES_CONTROLLER_PROTECT_TIMBRE = 113;
        private const int MILES_CONTROLLER_MODULATION = 1;
        private const int MILES_CONTROLLER_VOLUME = 7;
        private const int MILES_CONTROLLER_EXPRESSION = 11;
        private const int MILES_CONTROLLER_PANNING = 10;
        private const int MILES_CONTROLLER_SUSTAIN = 64;
        private const int MILES_CONTROLLER_PITCH_RANGE = 6;
        private const int MILES_CONTROLLER_RESET_ALL = 121;
        private const int MILES_CONTROLLER_ALL_NOTES_OFF = 123;
        private const int MILES_CONTROLLER_PATCH_REVERB = 59;
        private const int MILES_CONTROLLER_PATCH_BENDER = 60;
        private const int MILES_CONTROLLER_REVERB_MODE = 61;
        private const int MILES_CONTROLLER_REVERB_TIME = 62;
        private const int MILES_CONTROLLER_REVERB_LEVEL = 63;
        private const int MILES_CONTROLLER_RHYTHM_KEY_TIMBRE = 58;

        // 3 SysEx controllers, each range 5
        // 32-36 for 1st queue
        // 37-41 for 2nd queue
        // 42-46 for 3rd queue
        private const int MILES_CONTROLLER_SYSEX_RANGE_BEGIN = 32;
        private const int MILES_CONTROLLER_SYSEX_RANGE_END = 46;

        private const int MILES_CONTROLLER_SYSEX_QUEUE_COUNT = 3;
        private const int MILES_CONTROLLER_SYSEX_QUEUE_SIZE = 32;

        private const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS1 = 0;
        private const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS2 = 1;
        private const int MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS3 = 2;
        private const int MILES_CONTROLLER_SYSEX_COMMAND_DATA = 3;
        private const int MILES_CONTROLLER_SYSEX_COMMAND_SEND = 4;

        private const int MILES_CONTROLLER_XMIDI_RANGE_BEGIN = 110;
        private const int MILES_CONTROLLER_XMIDI_RANGE_END = 120;

        private const int MILES_MIDI_CHANNEL_COUNT = 16;

        private const int MILES_MT32_PATCHES_COUNT = 128;
        private const int MILES_MT32_CUSTOMTIMBRE_COUNT = 64;

        private const int MILES_MT32_TIMBREBANK_STANDARD_ROLAND = 0;
        private const int MILES_MT32_TIMBREBANK_MELODIC_MODULE = 127;

        public const int MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE = 14;
        public const int MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE = 58;
        public const int MILES_MT32_PATCHDATA_PARTIALPARAMETERS_COUNT = 4;

        private const int MILES_MT32_PATCHDATA_TOTAL_SIZE =
            MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE +
            (MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE * MILES_MT32_PATCHDATA_PARTIALPARAMETERS_COUNT);

        private const int MILES_MT32_SYSEX_TERMINATOR = 0xFF;

        private static readonly byte[] milesMT32SysExResetParameters =
        {
            0x01, MILES_MT32_SYSEX_TERMINATOR
        };

        private static readonly byte[] milesMT32SysExChansSetup =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, MILES_MT32_SYSEX_TERMINATOR
        };

        private static readonly byte[] milesMT32SysExPartialReserveTable =
        {
            0x03, 0x04, 0x03, 0x04, 0x03, 0x04, 0x03, 0x04, 0x04, MILES_MT32_SYSEX_TERMINATOR
        };

        private static readonly byte[] milesMT32SysExInitReverb =
        {
            0x00, 0x03, 0x02, MILES_MT32_SYSEX_TERMINATOR // Reverb mode 0, reverb time 3, reverb level 2
        };

        class MidiChannelEntry
        {
            public byte currentPatchBank;
            public byte currentPatchId;

            public bool usingCustomTimbre;
            public byte currentCustomTimbreId;
        }

        class MidiCustomTimbreEntry
        {
            public bool used;
            public bool protectionEnabled;
            public byte currentPatchBank;
            public byte currentPatchId;

            public uint lastUsedNoteCounter;
        }

        class MilesMT32SysExQueueEntry
        {
            public uint targetAddress;
            public byte dataPos;
            public byte[] data = new byte[MILES_CONTROLLER_SYSEX_QUEUE_SIZE + 1]; // 1 extra byte for terminator
        }

        private object _mutex;
        private MidiDriver _driver;
        private bool _MT32;
        private bool _nativeMT32;

        private bool _isOpen;
        private int _baseFreq;


        // stores information about all MIDI channels
        MidiChannelEntry[] _midiChannels = ScummHelper.CreateArray<MidiChannelEntry>(MILES_MIDI_CHANNEL_COUNT);

        // stores information about all custom timbres
        MidiCustomTimbreEntry[] _customTimbres =
            ScummHelper.CreateArray<MidiCustomTimbreEntry>(MILES_MT32_CUSTOMTIMBRE_COUNT);

        byte[] _patchesBank = new byte[MILES_MT32_PATCHES_COUNT];

        // holds all instruments
        private Ptr<MilesMT32InstrumentEntry> _instrumentTablePtr;
        private ushort _instrumentTableCount;

        int _noteCounter; // used to figure out, which timbres are outdated

        // SysEx Queues
        MilesMT32SysExQueueEntry[] _sysExQueues =
            ScummHelper.CreateArray<MilesMT32SysExQueueEntry>(MILES_CONTROLLER_SYSEX_QUEUE_COUNT);

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

        public MidiDriverMilesMt32(Ptr<MilesMT32InstrumentEntry> instrumentTablePtr, ushort instrumentTableCount)
        {
            _instrumentTablePtr = instrumentTablePtr;
            _instrumentTableCount = instrumentTableCount;

            _baseFreq = 250;
        }

        public override MidiDriverError Open()
        {
            System.Diagnostics.Debug.Assert(_driver == null);

            // Setup midi driver
            var dev = DetectDevice(MusicDriverTypes.Midi | MusicDriverTypes.PreferMt32);
            var musicType = GetMusicType(dev);

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

            if (!_nativeMT32)
            {
                Error("MILES-MT32: non-mt32 currently not supported!");
            }

            _driver = (MidiDriver) CreateMidi(Engine.Instance.Mixer, dev);
            if (_driver == null)
                return (MidiDriverError) 255;

            if (_nativeMT32)
                _driver.Property(PROP_CHANNEL_MASK, 0x03FE);

            var ret = _driver.Open();
            if (ret != 0)
                return ret;

            if (_nativeMT32)
            {
                _driver.SendMt32Reset();

                ResetMt32();
            }

            return 0;
        }

        // // MIDI messages can be found at http://www.midi.org/techspecs/midimessages.php
        public override void Send(int b)
        {
            byte command = (byte) (b & 0xf0);
            byte midiChannel = (byte) (b & 0xf);
            byte op1 = (byte) ((b >> 8) & 0xff);
            byte op2 = (byte) ((b >> 16) & 0xff);

            switch (command)
            {
                case 0x80: // note off
                case 0x90: // note on
                case 0xa0: // Polyphonic key pressure (aftertouch)
                case 0xd0: // Channel pressure (aftertouch)
                case 0xe0: // pitch bend change
                    _noteCounter++;
                    if (_midiChannels[midiChannel].usingCustomTimbre)
                    {
                        // Remember that this timbre got used now
                        _customTimbres[_midiChannels[midiChannel].currentCustomTimbreId].lastUsedNoteCounter =
                            (uint) _noteCounter;
                    }
                    _driver.Send(b);
                    break;
                case 0xb0: // Control change
                    ControlChange(midiChannel, op1, op2);
                    break;
                case 0xc0: // Program Change
                    ProgramChange(midiChannel, op1);
                    break;
                case 0xf0: // SysEx
                    Warning("MILES-MT32: SysEx: {0:X}", b);
                    break;
                default:
                    Warning("MILES-MT32: Unknown event {0:X2}", command);
                    break;
            }
        }

        public override MidiChannel AllocateChannel()
        {
            return _driver?.AllocateChannel();
        }

        public override MidiChannel GetPercussionChannel()
        {
            return _driver?.GetPercussionChannel();
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _driver?.SetTimerCallback(timerParam, timerProc);
        }

        private void ResetMt32()
        {
            // reset all internal parameters / patches
            MT32SysEx(0x7F0000, milesMT32SysExResetParameters);

            // init part/channel assignments
            MT32SysEx(0x10000D, milesMT32SysExChansSetup);

            // partial reserve table
            MT32SysEx(0x100004, milesMT32SysExPartialReserveTable);

            // init reverb
            MT32SysEx(0x100001, milesMT32SysExInitReverb);
        }

        private void MT32SysEx(int targetAddress, BytePtr dataPtr)
        {
            byte[] sysExMessage = new byte[270];

            sysExMessage[0] = 0x41; // Roland
            sysExMessage[1] = 0x10;
            sysExMessage[2] = 0x16; // Model MT32
            sysExMessage[3] = 0x12; // Command DT1

            ushort sysExChecksum = 0;

            sysExMessage[4] = (byte) ((targetAddress >> 16) & 0xFF);
            sysExMessage[5] = (byte) ((targetAddress >> 8) & 0xFF);
            sysExMessage[6] = (byte) (targetAddress & 0xFF);

            for (byte targetAddressByte = 4; targetAddressByte < 7; targetAddressByte++)
            {
                System.Diagnostics.Debug.Assert(sysExMessage[targetAddressByte] < 0x80); // security check
                sysExChecksum -= sysExMessage[targetAddressByte];
            }

            ushort sysExPos = 7;
            while (true)
            {
                var sysExByte = dataPtr.Value;
                dataPtr.Offset++;
                if (sysExByte == MILES_MT32_SYSEX_TERMINATOR)
                    break; // Message done

                System.Diagnostics.Debug.Assert(sysExPos < sysExMessage.Length);
                System.Diagnostics.Debug.Assert(sysExByte < 0x80); // security check
                sysExMessage[sysExPos++] = sysExByte;
                sysExChecksum -= sysExByte;
            }

            // Calculate checksum
            System.Diagnostics.Debug.Assert(sysExPos < sysExMessage.Length);
            sysExMessage[sysExPos++] = (byte) (sysExChecksum & 0x7f);

            // Send SysEx
            _driver.SysEx(sysExMessage, sysExPos);

            // Wait the time it takes to send the SysEx data
            int delay = (sysExPos + 2) * 1000 / 3125;

            // Plus an additional delay for the MT-32 rev00
            if (_nativeMT32)
                delay += 40;

            ServiceLocator.Platform.Sleep(delay);
        }

        private void ControlChange(byte midiChannel, byte controllerNumber, byte controllerValue)
        {
            byte channelPatchId;

            switch (controllerNumber)
            {
                case MILES_CONTROLLER_SELECT_PATCH_BANK:
                    _midiChannels[midiChannel].currentPatchBank = controllerValue;
                    return;

                case MILES_CONTROLLER_PATCH_REVERB:
                    channelPatchId = _midiChannels[midiChannel].currentPatchId;

                    WritePatchByte(channelPatchId, 6, controllerValue);
                    _driver.Send(0xC0 | midiChannel | (channelPatchId << 8)); // execute program change
                    return;

                case MILES_CONTROLLER_PATCH_BENDER:
                    channelPatchId = _midiChannels[midiChannel].currentPatchId;

                    WritePatchByte(channelPatchId, 4, controllerValue);
                    _driver.Send(0xC0 | midiChannel | (channelPatchId << 8)); // execute program change
                    return;

                case MILES_CONTROLLER_REVERB_MODE:
                    WriteToSystemArea(1, controllerValue);
                    return;

                case MILES_CONTROLLER_REVERB_TIME:
                    WriteToSystemArea(2, controllerValue);
                    return;

                case MILES_CONTROLLER_REVERB_LEVEL:
                    WriteToSystemArea(3, controllerValue);
                    return;

                case MILES_CONTROLLER_RHYTHM_KEY_TIMBRE:
                    if (_midiChannels[midiChannel].usingCustomTimbre)
                    {
                        // custom timbre is set on current channel
                        WriteRhythmSetup(controllerValue, _midiChannels[midiChannel].currentCustomTimbreId);
                    }
                    return;

                case MILES_CONTROLLER_PROTECT_TIMBRE:
                    if (_midiChannels[midiChannel].usingCustomTimbre)
                    {
                        // custom timbre set on current channel
                        var channelCustomTimbreId = _midiChannels[midiChannel].currentCustomTimbreId;
                        if (controllerValue >= 64)
                        {
                            // enable protection
                            _customTimbres[channelCustomTimbreId].protectionEnabled = true;
                        }
                        else
                        {
                            // disable protection
                            _customTimbres[channelCustomTimbreId].protectionEnabled = false;
                        }
                    }
                    return;
            }

            if ((controllerNumber >= MILES_CONTROLLER_SYSEX_RANGE_BEGIN) &&
                (controllerNumber <= MILES_CONTROLLER_SYSEX_RANGE_END))
            {
                // send SysEx
                byte sysExQueueNr = 0;

                // figure out which queue is accessed
                controllerNumber -= MILES_CONTROLLER_SYSEX_RANGE_BEGIN;
                while (controllerNumber > MILES_CONTROLLER_SYSEX_COMMAND_SEND)
                {
                    sysExQueueNr++;
                    controllerNumber -= (MILES_CONTROLLER_SYSEX_COMMAND_SEND + 1);
                }
                System.Diagnostics.Debug.Assert(sysExQueueNr < MILES_CONTROLLER_SYSEX_QUEUE_COUNT);

                byte sysExPos = _sysExQueues[sysExQueueNr].dataPos;
                bool sysExSend = false;

                switch (controllerNumber)
                {
                    case MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS1:
                        _sysExQueues[sysExQueueNr].targetAddress &= 0x00FFFF;
                        _sysExQueues[sysExQueueNr].targetAddress =
                            (uint) (_sysExQueues[sysExQueueNr].targetAddress | (controllerValue << 16));
                        break;
                    case MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS2:
                        _sysExQueues[sysExQueueNr].targetAddress &= 0xFF00FF;
                        _sysExQueues[sysExQueueNr].targetAddress =
                            (uint) (_sysExQueues[sysExQueueNr].targetAddress | (controllerValue << 8));
                        break;
                    case MILES_CONTROLLER_SYSEX_COMMAND_ADDRESS3:
                        _sysExQueues[sysExQueueNr].targetAddress &= 0xFFFF00;
                        _sysExQueues[sysExQueueNr].targetAddress |= controllerValue;
                        break;
                    case MILES_CONTROLLER_SYSEX_COMMAND_DATA:
                        if (sysExPos < MILES_CONTROLLER_SYSEX_QUEUE_SIZE)
                        {
                            // Space left? put current byte into queue
                            _sysExQueues[sysExQueueNr].data[sysExPos] = controllerValue;
                            sysExPos++;
                            _sysExQueues[sysExQueueNr].dataPos = sysExPos;
                            if (sysExPos >= MILES_CONTROLLER_SYSEX_QUEUE_SIZE)
                            {
                                // overflow? . send it now
                                sysExSend = true;
                            }
                        }
                        break;
                    case MILES_CONTROLLER_SYSEX_COMMAND_SEND:
                        sysExSend = true;
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }

                if (sysExSend)
                {
                    if (sysExPos > 0)
                    {
                        // data actually available? . send it
                        _sysExQueues[sysExQueueNr].data[sysExPos] = MILES_MT32_SYSEX_TERMINATOR; // put terminator

                        // Execute SysEx
                        MT32SysEx((int) _sysExQueues[sysExQueueNr].targetAddress, _sysExQueues[sysExQueueNr].data);

                        // adjust target address to point at the end of the current data
                        _sysExQueues[sysExQueueNr].targetAddress += sysExPos;
                        // reset queue data buffer
                        _sysExQueues[sysExQueueNr].dataPos = 0;
                    }
                }
                return;
            }

            if ((controllerNumber >= MILES_CONTROLLER_XMIDI_RANGE_BEGIN) &&
                (controllerNumber <= MILES_CONTROLLER_XMIDI_RANGE_END))
            {
                // XMIDI controllers? ignore those
                return;
            }

            _driver.Send(0xB0 | midiChannel | (controllerNumber << 8) | (controllerValue << 16));
        }

        private void ProgramChange(byte midiChannel, byte patchId)
        {
            byte channelPatchBank = _midiChannels[midiChannel].currentPatchBank;
            byte activePatchBank = _patchesBank[patchId];

            //warning("patch channel %d, patch %x, bank %x", midiChannel, patchId, channelPatchBank);

            // remember patch id for the current MIDI-channel
            _midiChannels[midiChannel].currentPatchId = patchId;

            if (channelPatchBank != activePatchBank)
            {
                // associate patch with timbre
                SetupPatch(channelPatchBank, patchId);
            }

            // If this is a custom patch, remember customTimbreId
            short customTimbre = SearchCustomTimbre(channelPatchBank, patchId);
            if (customTimbre >= 0)
            {
                _midiChannels[midiChannel].usingCustomTimbre = true;
                _midiChannels[midiChannel].currentCustomTimbreId = (byte) customTimbre;
            }
            else
            {
                _midiChannels[midiChannel].usingCustomTimbre = false;
            }

            // Finally send program change to MT32
            _driver.Send(0xC0 | midiChannel | (patchId << 8));
        }

        private short SearchCustomTimbre(byte patchBank, byte patchId)
        {
            byte customTimbreId = 0;

            for (customTimbreId = 0; customTimbreId < MILES_MT32_CUSTOMTIMBRE_COUNT; customTimbreId++)
            {
                if (_customTimbres[customTimbreId].used)
                {
                    if ((_customTimbres[customTimbreId].currentPatchBank == patchBank) &&
                        (_customTimbres[customTimbreId].currentPatchId == patchId))
                    {
                        return customTimbreId;
                    }
                }
            }
            return -1;
        }

        private Ptr<MilesMT32InstrumentEntry> SearchCustomInstrument(byte patchBank, byte patchId)
        {
            var instrumentPtr = _instrumentTablePtr;

            for (var instrumentNr = 0; instrumentNr < _instrumentTableCount; instrumentNr++)
            {
                if ((instrumentPtr.Value.bankId == patchBank) && (instrumentPtr.Value.patchId == patchId))
                    return instrumentPtr;
                instrumentPtr.Offset++;
            }
            return null;
        }

        private void SetupPatch(byte patchBank, byte patchId)
        {
            _patchesBank[patchId] = patchBank;

            if (patchBank != 0)
            {
                // non-built-in bank
                short customTimbreId = SearchCustomTimbre(patchBank, patchId);
                if (customTimbreId >= 0)
                {
                    // now available? . use this timbre
                    WritePatchTimbre(patchId, 2, (byte) customTimbreId); // Group MEMORY
                    return;
                }
            }

            // for built-in bank (or timbres, that are not available) use default MT32 timbres
            byte timbreId = (byte) (patchId & 0x3F);
            if ((patchId & 0x40) == 0)
            {
                WritePatchTimbre(patchId, 0, timbreId); // Group A
            }
            else
            {
                WritePatchTimbre(patchId, 1, timbreId); // Group B
            }
        }

        private void ProcessXMIDITimbreChunk(BytePtr timbreListPtr, int timbreListSize)
        {
            ushort timbreCount = 0;
            int expectedSize = 0;
            var timbreListSeeker = timbreListPtr;

            if (timbreListSize < 2)
            {
                Warning("MILES-MT32: XMIDI-TIMB chunk - not enough bytes in chunk");
                return;
            }

            timbreCount = timbreListPtr.ToUInt16();
            expectedSize = timbreCount * 2;
            if (expectedSize > timbreListSize)
            {
                Warning("MILES-MT32: XMIDI-TIMB chunk - size mismatch");
                return;
            }

            timbreListSeeker += 2;

            while (timbreCount != 0)
            {
                byte patchId = timbreListSeeker.Value;
                timbreListSeeker.Offset++;
                byte patchBank = timbreListSeeker.Value;
                timbreListSeeker.Offset++;

                switch (patchBank)
                {
                    case MILES_MT32_TIMBREBANK_STANDARD_ROLAND:
                    case MILES_MT32_TIMBREBANK_MELODIC_MODULE:
                        // ignore those 2 banks
                        break;

                    default:
                        // Check, if this timbre was already loaded
                        var customTimbreId = SearchCustomTimbre(patchBank, patchId);

                        if (customTimbreId < 0)
                        {
                            // currently not loaded, try to install it
                            InstallCustomTimbre(patchBank, patchId);
                        }
                        break;
                }
                timbreCount--;
            }
        }

        //
        private short InstallCustomTimbre(byte patchBank, byte patchId)
        {
            switch (patchBank)
            {
                case MILES_MT32_TIMBREBANK_STANDARD_ROLAND: // Standard Roland MT32 bank
                case MILES_MT32_TIMBREBANK_MELODIC_MODULE: // Reserved for melodic mode
                    return -1;
            }

            // Original driver did a search for custom timbre here
            // and in case it was found, it would call setup_patch()
            // we are called from within setup_patch(), so this isn't needed

            short customTimbreId = -1;
            short leastUsedTimbreId = -1;
            int leastUsedTimbreNoteCounter = _noteCounter;

            // Check, if requested instrument is actually available
            var instrumentPtr = SearchCustomInstrument(patchBank, patchId);
            if (instrumentPtr == Ptr<MilesMT32InstrumentEntry>.Null)
            {
                Warning("MILES-MT32: instrument not found during installCustomTimbre()");
                return -1; // not found . bail out
            }

            // Look for an empty timbre slot
            // or get the least used non-protected slot
            for (byte customTimbreNr = 0; customTimbreNr < MILES_MT32_CUSTOMTIMBRE_COUNT; customTimbreNr++)
            {
                if (!_customTimbres[customTimbreNr].used)
                {
                    // found an empty slot . use this one
                    customTimbreId = customTimbreNr;
                    break;
                }
                else
                {
                    // used slot
                    if (!_customTimbres[customTimbreNr].protectionEnabled)
                    {
                        // not protected
                        int customTimbreNoteCounter = (int) _customTimbres[customTimbreNr].lastUsedNoteCounter;
                        if (customTimbreNoteCounter < leastUsedTimbreNoteCounter)
                        {
                            leastUsedTimbreId = customTimbreNr;
                            leastUsedTimbreNoteCounter = customTimbreNoteCounter;
                        }
                    }
                }
            }

            if (customTimbreId < 0)
            {
                // no empty slot found, check if we got a least used non-protected slot
                if (leastUsedTimbreId < 0)
                {
                    // everything is protected, bail out
                    Warning("MILES-MT32: no non-protected timbre slots available during installCustomTimbre()");
                    return -1;
                }
                customTimbreId = leastUsedTimbreId;
            }

            // setup timbre slot
            _customTimbres[customTimbreId].used = true;
            _customTimbres[customTimbreId].currentPatchBank = patchBank;
            _customTimbres[customTimbreId].currentPatchId = patchId;
            _customTimbres[customTimbreId].lastUsedNoteCounter = (uint) _noteCounter;
            _customTimbres[customTimbreId].protectionEnabled = false;

            int targetAddress = 0x080000 | (customTimbreId << 9);
            int targetAddressCommon = targetAddress + 0x000000;
            int targetAddressPartial1 = targetAddress + 0x00000E;
            int targetAddressPartial2 = targetAddress + 0x000048;
            int targetAddressPartial3 = targetAddress + 0x000102;
            int targetAddressPartial4 = targetAddress + 0x00013C;

#if Undefined
	byte parameterData[MILES_MT32_PATCHDATA_TOTAL_SIZE + 1];
	ushort parameterDataPos = 0;

	memcpy(parameterData, instrumentPtr.commonParameter, MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE);
	parameterDataPos += MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE;
	memcpy(parameterData + parameterDataPos, instrumentPtr.partialParameters[0], MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE);
	parameterDataPos += MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE;
	memcpy(parameterData + parameterDataPos, instrumentPtr.partialParameters[1], MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE);
	parameterDataPos += MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE;
	memcpy(parameterData + parameterDataPos, instrumentPtr.partialParameters[2], MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE);
	parameterDataPos += MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE;
	memcpy(parameterData + parameterDataPos, instrumentPtr.partialParameters[3], MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE);
	parameterDataPos += MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE;
	parameterData[parameterDataPos] = MILES_MT32_SYSEX_TERMINATOR;

	MT32SysEx(targetAddressCommon, parameterData);
#endif

            // upload common parameter data
            MT32SysEx(targetAddressCommon, instrumentPtr.Value.commonParameter);
            // upload partial parameter data
            MT32SysEx(targetAddressPartial1, instrumentPtr.Value.partialParameters[0]);
            MT32SysEx(targetAddressPartial2, instrumentPtr.Value.partialParameters[1]);
            MT32SysEx(targetAddressPartial3, instrumentPtr.Value.partialParameters[2]);
            MT32SysEx(targetAddressPartial4, instrumentPtr.Value.partialParameters[3]);

            SetupPatch(patchBank, patchId);

            return customTimbreId;
        }

        private int CalculateSysExTargetAddress(int baseAddress, int index)
        {
            ushort targetAddressLSB = (ushort) (baseAddress & 0xFF);
            ushort targetAddressKSB = (ushort) ((baseAddress >> 8) & 0xFF);
            ushort targetAddressMSB = (ushort) ((baseAddress >> 16) & 0xFF);

            // add index to it, but use 7-bit of the index for each byte
            targetAddressLSB = (ushort) (targetAddressLSB + (index & 0x7F));
            targetAddressKSB = (ushort) (targetAddressKSB + ((index >> 7) & 0x7F));
            targetAddressMSB = (ushort) (targetAddressMSB + ((index >> 14) & 0x7F));

            // adjust bytes, so that none of them is above or equal 0x80
            while (targetAddressLSB >= 0x80)
            {
                targetAddressLSB -= 0x80;
                targetAddressKSB++;
            }
            while (targetAddressKSB >= 0x80)
            {
                targetAddressKSB -= 0x80;
                targetAddressMSB++;
            }
            System.Diagnostics.Debug.Assert(targetAddressMSB < 0x80);

            // put everything together
            return targetAddressLSB | (targetAddressKSB << 8) | (targetAddressMSB << 16);
        }

        private void WriteRhythmSetup(byte note, byte customTimbreId)
        {
            byte[] sysExData = new byte[2];

            int targetAddress = CalculateSysExTargetAddress(0x030110, (note - 24) << 2);

            sysExData[0] = customTimbreId;
            sysExData[1] = MILES_MT32_SYSEX_TERMINATOR; // terminator

            MT32SysEx(targetAddress, sysExData);
        }

        private void WritePatchTimbre(byte patchId, byte timbreGroup, byte timbreId)
        {
            byte[] sysExData = new byte[3];

            // write to patch memory (starts at 0x050000, each entry is 8 bytes)
            var targetAddress = CalculateSysExTargetAddress(0x050000, patchId << 3);

            sysExData[0] = timbreGroup; // 0 - group A, 1 - group B, 2 - memory, 3 - rhythm
            sysExData[1] = timbreId; // timbre number (0-63)
            sysExData[2] = MILES_MT32_SYSEX_TERMINATOR; // terminator

            MT32SysEx(targetAddress, sysExData);
        }

        private void WritePatchByte(byte patchId, byte index, byte patchValue)
        {
            byte[] sysExData = new byte[2];

            var targetAddress = CalculateSysExTargetAddress(0x050000, (patchId << 3) + index);

            sysExData[0] = patchValue;
            sysExData[1] = MILES_MT32_SYSEX_TERMINATOR; // terminator

            MT32SysEx(targetAddress, sysExData);
        }

        private void WriteToSystemArea(byte index, byte value)
        {
            byte[] sysExData = new byte[2];

            var targetAddress = CalculateSysExTargetAddress(0x100000, index);

            sysExData[0] = value;
            sysExData[1] = MILES_MT32_SYSEX_TERMINATOR; // terminator

            MT32SysEx(targetAddress, sysExData);
        }

        public static MidiDriver Create(string instrumentDataFilename)
        {
            ushort instrumentTableCount = 0;

            if (!string.IsNullOrEmpty(instrumentDataFilename))
            {
                // Load MT32 instrument data from file SAMPLE.MT
                int fileDataOffset;
                int fileDataLeft;

                byte curBankId;
                byte curPatchId;

                var fileStream = Engine.OpenFileRead(instrumentDataFilename);
                if (fileStream == null)
                    Error("MILES-MT32: could not open instrument file '{0}'", instrumentDataFilename);

                int fileSize = (int) fileStream.Length;

                var fileDataPtr = new byte[fileSize];

                if (fileStream.Read(fileDataPtr, 0, fileSize) != fileSize)
                    Error("MILES-MT32: error while reading instrument file");
                fileStream.Dispose();

                // File is like this:
                // [patch:BYTE] [bank:BYTE] [patchoffset:UINT32]
                // ...
                // until patch + bank are both 0xFF, which signals end of header

                // First we check how many entries there are
                fileDataOffset = 0;
                fileDataLeft = fileSize;
                while (true)
                {
                    if (fileDataLeft < 6)
                        Error("MILES-MT32: unexpected EOF in instrument file");

                    curPatchId = fileDataPtr[fileDataOffset++];
                    curBankId = fileDataPtr[fileDataOffset++];

                    if ((curBankId == 0xFF) && (curPatchId == 0xFF))
                        break;

                    fileDataOffset += 4; // skip over offset
                    instrumentTableCount++;
                }

                if (instrumentTableCount == 0)
                    Error("MILES-MT32: no instruments in instrument file");

                // Allocate space for instruments
                var instrumentTablePtr = new MilesMT32InstrumentEntry[instrumentTableCount];

                // Now actually read all entries

                fileDataOffset = 0;
                foreach (var instrumentPtr in instrumentTablePtr)
                {
                    curPatchId = fileDataPtr[fileDataOffset++];
                    curBankId = fileDataPtr[fileDataOffset++];

                    if ((curBankId == 0xFF) && (curPatchId == 0xFF))
                        break;

                    int instrumentOffset = fileDataPtr.ToInt32(fileDataOffset);
                    fileDataOffset += 4;

                    instrumentPtr.bankId = curBankId;
                    instrumentPtr.patchId = curPatchId;

                    ushort instrumentDataSize = fileDataPtr.ToUInt16(instrumentOffset);
                    if (instrumentDataSize != (MILES_MT32_PATCHDATA_TOTAL_SIZE + 2))
                        Error("MILES-MT32: unsupported instrument size");

                    instrumentOffset += 2;
                    // Copy common parameter data
                    Array.Copy(fileDataPtr, instrumentOffset, instrumentPtr.commonParameter, 0,
                        MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE);
                    instrumentPtr.commonParameter[MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE] =
                        MILES_MT32_SYSEX_TERMINATOR; // Terminator
                    instrumentOffset += MILES_MT32_PATCHDATA_COMMONPARAMETER_SIZE;

                    // Copy partial parameter data
                    for (byte partialNr = 0; partialNr < MILES_MT32_PATCHDATA_PARTIALPARAMETERS_COUNT; partialNr++)
                    {
                        Array.Copy(fileDataPtr, instrumentOffset, instrumentPtr.partialParameters, partialNr,
                            MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE);
                        instrumentPtr.partialParameters[partialNr][MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE] =
                            MILES_MT32_SYSEX_TERMINATOR; // Terminator
                        instrumentOffset += MILES_MT32_PATCHDATA_PARTIALPARAMETER_SIZE;
                    }

                    // Instrument read, next instrument please
                }

                return new MidiDriverMilesMt32(instrumentTablePtr, instrumentTableCount);
            }
            return null;
        }

        private void MidiDriver_Miles_MT32_processXMIDITimbreChunk(MidiDriver driver, BytePtr timbreListPtr,
            int timbreListSize)
        {
            var driverMT32 = (MidiDriverMilesMt32) driver;

            driverMT32?.ProcessXMIDITimbreChunk(timbreListPtr, timbreListSize);
        }
    }
}