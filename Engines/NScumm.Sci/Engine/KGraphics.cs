//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 3 of the License; or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful;
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not; see <http://www.gnu.org/licenses/>.


using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Graphics;
using System;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        const int K_DRAWPIC_FLAGS_MIRRORED = (1 << 14);
        const int K_DRAWPIC_FLAGS_ANIMATIONBLACKOUT = (1 << 15);

        private static Register kAddToPic(EngineState s, int argc, StackPtr? argv)
        {
            int viewId;
            short loopNo;
            short celNo;
            short leftPos, topPos, priority, control;

            switch (argc)
            {
                // Is this ever really gets called with 0 parameters, we need to set _picNotValid!!
                //case 0:
                //	break;
                case 1:
                    if (argv.Value[0].IsNull)
                        return s.r_acc;
                    SciEngine.Instance._gfxAnimate.KernelAddToPicList(argv.Value[0], argc, argv);
                    break;
                case 7:
                    viewId = argv.Value[0].ToUInt16();
                    loopNo = argv.Value[1].ToInt16();
                    celNo = argv.Value[2].ToInt16();
                    leftPos = argv.Value[3].ToInt16();
                    topPos = argv.Value[4].ToInt16();
                    priority = argv.Value[5].ToInt16();
                    control = argv.Value[6].ToInt16();
                    SciEngine.Instance._gfxAnimate.KernelAddToPicView(viewId, loopNo, celNo, leftPos, topPos, priority, control);
                    break;
                default:
                    throw new InvalidOperationException($"kAddToPic with unsupported parameter count {argc}");
            }
            return s.r_acc;
        }

        private static Register kAnimate(EngineState s, int argc, StackPtr? argv)
        {
            Register castListReference = (argc > 0) ? argv.Value[0] : Register.NULL_REG;
            bool cycle = (argc > 1) ? ((argv.Value[1].ToUInt16() != 0) ? true : false) : false;

            SciEngine.Instance._gfxAnimate.KernelAnimate(castListReference, cycle, argc, argv);

            // WORKAROUND: At the end of Ecoquest 1, during the credits, the game
            // doesn't call kGetEvent(), so no events are processed (e.g. window
            // focusing, window moving etc). We poll events for that scene, to
            // keep ScummVM responsive. Fixes ScummVM "freezing" during the credits,
            // bug #3101846
            if (SciEngine.Instance.GameId == SciGameId.ECOQUEST && s.CurrentRoomNumber == 680)
                SciEngine.Instance.EventManager.GetSciEvent(SciEvent.SCI_EVENT_PEEK);

            return s.r_acc;
        }

        private static Register kBaseSetter(EngineState s, int argc, StackPtr? argv)
        {
            Register @object = argv.Value[0];

            SciEngine.Instance._gfxCompare.KernelBaseSetter(@object);
            return s.r_acc;
        }

        private static Register kCanBeHere(EngineState s, int argc, StackPtr? argv)
        {
            Register curObject = argv.Value[0];
            Register listReference = (argc > 1) ? argv.Value[1] : Register.NULL_REG;

            Register canBeHere = SciEngine.Instance._gfxCompare.KernelCanBeHere(curObject, listReference);
            return Register.Make(0, (ushort)(canBeHere.IsNull ? 1 : 0));
        }

        private static Register kCelHigh(EngineState s, int argc, StackPtr? argv)
        {
            int viewId = argv.Value[0].ToInt16();
            if (viewId == -1)   // Happens in SCI32
                return Register.NULL_REG;
            short loopNo = argv.Value[1].ToInt16();
            short celNo = (short)((argc >= 3) ? argv.Value[2].ToInt16() : 0);
            short celHeight;

            celHeight = SciEngine.Instance._gfxCache.KernelViewGetCelHeight(viewId, loopNo, celNo);

            return Register.Make(0, (ushort)celHeight);
        }

        private static Register kCelWide(EngineState s, int argc, StackPtr? argv)
        {
            int viewId = argv.Value[0].ToInt16();
            if (viewId == -1)   // Happens in SCI32
                return Register.NULL_REG;
            short loopNo = argv.Value[1].ToInt16();
            short celNo = (short)((argc >= 3) ? argv.Value[2].ToInt16() : 0);
            short celWidth;

            celWidth = SciEngine.Instance._gfxCache.KernelViewGetCelWidth(viewId, loopNo, celNo);

            return Register.Make(0, (ushort)celWidth);
        }

        private static Register kDirLoop(EngineState s, int argc, StackPtr? argv)
        {
            kDirLoopWorker(argv.Value[0], argv.Value[1].ToUInt16(), s, argc, argv);

            return s.r_acc;
        }

        private static Register kDisplay(EngineState s, int argc, StackPtr? argv)
        {
            Register textp = argv.Value[0];
            int index = (argc > 1) ? argv.Value[1].ToUInt16() : 0;

            string text;

            if (textp.Segment != 0)
            {
                argc--; argv++;
                text = s._segMan.GetString(textp);
            }
            else {
                argc--; argc--; argv++; argv++;
                text = SciEngine.Instance.Kernel.LookupText(textp, index);
            }

            ushort languageSplitter = 0;
            string splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter);

            return SciEngine.Instance._gfxPaint16.KernelDisplay(splitText, languageSplitter, argc, argv);
        }

        private static Register kDisposeWindow(EngineState s, int argc, StackPtr? argv)
        {
            int windowId = argv.Value[0].ToInt16();
            bool reanimate = false;
            if ((argc != 2) || (argv.Value[1].IsNull))
                reanimate = true;

            SciEngine.Instance._gfxPorts.KernelDisposeWindow((ushort)windowId, reanimate);
            return s.r_acc;
        }

        private static Register kDrawCel(EngineState s, int argc, StackPtr? argv)
        {
            int viewId = argv.Value[0].ToInt16();
            short loopNo = argv.Value[1].ToInt16();
            short celNo = argv.Value[2].ToInt16();
            ushort x = argv.Value[3].ToUInt16();
            ushort y = argv.Value[4].ToUInt16();
            short priority = (short)((argc > 5) ? argv.Value[5].ToInt16() : -1);
            ushort paletteNo = (ushort)((argc > 6) ? argv.Value[6].ToUInt16() : 0);
            bool hiresMode = false;
            Register upscaledHiresHandle = Register.NULL_REG;
            ushort scaleX = 128;
            ushort scaleY = 128;

            if (argc > 7)
            {
                // this is either kq6 hires or scaling
                if (paletteNo > 0)
                {
                    // it's scaling
                    scaleX = argv.Value[6].ToUInt16();
                    scaleY = argv.Value[7].ToUInt16();
                    paletteNo = 0;
                }
                else {
                    // KQ6 hires
                    hiresMode = true;
                    upscaledHiresHandle = argv.Value[7];
                }
            }

            SciEngine.Instance._gfxPaint16.KernelDrawCel(viewId, loopNo, celNo, x, y, priority, paletteNo, scaleX, scaleY, hiresMode, upscaledHiresHandle);

            return s.r_acc;
        }

        private static Register kDrawControl(EngineState s, int argc, StackPtr? argv)
        {
            Register controlObject = argv.Value[0];
            string objName = s._segMan.GetObjectName(controlObject);

            // Most of the time, we won't return anything to the caller
            //  but |r| textcodes will trigger creation of rects in memory and will then set s.r_acc
            s.r_acc = Register.NULL_REG;

            // Disable the "Change Directory" button, as we don't allow the game engine to
            // change the directory where saved games are placed
            // "changeDirItem" is used in the import windows of QFG2&3
            if ((objName == "changeDirI") || (objName == "changeDirItem"))
            {
                int state = (int)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.state);
                SciEngine.WriteSelectorValue(s._segMan, controlObject, o => o.state, (ushort)(((ControlStateFlags)state | ControlStateFlags.DISABLED) & ~ControlStateFlags.ENABLED));
            }
            if (objName == "DEdit")
            {
                Register textReference = SciEngine.ReadSelector(s._segMan, controlObject, o => o.text);
                if (!textReference.IsNull)
                {
                    string text = s._segMan.GetString(textReference);
                    if ((text == "a:hq1_hero.sav") || (text == "a:glory1.sav") || (text == "a:glory2.sav") || (text == "a:glory3.sav"))
                    {
                        // Remove "a:" from hero quest / quest for glory export default filenames
                        text = text.Remove(0, 1);
                        text = text.Remove(0, 1);
                        s._segMan.Strcpy(textReference, text);
                    }
                }
            }
            if (objName == "savedHeros")
            {
                // Import of QfG character files dialog is shown.
                // Display additional popup information before letting user use it.
                // For the SCI32 version of this, check kernelAddPlane().
                Register changeDirButton = s._segMan.FindObjectByName("changeDirItem");
                if (!changeDirButton.IsNull)
                {
                    // check if checkDirButton is still enabled, in that case we are called the first time during that room
                    if (((ControlStateFlags)SciEngine.ReadSelectorValue(s._segMan, changeDirButton, o => o.state) & ControlStateFlags.DISABLED) == 0)
                    {
                        // TODO: showScummVMDialog("Characters saved inside ScummVM are shown " +
                        //    "automatically. Character files saved in the original " +
                        //        "interpreter need to be put inside ScummVM's saved games " +
                        //        "directory and a prefix needs to be added depending on which " +
                        //        "game it was saved in: 'qfg1-' for Quest for Glory 1, 'qfg2-' " +
                        //        "for Quest for Glory 2. Example: 'qfg2-thief.sav'.");
                    }
                }

                // For the SCI32 version of this, check kListAt().
                s._chosenQfGImportItem = SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mark);
            }

            _k_GenericDrawControl(s, controlObject, false);
            return s.r_acc;
        }

        private static Register kDrawPic(EngineState s, int argc, StackPtr? argv)
        {
            int pictureId = argv.Value[0].ToUInt16();
            ushort flags = 0;
            short animationNr = -1;
            bool animationBlackoutFlag = false;
            bool mirroredFlag = false;
            bool addToFlag = false;
            short EGApaletteNo = 0; // default needs to be 0

            if (argc >= 2)
            {
                flags = argv.Value[1].ToUInt16();
                if ((flags & K_DRAWPIC_FLAGS_ANIMATIONBLACKOUT) != 0)
                    animationBlackoutFlag = true;
                animationNr = (short)(flags & 0xFF);
                if ((flags & K_DRAWPIC_FLAGS_MIRRORED) != 0)
                    mirroredFlag = true;
            }
            if (argc >= 3)
            {
                if (!argv.Value[2].IsNull)
                    addToFlag = true;
                if (!SciEngine.Instance.Features.UsesOldGfxFunctions())
                    addToFlag = !addToFlag;
            }
            if (argc >= 4)
                EGApaletteNo = argv.Value[3].ToInt16();

            SciEngine.Instance._gfxPaint16.KernelDrawPicture(pictureId, animationNr, animationBlackoutFlag, mirroredFlag, addToFlag, EGApaletteNo);

            return s.r_acc;
        }

        private static Register kEditControl(EngineState s, int argc, StackPtr? argv)
        {
            Register controlObject = argv.Value[0];
            Register eventObject = argv.Value[1];

            if (!controlObject.IsNull)
            {
                var controlType = (ControlType)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.type);

                switch (controlType)
                {
                    case ControlType.TEXTEDIT:
                        // Only process textedit controls in here
                        SciEngine.Instance._gfxControls16.KernelTexteditChange(controlObject, eventObject);
                        break;
                    default:
                        break;
                }
            }
            return s.r_acc;
        }

        private static Register kGetPort(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._gfxPorts.KernelGetActive();
        }

        private static Register kGraph(EngineState s, int argc, StackPtr? argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        // Seems to be only implemented for SCI0/SCI01 games
        private static Register kGraphAdjustPriority(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._gfxPorts.KernelGraphAdjustPriority(argv.Value[0].ToUInt16(), argv.Value[1].ToUInt16());
            return s.r_acc;
        }

        private static Register kGraphDrawLine(EngineState s, int argc, StackPtr? argv)
        {
            short color = AdjustGraphColor(argv.Value[4].ToInt16());
            short priority = (argc > 5) ? argv.Value[5].ToInt16() : (short)-1;
            short control = (argc > 6) ? argv.Value[6].ToInt16() : (short)-1;

            SciEngine.Instance._gfxPaint16.KernelGraphDrawLine(GetGraphPoint(argv.Value), GetGraphPoint(argv.Value + 2), color, priority, control);
            return s.r_acc;
        }

        private static Register kGraphFillBoxAny(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            short colorMask = argv.Value[4].ToInt16();
            short color = AdjustGraphColor(argv.Value[5].ToInt16());
            short priority = argv.Value[6].ToInt16(); // yes, we may read from stack sometimes here
            short control = argv.Value[7].ToInt16(); // sierra did the same

            SciEngine.Instance._gfxPaint16.KernelGraphFillBox(rect, colorMask, color, priority, control);
            return s.r_acc;
        }

        private static Register kGraphFillBoxBackground(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            SciEngine.Instance._gfxPaint16.KernelGraphFillBoxBackground(rect);
            return s.r_acc;
        }

        private static Register kGraphFillBoxForeground(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            SciEngine.Instance._gfxPaint16.KernelGraphFillBoxForeground(rect);
            return s.r_acc;
        }

        private static Register kGraphGetColorCount(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, SciEngine.Instance._gfxPalette.TotalColorCount);
        }

        private static Register kGraphRedrawBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            SciEngine.Instance._gfxPaint16.KernelGraphRedrawBox(rect);
            return s.r_acc;
        }

        private static Register kGraphRestoreBox(EngineState s, int argc, StackPtr? argv)
        {
            // This may be called with a memoryhandle from SAVE_BOX or SAVE_UPSCALEDHIRES_BOX
            SciEngine.Instance._gfxPaint16.KernelGraphRestoreBox(argv.Value[0]);
            return s.r_acc;
        }

        private static Register kGraphSaveBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            ushort screenMask = (ushort)(argv.Value[4].ToUInt16() & (ushort)(Graphics.GfxScreenMasks.ALL));
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveBox(rect, screenMask);
        }

        private static Register kGraphSaveUpscaledHiresBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveUpscaledHiresBox(rect);
        }

        private static Register kGraphUpdateBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            // argv[4] is the map (1 for visual, etc.)
            // argc == 6 on upscaled hires
            bool hiresMode = (argc > 5) ? true : false;
            SciEngine.Instance._gfxPaint16.KernelGraphUpdateBox(rect, hiresMode);
            return s.r_acc;
        }

        private static Register kMoveCursor(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._gfxCursor.KernelSetPos(new Point(argv.Value[0].ToInt16(), argv.Value[1].ToInt16()));
            return s.r_acc;
        }

        private static Register kNewWindow(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect1 = new Rect(argv.Value[1].ToInt16(), argv.Value[0].ToInt16(), argv.Value[3].ToInt16(), argv.Value[2].ToInt16());
            Rect rect2 = new Rect();
            int argextra = argc >= 13 ? 4 : 0; // Triggers in PQ3 and SCI1.1 games, argc 13 for DOS argc 15 for mac
            int style = argv.Value[5 + argextra].ToInt16();
            int priority = (argc > 6 + argextra) ? argv.Value[6 + argextra].ToInt16() : -1;
            int colorPen = AdjustGraphColor((short)((argc > 7 + argextra) ? argv.Value[7 + argextra].ToInt16() : 0));
            int colorBack = AdjustGraphColor((short)((argc > 8 + argextra) ? argv.Value[8 + argextra].ToInt16() : 255));

            if (argc >= 13)
                rect2 = new Rect(argv.Value[5].ToInt16(), argv.Value[4].ToInt16(), argv.Value[7].ToInt16(), argv.Value[6].ToInt16());

            string title = string.Empty;
            if (argv.Value[4 + argextra].Segment != 0)
            {
                title = s._segMan.GetString(argv.Value[4 + argextra]);
                title = SciEngine.Instance.StrSplit(title, null);
            }

            return SciEngine.Instance._gfxPorts.KernelNewWindow(rect1, rect2, (ushort)style, (short)priority, (short)colorPen, (short)colorBack, title);
        }

        private static Register kNumCels(EngineState s, int argc, StackPtr? argv)
        {
            Register @object = argv.Value[0];
            int viewId = (int)SciEngine.ReadSelectorValue(s._segMan, @object, SciEngine.Selector(o => o.view));
            short loopNo = (short)SciEngine.ReadSelectorValue(s._segMan, @object, SciEngine.Selector(o => o.loop));
            short celCount;

            celCount = SciEngine.Instance._gfxCache.KernelViewGetCelCount(viewId, loopNo);

            // TODO: debugC(kDebugLevelGraphics, "NumCels(view.%d, %d) = %d", viewId, loopNo, celCount);

            return Register.Make(0, (ushort)celCount);
        }

        private static Register kOnControl(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect;
            GfxScreenMasks screenMask;
            int argBase = 0;

            if ((argc == 2) || (argc == 4))
            {
                screenMask = GfxScreenMasks.CONTROL;
            }
            else {
                screenMask = (GfxScreenMasks)argv.Value[0].ToUInt16();
                argBase = 1;
            }
            rect.Left = argv.Value[argBase].ToInt16();
            rect.Top = argv.Value[argBase + 1].ToInt16();
            if (argc > 3)
            {
                rect.Right = argv.Value[argBase + 2].ToInt16();
                rect.Bottom = argv.Value[argBase + 3].ToInt16();
            }
            else {
                rect.Right = rect.Left + 1;
                rect.Bottom = rect.Top + 1;
            }
            ushort result = SciEngine.Instance._gfxCompare.KernelOnControl(screenMask, rect);
            return Register.Make(0, result);
        }


        private static Register kPicNotValid(EngineState s, int argc, StackPtr? argv)
        {
            short newPicNotValid = (short)((argc > 0) ? argv.Value[0].ToInt16() : -1);

            return Register.Make(0, (ushort)SciEngine.Instance._gfxScreen.KernelPicNotValid(newPicNotValid));
        }

        private static Register kSetCursor(EngineState s, int argc, StackPtr? argv)
        {
            switch (SciEngine.Instance.Features.DetectSetCursorType())
            {
                case SciVersion.V0_EARLY:
                    return kSetCursorSci0(s, argc, argv);
                case SciVersion.V1_1:
                    return kSetCursorSci11(s, argc, argv);
                default:
                    throw new InvalidOperationException("Unknown SetCursor type");
            }
        }

        private static Register kSetCursorSci0(EngineState s, int argc, StackPtr? argv)
        {
            Point pos = new Point();
            int cursorId = argv.Value[0].ToInt16();

            // Set pointer position, if requested
            if (argc >= 4)
            {
                pos.Y = argv.Value[3].ToInt16();
                pos.X = argv.Value[2].ToInt16();
                SciEngine.Instance._gfxCursor.KernelSetPos(pos);
            }

            if ((argc >= 2) && (argv.Value[1].ToInt16() == 0))
            {
                cursorId = -1;
            }

            SciEngine.Instance._gfxCursor.KernelSetShape(cursorId);
            return s.r_acc;
        }

        private static Register kSetCursorSci11(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }

        private static Register kSetNowSeen(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._gfxCompare.KernelSetNowSeen(argv.Value[0]);
            return s.r_acc;
        }

        private static Register kSetPort(EngineState s, int argc, StackPtr? argv)
        {
            ushort portId;
            Rect picRect;
            short picTop, picLeft;
            bool initPriorityBandsFlag = false;

            switch (argc)
            {
                case 1:
                    portId = argv.Value[0].ToUInt16();
                    SciEngine.Instance._gfxPorts.KernelSetActive(portId);
                    break;

                case 6:
                case 7:
                    initPriorityBandsFlag = argc == 7;
                    picRect.Top = argv.Value[0].ToInt16();
                    picRect.Left = argv.Value[1].ToInt16();
                    picRect.Bottom = argv.Value[2].ToInt16();
                    picRect.Right = argv.Value[3].ToInt16();
                    picTop = argv.Value[4].ToInt16();
                    picLeft = argv.Value[5].ToInt16();
                    SciEngine.Instance._gfxPorts.KernelSetPicWindow(picRect, picTop, picLeft, initPriorityBandsFlag);
                    break;

                default:
                    throw new InvalidOperationException($"SetPort was called with {argc} parameters");
            }
            return Register.NULL_REG;
        }

        private static Register kTextSize(EngineState s, int argc, StackPtr? argv)
        {
            short textWidth, textHeight;
            string text = s._segMan.GetString(argv.Value[1]);
            StackPtr dest = s._segMan.DerefRegPtr(argv.Value[0], 4);
            int maxwidth = (argc > 3) ? argv.Value[3].ToUInt16() : 0;
            int font_nr = argv.Value[2].ToUInt16();

            if (dest == null)
            {
                // TODO: debugC(kDebugLevelStrings, "GetTextSize: Empty destination");
                return s.r_acc;
            }

            string sep_str;
            string sep = null;
            if ((argc > 4) && (argv.Value[4].Segment != 0))
            {
                sep_str = s._segMan.GetString(argv.Value[4]);
                sep = sep_str;
            }

            dest[0] = dest[1] = Register.NULL_REG;

            if (string.IsNullOrEmpty(text))
            { // Empty text
                dest[2] = dest[3] = Register.Make(0, 0);
                // TODO: debugC(kDebugLevelStrings, "GetTextSize: Empty string");
                return s.r_acc;
            }

            textWidth = dest[3].ToInt16(); textHeight = dest[2].ToInt16();

            ushort languageSplitter = 0;
            string splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter, sep);

