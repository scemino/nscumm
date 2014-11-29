//
//  DosBoxOPL_Operator.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

//Use 8 handlers based on a small logatirmic wavetabe and an exponential table for volume
#define WAVE_HANDLER
//Use a logarithmic wavetable with an exponential table for volume
#define WAVE_TABLELOG
//Use a linear wavetable with a multiply table for volume
#define WAVE_TABLEMUL

//Select the type of wave generator routine
#define DBOPL_WAVE_EQUALS_WAVE_TABLEMUL

using System;

namespace NScumm.Core.Audio.OPL
{
    partial class DosBoxOPL
    {
        const double OPLRATE = (14318180.0 / 288.0);
        const int TREMOLO_TABLE = 52;

        //Try to use most precision for frequencies
        //Else try to keep different waves in synch
        //#define WAVE_PRECISION;
        #if !WAVE_PRECISION
        //Wave bits available in the top of the 32bit range
        //Original adlib uses 10.10, we use 10.22
        const int WAVE_BITS = 10;
        #else
        //Need some extra bits at the top to have room for octaves and frequency multiplier
        //We support to 8 times lower rate
        //128 * 15 * 8 = 15350, 2^13.9, so need 14 bits
        const int WAVE_BITS   =14;
        #endif
        const int WAVE_SH = (32 - WAVE_BITS);
        const int WAVE_MASK = ((1 << WAVE_SH) - 1);

        //Use the same accuracy as the waves
        const int LFO_SH = (WAVE_SH - 10);
        //LFO is controlled by our tremolo 256 sample limit
        const int LFO_MAX = (256 << (LFO_SH));

        //Maximum amount of attenuation bits
        //Envelope goes to 511, 9 bits
        #if (DBOPL_WAVE_EQUALS_WAVE_TABLEMUL )
        //Uses the value directly
        public const int ENV_BITS = (9);

        #else
        //Add 3 bits here for more accuracy and would have to be shifted up either way
        const int ENV_BITS = (9);
        #endif

        //Limits of the envelope with those bits and when the envelope goes silent
        public const int ENV_MIN = 0;
        public const int ENV_EXTRA = (ENV_BITS - 9);
        public const int ENV_MAX = (511 << ENV_EXTRA);
        public const int ENV_LIMIT = ((12 * 256) >> (3 - ENV_EXTRA));

        public static bool ENV_SILENT(int x)
        {
            return x >= ENV_LIMIT;
        }

        //Attack/decay/release rate counter shift
        const int RATE_SH = 24;
        const int RATE_MASK = ((1 << RATE_SH) - 1);

        //Has to fit within 16bit lookuptable
        const int MUL_SH = 16;


        #if (DBOPL_WAVE == WAVE_HANDLER)
        delegate int WaveHandler(uint i,uint volume);
        #endif

        delegate int VolumeHandler();

        delegate Channel SynthHandler(Chip chip,uint samples,int[] output,int pos);

        #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER ) || ( DBOPL_WAVE_EQUALS_WAVE_TABLELOG )
        static ushort[] ExpTable = new ushort[256];
        #endif

        #if DBOPL_WAVE_EQUALS_WAVE_HANDLER
        static ushort[] SinTable = new ushort[512];
        #endif

        static ushort[] MulTable = new ushort[384];

        //Layout of the waveform table in 512 entry intervals
        //With overlapping waves we reduce the table to half it's size

        //  |    |//\\|____|WAV7|//__|/\  |____|/\/\|
        //  |\\//|    |    |WAV7|    |  \/|    |    |
        //  |06  |0126|17  |7   |3   |4   |4 5 |5   |

        //6 is just 0 shifted and masked

        static short[] WaveTable = new short[8 * 512];
        //Distance into WaveTable the wave starts
        static readonly ushort[] WaveBaseTable =
            {
                0x000, 0x200, 0x200, 0x800,
                0xa00, 0xc00, 0x100, 0x400,
            };
        //Where to start the counter on at keyon
        static readonly ushort[] WaveStartTable =
            {
                512, 0, 0, 0,
                0, 512, 512, 256,
            };
        //Mask the counter with this
        static readonly ushort[] WaveMaskTable =
            {
                1023, 1023, 511, 511,
                1023, 1023, 512, 1023,
            };

        static byte[] KslTable = new byte[8 * 16];

        //How much to substract from the base value for the final attenuation
        static readonly byte[] KslCreateTable =
            {
                //0 will always be be lower than 7 * 8
                64, 32, 24, 19,
                16, 12, 11, 10,
                8,  6,  5,  4,
                3,  2,  1,  0,
            };

        static byte[] TremoloTable = new byte[ TREMOLO_TABLE ];
        //Start of a channel behind the chip struct start
        static Func<Chip,Channel>[] ChanOffsetTable = new Func<Chip,Channel>[32];
        //Start of an operator behind the chip struct start
        static Func<Chip,Operator>[] OpOffsetTable = new Func<Chip,Operator>[64];

        static byte M(double x)
        {
            return (byte)(x * 2);
        }

        static readonly byte[] FreqCreateTable =
            {
                M(0.5), M(1), M(2), M(3), M(4), M(5), M(6), M(7),
                M(8), M(9), M(10), M(10), M(12), M(12), M(15), M(15)
            };

        //Generate a table index and table shift value using input value from a selected rate
        static void EnvelopeSelect(byte val, out byte index, out byte shift)
        {
            if (val < 13 * 4)
            {               //Rate 0 - 12
                shift = (byte)(12 - (val >> 2));
                index = (byte)(val & 3);
            }
            else if (val < 15 * 4)
            {        //rate 13 - 14
                shift = 0;
                index = (byte)(val - 12 * 4);
            }
            else
            {                            //rate 15 and up
                shift = 0;
                index = 12;
            }
        }

        //On a real opl these values take 8 samples to reach and are based upon larger tables
        static readonly byte[] EnvelopeIncreaseTable =
            {
                4,  5,  6,  7,
                8, 10, 12, 14,
                16, 20, 24, 28,
                32,
            };

        //We're not including the highest attack rate, that gets a special value
        static readonly byte[] AttackSamplesTable =
            {
                69, 55, 46, 40,
                35, 29, 23, 20,
                19, 15, 11, 10,
                9
            };

        //Different synth modes that can generate blocks of data
        enum SynthMode
        {
            Sm2AM,
            Sm2FM,
            Sm3AM,
            Sm3FM,
            Sm4Start,
            Sm3FMFM,
            Sm3AMFM,
            Sm3FMAM,
            Sm3AMAM,
            Sm6Start,
            Sm2Percussion,
            Sm3Percussion
        }
    }
}

