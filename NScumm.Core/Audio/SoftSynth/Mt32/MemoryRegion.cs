//
//  MemoryRegion.cs
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
    enum MemoryRegionType
    {
        MR_PatchTemp, MR_RhythmTemp, MR_TimbreTemp, MR_Patches, MR_Timbres, MR_System, MR_Display, MR_Reset
    }

    class PatchTempMemoryRegion : MemoryRegion
    {
        public PatchTempMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
            : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_PatchTemp, Mt32Emu.MT32EMU_MEMADDR(0x030000), MemParams.PatchTemp.Size, 9)
        {
        }
    }

    class RhythmTempMemoryRegion : MemoryRegion
    {
        public RhythmTempMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
            : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_RhythmTemp, Mt32Emu.MT32EMU_MEMADDR(0x030110), MemParams.RhythmTemp.Size, 85) { }
    }

    class TimbreTempMemoryRegion : MemoryRegion
    {
        public TimbreTempMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
            : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_TimbreTemp, Mt32Emu.MT32EMU_MEMADDR(0x040000), TimbreParam.Size, 8) { }
    }
    class PatchesMemoryRegion : MemoryRegion
    {
        public PatchesMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
            : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_Patches, Mt32Emu.MT32EMU_MEMADDR(0x050000), PatchParam.Size, 128) { }
    }
    class TimbresMemoryRegion : MemoryRegion
    {
        public TimbresMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
            : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_Timbres, Mt32Emu.MT32EMU_MEMADDR(0x080000), MemParams.PaddedTimbre.Size, 64 + 64 + 64 + 64) { }
    }
    class SystemMemoryRegion : MemoryRegion
    {
        public SystemMemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable)
                    : base(useSynth, useRealMemory, useMaxTable, MemoryRegionType.MR_System, Mt32Emu.MT32EMU_MEMADDR(0x100000), MemParams.System.Size, 1)
        { }
    }
    class DisplayMemoryRegion : MemoryRegion
    {
        public DisplayMemoryRegion(Synth useSynth)
                    : base(useSynth, BytePtr.Null, BytePtr.Null, MemoryRegionType.MR_Display, Mt32Emu.MT32EMU_MEMADDR(0x200000), Synth.MAX_SYSEX_SIZE - 1, 1) { }
    }
    class ResetMemoryRegion : MemoryRegion
    {
        public ResetMemoryRegion(Synth useSynth)
            : base(useSynth, BytePtr.Null, BytePtr.Null, MemoryRegionType.MR_Reset, Mt32Emu.MT32EMU_MEMADDR(0x7F0000), 0x3FFF, 1) { }
    }


    class MemoryRegion
    {
        private Synth synth;
        public readonly BytePtr RealMemory;
        private BytePtr maxTable;
        public MemoryRegionType type;
        public int startAddr, entrySize, entries;

        public MemoryRegion(Synth useSynth, BytePtr useRealMemory, BytePtr useMaxTable, MemoryRegionType useType, int useStartAddr, int useEntrySize, int useEntries)
        {
            synth = useSynth;
            RealMemory = useRealMemory;
            maxTable = useMaxTable;
            type = useType;
            startAddr = useStartAddr;
            entrySize = useEntrySize;
            entries = useEntries;
        }

        public int LastTouched(int addr, int len)
        {
            return (Offset(addr) + len - 1) / entrySize;
        }

        public int FirstTouchedOffset(int addr)
        {
            return Offset(addr) % entrySize;
        }

        public int FirstTouched(int addr)
        {
            return Offset(addr) / entrySize;
        }

        public int RegionEnd
        {
            get
            {
                return startAddr + entrySize * entries;
            }
        }

        public bool Contains(int addr)
        {
            return addr >= startAddr && addr < RegionEnd;
        }

        public int Offset(int addr)
        {
            return addr - startAddr;
        }

        public int GetClampedLen(int addr, int len)
        {
            if (addr + len > RegionEnd)
                return RegionEnd - addr;
            return len;
        }

        public int Next(int addr, int len)
        {
            if (addr + len > RegionEnd)
            {
                return RegionEnd - addr;
            }
            return 0;
        }

        public byte GetMaxValue(int off)
        {
            if (maxTable == BytePtr.Null)
                return 0xFF;
            return maxTable[off % entrySize];
        }

        public bool IsReadable
        {
            get
            {
                return RealMemory != BytePtr.Null;
            }
        }

        public void Read(int entry, int off, byte[] dst, int len)
        {
            off += entry * entrySize;
            // This method should never be called with out-of-bounds parameters,
            // or on an unsupported region - seeing any of this debug output indicates a bug in the emulator
            if (off > entrySize * entries - 1)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("read[%d]: parameters start out of bounds: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
                return;
            }
            if (off + len > entrySize * entries)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("read[%d]: parameters end out of bounds: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
                len = entrySize * entries - off;
            }
            var src = RealMemory;
            if (src == BytePtr.Null)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("read[%d]: unreadable region: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
                return;
            }
            Array.Copy(src.Data, src.Offset + off, dst, 0, len);
        }

        public void Write(int entry, int off, BytePtr src, int len, bool init)
        {
            int memOff = entry * entrySize + off;
            // This method should never be called with out-of-bounds parameters,
            // or on an unsupported region - seeing any of this debug output indicates a bug in the emulator
            if (off > entrySize * entries - 1)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("write[%d]: parameters start out of bounds: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
                return;
            }
            if (off + len > entrySize * entries)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("write[%d]: parameters end out of bounds: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
                len = entrySize * entries - off;
            }
            var dest = RealMemory;
            if (dest == BytePtr.Null)
            {
#if MT32EMU_MONITOR_SYSEX
        synth->printDebug("write[%d]: unwritable region: entry=%d, off=%d, len=%d", type, entry, off, len);
#endif
            }

            for (int i = 0; i < len; i++)
            {
                var desiredValue = src[i];
                var maxValue = GetMaxValue(memOff);
                // maxValue == 0 means write-protected unless called from initialisation code, in which case it really means the maximum value is 0.
                if (maxValue != 0 || init)
                {
                    if (desiredValue > maxValue)
                    {
#if MT32EMU_MONITOR_SYSEX
                synth->printDebug("write[%d]: Wanted 0x%02x at %d, but max 0x%02x", type, desiredValue, memOff, maxValue);
#endif
                        desiredValue = maxValue;
                    }
                    dest[memOff] = desiredValue;
                }
                else if (desiredValue != 0)
                {
#if MT32EMU_MONITOR_SYSEX
            // Only output debug info if they wanted to write non-zero, since a lot of things cause this to spit out a lot of debug info otherwise.
            synth->printDebug("write[%d]: Wanted 0x%02x at %d, but write-protected", type, desiredValue, memOff);
#endif
                }
                memOff++;
            }
        }

    }
}
