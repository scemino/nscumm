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

using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    /**
     * This class handles color remapping for the QFG4 demo.
     */
    internal class GfxRemap
    {
        private readonly GfxPalette _palette;
        private bool _remapOn;
        private readonly ColorRemappingType[] _remappingType =new ColorRemappingType[256];
        private readonly byte[] _remappingByPercent=new byte [256];
        private readonly byte[] _remappingByRange = new byte[256];
        private ushort _remappingPercentToSet;

        public GfxRemap(GfxPalette palette)
        {
            _palette = palette;
        }

        public bool IsRemapped(byte color)
        {
            return _remapOn && (_remappingType[color] != ColorRemappingType.None);
        }

        public void ResetRemapping()
        {
            _remapOn = false;
            _remappingPercentToSet = 0;

            for (int i = 0; i < 256; i++)
            {
                _remappingType[i] = ColorRemappingType.None;
                _remappingByPercent[i] = (byte) i;
                _remappingByRange[i] = (byte) i;
            }
        }

        public void SetRemappingPercent(byte color, byte percent)
        {
            _remapOn = true;

            // We need to defer the setup of the remapping table every time the screen
            // palette is changed, so that kernelFindColor() can find the correct
            // colors. Set it once here, in case the palette stays the same and update
            // it on each palette change by copySysPaletteToScreen().
            _remappingPercentToSet = percent;

            for (int i = 0; i < 256; i++)
            {
                byte r = (byte) (_palette._sysPalette.colors[i].R * _remappingPercentToSet / 100);
                byte g = (byte) (_palette._sysPalette.colors[i].G * _remappingPercentToSet / 100);
                byte b = (byte) (_palette._sysPalette.colors[i].B * _remappingPercentToSet / 100);
                _remappingByPercent[i] = (byte) _palette.KernelFindColor(r, g, b);
            }

            _remappingType[color] = ColorRemappingType.ByPercent;
        }

        public void SetRemappingRange(byte color, byte from, byte to, byte @base)
        {
            _remapOn = true;

            for (int i = from; i <= to; i++)
            {
                _remappingByRange[i] = (byte) (i + @base);
            }

            _remappingType[color] = ColorRemappingType.ByRange;
        }

        public byte RemapColor(byte remappedColor, byte screenColor)
        {
            System.Diagnostics.Debug.Assert(_remapOn);
            if (_remappingType[remappedColor] == ColorRemappingType.ByRange)
                return _remappingByRange[screenColor];
            if (_remappingType[remappedColor] == ColorRemappingType.ByPercent)
                return _remappingByPercent[screenColor];

            Error("remapColor(): Color {0} isn't remapped", remappedColor);

            return 0; // should never reach here
        }


        public void UpdateRemapping()
        {
            // Check if we need to reset remapping by percent with the new colors.
            if (_remappingPercentToSet == 0) return;

            for (var i = 0; i < 256; i++)
            {
                byte r = (byte) (_palette._sysPalette.colors[i].R * _remappingPercentToSet / 100);
                byte g = (byte) (_palette._sysPalette.colors[i].G * _remappingPercentToSet / 100);
                byte b = (byte) (_palette._sysPalette.colors[i].B * _remappingPercentToSet / 100);
                _remappingByPercent[i] = (byte) _palette.KernelFindColor(r, g, b);
            }
        }
    }
}