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

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
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