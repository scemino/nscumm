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

using System;

namespace NScumm.Sci.Parser
{
    internal class ParseRule : IDisposable
    {
        internal static int _allocd_rules = 0;	// FIXME: Avoid non-const global vars

        /// <summary>
        /// non-terminal ID
        /// </summary>
        public int _id;
        /// <summary>
        /// first terminal or non-terminal
        /// </summary>
        public uint _firstSpecial;
        /// <summary>
        /// number of terminals and non-terminals
        /// </summary>
        public uint _numSpecials;
        /// <summary>
        /// actual data
        /// </summary>
        public int[] _data;

        public ParseRule()
        {
        }

        public ParseRule(ParseRule rule)
        {
            _id = rule._id;
            _firstSpecial = rule._firstSpecial;
            _numSpecials = rule._numSpecials;
            _data = new int[rule._data.Length];
            Array.Copy(rule._data, _data, rule._data.Length);
        }

        ~ParseRule()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }

        public void Dispose()
        {
            --_allocd_rules;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ParseRule;
            if (ReferenceEquals(other, null)) return false;

            return _id == other._id &&
                _firstSpecial == other._firstSpecial &&
                _numSpecials == other._numSpecials &&
                _data == other._data;
        }

        public override int GetHashCode()
        {
            return _id ^ _firstSpecial.GetHashCode() ^ _numSpecials.GetHashCode();
        }

        // FIXME remove this one again?
        public static bool operator ==(ParseRule r1, ParseRule r2)
        {
            if (ReferenceEquals(r1, null) && ReferenceEquals(r2, null))
                return true;

            if (ReferenceEquals(r1, null))
                return false;

            return r1.Equals(r2);
        }

        public static bool operator !=(ParseRule r1, ParseRule r2)
        {
            return !(r1 == r2);
        }
    }

    internal class ParseRuleList : IDisposable
    {
        public const uint TOKEN_OPAREN = 0xff000000;
        public const uint TOKEN_CPAREN = 0xfe000000;
        public const int TOKEN_TERMINAL_CLASS = 0x10000;
        public const int TOKEN_TERMINAL_GROUP = 0x20000;
        public const int TOKEN_STUFFING_LEAF = 0x40000;
        public const int TOKEN_STUFFING_WORD = 0x80000;
        public const uint TOKEN_NON_NT = (TOKEN_OPAREN | TOKEN_TERMINAL_CLASS | TOKEN_TERMINAL_GROUP | TOKEN_STUFFING_LEAF | TOKEN_STUFFING_WORD);
        public const int TOKEN_TERMINAL = (TOKEN_TERMINAL_CLASS | TOKEN_TERMINAL_GROUP);

        /// <summary>
        /// Terminal character this rule matches against or 0 for a non-terminal rule
        /// </summary>
        public int terminal;
        public ParseRule rule;
        public ParseRuleList next;

        public ParseRuleList(ParseRule r)
        {
            rule = r;
            int term = rule._data[rule._firstSpecial];
            terminal = ((term & TOKEN_TERMINAL) != 0 ? term : 0);
        }

        ~ParseRuleList()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }

        public void Dispose()
        {
            if (rule != null)
            {
                rule.Dispose();
            }
            if (next != null)
            {
                next.Dispose();
            }
        }
    }
}
