//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Special Font class, handles SJIS inside sci games, uses ScummVM SJIS support
    /// </summary>
    internal class GfxFontSjis : GfxFont
    {
        private int _resourceId;
        private GfxScreen _screen;
        // private FontSJIS _commonFont;

        public GfxFontSjis(GfxScreen screen, int resourceId)
        {
            _screen = screen;
            _resourceId = resourceId;

            if (_screen.UpscaledHires == GfxScreenUpscaledMode.DISABLED)
                throw new System.InvalidOperationException("I don't want to initialize, when not being in upscaled hires mode");

            throw new System.NotImplementedException();
            //_commonFont = Graphics::FontSJIS::createFont(Common::kPlatformPC98);

            //if (!_commonFont)
            //    error("Could not load ScummVM's 'SJIS.FNT'");
        }
    }
}
