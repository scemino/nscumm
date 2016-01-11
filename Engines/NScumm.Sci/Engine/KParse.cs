//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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


using NScumm.Sci.Parser;
using System;
using System.Collections.Generic;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kParse(EngineState s, int argc, StackPtr? argv)
        {
            SegManager segMan = s._segMan;
            Register stringpos = argv.Value[0];
            string @string = s._segMan.GetString(stringpos);
            string error;
            Register @event = argv.Value[1];
            SciEngine.Instance.CheckVocabularySwitch();
            Vocabulary voc = SciEngine.Instance.Vocabulary;
            voc.parser_event = @event;
            Register[] @params = new[] { s._segMan.ParserPtr, stringpos };

            var words = new List<ResultWordList>();

            bool res = voc.TokenizeString(words, @string, out error);
            voc.parserIsValid = false; /* not valid */

            if (res && words.Count != 0)
            {
                voc.SynonymizeTokens(words);

                s.r_acc = Register.Make(0, 1);

#if DEBUG_PARSER
        debugC(kDebugLevelParser, "Parsed to the following blocks:");

		for (ResultWordListList::const_iterator i = words.begin(); i != words.end(); ++i) {

            debugCN(2, kDebugLevelParser, "   ");
			for (ResultWordList::const_iterator j = i.begin(); j != i.end(); ++j) {

                debugCN(2, kDebugLevelParser, "%sType[%04x] Group[%04x]", j == i.begin() ? "" : " / ", j._class, j._group);
			}

            debugCN(2, kDebugLevelParser, "\n");
}
#endif

                int syntax_fail = voc.ParseGNF(words);

                if (syntax_fail != 0)
                {
                    s.r_acc = Register.Make(0, 1);

                    SciEngine.WriteSelectorValue(segMan, @event, o => o.claimed, 1);

                    SciEngine.InvokeSelector(s, SciEngine.Instance.GameObject, o => o.syntaxFail, argc, argv, 2, new StackPtr(@params, 0));
                    /* Issue warning */

                    // TODO: debugC(kDebugLevelParser, "Tree building failed");

                }
                else {
                    voc.parserIsValid = true;

                    SciEngine.WriteSelectorValue(segMan, @event, o => o.claimed, 0);

#if DEBUG_PARSER
			voc.dumpParseTree();
#endif
                }

            }
            else {

                s.r_acc = Register.Make(0, 0);

                SciEngine.WriteSelectorValue(segMan, @event, o => o.claimed, 1);

                if (!string.IsNullOrEmpty(error))
                {
                    s._segMan.Strcpy(s._segMan.ParserPtr, error);

                    // TODO: debugC(kDebugLevelParser, "Word unknown: %s", error);
                    /* Issue warning: */

                    SciEngine.InvokeSelector(s, SciEngine.Instance.GameObject, o => o.wordFail, argc, argv, 2, new StackPtr(@params, 0));

                    return Register.Make(0, 1); /* Tell them that it didn't work */
                }
            }

            return s.r_acc;
        }

        private static Register kSaid(EngineState s, int argc, StackPtr? argv)
        {
            Register heap_said_block = argv.Value[0];
            Vocabulary voc = SciEngine.Instance.Vocabulary;
#if DEBUG_PARSER
            const bool debug_parser = true;
#else
            const bool debug_parser = false;
#endif

            if (heap_said_block.Segment == 0)
                return Register.NULL_REG;

            var said_block = s._segMan.DerefBulkPtr(heap_said_block, 0);

            if (said_block == null)
            {
                // TODO: warning("Said on non-string, pointer %04x:%04x", PRINT_REG(heap_said_block));
                return Register.NULL_REG;
            }

#if DEBUG_PARSER
            debugN("Said block: ");
            SciEngine.Instance.Vocabulary.DebugDecipherSaidBlock(said_block);
#endif

            if (voc.parser_event.IsNull || (SciEngine.ReadSelectorValue(s._segMan, voc.parser_event, o => o.claimed) != 0))
            {
                return Register.NULL_REG;
            }

            var new_lastmatch = Said.said(said_block, debug_parser);
            if (new_lastmatch != Vocabulary.SAID_NO_MATCH)
            { /* Build and possibly display a parse tree */

#if DEBUG_PARSER
                debugN("kSaid: Match.\n");
#endif

                s.r_acc = Register.Make(0, 1);

                if (new_lastmatch != Vocabulary.SAID_PARTIAL_MATCH)
                    SciEngine.WriteSelectorValue(s._segMan, voc.parser_event, o => o.claimed, 1);

            }
            else {
                return Register.NULL_REG;
            }
            return s.r_acc;
        }

        private static Register kSetSynonyms(EngineState s, int argc, StackPtr? argv)
        {
            SegManager segMan = s._segMan;
            Register @object = argv.Value[0];
            List list;
            Node node;
            int script;
            int numSynonyms = 0;
            Vocabulary voc = SciEngine.Instance.Vocabulary;

            // Only SCI0-SCI1 EGA games had a parser. In newer versions, this is a stub
            if (ResourceManager.GetSciVersion() > SciVersion.V1_EGA_ONLY)
                return s.r_acc;

            voc.ClearSynonyms();

            list = s._segMan.LookupList(SciEngine.ReadSelector(segMan, @object, SciEngine.Selector(o => o.elements)));
            node = s._segMan.LookupNode(list.first);

            while (node != null)
            {
                Register objpos = node.value;
                int seg;

                script = (int)SciEngine.ReadSelectorValue(segMan, objpos, SciEngine.Selector(o => o.number));
                seg = s._segMan.GetScriptSegment(script);

                if (seg > 0)
                    numSynonyms = s._segMan.GetScript(seg).SynonymsNr;

                if (numSynonyms != 0)
                {
                    var synonyms = s._segMan.GetScript(seg).Synonyms;

                    if (synonyms != null)
                    {
                        // TODO: debugC(kDebugLevelParser, "Setting %d synonyms for script.%d",
                        //          numSynonyms, script);

                        if (numSynonyms > 16384)
                        {
                            throw new InvalidOperationException("Segtable corruption: script.{script} has {numSynonyms} synonyms");
                            /* We used to reset the corrupted value here. I really don't think it's appropriate.
                             * Lars */
                        }
                        else
                            for (int i = 0; i < numSynonyms; i++)
                            {
                                Synonym tmp = new Synonym();
                                tmp.replaceant = synonyms.ReadUInt16(i * 4);
                                tmp.replacement = synonyms.ReadUInt16(i * 4 + 2);
                                voc.AddSynonym(tmp);
                            }
                    }
                    else {
                        // TODO: warning("Synonyms of script.%03d were requested, but script is not available", script);
                    }

                }

                node = s._segMan.LookupNode(node.succ);
            }

            // TODO: debugC(kDebugLevelParser, "A total of %d synonyms are active now.", numSynonyms);

            return s.r_acc;
        }
    }
}