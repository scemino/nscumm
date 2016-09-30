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
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal class GfxPalette32
    {
        private readonly ResourceManager _resMan;
        /**
         * The palette revision version. Increments once per game
         * loop that changes the source palette.
         */
        private uint _version;

        /**
         * Whether or not the hardware palette needs updating.
         */
        private bool _needsUpdate;

        /**
         * The currently displayed palette.
         */
        private Palette _currentPalette;

        /**
         * The unmodified source palette loaded by kPalette. Additional
         * palette entries may be mixed into the source palette by
         * CelObj objects, which contain their own palettes.
         */
        private Palette _sourcePalette;

        /**
         * The palette to be used when the hardware is next updated.
         * On update, _nextPalette is transferred to _currentPalette.
         */
        private Palette _nextPalette;

        /**
         * The fade table records the expected intensity level of each pixel
         * in the palette that will be displayed on the next frame.
         */
        private readonly ushort[] _fadeTable = new ushort[256];

        /**
         * An optional palette used to describe the source colors used
         * in a palette vary operation. If this palette is not specified,
         * sourcePalette is used instead.
         */
        private Palette _varyStartPalette;

        /**
         * An optional palette used to describe the target colors used
         * in a palette vary operation.
         */
        private Palette _varyTargetPalette;

        /**
         * The minimum palette index that has been varied from the
         * source palette. 0–255
         */
        private byte _varyFromColor;

        /**
         * The maximum palette index that is has been varied from the
         * source palette. 0-255
         */
        private byte _varyToColor;

        /**
         * The tick at the last time the palette vary was updated.
         */
        private uint _varyLastTick;

        /**
         * The amount of time to elapse, in ticks, between each cycle
         * of a palette vary animation.
         */
        private int _varyTime;

        /**
         * The direction of change: -1, 0, or 1.
         */
        private short _varyDirection;

        /**
         * The amount, in percent, that the vary color is currently
         * blended into the source color.
         */
        private short _varyPercent;

        /**
         * The target amount that a vary color will be blended into
         * the source color.
         */
        private short _varyTargetPercent;

        /**
         * The number of time palette varying has been paused.
         */
        private ushort _varyNumTimesPaused;

        public GfxPalette32(ResourceManager resMan)
        {
            _resMan = resMan;
            _version = 1;
            _varyToColor = 255;
            for (int i = 0, len = _fadeTable.Length; i < len; ++i)
            {
                _fadeTable[i] = 100;
            }

            LoadPalette(999);
        }

        private bool LoadPalette(int resourceId)
        {
            var palResource = _resMan.FindResource(new ResourceId(ResourceType.Palette, (ushort) resourceId), false);

            if (palResource == null)
            {
                return false;
            }

            var palette = new HunkPalette(palResource.data);
            Submit(palette);
            return true;
        }

        public void Submit(HunkPalette hunkPalette)
        {
            if (hunkPalette.GetVersion() == _version)
            {
                return;
            }

            var oldSourcePalette = new Palette(_sourcePalette);
            var palette = hunkPalette.ToPalette();
            MergePaletteInternal(_sourcePalette, palette);

            if (!_needsUpdate && oldSourcePalette != _sourcePalette)
            {
                ++_version;
                _needsUpdate = true;
            }

            hunkPalette.SetVersion(_version);
        }

        private void MergePaletteInternal(Palette to, Palette from)
        {
            // The last color is always white, so it is not copied.
            // (Some palettes try to set the last color, which causes
            // churning in the palettes when they are merged)
            for (int i = 0, len = to.colors.Length - 1; i < len; ++i)
            {
                if (from.colors[i].used != 0)
                {
                    to.colors[i] = from.colors[i];
                }
            }
        }

        internal void SaveLoadWithSerializer(Serializer s)
        {
            throw new NotImplementedException();
        }

        public void KernelPalVarySet(int paletteId, short percent, int time, short fromColor, short toColor)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetVary(palette, percent, time, fromColor, toColor);
        }

        private Palette GetPaletteFromResourceInternal(int resourceId)
        {
            var palResource = _resMan.FindResource(new ResourceId(ResourceType.Palette, (ushort) resourceId), false);

            if (palResource == null)
            {
                Error("Could not load vary palette {0}", resourceId);
            }

            var rawPalette = new HunkPalette(palResource.data);
            return rawPalette.ToPalette();
        }

        private void SetVary(Palette target, short percent, int time, short fromColor, short toColor)
        {
            SetTarget(target);
            SetVaryTimeInternal(percent, time);

            if (fromColor > -1)
            {
                _varyFromColor = (byte) fromColor;
            }
            if (toColor > -1)
            {
                System.Diagnostics.Debug.Assert(toColor < 256);
                _varyToColor = (byte) toColor;
            }
        }

        private void SetTarget(Palette palette)
        {
            _varyTargetPalette = new Palette(palette);
        }

        private void SetVaryTimeInternal(short percent, int time)
        {
            _varyLastTick = SciEngine.Instance.TickCount;
            if (time == 0 || _varyPercent == percent)
            {
                _varyDirection = 0;
                _varyTargetPercent = _varyPercent = percent;
            }
            else
            {
                _varyTime = time / (percent - _varyPercent);
                _varyTargetPercent = percent;

                if (_varyTime > 0)
                {
                    _varyDirection = 1;
                }
                else if (_varyTime < 0)
                {
                    _varyDirection = -1;
                    _varyTime = -_varyTime;
                }
                else
                {
                    _varyDirection = 0;
                    _varyTargetPercent = _varyPercent = percent;
                }
            }
        }

        public void SetVaryPercent(short percent, int time, short fromColor, short fromColorAlternate)
        {
            if (_varyTargetPalette != null)
            {
                SetVaryTimeInternal(percent, time);
            }

            // This looks like a mistake in the actual SCI engine (both SQ6 and Lighthouse);
            // the values are always hardcoded to -1 in kPalVary, so this code can never
            // actually be executed
            if (fromColor > -1)
            {
                _varyFromColor = (byte) fromColor;
            }
            if (fromColorAlternate > -1)
            {
                _varyFromColor = (byte) fromColorAlternate;
            }
        }

        public short GetVaryPercent()
        {
            return Math.Abs(_varyPercent);
        }

        public void VaryOff()
        {
            _varyNumTimesPaused = 0;
            _varyPercent = _varyTargetPercent = 0;
            _varyFromColor = 0;
            _varyToColor = 255;
            _varyDirection = 0;

            _varyTargetPalette = null;
            _varyStartPalette = null;
        }

        public void KernelPalVaryMergeTarget(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            MergeTarget(palette);
        }

        private void MergeTarget(Palette palette)
        {
            if (_varyTargetPalette != null)
            {
                MergePaletteInternal(_varyTargetPalette, palette);
            }
            else
            {
                _varyTargetPalette = new Palette(palette);
            }
        }

        public void SetVaryTime(int time)
        {
            if (_varyTargetPalette == null)
            {
                return;
            }
            SetVaryTimeInternal(_varyTargetPercent, time);
        }

        public void KernelPalVarySetTarget(int paletteId) {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetTarget(palette);
        }

        public void KernelPalVarySetStart(int paletteId) {
            var palette = GetPaletteFromResourceInternal(paletteId);
            SetStart(palette);
        }

        private void SetStart(Palette palette)
        {
            _varyStartPalette = new Palette(palette);
        }

        public void KernelPalVaryMergeStart(int paletteId)
        {
            var palette = GetPaletteFromResourceInternal(paletteId);
            MergeStart(palette);
        }

        private void MergeStart(Palette palette)
        {
            if (_varyStartPalette != null)
            {
                MergePaletteInternal(_varyStartPalette, palette);
            }
            else
            {
                _varyStartPalette = new Palette(palette);
            }
        }
    }
}
