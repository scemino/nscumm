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

using System;
using NScumm.Core;

namespace NScumm.Scumm.Graphics
{
    public abstract class CharsetRendererCommon: CharsetRenderer
    {
        protected byte[] _fontPtr;
        protected int _fontPos;
        protected int _fontHeight;
        protected int _bytesPerPixel;
        protected byte _shadowColor;
        protected bool _shadowMode;
        protected bool _enableShadow;

        #region Properties

        public uint NumChars { get; set; }

        #endregion

        protected CharsetRendererCommon(ScummEngine vm)
            : base(vm)
        {
        }

        public override void SetCurID(int id)
        {
            if (id == -1)
                return;

            CurId = id;

            _fontPtr = Vm.ResourceManager.GetCharsetData((byte)id);
            _fontPos = 0;
            if (_fontPtr == null)
                throw new InvalidOperationException(string.Format("CharsetRendererCommon::setCurID: charset {0} not found", id));

            if (Vm.Game.Version == 4)
                _fontPos += 17;
            else
                _fontPos += 29;

            _bytesPerPixel = _fontPtr[_fontPos];
            _fontHeight = _fontPtr[_fontPos + 1];
            NumChars = _fontPtr.ToUInt16(_fontPos + 2);
        }

        public override int GetFontHeight()
        {
//            if (_vm->_useCJKMode)
//                return MAX(_vm->_2byteHeight + 1, _fontHeight);
//            else
            return _fontHeight;
        }
    }
}

