/*
 * This file is part of NScumm.
 *
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using NScumm.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        /// <summary>
        /// Palette cycles.
        /// </summary>
        private ColorCycle[] _colorCycle;

        private void CyclePalette()
        {
            int valueToAdd = _variables[VariableTimer];
            if (valueToAdd < _variables[VariableTimerNext])
                valueToAdd = _variables[VariableTimerNext];

            for (int i = 0; i < 16; i++)
            {
                ColorCycle cycl = _colorCycle[i];
                if (cycl.delay == 0 || cycl.start > cycl.end)
                    continue;
                cycl.counter = (ushort)(cycl.counter + valueToAdd);
                if (cycl.counter >= cycl.delay)
                {
                    cycl.counter %= cycl.delay;

                    SetDirtyColors(cycl.start, cycl.end);

                    DoCyclePalette(_currentPalette, cycl.start, cycl.end, (cycl.flags & 2) == 0);

                    if (_shadowPalette != null)
                    {
                        DoCycleIndirectPalette(_shadowPalette, cycl.start, cycl.end, (cycl.flags & 2) == 0);
                    }
                }
            }
        }

        private void DoCycleIndirectPalette(byte[] palette, byte cycleStart, byte cycleEnd, bool forward)
        {
            int num = cycleEnd - cycleStart + 1;
            int i;
            int offset = forward ? 1 : num - 1;

            for (i = 0; i < 256; i++)
            {
                if (cycleStart <= palette[i] && palette[i] <= cycleEnd)
                {
                    palette[i] = (byte)((palette[i] - cycleStart + offset) % num + cycleStart);
                }
            }

            DoCyclePalette(palette, cycleStart, cycleEnd, forward);
        }

        /// <summary>
        /// Cycle the colors in the given palette in the interval [cycleStart, cycleEnd]
        /// either one step forward or backward.
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="cycleStart"></param>
        /// <param name="cycleEnd"></param>
        /// <param name="forward"></param>
        private static void DoCyclePalette(Palette palette, byte cycleStart, byte cycleEnd, bool forward)
        {
            int num = cycleEnd - cycleStart;

            if (forward)
            {
                var tmp = palette.Colors[cycleEnd];
                Buffer.BlockCopy(palette.Colors, cycleStart, palette.Colors, cycleStart + 1, num);
                palette.Colors[cycleStart] = tmp;
            }
            else
            {
                var tmp = palette.Colors[cycleStart];
                Array.Copy(palette.Colors, cycleStart + 1, palette.Colors, cycleStart, num);
                palette.Colors[cycleEnd] = tmp;
            }
        }

        private static void DoCyclePalette(byte[] palette, byte cycleStart, byte cycleEnd, bool forward)
        {
            int num = cycleEnd - cycleStart;

            if (forward)
            {
                var tmp = palette[cycleEnd];
                Buffer.BlockCopy(palette, cycleStart, palette, cycleStart + 1, num);
                palette[cycleStart] = tmp;
            }
            else
            {
                var tmp = palette[cycleStart];
                Array.Copy(palette, cycleStart + 1, palette, cycleStart, num);
                palette[cycleEnd] = tmp;
            }
        }
    }
}
