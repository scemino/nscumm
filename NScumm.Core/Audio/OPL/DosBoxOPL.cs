//
//  DosBoxOPL.cs
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

#define DBOPL_WAVE_EQUALS_WAVE_TABLEMUL

using System;
using NScumm.Core.Audio.OPL;
using System.Runtime.InteropServices;

namespace NScumm.Core.Audio.OPL
{
    class OplTimer
    {
        public double startTime;
        public double delay;
        public bool enabled, overflow, masked;
        public byte counter;

        //Call update before making any further changes
        public void Update(double time)
        {
            if (!enabled || delay == 0)
                return;
            double deltaStart = time - startTime;
            // Only set the overflow flag when not masked
            if (deltaStart >= 0 && !masked)
                overflow = true;
        }

        //On a reset make sure the start is in sync with the next cycle
        public void Reset(double time)
        {
            overflow = false;
            if (delay == 0 || !enabled)
                return;
            double delta = (time - startTime);
//            double rem = fmod(delta, delay);
            double rem = delta % delay;
            double next = delay - rem;
            startTime = time + next;
        }

        public void Stop()
        {
            enabled = false;
        }

        public void Start(double time, int scale)
        {
            //Don't enable again
            if (enabled)
                return;
            enabled = true;
            delay = 0.001 * (256 - counter) * scale;
            startTime = time + delay;
        }
    }


    class OplChip
    {
        //Last selected register
        OplTimer[] timer = new OplTimer[2];

        //Check for it being a write to the timer
        public bool Write(uint reg, byte val)
        {
            switch (reg)
            {
                case 0x02:
                    timer[0].counter = val;
                    return true;
                case 0x03:
                    timer[1].counter = val;
                    return true;
                case 0x04:
                    double time = Environment.TickCount / 1000.0;

                    if ((val & 0x80) != 0)
                    {
                        timer[0].Reset(time);
                        timer[1].Reset(time);
                    }
                    else
                    {
                        timer[0].Update(time);
                        timer[1].Update(time);

                        if ((val & 0x1) != 0)
                            timer[0].Start(time, 80);
                        else
                            timer[0].Stop();

                        timer[0].masked = (val & 0x40) > 0;

                        if (timer[0].masked)
                            timer[0].overflow = false;

                        if ((val & 0x2) != 0)
                            timer[1].Start(time, 320);
                        else
                            timer[1].Stop();

                        timer[1].masked = (val & 0x20) > 0;

                        if (timer[1].masked)
                            timer[1].overflow = false;
                    }
                    return true;
            }
            return false;
        }
        //Read the current timer state, will use current double
        public byte read()
        {
            double time = Environment.TickCount / 1000.0;

            timer[0].Update(time);
            timer[1].Update(time);

            byte ret = 0;
            // Overflow won't be set if a channel is masked
            if (timer[0].overflow)
            {
                ret |= 0x40;
                ret |= 0x80;
            }
            if (timer[1].overflow)
            {
                ret |= 0x20;
                ret |= 0x80;
            }
            return ret;
        }
    }

    partial class DosBoxOPL: IOpl
    {
        uint _rate;
        OplType _type;

        Chip _emulator;
        OplChip[] _chip = new OplChip[2];

        [StructLayout(LayoutKind.Explicit)]
        struct Reg
        {
            [FieldOffset(0)]
            public byte dual1;

            [FieldOffset(1)]
            public byte dual2;

            [FieldOffset(0)]
            public ushort normal;
        }

        Reg _reg;

        public DosBoxOPL(OplType type)
        {
            _type = type;
        }

        #region IOpl implementation

        public bool Init(uint rate)
        {
            Free();

            for (int i = 0; i < _chip.Length; i++)
            {
                _chip[i] = new OplChip();
            }
            _emulator = new Chip();
            if (_emulator == null)
                return false;

            InitTables();
            _emulator.Setup(rate);

            if (_type == OplType.DualOpl2)
            {
                // Setup opl3 mode in the hander
                _emulator.WriteReg(0x105, 1);
            }

            _rate = rate;
            return true;
        }

