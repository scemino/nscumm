using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NScumm.Core
{
    /// <summary>
    /// A simple rational class that holds fractions.
    /// </summary>
    class Rational
    {
        private int _numerator;
        private int _denominator;

        public int Numerator
        {
            get { return _numerator; }
        }

        public int Denominator { get { return _denominator; } }

        public Rational(Rational rational)
            : this(rational.Numerator, rational.Denominator)
        {
        }

        public Rational(int num = 1, int denom = 1)
        {
            _numerator = num;
            _denominator = denom;
        }

        public static implicit operator int (Rational rational)
        {
            return rational.Numerator / rational.Denominator;
        }

        public static implicit operator double (Rational rational)
        {
            return (double)rational.Numerator / rational.Denominator;
        }

        public Rational Inverse()
        {
            var inverse = new Rational(this);
            inverse.Invert();
            return inverse;
        }

        private void Invert()
        {
            Debug.Assert(Numerator != 0);

            ScummHelper.Swap(ref _numerator, ref _denominator);

            if (_denominator < 0)
            {
                _denominator = -_denominator;
                _numerator = -_numerator;
            }
        }

        public static Rational operator *(Rational left, Rational right)
        {
            // Cross-cancel to avoid unnecessary overflow;
            // the result then is automatically normalized
            int gcd1 = Gcd(left.Numerator, right.Denominator);
            int gcd2 = Gcd(right.Numerator, left.Denominator);

            var num = left.Numerator / gcd1 * (right.Numerator / gcd2);
            var denominator = left.Denominator / gcd2 * (right.Denominator / gcd1);

            return new Rational(num, denominator);
        }

        public static int Gcd(int a, int b)
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
                int tmp = a;
                a = b % a;
                b = tmp;
            }

            return b;
        }
    }
}
