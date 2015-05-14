//
//  ISid.cs
//
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

/*
 *  This file is based on reSID, a MOS6581 SID emulator engine.
 *  Copyright (C) 2004  Dag Lem <resid@nimrod.no>
 */

namespace NScumm.Core.Audio.SoftSynth
{
    public interface ISid
    {
        bool EnableFilter { get; set; }

        bool SetSamplingParameters(double clock_freq, double sample_freq, double pass_freq = -1, double filter_scale = 0.97);

        int UpdateClock(ref int delta_t, short[] buf, int offset, int n, int interleave = 1);

        void Reset();

        // write registers.
        void Write(int offset, int value);
    }    
}