        public void Reset()
        {
            Init(_rate);
        }

        public void Write(int port, int val)
        {
            if ((port & 1) != 0)
            {
                switch (_type)
                {
                    case OplType.Opl2:
                    case OplType.Opl3:
                        if (!_chip[0].Write(_reg.normal, (byte)val))
                            _emulator.WriteReg(_reg.normal, (byte)val);
                        break;
                    case OplType.DualOpl2:
                            // Not a 0x??8 port, then write to a specific port
                        if (0 == (port & 0x8))
                        {
                            byte index = (byte)((port & 2) >> 1);
                            DualWrite(index, index == 0 ? _reg.dual1 : _reg.dual2, (byte)val);
                        }
                        else
                        {
                            //Write to both ports
                            DualWrite(0, _reg.dual1, (byte)val);
                            DualWrite(1, _reg.dual2, (byte)val);
                        }
                        break;
                }
            }
            else
            {
                // Ask the handler to write the address
                // Make sure to clip them in the right range
                switch (_type)
                {
                    case OplType.Opl2:
                        _reg.normal = (ushort)(_emulator.WriteAddr((uint)port, (byte)val) & 0xff);
                        break;
                    case OplType.Opl3:
                        _reg.normal = (ushort)(_emulator.WriteAddr((uint)port, (byte)val) & 0x1ff);
                        break;
                    case OplType.DualOpl2:
                            // Not a 0x?88 port, when write to a specific side
                        if (0 == (port & 0x8))
                        {
                            byte index = (byte)((port & 2) >> 1);
                            if (index == 0)
                            {
                                _reg.dual1 = (byte)(val & 0xff);
                            }
                            else
                            {
                                _reg.dual2 = (byte)(val & 0xff);
                            }
                        }
                        else
                        {
                            _reg.dual1 = (byte)(val & 0xff);
                            _reg.dual2 = (byte)(val & 0xff);
                        }
                        break;
                }
            }
        }

        public byte Read(int port)
        {
            switch (_type)
            {
                case OplType.Opl2:
                    if (0 == (port & 1))
                        //Make sure the low bits are 6 on opl2
                        return (byte)(_chip[0].read() | 0x6);
                    break;
                case OplType.Opl3:
                    if (0 == (port & 1))
                        return _chip[0].read();
                    break;
                case OplType.DualOpl2:
                    // Only return for the lower ports
                    if ((port & 1) != 0)
                        return 0xff;
                    // Make sure the low bits are 6 on opl2
                    return (byte)(_chip[(port >> 1) & 1].read() | 0x6);
            }
            return 0;
        }

        public void WriteReg(int r, int v)
        {
            int tempReg = 0;
            switch (_type)
            {
                case OplType.Opl2:
                case OplType.DualOpl2:
                case OplType.Opl3:
                    // We can't use _handler->writeReg here directly, since it would miss timer changes.

                    // Backup old setup register
                    tempReg = _reg.normal;

                    // We directly allow writing to secondary OPL3 registers by using
                    // register values >= 0x100.
                    if (_type == OplType.Opl3 && r >= 0x100)
                    {
                        // We need to set the register we want to write to via port 0x222,
                        // since we want to write to the secondary register set.
                        Write(0x222, r);
                        // Do the real writing to the register
                        Write(0x223, v);
                    }
                    else
                    {
                        // We need to set the register we want to write to via port 0x388
                        Write(0x388, r);
                        // Do the real writing to the register
                        Write(0x389, v);
                    }

                    // Restore the old register
                    if (_type == OplType.Opl3 && tempReg >= 0x100)
                    {
                        Write(0x222, tempReg & ~0x100);
                    }
                    else
                    {
                        Write(0x388, tempReg);
                    }
                    break;
            }
        }

