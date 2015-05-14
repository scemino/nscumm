//
//  ExternalFilter.cs
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
using System;

namespace NScumm.Core.Audio.SoftSynth
{
    class ExternalFilter
    {
        public bool IsEnabled { get; set; }

        public ExternalFilter()
        {
            Reset();
            IsEnabled = true;
            SetSamplingParameter(15915.6);
            mixer_DC = ((((0x800 - 0x380) + 0x800) * 0xff * 3 - 0xfff * 0xff / 18) >> 7) * 0x0f;
        }

        public void SetSamplingParameter(double pass_freq)
        {
            w0hp = 105;
            w0lp = (int)(pass_freq * (2.0 * Math.PI * 1.048576));
            if (w0lp > 104858)
                w0lp = 104858;
        }

        public void UpdateClock(int delta_t, int Vi)
        {
            // This is handy for testing.
            if (!IsEnabled)
            {
                // Remove maximum DC level since there is no filter to do it.
                Vlp = Vhp = 0;
                Vo = Vi - mixer_DC;
                return;
            }

            // Maximum delta cycles for the external filter to work satisfactorily
            // is approximately 8.
            var delta_t_flt = 8;

            while (delta_t != 0)
            {
                if (delta_t < delta_t_flt)
                {
                    delta_t_flt = delta_t;
                }

                // delta_t is converted to seconds given a 1MHz clock by dividing
                // with 1 000 000.

                // Calculate filter outputs.
                // Vo  = Vlp - Vhp;
                // Vlp = Vlp + w0lp*(Vi - Vlp)*delta_t;
                // Vhp = Vhp + w0hp*(Vlp - Vhp)*delta_t;

                var dVlp = (w0lp * delta_t_flt >> 8) * (Vi - Vlp) >> 12;
                var dVhp = w0hp * delta_t_flt * (Vlp - Vhp) >> 20;
                Vo = Vlp - Vhp;
                Vlp += dVlp;
                Vhp += dVhp;

                delta_t -= delta_t_flt;
            }
        }

        public void Reset()
        {
            // State of filter.
            Vlp = 0;
            Vhp = 0;
            Vo = 0;
        }

        // Audio output (20 bits).
        public int Output()
        {
            return Vo;
        }

        // Maximum mixer DC offset.
        int mixer_DC;

        // State of filters.
        int Vlp;
        // lowpass
        int Vhp;
        // highpass
        int Vo;

        // Cutoff frequencies.
        int w0lp;
        int w0hp;
    }
    
}
