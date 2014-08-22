//
//  CharsetRendererCommon.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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

namespace NScumm.Core.Graphics
{
    public abstract class CharsetRendererCommon: CharsetRenderer
    {
        protected byte[] _fontPtr;
        protected int _fontHeight;
        protected int _bytesPerPixel;
        protected byte _shadowColor;
        protected bool _shadowMode;

        #region Properties

        public uint NumChars { get; set; }

        #endregion

        protected CharsetRendererCommon(ScummEngine vm)
            : base(vm)
        {
        }
    }
}