        public void DualWrite(byte index, byte reg, byte val)
        {
            // Make sure you don't use opl3 features
            // Don't allow write to disable opl3
            if (reg == 5)
                return;

            // Only allow 4 waveforms
            if (reg >= 0xE0 && reg <= 0xE8)
                val &= 3;

            // Write to the timer?
            if (_chip[index].Write(reg, val))
                return;

            // Enabling panning
            if (reg >= 0xC0 && reg <= 0xC8)
            {
                val &= 15;
                val |= (byte)(index != 0 ? 0xA0 : 0x50);
            }

            uint fullReg = (uint)(reg + (index != 0 ? 0x100 : 0));
            _emulator.WriteReg(fullReg, val);
        }

        public void ReadBuffer(short[] buffer, int pos, int length)
        {
            // For stereo OPL cards, we divide the sample count by 2,
            // to match stereo AudioStream behavior.
            if (_type != OplType.Opl2)
                length >>= 1;

            const uint bufferLength = 512;
            int[] tempBuffer = new int[bufferLength * 2];

            if (_emulator.Opl3Active != 0)
            {
                while (length > 0)
                {
                    uint readSamples = (uint)Math.Min(length, bufferLength);

                    _emulator.GenerateBlock3(readSamples, tempBuffer);

                    for (uint i = 0; i < (readSamples << 1); ++i)
                        buffer[pos + i] = (short)tempBuffer[i];

                    pos += (int)(readSamples << 1);
                    length -= (int)readSamples;
                }
            }
            else
            {
                while (length > 0)
                {
                    uint readSamples = (uint)Math.Min(length, bufferLength << 1);

                    _emulator.GenerateBlock2(readSamples, tempBuffer);

                    for (uint i = 0; i < readSamples; ++i)
                        buffer[pos + i] = (short)tempBuffer[i];

                    pos += (int)readSamples;
                    length -= (int)readSamples;
                }
            }
        }

        public bool IsStereo
        {
            get
            {
                return _type != OplType.Opl2;
            }
        }

        #endregion

        void Free()
        {
            _emulator = null;
        }

        static bool doneTables = false;

