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

namespace NScumm.Core
{
    partial class ScummEngine
    {
        /// <summary>
        /// Palette cycles.
        /// </summary>
        ColorCycle[] _colorCycle;

        void CyclePalette()
        {
            int valueToAdd = _variables[VariableTimer.Value];
            if (valueToAdd < _variables[VariableTimerNext.Value])
                valueToAdd = _variables[VariableTimerNext.Value];

            for (int i = 0; i < 16; i++)
            {
                ColorCycle cycl = _colorCycle[i];
                if (cycl.Delay == 0 || cycl.Start > cycl.End)
                    continue;
                cycl.Counter = (ushort)(cycl.Counter + valueToAdd);
                if (cycl.Counter >= cycl.Delay)
                {
                    cycl.Counter %= cycl.Delay;

                    SetDirtyColors(cycl.Start, cycl.End);

                    DoCyclePalette(_currentPalette, cycl.Start, cycl.End, (cycl.Flags & 2) == 0);

                    if (_shadowPalette != null)
                    {
                        DoCycleIndirectPalette(_shadowPalette, cycl.Start, cycl.End, (cycl.Flags & 2) == 0);
                    }
                }
            }
        }

        void SetCurrentPalette()
        {
            if (roomData != null && roomData.HasPalette)
            {
                if (_game.Version < 5)
                {
                    Array.Copy(roomData.Palette.Colors, _currentPalette.Colors, roomData.Palette.Colors.Length);
                }
                else
                {
                    for (int i = 0; i < roomData.Palette.Colors.Length; i++)
                    {
                        var color = roomData.Palette.Colors[i];
                        if (i <= 15 || color.R < 252 || color.G < 252 || color.B < 252)
                        {
                            _currentPalette.Colors[i] = color;
                        }
                    }
                }
            }
        }

        void DarkenPalette(int redScale, int greenScale, int blueScale, int startColor, int endColor)
        {
            if (startColor <= endColor)
            {
                var max = _game.Version >= 5 ? 252 : 255;

                for (var j = startColor; j <= endColor; j++)
                {
                    var color = roomData.Palette.Colors[j];
                    var red = (color.R * redScale) / 255.0;
                    if (red > max)
                        red = max;

                    var green = (color.G * greenScale) / 255.0;
                    if (green > max)
                        green = max;

                    var blue = (color.B * blueScale) / 255.0;
                    if (blue > max)
                        blue = max;

                    _currentPalette.Colors[j] = Color.FromRgb((int)red, (int)green, (int)blue);
                    SetDirtyColors(startColor, endColor);
                    //                    if (_game.features & GF_16BIT_COLOR)
                    //                        _16BitPalette[idx] = get16BitColor(_currentPalette[idx * 3 + 0], _currentPalette[idx * 3 + 1], _currentPalette[idx * 3 + 2]);
                }
            }

        }

        void SetPalColor(int index, int r, int g, int b)
        {
            _currentPalette.Colors[index] = Color.FromRgb(r, g, b);

            //            if (_game.Features.HasFlag(GameFeatures.SixteenColors))
            //                _16BitPalette[idx] = get16BitColor(r, g, b);

            SetDirtyColors(index, index);
        }

        void SetShadowPalette(int redScale, int greenScale, int blueScale, int startColor, int endColor, int start, int end)
        {
            // This is an implementation based on the original games code.
            //
            // The four known rooms where setShadowPalette is used in atlantis are:
            //
            // 1) FOA Room 53: subway departing Knossos for Atlantis.
            // 2) FOA Room 48: subway crashing into the Atlantis entrance area
            // 3) FOA Room 82: boat/sub shadows while diving near Thera
            // 4) FOA Room 23: the big machine room inside Atlantis
            //
            // There seems to be no explanation for why this function is called
            // from within Room 23 (the big machine), as it has no shadow effects
            // and thus doesn't result in any visual differences.


            for (var i = start; i < end; i++)
            {
                var r = ((_currentPalette.Colors[i].R >> 2) * redScale) >> 8;
                var g = ((_currentPalette.Colors[i].G >> 2) * greenScale) >> 8;
                var b = ((_currentPalette.Colors[i].B >> 2) * blueScale) >> 8;

                var bestitem = 0;
                uint bestsum = 32000;

                for (var j = startColor; j <= endColor; j++)
                {
                    int ar = _currentPalette.Colors[j].R >> 2;
                    int ag = _currentPalette.Colors[j].G >> 2;
                    int ab = _currentPalette.Colors[j].B >> 2;

                    uint sum = (uint)(Math.Abs(ar - r) + Math.Abs(ag - g) + Math.Abs(ab - b));

                    if (sum < bestsum)
                    {
                        bestsum = sum;
                        bestitem = j;
                    }
                }
                _shadowPalette[i] = (byte)bestitem;
            }
        }

        static void DoCycleIndirectPalette(byte[] palette, byte cycleStart, byte cycleEnd, bool forward)
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
        static void DoCyclePalette(Palette palette, byte cycleStart, byte cycleEnd, bool forward)
        {
            int num = cycleEnd - cycleStart;

            if (forward)
            {
                var tmp = palette.Colors[cycleEnd];
                Array.Copy(palette.Colors, cycleStart, palette.Colors, cycleStart + 1, num);
                palette.Colors[cycleStart] = tmp;
            }
            else
            {
                var tmp = palette.Colors[cycleStart];
                Array.Copy(palette.Colors, cycleStart + 1, palette.Colors, cycleStart, num);
                palette.Colors[cycleEnd] = tmp;
            }
        }

        static void DoCyclePalette(byte[] palette, byte cycleStart, byte cycleEnd, bool forward)
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
