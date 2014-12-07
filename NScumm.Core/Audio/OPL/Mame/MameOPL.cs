//
//  MameOPL.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
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

namespace NScumm.Core.Audio.OPL.Mame
{
    class MameOPL:IOpl
    {
        #region IOpl implementation

        public bool Init(uint rate)
        {
            _opl = FmOPL.MakeAdLibOPL(rate);
            return (_opl != null);
        }

        public void Reset()
        {
            _opl.OPLResetChip();
        }

        public void Write(int a, int v)
        {
            _opl.OPLWrite(a, v);
        }

        public byte Read(int a)
        {
            return _opl.OPLRead(a);
        }

        public void WriteReg(int r, int v)
        {
            _opl.OPLWriteReg(r, v);
        }

        public void ReadBuffer(short[] buffer, int pos, int length)
        {
            _opl.YM3812UpdateOne(buffer, length);
        }

        public bool IsStereo
        {
            get { return false; }
        }

        #endregion

        FmOPL _opl;
    }
}

