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
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    internal class SingleRemap
    {
        /**
         * The type of remap.
         */
        public RemapType _type;

        /**
         * The first color that should be shifted by a range
         * remap.
         */
        public byte _from;

        /**
         * The last color that should be shifted a range remap.
         */
        public byte _to;

        /**
         * The direction and amount that the colors should be
         * shifted in a range remap.
         */
        public short _delta;

        /**
         * The difference in brightness that should be
         * applied by a brightness (percent) remap.
         *
         * This value may be be greater than 100, in
         * which case the color will be oversaturated.
         */
        public short _percent;

        /**
         * The amount of desaturation that should be
         * applied by a saturation (gray) remap, where
         * 0 is full saturation and 100 is full
         * desaturation.
         */
        public byte _gray;

        /**
         * The final array used by CelObj renderers to composite
         * remapped pixels to the screen buffer.
         *
         * Here is how it works:
         *
         * The source bitmap being rendered will have pixels
         * within the remap range (236-245 or 236-254), and the
         * target buffer will have colors in the non-remapped
         * range (0-235).
         *
         * To arrive at the correct color, first the source
         * pixel is used to look up the correct SingleRemap for
         * that pixel. Then, the final composited color is
         * looked up in this array using the target's pixel
         * color. In other words,
         * `target = _remaps[remapEndColor - source].remapColors[target]`.
         */
        public byte[] _remapColors = new byte[236];

        /**
	 * The previous brightness value. Used to
	 * determine whether or not targetColors needs
	 * to be updated.
	 */
        short _lastPercent;

        /**
         * The previous saturation value. Used to
         * determine whether or not targetColors needs
         * to be updated.
         */
        byte _lastGray;

        /**
         * The colors from the current GfxPalette32 palette
         * before this SingleRemap is applied.
         */
        Color[] _originalColors = new Color[236];

        /**
         * Map of colors that changed in `_originalColors`
         * when this SingleRemap was updated. This map is
         * transient and gets reset to `false` after the
         * SingleRemap finishes updating.
         */
        bool[] _originalColorsChanged = new bool[236];

        /**
         * The ideal target RGB color values for each generated
         * remap color.
         */
        Color[] _idealColors = new Color[236];

        /**
         * Map of colors that changed in `_idealColors` when
         * this SingleRemap was updated. This map is transient
         * and gets reset to `false` after the SingleRemap
         * finishes applying.
         */
        bool[] _idealColorsChanged = new bool[236];

        /**
         * When applying a SingleRemap, finding an appropriate
         * color in the palette is the responsibility of a
         * distance function. Once a match is found, the
         * distance of that match is stored here so that the
         * next time the SingleRemap is applied, it can check
         * the distance from the previous application and avoid
         * triggering an expensive redraw of the entire screen
         * if the new palette value only changed slightly.
         */
        int[] _matchDistances = new int[236];

        public void Reset()
        {
            _lastPercent = 100;
            _lastGray = 0;

            byte remapStartColor = SciEngine.Instance._gfxRemap32.StartColor;
            Palette currentPalette = SciEngine.Instance._gfxPalette32.CurrentPalette;
            for (var i = 0; i < remapStartColor; ++i)
            {
                Color color = currentPalette.colors[i];
                _remapColors[i] = (byte) i;
                _originalColors[i] = color;
                _originalColorsChanged[i] = true;
                _idealColors[i] = color;
                _idealColorsChanged[i] = false;
                _matchDistances[i] = 0;
            }
        }

        public bool Update()
        {
            switch (_type)
            {
                case RemapType.kRemapNone:
                    break;
                case RemapType.kRemapByRange:
                    return UpdateRange();
                case RemapType.kRemapByPercent:
                    return UpdateBrightness();
                case RemapType.kRemapToGray:
                    return UpdateSaturation();
                case RemapType.kRemapToPercentGray:
                    return UpdateSaturationAndBrightness();
                default:
                    DebugHelper.Error("Illegal remap type {0}", _type);
                    break;
            }

            return false;
        }

        private bool UpdateRange()
        {
            byte remapStartColor = SciEngine.Instance._gfxRemap32.StartColor;
            bool updated = false;

            for (uint i = 0; i < remapStartColor; ++i)
            {
                byte targetColor;
                if (_from <= i && i <= _to)
                {
                    targetColor = (byte) (i + _delta);
                }
                else
                {
                    targetColor = (byte) i;
                }

                if (_remapColors[i] != targetColor)
                {
                    updated = true;
                    _remapColors[i] = targetColor;
                }

                _originalColorsChanged[i] = true;
            }

            return updated;
        }

        bool UpdateBrightness()
        {
            byte remapStartColor = SciEngine.Instance._gfxRemap32.StartColor;
            Palette nextPalette = SciEngine.Instance._gfxPalette32.NextPalette;
            for (var i = 1; i < remapStartColor; ++i)
            {
                Color color = nextPalette.colors[i];

                if (_originalColors[i] != color)
                {
                    _originalColorsChanged[i] = true;
                    _originalColors[i] = color;
                }

                if (_percent != _lastPercent || _originalColorsChanged[i])
                {
                    // NOTE: SSCI checked if percent was over 100 and only
                    // then clipped values, but we always unconditionally
                    // ensure the result is in the correct range
                    color.R = (byte) Math.Min(255, color.R * _percent / 100);
                    color.G = (byte) Math.Min(255, color.G * _percent / 100);
                    color.B = (byte) Math.Min(255, color.B * _percent / 100);

                    if (_idealColors[i] != color)
                    {
                        _idealColorsChanged[i] = true;
                        _idealColors[i] = color;
                    }
                }
            }

            bool updated = Apply();
            _originalColorsChanged.Set(0, false, remapStartColor);
            _idealColorsChanged.Set(0, false, remapStartColor);
            _lastPercent = _percent;
            return updated;
        }

        private bool UpdateSaturation()
        {
            byte remapStartColor = SciEngine.Instance._gfxRemap32.StartColor;
            Palette currentPalette = SciEngine.Instance._gfxPalette32.CurrentPalette;
            for (var i = 1; i < remapStartColor; ++i)
            {
                Color color = currentPalette.colors[i];
                if (_originalColors[i] != color)
                {
                    _originalColorsChanged[i] = true;
                    _originalColors[i] = color;
                }

                if (_gray != _lastGray || _originalColorsChanged[i])
                {
                    int luminosity = (((color.R * 77) + (color.G * 151) + (color.B * 28)) >> 8) * _percent / 100;

                    color.R = (byte) Math.Min(255, color.R - ((color.R - luminosity) * _gray / 100));
                    color.G = (byte) Math.Min(255, color.G - ((color.G - luminosity) * _gray / 100));
                    color.B = (byte) Math.Min(255, color.B - ((color.B - luminosity) * _gray / 100));

                    if (_idealColors[i] != color)
                    {
                        _idealColorsChanged[i] = true;
                        _idealColors[i] = color;
                    }
                }
            }

            bool updated = Apply();
            _originalColorsChanged.Set(0, false, remapStartColor);
            _idealColorsChanged.Set(0, false, remapStartColor);
            _lastGray = _gray;
            return updated;
        }

        private bool UpdateSaturationAndBrightness()
        {
            byte remapStartColor = SciEngine.Instance._gfxRemap32.StartColor;
            Palette currentPalette = SciEngine.Instance._gfxPalette32.CurrentPalette;
            for (var i = 1; i < remapStartColor; i++)
            {
                Color color = currentPalette.colors[i];
                if (_originalColors[i] != color)
                {
                    _originalColorsChanged[i] = true;
                    _originalColors[i] = color;
                }

                if (_percent != _lastPercent || _gray != _lastGray || _originalColorsChanged[i])
                {
                    int luminosity = (((color.R * 77) + (color.G * 151) + (color.B * 28)) >> 8) * _percent / 100;

                    color.R = (byte) Math.Min(255, color.R - ((color.R - luminosity) * _gray) / 100);
                    color.G = (byte) Math.Min(255, color.G - ((color.G - luminosity) * _gray) / 100);
                    color.B = (byte) Math.Min(255, color.B - ((color.B - luminosity) * _gray) / 100);

                    if (_idealColors[i] != color)
                    {
                        _idealColorsChanged[i] = true;
                        _idealColors[i] = color;
                    }
                }
            }

            bool updated = Apply();
            _originalColorsChanged.Set(0, false, remapStartColor);
            _idealColorsChanged.Set(0, false, remapStartColor);
            _lastPercent = _percent;
            _lastGray = _gray;
            return updated;
        }

        private bool Apply()
        {
            GfxRemap32 gfxRemap32 = SciEngine.Instance._gfxRemap32;
            byte remapStartColor = gfxRemap32.StartColor;

            // Blocked colors are not allowed to be used as target
            // colors for the remap
            bool[] blockedColors = new bool[236];
            blockedColors.Set(0, false, remapStartColor);

            var paletteCycleMap = SciEngine.Instance._gfxPalette32.CycleMap;

            short blockedRangeCount = gfxRemap32.BlockedRangeCount;
            if (blockedRangeCount != 0)
            {
                byte blockedRangeStart = gfxRemap32.BlockedRangeStart;
                blockedColors.Set(blockedRangeStart, true, blockedRangeCount);
            }

            for (uint i = 0; i < remapStartColor; ++i)
            {
                if (paletteCycleMap[i])
                {
                    blockedColors[i] = true;
                }
            }

            // NOTE: SSCI did a loop over colors here to create a
            // new array of updated, unblocked colors, but then
            // never used it

            bool updated = false;
            for (uint i = 1; i < remapStartColor; ++i)
            {
                int distance;

                if (!_idealColorsChanged[i] && !_originalColorsChanged[_remapColors[i]])
                {
                    continue;
                }

                if (
                    _idealColorsChanged[i] &&
                    _originalColorsChanged[_remapColors[i]] &&
                    _matchDistances[i] < 100 &&
                    ColorDistance(ref _idealColors[i], ref _originalColors[_remapColors[i]]) <= _matchDistances[i]
                )
                {
                    continue;
                }

                short bestColor = MatchColor(ref _idealColors[i], _matchDistances[i], out distance, blockedColors);

                if (bestColor != -1 && _remapColors[i] != bestColor)
                {
                    updated = true;
                    _remapColors[i] = (byte) bestColor;
                    _matchDistances[i] = distance;
                }
            }

            return updated;
        }

        private static int ColorDistance(ref Color a, ref Color b)
        {
            int channelDistance = a.R - b.R;
            int distance = channelDistance * channelDistance;
            channelDistance = a.G - b.G;
            distance += channelDistance * channelDistance;
            channelDistance = a.B - b.B;
            distance += channelDistance * channelDistance;
            return distance;
        }

        private static short MatchColor(ref Color color, int minimumDistance, out int outDistance, bool[] blockedIndexes)
        {
            short bestIndex = -1;
            int bestDistance = 0xFFFFF;
            int distance = minimumDistance;
            Palette nextPalette = SciEngine.Instance._gfxPalette32.NextPalette;
            for (var i = 0; i < SciEngine.Instance._gfxRemap32.StartColor; ++i)
            {
                if (blockedIndexes[i])
                {
                    continue;
                }

                distance = nextPalette.colors[i].R - color.R;
                distance *= distance;
                if (bestDistance <= distance)
                {
                    continue;
                }
                int channelDistance = nextPalette.colors[i].G - color.G;
                distance += channelDistance * channelDistance;
                if (bestDistance <= distance)
                {
                    continue;
                }
                channelDistance = nextPalette.colors[i].B - color.B;
                distance += channelDistance * channelDistance;
                if (bestDistance <= distance)
                {
                    continue;
                }
                bestDistance = distance;
                bestIndex = (short) i;
            }

            // This value is only valid if the last index to
            // perform a distance calculation was the best index
            outDistance = distance;
            return bestIndex;
        }
    }
}