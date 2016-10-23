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

using System.Collections.Generic;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal enum RemapType
    {
        kRemapNone = 0,
        kRemapByRange = 1,
        kRemapByPercent = 2,
        kRemapToGray = 3,
        kRemapToPercentGray = 4
    }

    /// <summary>
    /// SingleRemap objects each manage one remapping operation.
    /// </summary>
    internal class GfxRemap32
    {
        /**
         * The number of currently active remaps.
         */
        private byte _numActiveRemaps;
        /**
         * The first index of the remap area in the system
         * palette.
         */
        private readonly byte _remapStartColor;
        /**
         * The last index of the remap area in the system
         * palette.
         */
        private readonly byte _remapEndColor;

        /**
         * The list of SingleRemaps.
         */
        readonly List<SingleRemap> _remaps;

        /**
         * If true, indicates that one or more SingleRemaps were
         * reconfigured and all remaps need to be recalculated.
         */
        bool _needsUpdate;

        /**
         * The first color that is blocked from being used as a
         * remap target color.
         */
        byte _blockedRangeStart;

        /**
         * The size of the range of blocked colors. If zero,
         * all colors are potential targets for remapping.
         */
        short _blockedRangeCount;

        public byte RemapCount => _numActiveRemaps;
        public byte StartColor => _remapStartColor;
        public byte EndColor => _remapEndColor;

        public short BlockedRangeCount => _blockedRangeCount;
        public byte BlockedRangeStart => _blockedRangeStart;

        public GfxRemap32()
        {
            _remapStartColor = 236;
            // The `_remapStartColor` seems to always be 236 in SSCI,
            // but if it is ever changed then the various C-style
            // member arrays hard-coded to 236 need to be changed to
            // match the highest possible value of `_remapStartColor`
            System.Diagnostics.Debug.Assert(_remapStartColor == 236);

            if (ResourceManager.GetSciVersion() >= SciVersion.V2_1_MIDDLE || SciEngine.Instance.GameId == SciGameId.KQ7)
            {
                _remaps = new List<SingleRemap>(9);
            }
            else
            {
                _remaps = new List<SingleRemap>(19);
            }

            _remapEndColor = (byte) (_remapStartColor + _remaps.Count - 1);
        }

        public void RemapByRange(byte color, short from, short to, short delta)
        {
            // NOTE: SSCI simply ignored invalid input values, but
            // we at least give a warning so games can be investigated
            // for script bugs
            if (color < _remapStartColor || color > _remapEndColor)
            {
                Warning("GfxRemap32::remapByRange: {0} out of remap range", color);
                return;
            }

            if (from < 0)
            {
                Warning("GfxRemap32::remapByRange: attempt to remap negative color {0}", from);
                return;
            }

            if (to >= _remapStartColor)
            {
                Warning("GfxRemap32::remapByRange: attempt to remap into the remap zone at {0}", to);
                return;
            }

            byte index = (byte) (_remapEndColor - color);
            SingleRemap singleRemap = _remaps[index];

            if (singleRemap._type == RemapType.kRemapNone)
            {
                ++_numActiveRemaps;
                singleRemap.Reset();
            }

            singleRemap._from = (byte) @from;
            singleRemap._to = (byte) to;
            singleRemap._delta = delta;
            singleRemap._type = RemapType.kRemapByRange;
            _needsUpdate = true;
        }

        public void RemapByPercent(byte color, short percent)
        {
            // NOTE: SSCI simply ignored invalid input values, but
            // we at least give a warning so games can be investigated
            // for script bugs
            if (color < _remapStartColor || color > _remapEndColor)
            {
                Warning("GfxRemap32::remapByPercent: {0} out of remap range", color);
                return;
            }

            byte index = (byte) (_remapEndColor - color);
            SingleRemap singleRemap = _remaps[index];

            if (singleRemap._type == RemapType.kRemapNone)
            {
                ++_numActiveRemaps;
                singleRemap.Reset();
            }

            singleRemap._percent = percent;
            singleRemap._type = RemapType.kRemapByPercent;
            _needsUpdate = true;
        }

        public void RemapToGray(byte color, sbyte gray)
        {
            // NOTE: SSCI simply ignored invalid input values, but
            // we at least give a warning so games can be investigated
            // for script bugs
            if (color < _remapStartColor || color > _remapEndColor)
            {
                Warning("GfxRemap32::remapToGray: %d out of remap range", color);
                return;
            }

            if (gray < 0 || gray > 100)
            {
                Error("RemapToGray percent out of range; gray = {0}", gray);
            }

            byte index = (byte) (_remapEndColor - color);
            SingleRemap singleRemap = _remaps[index];

            if (singleRemap._type == RemapType.kRemapNone)
            {
                ++_numActiveRemaps;
                singleRemap.Reset();
            }

            singleRemap._gray = (byte) gray;
            singleRemap._type = RemapType.kRemapToGray;
            _needsUpdate = true;
        }

        public void RemapToPercentGray(byte color, short gray, short percent)
        {
            // NOTE: SSCI simply ignored invalid input values, but
            // we at least give a warning so games can be investigated
            // for script bugs
            if (color < _remapStartColor || color > _remapEndColor)
            {
                Warning("GfxRemap32::remapToPercentGray: {0} out of remap range", color);
                return;
            }

            byte index = (byte) (_remapEndColor - color);
            SingleRemap singleRemap = _remaps[index];

            if (singleRemap._type == RemapType.kRemapNone)
            {
                ++_numActiveRemaps;
                singleRemap.Reset();
            }

            singleRemap._percent = percent;
            singleRemap._gray = (byte) gray;
            singleRemap._type = RemapType.kRemapToPercentGray;
            _needsUpdate = true;
        }

        public void BlockRange(byte from, short count)
        {
            _blockedRangeStart = from;
            _blockedRangeCount = count;
        }

        public bool RemapAllTables(bool paletteUpdated)
        {
            if (!_needsUpdate && !paletteUpdated)
            {
                return false;
            }

            bool updated = false;

            foreach (var it in _remaps)
            {
                if (it._type != RemapType.kRemapNone)
                {
                    updated |= it.Update();
                }
            }

            _needsUpdate = false;
            return updated;
        }


        public void RemapOff(byte color)
        {
            if (color == 0)
            {
                RemapAllOff();
                return;
            }

            // NOTE: SSCI simply ignored invalid input values, but
            // we at least give a warning so games can be investigated
            // for script bugs
            if (color < _remapStartColor || color > _remapEndColor)
            {
                Warning("GfxRemap32::remapOff: {0} out of remap range", color);
                return;
            }

            byte index = (byte) (_remapEndColor - color);
            SingleRemap singleRemap = _remaps[index];
            singleRemap._type = RemapType.kRemapNone;
            --_numActiveRemaps;
            _needsUpdate = true;
        }

        public void RemapAllOff()
        {
            foreach (var t in _remaps)
            {
                t._type = RemapType.kRemapNone;
            }
            _numActiveRemaps = 0;
            _needsUpdate = true;
        }

        /**
         * Determines whether or not the given color has an
         * active remapper. If it does not, it is treated as a
         * skip color and the pixel is not drawn.
         *
         * @note SSCI uses a boolean array to decide whether a
         * a pixel is remapped, but it is possible to get the
         * same information from `_remaps`, as this function
         * does.
         * Presumably, the separate array was created for
         * performance reasons, since this is called a lot in
         * the most critical section of the renderer.
         */

        public bool RemapEnabled(byte color)
        {
            byte index = (byte) (_remapEndColor - color);
            System.Diagnostics.Debug.Assert(index < _remaps.Count);
            return _remaps[index]._type != RemapType.kRemapNone;
        }

        /**
         * Calculates the correct color for a target by looking
         * up the target color in the SingleRemap that controls
         * the given sourceColor. If there is no remap for the
         * given color, it will be treated as a skip color.
         */

        public byte RemapColor(byte sourceColor, byte targetColor)
        {
            byte index = (byte) (_remapEndColor - sourceColor);
            System.Diagnostics.Debug.Assert(index < _remaps.Count);
            SingleRemap singleRemap = _remaps[index];
            System.Diagnostics.Debug.Assert(singleRemap._type != RemapType.kRemapNone);
            return singleRemap._remapColors[targetColor];
        }
    }
}