        static void InitTables()
        {
            if (doneTables)
                return;
            doneTables = true;
            #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER ) || ( DBOPL_WAVE_EQUALS_WAVE_TABLELOG )
            //Exponential volume table, same as the real adlib
            for (int i = 0; i < 256; i++)
            {
                //Save them in reverse
                ExpTable[i] = (ushort)(0.5 + (Math.Pow(2.0, (255 - i) * (1.0 / 256)) - 1) * 1024);
                ExpTable[i] += 1024; //or remove the -1 oh well :)
                //Preshift to the left once so the final volume can shift to the right
                ExpTable[i] *= 2;
            }
            #endif
            #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER )
            //Add 0.5 for the trunc rounding of the integer cast
            //Do a PI sinetable instead of the original 0.5 PI
            for (int i = 0; i < 512; i++)
            {
                SinTable[i] = (ushort)(0.5 - Math.Log10(Math.Sin((i + 0.5) * (Math.PI / 512.0))) / Math.Log10(2.0) * 256);
            }
            #endif
            #if DBOPL_WAVE_EQUALS_WAVE_TABLEMUL
            //Multiplication based tables
            for (int i = 0; i < 384; i++)
            {
                int s = i * 8;
                //TODO maybe keep some of the precision errors of the original table?
                double val = (0.5 + (Math.Pow(2.0, -1.0 + (255 - s) * (1.0 / 256))) * (1 << MUL_SH));
                MulTable[i] = (ushort)(val);
            }

            //Sine Wave Base
            for (int i = 0; i < 512; i++)
            {
                WaveTable[0x0200 + i] = (short)(Math.Sin((i + 0.5) * (Math.PI / 512.0)) * 4084);
                WaveTable[0x0000 + i] = (short)-WaveTable[0x200 + i];
            }
            //Exponential wave
            for (int i = 0; i < 256; i++)
            {
                WaveTable[0x700 + i] = (short)(0.5 + (Math.Pow(2.0, -1.0 + (255 - i * 8) * (1.0 / 256))) * 4085);
                WaveTable[0x6ff - i] = (short)-WaveTable[0x700 + i];
            }
            #endif
            #if ( DBOPL_WAVE_EQUALS_WAVE_TABLELOG )
            //Sine Wave Base
            for (int i = 0; i < 512; i++)
            {
                WaveTable[0x0200 + i] = (short)(0.5 - Math.Log10(Math.Sin((i + 0.5) * (Math.PI / 512.0))) / Math.Log10(2.0) * 256);
                WaveTable[0x0000 + i] = (short)((0x8000) | WaveTable[0x200 + i]);
            }
            //Exponential wave
            for (int i = 0; i < 256; i++)
            {
                WaveTable[0x700 + i] = (short)(i * 8);
                WaveTable[0x6ff - i] = (short)unchecked(((short)0x8000) | i * 8);
            }
            #endif

            //  |    |//\\|____|WAV7|//__|/\  |____|/\/\|
            //  |\\//|    |    |WAV7|    |  \/|    |    |
            //  |06  |0126|27  |7   |3   |4   |4 5 |5   |

            #if (( DBOPL_WAVE_EQUALS_WAVE_TABLELOG ) || ( DBOPL_WAVE_EQUALS_WAVE_TABLEMUL ))
            for (int i = 0; i < 256; i++)
            {
                //Fill silence gaps
                WaveTable[0x400 + i] = WaveTable[0];
                WaveTable[0x500 + i] = WaveTable[0];
                WaveTable[0x900 + i] = WaveTable[0];
                WaveTable[0xc00 + i] = WaveTable[0];
                WaveTable[0xd00 + i] = WaveTable[0];
                //Replicate sines in other pieces
                WaveTable[0x800 + i] = WaveTable[0x200 + i];
                //double speed sines
                WaveTable[0xa00 + i] = WaveTable[0x200 + i * 2];
                WaveTable[0xb00 + i] = WaveTable[0x000 + i * 2];
                WaveTable[0xe00 + i] = WaveTable[0x200 + i * 2];
                WaveTable[0xf00 + i] = WaveTable[0x200 + i * 2];
            }
            #endif

            //Create the ksl table
            for (int oct = 0; oct < 8; oct++)
            {
                int @base = oct * 8;
                for (int i = 0; i < 16; i++)
                {
                    int val = @base - KslCreateTable[i];
                    if (val < 0)
                        val = 0;
                    //*4 for the final range to match attenuation range
                    KslTable[oct * 16 + i] = (byte)(val * 4);
                }
            }
            //Create the Tremolo table, just increase and decrease a triangle wave
            for (byte i = 0; i < TREMOLO_TABLE / 2; i++)
            {
                byte val = (byte)(i << Envelope.ENV_EXTRA);
                TremoloTable[i] = val;
                TremoloTable[TREMOLO_TABLE - 1 - i] = val;
            }

            #if false
            //Stupid checks if table's are correct
            for ( uint i = 0; i < 18; i++ ) {
            Bit32u find = (Bit16u)( &(chip.chan[ i ]) );
            for ( uint c = 0; c < 32; c++ ) {
            if ( ChanOffsetTable[c] == find ) {
            find = 0;
            break;
            }
            }
            if ( find ) {
            find = find;
            }
            }
            for ( uint i = 0; i < 36; i++ ) {
            Bit32u find = (Bit16u)( &(chip.chan[ i / 2 ].op[i % 2]) );
            for ( uint c = 0; c < 64; c++ ) {
            if ( OpOffsetTable[c] == find ) {
            find = 0;
            break;
            }
            }
            if ( find ) {
            find = find;
            }
            }
            #endif
        }
    }
}

