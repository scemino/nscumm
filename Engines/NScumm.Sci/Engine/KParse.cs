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
using NScumm.Core;
using static NScumm.Core.DebugHelper;
using NScumm.Core.Graphics;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kIntersections(EngineState s, int argc, StackPtr argv)
        {
            // This function computes intersection points for the "freeway pathing" in MUMG CD.
            int qSourceX = argv[0].ToInt16();
            int qSourceY = argv[1].ToInt16();
            int qDestX = argv[2].ToInt16();
            int qDestY = argv[3].ToInt16();
            ushort startIndex = argv[5].ToUInt16();
            ushort endIndex = argv[6].ToUInt16();
            ushort stepSize = argv[7].ToUInt16();
            bool backtrack = argv[9].ToUInt16() != 0;

            int kVertical = 0x7fffffff;

            ushort curIndex = startIndex;
            StackPtr? inpBuf = s._segMan.DerefRegPtr(argv[4], endIndex + 2);

            if (!inpBuf.HasValue)
            {
                Warning("Intersections: input buffer invalid");
                return Register.NULL_REG;
            }

            StackPtr? outBuf = s._segMan.DerefRegPtr(argv[8], (endIndex - startIndex + 2) / stepSize * 3);

            if (!outBuf.HasValue)
            {
                Warning("Intersections: output buffer invalid");
                return Register.NULL_REG;
            }

            // Slope and y-intercept of the query line in centipixels
            int qIntercept;
            int qSlope;

            if (qSourceX != qDestX)
            {
                // Compute slope of the line and round to nearest centipixel
                qSlope = (1000 * (qSourceY - qDestY)) / (qSourceX - qDestX);

                if (qSlope >= 0)
                    qSlope += 5;
                else
                    qSlope -= 5;

                qSlope /= 10;

                // Compute y-intercept in centipixels
                qIntercept = (100 * qDestY) - (qSlope * qDestX);

                if (backtrack)
                {
                    // If backtrack is set we extend the line from dest to source
                    // until we hit a screen edge and place the source point there

                    // First we try to place the source point on the left or right
                    // screen edge
                    if (qSourceX >= qDestX)
                        qSourceX = 319;
                    else
                        qSourceX = 0;

                    // Compute the y-coordinate
                    qSourceY = ((qSlope * qSourceX) + qIntercept) / 100;

                    // If the y-coordinate is off-screen, the point we want is on the
                    // top or bottom edge of the screen instead
                    if (qSourceY < 0 || qSourceY > 189)
                    {
                        if (qSourceY < 0)
                            qSourceY = 0;
                        else if (qSourceY > 189)
                            qSourceY = 189;

                        // Compute the x-coordinate
                        qSourceX = (((((qSourceY * 100) - qIntercept) * 10) / qSlope) + 5) / 10;
                    }
                }
            }
            else
            {
                // The query line is vertical
                qIntercept = qSlope = kVertical;

                if (backtrack)
                {
                    // If backtrack is set, extend to screen edge
                    if (qSourceY >= qDestY)
                        qSourceY = 189;
                    else
                        qSourceY = 0;
                }
            }

            int pSourceX = inpBuf.Value[curIndex].ToInt16();
            int pSourceY = inpBuf.Value[curIndex + 1].ToInt16();

            // If it's a polygon, we include the first point again at the end
            short doneIndex;
            if ((pSourceX & (1 << 13)) != 0)
                doneIndex = (short)startIndex;
            else
                doneIndex = (short)endIndex;

            pSourceX &= 0x1ff;

            //debugCN(kDebugLevelAvoidPath, "%s: (%i, %i)[%i]",
            //    (doneIndex == startIndex ? "Polygon" : "Polyline"), pSourceX, pSourceY, curIndex);

            curIndex += stepSize;
            ushort outCount = 0;

            while (true)
            {
                int pDestX = inpBuf.Value[curIndex].ToInt16() & 0x1ff;
                int pDestY = inpBuf.Value[curIndex + 1].ToInt16();

                // TODO: if (DebugMan.isDebugChannelEnabled(kDebugLevelAvoidPath))
                //{
                //    draw_line(s, Common::Point(pSourceX, pSourceY),
                //        Common::Point(pDestX, pDestY), 2, 320, 190);
                //    debugN(-1, " (%i, %i)[%i]", pDestX, pDestY, curIndex);
                //}

                // Slope and y-intercept of the polygon edge in centipixels
                int pIntercept;
                int pSlope;

                if (pSourceX != pDestX)
                {
                    // Compute slope and y-intercept (as above)
                    pSlope = ((pDestY - pSourceY) * 1000) / (pDestX - pSourceX);

                    if (pSlope >= 0)
                        pSlope += 5;
                    else
                        pSlope -= 5;

                    pSlope /= 10;

                    pIntercept = (pDestY * 100) - (pSlope * pDestX);
                }
                else
                {
                    // Polygon edge is vertical
                    pSlope = pIntercept = kVertical;
                }

                bool foundIntersection = true;
                int intersectionX = 0;
                int intersectionY = 0;

                if (qSlope == pSlope)
                {
                    // If the lines overlap, we test the source and destination points
                    // against the poly segment
                    if ((pIntercept == qIntercept) && (PointInRect(new Point(pSourceX, pSourceY), (short)qSourceX, (short)qSourceY, (short)qDestX, (short)qDestY)))
                    {
                        intersectionX = pSourceX * 100;
                        intersectionY = pSourceY * 100;
                    }
                    else if ((pIntercept == qIntercept) && PointInRect(new Point(qDestX, qDestY), (short)pSourceX, (short)pSourceY, (short)pDestX, (short)pDestY))
                    {
                        intersectionX = qDestX * 100;
                        intersectionY = qDestY * 100;
                    }
                    else
                    {
                        // Lines are parallel or segments don't overlap, no intersection
                        foundIntersection = false;
                    }
                }
                else
                {
                    // Lines are not parallel
                    if (qSlope == kVertical)
                    {
                        // Query segment is vertical, polygon segment is not vertical
                        intersectionX = qSourceX * 100;
                        intersectionY = pSlope * qSourceX + pIntercept;
                    }
                    else if (pSlope == kVertical)
                    {
                        // Polygon segment is vertical, query segment is not vertical
                        intersectionX = pDestX * 100;
                        intersectionY = qSlope * pDestX + qIntercept;
                    }
                    else
                    {
                        // Neither line is vertical
                        intersectionX = ((pIntercept - qIntercept) * 100) / (qSlope - pSlope);
                        intersectionY = ((intersectionX * pSlope) + (pIntercept * 100)) / 100;
                    }
                }

                if (foundIntersection)
                {
                    // Round back to pixels
                    intersectionX = (intersectionX + 50) / 100;
                    intersectionY = (intersectionY + 50) / 100;

                    // If intersection point lies on both the query line segment and the poly
                    // line segment, add it to the output
                    if (((PointInRect(new Point(intersectionX, intersectionY), (short)pSourceX, (short)pSourceY, (short)pDestX, (short)pDestY))
                        && PointInRect(new Point(intersectionX, intersectionY), (short)qSourceX, (short)qSourceY, (short)qDestX, (short)qDestY)))
                    {
                        var o = outBuf.Value;
                        o[outCount * 3] = Register.Make(0, (ushort)intersectionX);
                        o[outCount * 3 + 1] = Register.Make(0, (ushort)intersectionY);
                        o[outCount * 3 + 2] = Register.Make(0, curIndex);
                        outCount++;
                    }
                }

                if (curIndex == doneIndex)
                {
                    // TODO: End of polyline/polygon reached
                    //if (DebugMan.isDebugChannelEnabled(kDebugLevelAvoidPath))
                    //{
                    //    debug(";");
                    //    debugN(-1, "Found %i intersections", outCount);

                    //    if (outCount)
                    //    {
                    //        debugN(-1, ":");
                    //        for (int i = 0; i < outCount; i++)
                    //        {
                    //            Common::Point p = Common::Point(outBuf[i * 3].toSint16(), outBuf[i * 3 + 1].toSint16());
                    //            draw_point(s, p, 0, 320, 190);
                    //            debugN(-1, " (%i, %i)[%i]", p.x, p.y, outBuf[i * 3 + 2].toSint16());
                    //        }
                    //    }

                    //    debug(";");

                    //    g_sci._gfxScreen.copyToScreen();
                    //    g_system.updateScreen();
                    //}

                    return Register.Make(0, outCount);
                }

                if (curIndex != endIndex)
                {
                    // Go to next point in polyline/polygon
                    curIndex += stepSize;
                }
                else
                {
                    // Wrap-around for polygon case
                    curIndex = startIndex;
                }

                // Current destination point is source for the next line segment
                pSourceX = pDestX;
                pSourceY = pDestY;
            }
        }

        private static bool PointInRect(Point point, short rectX1, short rectY1, short rectX2, short rectY2)
        {
            short top = Math.Min(rectY1, rectY2);
            short left = Math.Min(rectX1, rectX2);
            short bottom = (short)(Math.Max(rectY1, rectY2) + 1);
            short right = (short)(Math.Max(rectX1, rectX2) + 1);

            Rect rect = new Rect(left, top, right, bottom);
            // Add a one pixel margin of error
            rect.Grow(1);

            return rect.Contains(point);
        }

        private static Register kParse(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            Register stringpos = argv[0];
            string @string = s._segMan.GetString(stringpos);
            string error;
            Register @event = argv[1];
            SciEngine.Instance.CheckVocabularySwitch();
            Vocabulary voc = SciEngine.Instance.Vocabulary;
            voc.parser_event = @event;
            Register[] @params = { s._segMan.ParserPtr, stringpos };

            var words = new List<ResultWordList>();

            bool res = voc.TokenizeString(words, @string, out error);
            voc.parserIsValid = false; /* not valid */

            if (res && words.Count != 0)
            {
                voc.SynonymizeTokens(words);

                s.r_acc = Register.Make(0, 1);

#if DEBUG_PARSER
        DebugC(DebugLevels.Parser, "Parsed to the following blocks:");

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

                    DebugC(DebugLevels.Parser, "Tree building failed");

                }
                else
                {
                    voc.parserIsValid = true;

                    SciEngine.WriteSelectorValue(segMan, @event, o => o.claimed, 0);

#if DEBUG_PARSER
			voc.dumpParseTree();
#endif
                }

            }
            else
            {

                s.r_acc = Register.Make(0, 0);

                SciEngine.WriteSelectorValue(segMan, @event, o => o.claimed, 1);

                if (!string.IsNullOrEmpty(error))
                {
                    s._segMan.Strcpy(s._segMan.ParserPtr, error);

                    DebugC(DebugLevels.Parser, "Word unknown: {0}", error);
                    /* Issue warning: */

                    SciEngine.InvokeSelector(s, SciEngine.Instance.GameObject, o => o.wordFail, argc, argv, 2, new StackPtr(@params, 0));

                    return Register.Make(0, 1); /* Tell them that it didn't work */
                }
            }

            return s.r_acc;
        }

        private static Register kSaid(EngineState s, int argc, StackPtr argv)
        {
            Register heap_said_block = argv[0];
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
                Warning($"Said on non-string, pointer {heap_said_block}");
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
            else
            {
                return Register.NULL_REG;
            }
            return s.r_acc;
        }

        private static Register kSetSynonyms(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            Register @object = argv[0];
            List list;
            Node node;
            int script;
            int numSynonyms = 0;
            Vocabulary voc = SciEngine.Instance.Vocabulary;

            // Only SCI0-SCI1 EGA games had a parser. In newer versions, this is a stub
            if (ResourceManager.GetSciVersion() > SciVersion.V1_EGA_ONLY)
                return s.r_acc;

            voc.ClearSynonyms();

            list = s._segMan.LookupList(SciEngine.ReadSelector(segMan, @object, o => o.elements));
            node = s._segMan.LookupNode(list.first);

            while (node != null)
            {
                Register objpos = node.value;
                int seg;

                script = (int)SciEngine.ReadSelectorValue(segMan, objpos, o => o.number);
                seg = s._segMan.GetScriptSegment(script);

                if (seg > 0)
                    numSynonyms = s._segMan.GetScript(seg).SynonymsNr;

                if (numSynonyms != 0)
                {
                    var synonyms = s._segMan.GetScript(seg).Synonyms;

                    if (synonyms != null)
                    {
                        DebugC(DebugLevels.Parser, "Setting {0} synonyms for script.{1}",
                                  numSynonyms, script);

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
                                tmp.replaceant = synonyms.ToUInt16(i * 4);
                                tmp.replacement = synonyms.ToUInt16(i * 4 + 2);
                                voc.AddSynonym(tmp);
                            }
                    }
                    else
                    {
                        Warning($"Synonyms of script.{script:D3} were requested, but script is not available");
                    }

                }

                node = s._segMan.LookupNode(node.succ);
            }

            DebugC(DebugLevels.Parser, "A total of {0} synonyms are active now.", numSynonyms);

            return s.r_acc;
        }
    }
}