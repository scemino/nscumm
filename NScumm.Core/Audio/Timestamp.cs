//
//  Timestamp.cs
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
using System.Diagnostics;

namespace NScumm.Core.Audio
{
    public class Timestamp : IComparable<Timestamp>, IEquatable<Timestamp>
    {
        int _secs;
        int _framerate;
        int _numFrames;
        int _framerateFactor;

        /**
         * Return the time in frames described by this timestamp.
         */
        public int TotalNumberOfFrames { get { return _numFrames / _framerateFactor + _secs * (_framerate / _framerateFactor); } }

        /// <summary>
        /// A timestamp consists of a number of seconds, plus a number
        /// of frames, the latter describing a fraction of a second.
        /// This method returns the latter number.
        /// </summary>
        /// <value>The number of frames.</value>
        public int NumberOfFrames { get { return _numFrames / _framerateFactor; } }

        /// <summary>
        /// Gets the the framerate used by this timestamp.
        /// </summary>
        /// <value>The framerate.</value>
        public int Framerate { get { return _framerate / _framerateFactor; } }

        /// <summary>
        /// Gets the time in seconds described by this timestamp,
        /// rounded down.
        /// </summary>
        /// <value>The seconds.</value>
        public int Seconds { get { return _secs; } }

        /// <summary>
        /// Gets the time in milliseconds described by this timestamp,
        /// rounded down.
        /// </summary>
        /// <value>The milliseconds.</value>
        public int Milliseconds
        {
            get
            {
                // Note that _framerate is always divisible by 1000.
                return _secs * 1000 + _numFrames / (_framerate / 1000);
            }
        }

        public Timestamp(Timestamp ts)
        {
            _secs = ts._secs;
            _framerate = ts._framerate;
            _numFrames = ts._numFrames;
            _framerateFactor = ts._framerateFactor;
        }

        public Timestamp(int ms, int fr = 1)
        {
            Debug.Assert(fr > 0);

            _secs = ms / 1000;
            _framerateFactor = 1000 / Gcd(1000, fr);
            _framerate = fr * _framerateFactor;

            // Note that _framerate is always divisible by 1000.
            _numFrames = (ms % 1000) * (_framerate / 1000);
        }

        public Timestamp(int s, int frames, int fr)
        {
            Debug.Assert(fr > 0);

            _secs = s;
            _framerateFactor = 1000 / Gcd(1000, fr);
            _framerate = fr * _framerateFactor;
            _numFrames = frames * _framerateFactor;

            Normalize();
        }

        public Timestamp ConvertToFramerate(int newFramerate)
        {
            var ts = new Timestamp(this);

            if (ts.Framerate != newFramerate)
            {
                ts._framerateFactor = 1000 / Gcd(1000, newFramerate);
                ts._framerate = newFramerate * ts._framerateFactor;

                var g = Gcd(_framerate, ts._framerate);
                var p = _framerate / g;
                var q = ts._framerate / g;

                // Convert the frame offset to the new framerate.
                // We round to the nearest (as opposed to always
                // rounding down), to minimize rounding errors during
                // round trip conversions.
                ts._numFrames = (ts._numFrames * q + p / 2) / p;

                ts.Normalize();
            }

            return ts;
        }

        void Normalize()
        {
            // Convert negative _numFrames values to positive ones by adjusting _secs
            if (_numFrames < 0)
            {
                var secsub = 1 + (-_numFrames / _framerate);

                _numFrames += _framerate * secsub;
                _secs -= secsub;
            }

            // Wrap around if necessary
            _secs += (_numFrames / _framerate);
            _numFrames %= _framerate;
        }

        public Timestamp AddFrames(int frames)
        {
            var ts = new Timestamp(this);

            // The frames are given in the original framerate, so we have to
            // adjust by _framerateFactor accordingly.
            ts._numFrames += frames * _framerateFactor;
            ts.Normalize();

            return ts;
        }

        public Timestamp AddMsecs(int ms)
        {
            var ts = new Timestamp(this);
            ts._secs += ms / 1000;
            // Add the remaining frames. Note that _framerate is always divisible by 1000.
            ts._numFrames += (ms % 1000) * (ts._framerate / 1000);

            ts.Normalize();

            return ts;
        }

        public int FrameDiff(Timestamp ts)
        {
            var delta = 0;
            if (_secs != ts._secs)
                delta = (_secs - ts._secs) * _framerate;

            delta += _numFrames;

            if (_framerate == ts._framerate)
            {
                delta -= ts._numFrames;
            }
            else
            {
                // We need to multiply by the quotient of the two framerates.
                // We cancel the GCD in this fraction to reduce the risk of
                // overflows.
                var g = Gcd(_framerate, ts._framerate);
                var p = _framerate / g;
                var q = ts._framerate / g;

                delta -= (int)(((long)ts._numFrames * p + q / 2) / (long)q);
            }

            return delta / _framerateFactor;
        }

        /// <summary>
        /// Euclid's algorithm to compute the greatest common divisor.
        /// </summary>
        static int Gcd(int a, int b)
        {
            // Note: We check for <= instead of < to avoid spurious compiler
            // warnings if T is an unsigned type, i.e. warnings like "comparison
            // of unsigned expression < 0 is always false".
            if (a <= 0)
                a = -a;
            if (b <= 0)
                b = -b;

            while (a > 0)
            {
                var tmp = a;
                a = b % a;
                b = tmp;
            }

            return b;
        }

        public static bool operator <(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) < 0;
        }

        public static bool operator <=(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) <= 0;
        }

        public static bool operator >(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) > 0;
        }

        public static bool operator >=(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) >= 0;
        }

        public static bool operator ==(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) == 0;
        }

        public static bool operator !=(Timestamp t1, Timestamp t2)
        {
            return t1.CompareTo(t2) != 0;
        }

        public static Timestamp operator +(Timestamp ts1, Timestamp ts2)
        {
            var result = new Timestamp(ts1);
            result.AddIntern(ts2);
            return result;
        }

        public static Timestamp operator -(Timestamp ts)
        {
            var result = new Timestamp(ts);
            result._secs = -result._secs;
            result._numFrames = -result._numFrames;
            result.Normalize();
            return result;
        }

        public static Timestamp operator -(Timestamp ts1, Timestamp ts2)
        {
            var result = new Timestamp(ts1);
            result.AddIntern(-ts2);
            return result;
        }

        public override int GetHashCode()
        {
            return _secs ^ _framerate ^ _numFrames ^ _framerateFactor;
        }

        public override bool Equals(object obj)
        {
            return obj is Timestamp && Equals((Timestamp)obj);
        }

        public bool Equals(Timestamp value)
        {
            return CompareTo(value) == 0;
        }

        void AddIntern(Timestamp ts)
        {
            Debug.Assert(_framerate == ts._framerate);
            _secs += ts._secs;
            _numFrames += ts._numFrames;

            Normalize();
        }

        #region IComparable implementation

        public int CompareTo(Timestamp other)
        {
            var delta = _secs - other._secs;
            if (delta == 0)
            {
                var g = Gcd(_framerate, other._framerate);
                var p = _framerate / g;
                var q = other._framerate / g;

                delta = (_numFrames * q - other._numFrames * p);
            }

            return delta;
        }

        #endregion
    }
}

