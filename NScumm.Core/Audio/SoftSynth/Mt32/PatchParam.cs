//
//  PatchParam.cs
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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class PatchParam
    {
        public const int Size = 8;

        public byte[] Data;
        public int Offset;

        /// <summary>
        /// TIMBRE GROUP  0-3 (group A, group B, Memory, Rhythm)
        /// </summary>
        public byte TimbreGroup
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }

        /// <summary>
        /// // TIMBRE NUMBER 0-63
        /// </summary>
        /// <value>The timbre number.</value>
        public byte TimbreNum
        {
            get { return Data[Offset + 1]; }
            set { Data[Offset + 1] = value; }
        }

        /// <summary>
        /// KEY SHIFT 0-48 (-24 - +24 semitones)
        /// </summary>
        public byte KeyShift
        {
            get { return Data[Offset + 2]; }
            set { Data[Offset + 2] = value; }
        }

        /// <summary>
        /// FINE TUNE 0-100 (-50 - +50 cents)
        /// </summary>
        public byte FineTune
        {
            get { return Data[Offset + 3]; }
            set { Data[Offset + 3] = value; }
        }

        /// <summary>
        /// BENDER RANGE 0-24
        /// </summary>
        public byte BenderRange
        {
            get { return Data[Offset + 4]; }
            set { Data[Offset + 4] = value; }
        }

        /// <summary>
        /// ASSIGN MODE 0-3 (POLY1, POLY2, POLY3, POLY4)
        /// </summary>
        public byte AssignMode
        {
            get { return Data[Offset + 5]; }
            set { Data[Offset + 5] = value; }
        }

        /// <summary>
        /// REVERB SWITCH 0-1 (OFF,ON)
        /// </summary>
        public byte ReverbSwitch
        {
            get { return Data[Offset + 6]; }
            set { Data[Offset + 6] = value; }
        }

        /// <summary>
        /// (DUMMY)
        /// </summary>
        /// <value>The dummy.</value>
        public byte Dummy
        {
            get { return Data[Offset + 7]; }
            set { Data[Offset + 7] = value; }
        }

        public PatchParam(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }
    }
}
