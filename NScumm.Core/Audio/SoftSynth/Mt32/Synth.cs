//
//  Synth.cs
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

#define MT32EMU_MONITOR_SYSEX
#define MT32EMU_MONITOR_INIT
using System;

#if MT32EMU_USE_FLOAT_SAMPLES
using Sample = System.Single;
using SampleEx = System.Single;
#else
using Sample = System.Int16;
using SampleEx = System.Int32;
#endif
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class TimbreParam
    {
        public const int Size = CommonParam.Size + 4 * PartialParam.Size;

        public byte[] Data;
        public int Offset;

        public class CommonParam
        {
            public const int Size = 10 + 4;

            public byte[] Data;
            public int Offset;

            public readonly BytePtr name;

            // 1 & 2  0-12 (1-13)
            public byte partialStructure12
            {
                get { return Data[Offset + 10]; }
                set { Data[Offset + 10] = value; }
            }

            // 3 & 4  0-12 (1-13)
            public byte partialStructure34
            {
                get { return Data[Offset + 11]; }
                set { Data[Offset + 11] = value; }
            }

            // 0-15 (0000-1111)
            public byte partialMute
            {
                get { return Data[Offset + 12]; }
                set { Data[Offset + 12] = value; }
            }
            // ENV MODE 0-1 (Normal, No sustain)
            public byte noSustain
            {
                get { return Data[Offset + 13]; }
                set { Data[Offset + 13] = value; }
            }

            public CommonParam(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                name = new BytePtr(data, offset);
            }
        }

        public class PartialParam
        {
            public const int Size = WGParam.Size + PitchEnvParam.Size + PitchLFOParam.Size + TVFParam.Size + TVAParam.Size;

            public byte[] Data;
            public int Offset;

            public class WGParam
            {
                public const int Size = 8;

                public byte[] Data;
                public int Offset;

                // 0-96 (C1,C#1-C9)
                public byte pitchCoarse
                {
                    get { return Data[Offset]; }
                    set { Data[Offset] = value; }
                }

                // 0-100 (-50 to +50 (cents - confirmed by Mok))
                public byte pitchFine
                {
                    get { return Data[Offset + 1]; }
                    set { Data[Offset + 1] = value; }
                }

                // 0-16 (-1, -1/2, -1/4, 0, 1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8, 1, 5/4, 3/2, 2, s1, s2)
                public byte pitchKeyfollow
                {
                    get { return Data[Offset + 2]; }
                    set { Data[Offset + 2] = value; }
                }

                // 0-1 (OFF, ON)
                public byte pitchBenderEnabled
                {
                    get { return Data[Offset + 3]; }
                    set { Data[Offset + 3] = value; }
                }

                // MT-32: 0-1 (SQU/SAW); LAPC-I: WG WAVEFORM/PCM BANK 0 - 3 (SQU/1, SAW/1, SQU/2, SAW/2)
                public byte waveform
                {
                    get { return Data[Offset + 4]; }
                    set { Data[Offset + 4] = value; }
                }

                // 0-127 (1-128)
                public byte pcmWave
                {
                    get { return Data[Offset + 5]; }
                    set { Data[Offset + 5] = value; }
                }

                // 0-100
                public byte pulseWidth
                {
                    get { return Data[Offset + 6]; }
                    set { Data[Offset + 6] = value; }
                }

                // 0-14 (-7 - +7)
                public byte pulseWidthVeloSensitivity
                {
                    get { return Data[Offset + 7]; }
                    set { Data[Offset + 7] = value; }
                }

                public WGParam(byte[] data, int offset)
                {
                    Data = data;
                    Offset = offset;
                }
            }

            public class PitchEnvParam
            {
                public const int Size = 3 + 4 + 5;

                public byte[] Data;
                public int Offset;

                // 0-10
                public byte depth
                {
                    get { return Data[Offset]; }
                    set { Data[Offset] = value; }
                }

                // 0-100
                public byte veloSensitivity
                {
                    get { return Data[Offset + 1]; }
                    set { Data[Offset + 1] = value; }
                }

                // 0-4
                public byte timeKeyfollow
                {
                    get { return Data[Offset + 2]; }
                    set { Data[Offset + 2] = value; }
                }

                // 0-100
                public readonly BytePtr time; // [4]
                // 0-100 (-50 - +50) // [3]: SUSTAIN LEVEL, [4]: END LEVEL
                public readonly BytePtr level; // [5]

                public PitchEnvParam(byte[] data, int offset)
                {
                    Data = data;
                    Offset = offset;
                    time = new BytePtr(Data, Offset + 3);
                    level = new BytePtr(Data, Offset + 3 + 4);
                }
            }

            public class PitchLFOParam
            {
                public const int Size = 3;

                public byte[] Data;
                public int Offset;

                // 0-100
                public byte rate
                {
                    get { return Data[Offset]; }
                    set { Data[Offset] = value; }
                }

                // 0-100
                public byte depth
                {
                    get { return Data[Offset + 1]; }
                    set { Data[Offset + 1] = value; }
                }

                // 0-100
                public byte modSensitivity
                {
                    get { return Data[Offset + 2]; }
                    set { Data[Offset + 2] = value; }
                }

                public PitchLFOParam(byte[] data, int offset)
                {
                    Data = data;
                    Offset = offset;
                }
            }

            public class TVFParam
            {
                public const int Size = 9 + 5 + 4;

                public byte[] Data;
                public int Offset;

                // 0-100
                public byte cutoff
                {
                    get { return Data[Offset]; }
                    set { Data[Offset] = value; }
                }

                // 0-30
                public byte resonance
                {
                    get { return Data[Offset + 1]; }
                    set { Data[Offset + 1] = value; }
                }

                // -1, -1/2, -1/4, 0, 1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8, 1, 5/4, 3/2, 2
                public byte keyfollow
                {
                    get { return Data[Offset + 2]; }
                    set { Data[Offset + 2] = value; }
                }

                // 0-127 (<1A-<7C >1A-7C)
                public byte biasPoint
                {
                    get { return Data[Offset + 3]; }
                    set { Data[Offset + 3] = value; }
                }

                // 0-14 (-7 - +7)
                public byte biasLevel
                {
                    get { return Data[Offset + 4]; }
                    set { Data[Offset + 4] = value; }
                }

                // 0-100
                public byte envDepth
                {
                    get { return Data[Offset + 5]; }
                    set { Data[Offset + 5] = value; }
                }

                // 0-100
                public byte envVeloSensitivity
                {
                    get { return Data[Offset + 6]; }
                    set { Data[Offset + 6] = value; }
                }

                // DEPTH KEY FOLL0W 0-4
                public byte envDepthKeyfollow
                {
                    get { return Data[Offset + 7]; }
                    set { Data[Offset + 7] = value; }
                }

                // TIME KEY FOLLOW 0-4
                public byte envTimeKeyfollow
                {
                    get { return Data[Offset + 8]; }
                    set { Data[Offset + 8] = value; }
                }

                // 0-100
                public readonly BytePtr envTime;

                // 0-100 // [3]: SUSTAIN LEVEL
                public readonly BytePtr envLevel;

                public TVFParam(byte[] data, int offset)
                {
                    Data = data;
                    Offset = offset;
                    envTime = new BytePtr(Data, Offset + 9);
                    envLevel = new BytePtr(Data, Offset + 9 + 5);
                }
            }

            public class TVAParam
            {
                public const int Size = 8 + 5 + 4;

                public byte[] Data;
                public int Offset;

                // 0-100
                public byte level
                {
                    get { return Data[Offset]; }
                    set { Data[Offset] = value; }
                }

                // 0-100
                public byte veloSensitivity
                {
                    get { return Data[Offset + 1]; }
                    set { Data[Offset + 1] = value; }
                }

                // 0-127 (<1A-<7C >1A-7C)
                public byte biasPoint1
                {
                    get { return Data[Offset + 2]; }
                    set { Data[Offset + 2] = value; }
                }

                // 0-12 (-12 - 0)
                public byte biasLevel1
                {
                    get { return Data[Offset + 3]; }
                    set { Data[Offset + 3] = value; }
                }

                // 0-127 (<1A-<7C >1A-7C)
                public byte biasPoint2
                {
                    get { return Data[Offset + 4]; }
                    set { Data[Offset + 4] = value; }
                }

                // 0-12 (-12 - 0)
                public byte biasLevel2
                {
                    get { return Data[Offset + 5]; }
                    set { Data[Offset + 5] = value; }
                }

                // TIME KEY FOLLOW 0-4
                public byte envTimeKeyfollow
                {
                    get { return Data[Offset + 6]; }
                    set { Data[Offset + 6] = value; }
                }

                // VELOS KEY FOLL0W 0-4
                public byte envTimeVeloSensitivity
                {
                    get { return Data[Offset + 7]; }
                    set { Data[Offset + 7] = value; }
                }

                public readonly BytePtr envTime; // 0-100
                public readonly BytePtr envLevel; // 0-100 // [3]: SUSTAIN LEVEL

                public TVAParam(byte[] data, int offset)
                {
                    Data = data;
                    Offset = offset;
                    envTime = new BytePtr(Data, Offset + 8);
                    envLevel = new BytePtr(Data, Offset + 8 + 5);
                }
            }

            public readonly WGParam wg;
            public readonly PitchEnvParam pitchEnv;
            public readonly PitchLFOParam pitchLFO;
            public readonly TVFParam tvf;
            public readonly TVAParam tva;

            public PartialParam()
                : this(new byte[Size], 0)
            {
            }

            public PartialParam(PartialParam copy)
                : this(new byte[Size], 0)
            {
                Array.Copy(copy.Data, copy.Offset, Data, Offset, Size);
            }

            public PartialParam(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                wg = new WGParam(Data, offset);
                offset += WGParam.Size;
                pitchEnv = new PitchEnvParam(Data, offset);
                offset += PitchEnvParam.Size;
                pitchLFO = new PitchLFOParam(Data, offset);
                offset += PitchLFOParam.Size;
                tvf = new TVFParam(Data, offset);
                offset += TVFParam.Size;
                tva = new TVAParam(Data, offset);
            }
        }

        public readonly CommonParam common;
        public readonly PartialParam[] partial = new PartialParam[4];

        public TimbreParam(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
            common = new CommonParam(data, offset);
            offset += CommonParam.Size;
            for (int i = 0; i < partial.Length; i++)
            {
                partial[i] = new PartialParam(data, offset);
                offset += PartialParam.Size;
            }
        }
    }

    class MemParams
    {
        // NOTE: The MT-32 documentation only specifies PatchTemp areas for parts 1-8.
        // The LAPC-I documentation specified an additional area for rhythm at the end,
        // where all parameters but fine tune, assign mode and output level are ignored
        public class PatchTemp
        {
            public const int Size = PatchParam.Size + 2 + 6;

            public byte[] Data;
            public int Offset;

            private PatchParam patch;
            public PatchParam Patch
            {
                get { return patch; }
                set { Array.Copy(value.Data, value.Offset, patch.Data, patch.Offset, PatchParam.Size); }
            }

            /// <summary>
            /// OUTPUT LEVEL 0-100.
            /// </summary>
            public byte OutputLevel
            {
                get { return Data[Offset + PatchParam.Size]; }
                set { Data[Offset + PatchParam.Size] = value; }
            }

            /// <summary>
            /// PANPOT 0-14 (R-L)
            /// </summary>
            public byte Panpot
            {
                get { return Data[Offset + PatchParam.Size + 1]; }
                set { Data[Offset + PatchParam.Size + 1] = value; }
            }

            public BytePtr Dummy;

            public PatchTemp(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                patch = new PatchParam(Data, Offset);
                Dummy = new BytePtr(Data, Offset + PatchParam.Size + 2);
            }
        }

        public class RhythmTemp
        {
            public const int Size = 4;

            public byte[] Data;
            public int Offset;

            // TIMBRE  0-94 (M1-M64,R1-30,OFF); LAPC-I: 0-127 (M01-M64,R01-R63)
            public byte timbre
            {
                get { return Data[Offset]; }
                set { Data[Offset] = value; }
            }
            // OUTPUT LEVEL 0-100
            public byte OutputLevel
            {
                get { return Data[Offset + 1]; }
                set { Data[Offset + 1] = value; }
            }

            // PANPOT 0-14 (R-L)
            public byte Panpot
            {
                get { return Data[Offset + 2]; }
                set { Data[Offset + 2] = value; }
            }

            // REVERB SWITCH 0-1 (OFF,ON)
            public byte ReverbSwitch
            {
                get { return Data[Offset + 3]; }
                set { Data[Offset + 3] = value; }
            }

            public RhythmTemp(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
            }
        }

        public class PaddedTimbre
        {
            public const int Size = TimbreParam.Size + 10;

            public readonly byte[] Data;
            public readonly int Offset;

            public readonly TimbreParam timbre;
            public readonly BytePtr padding;

            public PaddedTimbre(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                timbre = new TimbreParam(data, offset);
                padding = new BytePtr(data, offset + TimbreParam.Size);
            }
        }

        public class System
        {
            public const int Size = 4 + 9 + 9 + 1;

            public byte[] Data;
            public int Offset;

            // MASTER TUNE 0-127 432.1-457.6Hz
            public byte masterTune
            {
                get { return Data[Offset]; }
                set { Data[Offset] = value; }
            }

            // REVERB MODE 0-3 (room, hall, plate, tap delay)
            public byte reverbMode
            {
                get { return Data[Offset + 1]; }
                set { Data[Offset + 1] = value; }
            }

            // REVERB TIME 0-7 (1-8)
            public byte reverbTime
            {
                get { return Data[Offset + 2]; }
                set { Data[Offset + 2] = value; }
            }

            // REVERB LEVEL 0-7 (1-8)
            public byte reverbLevel
            {
                get { return Data[Offset + 3]; }
                set { Data[Offset + 3] = value; }
            }

            // PARTIAL RESERVE (PART 1) 0-32
            public readonly BytePtr reserveSettings;

            // MIDI CHANNEL (PART1) 0-16 (1-16,OFF)
            public BytePtr chanAssign;

            // MASTER VOLUME 0-100
            public byte masterVol
            {
                get { return Data[Offset + 22]; }
                set { Data[Offset + 22] = value; }
            }

            public System(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                reserveSettings = new BytePtr(data, offset + 4);
                chanAssign = new BytePtr(data, offset + 4 + 9);
            }
        }

        public const int Size = PatchTemp.Size * 9 + RhythmTemp.Size * 85 + TimbreParam.Size * 8 + PatchParam.Size * 128 + PaddedTimbre.Size * (64 + 64 + 64 + 64) + System.Size;

        public byte[] Data;
        public int Offset;

        public readonly PatchTemp[] patchTemp;
        public readonly RhythmTemp[] rhythmTemp;
        public readonly TimbreParam[] timbreTemp;
        public readonly PatchParam[] patches;

        // NOTE: There are only 30 timbres in the "rhythm" bank for MT-32; the additional 34 are for LAPC-I and above
        public readonly PaddedTimbre[] timbres;
        public readonly BytePtr timbresOffset;

        public readonly System system;

        public MemParams()
            : this(new byte[Size], 0)
        {
        }

        public MemParams(MemParams copy)
            : this(new byte[Size], 0)
        {
            Array.Copy(copy.Data, copy.Offset, Data, Offset, Size);
        }

        public MemParams(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;

            patchTemp = new PatchTemp[9];
            for (int i = 0; i < patchTemp.Length; i++)
            {
                patchTemp[i] = new PatchTemp(Data, offset);
                offset += PatchTemp.Size;
            }
            rhythmTemp = new RhythmTemp[85];
            for (int i = 0; i < rhythmTemp.Length; i++)
            {
                rhythmTemp[i] = new RhythmTemp(Data, offset);
                offset += RhythmTemp.Size;
            }
            timbreTemp = new TimbreParam[8];
            for (int i = 0; i < timbreTemp.Length; i++)
            {
                timbreTemp[i] = new TimbreParam(Data, offset);
                offset += TimbreParam.Size;
            }
            patches = new PatchParam[128];
            for (int i = 0; i < patches.Length; i++)
            {
                patches[i] = new PatchParam(Data, offset);
                offset += PatchParam.Size;
            }
            timbresOffset = new BytePtr(data, offset);
            timbres = new PaddedTimbre[64 + 64 + 64 + 64]; // Group A, Group B, Memory, Rhythm
            for (int i = 0; i < timbres.Length; i++)
            {
                timbres[i] = new PaddedTimbre(Data, offset);
                offset += PaddedTimbre.Size;
            }
            system = new System(Data, offset);
        }
    }


    /// <summary>
    /// Methods for emulating the connection between the LA32 and the DAC, which involves
    /// some hacks in the real devices for doubling the volume.
    /// See also http://en.wikipedia.org/wiki/Roland_MT-32#Digital_overflow
    /// </summary>
    enum DACInputMode
    {
        // Produces samples at double the volume, without tricks.
        // * Nicer overdrive characteristics than the DAC hacks (it simply clips samples within range)
        // * Higher quality than the real devices
        NICE,

        // Produces samples that exactly match the bits output from the emulated LA32.
        // * Nicer overdrive characteristics than the DAC hacks (it simply clips samples within range)
        // * Much less likely to overdrive than any other mode.
        // * Half the volume of any of the other modes.
        // * Output gain is ignored for both LA32 and reverb output.
        // * Perfect for developers while debugging :)
        PURE,

        // Re-orders the LA32 output bits as in early generation MT-32s (according to Wikipedia).
        // Bit order at DAC (where each number represents the original LA32 output bit number, and XX means the bit is always low):
        // 15 13 12 11 10 09 08 07 06 05 04 03 02 01 00 XX
        GENERATION1,

        // Re-orders the LA32 output bits as in later generations (personally confirmed on my CM-32L - KG).
        // Bit order at DAC (where each number represents the original LA32 output bit number):
        // 15 13 12 11 10 09 08 07 06 05 04 03 02 01 00 14
        GENERATION2
    }

    /// <summary>
    /// Methods for emulating the effective delay of incoming MIDI messages introduced by a MIDI interface.
    /// </summary>
    enum MIDIDelayMode
    {
        // Process incoming MIDI events immediately.
        IMMEDIATE,

        // Delay incoming short MIDI messages as if they where transferred via a MIDI cable to a real hardware unit and immediate sysex processing.
        // This ensures more accurate timing of simultaneous NoteOn messages.
        DELAY_SHORT_MESSAGES_ONLY,

        // Delay all incoming MIDI events as if they where transferred via a MIDI cable to a real hardware unit.
        DELAY_ALL
    }

    enum ReverbMode
    {
        ROOM,
        HALL,
        PLATE,
        TAP_DELAY
    }

    enum PartialState
    {
        INACTIVE,
        ATTACK,
        SUSTAIN,
        RELEASE
    }

    class ControlROMPCMStruct
    {
        public const int Size = 4;

        public byte pos
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }
        public byte len
        {
            get { return Data[Offset + 1]; }
            set { Data[Offset + 1] = value; }
        }
        public byte pitchLSB
        {
            get { return Data[Offset + 2]; }
            set { Data[Offset + 2] = value; }
        }
        public byte pitchMSB
        {
            get { return Data[Offset + 3]; }
            set { Data[Offset + 3] = value; }
        }

        public readonly byte[] Data;
        public readonly int Offset;

        public ControlROMPCMStruct()
            : this(new byte[Size], 0)
        {
        }

        public ControlROMPCMStruct(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }
    }

    class PCMWaveEntry
    {
        public int addr;
        public int len;
        public bool loop;
        public ControlROMPCMStruct controlROMPCMStruct = new ControlROMPCMStruct();
    }

    class Synth
    {
        public const int MAX_SYSEX_SIZE = 512; // FIXME: Does this correspond to a real MIDI buffer used in h/w devices?
        private const int CONTROL_ROM_SIZE = 64 * 1024;
        private const int SYSEX_MANUFACTURER_ROLAND = 0x41;

        private const int SYSEX_MDL_MT32 = 0x16;
        private const int SYSEX_MDL_D50 = 0x14;

        private const int SYSEX_CMD_RQ1 = 0x11; // Request data #1
        private const int SYSEX_CMD_DT1 = 0x12; // Data set 1
        private const int SYSEX_CMD_WSD = 0x40; // Want to send data
        private const int SYSEX_CMD_RQD = 0x41; // Request data
        private const int SYSEX_CMD_DAT = 0x42; // Data set
        private const int SYSEX_CMD_ACK = 0x43; // Acknowledge
        private const int SYSEX_CMD_EOD = 0x45; // End of data
        private const int SYSEX_CMD_ERR = 0x4E; // Communications error
        private const int SYSEX_CMD_RJC = 0x4F; // Rejection

        const int SYSTEM_MASTER_TUNE_OFF = 0;
        const int SYSTEM_REVERB_MODE_OFF = 1;
        const int SYSTEM_REVERB_TIME_OFF = 2;
        const int SYSTEM_REVERB_LEVEL_OFF = 3;
        const int SYSTEM_RESERVE_SETTINGS_START_OFF = 4;
        const int SYSTEM_RESERVE_SETTINGS_END_OFF = 12;
        const int SYSTEM_CHAN_ASSIGN_START_OFF = 13;
        const int SYSTEM_CHAN_ASSIGN_END_OFF = 21;
        const int SYSTEM_MASTER_VOL_OFF = 22;

        PatchTempMemoryRegion patchTempMemoryRegion;
        RhythmTempMemoryRegion rhythmTempMemoryRegion;
        TimbreTempMemoryRegion timbreTempMemoryRegion;
        PatchesMemoryRegion patchesMemoryRegion;
        TimbresMemoryRegion timbresMemoryRegion;
        SystemMemoryRegion systemMemoryRegion;
        DisplayMemoryRegion displayMemoryRegion;
        ResetMemoryRegion resetMemoryRegion;

        byte[] paddedTimbreMaxTable;

        bool isEnabled;

        internal PCMWaveEntry[] pcmWaves;

        ControlROMFeatureSet controlROMFeatures;
        internal ControlROMMap controlROMMap;
        readonly byte[] controlROMData = new byte[CONTROL_ROM_SIZE];
        internal short[] pcmROMData;
        int pcmROMSize; // This is in 16-bit samples, therefore half the number of bytes in the ROM

        int partialCount;
        sbyte[] chantable = new sbyte[32]; // FIXME: Need explanation why 32 is set, obviously it should be 16

        MidiEventQueue midiQueue;
        volatile int lastReceivedMIDIEventTimestamp;
        volatile int renderedSampleCount;

        public MemParams mt32ram = new MemParams();
        MemParams mt32default;

        BReverbModel[] reverbModels = new BReverbModel[4];
        BReverbModel reverbModel;
        bool reverbOverridden;

        MIDIDelayMode midiDelayMode;
        DACInputMode dacInputMode;

        float outputGain;
        float reverbOutputGain;

        bool reversedStereoEnabled;

        bool isOpen;

        bool isDefaultReportHandler;
        //ReportHandler reportHandler;

        public PartialManager partialManager;
        Part[] parts = new Part[9];

        // When a partial needs to be aborted to free it up for use by a new Poly,
        // the controller will busy-loop waiting for the sound to finish.
        // We emulate this by delaying new MIDI events processing until abortion finishes.
        internal Poly abortingPoly;

        Analog analog;

        static readonly ControlROMMap[] ControlROMMaps = {
                              // ID    IDc IDbytes                     PCMmap  PCMc  tmbrA   tmbrAO, tmbrAC tmbrB   tmbrBO, tmbrBC tmbrR   trC  rhythm  rhyC  rsrv    panpot  prog    rhyMax  patMax  sysMax  timMax
            new ControlROMMap(0x4014, 21, "\x0 ver1.04 14 July 87 ", 0x3000,  128, 0x8000, 0x0000, false, 0xC000, 0x4000, false, 0x3200,  30, 0x73A6,  85,  0x57C7, 0x57E2, 0x57D0, 0x5252, 0x525E, 0x526E, 0x520A),
            new ControlROMMap(0x4014, 21, "\x0 ver1.05 06 Aug, 87 ", 0x3000,  128, 0x8000, 0x0000, false, 0xC000, 0x4000, false, 0x3200,  30, 0x7414,  85,  0x57C7, 0x57E2, 0x57D0, 0x5252, 0x525E, 0x526E, 0x520A),
            new ControlROMMap(0x4014, 21, "\x0 ver1.06 31 Aug, 87 ", 0x3000,  128, 0x8000, 0x0000, false, 0xC000, 0x4000, false, 0x3200,  30, 0x7414,  85,  0x57D9, 0x57F4, 0x57E2, 0x5264, 0x5270, 0x5280, 0x521C),
            new ControlROMMap(0x4010, 21, "\x0 ver1.07 10 Oct, 87 ", 0x3000,  128, 0x8000, 0x0000, false, 0xC000, 0x4000, false, 0x3200,  30, 0x73fe,  85,  0x57B1, 0x57CC, 0x57BA, 0x523C, 0x5248, 0x5258, 0x51F4), // MT-32 revision 1
            new ControlROMMap(0x4010, 21, "\x0verX.XX  30 Sep, 88 ", 0x3000,  128, 0x8000, 0x0000, false, 0xC000, 0x4000, false, 0x3200,  30, 0x741C,  85,  0x57E5, 0x5800, 0x57EE, 0x5270, 0x527C, 0x528C, 0x5228), // MT-32 Blue Ridge mod
            new ControlROMMap(0x2205, 21, "\x0CM32/LAPC1.00 890404", 0x8100,  256, 0x8000, 0x8000, false, 0x8080, 0x8000, false, 0x8500,  64, 0x8580,  85,  0x4F65, 0x4F80, 0x4F6E, 0x48A1, 0x48A5, 0x48BE, 0x48D5),
            new ControlROMMap(0x2205, 21, "\x0CM32/LAPC1.02 891205", 0x8100,  256, 0x8000, 0x8000, true,  0x8080, 0x8000, true,  0x8500,  64, 0x8580,  85,  0x4F93, 0x4FAE, 0x4F9C, 0x48CB, 0x48CF, 0x48E8, 0x48FF)  // CM-32L
        // (Note that all but CM-32L ROM actually have 86 entries for rhythmTemp)
        };

        public int StereoOutputSampleRate
        {
            get
            {
                return (analog == null) ? Mt32Emu.SAMPLE_RATE : analog.OutputSampleRate;
            }
        }

        public DACInputMode DACInputMode
        {
            get
            {
                return dacInputMode;
            }
            set
            {
#if MT32EMU_USE_FLOAT_SAMPLES
                // We aren't emulating these in float mode, so better to inform the invoker
                if ((mode == DACInputMode.GENERATION1) || (mode == DACInputMode.GENERATION2)) {
                    mode = DACInputMode.NICE;
                }
#endif
                dacInputMode = value;
            }
        }

        public MIDIDelayMode MIDIDelayMode
        {
            get { return midiDelayMode; }
            set { midiDelayMode = value; }
        }

        public float OutputGain
        {
            get { return outputGain; }
            set
            {
                if (value < 0.0f) value = -value;
                outputGain = value;
                if (analog != null) analog.SynthOutputGain = value;
            }
        }

        public float ReverbOutputGain
        {
            get { return reverbOutputGain; }
            set
            {
                if (value < 0.0f) value = -value;
                reverbOutputGain = value;
                if (analog != null) analog.SetReverbOutputGain(value, IsMT32ReverbCompatibilityMode);
            }
        }

        public bool IsReversedStereoEnabled
        {
            get { return reversedStereoEnabled; }
            set { reversedStereoEnabled = value; }
        }

        public bool IsMT32ReverbCompatibilityMode
        {
            get { return isOpen && (reverbModels[(int)ReverbMode.ROOM].IsMT32Compatible(ReverbMode.ROOM)); }
        }

        public bool IsReverbEnabled
        {
            get { return reverbModel != null; }
        }

        public int PartialCount
        {
            get
            {
                return partialCount;
            }
        }

        public bool IsAbortingPoly { get { return abortingPoly != null; } }

        public Synth()
        {
            partialCount = Mt32Emu.DEFAULT_MAX_PARTIALS;

            DACInputMode = DACInputMode.NICE;
            MIDIDelayMode = MIDIDelayMode.DELAY_SHORT_MESSAGES_ONLY;
            OutputGain = 1.0f;
            ReverbOutputGain = 1.0f;
        }

        public Part GetPart(int partNum)
        {
            if (partNum > 8)
            {
                return null;
            }
            return parts[partNum];
        }

        public void Render(short[] buf, int pos, int len)
        {
            if (!isEnabled)
            {
                renderedSampleCount += analog.GetDACStreamsLength(len);
                analog.Process(null, 0, null, null, null, null, null, null, len);
                MuteSampleBuffer(buf, pos, len << 1);
                return;
            }

            // As in AnalogOutputMode_ACCURATE mode output is upsampled, buffer size MAX_SAMPLES_PER_RUN is more than enough.
            Sample[] tmpNonReverbLeft = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN], tmpNonReverbRight = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN];
            Sample[] tmpReverbDryLeft = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN], tmpReverbDryRight = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN];
            Sample[] tmpReverbWetLeft = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN], tmpReverbWetRight = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN];

            while (len > 0)
            {
                int thisPassLen = len > Mt32Emu.MAX_SAMPLES_PER_RUN ? Mt32Emu.MAX_SAMPLES_PER_RUN : len;
                RenderStreams(tmpNonReverbLeft, tmpNonReverbRight, tmpReverbDryLeft, tmpReverbDryRight, tmpReverbWetLeft, tmpReverbWetRight, analog.GetDACStreamsLength(thisPassLen));
                analog.Process(buf, pos, tmpNonReverbLeft, tmpNonReverbRight, tmpReverbDryLeft, tmpReverbDryRight, tmpReverbWetLeft, tmpReverbWetRight, thisPassLen);
                len -= thisPassLen;
            }
        }

        public void PlaySysexWithoutFraming(BytePtr sysex, ushort len)
        {
            if (len < 4)
            {
                PrintDebug("playSysexWithoutFraming: Message is too short (%d bytes)!", len);
                return;
            }
            if (sysex[0] != SYSEX_MANUFACTURER_ROLAND)
            {
                PrintDebug("playSysexWithoutFraming: Header not intended for this device manufacturer: %02x %02x %02x %02x", (int)sysex[0], (int)sysex[1], (int)sysex[2], (int)sysex[3]);
                return;
            }
            if (sysex[2] == SYSEX_MDL_D50)
            {
                PrintDebug("playSysexWithoutFraming: Header is intended for model D-50 (not yet supported): %02x %02x %02x %02x", (int)sysex[0], (int)sysex[1], (int)sysex[2], (int)sysex[3]);
                return;
            }
            else if (sysex[2] != SYSEX_MDL_MT32)
            {
                PrintDebug("playSysexWithoutFraming: Header not intended for model MT-32: %02x %02x %02x %02x", (int)sysex[0], (int)sysex[1], (int)sysex[2], (int)sysex[3]);
                return;
            }
            PlaySysexWithoutHeader(sysex[1], sysex[3], new BytePtr(sysex, 4), len - 4);
        }

        private void PlaySysexWithoutHeader(byte device, byte command, BytePtr sysex, int len)
        {
            if (device > 0x10)
            {
                // We have device ID 0x10 (default, but changeable, on real MT-32), < 0x10 is for channels
                PrintDebug("playSysexWithoutHeader: Message is not intended for this device ID (provided: %02x, expected: 0x10 or channel)", (int)device);
                return;
            }
            // This is checked early in the real devices (before any sysex length checks or further processing)
            // FIXME: Response to SYSEX_CMD_DAT reset with partials active (and in general) is untested.
            if ((command == SYSEX_CMD_DT1 || command == SYSEX_CMD_DAT) && sysex[0] == 0x7F)
            {
                Reset();
                return;
            }
            if (len < 4)
            {
                PrintDebug("playSysexWithoutHeader: Message is too short (%d bytes)!", len);
                return;
            }
            byte checksum = CalcSysexChecksum(sysex, len - 1);
            if (checksum != sysex[len - 1])
            {
                PrintDebug("playSysexWithoutHeader: Message checksum is incorrect (provided: %02x, expected: %02x)!", sysex[len - 1], checksum);
                return;
            }
            len -= 1; // Exclude checksum
            switch (command)
            {
                case SYSEX_CMD_DAT:
                case SYSEX_CMD_DT1:
                    if (command == SYSEX_CMD_DAT && HasActivePartials())
                    {
                        PrintDebug("playSysexWithoutHeader: Got SYSEX_CMD_DAT but partials are active - ignoring");
                        // FIXME: We should send SYSEX_CMD_RJC in this case
                        break;
                    }
                    WriteSysex(device, sysex, len);
                    break;
                case SYSEX_CMD_RQD:
                case SYSEX_CMD_RQ1:
                    if (command == SYSEX_CMD_RQD && HasActivePartials())
                    {
                        PrintDebug("playSysexWithoutHeader: Got SYSEX_CMD_RQD but partials are active - ignoring");
                        // FIXME: We should send SYSEX_CMD_RJC in this case
                        break;
                    }
                    ReadSysex(device, sysex, len);
                    break;
                default:
                    PrintDebug("playSysexWithoutHeader: Unsupported command %02x", command);
                    return;
            }
        }

        private bool HasActivePartials()
        {
            for (int partialNum = 0; partialNum < PartialCount; partialNum++)
            {
                if (partialManager.GetPartial(partialNum).IsActive)
                {
                    return true;
                }
            }
            return false;
        }

        private void WriteSysex(byte device, BytePtr sysex, int len)
        {
            //reportHandler.onMIDIMessagePlayed();
            int addr = (sysex[0] << 16) | (sysex[1] << 8) | (sysex[2]);
            addr = Mt32Emu.MT32EMU_MEMADDR(addr);
            sysex.Offset += 3;
            len -= 3;
            //PrintDebug("Sysex addr: 0x{0}", Mt32Emu.MT32EMU_SYSEXMEMADDR(addr));
            // NOTE: Please keep both lower and upper bounds in each check, for ease of reading

            // Process channel-specific sysex by converting it to device-global
            if (device < 0x10)
            {
#if MT32EMU_MONITOR_SYSEX
                PrintDebug("WRITE-CHANNEL: Channel {0} temp area 0x{1:X}", device, Mt32Emu.MT32EMU_SYSEXMEMADDR(addr));
#endif
                if (/*addr >= MT32EMU_MEMADDR(0x000000) && */addr < Mt32Emu.MT32EMU_MEMADDR(0x010000))
                {
                    int offset;
                    if (chantable[device] == -1)
                    {
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Channel not mapped to a part... 0 offset)");
#endif
                        offset = 0;
                    }
                    else if (chantable[device] == 8)
                    {
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Channel mapped to rhythm... 0 offset)");
#endif
                        offset = 0;
                    }
                    else {
                        offset = chantable[device] * MemParams.PatchTemp.Size;
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Setting extra offset to {0})", offset);
#endif
                    }
                    addr += Mt32Emu.MT32EMU_MEMADDR(0x030000) + offset;
                }
                else if (/*addr >= MT32EMU_MEMADDR(0x010000) && */ addr < Mt32Emu.MT32EMU_MEMADDR(0x020000))
                {
                    addr += Mt32Emu.MT32EMU_MEMADDR(0x030110) - Mt32Emu.MT32EMU_MEMADDR(0x010000);
                }
                else if (/*addr >= MT32EMU_MEMADDR(0x020000) && */ addr < Mt32Emu.MT32EMU_MEMADDR(0x030000))
                {
                    int offset;
                    if (chantable[device] == -1)
                    {
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Channel not mapped to a part... 0 offset)");
#endif
                        offset = 0;
                    }
                    else if (chantable[device] == 8)
                    {
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Channel mapped to rhythm... 0 offset)");
#endif
                        offset = 0;
                    }
                    else {
                        offset = chantable[device] * TimbreParam.Size;
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug(" (Setting extra offset to {0})", offset);
#endif
                    }
                    addr += Mt32Emu.MT32EMU_MEMADDR(0x040000) - Mt32Emu.MT32EMU_MEMADDR(0x020000) + offset;
                }
                else {
#if MT32EMU_MONITOR_SYSEX
                    PrintDebug(" Invalid channel");
#endif
                    return;
                }
            }

            // Process device-global sysex (possibly converted from channel-specific sysex above)
            for (;;)
            {
                // Find the appropriate memory region
                MemoryRegion region = FindMemoryRegion(addr);

                if (region == null)
                {
                    PrintDebug("Sysex write to unrecognised address {0}, len {1}", Mt32Emu.MT32EMU_SYSEXMEMADDR(addr), len);
                    break;
                }
                WriteMemoryRegion(region, addr, region.GetClampedLen(addr, len), sysex);

                int next = region.Next(addr, len);
                if (next == 0)
                {
                    break;
                }
                addr += next;
                sysex.Offset += next;
                len -= next;
            }
        }

        private void WriteMemoryRegion(MemoryRegion region, int addr, int len, BytePtr data)
        {
            int first = region.FirstTouched(addr);
            int last = region.LastTouched(addr, len);
            int off = region.FirstTouchedOffset(addr);
            switch (region.type)
            {
                case MemoryRegionType.MR_PatchTemp:
                    region.Write(first, off, data, len);
                    PrintDebug("Patch temp: Patch {0}, offset {1:X}, len {2}", off / 16, off % 16, len);

                    for (int i = first; i <= last; i++)
                    {
                        int absTimbreNum = mt32ram.patchTemp[i].Patch.TimbreGroup * 64 + mt32ram.patchTemp[i].Patch.TimbreNum;
                        string timbreName = mt32ram.timbres[absTimbreNum].timbre.common.name.GetRawText(0, 10);
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug("WRITE-PARTPATCH ({0}-{1}..{2}): {3}; timbre={4} ({5}), outlevel={6}", first, last, off, off + len, i, absTimbreNum, timbreName, mt32ram.patchTemp[i].OutputLevel);
#endif
                        if (parts[i] != null)
                        {
                            if (i != 8)
                            {
                                // Note: Confirmed on CM-64 that we definitely *should* update the timbre here,
                                // but only in the case that the sysex actually writes to those values
                                if (i == first && off > 2)
                                {
#if MT32EMU_MONITOR_SYSEX
                                    PrintDebug(" (Not updating timbre, since those values weren't touched)");
#endif
                                }
                                else {
                                    parts[i].SetTimbre(mt32ram.timbres[parts[i].AbsTimbreNum].timbre);
                                }
                            }
                            parts[i].Refresh();
                        }
                    }
                    break;
                case MemoryRegionType.MR_RhythmTemp:
                    region.Write(first, off, data, len);
                    for (int i = first; i <= last; i++)
                    {
                        int timbreNum = mt32ram.rhythmTemp[i].timbre;
                        string timbreName;
                        if (timbreNum < 94)
                        {
                            timbreName = mt32ram.timbres[128 + timbreNum].timbre.common.name.GetRawText(0, 10);
                        }
                        else {
                            timbreName = "[None]";
                        }
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug("WRITE-RHYTHM ({0}-{1}@{2}..{3}): {4}; level={5:X2}, panpot={6:X2}, reverb={7:X2}, timbre={8:X2} ({9})", first, last, off, off + len, i, mt32ram.rhythmTemp[i].OutputLevel, mt32ram.rhythmTemp[i].Panpot, mt32ram.rhythmTemp[i].ReverbSwitch, mt32ram.rhythmTemp[i].timbre, timbreName);
#endif

                    }
                    if (parts[8] != null)
                    {
                        parts[8].Refresh();
                    }
                    break;
                case MemoryRegionType.MR_TimbreTemp:
                    region.Write(first, off, data, len);
                    for (int i = first; i <= last; i++)
                    {
                        string instrumentName = mt32ram.timbreTemp[i].common.name.GetRawText();
#if MT32EMU_MONITOR_SYSEX
                        PrintDebug("WRITE-PARTTIMBRE ({0}-{1}@{2}..{3}): timbre={4} ({5})", first, last, off, off + len, i, instrumentName);
#endif
                        if (parts[i] != null)
                        {
                            parts[i].Refresh();
                        }
                    }
                    break;
                case MemoryRegionType.MR_Patches:
                    region.Write(first, off, data, len);
#if MT32EMU_MONITOR_SYSEX
                    for (int i = first; i <= last; i++)
                    {
                        PatchParam patch = mt32ram.patches[i];
                        int patchAbsTimbreNum = patch.TimbreGroup * 64 + patch.TimbreNum;
                        var instrumentName = mt32ram.timbres[patchAbsTimbreNum].timbre.common.name.GetRawText(0, 10);
                        var n = new BytePtr(patch.Data, patch.Offset);
                        PrintDebug("WRITE-PATCH ({0}-{1}@{2}..{3}): {4}; timbre={5} ({6}) {7:X2}{8:X2}{9:X2}{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}", first, last, off, off + len, i, patchAbsTimbreNum, instrumentName, n[0], n[1], n[2], n[3], n[4], n[5], n[6], n[7]);
                    }
#endif
                    break;
                case MemoryRegionType.MR_Timbres:
                    // Timbres
                    first += 128;
                    last += 128;
                    region.Write(first, off, data, len);
                    for (int i = first; i <= last; i++)
                    {
                        // FIXME:KG: Not sure if the stuff below should be done (for rhythm and/or parts)...
                        // Does the real MT-32 automatically do this?
                        for (int part = 0; part < 9; part++)
                        {
                            if (parts[part] != null)
                            {
                                parts[part].RefreshTimbre(i);
                            }
                        }
                    }
                    break;
                case MemoryRegionType.MR_System:
                    region.Write(0, off, data, len);

                    //reportHandler.onDeviceReconfig();
                    // FIXME: We haven't properly confirmed any of this behaviour
                    // In particular, we tend to reset things such as reverb even if the write contained
                    // the same parameters as were already set, which may be wrong.
                    // On the other hand, the real thing could be resetting things even when they aren't touched
                    // by the write at all.
#if MT32EMU_MONITOR_SYSEX
                    PrintDebug("WRITE-SYSTEM:");
#endif
                    if (off <= SYSTEM_MASTER_TUNE_OFF && off + len > SYSTEM_MASTER_TUNE_OFF)
                    {
                        RefreshSystemMasterTune();
                    }
                    if (off <= SYSTEM_REVERB_LEVEL_OFF && off + len > SYSTEM_REVERB_MODE_OFF)
                    {
                        RefreshSystemReverbParameters();
                    }
                    if (off <= SYSTEM_RESERVE_SETTINGS_END_OFF && off + len > SYSTEM_RESERVE_SETTINGS_START_OFF)
                    {
                        RefreshSystemReserveSettings();
                    }
                    if (off <= SYSTEM_CHAN_ASSIGN_END_OFF && off + len > SYSTEM_CHAN_ASSIGN_START_OFF)
                    {
                        int firstPart = off - SYSTEM_CHAN_ASSIGN_START_OFF;
                        if (firstPart < 0)
                            firstPart = 0;
                        int lastPart = off + len - SYSTEM_CHAN_ASSIGN_START_OFF;
                        if (lastPart > 9)
                            lastPart = 9;
                        RefreshSystemChanAssign(firstPart, lastPart);
                    }
                    if (off <= SYSTEM_MASTER_VOL_OFF && off + len > SYSTEM_MASTER_VOL_OFF)
                    {
                        RefreshSystemMasterVol();
                    }
                    break;
                case MemoryRegionType.MR_Display:
#if MT32EMU_MONITOR_SYSEX
                    var buf = data.GetRawText(0, len);
                    PrintDebug("WRITE-LCD: {0}", buf);
#endif
                    //reportHandler.showLCDMessage(buf);
                    break;
                case MemoryRegionType.MR_Reset:
                    Reset();
                    break;
            }

        }

        private void ReadSysex(byte device, BytePtr sysex, int len)
        {
            // NYI
        }

        private byte CalcSysexChecksum(BytePtr data, int len, int initChecksum = 0)
        {
            int checksum = -initChecksum;
            for (int i = 0; i < len; i++)
            {
                checksum -= data[i];
            }
            return ((byte)(checksum & 0x7f));
        }

        private MemoryRegion FindMemoryRegion(int addr)
        {
            MemoryRegion[] regions = {
                patchTempMemoryRegion,
                rhythmTempMemoryRegion,
                timbreTempMemoryRegion,
                patchesMemoryRegion,
                timbresMemoryRegion,
                systemMemoryRegion,
                displayMemoryRegion,
                resetMemoryRegion,
            };
            foreach (var region in regions)
            {
                if (region.Contains(addr))
                {
                    return region;
                }
            }
            return null;
        }

        private void Reset()
        {
#if MT32EMU_MONITOR_SYSEX
            PrintDebug("RESET");
#endif
            //reportHandler.onDeviceReset();
            partialManager.DeactivateAll();
            Array.Copy(mt32default.Data, mt32default.Offset, mt32ram.Data, mt32ram.Offset, MemParams.Size);
            for (int i = 0; i < 9; i++)
            {
                parts[i].Reset();
                if (i != 8)
                {
                    parts[i].SetProgram(controlROMData[controlROMMap.programSettings + i]);
                }
                else {
                    parts[8].Refresh();
                }
            }
            RefreshSystem();
            isEnabled = false;
        }

        public bool PlaySysex(BytePtr sysex, int len)
        {
            return PlaySysex(sysex, len, renderedSampleCount);
        }

        private bool PlaySysex(BytePtr sysex, int len, int timestamp)
        {
            if (midiQueue == null) return false;
            if (midiDelayMode == MIDIDelayMode.DELAY_ALL)
            {
                timestamp = AddMidiInterfaceDelay(len, timestamp);
            }
            if (!isEnabled) isEnabled = true;
            return midiQueue.PushSysex(sysex, len, timestamp);
        }

        public bool PlayMsg(int msg)
        {
            return PlayMsg(msg, renderedSampleCount);
        }

        private bool PlayMsg(int msg, int timestamp)
        {
            if (midiQueue == null) return false;
            if (midiDelayMode != MIDIDelayMode.IMMEDIATE)
            {
                timestamp = AddMidiInterfaceDelay(GetShortMessageLength(msg), timestamp);
            }
            if (!isEnabled) isEnabled = true;
            return midiQueue.PushShortMessage(msg, timestamp);
        }

        private int GetShortMessageLength(int msg)
        {
            if ((msg & 0xF0) == 0xF0)
            {
                switch (msg & 0xFF)
                {
                    case 0xF1:
                    case 0xF3:
                        return 2;
                    case 0xF2:
                        return 3;
                    default:
                        return 1;
                }
            }
            // NOTE: This calculation isn't quite correct
            // as it doesn't consider the running status byte
            return ((msg & 0xE0) == 0xC0) ? 2 : 3;
        }

        private int AddMidiInterfaceDelay(int len, int timestamp)
        {
            int transferTime = (int)(len * Mt32Emu.MIDI_DATA_TRANSFER_RATE);
            // Dealing with wrapping
            if (timestamp - lastReceivedMIDIEventTimestamp < 0)
            {
                timestamp = lastReceivedMIDIEventTimestamp;
            }
            timestamp += transferTime;
            lastReceivedMIDIEventTimestamp = timestamp;
            return timestamp;
        }

        private void RenderStreams(Sample[] nonReverbLeft, Sample[] nonReverbRight, Sample[] reverbDryLeft, Sample[] reverbDryRight, Sample[] reverbWetLeft, Sample[] reverbWetRight, int len)
        {
            var nrl = new Ptr<Sample>(nonReverbLeft);
            var nrr = new Ptr<Sample>(nonReverbRight);
            var rdl = new Ptr<Sample>(reverbDryLeft);
            var rdr = new Ptr<Sample>(reverbDryRight);
            var rwl = new Ptr<Sample>(reverbWetLeft);
            var rwr = new Ptr<Sample>(reverbWetRight);
            while (len > 0)
            {
                // We need to ensure zero-duration notes will play so add minimum 1-sample delay.
                int thisLen = 1;
                if (!IsAbortingPoly)
                {
                    MidiEvent nextEvent = midiQueue.PeekMidiEvent();
                    int samplesToNextEvent = (nextEvent != null) ? (nextEvent.timestamp - renderedSampleCount) : Mt32Emu.MAX_SAMPLES_PER_RUN;
                    if (samplesToNextEvent > 0)
                    {
                        thisLen = len > Mt32Emu.MAX_SAMPLES_PER_RUN ? Mt32Emu.MAX_SAMPLES_PER_RUN : len;
                        if (thisLen > samplesToNextEvent)
                        {
                            thisLen = samplesToNextEvent;
                        }
                    }
                    else {
                        if (nextEvent.sysexData == BytePtr.Null)
                        {
                            PlayMsgNow(nextEvent.shortMessageData);
                            // If a poly is aborting we don't drop the event from the queue.
                            // Instead, we'll return to it again when the abortion is done.
                            if (!IsAbortingPoly)
                            {
                                midiQueue.DropMidiEvent();
                            }
                        }
                        else {
                            PlaySysexNow(nextEvent.sysexData, nextEvent.sysexLength);
                            midiQueue.DropMidiEvent();
                        }
                    }
                }
                DoRenderStreams(nrl, nrr, rdl, rdr, rwl, rwr, thisLen);
                AdvanceStreamPosition(ref nrl, thisLen);
                AdvanceStreamPosition(ref nrr, thisLen);
                AdvanceStreamPosition(ref rdl, thisLen);
                AdvanceStreamPosition(ref rdr, thisLen);
                AdvanceStreamPosition(ref rwl, thisLen);
                AdvanceStreamPosition(ref rwr, thisLen);
                len -= thisLen;
            }
        }

        private void AdvanceStreamPosition(ref Ptr<Sample> stream, int posDelta)
        {
            if (stream != Ptr<Sample>.Null)
            {
                stream.Offset += posDelta;
            }
        }

        private void DoRenderStreams(Ptr<Sample> nonReverbLeft, Ptr<Sample> nonReverbRight, Ptr<Sample> reverbDryLeft, Ptr<Sample> reverbDryRight, Ptr<Sample> reverbWetLeft, Ptr<Sample> reverbWetRight, int len)
        {
            // Even if LA32 output isn't desired, we proceed anyway with temp buffers
            Sample[] tmpBufNonReverbLeft = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN], tmpBufNonReverbRight = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN];
            if (nonReverbLeft == Ptr<Sample>.Null) nonReverbLeft = tmpBufNonReverbLeft;
            if (nonReverbRight == Ptr<Sample>.Null) nonReverbRight = tmpBufNonReverbRight;

            Sample[] tmpBufReverbDryLeft = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN], tmpBufReverbDryRight = new Sample[Mt32Emu.MAX_SAMPLES_PER_RUN];
            if (reverbDryLeft == Ptr<Sample>.Null) reverbDryLeft = tmpBufReverbDryLeft;
            if (reverbDryRight == Ptr<Sample>.Null) reverbDryRight = tmpBufReverbDryRight;

            if (isEnabled)
            {
                MuteSampleBuffer(nonReverbLeft.Data, nonReverbLeft.Offset, len);
                MuteSampleBuffer(nonReverbRight.Data, nonReverbRight.Offset, len);
                MuteSampleBuffer(reverbDryLeft.Data, reverbDryLeft.Offset, len);
                MuteSampleBuffer(reverbDryRight.Data, reverbDryRight.Offset, len);

                for (int i = 0; i < PartialCount; i++)
                {
                    if (partialManager.ShouldReverb(i))
                    {
                        partialManager.ProduceOutput(i, reverbDryLeft, reverbDryRight, len);
                    }
                    else {
                        partialManager.ProduceOutput(i, nonReverbLeft, nonReverbRight, len);
                    }
                }

                ProduceLA32Output(reverbDryLeft, len);
                ProduceLA32Output(reverbDryRight, len);

                if (IsReverbEnabled)
                {
                    reverbModel.Process(reverbDryLeft, reverbDryRight, reverbWetLeft, reverbWetRight, len);
                    if (reverbWetLeft != Ptr<Sample>.Null) ConvertSamplesToOutput(reverbWetLeft, len);
                    if (reverbWetRight != Ptr<Sample>.Null) ConvertSamplesToOutput(reverbWetRight, len);
                }
                else {
                    MuteSampleBuffer(reverbWetLeft.Data, reverbWetLeft.Offset, len);
                    MuteSampleBuffer(reverbWetRight.Data, reverbWetRight.Offset, len);
                }

                // Don't bother with conversion if the output is going to be unused
                if (nonReverbLeft != tmpBufNonReverbLeft)
                {
                    ProduceLA32Output(nonReverbLeft, len);
                    ConvertSamplesToOutput(nonReverbLeft, len);
                }
                if (nonReverbRight != tmpBufNonReverbRight)
                {
                    ProduceLA32Output(nonReverbRight, len);
                    ConvertSamplesToOutput(nonReverbRight, len);
                }
                if (reverbDryLeft != tmpBufReverbDryLeft) ConvertSamplesToOutput(reverbDryLeft, len);
                if (reverbDryRight != tmpBufReverbDryRight) ConvertSamplesToOutput(reverbDryRight, len);
            }
            else {
                // Avoid muting buffers that wasn't requested
                if (nonReverbLeft != tmpBufNonReverbLeft) MuteSampleBuffer(nonReverbLeft.Data, nonReverbLeft.Offset, len);
                if (nonReverbRight != tmpBufNonReverbRight) MuteSampleBuffer(nonReverbRight.Data, nonReverbRight.Offset, len);
                if (reverbDryLeft != tmpBufReverbDryLeft) MuteSampleBuffer(reverbDryLeft.Data, reverbDryLeft.Offset, len);
                if (reverbDryRight != tmpBufReverbDryRight) MuteSampleBuffer(reverbDryRight.Data, reverbDryRight.Offset, len);
                MuteSampleBuffer(reverbWetLeft.Data, reverbWetLeft.Offset, len);
                MuteSampleBuffer(reverbWetRight.Data, reverbWetRight.Offset, len);
            }

            partialManager.ClearAlreadyOutputed();
            renderedSampleCount += len;
        }

        private void ConvertSamplesToOutput(Ptr<Sample> buffer, int len)
        {
#if MT32EMU_USE_FLOAT_SAMPLES
#else
            if (dacInputMode == DACInputMode.GENERATION1)
            {
                var b = 0;
                while ((len--) != 0)
                {
                    buffer[b] = ((Sample)((buffer[b] & 0x8000) | ((buffer[b] << 1) & 0x7FFE)));
                    ++b;
                }
            }
#endif
        }

        // In GENERATION2 units, the output from LA32 goes to the Boss chip already bit-shifted.
        // In NICE mode, it's also better to increase volume before the reverb processing to preserve accuracy.
        private void ProduceLA32Output(Ptr<Sample> buffer, int len)
        {
#if MT32EMU_USE_FLOAT_SAMPLES
#else
            var b = 0;
            switch (dacInputMode)
            {
                case DACInputMode.GENERATION2:
                    while ((len--) != 0)
                    {
                        buffer[b] = (Sample)((buffer[b] & 0x8000) | ((buffer[b] << 1) & 0x7FFE) | ((buffer[b] >> 14) & 0x0001));
                        ++b;
                    }
                    break;
                case DACInputMode.NICE:
                    while ((len--) != 0)
                    {
                        buffer[b] = ClipSampleEx((buffer[b]) << 1);
                        ++b;
                    }
                    break;
            }
#endif
        }

        private void PlaySysexNow(BytePtr sysex, int len)
        {
            if (len < 2)
            {
                PrintDebug("playSysex: Message is too short for sysex (%d bytes)", len);
            }
            if (sysex[0] != 0xF0)
            {
                PrintDebug("playSysex: Message lacks start-of-sysex (0xF0)");
                return;
            }
            // Due to some programs (e.g. Java) sending buffers with junk at the end, we have to go through and find the end marker rather than relying on len.
            int endPos;
            for (endPos = 1; endPos < len; endPos++)
            {
                if (sysex[endPos] == 0xF7)
                {
                    break;
                }
            }
            if (endPos == len)
            {
                PrintDebug("playSysex: Message lacks end-of-sysex (0xf7)");
                return;
            }
            PlaySysexWithoutFraming(new BytePtr(sysex, 1), (ushort)(endPos - 1));
        }

        private void PlayMsgNow(int msg)
        {
            // NOTE: Active sense IS implemented in real hardware. However, realtime processing is clearly out of the library scope.
            //       It is assumed that realtime consumers of the library respond to these MIDI events as appropriate.

            byte code = (byte)((msg & 0x0000F0) >> 4);
            byte chan = (byte)(msg & 0x00000F);
            byte note = (byte)((msg & 0x007F00) >> 8);
            byte velocity = (byte)((msg & 0x7F0000) >> 16);
            if (!isEnabled) isEnabled = true;

            //PrintDebug("Playing chan {0}, code 0x{1:X1} note: 0x{2:X2}", chan, code, note);

            var part = (byte)chantable[chan];
            if (part < 0 || part > 8)
            {
#if MT32EMU_MONITOR_MIDI
                PrintDebug("Play msg on unreg chan %d (%d): code=0x%01x, vel=%d", chan, part, code, velocity);
#endif
                return;
            }
            PlayMsgOnPart(part, code, note, velocity);
        }

        private void PlayMsgOnPart(byte part, byte code, byte note, byte velocity)
        {
            int bend;

            //PrintDebug("Synth::playMsgOnPart({0:X2}, {1:X2}, {2:X2}, {3:X2})", part, code, note, velocity);
            switch (code)
            {
                case 0x8:
                    //PrintDebug("Note OFF - Part {0}", part);
                    // The MT-32 ignores velocity for note off
                    parts[part].NoteOff(note);
                    break;
                case 0x9:
                    //PrintDebug("Note ON - Part {0}, Note {1} Vel {2}", part, note, velocity);
                    if (velocity == 0)
                    {
                        // MIDI defines note-on with velocity 0 as being the same as note-off with velocity 40
                        parts[part].NoteOff(note);
                    }
                    else {
                        parts[part].NoteOn(note, velocity);
                    }
                    break;
                case 0xB: // Control change
                    switch (note)
                    {
                        case 0x01:  // Modulation
                            //PrintDebug("Modulation: {0}", velocity);
                            parts[part].SetModulation(velocity);
                            break;
                        case 0x06:
                            parts[part].SetDataEntryMSB(velocity);
                            break;
                        case 0x07:  // Set volume
                            //PrintDebug("Volume set: {0}", velocity);
                            parts[part].SetVolume(velocity);
                            break;
                        case 0x0A:  // Pan
                            //PrintDebug("Pan set: {0}", velocity);
                            parts[part].SetPan(velocity);
                            break;
                        case 0x0B:
                            //PrintDebug("Expression set: {0}", velocity);
                            parts[part].SetExpression(velocity);
                            break;
                        case 0x40: // Hold (sustain) pedal
                            //PrintDebug("Hold pedal set: {0}", velocity);
                            parts[part].SetHoldPedal(velocity >= 64);
                            break;

                        case 0x62:
                        case 0x63:
                            parts[part].SetNRPN();
                            break;
                        case 0x64:
                            parts[part].SetRPNLSB(velocity);
                            break;
                        case 0x65:
                            parts[part].SetRPNMSB(velocity);
                            break;

                        case 0x79: // Reset all controllers
                            //PrintDebug("Reset all controllers");
                            parts[part].ResetAllControllers();
                            break;

                        case 0x7B: // All notes off
                            //PrintDebug("All notes off");
                            parts[part].AllNotesOff();
                            break;

                        case 0x7C:
                        case 0x7D:
                        case 0x7E:
                        case 0x7F:
                            // CONFIRMED:Mok: A real LAPC-I responds to these controllers as follows:
                            parts[part].SetHoldPedal(false);
                            parts[part].AllNotesOff();
                            break;

                        default:
#if MT32EMU_MONITOR_MIDI
                            PrintDebug("Unknown MIDI Control code: 0x%02x - vel 0x%02x", note, velocity);
#endif
                            return;
                    }

                    break;
                case 0xC: // Program change
                    //PrintDebug("Program change {0:X1}", note);
                    parts[part].SetProgram(note);
                    break;
                case 0xE: // Pitch bender
                    bend = (velocity << 7) | (note);
                    //PrintDebug("Pitch bender {0:x2}", bend);
                    parts[part].SetBend(bend);
                    break;
                default:
#if MT32EMU_MONITOR_MIDI
                    PrintDebug("Unknown Midi code: 0x%01x - %02x - %02x", code, note, velocity);
#endif
                    return;
            }
            //reportHandler.onMIDIMessagePlayed();
        }

        public void NewTimbreSet(int partNum, byte timbreGroup, byte[] patchName)
        {
            //reportHandler.OnProgramChanged(partNum, timbreGroup, patchName);
        }

        public static Sample ClipSampleEx(SampleEx sampleEx)
        {
#if MT32EMU_USE_FLOAT_SAMPLES
            return sampleEx;
#else
            // Clamp values above 32767 to 32767, and values below -32768 to -32768
            // FIXME: Do we really need this stuff? I think these branches are very well predicted. Instead, this introduces a chain.
            // The version below is actually a bit faster on my system...
            return (Sample)(((-0x8000 <= sampleEx) && (sampleEx <= 0x7FFF)) ? (Sample)sampleEx : (sampleEx >> 31) ^ 0x7FFF);
#endif
        }

        public bool Open(ROMImage controlROMImage, ROMImage pcmROMImage, AnalogOutputMode analogOutputMode)
        {
            return Open(controlROMImage, pcmROMImage, Mt32Emu.DEFAULT_MAX_PARTIALS, analogOutputMode);
        }

        public bool Open(ROMImage controlROMImage, ROMImage pcmROMImage, int usePartialCount = Mt32Emu.DEFAULT_MAX_PARTIALS, AnalogOutputMode analogOutputMode = AnalogOutputMode.COARSE)
        {
            if (isOpen)
            {
                return false;
            }
            partialCount = usePartialCount;
            abortingPoly = null;

            // This is to help detect bugs
            //memset(&mt32ram, '?', sizeof(mt32ram));

#if MT32EMU_MONITOR_INIT
            PrintDebug("Loading Control ROM");
#endif
            if (!LoadControlROM(controlROMImage))
            {
                PrintDebug("Init Error - Missing or invalid Control ROM image");
                //ReportHandler.OnErrorControlROM();
                return false;
            }

            InitMemoryRegions();

            // 512KB PCM ROM for MT-32, etc.
            // 1MB PCM ROM for CM-32L, LAPC-I, CM-64, CM-500
            // Note that the size below is given in samples (16-bit), not bytes
            pcmROMSize = controlROMMap.pcmCount == 256 ? 512 * 1024 : 256 * 1024;
            pcmROMData = new short[pcmROMSize];

#if MT32EMU_MONITOR_INIT
            PrintDebug("Loading PCM ROM");
#endif
            if (!LoadPCMROM(pcmROMImage))
            {
                PrintDebug("Init Error - Missing PCM ROM image");
                //ReportHandler.onErrorPCMROM();
                return false;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Reverb Models");
#endif
            bool mt32CompatibleReverb = controlROMFeatures.IsDefaultReverbMT32Compatible;
#if MT32EMU_MONITOR_INIT
            PrintDebug("Using %s Compatible Reverb Models", mt32CompatibleReverb ? "MT-32" : "CM-32L");
#endif
            SetReverbCompatibilityMode(mt32CompatibleReverb);

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Timbre Bank A");
#endif
            if (!InitTimbres(controlROMMap.timbreAMap, controlROMMap.timbreAOffset, 0x40, 0, controlROMMap.timbreACompressed))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Timbre Bank B");
#endif
            if (!InitTimbres(controlROMMap.timbreBMap, controlROMMap.timbreBOffset, 0x40, 64, controlROMMap.timbreBCompressed))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Timbre Bank R");
#endif
            if (!InitTimbres(controlROMMap.timbreRMap, 0, controlROMMap.timbreRCount, 192, true))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Timbre Bank M");
#endif
            // CM-64 seems to initialise all bytes in this bank to 0.
            Array.Clear(mt32ram.timbresOffset.Data, mt32ram.timbresOffset.Offset, 64);

            partialManager = new PartialManager(this, parts);

            pcmWaves = new PCMWaveEntry[controlROMMap.pcmCount];
            for (int i = 0; i < pcmWaves.Length; i++)
            {
                pcmWaves[i] = new PCMWaveEntry();
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising PCM List");
#endif
            InitPCMList(controlROMMap.pcmTable, controlROMMap.pcmCount);


#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Rhythm Temp");
#endif
            Array.Copy(controlROMData, controlROMMap.rhythmSettings, mt32ram.rhythmTemp[0].Data, mt32ram.rhythmTemp[0].Offset, controlROMMap.rhythmSettingsCount * 4);

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising Patches");
#endif
            for (var i = 0; i < 128; i++)
            {
                PatchParam patch = mt32ram.patches[i];
                patch.TimbreGroup = (byte)(i / 64);
                patch.TimbreNum = (byte)(i % 64);
                patch.KeyShift = 24;
                patch.FineTune = 50;
                patch.BenderRange = 12;
                patch.AssignMode = 0;
                patch.ReverbSwitch = 1;
                patch.Dummy = 0;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Initialising System");
#endif
            // The MT-32 manual claims that "Standard pitch" is 442Hz.
            mt32ram.system.masterTune = 0x4A; // Confirmed on CM-64
            mt32ram.system.reverbMode = 0; // Confirmed
            mt32ram.system.reverbTime = 5; // Confirmed
            mt32ram.system.reverbLevel = 3; // Confirmed
            Array.Copy(controlROMData, controlROMMap.reserveSettings, mt32ram.system.reserveSettings.Data, mt32ram.system.reserveSettings.Offset, 9);
            for (byte i = 0; i < 9; i++)
            {
                // This is the default: {1, 2, 3, 4, 5, 6, 7, 8, 9}
                // An alternative configuration can be selected by holding "Master Volume"
                // and pressing "PART button 1" on the real MT-32's frontpanel.
                // The channel assignment is then {0, 1, 2, 3, 4, 5, 6, 7, 9}
                mt32ram.system.chanAssign[i] = (byte)(i + 1);
            }
            mt32ram.system.masterVol = 100; // Confirmed

            bool oldReverbOverridden = reverbOverridden;
            reverbOverridden = false;
            RefreshSystem();
            reverbOverridden = oldReverbOverridden;

            for (int i = 0; i < 9; i++)
            {
                MemParams.PatchTemp patchTemp = mt32ram.patchTemp[i];

                // Note that except for the rhythm part, these patch fields will be set in setProgram() below anyway.
                patchTemp.Patch.TimbreGroup = 0;
                patchTemp.Patch.TimbreNum = 0;
                patchTemp.Patch.KeyShift = 24;
                patchTemp.Patch.FineTune = 50;
                patchTemp.Patch.BenderRange = 12;
                patchTemp.Patch.AssignMode = 0;
                patchTemp.Patch.ReverbSwitch = 1;
                patchTemp.Patch.Dummy = 0;

                patchTemp.OutputLevel = 80;
                patchTemp.Panpot = controlROMData[controlROMMap.panSettings + i];
                Array.Clear(patchTemp.Dummy.Data, patchTemp.Dummy.Offset, 6);
                patchTemp.Dummy[1] = 127;

                if (i < 8)
                {
                    parts[i] = new Part(this, i);
                    parts[i].SetProgram(controlROMData[controlROMMap.programSettings + i]);
                }
                else {
                    parts[i] = new RhythmPart(this, i);
                }
            }

            // For resetting mt32 mid-execution
            mt32default = new MemParams(mt32ram);

            midiQueue = new MidiEventQueue();

            analog = new Analog(analogOutputMode, controlROMFeatures);
            OutputGain = outputGain;
            ReverbOutputGain = reverbOutputGain;

            isOpen = true;
            isEnabled = false;

#if MT32EMU_MONITOR_INIT
            PrintDebug("*** Initialisation complete ***");
#endif
            return true;
        }

        private void RefreshSystem()
        {
            RefreshSystemMasterTune();
            RefreshSystemReverbParameters();
            RefreshSystemReserveSettings();
            RefreshSystemChanAssign(0, 8);
            RefreshSystemMasterVol();
        }

        private void RefreshSystemMasterTune()
        {
#if MT32EMU_MONITOR_SYSEX
            //FIXME:KG: This is just an educated guess.
            // The LAPC-I documentation claims a range of 427.5Hz-452.6Hz (similar to what we have here)
            // The MT-32 documentation claims a range of 432.1Hz-457.6Hz
            float masterTune = 440.0f * MT32EmuMath.EXP2F((mt32ram.system.masterTune - 64.0f) / (128.0f * 12.0f));
            PrintDebug(" Master Tune: {0}", masterTune);
#endif
        }

        private void RefreshSystemReserveSettings()
        {
            var rset = mt32ram.system.reserveSettings;
#if MT32EMU_MONITOR_SYSEX
            PrintDebug(" Partial reserve: 1={0} 2={1} 3={2} 4={3} 5={4} 6={5} 7={6} 8={7} Rhythm={8}", rset[0], rset[1], rset[2], rset[3], rset[4], rset[5], rset[6], rset[7], rset[8]);
#endif
            partialManager.SetReserve(rset);
        }

        private void RefreshSystemChanAssign(int firstPart, int lastPart)
        {
            chantable.Set(0, (sbyte)-1, chantable.Length);

            // CONFIRMED: In the case of assigning a channel to multiple parts, the lower part wins.
            for (int i = 0; i <= 8; i++)
            {
                if (parts[i] != null && i >= firstPart && i <= lastPart)
                {
                    // CONFIRMED: Decay is started for all polys, and all controllers are reset, for every part whose assignment was touched by the sysex write.
                    parts[i].AllSoundOff();
                    parts[i].ResetAllControllers();
                }
                int chan = mt32ram.system.chanAssign[i];
                if (chan != 16 && chantable[chan] == -1)
                {
                    chantable[chan] = (sbyte)i;
                }
            }

#if MT32EMU_MONITOR_SYSEX
            BytePtr rset = mt32ram.system.chanAssign;
            PrintDebug(" Part assign:     1={0} 2={1} 3={2} 4={3} 5={4} 6={5} 7={6} 8={7} Rhythm={8}", rset[0], rset[1], rset[2], rset[3], rset[4], rset[5], rset[6], rset[7], rset[8]);
#endif
        }

        private void RefreshSystemMasterVol()
        {
#if MT32EMU_MONITOR_SYSEX
            PrintDebug(" Master volume: {0}", mt32ram.system.masterVol);
#endif
        }

        private bool InitPCMList(ushort mapAddress, ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                var tps = new ControlROMPCMStruct(controlROMData, mapAddress + i * ControlROMPCMStruct.Size);
                int rAddr = tps.pos * 0x800;
                int rLenExp = (tps.len & 0x70) >> 4;
                int rLen = 0x800 << rLenExp;
                if (rAddr + rLen > pcmROMSize)
                {
                    PrintDebug("Control ROM error: Wave map entry %d points to invalid PCM address 0x%04X, length 0x%04X", i, rAddr, rLen);
                    return false;
                }
                pcmWaves[i].addr = rAddr;
                pcmWaves[i].len = rLen;
                pcmWaves[i].loop = (tps.len & 0x80) != 0;
                pcmWaves[i].controlROMPCMStruct = tps;
                int pitch = (tps.pitchMSB << 8) | tps.pitchLSB;
                bool unaffectedByMasterTune = (tps.len & 0x01) == 0;
                PrintDebug("PCM {0}: pos={1}, len={2}, pitch={3}, loop={4}, unaffectedByMasterTune={5}", i, rAddr, rLen, pitch, pcmWaves[i].loop ? "YES" : "NO", unaffectedByMasterTune ? "YES" : "NO");
            }
            return false;
        }

        private bool InitTimbres(ushort mapAddress, ushort offset, int count, int startTimbre, bool compressed)
        {
            var timbreMap = new BytePtr(controlROMData, mapAddress);
            for (var i = 0; i < count * 2; i += 2)
            {
                ushort address = (ushort)((timbreMap[i + 1] << 8) | timbreMap[i]);
                if (!compressed && (address + offset + TimbreParam.Size > CONTROL_ROM_SIZE))
                {
                    PrintDebug("Control ROM error: Timbre map entry 0x%04x for timbre %d points to invalid timbre address 0x%04x", i, startTimbre, address);
                    return false;
                }
                address += offset;
                if (compressed)
                {
                    if (!InitCompressedTimbre(startTimbre, new BytePtr(controlROMData, address), CONTROL_ROM_SIZE - address))
                    {
                        PrintDebug("Control ROM error: Timbre map entry 0x%04x for timbre %d points to invalid timbre at 0x%04x", i, startTimbre, address);
                        return false;
                    }
                }
                else {
                    timbresMemoryRegion.Write(startTimbre, 0, new BytePtr(controlROMData, address), TimbreParam.Size, true);
                }
                startTimbre++;
            }
            return true;
        }

        private bool InitCompressedTimbre(int timbreNum, BytePtr src, int srcLen)
        {
            // "Compressed" here means that muted partials aren't present in ROM (except in the case of partial 0 being muted).
            // Instead the data from the previous unmuted partial is used.
            if (srcLen < TimbreParam.CommonParam.Size)
            {
                return false;
            }
            TimbreParam timbre = mt32ram.timbres[timbreNum].timbre;
            timbresMemoryRegion.Write(timbreNum, 0, src, TimbreParam.CommonParam.Size, true);
            int srcPos = TimbreParam.CommonParam.Size;
            int memPos = TimbreParam.CommonParam.Size;
            for (int t = 0; t < 4; t++)
            {
                if (t != 0 && ((timbre.common.partialMute >> t) & 0x1) == 0x00)
                {
                    // This partial is muted - we'll copy the previously copied partial, then
                    srcPos -= TimbreParam.PartialParam.Size;
                }
                else if (srcPos + TimbreParam.PartialParam.Size >= srcLen)
                {
                    return false;
                }
                timbresMemoryRegion.Write(timbreNum, memPos, new BytePtr(src, srcPos), TimbreParam.PartialParam.Size);
                srcPos += TimbreParam.PartialParam.Size;
                memPos += TimbreParam.PartialParam.Size;
            }
            return true;
        }

        public void SetReverbCompatibilityMode(bool value)
        {
            if (reverbModels[(int)ReverbMode.ROOM] != null)
            {
                if (IsMT32ReverbCompatibilityMode == value) return;
                SetReverbEnabled(false);
                for (int i = 0; i < 4; i++)
                {
                    reverbModels[i] = null;
                }
            }
            reverbModels[(int)ReverbMode.ROOM] = new BReverbModel(ReverbMode.ROOM, value);
            reverbModels[(int)ReverbMode.HALL] = new BReverbModel(ReverbMode.HALL, value);
            reverbModels[(int)ReverbMode.PLATE] = new BReverbModel(ReverbMode.PLATE, value);
            reverbModels[(int)ReverbMode.TAP_DELAY] = new BReverbModel(ReverbMode.TAP_DELAY, value);
#if !MT32EMU_REDUCE_REVERB_MEMORY
            for (int i = (int)ReverbMode.ROOM; i <= (int)ReverbMode.TAP_DELAY; i++)
            {
                reverbModels[i].Open();
            }
#endif
            if (isOpen)
            {
                ReverbOutputGain = reverbOutputGain;
                SetReverbEnabled(true);
            }
        }

        public static void MuteSampleBuffer(Sample[] buffer, int offset, int len)
        {
            if (buffer == null) return;
            Array.Clear(buffer, offset, len);
        }

        private bool LoadPCMROM(ROMImage pcmROMImage)
        {
            int[] order = { 0, 9, 1, 2, 3, 4, 5, 6, 7, 10, 11, 12, 13, 14, 15, 8 };

            var file = pcmROMImage.File;
            ROMInfo pcmROMInfo = pcmROMImage.ROMInfo;
            if ((pcmROMInfo == null)
                || (pcmROMInfo.Type != RomInfoType.PCM)
                || (pcmROMInfo.PairType != PairType.Full))
            {
                return false;
            }
#if MT32EMU_MONITOR_INIT
            PrintDebug("Found PCM ROM: {0}, {1}", pcmROMInfo.ShortName, pcmROMInfo.Description);
#endif
            var fileSize = file.Length;
            if (fileSize != (2 * pcmROMSize))
            {
#if MT32EMU_MONITOR_INIT
                PrintDebug("PCM ROM file has wrong size (expected {0}, got {1})", 2 * pcmROMSize, fileSize);
#endif
                return false;
            }

            var buffer = new byte[file.Length];
            file.Read(buffer, 0, buffer.Length);
            var f = 0;
            var fileData = buffer;
            for (var i = 0; i < pcmROMSize; i++)
            {
                byte s = fileData[f++];
                byte c = fileData[f++];


                short log = 0;
                for (int u = 0; u < 15; u++)
                {
                    int bit;
                    if (order[u] < 8)
                    {
                        bit = (s >> (7 - order[u])) & 0x1;
                    }
                    else {
                        bit = (c >> (7 - (order[u] - 8))) & 0x1;
                    }
                    log = (short)(log | (short)(bit << (15 - u)));
                }
                pcmROMData[i] = log;
            }

            return true;
        }

        private void InitMemoryRegions()
        {
            // Timbre max tables are slightly more complicated than the others, which are used directly from the ROM.
            // The ROM (sensibly) just has maximums for TimbreParam.commonParam followed by just one TimbreParam.partialParam,
            // so we produce a table with all partialParams filled out, as well as padding for PaddedTimbre, for quick lookup.
            paddedTimbreMaxTable = new byte[MemParams.PaddedTimbre.Size];
            Array.Copy(controlROMData, controlROMMap.timbreMaxTable, paddedTimbreMaxTable, 0, TimbreParam.CommonParam.Size + TimbreParam.PartialParam.Size); // commonParam and one partialParam
            int pos = TimbreParam.CommonParam.Size + TimbreParam.PartialParam.Size;
            for (int i = 0; i < 3; i++)
            {
                Array.Copy(controlROMData, controlROMMap.timbreMaxTable + TimbreParam.CommonParam.Size, paddedTimbreMaxTable, pos, TimbreParam.PartialParam.Size);
                pos += TimbreParam.PartialParam.Size;
            }
            var off = mt32ram.Offset;
            patchTempMemoryRegion = new PatchTempMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.patchMaxTable));
            off += MemParams.PatchTemp.Size * mt32ram.patchTemp.Length;
            rhythmTempMemoryRegion = new RhythmTempMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.rhythmMaxTable));
            off += MemParams.RhythmTemp.Size * mt32ram.rhythmTemp.Length;
            timbreTempMemoryRegion = new TimbreTempMemoryRegion(this, new BytePtr(mt32ram.Data, off), paddedTimbreMaxTable);
            off += TimbreParam.Size * mt32ram.timbreTemp.Length;
            patchesMemoryRegion = new PatchesMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.patchMaxTable));
            off += PatchParam.Size * mt32ram.patches.Length;
            timbresMemoryRegion = new TimbresMemoryRegion(this, new BytePtr(mt32ram.Data, off), paddedTimbreMaxTable);
            off += MemParams.PaddedTimbre.Size * mt32ram.timbres.Length;
            systemMemoryRegion = new SystemMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.systemMaxTable));
            displayMemoryRegion = new DisplayMemoryRegion(this);
            resetMemoryRegion = new ResetMemoryRegion(this);
        }

        private void SetReverbEnabled(bool newReverbEnabled)
        {
            if (IsReverbEnabled == newReverbEnabled) return;
            if (newReverbEnabled)
            {
                bool oldReverbOverridden = reverbOverridden;
                reverbOverridden = false;
                RefreshSystemReverbParameters();
                reverbOverridden = oldReverbOverridden;
            }
            else {
#if MT32EMU_REDUCE_REVERB_MEMORY
        reverbModel.close();
#endif
                reverbModel = null;
            }
        }

        private void RefreshSystemReverbParameters()
        {
#if MT32EMU_MONITOR_SYSEX
            PrintDebug(" Reverb: mode={0}, time={1}, level={2}", mt32ram.system.reverbMode, mt32ram.system.reverbTime, mt32ram.system.reverbLevel);
#endif
            if (reverbOverridden)
            {
#if MT32EMU_MONITOR_SYSEX
                PrintDebug(" (Reverb overridden - ignoring)");
#endif
                return;
            }
            //reportHandler.onNewReverbMode(mt32ram.system.reverbMode);
            //reportHandler.onNewReverbTime(mt32ram.system.reverbTime);
            //reportHandler.onNewReverbLevel(mt32ram.system.reverbLevel);

            BReverbModel oldReverbModel = reverbModel;
            if (mt32ram.system.reverbTime == 0 && mt32ram.system.reverbLevel == 0)
            {
                // Setting both time and level to 0 effectively disables wet reverb output on real devices.
                // Take a shortcut in this case to reduce CPU load.
                reverbModel = null;
            }
            else {
                reverbModel = reverbModels[mt32ram.system.reverbMode];
            }
            if (reverbModel != oldReverbModel)
            {
#if MT32EMU_REDUCE_REVERB_MEMORY
        if (oldReverbModel != null) {
            oldReverbModel.close();
        }
        if (isReverbEnabled()) {
            reverbModel.open();
        }
#else
                if (IsReverbEnabled)
                {
                    reverbModel.Mute();
                }
#endif
            }
            if (IsReverbEnabled)
            {
                reverbModel.SetParameters(mt32ram.system.reverbTime, mt32ram.system.reverbLevel);
            }
        }

        internal void PrintDebug(string format, params object[] args)
        {
            Debug(4, format, args);
        }

        private bool LoadControlROM(ROMImage controlROMImage)
        {
            var file = controlROMImage.File;
            ROMInfo controlROMInfo = controlROMImage.ROMInfo;
            if ((controlROMInfo == null)
                || (controlROMInfo.Type != RomInfoType.Control)
                || (controlROMInfo.PairType != PairType.Full))
            {
                return false;
            }
            controlROMFeatures = controlROMImage.ROMInfo.ControlROMFeatures;
            if (controlROMFeatures == null)
            {
#if MT32EMU_MONITOR_INIT
                PrintDebug("Invalid Control ROM Info provided without feature set");
#endif
                return false;
            }

#if MT32EMU_MONITOR_INIT
            PrintDebug("Found Control ROM: %s, %s", controlROMInfo.ShortName, controlROMInfo.Description);
#endif
            file.Read(controlROMData, 0, CONTROL_ROM_SIZE);

            // Control ROM successfully loaded, now check whether it's a known type
            controlROMMap = null;
            for (int i = 0; i < ControlROMMaps.Length; i++)
            {
                if (ScummHelper.ArrayEquals(controlROMData, ControlROMMaps[i].idPos, ScummHelper.GetBytes(ControlROMMaps[i].idBytes), 0, ControlROMMaps[i].idLen))
                {
                    controlROMMap = ControlROMMaps[i];
                    return true;
                }
            }
#if MT32EMU_MONITOR_INIT
            PrintDebug("Control ROM failed to load");
#endif
            return false;
        }

        public void PolyStateChanged(int partNum)
        {
            //reportHandler.onPolyStateChanged(partNum);
        }

    }
}
