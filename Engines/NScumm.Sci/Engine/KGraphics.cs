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

        private static Register kDirLoop(EngineState s, int argc, StackPtr? argv)
        {
            kDirLoopWorker(argv.Value[0], argv.Value[1].ToUInt16(), s, argc, argv);

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

        private static Register kSetCursorSci11(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
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

        private static Register kGraph(EngineState s, int argc, StackPtr? argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kGraphGetColorCount(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, SciEngine.Instance._gfxPalette.TotalColorCount);
        }

        private static Register kGraphDrawLine(EngineState s, int argc, StackPtr? argv)
        {
            short color = AdjustGraphColor(argv.Value[4].ToInt16());
            short priority = (argc > 5) ? argv.Value[5].ToInt16() : (short)-1;
            short control = (argc > 6) ? argv.Value[6].ToInt16() : (short)-1;

            SciEngine.Instance._gfxPaint16.KernelGraphDrawLine(GetGraphPoint(argv.Value), GetGraphPoint(argv.Value + 2), color, priority, control);
            return s.r_acc;
        }

        private static Register kGraphSaveBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            ushort screenMask = (ushort)(argv.Value[4].ToUInt16() & (ushort)(Graphics.GfxScreenMasks.ALL));
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveBox(rect, screenMask);
        }

        private static Register kGraphRestoreBox(EngineState s, int argc, StackPtr? argv)
        {
            // This may be called with a memoryhandle from SAVE_BOX or SAVE_UPSCALEDHIRES_BOX
            SciEngine.Instance._gfxPaint16.KernelGraphRestoreBox(argv.Value[0]);
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

        private static Register kGraphUpdateBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            // argv[4] is the map (1 for visual, etc.)
            // argc == 6 on upscaled hires
            bool hiresMode = (argc > 5) ? true : false;
            SciEngine.Instance._gfxPaint16.KernelGraphUpdateBox(rect, hiresMode);
            return s.r_acc;
        }

        private static Register kGraphRedrawBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            SciEngine.Instance._gfxPaint16.KernelGraphRedrawBox(rect);
            return s.r_acc;
        }

        // Seems to be only implemented for SCI0/SCI01 games
        private static Register kGraphAdjustPriority(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._gfxPorts.KernelGraphAdjustPriority(argv.Value[0].ToUInt16(), argv.Value[1].ToUInt16());
            return s.r_acc;
        }

        private static Register kGraphSaveUpscaledHiresBox(EngineState s, int argc, StackPtr? argv)
        {
            Rect rect = GetGraphRect(argv.Value);
            return SciEngine.Instance._gfxPaint16.KernelGraphSaveUpscaledHiresBox(rect);
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
    }
}
