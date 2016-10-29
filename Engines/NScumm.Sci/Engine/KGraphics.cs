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
using static NScumm.Core.DebugHelper;
using NScumm.Core.IO;

namespace NScumm.Sci.Engine
{
    internal partial class Kernel
    {
        private const int K_DRAWPIC_FLAGS_MIRRORED = 1 << 14;
        private const int K_DRAWPIC_FLAGS_ANIMATIONBLACKOUT = 1 << 15;

        private static Register kAddToPic(EngineState s, int argc, StackPtr argv)
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
                    if (argv[0].IsNull)
                        return s.r_acc;
                    SciEngine.Instance._gfxAnimate.KernelAddToPicList(argv[0], argc, argv);
                    break;
                case 7:
                    viewId = argv[0].ToUInt16();
                    loopNo = argv[1].ToInt16();
                    celNo = argv[2].ToInt16();
                    leftPos = argv[3].ToInt16();
                    topPos = argv[4].ToInt16();
                    priority = argv[5].ToInt16();
                    control = argv[6].ToInt16();
                    SciEngine.Instance._gfxAnimate.KernelAddToPicView(viewId, loopNo, celNo, leftPos, topPos, priority,
                        control);
                    break;
                default:
                    throw new InvalidOperationException($"kAddToPic with unsupported parameter count {argc}");
            }
            return s.r_acc;
        }

        private static Register kAnimate(EngineState s, int argc, StackPtr argv)
        {
            Register castListReference = argc > 0 ? argv[0] : Register.NULL_REG;
            bool cycle = (argc > 1) && argv[1].ToUInt16() != 0;

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

        private static Register kAssertPalette(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();

            SciEngine.Instance._gfxPalette16.KernelAssertPalette(paletteId);
            return s.r_acc;
        }

        private static Register kBaseSetter(EngineState s, int argc, StackPtr argv)
        {
            Register @object = argv[0];

            SciEngine.Instance._gfxCompare.KernelBaseSetter(@object);
            return s.r_acc;
        }

        private static Register kCanBeHere(EngineState s, int argc, StackPtr argv)
        {
            Register curObject = argv[0];
            Register listReference = argc > 1 ? argv[1] : Register.NULL_REG;

            Register canBeHere = SciEngine.Instance._gfxCompare.KernelCanBeHere(curObject, listReference);
            return Register.Make(0, (ushort) (canBeHere.IsNull ? 1 : 0));
        }

        private static Register kCantBeHere(EngineState s, int argc, StackPtr argv)
        {
            Register curObject = argv[0];
            Register listReference = argc > 1 ? argv[1] : Register.NULL_REG;

            Register canBeHere = SciEngine.Instance._gfxCompare.KernelCanBeHere(curObject, listReference);
            return canBeHere;
        }

        private static Register kCelHigh(EngineState s, int argc, StackPtr argv)
        {
            int viewId = argv[0].ToInt16();
            if (viewId == -1) // Happens in SCI32
                return Register.NULL_REG;
            short loopNo = argv[1].ToInt16();
            var celNo = (short) (argc >= 3 ? argv[2].ToInt16() : 0);
            short celHeight;

            celHeight = SciEngine.Instance._gfxCache.KernelViewGetCelHeight(viewId, loopNo, celNo);

            return Register.Make(0, (ushort) celHeight);
        }

        private static Register kCelWide(EngineState s, int argc, StackPtr argv)
        {
            int viewId = argv[0].ToInt16();
            if (viewId == -1) // Happens in SCI32
                return Register.NULL_REG;
            short loopNo = argv[1].ToInt16();
            var celNo = (short) (argc >= 3 ? argv[2].ToInt16() : 0);
            short celWidth;

            celWidth = SciEngine.Instance._gfxCache.KernelViewGetCelWidth(viewId, loopNo, celNo);

            return Register.Make(0, (ushort) celWidth);
        }

#if ENABLE_SCI32
        private static Register kCelHigh32(EngineState s, int argc, StackPtr argv)
        {
            int resourceId = argv[0].ToUInt16();
            short loopNo = argv[1].ToInt16();
            short celNo = argv[2].ToInt16();
            var celObj = CelObjView.Create(resourceId, loopNo, celNo);
            var ratio = new Rational(SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight,
                celObj._yResolution);
            return Register.Make(0, (ushort) Helpers.Mulru(celObj._height, ref ratio));
        }

        private static Register kCelWide32(EngineState s, int argc, StackPtr argv)
        {
            int resourceId = argv[0].ToUInt16();
            short loopNo = argv[1].ToInt16();
            short celNo = argv[2].ToInt16();
            var celObj = CelObjView.Create(resourceId, loopNo, celNo);
            var ratio = new Rational(
                SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth, (int) celObj._xResolution);
            return Register.Make(0, (ushort) Helpers.Mulru(celObj._width, ref ratio));
        }

        private static Register kRemapColors32(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }
#endif

        private static Register kCoordPri(EngineState s, int argc, StackPtr argv)
        {
            short y = argv[0].ToInt16();

            if ((argc < 2) || (y != 1))
            {
                return Register.Make(0, (ushort) SciEngine.Instance._gfxPorts.KernelCoordinateToPriority(y));
            }
            short priority = argv[1].ToInt16();
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPorts.KernelPriorityToCoordinate((byte) priority));
        }

        private static Register kDirLoop(EngineState s, int argc, StackPtr argv)
        {
            kDirLoopWorker(argv[0], argv[1].ToUInt16(), s, argc, argv);

            return s.r_acc;
        }

        private static Register kDisplay(EngineState s, int argc, StackPtr argv)
        {
            Register textp = argv[0];
            int index = argc > 1 ? argv[1].ToUInt16() : 0;

            string text;

            if (textp.Segment != 0)
            {
                argc--;
                argv++;
                text = s._segMan.GetString(textp);
            }
            else
            {
                argc--;
                argc--;
                argv++;
                argv++;
                text = SciEngine.Instance.Kernel.LookupText(textp, index);
            }

            ushort languageSplitter = 0;
            string splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter);

            return SciEngine.Instance._gfxPaint16.KernelDisplay(splitText, languageSplitter, argc, argv);
        }

        private static Register kDisposeWindow(EngineState s, int argc, StackPtr argv)
        {
            int windowId = argv[0].ToInt16();
            bool reanimate = false;
            if ((argc != 2) || argv[1].IsNull)
                reanimate = true;

            SciEngine.Instance._gfxPorts.KernelDisposeWindow((ushort) windowId, reanimate);
            return s.r_acc;
        }

        private static Register kDrawCel(EngineState s, int argc, StackPtr argv)
        {
            int viewId = argv[0].ToInt16();
            short loopNo = argv[1].ToInt16();
            short celNo = argv[2].ToInt16();
            ushort x = argv[3].ToUInt16();
            ushort y = argv[4].ToUInt16();
            var priority = (short) (argc > 5 ? argv[5].ToInt16() : -1);
            var paletteNo = (ushort) (argc > 6 ? argv[6].ToUInt16() : 0);
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
                    scaleX = argv[6].ToUInt16();
                    scaleY = argv[7].ToUInt16();
                    paletteNo = 0;
                }
                else
                {
                    // KQ6 hires
                    hiresMode = true;
                    upscaledHiresHandle = argv[7];
                }
            }

            SciEngine.Instance._gfxPaint16.KernelDrawCel(viewId, loopNo, celNo, x, y, priority, paletteNo, scaleX,
                scaleY, hiresMode, upscaledHiresHandle);

            return s.r_acc;
        }

        private static Register kDrawControl(EngineState s, int argc, StackPtr argv)
        {
            Register controlObject = argv[0];
            string objName = s._segMan.GetObjectName(controlObject);

            // Most of the time, we won't return anything to the caller
            //  but |r| textcodes will trigger creation of rects in memory and will then set s.r_acc
            s.r_acc = Register.NULL_REG;

            // Disable the "Change Directory" button, as we don't allow the game engine to
            // change the directory where saved games are placed
            // "changeDirItem" is used in the import windows of QFG2&3
            if ((objName == "changeDirI") || (objName == "changeDirItem"))
            {
                var state = (int) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.state);
                SciEngine.WriteSelectorValue(s._segMan, controlObject, o => o.state,
                    (ushort) (((ControlStateFlags) state | ControlStateFlags.DISABLED) & ~ControlStateFlags.ENABLED));
            }
            if (objName == "DEdit")
            {
                Register textReference = SciEngine.ReadSelector(s._segMan, controlObject, o => o.text);
                if (!textReference.IsNull)
                {
                    string text = s._segMan.GetString(textReference);
                    if ((text == "a:hq1_hero.sav") || (text == "a:glory1.sav") || (text == "a:glory2.sav") ||
                        (text == "a:glory3.sav"))
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
                    if (((ControlStateFlags) SciEngine.ReadSelectorValue(s._segMan, changeDirButton, o => o.state) &
                         ControlStateFlags.DISABLED) == 0)
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
                s._chosenQfGImportItem = (int) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mark);
            }

            _k_GenericDrawControl(s, controlObject, false);
            return s.r_acc;
        }

        private static Register kDrawPic(EngineState s, int argc, StackPtr argv)
        {
            int pictureId = argv[0].ToUInt16();
            ushort flags = 0;
            short animationNr = -1;
            bool animationBlackoutFlag = false;
            bool mirroredFlag = false;
            bool addToFlag = false;
            short EGApaletteNo = 0; // default needs to be 0

            if (argc >= 2)
            {
                flags = argv[1].ToUInt16();
                if ((flags & K_DRAWPIC_FLAGS_ANIMATIONBLACKOUT) != 0)
                    animationBlackoutFlag = true;
                animationNr = (short) (flags & 0xFF);
                if ((flags & K_DRAWPIC_FLAGS_MIRRORED) != 0)
                    mirroredFlag = true;
            }
            if (argc >= 3)
            {
                if (!argv[2].IsNull)
                    addToFlag = true;
                if (!SciEngine.Instance.Features.UsesOldGfxFunctions())
                    addToFlag = !addToFlag;
            }
            if (argc >= 4)
                EGApaletteNo = argv[3].ToInt16();

            SciEngine.Instance._gfxPaint16.KernelDrawPicture(pictureId, animationNr, animationBlackoutFlag, mirroredFlag,
                addToFlag, EGApaletteNo);
            return s.r_acc;
        }

        private static Register kEditControl(EngineState s, int argc, StackPtr argv)
        {
            Register controlObject = argv[0];
            Register eventObject = argv[1];

            if (!controlObject.IsNull)
            {
                var controlType = (ControlType) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.type);

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

        private static Register kGetPort(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._gfxPorts.KernelGetActive();
        }

        private static Register kGraph(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        // Seems to be only implemented for SCI0/SCI01 games
        private static Register kGraphAdjustPriority(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxPorts.KernelGraphAdjustPriority(argv[0].ToUInt16(), argv[1].ToUInt16());
            return s.r_acc;
        }

        private static Register kGraphDrawLine(EngineState s, int argc, StackPtr argv)
        {
            short color = AdjustGraphColor(argv[4].ToInt16());
            short priority = argc > 5 ? argv[5].ToInt16() : (short) -1;
            short control = argc > 6 ? argv[6].ToInt16() : (short) -1;

            SciEngine.Instance._gfxPaint16.KernelGraphDrawLine(GetGraphPoint(argv), GetGraphPoint(argv + 2), color,
                priority, control);
            return s.r_acc;
        }

        private static Register kGraphFillBoxAny(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            short colorMask = argv[4].ToInt16();
            short color = AdjustGraphColor(argv[5].ToInt16());
            short priority = argv[6].ToInt16(); // yes, we may read from stack sometimes here
            short control = argv[7].ToInt16(); // sierra did the same

            SciEngine.Instance._gfxPaint16.KernelGraphFillBox(rect, colorMask, color, priority, control);
            return s.r_acc;
        }

        private static Register kGraphFillBoxBackground(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            SciEngine.Instance._gfxPaint16.KernelGraphFillBoxBackground(rect);
            return s.r_acc;
        }

        private static Register kGraphFillBoxForeground(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            SciEngine.Instance._gfxPaint16.KernelGraphFillBoxForeground(rect);
            return s.r_acc;
        }

        private static Register kGraphGetColorCount(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, SciEngine.Instance._gfxPalette16.TotalColorCount);
        }

        private static Register kGraphRedrawBox(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            SciEngine.Instance._gfxPaint16.KernelGraphRedrawBox(rect);
            return s.r_acc;
        }

        private static Register kGraphRestoreBox(EngineState s, int argc, StackPtr argv)
        {
            // This may be called with a memoryhandle from SAVE_BOX or SAVE_UPSCALEDHIRES_BOX
            SciEngine.Instance._gfxPaint16.KernelGraphRestoreBox(argv[0]);
            return s.r_acc;
        }

        private static Register kGraphSaveBox(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            var screenMask = (ushort) (argv[4].ToUInt16() & (ushort) GfxScreenMasks.ALL);
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveBox(rect, screenMask);
        }

        private static Register kGraphSaveUpscaledHiresBox(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveUpscaledHiresBox(rect);
        }

        private static Register kGraphUpdateBox(EngineState s, int argc, StackPtr argv)
        {
            Rect rect = GetGraphRect(argv);
            // argv[4] is the map (1 for visual, etc.)
            // argc == 6 on upscaled hires
            bool hiresMode = argc > 5;
            SciEngine.Instance._gfxPaint16.KernelGraphUpdateBox(rect, hiresMode);
            return s.r_acc;
        }

        private static Register kHiliteControl(EngineState s, int argc, StackPtr argv)
        {
            Register controlObject = argv[0];
            _k_GenericDrawControl(s, controlObject, true);
            return s.r_acc;
        }

        private static Register kIsItSkip(EngineState s, int argc, StackPtr argv)
        {
            int viewId = argv[0].ToInt16();
            short loopNo = argv[1].ToInt16();
            short celNo = argv[2].ToInt16();
            var position = new Point(argv[4].ToInt16(), argv[3].ToInt16());

            bool result = SciEngine.Instance._gfxCompare.KernelIsItSkip(viewId, loopNo, celNo, position);
            return Register.Make(0, result);
        }

        private static Register kMoveCursor(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxCursor.KernelSetPos(new Point(argv[0].ToInt16(), argv[1].ToInt16()));
            return s.r_acc;
        }

        private static Register kNewWindow(EngineState s, int argc, StackPtr argv)
        {
            var rect1 = new Rect(argv[1].ToInt16(), argv[0].ToInt16(), argv[3].ToInt16(), argv[2].ToInt16());
            var rect2 = new Rect();
            int argextra = argc >= 13 ? 4 : 0; // Triggers in PQ3 and SCI1.1 games, argc 13 for DOS argc 15 for mac
            int style = argv[5 + argextra].ToInt16();
            int priority = argc > 6 + argextra ? argv[6 + argextra].ToInt16() : -1;
            int colorPen = AdjustGraphColor((short) (argc > 7 + argextra ? argv[7 + argextra].ToInt16() : 0));
            int colorBack = AdjustGraphColor((short) (argc > 8 + argextra ? argv[8 + argextra].ToInt16() : 255));

            if (argc >= 13)
                rect2 = new Rect(argv[5].ToInt16(), argv[4].ToInt16(), argv[7].ToInt16(), argv[6].ToInt16());

            string title = string.Empty;
            if (argv[4 + argextra].Segment != 0)
            {
                title = s._segMan.GetString(argv[4 + argextra]);
                title = SciEngine.Instance.StrSplit(title, null);
            }

            return SciEngine.Instance._gfxPorts.KernelNewWindow(rect1, rect2, (ushort) style, (short) priority,
                (short) colorPen, (short) colorBack, title);
        }

        private static Register kNumCels(EngineState s, int argc, StackPtr argv)
        {
            Register @object = argv[0];
            var viewId = (int) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.view);
            var loopNo = (short) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.loop);
            short celCount;

            celCount = SciEngine.Instance._gfxCache.KernelViewGetCelCount(viewId, loopNo);

            DebugC(DebugLevels.Graphics, "NumCels(view.{0}, {1}) = {2}", viewId, loopNo, celCount);

            return Register.Make(0, (ushort) celCount);
        }

        private static Register kNumLoops(EngineState s, int argc, StackPtr argv)
        {
            Register @object = argv[0];
            var viewId = (int) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.view);
            short loopCount;

            loopCount = SciEngine.Instance._gfxCache.KernelViewGetLoopCount(viewId);

            DebugC(DebugLevels.Graphics, "NumLoops(view.{0}) = {1}", viewId, loopCount);

            return Register.Make(0, (ushort) loopCount);
        }

        private static Register kOnControl(EngineState s, int argc, StackPtr argv)
        {
            Rect rect;
            GfxScreenMasks screenMask;
            int argBase = 0;

            if ((argc == 2) || (argc == 4))
            {
                screenMask = GfxScreenMasks.CONTROL;
            }
            else
            {
                screenMask = (GfxScreenMasks) argv[0].ToUInt16();
                argBase = 1;
            }
            rect.Left = argv[argBase].ToInt16();
            rect.Top = argv[argBase + 1].ToInt16();
            if (argc > 3)
            {
                rect.Right = argv[argBase + 2].ToInt16();
                rect.Bottom = argv[argBase + 3].ToInt16();
            }
            else
            {
                rect.Right = (short) (rect.Left + 1);
                rect.Bottom = (short) (rect.Top + 1);
            }
            ushort result = SciEngine.Instance._gfxCompare.KernelOnControl(screenMask, rect);
            return Register.Make(0, result);
        }

        private static Register kPalette(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kPaletteSetFromResource(EngineState s, int argc, StackPtr argv)
        {
            int resourceId = argv[0].ToUInt16();
            bool force = false;
            if (argc == 2)
                force = argv[1].ToUInt16() == 2;

            // Non-VGA games don't use palette resources.
            // This has been changed to 64 colors because Longbow Amiga does have
            // one palette (palette 999).
            if (SciEngine.Instance._gfxPalette16.TotalColorCount < 64)
                return s.r_acc;

            SciEngine.Instance._gfxPalette16.KernelSetFromResource(resourceId, force);
            return s.r_acc;
        }

        private static Register kPaletteSetFlag(EngineState s, int argc, StackPtr argv)
        {
            var fromColor = (ushort) ScummHelper.Clip(argv[0].ToUInt16(), 1, 255);
            var toColor = (ushort) ScummHelper.Clip(argv[1].ToUInt16(), 1, 255);
            ushort flags = argv[2].ToUInt16();
            SciEngine.Instance._gfxPalette16.KernelSetFlag(fromColor, toColor, flags);
            return s.r_acc;
        }

        private static Register kPaletteUnsetFlag(EngineState s, int argc, StackPtr argv)
        {
            var fromColor = (ushort) ScummHelper.Clip(argv[0].ToUInt16(), 1, 255);
            var toColor = (ushort) ScummHelper.Clip(argv[1].ToUInt16(), 1, 255);
            ushort flags = argv[2].ToUInt16();
            SciEngine.Instance._gfxPalette16.KernelUnsetFlag(fromColor, toColor, flags);
            return s.r_acc;
        }

        private static Register kPaletteSetIntensity(EngineState s, int argc, StackPtr argv)
        {
            var fromColor = (ushort) ScummHelper.Clip(argv[0].ToUInt16(), 1, 255);
            var toColor = (ushort) ScummHelper.Clip(argv[1].ToUInt16(), 1, 255);
            ushort intensity = argv[2].ToUInt16();
            bool setPalette = (argc < 4) || argv[3].IsNull;

            // Palette intensity in non-VGA SCI1 games has been removed
            if (SciEngine.Instance._gfxPalette16.TotalColorCount < 256)
                return s.r_acc;

            SciEngine.Instance._gfxPalette16.KernelSetIntensity(fromColor, toColor, intensity, setPalette);
            return s.r_acc;
        }

        private static Register kPaletteFindColor(EngineState s, int argc, StackPtr argv)
        {
            ushort r = argv[0].ToUInt16();
            ushort g = argv[1].ToUInt16();
            ushort b = argv[2].ToUInt16();
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette16.KernelFindColor(r, g, b));
        }

        private static Register kPaletteAnimate(EngineState s, int argc, StackPtr argv)
        {
            short argNr;
            bool paletteChanged = false;

            // Palette animation in non-VGA SCI1 games has been removed
            if (SciEngine.Instance._gfxPalette16.TotalColorCount < 256)
                return s.r_acc;

            for (argNr = 0; argNr < argc; argNr += 3)
            {
                ushort fromColor = argv[argNr].ToUInt16();
                ushort toColor = argv[argNr + 1].ToUInt16();
                short speed = argv[argNr + 2].ToInt16();
                if (SciEngine.Instance._gfxPalette16.KernelAnimate((byte) fromColor, (byte) toColor, speed))
                    paletteChanged = true;
            }
            if (paletteChanged)
                SciEngine.Instance._gfxPalette16.KernelAnimateSet();

            // WORKAROUND: The game scripts in SQ4 floppy count the number of elapsed
            // cycles in the intro from the number of successive kAnimate calls during
            // the palette cycling effect, while showing the SQ4 logo. This worked in
            // older computers because each animate call took awhile to complete.
            // Normally, such scripts are handled automatically by our speed throttler,
            // however in this case there are no calls to kGameIsRestarting (where the
            // speed throttler gets called) between the different palette animation calls.
            // Thus, we add a small delay between each animate call to make the whole
            // palette animation effect slower and visible, and not have the logo screen
            // get skipped because the scripts don't wait between animation steps. Fixes
            // bug #3537232.
            if (SciEngine.Instance.GameId == SciGameId.SQ4 && !SciEngine.Instance.IsCd && s.CurrentRoomNumber == 1)
                SciEngine.Instance.Sleep(10);

            return s.r_acc;
        }

        private static Register kPaletteSave(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._gfxPalette16.KernelSave();
        }

        private static Register kPaletteRestore(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxPalette16.KernelRestore(argv[0]);
            return argv[0];
        }

        private static Register kPalVary(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kPalVaryInit(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            ushort ticks = argv[1].ToUInt16();
            var stepStop = (ushort) (argc >= 3 ? argv[2].ToUInt16() : 64);
            var direction = (ushort) (argc >= 4 ? argv[3].ToUInt16() : 1);
            if (SciEngine.Instance._gfxPalette16.KernelPalVaryInit(paletteId, ticks, stepStop, direction))
                return Register.SIGNAL_REG;
            return Register.NULL_REG;
        }

        private static Register kPalVaryReverse(EngineState s, int argc, StackPtr argv)
        {
            var ticks = (short) (argc >= 1 ? argv[0].ToUInt16() : -1);
            var stepStop = (short) (argc >= 2 ? argv[1].ToUInt16() : 0);
            var direction = (short) (argc >= 3 ? argv[2].ToInt16() : -1);

            return Register.Make(0,
                (ushort) SciEngine.Instance._gfxPalette16.KernelPalVaryReverse(ticks, stepStop, direction));
        }

        private static Register kPalVaryGetCurrentStep(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette16.KernelPalVaryGetCurrentStep());
        }

        private static Register kPalVaryDeinit(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxPalette16.KernelPalVaryDeinit();
            return Register.NULL_REG;
        }

        private static Register kPalVaryChangeTarget(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            short currentStep = SciEngine.Instance._gfxPalette16.KernelPalVaryChangeTarget(paletteId);
            return Register.Make(0, (ushort) currentStep);
        }

        private static Register kPalVaryChangeTicks(EngineState s, int argc, StackPtr argv)
        {
            ushort ticks = argv[0].ToUInt16();
            SciEngine.Instance._gfxPalette16.KernelPalVaryChangeTicks(ticks);
            return Register.NULL_REG;
        }

        private static Register kPalVaryPauseResume(EngineState s, int argc, StackPtr argv)
        {
            bool pauseState = !argv[0].IsNull;
            SciEngine.Instance._gfxPalette16.KernelPalVaryPause(pauseState);
            return Register.NULL_REG;
        }

        private static Register kPicNotValid(EngineState s, int argc, StackPtr argv)
        {
            var newPicNotValid = (short) (argc > 0 ? argv[0].ToInt16() : -1);

            return Register.Make(0, (ushort) SciEngine.Instance._gfxScreen.KernelPicNotValid(newPicNotValid));
        }

        private static Register kPriCoord(EngineState s, int argc, StackPtr argv)
        {
            short priority = argv[0].ToInt16();

            return Register.Make(0, (ushort) SciEngine.Instance._gfxPorts.KernelPriorityToCoordinate((byte) priority));
        }

        // Early variant of the SCI32 kRemapColors kernel function, used in the demo of QFG4
        private static Register kRemapColors(EngineState s, int argc, StackPtr argv)
        {
            ushort operation = argv[0].ToUInt16();

            switch (operation)
            {
                case 0:
                {
                    // remap by percent
                    ushort percent = argv[1].ToUInt16();
                    SciEngine.Instance._gfxRemap16.ResetRemapping();
                    SciEngine.Instance._gfxRemap16.SetRemappingPercent(254, (byte) percent);
                }
                    break;
                case 1:
                {
                    // remap by range
                    ushort from = argv[1].ToUInt16();
                    ushort to = argv[2].ToUInt16();
                    ushort @base = argv[3].ToUInt16();
                    SciEngine.Instance._gfxRemap16.ResetRemapping();
                    SciEngine.Instance._gfxRemap16.SetRemappingRange(254, (byte) from, (byte) to, (byte) @base);
                }
                    break;
                case 2: // turn remapping off (unused)
                    throw new InvalidOperationException("Unused subop kRemapColors(2) has been called");
            }

            return s.r_acc;
        }

        private static Register kRemapColorsOff(EngineState s, int argc, StackPtr argv)
        {
            if (argc == 0)
            {
                SciEngine.Instance._gfxRemap32.RemapAllOff();
            }
            else
            {
                byte color = (byte) argv[0].ToUInt16();
                SciEngine.Instance._gfxRemap32.RemapOff(color);
            }
            return s.r_acc;
        }

        private static Register kSetCursor(EngineState s, int argc, StackPtr argv)
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

        private static Register kSetCursorSci0(EngineState s, int argc, StackPtr argv)
        {
            var pos = new Point();
            int cursorId = argv[0].ToInt16();

            // Set pointer position, if requested
            if (argc >= 4)
            {
                pos.Y = argv[3].ToInt16();
                pos.X = argv[2].ToInt16();
                SciEngine.Instance._gfxCursor.KernelSetPos(pos);
            }

            if ((argc >= 2) && (argv[1].ToInt16() == 0))
            {
                cursorId = -1;
            }

            SciEngine.Instance._gfxCursor.KernelSetShape(cursorId);
            return s.r_acc;
        }

        private static Register kSetCursorSci11(EngineState s, int argc, StackPtr argv)
        {
            Point pos;
            Point hotspot = new Point();

            switch (argc)
            {
                case 1:
                    switch (argv[0].ToInt16())
                    {
                        case 0:
                            SciEngine.Instance._gfxCursor.KernelHide();
                            break;
                        case -1:
                            SciEngine.Instance._gfxCursor.KernelClearZoomZone();
                            break;
                        case -2:
                            SciEngine.Instance._gfxCursor.KernelResetMoveZone();
                            break;
                        default:
                            SciEngine.Instance._gfxCursor.KernelShow();
                            break;
                    }
                    break;
                case 2:
                    pos.Y = argv[1].ToInt16();
                    pos.X = argv[0].ToInt16();

                    SciEngine.Instance._gfxCursor.KernelSetPos(pos);
                    break;
                case 4:
                {
                    short top, left, bottom, right;

                    if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                    {
                        top = argv[1].ToInt16();
                        left = argv[0].ToInt16();
                        bottom = argv[3].ToInt16();
                        right = argv[2].ToInt16();
                    }
                    else
                    {
                        top = argv[0].ToInt16();
                        left = argv[1].ToInt16();
                        bottom = argv[2].ToInt16();
                        right = argv[3].ToInt16();
                    }
                    // bottom/right needs to be included into our movezone, because we compare it like any regular Common::Rect
                    bottom++;
                    right++;

                    if ((right >= left) && (bottom >= top))
                    {
                        Rect rect = new Rect(left, top, right, bottom);
                        SciEngine.Instance._gfxCursor.KernelSetMoveZone(rect);
                    }
                    else
                    {
                        Warning("kSetCursor: Ignoring invalid mouse zone (%i, %i)-(%i, %i)", left, top, right, bottom);
                    }
                    break;
                }
                case 9: // case for kq5cd, we are getting calling with 4 additional 900d parameters
                case 5:
                // Fallthrough
                case 3:
                    if (argc == 5 || argc == 9)
                    {
                        hotspot = new Point(argv[3].ToInt16(), argv[4].ToInt16());
                    }
                    if (SciEngine.Instance.Platform == Platform.Macintosh &&
                        SciEngine.Instance.GameId != SciGameId.TORIN)
                    {
                        // Torin Mac seems to be the only game that uses view cursors
                        // Mac cursors have their own hotspot, so ignore any we get here
                        SciEngine.Instance._gfxCursor.KernelSetMacCursor(argv[0].ToUInt16(), argv[1].ToUInt16(),
                            argv[2].ToUInt16());
                    }
                    else
                    {
                        SciEngine.Instance._gfxCursor.KernelSetView(argv[0].ToUInt16(), argv[1].ToUInt16(),
                            argv[2].ToUInt16(), ref hotspot);
                    }
                    break;
                case 10:
                    // Freddy pharkas, when using the whiskey glass to read the prescription (bug #3034973)
                    SciEngine.Instance._gfxCursor.KernelSetZoomZone((byte) argv[0].ToUInt16(),
                        new Rect(argv[1].ToInt16(), argv[2].ToInt16(), argv[3].ToInt16(), argv[4].ToInt16()),
                        argv[5].ToUInt16(), argv[6].ToUInt16(), argv[7].ToUInt16(),
                        argv[8].ToUInt16(), (byte) argv[9].ToUInt16());
                    break;
                default:
                    Error("kSetCursor: Unhandled case: {0} arguments given", argc);
                    break;
            }
            return s.r_acc;
        }

        private static Register kSetNowSeen(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxCompare.KernelSetNowSeen(argv[0]);
            return s.r_acc;
        }

        private static Register kSetPort(EngineState s, int argc, StackPtr argv)
        {
            ushort portId;
            Rect picRect;
            short picTop, picLeft;
            bool initPriorityBandsFlag = false;

            switch (argc)
            {
                case 1:
                    portId = argv[0].ToUInt16();
                    SciEngine.Instance._gfxPorts.KernelSetActive(portId);
                    break;

                case 6:
                case 7:
                    initPriorityBandsFlag = argc == 7;
                    picRect.Top = argv[0].ToInt16();
                    picRect.Left = argv[1].ToInt16();
                    picRect.Bottom = argv[2].ToInt16();
                    picRect.Right = argv[3].ToInt16();
                    picTop = argv[4].ToInt16();
                    picLeft = argv[5].ToInt16();
                    SciEngine.Instance._gfxPorts.KernelSetPicWindow(picRect, picTop, picLeft, initPriorityBandsFlag);
                    break;

                default:
                    throw new InvalidOperationException($"SetPort was called with {argc} parameters");
            }
            return Register.NULL_REG;
        }

        private static Register kSetVideoMode(EngineState s, int argc, StackPtr argv)
        {
            // This call is used for KQ6's intro. It has one parameter, which is 1 when
            // the intro begins, and 0 when it ends. It is suspected that this is
            // actually a flag to enable video planar memory access, as the video
            // decoder in KQ6 is specifically written for the planar memory model.
            // Planar memory mode access was used for VGA "Mode X" (320x240 resolution,
            // although the intro in KQ6 is 320x200).
            // Refer to http://en.wikipedia.org/wiki/Mode_X

            //warning("STUB: SetVideoMode %d", argv[0].toUint16());
            return s.r_acc;
        }

        private static Register kShakeScreen(EngineState s, int argc, StackPtr argv)
        {
            var shakeCount = (short) (argc > 0 ? argv[0].ToUInt16() : 1);
            var directions = (short) (argc > 1 ? argv[1].ToUInt16() : 1);

            SciEngine.Instance._gfxScreen.KernelShakeScreen(shakeCount, directions);
            return s.r_acc;
        }

        /// <summary>
        /// Debug command, used by the SCI builtin debugger
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kShow(EngineState s, int argc, StackPtr argv)
        {
            ushort map = argv[0].ToUInt16();

            switch (map)
            {
                case 1: // Visual, substituted by display for us
                    SciEngine.Instance._gfxScreen.DebugShowMap(3);
                    break;
                case 2: // Priority
                    SciEngine.Instance._gfxScreen.DebugShowMap(1);
                    break;
                case 3: // Control
                case 4: // Control
                    SciEngine.Instance._gfxScreen.DebugShowMap(2);
                    break;
                default:
                    Warning($"Map {map} is not available");
                    break;
            }

            return s.r_acc;
        }

        private static Register kTextColors(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxText16.KernelTextColors(argc, argv);
            return s.r_acc;
        }

        // New calls for SCI11. Using those is only needed when using text-codes so that
        // one is able to change font and/or color multiple times during kDisplay and
        // kDrawControl
        private static Register kTextFonts(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxText16.KernelTextFonts(argc, argv);
            return s.r_acc;
        }

        private static Register kTextSize(EngineState s, int argc, StackPtr argv)
        {
            short textWidth, textHeight;
            string text = s._segMan.GetString(argv[1]);
            var dest = s._segMan.DerefRegPtr(argv[0], 4);
            int maxwidth = argc > 3 ? argv[3].ToUInt16() : 0;
            int fontNr = argv[2].ToUInt16();

            if (!dest.HasValue)
            {
                DebugC(DebugLevels.Strings, "GetTextSize: Empty destination");
                return s.r_acc;
            }

            string sep = null;
            if ((argc > 4) && (argv[4].Segment != 0))
            {
                var sepStr = s._segMan.GetString(argv[4]);
                sep = sepStr;
            }

            var d = dest.Value;
            d[0] = d[1] = Register.NULL_REG;

            if (text.Length == 0)
            {
                // Empty text
                d[2] = d[3] = Register.Make(0, 0);
                DebugC(DebugLevels.Strings, "GetTextSize: Empty string");
                return s.r_acc;
            }

            textWidth = d[3].ToInt16();
            textHeight = d[2].ToInt16();

            ushort languageSplitter = 0;
            string splitText = SciEngine.Instance.StrSplitLanguage(text, languageSplitter, sep);

            SciEngine.Instance._gfxText16.KernelTextSize(splitText, languageSplitter, (short) fontNr,
                (short) maxwidth, out textWidth, out textHeight);

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
                    Warning("kTextSize: string would be too big to fit on screen. Trimming it");
                    text = text.Trim();
                    // Copy over the trimmed string...
                    s._segMan.Strcpy(argv[1], text);
                    // ...and recalculate bounding box dimensions
                    SciEngine.Instance._gfxText16.KernelTextSize(splitText, languageSplitter, (short) fontNr,
                        (short) maxwidth, out textWidth, out textHeight);
                }
            }

            DebugC(DebugLevels.Strings, "GetTextSize '{0}' . {1}x{2}", text, textWidth, textHeight);
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                d[2] = Register.Make(0, (ushort) textHeight);
                d[3] = Register.Make(0, (ushort) textWidth);
            }
            else
            {
                d[2] = Register.Make(0, (ushort) textWidth);
                d[3] = Register.Make(0, (ushort) textHeight);
            }

            return s.r_acc;
        }

        private static Register kWait(EngineState s, int argc, StackPtr argv)
        {
            int sleep_time = argv[0].ToUInt16();

            s.Wait(sleep_time);

            return s.r_acc;
        }


        private static void kDirLoopWorker(Register @object, ushort angle, EngineState s, int argc, StackPtr argv)
        {
            var viewId = (int) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.view);
            var signal = (ViewSignals) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.signal);

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
            else
            {
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
                else
                {
                    useLoop = 0;
                }
            }
            else
            {
                short loopCount = SciEngine.Instance._gfxCache.KernelViewGetLoopCount(viewId);
                if (loopCount < 4)
                    return;
            }

            SciEngine.WriteSelectorValue(s._segMan, @object, o => o.loop, (ushort) useLoop);
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
                return (short) (color & 0x0F); // 0 - 15
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
            var type = (ControlType) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.type);
            var style = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.state);
            var x = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsLeft);
            var y = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsTop);
            int fontId = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.font);
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
                (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsRight),
                (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.nsBottom));

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
                    DebugC(DebugLevels.Graphics, "drawing button {0} to {1},{2}", controlObject, x, y);
                    SciEngine.Instance._gfxControls16.KernelDrawButton(rect, controlObject, splitText, languageSplitter,
                        fontId, (ControlStateFlags) style, hilite);
                    return;

                case ControlType.TEXT:
                    alignment = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mode);
                    DebugC(DebugLevels.Graphics, "drawing text {0} ('{1}') to {2},{3}, mode={4}", controlObject, text, x,
                        y, alignment);
                    SciEngine.Instance._gfxControls16.KernelDrawText(rect, controlObject, splitText, languageSplitter,
                        fontId, alignment, (ControlStateFlags) style, hilite);
                    s.r_acc = SciEngine.Instance._gfxText16.AllocAndFillReferenceRectArray();
                    return;

                case ControlType.TEXTEDIT:
                    mode = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.mode);
                    maxChars = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.max);
                    cursorPos = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cursor);
                    if (cursorPos > text.Length)
                    {
                        // if cursor is outside of text, adjust accordingly
                        cursorPos = (short) text.Length;
                        SciEngine.WriteSelectorValue(s._segMan, controlObject, o => o.cursor, (ushort) cursorPos);
                    }
                    DebugC(DebugLevels.Graphics, "drawing edit control {0} (text {1}, '{2}') to {3},{4}", controlObject,
                        textReference, text, x, y);
                    SciEngine.Instance._gfxControls16.KernelDrawTextEdit(rect, controlObject, splitText,
                        languageSplitter, fontId, mode, (ControlStateFlags) style, cursorPos, maxChars, hilite);
                    return;

                case ControlType.ICON:
                    viewId = (int) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.view);
                {
                    var l = (int) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.loop);
                    loopNo = (short) ((l & 0x80) != 0 ? l - 256 : l);
                    var c = (int) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cel);
                    celNo = (short) ((c & 0x80) != 0 ? c - 256 : c);
                    // Check if the control object specifies a priority selector (like in Jones)
                    Register tmp;
                    if (SciEngine.LookupSelector(s._segMan, controlObject, o => o.priority, null, out tmp) ==
                        SelectorType.Variable)
                        priority = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.priority);
                    else
                        priority = -1;
                }
                    DebugC(DebugLevels.Graphics, "drawing icon control {0} to {1},{2}", controlObject, x, y - 1);
                    SciEngine.Instance._gfxControls16.KernelDrawIcon(rect, controlObject, viewId, loopNo, celNo,
                        priority, style, hilite);
                    return;

                case ControlType.LIST:
                case ControlType.LIST_ALIAS:
                    if (type == ControlType.LIST_ALIAS)
                        isAlias = true;

                    maxChars = (short) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.x);
                    // max chars per entry
                    cursorOffset = (ushort) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.cursor);
                    if (SciEngine.Selector(o => o.topString) != -1)
                    {
                        // Games from early SCI1 onwards use topString
                        upperOffset = (ushort) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.topString);
                    }
                    else
                    {
                        // Earlier games use lsTop or brTop
                        Register tmp;
                        if (SciEngine.LookupSelector(s._segMan, controlObject, o => o.brTop, null, out tmp) ==
                            SelectorType.Variable)
                            upperOffset = (ushort) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.brTop);
                        else
                            upperOffset = (ushort) SciEngine.ReadSelectorValue(s._segMan, controlObject, o => o.lsTop);
                    }

                    // Count string entries in NULL terminated string list
                    listCount = 0;
                    listSeeker = textReference;
                    while (s._segMan.Strlen(listSeeker) > 0)
                    {
                        listCount++;
                        listSeeker = Register.IncOffset(listSeeker, maxChars);
                    }

                    // TODO: This is rather convoluted... It would be a lot cleaner
                    // if sciw_new_list_control would take a list of Common::String
                    cursorPos = 0;
                    upperPos = 0;
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
                            listSeeker = Register.IncOffset(listSeeker, maxChars);
                        }
                    }

                    DebugC(DebugLevels.Graphics, "drawing list control {0} to {1},{2}, diff {3}", controlObject, x, y,
                        SCI_MAX_SAVENAME_LENGTH);
                    SciEngine.Instance._gfxControls16.KernelDrawList(rect, controlObject, maxChars, listCount,
                        listEntries, fontId, (ControlStateFlags) style, upperPos, cursorPos, isAlias, hilite);
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

        private static Register kPalVarySetVary(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            int time = argc > 1 ? argv[1].ToInt16() * 60 : 0;
            short percent = (short) (argc > 2 ? argv[2].ToInt16() : 100);
            short fromColor;
            short toColor;

            if (argc > 4)
            {
                fromColor = argv[3].ToInt16();
                toColor = argv[4].ToInt16();
            }
            else
            {
                fromColor = toColor = -1;
            }

            SciEngine.Instance._gfxPalette32.KernelPalVarySet(paletteId, percent, time, fromColor, toColor);
            return s.r_acc;
        }

        private static Register kPalVarySetPercent(EngineState s, int argc, StackPtr argv)
        {
            int time = argc > 0 ? argv[0].ToInt16() * 60 : 0;
            short percent = (short) (argc > 1 ? argv[1].ToInt16() : 0);
            SciEngine.Instance._gfxPalette32.SetVaryPercent(percent, time, -1, -1);
            return s.r_acc;
        }

        private static Register kPalVaryGetPercent(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette32.GetVaryPercent());
        }

        private static Register kPalVaryOff(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxPalette32.VaryOff();
            return s.r_acc;
        }

        private static Register kPalVaryMergeTarget(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            SciEngine.Instance._gfxPalette32.KernelPalVaryMergeTarget(paletteId);
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette32.GetVaryPercent());
        }

        private static Register kPalVarySetTime(EngineState s, int argc, StackPtr argv)
        {
            int time = argv[0].ToInt16() * 60;
            SciEngine.Instance._gfxPalette32.SetVaryTime(time);
            return s.r_acc;
        }

        private static Register kPalVarySetTarget(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            SciEngine.Instance._gfxPalette32.KernelPalVarySetTarget(paletteId);
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette32.GetVaryPercent());
        }

        private static Register kPalVarySetStart(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            SciEngine.Instance._gfxPalette32.KernelPalVarySetStart(paletteId);
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette32.GetVaryPercent());
        }

        private static Register kPalVaryMergeStart(EngineState s, int argc, StackPtr argv)
        {
            int paletteId = argv[0].ToUInt16();
            SciEngine.Instance._gfxPalette32.KernelPalVaryMergeStart(paletteId);
            return Register.Make(0, (ushort) SciEngine.Instance._gfxPalette32.GetVaryPercent());
        }

