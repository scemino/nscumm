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

using System;

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
                public readonly BytePtr time;
                // 0-100 (-50 - +50) // [3]: SUSTAIN LEVEL, [4]: END LEVEL
                public readonly BytePtr level;

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
                    envLevel = new BytePtr(Data, Offset + 5);
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
                    envLevel = new BytePtr(Data, Offset + 8);
                }
            }

            public readonly WGParam wg;
            public readonly PitchEnvParam pitchEnv;
            public readonly PitchLFOParam pitchLFO;
            public readonly TVFParam tvf;
            public readonly TVAParam tva;

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

            public PatchParam Patch { get; }
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

            public BytePtr Dummy { get; }

            public PatchTemp(byte[] data, int offset)
            {
                Data = data;
                Offset = offset;
                Patch = new PatchParam(Data, Offset);
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

            public byte[] Data;
            public int Offset;

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
            public readonly BytePtr chanAssign;

            // MASTER VOLUME 0-100
            public byte masterVol
            {
                get { return Data[Offset + 22]; }
                set { Data[Offset + 22] = value; }
            }

            public System(byte[] data, int offset)
            {
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

        public readonly System system;

        public MemParams()
            : this(new byte[Size], 0)
        {
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

    class Synth
    {
        public const int MAX_SYSEX_SIZE = 512; // FIXME: Does this correspond to a real MIDI buffer used in h/w devices?
        private const int CONTROL_ROM_SIZE = 64 * 1024;

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

        //PCMWaveEntry pcmWaves; // Array

        ControlROMFeatureSet controlROMFeatures;
        ControlROMMap controlROMMap;
        readonly byte[] controlROMData = new byte[CONTROL_ROM_SIZE];
        short[] pcmROMData;
        int pcmROMSize; // This is in 16-bit samples, therefore half the number of bytes in the ROM

        uint partialCount;
        sbyte[] chantable = new sbyte[32]; // FIXME: Need explanation why 32 is set, obviously it should be 16

        //MidiEventQueue midiQueue;
        volatile uint lastReceivedMIDIEventTimestamp;
        volatile uint renderedSampleCount;

        MemParams mt32ram = new MemParams();
        MemParams mt32default;

        BReverbModel[] reverbModels = new BReverbModel[4];
        //BReverbModel reverbModel;
        bool reverbOverridden;

        MIDIDelayMode midiDelayMode;
        DACInputMode dacInputMode;

        float outputGain;
        float reverbOutputGain;

        bool reversedStereoEnabled;

        bool isOpen;

        bool isDefaultReportHandler;
        //ReportHandler reportHandler;

        //PartialManager partialManager;
        //Part[] parts=new Part[9];

        // When a partial needs to be aborted to free it up for use by a new Poly,
        // the controller will busy-loop waiting for the sound to finish.
        // We emulate this by delaying new MIDI events processing until abortion finishes.
        Poly abortingPoly;

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

        public bool ReverbCompatibilityMode
        {
            get { throw new NotImplementedException(); }
            private set
            {
                throw new NotImplementedException();
                //if (reverbModels[(int)ReverbMode.ROOM] != null)
                //{
                //    if (IsMT32ReverbCompatibilityMode == value) return;
                //    ReverbEnabled = false;
                //    for (int i = 0; i < 4; i++)
                //    {
                //        delete reverbModels[i];
                //    }
                //}
                //                reverbModels[(int)ReverbMode.ROOM] = new BReverbModel(ReverbMode.ROOM, value);
                //                reverbModels[(int)ReverbMode.HALL] = new BReverbModel(ReverbMode.HALL, value);
                //                reverbModels[(int)ReverbMode.PLATE] = new BReverbModel(ReverbMode.PLATE, value);
                //                reverbModels[(int)ReverbMode.TAP_DELAY] = new BReverbModel(ReverbMode.TAP_DELAY, value);
                //#if !MT32EMU_REDUCE_REVERB_MEMORY
                //                for (int i = (int)ReverbMode.ROOM; i <= (int)ReverbMode.TAP_DELAY; i++)
                //                {
                //                    reverbModels[i].Open();
                //                }
                //#endif
                //                if (isOpen)
                //                {
                //                    ReverbOutputGain = reverbOutputGain;
                //                    ReverbEnabled = true;
                //                }
            }
        }

        public Synth()
        {
            partialCount = Mt32Emu.DEFAULT_MAX_PARTIALS;

            DACInputMode = DACInputMode.NICE;
            MIDIDelayMode = MIDIDelayMode.DELAY_SHORT_MESSAGES_ONLY;
            OutputGain = 1.0f;
            ReverbOutputGain = 1.0f;
        }

        public static float ClipSampleEx(float sampleEx)
        {
            return sampleEx;
        }

        public bool Open(ROMImage controlROMImage, ROMImage pcmROMImage, AnalogOutputMode analogOutputMode)
        {
            return Open(controlROMImage, pcmROMImage, Mt32Emu.DEFAULT_MAX_PARTIALS, analogOutputMode);
        }

        public bool Open(ROMImage controlROMImage, ROMImage pcmROMImage, uint usePartialCount = Mt32Emu.DEFAULT_MAX_PARTIALS, AnalogOutputMode analogOutputMode = AnalogOutputMode.COARSE)
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
    printDebug("Loading Control ROM");
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
    printDebug("Loading PCM ROM");
#endif
            if (!LoadPCMROM(pcmROMImage))
            {
                PrintDebug("Init Error - Missing PCM ROM image");
                //ReportHandler.onErrorPCMROM();
                return false;
            }

#if MT32EMU_MONITOR_INIT
    printDebug("Initialising Reverb Models");
#endif
            bool mt32CompatibleReverb = controlROMFeatures.IsDefaultReverbMT32Compatible;
#if MT32EMU_MONITOR_INIT
    printDebug("Using %s Compatible Reverb Models", mt32CompatibleReverb ? "MT-32" : "CM-32L");
#endif
            ReverbCompatibilityMode = mt32CompatibleReverb;

#if MT32EMU_MONITOR_INIT
    printDebug("Initialising Timbre Bank A");
#endif
            if (!InitTimbres(controlROMMap.timbreAMap, controlROMMap.timbreAOffset, 0x40, 0, controlROMMap.timbreACompressed))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
    printDebug("Initialising Timbre Bank B");
#endif
            if (!InitTimbres(controlROMMap.timbreBMap, controlROMMap.timbreBOffset, 0x40, 64, controlROMMap.timbreBCompressed))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
    printDebug("Initialising Timbre Bank R");
#endif
            if (!InitTimbres(controlROMMap.timbreRMap, 0, controlROMMap.timbreRCount, 192, true))
            {
                return false;
            }

#if MT32EMU_MONITOR_INIT
    printDebug("Initialising Timbre Bank M");
#endif
            // TODO: CM-64 seems to initialise all bytes in this bank to 0.
            //            memset(&mt32ram.timbres[128], 0, sizeof(mt32ram.timbres[128]) * 64);

            //            partialManager = new PartialManager(this, parts);

            //            pcmWaves = new PCMWaveEntry[controlROMMap.pcmCount];

            //#if MT32EMU_MONITOR_INIT
            //    printDebug("Initialising PCM List");
            //#endif
            //            InitPCMList(controlROMMap.pcmTable, controlROMMap.pcmCount);

            //#if MT32EMU_MONITOR_INIT
            //    printDebug("Initialising Rhythm Temp");
            //#endif
            //            memcpy(mt32ram.rhythmTemp, &controlROMData[controlROMMap.rhythmSettings], controlROMMap.rhythmSettingsCount * 4);

            //#if MT32EMU_MONITOR_INIT
            //    printDebug("Initialising Patches");
            //#endif
            //            for (byte i = 0; i < 128; i++)
            //            {
            //                PatchParam patch = mt32ram.patches[i];
            //                patch.timbreGroup = i / 64;
            //                patch.timbreNum = i % 64;
            //                patch.keyShift = 24;
            //                patch.fineTune = 50;
            //                patch.benderRange = 12;
            //                patch.assignMode = 0;
            //                patch.reverbSwitch = 1;
            //                patch.dummy = 0;
            //            }

            //#if MT32EMU_MONITOR_INIT
            //    printDebug("Initialising System");
            //#endif
            //            // The MT-32 manual claims that "Standard pitch" is 442Hz.
            //            mt32ram.system.masterTune = 0x4A; // Confirmed on CM-64
            //            mt32ram.system.reverbMode = 0; // Confirmed
            //            mt32ram.system.reverbTime = 5; // Confirmed
            //            mt32ram.system.reverbLevel = 3; // Confirmed
            //            memcpy(mt32ram.system.reserveSettings, &controlROMData[controlROMMap.reserveSettings], 9); // Confirmed
            //            for (byte i = 0; i < 9; i++)
            //            {
            //                // This is the default: {1, 2, 3, 4, 5, 6, 7, 8, 9}
            //                // An alternative configuration can be selected by holding "Master Volume"
            //                // and pressing "PART button 1" on the real MT-32's frontpanel.
            //                // The channel assignment is then {0, 1, 2, 3, 4, 5, 6, 7, 9}
            //                mt32ram.system.chanAssign[i] = i + 1;
            //            }
            //            mt32ram.system.masterVol = 100; // Confirmed

            //            bool oldReverbOverridden = reverbOverridden;
            //            reverbOverridden = false;
            //            RefreshSystem();
            //            reverbOverridden = oldReverbOverridden;

            //            for (int i = 0; i < 9; i++)
            //            {
            //                MemParams::PatchTemp* patchTemp = &mt32ram.patchTemp[i];

            //                // Note that except for the rhythm part, these patch fields will be set in setProgram() below anyway.
            //                patchTemp.patch.timbreGroup = 0;
            //                patchTemp.patch.timbreNum = 0;
            //                patchTemp.patch.keyShift = 24;
            //                patchTemp.patch.fineTune = 50;
            //                patchTemp.patch.benderRange = 12;
            //                patchTemp.patch.assignMode = 0;
            //                patchTemp.patch.reverbSwitch = 1;
            //                patchTemp.patch.dummy = 0;

            //                patchTemp.outputLevel = 80;
            //                patchTemp.panpot = controlROMData[controlROMMap.panSettings + i];
            //                memset(patchTemp.dummyv, 0, sizeof(patchTemp.dummyv));
            //                patchTemp.dummyv[1] = 127;

            //                if (i < 8)
            //                {
            //                    parts[i] = new Part(this, i);
            //                    parts[i].setProgram(controlROMData[controlROMMap.programSettings + i]);
            //                }
            //                else {
            //                    parts[i] = new RhythmPart(this, i);
            //                }
            //            }

            //            // For resetting mt32 mid-execution
            //            mt32default = mt32ram;

            //            midiQueue = new MidiEventQueue();

            //            analog = new Analog(analogOutputMode, controlROMFeatures);
            //            setOutputGain(outputGain);
            //            setReverbOutputGain(reverbOutputGain);

            //            isOpen = true;
            //            isEnabled = false;

            //#if MT32EMU_MONITOR_INIT
            //    printDebug("*** Initialisation complete ***");
            //#endif
            return true;
        }

        bool InitTimbres(ushort timbreAMap, ushort timbreAOffset, int v1, int v2, bool timbreACompressed)
        {
            throw new NotImplementedException();
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
    printDebug("Found PCM ROM: %s, %s", pcmROMInfo.shortName, pcmROMInfo.description);
#endif
            var fileSize = file.Length;
            if (fileSize != (2 * pcmROMSize))
            {
#if MT32EMU_MONITOR_INIT
        printDebug("PCM ROM file has wrong size (expected %d, got %d)", 2 * pcmROMSize, fileSize);
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
            off += MemParams.RhythmTemp.Size * mt32ram.timbreTemp.Length;
            patchesMemoryRegion = new PatchesMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.patchMaxTable));
            off += MemParams.RhythmTemp.Size * mt32ram.patches.Length;
            timbresMemoryRegion = new TimbresMemoryRegion(this, new BytePtr(mt32ram.Data, off), paddedTimbreMaxTable);
            off += MemParams.RhythmTemp.Size * mt32ram.timbres.Length;
            systemMemoryRegion = new SystemMemoryRegion(this, new BytePtr(mt32ram.Data, off), new BytePtr(controlROMData, controlROMMap.systemMaxTable));
            displayMemoryRegion = new DisplayMemoryRegion(this);
            resetMemoryRegion = new ResetMemoryRegion(this);
        }

        void PrintDebug(string v)
        {
            throw new NotImplementedException();
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
        printDebug("Invalid Control ROM Info provided without feature set");
#endif
                return false;
            }

#if MT32EMU_MONITOR_INIT
    printDebug("Found Control ROM: %s, %s", controlROMInfo.shortName, controlROMInfo.description);
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
    printDebug("Control ROM failed to load");
#endif
            return false;
        }

    }
}
