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
using System.Collections.Generic;
using static NScumm.Core.DebugHelper;

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
        public int _firstSpecial;
        /// <summary>
        /// number of terminals and non-terminals
        /// </summary>
        public uint _numSpecials;
        /// <summary>
        /// actual data
        /// </summary>
        public List<int> _data;

        public ParseRule()
        {
        }

        public ParseRule(ParseRule rule)
        {
            _id = rule._id;
            _firstSpecial = rule._firstSpecial;
            _numSpecials = rule._numSpecials;
            _data = new List<int>(rule._data);
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

        public static ParseRuleList _vocab_clone_rule_list_by_id(ParseRuleList list, int id)
        {
            ParseRuleList result = null;
            ParseRuleList seeker = list;

            while (seeker != null)
            {
                if (seeker.rule._id == id)
                {
                    result = _vocab_add_rule(result, _vdup(seeker.rule));
                }
                seeker = seeker.next;
            }

            return result;
        }

        public static ParseRule _vsatisfy_rule(ParseRule rule, ResultWordList input)
        {
            if (rule._numSpecials == 0)
                return null;

            var dep = rule._data[rule._firstSpecial];

            int count = 0;
            int match = 0;
            // TODO: Inserting an array in the middle of another array is slow
            var matches = new List<int>();

            // We store the first match in 'match', and any subsequent matches in
            // 'matches'. 'match' replaces the special in the rule, and 'matches' gets
            // inserted after it.
            foreach (var iter in input)
                if (((dep & ParseRuleList.TOKEN_TERMINAL_CLASS) != 0 && ((dep & 0xffff) & iter._class) != 0) ||
                    ((dep & ParseRuleList.TOKEN_TERMINAL_GROUP) != 0 && ((dep & 0xffff) & iter._group) != 0))
                {
                    if (count == 0)
                        match = ParseRuleList.TOKEN_STUFFING_WORD | iter._group;
                    else
                        matches.Add(ParseRuleList.TOKEN_STUFFING_WORD | iter._group);
                    count++;
                }

            if (count != 0)
            {
                ParseRule retval = new ParseRule(rule);
                ++_allocd_rules;
                retval._data[rule._firstSpecial] = match;
                if (count > 1)
                    retval._data.InsertRange(rule._firstSpecial + 1, matches);
                retval._numSpecials--;
                retval._firstSpecial = 0;

                if (retval._numSpecials != 0)
                { // find first special, if it exists
                    for (var i = rule._firstSpecial; i < retval._data.Count; ++i)
                    {
                        int tmp = retval._data[i];
                        if ((tmp & ParseRuleList.TOKEN_NON_NT) == 0 || (tmp & ParseRuleList.TOKEN_TERMINAL) != 0)
                        {
                            retval._firstSpecial = i;
                            break;
                        }
                    }
                }

                return retval;
            }
            else
                return null;
        }

        public static int _vbpt_append(ParseTreeNode[] nodes, ref int pos, int @base, int value)
        {
            // writes one value to an existing base node and creates a successor node for writing
            nodes[@base].left = nodes[++pos];
            nodes[pos].type = ParseTypes.LeafNode;
            nodes[pos].value = value;
            nodes[pos].right = null;
            nodes[@base].right = nodes[++pos];
            nodes[pos].type = ParseTypes.BranchNode;
            nodes[pos].left = null;
            nodes[pos].right = null;
            return pos;
        }

        public static int _vbpt_write_subexpression(ParseTreeNode[] nodes, ref int pos, ParseRule rule, int rulepos, int writepos)
        {
            uint token;

            while ((token = ((rulepos < rule._data.Count) ? (uint)rule._data[rulepos++] : ParseRuleList.TOKEN_CPAREN)) != ParseRuleList.TOKEN_CPAREN)
            {
                uint nexttoken = (rulepos < rule._data.Count) ? (uint)rule._data[rulepos] : ParseRuleList.TOKEN_CPAREN;
                if (token == ParseRuleList.TOKEN_OPAREN)
                {
                    int writepos2 = _vbpt_pareno(nodes, ref pos, writepos);
                    rulepos = _vbpt_write_subexpression(nodes, ref pos, rule, rulepos, writepos2);
                    nexttoken = (rulepos < rule._data.Count) ? (uint)rule._data[rulepos] : ParseRuleList.TOKEN_CPAREN;
                    if (nexttoken != ParseRuleList.TOKEN_CPAREN)
                        writepos = _vbpt_parenc(nodes, ref pos, writepos);
                }
                else if ((token & ParseRuleList.TOKEN_STUFFING_LEAF) != 0)
                {
                    if (nexttoken == ParseRuleList.TOKEN_CPAREN)
                        writepos = _vbpt_terminate(nodes, ref pos, writepos, (int)(token & 0xffff));
                    else
                        writepos = _vbpt_append(nodes, ref pos, writepos, (int)(token & 0xffff));
                }
                else if ((token & ParseRuleList.TOKEN_STUFFING_WORD) != 0)
                {
                    if (nexttoken == ParseRuleList.TOKEN_CPAREN)
                        writepos = _vbpt_terminate_word(nodes, ref pos, writepos, (int)(token & 0xffff));
                    else
                        writepos = _vbpt_append_word(nodes, ref pos, writepos, (int)(token & 0xffff));
                }
                else {
                    // TODO: warning("\nError in parser (grammar.cpp, _vbpt_write_subexpression()): Rule data broken in rule ");
                    // TODO: vocab_print_rule(rule);
                    // TODO: debugN(", at token position %d\n", *pos);
                    return rulepos;
                }
            }

            return rulepos;
        }

        private static int _vbpt_terminate(ParseTreeNode[] nodes, ref int pos, int @base, int value)
        {
            // Terminates, overwriting a nextwrite forknode
            nodes[@base].type = ParseTypes.LeafNode;
            nodes[@base].value = value;
            nodes[@base].right = null;
            return pos;
        }
        private static int _vbpt_append_word(ParseTreeNode[] nodes, ref int pos, int @base, int value)
        {
            // writes one value to an existing node and creates a sibling for writing
            nodes[@base].type = ParseTypes.WordNode;
            nodes[@base].value = value;
            nodes[@base].right = nodes[++(pos)];
            nodes[pos].type = ParseTypes.BranchNode;
            nodes[pos].left = null;
            nodes[pos].right = null;
            return pos;
        }

        private static int _vbpt_terminate_word(ParseTreeNode[] nodes, ref int pos, int @base, int value)
        {
            // Terminates, overwriting a nextwrite forknode
            nodes[@base].type = ParseTypes.WordNode;
            nodes[@base].value = value;
            nodes[@base].right = null;
            return pos;
        }

        private static int _vbpt_parenc(ParseTreeNode[] nodes, ref int pos, int paren)
        {
            // Closes parentheses for appending
            nodes[paren].right = nodes[++(pos)];
            nodes[pos].type = ParseTypes.BranchNode;
            nodes[pos].left = null;
            nodes[pos].right = null;
            return pos;
        }

        private static int _vbpt_pareno(ParseTreeNode[] nodes, ref int pos, int @base)
        {
            // Opens parentheses
            nodes[@base].left = nodes[pos + 1];
            nodes[++pos].type = ParseTypes.BranchNode;
            nodes[pos].left = null;
            nodes[pos].right = null;
            return pos;
        }

        private static ParseRule _vdup(ParseRule a)
        {
            ++_allocd_rules;
            return new ParseRule(a);
        }

        private static ParseRuleList _vocab_add_rule(ParseRuleList list, ParseRule rule)
        {
            if (rule == null)
                return list;
            if (rule._data.Count == 0)
            {
                // Special case for qfg2 demo
                Warning("no rule contents on _vocab_add_rule()");
                return list;
            }

            ParseRuleList new_elem = new ParseRuleList(rule);

            if (list != null)
            {
                int term = new_elem.terminal;
                /*		if (term < list.terminal) {
                            new_elem.next = list;
                            return new_elem;
                        } else {*/
                ParseRuleList seeker = list;

                while (seeker.next != null/* && seeker.next.terminal <= term*/)
                {
                    if (seeker.next.terminal == term)
                    {
                        if (seeker.next.rule == rule)
                        {
                            //delete new_elem; // NB: This also deletes 'rule'

                            return list; // No duplicate rules
                        }
                    }
                    seeker = seeker.next;
                }

                new_elem.next = seeker.next;
                seeker.next = new_elem;
                return list;
            }
            else {
                return new_elem;
            }
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