#if ENABLE_SCI32
        private static Register kTextSize32(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxText32.SetFont(argv[2].ToUInt16());

            StackPtr? rect = s._segMan.DerefRegPtr(argv[0], 4);
            if (rect.HasValue)
            {
                Error("kTextSize: {0} cannot be dereferenced", argv[0]);
            }

            string text = s._segMan.GetString(argv[1]);
            short maxWidth = (short) (argc > 3 ? argv[3].ToInt16() : 0);
            bool doScaling = argc <= 4 || argv[4].ToInt16() != 0;

            Rect textRect = SciEngine.Instance._gfxText32.GetTextSize(text, maxWidth, doScaling);
            var r = rect.Value;
            r[0] = Register.Make(0, (ushort) textRect.Left);
            r[1] = Register.Make(0, (ushort) textRect.Top);
            r[2] = Register.Make(0, (ushort) (textRect.Right - 1));
            r[3] = Register.Make(0, (ushort) (textRect.Bottom - 1));
            return s.r_acc;
        }

        private static Register kAddPlane(EngineState s, int argc, StackPtr argv)
        {
            DebugC(6, DebugLevels.Graphics, "kAddPlane {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelAddPlane(argv[0]);
            return s.r_acc;
        }

        private static Register kAddScreenItem(EngineState s, int argc, StackPtr argv)
        {
            DebugC(6, DebugLevels.Graphics, "kAddScreenItem {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelAddScreenItem(argv[0]);
            return s.r_acc;
        }

        private static Register kCreateTextBitmap(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;

            short subop = (short) argv[0].ToUInt16();

            short width = 0;
            short height = 0;
            Register @object;

            if (subop == 0)
            {
                width = (short) argv[1].ToUInt16();
                height = (short) argv[2].ToUInt16();
                @object = argv[3];
            }
            else if (subop == 1)
            {
                @object = argv[1];
            }
            else
            {
                Warning("Invalid kCreateTextBitmap subop {0}", subop);
                return Register.NULL_REG;
            }

            string text = segMan.GetString(SciEngine.ReadSelector(segMan, @object, o => o.text));
            short foreColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.fore);
            short backColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.back);
            short skipColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.skip);
            int fontId = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.font);
            short borderColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.borderColor);
            short dimmed = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.dimmed);

            Rect rect = new Rect(
                (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.textLeft),
                (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.textTop),
                (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.textRight) + 1),
                (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.textBottom) + 1));

            if (subop == 0)
            {
                TextAlign alignment = (TextAlign) SciEngine.ReadSelectorValue(segMan, @object, o => o.mode);
                return SciEngine.Instance._gfxText32.CreateFontBitmap(width, height, rect, text, (byte) foreColor,
                    (byte) backColor, (byte) skipColor, fontId, alignment, borderColor, dimmed != 0, true, true);
            }
            CelInfo32 celInfo = new CelInfo32
            {
                type = CelType.View,
                resourceId = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.view),
                loopNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.loop),
                celNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.cel)
            };
            return SciEngine.Instance._gfxText32.CreateFontBitmap(celInfo, rect, text, foreColor, backColor, fontId,
                skipColor, borderColor, dimmed != 0, true);
        }

        private static Register kDeletePlane(EngineState s, int argc, StackPtr argv)
        {
            DebugC(6, DebugLevels.Graphics, "kDeletePlane {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelDeletePlane(argv[0]);
            return s.r_acc;
        }

        private static Register kDeleteScreenItem(EngineState s, int argc, StackPtr argv)
        {
            DebugC(6, DebugLevels.Graphics, "kDeleteScreenItem {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelDeleteScreenItem(argv[0]);
            return s.r_acc;
        }

        private static Register kBitmapDestroy(EngineState s, int argc, StackPtr argv)
        {
            s._segMan.FreeHunkEntry(argv[0]);
            return s.r_acc;
        }

        private static Register kFrameOut(EngineState s, int argc, StackPtr argv)
        {
            bool showBits = argc <= 0 || argv[0].ToUInt16() != 0;
            SciEngine.Instance._gfxFrameout.KernelFrameOut(showBits);
            return s.r_acc;
        }

        private static Register kGetHighPlanePri(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort) SciEngine.Instance._gfxFrameout.KernelGetHighPlanePri());
        }

        private static Register kIsHiRes(EngineState s, int argc, StackPtr argv)
        {
            var buffer = SciEngine.Instance._gfxFrameout.CurrentBuffer;
            if (buffer.ScreenWidth < 640 || buffer.ScreenHeight < 400)
                return Register.Make(0, 0);

            return Register.Make(0, 1);
        }

        private static Register kMessageBox(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._gfxControls32.KernelMessageBox(s._segMan.GetString(argv[0]),
                s._segMan.GetString(argv[1]), argv[2].ToUInt16());
        }

        private static Register kIsOnMe(EngineState s, int argc, StackPtr argv)
        {
            short x = argv[0].ToInt16();
            short y = argv[1].ToInt16();
            Register @object = argv[2];
            bool checkPixel = argv[3].ToInt16() != 0;

            return SciEngine.Instance._gfxFrameout.KernelIsOnMe(@object, new Point(x, y), checkPixel);
        }

        /// <summary>
        /// Causes an immediate plane transition with an optional transition
        /// effect
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kSetShowStyle(EngineState s, int argc, StackPtr argv)
        {
            ShowStyleType type = (ShowStyleType) argv[0].ToUInt16();
            Register planeObj = argv[1];
            short seconds = argv[2].ToInt16();
            // NOTE: This value seems to indicate whether the transition is an
            // “exit” transition (0) or an “enter” transition (-1) for fade
            // transitions. For other types of transitions, it indicates a palette
            // index value to use when filling the screen.
            short back = argv[3].ToInt16();
            short priority = argv[4].ToInt16();
            short animate = argv[5].ToInt16();
            // TODO: Rename to frameOutNow?
            short refFrame = argv[6].ToInt16();
            short blackScreen;
            Register pFadeArray;
            short divisions;

            // SCI 2–2.1early
            if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
            {
                blackScreen = 0;
                pFadeArray = Register.NULL_REG;
                divisions = (short) (argc > 7 ? argv[7].ToInt16() : -1);
            }
            // SCI 2.1mid–2.1late
            else if (ResourceManager.GetSciVersion() < SciVersion.V3)
            {
                blackScreen = 0;
                pFadeArray = argc > 7 ? argv[7] : Register.NULL_REG;
                divisions = (short) (argc > 8 ? argv[8].ToInt16() : -1);
            }
            // SCI 3
            else
            {
                blackScreen = argv[7].ToInt16();
                pFadeArray = argc > 8 ? argv[8] : Register.NULL_REG;
                divisions = (short) (argc > 9 ? argv[9].ToInt16() : -1);
            }

            if ((ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE && SciEngine.Instance.GameId != SciGameId.KQ7 && type == ShowStyleType.kShowStyleMorph) || type > ShowStyleType.kShowStyleMorph)
            {
                Error("Illegal show style {0} for plane {1}", type, planeObj);
            }

            // TODO: Reuse later for SCI2 and SCI3 implementation and then discard
            //	warning("kSetShowStyle: effect %d, plane: %04x:%04x (%s), sec: %d, "
            //			"dir: %d, prio: %d, animate: %d, ref frame: %d, black screen: %d, "
            //			"pFadeArray: %04x:%04x (%s), divisions: %d",
            //			type, PRINT_REG(planeObj), s._segMan.getObjectName(planeObj), seconds,
            //			back, priority, animate, refFrame, blackScreen,
            //			PRINT_REG(pFadeArray), s._segMan.getObjectName(pFadeArray), divisions);

            // NOTE: The order of planeObj and showStyle are reversed
            // because this is how SCI3 called the corresponding method
            // on the KernelMgr
            SciEngine.Instance._gfxTransitions32.KernelSetShowStyle((ushort)argc, planeObj, type, seconds, back, priority, animate, refFrame, pFadeArray, divisions, blackScreen);

            return s.r_acc;
        }

        private static Register kUpdatePlane(EngineState s, int argc, StackPtr argv)
        {
            DebugC(7, DebugLevels.Graphics, "kUpdatePlane {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelUpdatePlane(argv[0]);
            return s.r_acc;
        }

        private static Register kUpdateScreenItem(EngineState s, int argc, StackPtr argv)
        {
            DebugC(7, DebugLevels.Graphics, "kUpdateScreenItem {0} ({1})", argv[0], s._segMan.GetObjectName(argv[0]));
            SciEngine.Instance._gfxFrameout.KernelUpdateScreenItem(argv[0]);
            return s.r_acc;
        }

        private static Register kObjectIntersect(EngineState s, int argc, StackPtr argv)
        {
            Rect objRect1 = SciEngine.Instance._gfxCompare.GetNSRect(argv[0]);
            Rect objRect2 = SciEngine.Instance._gfxCompare.GetNSRect(argv[1]);
            return Register.Make(0, objRect1.Intersects(objRect2));
        }

        private static Register kEditText(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._gfxControls32.KernelEditText(argv[0]);
        }

        private static Register kSetScroll(EngineState s, int argc, StackPtr argv)
        {
            // Called in the intro of LSL6 hires (room 110)
            // The end effect of this is the same as the old screen scroll transition

            // 7 parameters
            Register planeObject = argv[0];
            //int16 x = argv[1].toSint16();
            //int16 y = argv[2].toSint16();
            ushort pictureId = argv[3].ToUInt16();
            // param 4: int (0 in LSL6, probably scroll direction? The picture in LSL6 scrolls down)
            // param 5: int (first call is 1, then the subsequent one is 0 in LSL6)
            // param 6: optional int (0 in LSL6)

            // Set the new picture directly for now
            //writeSelectorValue(s._segMan, planeObject, SELECTOR(left), x);
            //writeSelectorValue(s._segMan, planeObject, SELECTOR(top), y);
            SciEngine.WriteSelectorValue(s._segMan, planeObject, o => o.picture, pictureId);
            // and update our draw list
            SciEngine.Instance._gfxFrameout.KernelUpdatePlane(planeObject);

            // TODO
            return kStub(s, argc, argv);
        }

        private static Register kPalCycle(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kPalCycleSetCycle(EngineState s, int argc, StackPtr argv)
        {
            ushort fromColor = argv[0].ToUInt16();
            ushort toColor = argv[1].ToUInt16();
            short direction = argv[2].ToInt16();
            ushort delay = (ushort) (argc > 3 ? argv[3].ToUInt16() : 0);

            SciEngine.Instance._gfxPalette32.SetCycle(fromColor, toColor, direction, delay);
            return s.r_acc;
        }

        private static Register kPalCycleDoCycle(EngineState s, int argc, StackPtr argv)
        {
            ushort fromColor = argv[0].ToUInt16();
            short speed = (short) (argc > 1 ? argv[1].ToInt16() : 1);

            SciEngine.Instance._gfxPalette32.DoCycle((byte) fromColor, speed);
            return s.r_acc;
        }

        private static Register kPalCyclePause(EngineState s, int argc, StackPtr argv)
        {
            if (argc == 0)
            {
                SciEngine.Instance._gfxPalette32.CycleAllPause();
            }
            else
            {
                ushort fromColor = argv[0].ToUInt16();
                SciEngine.Instance._gfxPalette32.CyclePause((byte) fromColor);
            }
            return s.r_acc;
        }

        private static Register kPalCycleOn(EngineState s, int argc, StackPtr argv)
        {
            if (argc == 0)
            {
                SciEngine.Instance._gfxPalette32.CycleAllOn();
            }
            else
            {
                ushort fromColor = argv[0].ToUInt16();
                SciEngine.Instance._gfxPalette32.CycleOn((byte) fromColor);
            }
            return s.r_acc;
        }

        private static Register kPalCycleOff(EngineState s, int argc, StackPtr argv)
        {
            if (argc == 0)
            {
                SciEngine.Instance._gfxPalette32.CycleAllOff();
            }
            else
            {
                ushort fromColor = argv[0].ToUInt16();
                SciEngine.Instance._gfxPalette32.CycleOff((byte) fromColor);
            }
            return s.r_acc;
        }

        private static Register kTextWidth(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxText32.SetFont(argv[1].ToUInt16());
            string text = s._segMan.GetString(argv[0]);
            return Register.Make(0, (ushort) SciEngine.Instance._gfxText32.GetStringWidth(text));
        }

        private static Register kText(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kAddPicAt(EngineState s, int argc, StackPtr argv)
        {
            Register planeObj = argv[0];
            int pictureId = argv[1].ToUInt16();
            short x = argv[2].ToInt16();
            short y = argv[3].ToInt16();
            bool mirrorX = argc > 4 && argv[4].ToInt16() != 0;

            SciEngine.Instance._gfxFrameout.KernelAddPicAt(planeObj, pictureId, x, y, mirrorX);
            return s.r_acc;
        }

        private static Register kWinHelp(EngineState s, int argc, StackPtr argv)
        {
            switch (argv[0].ToUInt16())
            {
                case 1:
                    // Load a help file
                    // Maybe in the future we can implement this, but for now this message should suffice
                    ShowScummVMDialog("Please use an external viewer to open the game's help file: " +
                                      s._segMan.GetString(argv[1]));
                    break;
                case 2:
                    // Looks like some init function
                    break;
                default:
                    Warning("Unknown kWinHelp subop {0}", argv[0].ToUInt16());
                    break;
            }

            return s.r_acc;
        }

        private static Register kCelInfo(EngineState s, int argc, StackPtr argv)
        {
            // Used by Shivers 1, room 23601 to determine what blocks on the red door puzzle board
            // are occupied by pieces already

            CelObjView view = CelObjView.Create(argv[1].ToUInt16(), argv[2].ToInt16(), argv[3].ToInt16());

            short result = 0;

            switch (argv[0].ToUInt16())
            {
                case 0:
                    result = view._displace.X;
                    break;
                case 1:
                    result = view._displace.Y;
                    break;
                case 2:
                case 3:
                    // null operation
                    break;
                case 4:
                    result = view.ReadPixel(argv[4].ToUInt16(), argv[5].ToUInt16(), view._mirrorX);
                    break;
            }

            return Register.Make(0, (ushort) result);
        }

        private static Register kScrollWindow(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kScrollWindowCreate(EngineState s, int argc, StackPtr argv)
        {
            Register @object = argv[0];
            ushort maxNumEntries = argv[1].ToUInt16();

            SegManager segMan = s._segMan;
            short borderColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.borderColor);
            TextAlign alignment = (TextAlign) SciEngine.ReadSelectorValue(segMan, @object, o => o.mode);
            int fontId = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.font);
            short backColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.back);
            short foreColor = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.fore);
            Register plane = SciEngine.ReadSelector(segMan, @object, o => o.plane);

            Rect rect = new Rect();
            rect.Left = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.nsLeft);
            rect.Top = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.nsTop);
            rect.Right = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.nsRight) + 1);
            rect.Bottom = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.nsBottom) + 1);
            Point position = new Point(rect.Left, rect.Top);

            return SciEngine.Instance._gfxControls32.MakeScrollWindow(rect, position, plane, (byte) foreColor,
                (byte) backColor, fontId, alignment, borderColor, maxNumEntries);
        }

        private static Register kScrollWindowAdd(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            string text = s._segMan.GetString(argv[1]);
            int fontId = argv[2].ToInt16();
            short color = argv[3].ToInt16();
            TextAlign alignment = (TextAlign) argv[4].ToInt16();
            bool scrollTo = argc <= 5 || argv[5].ToUInt16() != 0;

            return scrollWindow.Add(text, fontId, color, alignment, scrollTo);
        }

        private static Register kScrollWindowWhere(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            ushort where = (ushort) (argv[1].ToUInt16() * scrollWindow.Where);

            return Register.Make(0, where);
        }

        private static Register kScrollWindowGo(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            Rational scrollTop = new Rational(argv[1].ToInt16(), argv[2].ToInt16());
            scrollWindow.Go(scrollTop);

            return s.r_acc;
        }

        private static Register kScrollWindowModify(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            Register entryId = argv[1];
            string newText = s._segMan.GetString(argv[2]);
            int fontId = argv[3].ToInt16();
            short color = argv[4].ToInt16();
            TextAlign alignment = (TextAlign) argv[5].ToInt16();
            bool scrollTo = argc <= 6 || argv[6].ToUInt16() != 0;

            return scrollWindow.Modify(entryId, newText, fontId, color, alignment, scrollTo);
        }

        private static Register kScrollWindowHide(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.Hide();

            return s.r_acc;
        }

        private static Register kScrollWindowShow(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.Show();

            return s.r_acc;
        }

        private static Register kScrollWindowPageUp(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.PageUp();

            return s.r_acc;
        }

        private static Register kScrollWindowPageDown(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.PageDown();

            return s.r_acc;
        }

        private static Register kScrollWindowUpArrow(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.UpArrow();

            return s.r_acc;
        }

        private static Register kScrollWindowDownArrow(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.DownArrow();

            return s.r_acc;
        }

        private static Register kScrollWindowHome(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.Home();

            return s.r_acc;
        }

        private static Register kScrollWindowEnd(EngineState s, int argc, StackPtr argv)
        {
            ScrollWindow scrollWindow = SciEngine.Instance._gfxControls32.GetScrollWindow(argv[0]);

            scrollWindow.End();

            return s.r_acc;
        }

        private static Register kScrollWindowDestroy(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxControls32.DestroyScrollWindow(argv[0]);

            return s.r_acc;
        }

        private static Register kSetFontHeight(EngineState s, int argc, StackPtr argv)
        {
            // TODO: Setting font may have just been for side effect
            // of setting the fontHeight on the font manager, in
            // which case we could just get the font directly ourselves.
            SciEngine.Instance._gfxText32.SetFont(argv[0].ToUInt16());
            GfxText32._yResolution = (short) ((SciEngine.Instance._gfxText32._font.Height *
                                                SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight +
                                                GfxText32._yResolution - 1) / GfxText32._yResolution);
            return Register.Make(0, (ushort) GfxText32._yResolution);
        }

        private static Register kSetFontRes(EngineState s, int argc, StackPtr argv)
        {
            GfxText32._xResolution = argv[0].ToInt16();
            GfxText32._yResolution = argv[1].ToInt16();
            return s.r_acc;
        }

        private static Register kFont(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kBitmap(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kBitmapCreate(EngineState s, int argc, StackPtr argv)
        {
            short width = argv[0].ToInt16();
            short height = argv[1].ToInt16();
            short skipColor = argv[2].ToInt16();
            short backColor = argv[3].ToInt16();
            short scaledWidth = argc > 4 ? argv[4].ToInt16() : GfxText32._xResolution;
            short scaledHeight = argc > 5 ? argv[5].ToInt16() : GfxText32._yResolution;
            bool useRemap = argc > 6 && argv[6].ToInt16() != 0;

            Register bitmapId = new Register();
            SciBitmap bitmap = s._segMan.AllocateBitmap(out bitmapId, width, height, (byte) skipColor, 0, 0, scaledWidth,
                scaledHeight, 0, useRemap, true);
            bitmap.Pixels.Data.Set(bitmap.Pixels.Offset, (byte) backColor, width * height);
            return bitmap.Object;
        }

        private static Register kBitmapDrawLine(EngineState s, int argc, StackPtr argv)
        {
            // bitmapMemId, (x1, y1, x2, y2) OR (x2, y2, x1, y1), line color, unknown int, unknown int
            return kStubNull(s, argc + 1, argv - 1);
        }

        private static Register kBitmapDrawView(EngineState s, int argc, StackPtr argv)
        {
            SciBitmap bitmap = s._segMan.LookupBitmap(argv[0]);
            CelObjView view = CelObjView.Create(argv[1].ToUInt16(), argv[2].ToInt16(), argv[3].ToInt16());

            short x = (short) (argc > 4 ? argv[4].ToInt16() : 0);
            short y = (short) (argc > 5 ? argv[5].ToInt16() : 0);
            short alignX = (short) (argc > 7 ? argv[7].ToInt16() : -1);
            short alignY = (short) (argc > 8 ? argv[8].ToInt16() : -1);

            Point position = new Point(x == -1 ? bitmap.Origin.X : x,
                y == -1 ? bitmap.Origin.Y : y
            );

            position.X -= alignX == -1 ? view._displace.X : alignX;
            position.Y -= alignY == -1 ? view._displace.Y : alignY;

            Rect drawRect = new Rect(position.X, position.Y,
                (short) (position.X + view._width), (short) (position.Y + view._height));
            drawRect.Clip(new Rect((short) bitmap.Width, (short) bitmap.Height));
            view.Draw(bitmap.Buffer, ref drawRect, ref position, view._mirrorX);
            return s.r_acc;
        }

        private static Register kBitmapDrawText(EngineState s, int argc, StackPtr argv)
        {
            // called e.g. from TextButton::createBitmap() in Torin's Passage, script 64894

            SciBitmap bitmap = s._segMan.LookupBitmap(argv[0]);
            string text = s._segMan.GetString(argv[1]);
            Rect textRect = new Rect(argv[2].ToInt16(),
                argv[3].ToInt16(),
                (short) (argv[4].ToInt16() + 1),
                (short) (argv[5].ToInt16() + 1));
            short foreColor = argv[6].ToInt16();
            short backColor = argv[7].ToInt16();
            short skipColor = argv[8].ToInt16();
            int fontId = (int) argv[9].ToUInt16();
            TextAlign alignment = (TextAlign) argv[10].ToInt16();
            short borderColor = argv[11].ToInt16();
            bool dimmed = argv[12].ToUInt16() != 0;

            // NOTE: Technically the engine checks these things:
            // textRect.bottom > 0
            // textRect.right > 0
            // textRect.left < sciBitmap.width
            // textRect.top < sciBitmap.height
            // Then clips. But this seems stupid.
            textRect.Clip(new Rect((short) bitmap.Width, (short) bitmap.Height));

            Register textBitmapObject = SciEngine.Instance._gfxText32.CreateFontBitmap(textRect.Width, textRect.Height,
                new Rect(textRect.Width, textRect.Height), text, (byte) foreColor, (byte) backColor, (byte) skipColor,
                fontId, alignment, borderColor, dimmed, false, false);
            CelObjMem textCel = new CelObjMem(textBitmapObject);
            var p = new Point(textRect.Left, textRect.Top);
            textCel.Draw(bitmap.Buffer, ref textRect, ref p, false);
            s._segMan.FreeHunkEntry(textBitmapObject);

            return s.r_acc;
        }

        private static Register kBitmapDrawColor(EngineState s, int argc, StackPtr argv)
        {
            // called e.g. from TextView::init() and TextView::draw() in Torin's Passage, script 64890

            SciBitmap bitmap = s._segMan.LookupBitmap(argv[0]);
            Rect fillRect = new Rect(argv[1].ToInt16(),
                argv[2].ToInt16(),
                (short) (argv[3].ToInt16() + 1),
                (short) (argv[4].ToInt16() + 1));

            bitmap.Buffer.FillRect(fillRect, (uint) argv[5].ToInt16());
            return s.r_acc;
        }

        private static Register kBitmapDrawBitmap(EngineState s, int argc, StackPtr argv)
        {
            // target bitmap, source bitmap, x, y, unknown boolean

            return kStubNull(s, argc + 1, argv - 1);
        }

        private static Register kBitmapInvert(EngineState s, int argc, StackPtr argv)
        {
            // bitmap, left, top, right, bottom, foreColor, backColor

            return kStubNull(s, argc + 1, argv - 1);
        }

        private static Register kBitmapSetDisplace(EngineState s, int argc, StackPtr argv)
        {
            SciBitmap bitmap = s._segMan.LookupBitmap(argv[0]);
            bitmap.Origin = new Point(argv[1].ToInt16(), argv[2].ToInt16());
            return s.r_acc;
        }

        private static Register kBitmapCreateFromView(EngineState s, int argc, StackPtr argv)
        {
            // viewId, loopNo, celNo, skipColor, backColor, useRemap, source overlay bitmap

            return kStub(s, argc + 1, argv - 1);
        }

        private static Register kBitmapCopyPixels(EngineState s, int argc, StackPtr argv)
        {
            // target bitmap, source bitmap

            return kStubNull(s, argc + 1, argv - 1);
        }

        private static Register kBitmapClone(EngineState s, int argc, StackPtr argv)
        {
            // bitmap

            return kStub(s, argc + 1, argv - 1);
        }

        private static Register kBitmapGetInfo(EngineState s, int argc, StackPtr argv)
        {
            // bitmap

            // argc 1 = get width
            // argc 2 = pixel at row 0 col n
            // argc 3 = pixel at row n col n
            return kStub(s, argc + 1, argv - 1);
        }

        private static Register kBitmapScale(EngineState s, int argc, StackPtr argv)
        {
            // TODO: SCI3
            return kStubNull(s, argc + 1, argv - 1);
        }

        private static Register kBitmapCreateFromUnknown(EngineState s, int argc, StackPtr argv)
        {
            // TODO: SCI3
            return kStub(s, argc + 1, argv - 1);
        }

        private static Register kAddLine(EngineState s, int argc, StackPtr argv)
        {
            Register plane = argv[0];
            Point startPoint = new Point(argv[1].ToInt16(), argv[2].ToInt16());
            Point endPoint = new Point(argv[3].ToInt16(), argv[4].ToInt16());

            short priority;
            byte color;
            LineStyle style;
            ushort pattern;
            byte thickness;

            if (argc == 10)
            {
                priority = argv[5].ToInt16();
                color = (byte) argv[6].ToUInt16();
                style = (LineStyle) argv[7].ToInt16();
                pattern = argv[8].ToUInt16();
                thickness = (byte) argv[9].ToUInt16();
            }
            else
            {
                priority = 1000;
                color = 255;
                style = LineStyle.Solid;
                pattern = 0;
                thickness = 1;
            }

            return SciEngine.Instance._gfxPaint32.KernelAddLine(plane, startPoint, endPoint, priority, color, style,
                pattern, thickness);
        }

        private static Register kUpdateLine(EngineState s, int argc, StackPtr argv)
        {
            Register screenItemObject = argv[0];
            Register planeObject = argv[1];
            Point startPoint = new Point(argv[2].ToInt16(), argv[3].ToInt16());
            Point endPoint = new Point(argv[4].ToInt16(), argv[5].ToInt16());

            short priority;
            byte color;
            LineStyle style;
            ushort pattern;
            byte thickness;

            var plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(planeObject);
            if (plane == null)
            {
                Error("kUpdateLine: Plane {0} not found", planeObject);
            }

            ScreenItem screenItem = plane._screenItemList.FindByObject(screenItemObject);
            if (screenItem == null)
            {
                Error("kUpdateLine: Screen item {0} not found", screenItemObject);
            }

            if (argc == 11)
            {
                priority = argv[6].ToInt16();
                color = (byte) argv[7].ToUInt16();
                style = (LineStyle) argv[8].ToInt16();
                pattern = argv[9].ToUInt16();
                thickness = (byte) argv[10].ToUInt16();
            }
            else
            {
                priority = screenItem._priority;
                color = screenItem._celInfo.color;
                style = LineStyle.Solid;
                pattern = 0;
                thickness = 1;
            }

            SciEngine.Instance._gfxPaint32.KernelUpdateLine(screenItem, plane, startPoint, endPoint, priority, color,
                style, pattern, thickness);

            return s.r_acc;
        }

        private static Register kDeleteLine(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxPaint32.KernelDeleteLine(argv[0], argv[1]);
            return s.r_acc;
        }

        private static Register kMovePlaneItems(EngineState s, int argc, StackPtr argv)
        {
            Register plane = argv[0];
            short deltaX = argv[1].ToInt16();
            short deltaY = argv[2].ToInt16();
            bool scrollPics = argc > 3 && argv[3].ToUInt16() != 0;

            SciEngine.Instance._gfxFrameout.KernelMovePlaneItems(plane, deltaX, deltaY, scrollPics);
            return s.r_acc;
        }

        private static Register kSetPalStyleRange(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxFrameout.KernelSetPalStyleRange((byte) argv[0].ToUInt16(), (byte) argv[1].ToUInt16());
            return s.r_acc;
        }

        // Used by SQ6, script 900, the datacorder reprogramming puzzle (from room 270)
        private static Register kMorphOn(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxFrameout._palMorphIsOn = true;
            return s.r_acc;
        }

        private static void ShowScummVMDialog(string message)
        {
            throw new NotImplementedException();
//            GUI::MessageDialog dialog(message, "OK");
//            dialog.runModal();
        }

        private static Register kBaseSetter32(EngineState s, int argc, StackPtr argv)
        {
            Register @object = argv[0];

            int viewId = (int) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.view);
            short loopNo = (short) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.loop);
            short celNo = (short) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.cel);
            short x = (short) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.x);
            short y = (short) SciEngine.ReadSelectorValue(s._segMan, @object, o => o.y);

            CelObjView celObj = CelObjView.Create(viewId, loopNo, celNo);

            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            Rational scaleX = new Rational(scriptWidth, celObj._xResolution);

            short brLeft;

            if (celObj._mirrorX)
            {
                brLeft = (short) (x - (celObj._width - celObj._origin.X) * scaleX);
            }
            else
            {
                brLeft = (short) (x - (celObj._origin.X * scaleX));
            }

            short brRight = (short) (brLeft + (celObj._width * scaleX) - 1);

            SciEngine.WriteSelectorValue(s._segMan, @object, o => o.brLeft, (ushort) brLeft);
            SciEngine.WriteSelectorValue(s._segMan, @object, o => o.brRight, (ushort) brRight);
            SciEngine.WriteSelectorValue(s._segMan, @object, o => o.brBottom, (ushort) (y + 1));
            SciEngine.WriteSelectorValue(s._segMan, @object, o => o.brTop,
                (ushort) (y + 1 - SciEngine.ReadSelectorValue(s._segMan,
                              @object, o => o.yStep)));

            return s.r_acc;
        }

        private static Register kSetCursor32(EngineState s, int argc, StackPtr argv)
        {
            switch (argc)
            {
                case 1:
                {
                    if (argv[0].ToInt16() == -2)
                    {
                        SciEngine.Instance._gfxCursor32.ClearRestrictedArea();
                    }
                    else
                    {
                        if (argv[0].IsNull)
                        {
                            SciEngine.Instance._gfxCursor32.Hide();
                        }
                        else
                        {
                            SciEngine.Instance._gfxCursor32.Show();
                        }
                    }
                    break;
                }
                case 2:
                {
                    Point position = new Point(argv[0].ToInt16(), argv[1].ToInt16());
                    SciEngine.Instance._gfxCursor32.SetPosition(position);
                    break;
                }
                case 3:
                {
                    SciEngine.Instance._gfxCursor32.SetView(argv[0].ToUInt16(), argv[1].ToInt16(), argv[2].ToInt16());
                    break;
                }
                case 4:
                {
                    Rect restrictRect = new Rect(argv[0].ToInt16(),
                        argv[1].ToInt16(),
                        (short) (argv[2].ToInt16() + 1),
                        (short) (argv[3].ToInt16() + 1));
                    SciEngine.Instance._gfxCursor32.SetRestrictedArea(restrictRect);
                    break;
                }
                default:
                    Error("kSetCursor: Invalid number of arguments ({0})", argc);
                    break;
            }

            return s.r_acc;
        }


        private static Register kSetNowSeen32(EngineState s, int argc, StackPtr argv)
        {
            bool found = SciEngine.Instance._gfxFrameout.KernelSetNowSeen(argv[0]);

            // NOTE: MGDX is assumed to use the older kSetNowSeen since it was
            // released before SQ6, but this has not been verified since it cannot be
            // disassembled at the moment (Phar Lap Windows-only release)
            if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_EARLY ||
                SciEngine.Instance.GameId == SciGameId.SQ6 ||
                SciEngine.Instance.GameId == SciGameId.MOTHERGOOSEHIRES)
            {
                if (!found)
                {
                    Error("kSetNowSeen: Unable to find screen item {0}", argv[0]);
                }
                return s.r_acc;
            }

            if (!found)
            {
                Warning("kSetNowSeen: Unable to find screen item {0}", argv[0]);
            }

            return Register.Make(0, found);
        }

        private static Register kShakeScreen32(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._gfxFrameout.ShakeScreen(argv[0].ToInt16(), (ShakeDirection) argv[1].ToInt16());
            return s.r_acc;
        }

        private static Register kRemapColorsByRange(EngineState s, int argc, StackPtr argv)
        {
            byte color = (byte) argv[0].ToUInt16();
            short from = argv[1].ToInt16();
            short to = argv[2].ToInt16();
            short @base = argv[3].ToInt16();
            // NOTE: There is an optional last parameter after `base`
            // which was only used by the priority map debugger, which
            // does not exist in release versions of SSCI
            SciEngine.Instance._gfxRemap32.RemapByRange(color, from, to, @base);
            return s.r_acc;
        }

        private static Register kRemapColorsByPercent(EngineState s, int argc, StackPtr argv)
        {
            byte color = (byte) argv[0].ToUInt16();
            short percent = argv[1].ToInt16();
            // NOTE: There is an optional last parameter after `percent`
            // which was only used by the priority map debugger, which
            // does not exist in release versions of SSCI
            SciEngine.Instance._gfxRemap32.RemapByPercent(color, percent);
            return s.r_acc;
        }

        private static Register kRemapColorsToGray(EngineState s, int argc, StackPtr argv)
        {
            byte color = (byte) argv[0].ToUInt16();
            short gray = argv[1].ToInt16();
            // NOTE: There is an optional last parameter after `gray`
            // which was only used by the priority map debugger, which
            // does not exist in release versions of SSCI
            SciEngine.Instance._gfxRemap32.RemapToGray(color, (sbyte) gray);
            return s.r_acc;
        }

        private static Register kRemapColorsToPercentGray(EngineState s, int argc, StackPtr argv)
        {
            byte color = (byte) argv[0].ToUInt16();
            short gray = argv[1].ToInt16();
            short percent = argv[2].ToInt16();
            // NOTE: There is an optional last parameter after `percent`
            // which was only used by the priority map debugger, which
            // does not exist in release versions of SSCI
            SciEngine.Instance._gfxRemap32.RemapToPercentGray(color, gray, percent);
            return s.r_acc;
        }

        private static Register kRemapColorsBlockRange(EngineState s, int argc, StackPtr argv)
        {
            byte from = (byte) argv[0].ToUInt16();
            byte count = (byte) argv[1].ToUInt16();
            SciEngine.Instance._gfxRemap32.BlockRange(from, count);
            return s.r_acc;
        }
#endif
    }
}