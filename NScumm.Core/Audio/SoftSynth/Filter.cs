//
//  Filter.cs
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
    class Filter
    {
        public Filter()
        {
            IsEnabled = true;

            // Create mappings from FC to cutoff frequency.
            Interpolate(
                new PointIter(f0_points_6581, 0), 
                new PointIter(f0_points_6581, f0_points_6581.GetLength(0) - 1),
                (x, y) => f0_6581[(int)x] = (int)y, 1.0);

            mixer_DC = (-0xfff * 0xff / 18) >> 7;

            f0 = f0_6581;
            f0_points = f0_points_6581;
            f0_count = f0_points_6581.GetLength(0);

            set_w0();
            set_Q();
        }

        interface IPointIter
        {
            int X { get; }

            int Y { get; }

            void MoveNext();

            IPointIter Clone();
        }

        class PointIter: IPointIter, IEquatable<PointIter>
        {
            readonly int[,] points;
            int index;

            public PointIter(int[,] points, int index)
            {
                this.points = points;
                this.index = index;
            }

            public PointIter Clone()
            {
                return new PointIter(points, index);
            }

            IPointIter IPointIter.Clone()
            {
                return Clone();
            }

            public int X { get { return points[index, 0]; } }

            public int Y { get { return points[index, 1]; } }

            public void MoveNext()
            {
                index++;
            }

            public override int GetHashCode()
            {
                return points.GetHashCode() ^ index;
            }

            public bool Equals(PointIter other)
            {
                return other != null && Equals(other.points, points) && other.index == index;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PointIter);
            }
        }

        void Interpolate<TPointIter>(TPointIter p0, TPointIter pn, Action<double,double> plot, double res)
                where TPointIter: IPointIter
        {
            double k1, k2;

            // Set up points for first curve segment.
            var p1 = p0.Clone();
            p1.MoveNext();
            var p2 = p1.Clone();
            p2.MoveNext();
            var p3 = p2.Clone();
            p3.MoveNext();

            // Draw each curve segment.
            for (; !Equals(p2, pn); p0.MoveNext(), p1.MoveNext(), p2.MoveNext(), p3.MoveNext())
            {
                // p1 and p2 equal; single point.
                if (p1.X == p2.X)
                {
                    continue;
                }
                // Both end points repeated; straight line.
                if (p0.X == p1.X && p2.X == p3.X)
                {
                    k1 = k2 = (p2.Y - p1.Y) / (p2.X - p1.X);
                }
                // p0 and p1 equal; use f''(x1) = 0.
                else if (p0.X == p1.X)
                {
                    k2 = (p3.Y - p1.Y) / (p3.X - p1.X);
                    k1 = (3 * (p2.Y - p1.Y) / (p2.X - p1.X) - k2) / 2;
                }
                // p2 and p3 equal; use f''(x2) = 0.
                else if (p2.X == p3.X)
                {
                    k1 = (p2.Y - p0.Y) / (p2.X - p0.X);
                    k2 = (3 * (p2.Y - p1.Y) / (p2.X - p1.X) - k1) / 2;
                }
                // Normal curve.
                else
                {
                    k1 = (p2.Y - p0.Y) / (p2.X - p0.X);
                    k2 = (p3.Y - p1.Y) / (p3.X - p1.X);
                }

                InterpolateSegment(p1.X, p1.Y, p2.X, p2.Y, k1, k2, plot, res);
            }
        }

        /// <summary>
        /// Evaluation of cubic polynomial by forward differencing.
        /// </summary>
        /// <param name="x1">The first x value.</param>
        /// <param name="y1">The first y value.</param>
        /// <param name="x2">The second x value.</param>
        /// <param name="y2">The second y value.</param>
        /// <param name="k1">K1.</param>
        /// <param name="k2">K2.</param>
        /// <param name="plot">Plot.</param>
        /// <param name="res">Res.</param>
        /// <typeparam name="TPointPlotter">The 1st type parameter.</typeparam>
        static void InterpolateSegment(double x1, double y1, double x2, double y2,
                                       double k1, double k2, Action<double,double> plot, double res)
        {
            double a, b, c, d;
            CubicCoefficients(x1, y1, x2, y2, k1, k2, out a, out b, out c, out d);

            double y = ((a * x1 + b) * x1 + c) * x1 + d;
            double dy = (3 * a * (x1 + res) + 2 * b) * x1 * res + ((a * res + b) * res + c) * res;
            double d2y = (6 * a * (x1 + res) + 2 * b) * res * res;
            double d3y = 6 * a * res * res * res;

            // Calculate each point.
            for (double x = x1; x <= x2; x += res)
            {
                plot(x, y);
                y += dy;
                dy += d2y;
                d2y += d3y;
            }
        }

        /// <summary>
        /// Calculation of coefficients.
        /// </summary>
        /// <param name="x1">The first x value.</param>
        /// <param name="y1">The first y value.</param>
        /// <param name="x2">The second x value.</param>
        /// <param name="y2">The second y value.</param>
        /// <param name="k1">K1.</param>
        /// <param name="k2">K2.</param>
        /// <param name="a">The alpha component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="c">C.</param>
        /// <param name="d">D.</param>
        static void CubicCoefficients(double x1, double y1, double x2, double y2,
                                      double k1, double k2,
                                      out double a, out double b, out double c, out double d)
        {
            double dx = x2 - x1, dy = y2 - y1;

            a = ((k1 + k2) - 2 * dy / dx) / (dx * dx);
            b = ((k2 - k1) / dx - 3 * (x1 + x2) * a) / 2;
            c = k1 - (3 * x1 * a + 2 * b) * x1;
            d = y1 - ((x1 * a + b) * x1 + c) * x1;
        }

        public bool IsEnabled { get; set; }

        public void UpdateClock(int delta_t, int voice1, int voice2, int voice3)
        {
            // Scale each voice down from 20 to 13 bits.
            voice1 >>= 7;
            voice2 >>= 7;

            // NB! Voice 3 is not silenced by voice3off if it is routed through
            // the filter.
            if ((voice3off != 0) && ((filt & 0x04) == 0))
            {
                voice3 = 0;
            }
            else
            {
                voice3 >>= 7;
            }

            // Enable filter on/off.
            // This is not really part of SID, but is useful for testing.
            // On slow CPUs it may be necessary to bypass the filter to lower the CPU
            // load.
            if (!IsEnabled)
            {
                Vnf = voice1 + voice2 + voice3;
                Vhp = Vbp = Vlp = 0;
                return;
            }

            // Route voices into or around filter.
            // The code below is expanded to a switch for faster execution.
            // (filt1 ? Vi : Vnf) += voice1;
            // (filt2 ? Vi : Vnf) += voice2;
            // (filt3 ? Vi : Vnf) += voice3;

            int Vi;

            switch (filt)
            {
                default:
                case 0x0:
                    Vi = 0;
                    Vnf = voice1 + voice2 + voice3;
                    break;
                case 0x1:
                    Vi = voice1;
                    Vnf = voice2 + voice3;
                    break;
                case 0x2:
                    Vi = voice2;
                    Vnf = voice1 + voice3;
                    break;
                case 0x3:
                    Vi = voice1 + voice2;
                    Vnf = voice3;
                    break;
                case 0x4:
                    Vi = voice3;
                    Vnf = voice1 + voice2;
                    break;
                case 0x5:
                    Vi = voice1 + voice3;
                    Vnf = voice2;
                    break;
                case 0x6:
                    Vi = voice2 + voice3;
                    Vnf = voice1;
                    break;
                case 0x7:
                    Vi = voice1 + voice2 + voice3;
                    Vnf = 0;
                    break;
                case 0x8:
                    Vi = 0;
                    Vnf = voice1 + voice2 + voice3;
                    break;
                case 0x9:
                    Vi = voice1;
                    Vnf = voice2 + voice3;
                    break;
                case 0xa:
                    Vi = voice2;
                    Vnf = voice1 + voice3;
                    break;
                case 0xb:
                    Vi = voice1 + voice2;
                    Vnf = voice3;
                    break;
                case 0xc:
                    Vi = voice3;
                    Vnf = voice1 + voice2;
                    break;
                case 0xd:
                    Vi = voice1 + voice3;
                    Vnf = voice2;
                    break;
                case 0xe:
                    Vi = voice2 + voice3;
                    Vnf = voice1;
                    break;
                case 0xf:
                    Vi = voice1 + voice2 + voice3;
                    Vnf = 0;
                    break;
            }

            // Maximum delta cycles for the filter to work satisfactorily under current
            // cutoff frequency and resonance constraints is approximately 8.
            int delta_t_flt = 8;

            while (delta_t != 0)
            {
                if (delta_t < delta_t_flt)
                {
                    delta_t_flt = delta_t;
                }

                // delta_t is converted to seconds given a 1MHz clock by dividing
                // with 1 000 000. This is done in two operations to avoid integer
                // multiplication overflow.

                // Calculate filter outputs.
                // Vhp = Vbp/Q - Vlp - Vi;
                // dVbp = -w0*Vhp*dt;
                // dVlp = -w0*Vbp*dt;
                int w0_delta_t = w0_ceil_dt * delta_t_flt >> 6;

                int dVbp = (w0_delta_t * Vhp >> 14);
                int dVlp = (w0_delta_t * Vbp >> 14);
                Vbp -= dVbp;
                Vlp -= dVlp;
                Vhp = (Vbp * _1024_div_Q >> 10) - Vlp - Vi;

                delta_t -= delta_t_flt;
            }
        }

        public void Reset()
        {
            fc = 0;
            res = 0;
            filt = 0;
            voice3off = 0;
            hp_bp_lp = 0;
            vol = 0;

            // State of filter.
            Vhp = 0;
            Vbp = 0;
            Vlp = 0;
            Vnf = 0;

            set_w0();
            set_Q();
        }

        // Write registers.
        public void WriteFC_LO(int fc_lo)
        {
            fc = (fc & 0x7f8) | (fc_lo & 0x007);
            set_w0();
        }

        public void WriteFC_HI(int fc_hi)
        {
            fc = ((fc_hi << 3) & 0x7f8) | (fc & 0x007);
            set_w0();
        }

        public void WriteRES_FILT(int res_filt)
        {
            res = (res_filt >> 4) & 0x0f;
            set_Q();

            filt = res_filt & 0x0f;
        }

        public void WriteMODE_VOL(int mode_vol)
        {
            voice3off = mode_vol & 0x80;

            hp_bp_lp = (mode_vol >> 4) & 0x07;

            vol = mode_vol & 0x0f;
        }

        // SID audio output (16 bits).
        public int Output()
        {
            // This is handy for testing.
            if (!IsEnabled)
            {
                return (Vnf + mixer_DC) * vol;
            }

            // Mix highpass, bandpass, and lowpass outputs. The sum is not
            // weighted, this can be confirmed by sampling sound output for
            // e.g. bandpass, lowpass, and bandpass+lowpass from a SID chip.

            // The code below is expanded to a switch for faster execution.
            // if (hp) Vf += Vhp;
            // if (bp) Vf += Vbp;
            // if (lp) Vf += Vlp;

            int Vf;

            switch (hp_bp_lp)
            {
                default:
                case 0x0:
                    Vf = 0;
                    break;
                case 0x1:
                    Vf = Vlp;
                    break;
                case 0x2:
                    Vf = Vbp;
                    break;
                case 0x3:
                    Vf = Vlp + Vbp;
                    break;
                case 0x4:
                    Vf = Vhp;
                    break;
                case 0x5:
                    Vf = Vlp + Vhp;
                    break;
                case 0x6:
                    Vf = Vbp + Vhp;
                    break;
                case 0x7:
                    Vf = Vlp + Vbp + Vhp;
                    break;
            }

            // Sum non-filtered and filtered output.
            // Multiply the sum with volume.
            return (Vnf + Vf + mixer_DC) * vol;
        }

        /// <summary>
        /// Set filter cutoff frequency.
        /// </summary>
        void set_w0()
        {
            // Multiply with 1.048576 to facilitate division by 1 000 000 by right-
            // shifting 20 times (2 ^ 20 = 1048576).
            w0 = (int)(2 * Math.PI * f0[fc] * 1.048576);

            // Limit f0 to 16kHz to keep 1 cycle filter stable.
            const int w0_max_1 = (int)(2 * Math.PI * 16000 * 1.048576);
            w0_ceil_1 = w0 <= w0_max_1 ? w0 : w0_max_1;

            // Limit f0 to 4kHz to keep delta_t cycle filter stable.
            const int w0_max_dt = (int)(2 * Math.PI * 4000 * 1.048576);
            w0_ceil_dt = w0 <= w0_max_dt ? w0 : w0_max_dt;
        }

        /// <summary>
        /// Set filter resonance.
        /// </summary>
        void set_Q()
        {
            // Q is controlled linearly by res. Q has approximate range [0.707, 1.7].
            // As resonance is increased, the filter must be clocked more often to keep
            // stable.

            // The coefficient 1024 is dispensed of later by right-shifting 10 times
            // (2 ^ 10 = 1024).
            _1024_div_Q = (int)(1024.0 / (0.707 + 1.0 * res / 0x0f));
        }

        // Filter cutoff frequency.
        int fc;

        // Filter resonance.
        int res;

        // Selects which inputs to route through filter.
        int filt;

        // Switch voice 3 off.
        int voice3off;

        // Highpass, bandpass, and lowpass filter modes.
        int hp_bp_lp;

        // Output master volume.
        int vol;

        // Mixer DC offset.
        int mixer_DC;

        // State of filter.
        int Vhp;
        // highpass
        int Vbp;
        // bandpass
        int Vlp;
        // lowpass
        int Vnf;
        // not filtered

        // Cutoff frequency, resonance.
        int w0, w0_ceil_1, w0_ceil_dt;
        int _1024_div_Q;

        // Cutoff frequency tables.
        // FC is an 11 bit register.
        int[] f0_6581 = new int[2048];
        int[] f0;
        static readonly int[,] f0_points_6581 =
            {
                //  FC      f         FCHI FCLO
                // ----------------------------
                { 0,   220 },   // 0x00      - repeated end point
                { 0,   220 },   // 0x00
                { 128,   230 },   // 0x10
                { 256,   250 },   // 0x20
                { 384,   300 },   // 0x30
                { 512,   420 },   // 0x40
                { 640,   780 },   // 0x50
                { 768,  1600 },   // 0x60
                { 832,  2300 },   // 0x68
                { 896,  3200 },   // 0x70
                { 960,  4300 },   // 0x78
                { 992,  5000 },   // 0x7c
                { 1008,  5400 },   // 0x7e
                { 1016,  5700 },   // 0x7f
                { 1023,  6000 },   // 0x7f 0x07
                { 1023,  6000 },   // 0x7f 0x07 - discontinuity
                { 1024,  4600 },   // 0x80      -
                { 1024,  4600 },   // 0x80
                { 1032,  4800 },   // 0x81
                { 1056,  5300 },   // 0x84
                { 1088,  6000 },   // 0x88
                { 1120,  6600 },   // 0x8c
                { 1152,  7200 },   // 0x90
                { 1280,  9500 },   // 0xa0
                { 1408, 12000 },   // 0xb0
                { 1536, 14500 },   // 0xc0
                { 1664, 16000 },   // 0xd0
                { 1792, 17100 },   // 0xe0
                { 1920, 17700 },   // 0xf0
                { 2047, 18000 },   // 0xff 0x07
                { 2047, 18000 }    // 0xff 0x07 - repeated end point
            };
        int[,] f0_points;
        int f0_count;
    }
    
}