#if ENABLE_SCI32
            if (SciEngine.Instance._gfxText32)
                SciEngine.Instance._gfxText32.kernelTextSize(splitText, font_nr, maxwidth, &textWidth, &textHeight);
            else
#endif
            SciEngine.Instance._gfxText16.KernelTextSize(splitText, languageSplitter, (short)font_nr, (short)maxwidth, out textWidth, out textHeight);

            // One of the game texts in LB2 German contains loads of spaces in
            // its end. We trim the text here, otherwise the graphics code will
            // attempt to draw a very large window (larger than the screen) to
            // show the text, and crash.
            // Fixes bug #3306417.
            if (textWidth >= SciEngine.Instance._gfxScreen.DisplayWidth ||
                textHeight >= SciEngine.Instance._gfxScreen.DisplayHeight)
            {
                // TODO: Is this needed for SCI32 as well?
                if (SciEngine.Instance._gfxText16 != null)
                {
                    // TODO/ warning("kTextSize: string would be too big to fit on screen. Trimming it");
                    text = text.Trim();
                    // Copy over the trimmed string...
                    s._segMan.Strcpy(argv.Value[1], text);
                    // ...and recalculate bounding box dimensions
                    SciEngine.Instance._gfxText16.KernelTextSize(splitText, languageSplitter, (short)font_nr, (short)maxwidth, out textWidth, out textHeight);
                }
            }

            // TODO: debugC(kDebugLevelStrings, "GetTextSize '%s' . %dx%d", text, textWidth, textHeight);
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                dest[2] = Register.Make(0, (ushort)textHeight);
                dest[3] = Register.Make(0, (ushort)textWidth);
            }
            else {
                dest[2] = Register.Make(0, (ushort)textWidth);
                dest[3] = Register.Make(0, (ushort)textHeight);
            }

            return s.r_acc;
        }

        private static Register kWait(EngineState s, int argc, StackPtr? argv)
        {
            int sleep_time = argv.Value[0].ToUInt16();

            s.Wait(sleep_time);

            return s.r_acc;
        }


        private static void kDirLoopWorker(Register @object, ushort angle, EngineState s, int argc, StackPtr? argv)
        {
            int viewId = (int)SciEngine.ReadSelectorValue(s._segMan, @object, SciEngine.Selector(o => o.view));
            ViewSignals signal = (ViewSignals)SciEngine.ReadSelectorValue(s._segMan, @object, SciEngine.Selector(o => o.signal));

            if (signal.HasFlag(ViewSignals.DoesntTurn))
                return;

            short useLoop = -1;
            if (ResourceManager.GetSciVersion() > SciVersion.V0_EARLY)
            {
                if ((angle > 315) || (angle < 45))
                {
                    useLoop = 3;
                }
                else if ((angle > 135) && (angle < 225))
                {
                    useLoop = 2;
                }
            }
            else {
                // SCI0EARLY
                if ((angle > 330) || (angle < 30))
                {
                    useLoop = 3;
                }
                else if ((angle > 150) && (angle < 210))
                {
                    useLoop = 2;
                }
            }
            if (useLoop == -1)
            {
                if (angle >= 180)
                {
                    useLoop = 1;
                }
                else {
                    useLoop = 0;
                }
            }
            else {
                short loopCount = SciEngine.Instance._gfxCache.KernelViewGetLoopCount(viewId);
                if (loopCount < 4)
                    return;
            }

            SciEngine.WriteSelectorValue(s._segMan, @object, SciEngine.Selector(o => o.loop), (ushort)useLoop);
        }

        private static short AdjustGraphColor(short color)
        {
            // WORKAROUND: EGA and Amiga games can set invalid colors (above 0 - 15).
            // It seems only the lower nibble was used in these games.
            // bug #3048908, #3486899.
            // Confirmed in EGA games KQ4(late), QFG1(ega), LB1 that
            // at least FillBox (only one of the functions using adjustGraphColor)
            // behaves like this.
            if (SciEngine.Instance.ResMan.ViewType == ViewType.Ega)
                return (short)(color & 0x0F);    // 0 - 15
            else
                return color;
        }

        private static Rect GetGraphRect(StackPtr argv)
        {
            short x = argv[1].ToInt16();
            short y = argv[0].ToInt16();
            short x1 = argv[3].ToInt16();
            short y1 = argv[2].ToInt16();
            if (x > x1) ScummHelper.Swap(ref x, ref x1);
            if (y > y1) ScummHelper.Swap(ref y, ref y1);
            return new Rect(x, y, x1, y1);
        }

        private static Point GetGraphPoint(StackPtr argv)
        {
            short x = argv[1].ToInt16();
            short y = argv[0].ToInt16();
            return new Point(x, y);
        }

        private static void _k_GenericDrawControl(EngineState s, Register controlObject, bool hilite)
        {
            ControlType type = (ControlType)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.type);
            short style = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.state);
            short x = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsLeft);
            short y = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsTop);
            int fontId = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.font);
            Register textReference = SciEngine.ReadSelector(s._segMan, controlObject, o => o.text);
            string text = string.Empty;
            Rect rect;
            short alignment;
            short mode, maxChars, cursorPos, upperPos, listCount, i;
            ushort upperOffset, cursorOffset;
            int viewId;
            short loopNo;
            short celNo;
            short priority;
            Register listSeeker;
            bool isAlias = false;

            rect = kControlCreateRect(x, y,
                        (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsRight),
                        (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsBottom));

            if (!textReference.IsNull)
                text = s._segMan.GetString(textReference);

            ushort languageSplitter = 0;
            string splitText = string.Empty;

            switch (type)
            {
                case ControlType.BUTTON:
                case ControlType.TEXTEDIT:
                    splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter, null);
                    break;
                case ControlType.TEXT:
                    splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter);
                    break;
            }

            switch (type)
            {
                case ControlType.BUTTON:
                    // TODO: debugC(kDebugLevelGraphics, "drawing button %04x:%04x to %d,%d", PRINT_REG(controlObject), x, y);
                    SciEngine.Instance._gfxControls16.KernelDrawButton(rect, controlObject, splitText, languageSplitter, fontId, (ControlStateFlags)style, hilite);
                    return;

                case ControlType.TEXT:
                    alignment = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mode);
                    // TODO: debugC(kDebugLevelGraphics, "drawing text %04x:%04x ('%s') to %d,%d, mode=%d", PRINT_REG(controlObject), text, x, y, alignment);
                    SciEngine.Instance._gfxControls16.KernelDrawText(rect, controlObject, splitText, languageSplitter, fontId, alignment, (ControlStateFlags)style, hilite);
                    s.r_acc = SciEngine.Instance._gfxText16.AllocAndFillReferenceRectArray();
                    return;

                case ControlType.TEXTEDIT:
                    mode = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mode);
                    maxChars = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.max);
                    cursorPos = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cursor);
                    if (cursorPos > text.Length)
                    {
                        // if cursor is outside of text, adjust accordingly
                        cursorPos = (short)text.Length;
                        SciEngine.WriteSelectorValue(s._segMan, controlObject, o => o.cursor, (ushort)cursorPos);
                    }
                    // TODO: debugC(kDebugLevelGraphics, "drawing edit control %04x:%04x (text %04x:%04x, '%s') to %d,%d", PRINT_REG(controlObject), PRINT_REG(textReference), text, x, y);
                    SciEngine.Instance._gfxControls16.KernelDrawTextEdit(rect, controlObject, splitText, languageSplitter, fontId, mode, (ControlStateFlags)style, cursorPos, maxChars, hilite);
                    return;

                case ControlType.ICON:
                    viewId = (int)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.view);
                    {
                        int l = (int)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.loop);
                        loopNo = (short)(((l & 0x80) != 0) ? l - 256 : l);
                        int c = (int)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cel);
                        celNo = (short)(((c & 0x80) != 0) ? c - 256 : c);
                        // Check if the control object specifies a priority selector (like in Jones)
                        Register tmp;
                        if (SciEngine.LookupSelector(s._segMan, controlObject, o => o.priority, null, out tmp) == SelectorType.Variable)
                            priority = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.priority);
                        else
                            priority = -1;
                    }
                    // TODO: debugC(kDebugLevelGraphics, "drawing icon control %04x:%04x to %d,%d", PRINT_REG(controlObject), x, y - 1);
                    SciEngine.Instance._gfxControls16.KernelDrawIcon(rect, controlObject, viewId, loopNo, celNo, priority, style, hilite);
                    return;

                case ControlType.LIST:
                case ControlType.LIST_ALIAS:
                    if (type == ControlType.LIST_ALIAS)
                        isAlias = true;

                    maxChars = (short)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.x); // max chars per entry
                    cursorOffset = (ushort)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cursor);
                    if (SciEngine.Selector(o => o.topString) != -1)
                    {
                        // Games from early SCI1 onwards use topString
                        upperOffset = (ushort)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.topString);
                    }
                    else {
                        // Earlier games use lsTop or brTop
                        Register tmp;
                        if (SciEngine.LookupSelector(s._segMan, controlObject, o => o.brTop, null, out tmp) == SelectorType.Variable)
                            upperOffset = (ushort)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.brTop);
                        else
                            upperOffset = (ushort)SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.lsTop);
                    }

                    // Count string entries in NULL terminated string list
                    listCount = 0; listSeeker = textReference;
                    while (s._segMan.Strlen(listSeeker) > 0)
                    {
                        listCount++;
                        listSeeker.IncOffset(maxChars);
                    }

                    // TODO: This is rather convoluted... It would be a lot cleaner
                    // if sciw_new_list_control would take a list of Common::String
                    cursorPos = 0; upperPos = 0;
                    string[] listEntries = null;
                    if (listCount != 0)
                    {
                        // We create a pointer-list to the different strings, we also find out whats upper and cursor position
                        listSeeker = textReference;
                        listEntries = new string[listCount];
                        var listStrings = new string[listCount];
                        for (i = 0; i < listCount; i++)
                        {
                            listStrings[i] = s._segMan.GetString(listSeeker);
                            listEntries[i] = listStrings[i];
                            if (listSeeker.Offset == upperOffset)
                                upperPos = i;
                            if (listSeeker.Offset == cursorOffset)
                                cursorPos = i;
                            listSeeker.IncOffset(maxChars);
                        }
                    }

                    // TODO: debugC(kDebugLevelGraphics, "drawing list control %04x:%04x to %d,%d, diff %d", PRINT_REG(controlObject), x, y, SCI_MAX_SAVENAME_LENGTH);
                    SciEngine.Instance._gfxControls16.KernelDrawList(rect, controlObject, maxChars, listCount, listEntries, fontId, (ControlStateFlags)style, upperPos, cursorPos, isAlias, hilite);
                    return;

                case ControlType.DUMMY:
                    // Actually this here does nothing at all, its required by at least QfG1/EGA that we accept this type
                    return;

                default:
                    throw new InvalidOperationException($"unsupported control type {type}");
            }
        }

        // Original top-left must stay on kControl rects, we adjust accordingly because
        // sierra sci actually wont draw rects that are upside down (example: jones,
        // when challenging jones - one button is a duplicate and also has lower-right
        // which is 0, 0)
        private static Rect kControlCreateRect(short x, short y, short x1, short y1)
        {
            if (x > x1) x1 = x;
            if (y > y1) y1 = y;
            return new Rect(x, y, x1, y1);
        }
    }
}
