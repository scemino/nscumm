//
//  ScummEngine3_Drawing.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void DrawBox()
        {
            var x = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = GetVarOrDirectWord(OpCodeParameter.Param2);

            _opCode = ReadByte();
            var x2 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var color = GetVarOrDirectByte(OpCodeParameter.Param3);

            DrawBoxCore(x, y, x2, y2, color);
        }
    }
}

