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

using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        /// <summary>
        /// Palette cycles.
        /// </summary>
        protected ColorCycle[] _colorCycle;
        protected int _palManipStart;
        protected int _palManipEnd;
        protected int _palManipCounter;
        protected Palette _palManipPalette;
        protected Palette _palManipIntermediatePal;
        protected int _curPalIndex;
        protected byte[] _shadowPalette;
        protected Palette _darkenPalette = new Palette();
        public ushort[] _16BitPalette = new ushort[512];

        public const int NumShadowPalette = 8;

        Palette _currentPalette = new Palette();

        internal Palette CurrentPalette
        { 
            get { return _currentPalette; } 
            set { _currentPalette = value; } 
        }

        protected internal byte[] ShadowPalette { get { return _shadowPalette; } }

        void InitPalettes()
        {
            _shadowPalette = new byte[Game.Version >= 7 ? NumShadowPalette * 256 : 256];
            if (Game.Version <= 1)
            {
                if (Game.Platform == Platform.Apple2GS || Game.Platform == Platform.C64)
                {
                    SetPalette(Palette.C64);
                }
                else
                {
                    SetPalette(Palette.V1);
                    if (Game.GameId == GameId.Zak)
                        SetPalColor(15, 170, 170, 170);
                }
            }
            else if (Game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                if ((_game.Platform == Platform.Amiga) || (_game.Platform == Platform.AtariST))
                    SetPalette(Palette.Amiga);
                else
                {
                    Array.Copy(Palette.Ega.Colors, _currentPalette.Colors, Palette.Ega.Colors.Length);
                    _gfxManager.SetPalette(_currentPalette.Colors);
                }

                for (int i = 0; i < 256; i++)
                    _shadowPalette[i] = (byte)i;
            }
            else if ((Game.Platform == Platform.Amiga) && Game.Version == 4)
            {
                // if rendermode is set to EGA we use the full palette from the resources
                // else we initialize and then lock down the first 16 colors.
                SetPalette(Palette.AmigaMonkeyIsland);
            }
            else if (Game.Platform == Platform.FMTowns)
            {
                if (Game.GameId == GameId.Indy4 || Game.GameId == GameId.Monkey2)
                    _townsClearLayerFlag = 0;
                else if (Game.GameId == GameId.Loom)
                    TownsSetTextPalette(Palette.TownsLoom);
                else if (Game.Version == 3)
                    TownsSetTextPalette(Palette.Towns3);

                _townsScreen.ToggleLayers(_townsActiveLayerFlags);
            }

            for (int i = 0; i < 256; i++)
                Gdi.RoomPalette[i] = (byte)i;
        }

        void CyclePalette()
        {
            if (_game.Platform == Platform.FMTowns && ((TownsPaletteFlags & 1) == 0))
                return;

            var valueToAdd = _variables[VariableTimer.Value];
            if (valueToAdd < _variables[VariableTimerNext.Value])
                valueToAdd = _variables[VariableTimerNext.Value];

            for (var i = 0; i < 16; i++)
            {
                var cycl = _colorCycle[i];
                if (cycl.Delay == 0 || cycl.Start > cycl.End)
                    continue;
                cycl.Counter = (ushort)(cycl.Counter + valueToAdd);
                if (cycl.Counter >= cycl.Delay)
                {
                    cycl.Counter %= cycl.Delay;

                    SetDirtyColors(cycl.Start, cycl.End);
                    MoveMemInPalRes(cycl.Start, cycl.End, (cycl.Flags & 2) != 0);

                    DoCyclePalette(_currentPalette, cycl.Start, cycl.End, (cycl.Flags & 2) == 0);

                    if (_shadowPalette != null)
                    {
                        if (_game.Version >= 7)
                        {
                            for (var j = 0; j < NumShadowPalette; j++)
                            {
                                DoCycleIndirectPalette(_shadowPalette, cycl.Start, cycl.End, (cycl.Flags & 2) == 0, j);
                            }
                        }
                        else
                        {
                            DoCycleIndirectPalette(_shadowPalette, cycl.Start, cycl.End, (cycl.Flags & 2) == 0, 0);
                        }
                    }
                }
            }
        }

        void TownsProcessPalCycleField()
        {
            for (int i = 0; i < _numCyclRects; i++)
            {
                int x1 = _cyclRects[i].Left - MainVirtScreen.XStart;
                int x2 = _cyclRects[i].Right - MainVirtScreen.XStart;
                if (x1 < 0)
                    x1 = 0;
                if (x2 > 320)
                    x2 = 320;
                if (x2 > 0)
                    MarkRectAsDirty(MainVirtScreen, x1, x2, _cyclRects[i].Top, _cyclRects[i].Bottom);
            }
        }

        void TownsResetPalCycleFields()
        {
            _numCyclRects = 0;
            TownsPaletteFlags &= ~1;
        }

        void MoveMemInPalRes(int start, int end, bool direction)
        {
            if (_palManipCounter == 0)
                return;

            DoCyclePalette(_palManipPalette, start, end, !direction);
            DoCyclePalette(_palManipIntermediatePal, start, end, !direction);
        }

        void SetPalette(Palette palette)
        {
            for (var i = 0; i < 256; i++)
            {
                SetPalColor(i, palette.Colors[i].R, palette.Colors[i].G, palette.Colors[i].B);
            }
        }

        protected void SetCurrentPalette(int palIndex)
        {
            _curPalIndex = palIndex;
            var palette = roomData.Palettes[palIndex];
            if (_game.Platform == Platform.FMTowns)
            {
                TownsSetPalette(palette);
            }
            else
            {
                SetCurrentPalette(palette);
            }
        }

        void TownsSetPalette(Palette palette)
        {
            SetCurrentPalette(palette);

            if (Game.Version == 5)
                TownsSetTextPalette(_currentPalette);

            TownsOverrideShadowColor = 1;
            int m = 48;
            for (int i = 1; i < 16; ++i)
            {
                int val = _currentPalette.Colors[i].R + _currentPalette.Colors[i].G + _currentPalette.Colors[i].B;
                if (m > val)
                {
                    TownsOverrideShadowColor = (byte)i;
                    m = val;
                }
            }
        }

        void TownsSetTextPalette(Palette palette)
        {
            for (int i = 0; i < 16; i++)
            {
                _textPalette[i * 3] = (byte)palette.Colors[i].R;
                _textPalette[i * 3 + 1] = (byte)palette.Colors[i].G;
                _textPalette[i * 3 + 2] = (byte)palette.Colors[i].B;
            }
        }

        void SetCurrentPalette(Palette palette)
        {
            for (var i = 0; i < 256; i++)
            {
                var color = palette.Colors[i];
                if (Game.Version >= 5 && Game.Version <= 6)
                {
                    if (i < 15 || i == 15 || color.R < 252 || color.G < 252 || color.B < 252)
                    {
                        CurrentPalette.Colors[i] = color;
                    }
                }
                else
                {
                    CurrentPalette.Colors[i] = color;
                }
            }
            if (Game.Version == 8)
            {
                Array.Copy(_currentPalette.Colors, _darkenPalette.Colors, _darkenPalette.Colors.Length);
            }
            SetDirtyColors(0, 255);
        }

        protected void StopCycle(int i)
        {
            ScummHelper.AssertRange(0, i, 16, "stopCycle: cycle");
            if (i != 0)
            {
                _colorCycle[i - 1].Delay = 0;
                return;
            }

            for (i = 0; i < 16; i++)
            {
                var cycl = _colorCycle[i];
                cycl.Delay = 0;
            }
        }

        protected void DarkenPalette(int redScale, int greenScale, int blueScale, int startColor, int endColor)
        {
            if (startColor <= endColor)
            {
                var max = _game.Version >= 5 && _game.Version <= 6 ? 252 : 255;
                var currentPalette = Game.Version == 8 ? _darkenPalette : roomData.Palettes[_curPalIndex];

                for (var j = startColor; j <= endColor; j++)
                {
                    var color = currentPalette.Colors[j];
                    var red = (color.R * redScale) / 255;
                    if (red > max)
                        red = max;

                    var green = (color.G * greenScale) / 255;
                    if (green > max)
                        green = max;

                    var blue = (color.B * blueScale) / 255;
                    if (blue > max)
                        blue = max;

                    _currentPalette.Colors[j] = Color.FromRgb(red, green, blue);
                    //                    if (_game.features & GF_16BIT_COLOR)
                    //                        _16BitPalette[idx] = get16BitColor(_currentPalette[idx * 3 + 0], _currentPalette[idx * 3 + 1], _currentPalette[idx * 3 + 2]);
                }

                SetDirtyColors(startColor, endColor);
            }

        }

        protected void SetPalColor(int index, int r, int g, int b)
        {
            _currentPalette.Colors[index] = Color.FromRgb(r, g, b);
            if (Game.Version == 8)
            {
                _darkenPalette.Colors[index] = Color.FromRgb(r, g, b);
            }

            //            if (_game.Features.HasFlag(GameFeatures.SixteenColors))
            //                _16BitPalette[idx] = get16BitColor(r, g, b);

            SetDirtyColors(index, index);
        }

        protected void SetShadowPalette(int redScale, int greenScale, int blueScale, int startColor, int endColor, int start, int end)
        {
            var currentPalette = roomData.Palettes[_curPalIndex];

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

            if (Game.GameId == Scumm.IO.GameId.SamNMax)
            {
                for (var i = 0; i < 256; i++)
                    _shadowPalette[i] = (byte)i;
            }

            for (var i = start; i < end; i++)
            {
                var r = ((currentPalette.Colors[i].R >> 2) * redScale) >> 8;
                var g = ((currentPalette.Colors[i].G >> 2) * greenScale) >> 8;
                var b = ((currentPalette.Colors[i].B >> 2) * blueScale) >> 8;

                var bestitem = 0;
                uint bestsum = 32000;

                for (var j = startColor; j <= endColor; j++)
                {
                    int ar = currentPalette.Colors[j].R >> 2;
                    int ag = currentPalette.Colors[j].G >> 2;
                    int ab = currentPalette.Colors[j].B >> 2;

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

        static void DoCycleIndirectPalette(byte[] palette, byte cycleStart, byte cycleEnd, bool forward, int palIndex)
        {
            var num = cycleEnd - cycleStart + 1;
            var offset = forward ? 1 : num - 1;
            var palOffset = palIndex * 256;

            for (var i = palOffset; i < (palOffset + 256); i++)
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
        static void DoCyclePalette(Palette palette, int cycleStart, int cycleEnd, bool forward)
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
                Array.Copy(palette, cycleStart, palette, cycleStart + 1, num);
                palette[cycleStart] = tmp;
            }
            else
            {
                var tmp = palette[cycleStart];
                Array.Copy(palette, cycleStart + 1, palette, cycleStart, num);
                palette[cycleEnd] = tmp;
            }
        }

        protected virtual void PalManipulateInit(int resID, int start, int end, int time)
        {
            var string1 = _strings[resID];
            var string2 = _strings[resID + 1];
            var string3 = _strings[resID + 2];
            if (string1 == null || string2 == null || string3 == null)
            {
                throw new InvalidOperationException(string.Format(
                        "palManipulateInit({0},{1},{2},{3}): Cannot obtain string resources {4}, {5} and {6}",
                        resID, start, end, time, resID, resID + 1, resID + 2));
            }

            _palManipStart = start;
            _palManipEnd = end;
            _palManipCounter = 0;

            if (_palManipPalette == null)
                _palManipPalette = new Palette();

            if (_palManipIntermediatePal == null)
                _palManipIntermediatePal = new Palette();

            for (var i = start; i < end; ++i)
            {
                _palManipPalette.Colors[i] = Color.FromRgb(string1[i], string2[i], string3[i]);
                var pal = _currentPalette.Colors[i];
                _palManipIntermediatePal.Colors[i] = Color.FromRgb(pal.R << 8, pal.G << 8, pal.B << 8);
            }

            _palManipCounter = time;
        }

        void PalManipulate()
        {
            if (_palManipCounter == 0 || _palManipPalette == null || _palManipIntermediatePal == null)
                return;

            for (var i = _palManipStart; i < _palManipEnd; ++i)
            {
                var target = _palManipPalette.Colors[i];
                var between = _palManipIntermediatePal.Colors[i];
                _palManipIntermediatePal.Colors[i] = Color.FromRgb(
                    between.R + ((target.R << 8) - between.R) / _palManipCounter, 
                    between.G + ((target.G << 8) - between.G) / _palManipCounter, 
                    between.B + ((target.B << 8) - between.B) / _palManipCounter);
                _currentPalette.Colors[i] = Color.FromRgb(between.R >> 8, between.G >> 8, between.B >> 8);
            }
            SetDirtyColors(_palManipStart, _palManipEnd);
            _palManipCounter--;
        }
    }
}
