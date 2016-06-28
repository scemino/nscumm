//
//  GraphicData.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using NScumm.Core;

namespace NScumm.Queen
{
    public struct GraphicData
    {
        //! coordinates of object
        public ushort x, y;
        //! bank bobframes
        /*!
		<table>
			<tr>
				<td>lastFrame == 0</td>
				<td>non-animated bob (one frame)</td>
			</tr>
			<tr>
				<td>lastFrame < 0</td>
				<td>rebound animation</td>
			</tr>
			<tr>
				<td>firstFrame < 0</td>
				<td>BobSlot::animString (animation is described by a string)</td>
			</tr>
			<tr>
				<td>firstFrame > 0</td>
				<td>BobSlot::animNormal (animation is a sequence of frames)</td>
			</tr>
		</table>
	*/
        public short firstFrame, lastFrame;
        //! moving speed of object
        public ushort speed;

        public void ReadFromBE(byte[] data, ref int ptr)
        {
            x = data.ToUInt16BigEndian(ptr); ptr += 2;
            y = data.ToUInt16BigEndian(ptr); ptr += 2;
            firstFrame = data.ToInt16BigEndian(ptr); ptr += 2;
            lastFrame = data.ToInt16BigEndian(ptr); ptr += 2;
            speed = data.ToUInt16BigEndian(ptr); ptr += 2;
        }
    }

}
