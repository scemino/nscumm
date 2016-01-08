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


using NScumm.Sci.Engine;
using System.Collections.Generic;
using System;
using NScumm.Core;
using System.Text;
using System.Collections;
using NScumm.Core.Common;
using System.Linq;

namespace NScumm.Sci.Parser
{
    internal enum ParseTypes
    {
        WordNode = 4,
        LeafNode = 5,
        BranchNode = 6
    }

    internal enum VocabularyVersions
    {
        SCI0 = 0,
        SCI1 = 1
    }

    internal struct ResultWord
    {
        /// <summary>
        /// Word class
        /// </summary>
        public int _class;
        /// <summary>
        /// Word group
        /// </summary>
        public int _group;
    }

    internal class ResultWordList : List<ResultWord>
    {
    }

    internal class WordMap : IDictionary<string, ResultWordList>
    {
        private Dictionary<string, ResultWordList> _items = new Dictionary<string, ResultWordList>();

        public ResultWordList this[string key]
        {
            get
            {
                if (!_items.ContainsKey(key))
                {
                    _items.Add(key, new ResultWordList());
                }
                return _items[key];
            }

            set
            {
                _items[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return _items.Keys;
            }
        }

        public ICollection<ResultWordList> Values
        {
            get
            {
                return _items.Values;
            }
        }

        public void Add(KeyValuePair<string, ResultWordList> item)
        {
            ((IDictionary<string, ResultWordList>)_items).Add(item);
        }

        public void Add(string key, ResultWordList value)
        {
            _items.Add(key, value);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(KeyValuePair<string, ResultWordList> item)
        {
            return _items.ContainsKey(item.Key);
        }

        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ResultWordList>[] array, int arrayIndex)
        {
            ((IDictionary<string, ResultWordList>)_items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, ResultWordList>> GetEnumerator()
        {
            return ((IDictionary<string, ResultWordList>)_items).GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, ResultWordList> item)
        {
            return ((IDictionary<string, ResultWordList>)_items).Remove(item);
        }

        public bool Remove(string key)
        {
            return _items.Remove(key);
        }

        public bool TryGetValue(string key, out ResultWordList value)
        {
            return _items.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }

    internal class Synonym
    {
        /// <summary>
        /// The word group to replace
        /// </summary>
        public ushort replaceant;
        /// <summary>
        /// The replacement word group for this one
        /// </summary>
        public ushort replacement;
    }

    internal class Suffix
    {
        /// <summary>
        /// the word class this suffix applies to
        /// </summary>
        public int class_mask;
        /// <summary>
        /// the word class a word is morphed to if it doesn't fail this check
        /// </summary>
        public int result_class;

        /// <summary>
        /// String length of the suffix
        /// </summary>
        public int alt_suffix_length;
        /// <summary>
        /// String length of the other suffix
        /// </summary>
        public int word_suffix_length;

        /// <summary>
        /// The alternative suffix
        /// </summary>
        public string alt_suffix;
        /// <summary>
        /// The suffix as used in the word vocabulary
        /// </summary>
        public string word_suffix;
    }

    internal class ParseTreeNode
    {
        /// <summary>
        /// leaf or branch
        /// </summary>
        public ParseTypes type;
        /// <summary>
        /// For leaves
        /// </summary>
        public int value;
        /// <summary>
        /// Left child, for branches
        /// </summary>
        public ParseTreeNode left;
        /// <summary>
        /// Right child, for branches (and word leaves)
        /// </summary>
        public ParseTreeNode right;
    }

    internal class ParseTreeBranch
    {
        public int id;
        public int[] data = new int[10];
    }

    internal class AltInput
    {
        public string _input;
        public string _replacement;
        public int _inputLength;
        public bool _prefix;
    }

    internal class Vocabulary
    {
        /// <summary>
        /// Number of nodes for each ParseTreeNode structure 
        /// </summary>
        private const int VOCAB_TREE_NODES = 500;

        private const int VOCAB_TREE_NODE_LAST_WORD_STORAGE = 0x140;
        private const int VOCAB_TREE_NODE_COMPARE_TYPE = 0x146;
        private const int VOCAB_TREE_NODE_COMPARE_GROUP = 0x14d;
        private const int VOCAB_TREE_NODE_FORCE_STORAGE = 0x154;

        private const int VOCAB_RESOURCE_SELECTORS = 997;
        private const int VOCAB_RESOURCE_SCI0_MAIN_VOCAB = 0;
        private const int VOCAB_RESOURCE_SCI0_PARSE_TREE_BRANCHES = 900;
        private const int VOCAB_RESOURCE_SCI0_SUFFIX_VOCAB = 901;
        private const int VOCAB_RESOURCE_SCI1_MAIN_VOCAB = 900;
        private const int VOCAB_RESOURCE_SCI1_PARSE_TREE_BRANCHES = 901;
        private const int VOCAB_RESOURCE_SCI1_SUFFIX_VOCAB = 902;
        private const int VOCAB_RESOURCE_ALT_INPUTS = 913;

        private const int VOCAB_MAX_WORDLENGTH = 256;

        /* There was no 'last matching word': */
        public const int SAID_FULL_MATCH = 0xffff;
        public const int SAID_NO_MATCH = 0xfffe;
        public const int SAID_PARTIAL_MATCH = 0xfffd;

        /// <summary>
        /// The parse tree
        /// </summary>
        public ParseTreeNode[] _parserNodes = new ParseTreeNode[VOCAB_TREE_NODES];

        // Parser data:
        /// <summary>
        /// The event passed to Parse() and later used by Said()
        /// </summary>
        public Register parser_event;
        /// <summary>
        /// If something has been correctly parsed
        /// </summary>
        public bool parserIsValid;

        private ResourceManager _resMan;
        private VocabularyVersions _vocabVersion;

        private bool _foreign;
        private ushort _resourceIdWords;
        private ushort _resourceIdSuffixes;
        private ushort _resourceIdBranches;

        // Parser-related lists
        private List<Suffix> _parserSuffixes;
        /// <summary>
        /// GNF rules used in the parser algorithm 
        /// </summary>
        private ParseRuleList _parserRules;
        private List<ParseTreeBranch> _parserBranches;
        private WordMap _parserWords;
        /// <summary>
        /// The list of synonyms
        /// </summary>
        private List<Synonym> _synonyms;
        private List<List<AltInput>> _altInputs;


        public Vocabulary(ResourceManager resMan, bool foreign)
        {
            _resMan = resMan;
            _foreign = foreign;
            _parserSuffixes = new List<Suffix>();
            _parserBranches = new List<ParseTreeBranch>();
            _parserWords = new WordMap();
            _altInputs = new List<List<AltInput>>();

            // Mark parse tree as unused
            for (int i = 0; i < _parserNodes.Length; i++)
            {
                _parserNodes[i] = new ParseTreeNode();
            }
            _parserNodes[0].type = ParseTypes.LeafNode;
            _parserNodes[0].value = 0;
            _parserNodes[0].right = null;

            _synonyms = new List<Synonym>(); // No synonyms

            // TODO: debug(2, "Initializing vocabulary");
            if (_resMan.TestResource(new ResourceId(ResourceType.Vocab, VOCAB_RESOURCE_SCI0_MAIN_VOCAB)) != null)
            {
                _vocabVersion = VocabularyVersions.SCI0;
                _resourceIdWords = VOCAB_RESOURCE_SCI0_MAIN_VOCAB;
                _resourceIdSuffixes = VOCAB_RESOURCE_SCI0_SUFFIX_VOCAB;
                _resourceIdBranches = VOCAB_RESOURCE_SCI0_PARSE_TREE_BRANCHES;
            }
            else {
                _vocabVersion = VocabularyVersions.SCI1;
                _resourceIdWords = VOCAB_RESOURCE_SCI1_MAIN_VOCAB;
                _resourceIdSuffixes = VOCAB_RESOURCE_SCI1_SUFFIX_VOCAB;
                _resourceIdBranches = VOCAB_RESOURCE_SCI1_PARSE_TREE_BRANCHES;
            }

            if (_foreign)
            {
                _resourceIdWords += 10;
                _resourceIdSuffixes += 10;
                _resourceIdBranches += 10;
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_EGA_ONLY && LoadParserWords())
            {
                LoadSuffixes();
                if (LoadBranches())
                {
                    // Now build a GNF grammar out of this
                    _parserRules = BuildGNF();
                }
            }
            else {
                // TODO: debug(2, "Assuming that this game does not use a parser.");
                _parserRules = null;
            }

            LoadAltInputs();

            parser_event = Register.NULL_REG;
            parserIsValid = false;
        }

        public void Reset()
        {
            parserIsValid = false; // Invalidate parser
            parser_event = Register.NULL_REG; // Invalidate parser event
        }

        private bool LoadAltInputs()
        {
            var resource = _resMan.FindResource(new ResourceId(ResourceType.Vocab, VOCAB_RESOURCE_ALT_INPUTS), true);

            if (resource == null)
                return true; // it's not a problem if this resource doesn't exist

            var data = resource.data;
            var end = resource.size;

            _altInputs.Clear();
            for (int i = 0; i < 256; i++)
            {
                _altInputs.Add(new List<AltInput>());
            }

            for (int i = 0; i < end && data[i] != 0;)
            {
                AltInput t = new AltInput();
                t._input = new string(data.GetByteText(i).Select(b => (char)b).ToArray());
                t._inputLength = t._input.Length;
                i += t._input.Length + 1;

                t._replacement = new string(data.GetByteText(i).Select(b => (char)b).ToArray());
                i += t._replacement.Length + 1;

                if (i < end && data.GetText(i) == t._input)
                    t._prefix = true;
                else
                    t._prefix = false;

                var firstChar = (byte)t._input[0];
                _altInputs[firstChar].Insert(0, t);
            }

            return true;
        }

        private ParseRuleList BuildGNF()
        {
            int iterations = 0;
            int termrules = 0;
            int ntrules_nr;
            ParseRuleList ntlist = null;
            ParseRuleList tlist, new_tlist;
            //Console con = g_sci.getSciDebugger();

            for (int i = 1; i < _parserBranches.Count; i++)
            { // branch rule 0 is treated specially
                var rule = _vbuild_rule(_parserBranches[i]);
                if (rule == null)
                {
                    FreeRuleList(ntlist);
                    return null;
                }
                ntlist = _vocab_add_rule(ntlist, rule);
            }

            tlist = _vocab_split_rule_list(ntlist);
            ntrules_nr = _vocab_rule_list_length(ntlist);

            // TODO: verbose
            //if (verbose)
            //    con.debugPrintf("Starting with %d rules\n", ntrules_nr);

            new_tlist = tlist;
            tlist = null;

            do
            {
                ParseRuleList new_new_tlist = null;
                ParseRuleList ntseeker, tseeker;

                ntseeker = ntlist;
                while (ntseeker != null)
                {
                    tseeker = new_tlist;

                    while (tseeker != null)
                    {
                        ParseRule newrule = _vinsert(ntseeker.rule, tseeker.rule);
                        if (newrule != null)
                            new_new_tlist = _vocab_add_rule(new_new_tlist, newrule);
                        tseeker = tseeker.next;
                    }

                    ntseeker = ntseeker.next;
                }

                tlist = _vocab_merge_rule_lists(tlist, new_tlist);
                new_tlist = new_new_tlist;
                termrules = _vocab_rule_list_length(new_new_tlist);

                // TODO: verbose
                //if (verbose)
                //    con.debugPrintf("After iteration #%d: %d new term rules\n", ++iterations, termrules);

            } while ((termrules != 0) && (iterations < 30));

            FreeRuleList(ntlist);

            // TODO: verbose
            //if (verbose)
            //{
            //    con.debugPrintf("\nGNF rules:\n");
            //    tlist.print();
            //    con.debugPrintf("%d allocd rules\n", _allocd_rules);
            //    con.debugPrintf("Freeing rule list...\n");
            //    freeRuleList(tlist);
            //    return NULL;
            //}

            return tlist;
        }

        private static ParseRuleList _vocab_merge_rule_lists(ParseRuleList l1, ParseRuleList l2)
        {
            ParseRuleList retval = l1, seeker = l2;
            while (seeker != null)
            {
                retval = _vocab_add_rule(retval, seeker.rule);
                seeker = seeker.next;
            }
            _vocab_free_empty_rule_list(l2);

            return retval;
        }

        private static void _vocab_free_empty_rule_list(ParseRuleList list)
        {
            if (list.next != null)
                _vocab_free_empty_rule_list(list.next);
            list.next = null;
            list.rule = null;
            list.Dispose();
        }

        private static ParseRule _vinsert(ParseRule turkey, ParseRule stuffing)
        {
            uint firstnt = turkey._firstSpecial;

            // Search for first TOKEN_NON_NT in 'turkey'
            while ((firstnt < turkey._data.Length) && (turkey._data[firstnt] & ParseRuleList.TOKEN_NON_NT) != 0)
                firstnt++;

            // If no TOKEN_NON_NT found, or if it doesn't match the id of 'stuffing', abort.
            if ((firstnt == turkey._data.Length) || (turkey._data[firstnt] != stuffing._id))
                return null;

            // Create a new rule as a copy of 'turkey', where the token firstnt has been substituted
            // by the rule 'stuffing'.
            ++ParseRule._allocd_rules;

            var rule = new ParseRule(turkey);
            rule._numSpecials += stuffing._numSpecials - 1;
            rule._firstSpecial = firstnt + stuffing._firstSpecial;
            rule._data = new int[turkey._data.Length - 1 + stuffing._data.Length];

            // Replace rule._data[firstnt] by all of stuffing._data
            Array.Copy(stuffing._data, 0, rule._data, (int)firstnt, stuffing._data.Length);

            if (firstnt < turkey._data.Length - 1)
            {
                Array.Copy(turkey._data, (int)(firstnt + 1), rule._data, (int)(firstnt + stuffing._data.Length), (int)(turkey._data.Length - (firstnt + 1)));
            }

            return rule;
        }

        public bool CheckAltInput(string text, ushort cursorPos)
        {
            throw new NotImplementedException();
        }

        private static int _vocab_rule_list_length(ParseRuleList list)
        {
            return ((list != null) ? _vocab_rule_list_length(list.next) + 1 : 0);
        }

        private static ParseRuleList _vocab_split_rule_list(ParseRuleList list)
        {
            if (list.next == null || (list.next.terminal != 0))
            {
                ParseRuleList tmp = list.next;
                list.next = null;
                return tmp;
            }
            else
                return _vocab_split_rule_list(list.next);
        }

        private static ParseRuleList _vocab_add_rule(ParseRuleList list, ParseRule rule)
        {
            if (rule == null)
                return list;
            if (rule._data.Length == 0)
            {
                // Special case for qfg2 demo
                // TODO: warning("no rule contents on _vocab_add_rule()");
                return list;
            }

            ParseRuleList new_elem = new ParseRuleList(rule);

            if (list != null)
            {
                var term = new_elem.terminal;
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
                            new_elem.Dispose(); // NB: This also deletes 'rule'

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

        private static void FreeRuleList(ParseRuleList list)
        {
            list.Dispose();
        }

        private static ParseRule _vbuild_rule(ParseTreeBranch branch)
        {

            int tokens = 0, tokenpos = 0, i;

            while (tokenpos < 10 && branch.data[tokenpos] != 0)
            {
                int type = branch.data[tokenpos];
                tokenpos += 2;

                if ((type == VOCAB_TREE_NODE_COMPARE_TYPE) || (type == VOCAB_TREE_NODE_COMPARE_GROUP) || (type == VOCAB_TREE_NODE_FORCE_STORAGE))
                    ++tokens;
                else if (type > VOCAB_TREE_NODE_LAST_WORD_STORAGE)
                    tokens += 5;
                else
                    return null; // invalid
            }

            ParseRule rule = new ParseRule();

            ++ParseRule._allocd_rules;
            rule._id = branch.id;
            rule._numSpecials = (uint)tokenpos >> 1;
            rule._data = new int[tokens];
            rule._firstSpecial = 0;

            tokens = 0;
            for (i = 0; i < tokenpos; i += 2)
            {
                int type = branch.data[i];
                int value = branch.data[i + 1];

                if (type == VOCAB_TREE_NODE_COMPARE_TYPE)
                    rule._data[tokens++] = value | ParseRuleList.TOKEN_TERMINAL_CLASS;
                else if (type == VOCAB_TREE_NODE_COMPARE_GROUP)
                    rule._data[tokens++] = value | ParseRuleList.TOKEN_TERMINAL_GROUP;
                else if (type == VOCAB_TREE_NODE_FORCE_STORAGE)
                    rule._data[tokens++] = value | ParseRuleList.TOKEN_STUFFING_WORD;
                else { // normal inductive rule
                    unchecked
                    {
                        rule._data[tokens++] = (int)(ParseRuleList.TOKEN_OPAREN);
                    }
                    rule._data[tokens++] = type | ParseRuleList.TOKEN_STUFFING_LEAF;
                    rule._data[tokens++] = value | ParseRuleList.TOKEN_STUFFING_LEAF;

                    if (i == 0)
                        rule._firstSpecial = (uint)tokens;

                    rule._data[tokens++] = value; // The non-terminal
                    unchecked
                    {
                        rule._data[tokens++] = (int)ParseRuleList.TOKEN_CPAREN;
                    }
                }
            }

            return rule;
        }

        private bool LoadBranches()
        {
            var resource = _resMan.FindResource(new ResourceId(ResourceType.Vocab, _resourceIdBranches), false);

            _parserBranches.Clear();

            if (resource == null)
                return false;       // No parser tree data found

            int branches_nr = resource.size / 20;

            if (branches_nr == 0)
            {
                // TODO: warning("Parser tree data is empty");
                return false;
            }

            for (int i = 0; i < branches_nr; i++)
            {
                _parserBranches.Add(new ParseTreeBranch());
            }

            for (int i = 0; i < branches_nr; i++)
            {
                ByteAccess @base = new ByteAccess(resource.data, i * 20);

                _parserBranches[i].id = @base.ReadInt16();

                for (int k = 0; k < 9; k++)
                    _parserBranches[i].data[k] = @base.ReadUInt16(2 + 2 * k);

                _parserBranches[i].data[9] = 0; // Always terminate
            }

            if (_parserBranches[branches_nr - 1].id == 0) // branch lists may be terminated by empty rules
                _parserBranches.RemoveAt(branches_nr - 1);

            return true;
        }

        private bool LoadSuffixes()
        {
            // Determine if we can find a SCI1 suffix vocabulary first
            var resource = _resMan.FindResource(new ResourceId(ResourceType.Vocab, _resourceIdSuffixes), true);
            if (resource == null)
                return false; // No vocabulary found

            int seeker = 1;

            while ((seeker < resource.size - 1) && (resource.data[seeker + 1] != 0xff))
            {
                var suffix = new Suffix();
                suffix.alt_suffix = resource.data.GetText(seeker);
                suffix.alt_suffix_length = suffix.alt_suffix.Length;
                seeker += suffix.alt_suffix_length + 1; // Hit end of string

                suffix.result_class = resource.data.ToInt16BigEndian(seeker);
                seeker += 2;

                // Beginning of next string - skip leading '*'
                seeker++;

                suffix.word_suffix = resource.data.GetText(seeker);
                suffix.word_suffix_length = suffix.word_suffix.Length;
                seeker += suffix.word_suffix_length + 1;

                suffix.class_mask = resource.data.ToInt16BigEndian(seeker);
                seeker += 3; // Next entry

                _parserSuffixes.Add(suffix);
            }

            return true;
        }

        private bool LoadParserWords()
        {
            char[] currentWord = new char[VOCAB_MAX_WORDLENGTH];
            int currentWordPos = 0;

            // First try to load the SCI0 vocab resource.
            var resource = _resMan.FindResource(new ResourceId(ResourceType.Vocab, _resourceIdWords), false);

            if (resource == null)
            {
                // TODO: warning("Could not find a main vocabulary");
                return false; // NOT critical: SCI1 games and some demos don't have one!
            }

            VocabularyVersions resourceType = _vocabVersion;

            if (resourceType == VocabularyVersions.SCI0)
            {
                if (resource.size < 26 * 2)
                {
                    // TODO: warning("Invalid main vocabulary encountered: Much too small");
                    return false;
                }
                // Check the alphabet-offset table for any content
                int alphabetNr;
                for (alphabetNr = 0; alphabetNr < 26; alphabetNr++)
                {
                    if (resource.data.ToUInt16(alphabetNr * 2) != 0)
                        break;
                }
                // If all of them were empty, we are definitely seeing SCI01 vocab in disguise (e.g. pq2 japanese)
                if (alphabetNr == 26)
                {
                    // TODO/ warning("SCI0: Found SCI01 vocabulary in disguise");
                    resourceType = VocabularyVersions.SCI1;
                }
            }

            uint seeker;
            if (resourceType == VocabularyVersions.SCI1)
                seeker = 255 * 2; // vocab.900 starts with 255 16-bit pointers which we don't use
            else
                seeker = 26 * 2; // vocab.000 starts with 26 16-bit pointers which we don't use

            if (resource.size < seeker)
            {
                // TODO: warning("Invalid main vocabulary encountered: Too small");
                return false;
                // Now this ought to be critical, but it'll just cause parse() and said() not to work
            }

            _parserWords.Clear();

            while (seeker < resource.size)
            {
                byte c;

                currentWordPos = resource.data[seeker++]; // Parts of previous words may be re-used

                if (resourceType == VocabularyVersions.SCI1)
                {
                    c = 1;
                    while (seeker < resource.size && currentWordPos < 255 && c != 0)
                    {
                        c = resource.data[seeker++];
                        currentWord[currentWordPos++] = (char)c;
                    }
                    if (seeker == resource.size)
                    {
                        // TODO: warning("SCI1: Vocabulary not usable, disabling");
                        _parserWords.Clear();
                        return false;
                    }
                }
                else {
                    do
                    {
                        c = resource.data[seeker++];
                        currentWord[currentWordPos++] = (char)(c & 0x7f); // 0x80 is used to terminate the string
                    } while (c < 0x80);
                }

                currentWord[currentWordPos] = '\0';

                // Now decode class and group:
                c = resource.data[seeker + 1];
                ResultWord newWord;
                newWord._class = ((resource.data[seeker]) << 4) | ((c & 0xf0) >> 4);
                newWord._group = (resource.data[seeker + 2]) | ((c & 0x0f) << 8);

                // SCI01 was the first version to support multiple class/group pairs
                // per word, so we clear the list in earlier versions
                // in earlier versions.
                if (ResourceManager.GetSciVersion() < SciVersion.V01)
                    _parserWords[new string(currentWord)].Clear();

                // Add this to the list of possible class,group pairs for this word
                _parserWords[new string(currentWord)].Add(newWord);

                seeker += 3;
            }

            return true;
        }

        public void ClearSynonyms()
        {
            _synonyms.Clear();
        }

        public void AddSynonym(Synonym syn)
        {
            _synonyms.Add(syn);
        }
    }
}
