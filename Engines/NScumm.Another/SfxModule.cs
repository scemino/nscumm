//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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

namespace NScumm.Another
{
    internal class SfxModule
    {
        public BytePtr Data;
        public ushort CurPos;
        public byte CurOrder;
        public byte NumOrder;
        public readonly byte[] OrderTable = new byte[0x80];
        public readonly SfxInstrument[] Samples = ScummHelper.CreateArray<SfxInstrument>(15);

        public void Reset()
        {
            Data = BytePtr.Null;
            CurPos = 0;
            CurOrder = 0;
            NumOrder = 0;
            Array.Clear(OrderTable, 0, OrderTable.Length);
            ResetSamples();
        }

        public void ResetSamples()
        {
            foreach (var t in Samples)
            {
                t.Reset();
            }
        }
    }
}