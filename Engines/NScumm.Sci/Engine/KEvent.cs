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

using NScumm.Core;
using NScumm.Core.Graphics;
using System.Collections.Generic;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal partial class Kernel
    {
        private static int g_debug_simulated_key = 0;

        private static Dictionary<ushort, ushort> keyToDirMap = new Dictionary<ushort, ushort>
        {
            {SciEvent.SCI_KEY_HOME, 8},
            {SciEvent.SCI_KEY_UP, 1},
            {SciEvent.SCI_KEY_PGUP, 2},
            {SciEvent.SCI_KEY_LEFT, 7},
            {SciEvent.SCI_KEY_CENTER, 0},
            {SciEvent.SCI_KEY_RIGHT, 3},
            {SciEvent.SCI_KEY_END, 6},
            {SciEvent.SCI_KEY_DOWN, 5},
            {SciEvent.SCI_KEY_PGDOWN, 4},
        };

        private static Register kGetEvent(EngineState s, int argc, StackPtr argv)
        {
            int mask = argv[0].ToUInt16();
            Register obj = argv[1];
            SciEvent curEvent;
            int modifier_mask = ResourceManager.GetSciVersion() <= SciVersion.V01
                ? SciEvent.SCI_KEYMOD_ALL
                : SciEvent.SCI_KEYMOD_NO_FOOLOCK;
            ushort modifiers = 0;
            SegManager segMan = s._segMan;
            Point mousePos;

            // For Mac games with an icon bar, handle possible icon bar events first
            if (SciEngine.Instance.HasMacIconBar)
            {
                Register iconObj = SciEngine.Instance._gfxMacIconBar.HandleEvents();
                if (!iconObj.IsNull)
                    SciEngine.InvokeSelector(s, iconObj, o => o.select, argc, argv, 0, StackPtr.Null);
            }

            // If there's a simkey pending, and the game wants a keyboard event, use the
            // simkey instead of a normal event
            if (g_debug_simulated_key != 0 && (mask & SciEvent.SCI_EVENT_KEYBOARD) != 0)
            {
                // In case we use a simulated event we query the current mouse position
                mousePos = SciEngine.Instance._gfxCursor.Position;

                // Limit the mouse cursor position, if necessary
                SciEngine.Instance._gfxCursor.RefreshPosition();

                SciEngine.WriteSelectorValue(segMan, obj, o => o.type, SciEvent.SCI_EVENT_KEYBOARD); // Keyboard event
                SciEngine.WriteSelectorValue(segMan, obj, o => o.message, (ushort)g_debug_simulated_key);
                SciEngine.WriteSelectorValue(segMan, obj, o => o.modifiers, SciEvent.SCI_KEYMOD_NUMLOCK); // Numlock on
                SciEngine.WriteSelectorValue(segMan, obj, o => o.x, (ushort)mousePos.X);
                SciEngine.WriteSelectorValue(segMan, obj, o => o.y, (ushort)mousePos.Y);
                g_debug_simulated_key = 0;
                return Register.Make(0, 1);
            }

            curEvent = SciEngine.Instance.EventManager.GetSciEvent(mask);

            if (s._delayedRestoreGame)
            {
                // delayed restore game from ScummVM menu got triggered
                Savegame.gamestate_delayedrestore(s);
                return Register.NULL_REG;
            }

            // For a real event we use its associated mouse position
# if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                mousePos = curEvent.mousePosSci;
            else
            {
#endif
                mousePos = curEvent.mousePos;

                // Limit the mouse cursor position, if necessary
                SciEngine.Instance._gfxCursor.RefreshPosition();
#if ENABLE_SCI32
            }
#endif

            if (SciEngine.Instance.Vocabulary != null)
                SciEngine.Instance.Vocabulary.parser_event = Register.NULL_REG; // Invalidate parser event

            if (s._cursorWorkaroundActive)
            {
                // We check if the actual cursor position is inside specific rectangles
                // where the cursor itself should be moved to. If this is the case, we
                // set the mouse cursor's position to be within the rectangle in
                // question. Check GfxCursor::setPosition(), for a more detailed
                // explanation and a list of cursor position workarounds.
                if (s._cursorWorkaroundRect.Contains(mousePos.X, mousePos.Y))
                {
                    // For OpenPandora and possibly other platforms, that support analog-stick control + touch screen
                    // control at the same time: in case the cursor is currently at the coordinate set by the scripts,
                    // we will count down instead of immediately disabling the workaround.
                    // On OpenPandora the cursor position is set, but it's overwritten shortly afterwards by the
                    // touch screen. In this case we would sometimes disable the workaround, simply because the touch
                    // screen hasn't yet overwritten the position and thus the workaround would not work anymore.
                    // On OpenPandora it would sometimes work and sometimes not without this.
                    if (s._cursorWorkaroundPoint == mousePos)
                    {
                        // Cursor is still at the same spot as set by the scripts
                        if (s._cursorWorkaroundPosCount > 0)
                        {
                            s._cursorWorkaroundPosCount--;
                        }
                        else
                        {
                            // Was for quite a bit of time at that spot, so disable workaround now
                            s._cursorWorkaroundActive = false;
                        }
                    }
                    else
                    {
                        // Cursor has moved, but is within the rect . disable workaround immediately
                        s._cursorWorkaroundActive = false;
                    }
                }
                else
                {
                    mousePos.X = s._cursorWorkaroundPoint.X;
                    mousePos.Y = s._cursorWorkaroundPoint.Y;
                }
            }

            SciEngine.WriteSelectorValue(segMan, obj, o => o.x, (ushort)mousePos.X);
            SciEngine.WriteSelectorValue(segMan, obj, o => o.y, (ushort)mousePos.Y);

            // Get current keyboard modifiers, only keep relevant bits
            modifiers = (ushort)(curEvent.modifiers & modifier_mask);
            if (SciEngine.Instance.Platform == Core.IO.Platform.DOS)
            {
                // We are supposed to emulate SCI running in DOS

                // We set the higher byte of the modifiers to 02h
                // Original SCI also did that indirectly, because it asked BIOS for shift status
                // via AH=0x02 INT16, which then sets the shift flags in AL
                // AH is supposed to be destroyed in that case and it's not defined that 0x02
                // is still in it on return. The value of AX was then set into the modifiers selector.
                // At least one fan-made game (Betrayed Alliance) requires 0x02 to be in the upper byte,
                // otherwise the darts game (script 111) will not work properly.

                // It seems Sierra fixed this behaviour (effectively bug) in the SCI1 keyboard driver.
                // SCI32 also resets the upper byte.

                // This was verified in SSCI itself by creating a SCI game and checking behavior.
                if (ResourceManager.GetSciVersion() <= SciVersion.V01)
                {
                    modifiers |= 0x0200;
                }
            }

            switch (curEvent.type)
            {
                case SciEvent.SCI_EVENT_QUIT:
                    s.abortScriptProcessing = AbortGameState.QuitGame; // Terminate VM
                    SciEngine.Instance._debugState.seeking = DebugSeeking.Nothing;
                    SciEngine.Instance._debugState.runningStep = 0;
                    break;

                case SciEvent.SCI_EVENT_KEYBOARD:
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.type, SciEvent.SCI_EVENT_KEYBOARD);
                    // Keyboard event
                    s.r_acc = Register.Make(0, 1);

                    SciEngine.WriteSelectorValue(segMan, obj, o => o.message, (ushort)curEvent.character);
                    // We only care about the translated character
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.modifiers,
                        (ushort)(curEvent.modifiers & modifier_mask));
                    break;

                case SciEvent.SCI_EVENT_MOUSE_RELEASE:
                case SciEvent.SCI_EVENT_MOUSE_PRESS:

                    // TODO: track left buttton clicks, if requested
                    //if (curEvent.type == SciEvent.SCI_EVENT_MOUSE_PRESS && curEvent.data == 1 && g_debug_track_mouse_clicks)
                    //{
                    //    SciEngine.Instance.getSciDebugger().debugPrintf("Mouse clicked at %d, %d\n",
                    //                mousePos.x, mousePos.y);
                    //}

                    if ((mask & curEvent.type) != 0)
                    {
                        SciEngine.WriteSelectorValue(segMan, obj, o => o.type, (ushort)curEvent.type);
                        SciEngine.WriteSelectorValue(segMan, obj, o => o.message, 0);
                        SciEngine.WriteSelectorValue(segMan, obj, o => o.modifiers, modifiers);
                        s.r_acc = Register.Make(0, 1);
                    }
                    break;

                default:
                    // Return a null event
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.type, SciEvent.SCI_EVENT_NONE);
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.message, 0);
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.modifiers, (ushort)modifiers);
                    s.r_acc = Register.NULL_REG;
                    break;
            }

            if ((s.r_acc.Offset != 0) && (SciEngine.Instance._debugState.stopOnEvent))
            {
                SciEngine.Instance._debugState.stopOnEvent = false;

                // TODO: A SCI event occurred, and we have been asked to stop, so open the debug console
                //Console* con = SciEngine.Instance.getSciDebugger();
                //con.debugPrintf("SCI event occurred: ");
                //switch (curEvent.type)
                //{
                //    case SciEvent.SCI_EVENT_QUIT:
                //        con.debugPrintf("quit event\n");
                //        break;
                //    case SciEvent.SCI_EVENT_KEYBOARD:
                //        con.debugPrintf("keyboard event\n");
                //        break;
                //    case SciEvent.SCI_EVENT_MOUSE_RELEASE:
                //    case SciEvent.SCI_EVENT_MOUSE_PRESS:
                //        con.debugPrintf("mouse click event\n");
                //        break;
                //    default:
                //        con.debugPrintf("unknown or no event (event type %d)\n", curEvent.type);
                //}

                //con.attach();
                //con.onFrame();
            }

            if (SciEngine.Instance.Features.DetectDoSoundType() <= SciVersion.V0_LATE)
            {
                // If we're running a sound-SCI0 game, update the sound cues, to
                // compensate for the fact that sound-SCI0 does not poll to update
                // the sound cues itself, like sound-SCI1 and later do with
                // cmdUpdateSoundCues. kGetEvent is called quite often, so emulate
                // the sound-SCI1 behavior of cmdUpdateSoundCues with this call
                SciEngine.Instance._soundCmd.UpdateSci0Cues();
            }

            // Wait a bit here, so that the CPU isn't maxed out when the game
            // is waiting for user input (e.g. when showing text boxes) - bug
            // #3037874. Make sure that we're not delaying while the game is
            // benchmarking, as that will affect the final benchmarked result -
            // check bugs #3058865 and #3127824
            if (s._gameIsBenchmarking)
            {
                // Game is benchmarking, don't add a delay
            }
            else if (ResourceManager.GetSciVersion() < SciVersion.V2)
            {
                ServiceLocator.Platform.Sleep(10);
            }

            return s.r_acc;
        }

        private static Register kGlobalToLocal(EngineState s, int argc, StackPtr argv)
        {
            Register obj = argv[0];
            Register planeObject = argc > 1 ? argv[1] : Register.NULL_REG; // SCI32
            SegManager segMan = s._segMan;

            if (obj.Segment != 0)
            {
                short x = (short)SciEngine.ReadSelectorValue(segMan, obj, o => o.x);
                short y = (short)SciEngine.ReadSelectorValue(segMan, obj, o => o.y);

                SciEngine.Instance._gfxCoordAdjuster.KernelGlobalToLocal(ref x, ref y, planeObject);

                SciEngine.WriteSelectorValue(segMan, obj, o => o.x, (ushort)x);
                SciEngine.WriteSelectorValue(segMan, obj, o => o.y, (ushort)y);
            }

            return s.r_acc;
        }

        private static Register kJoystick(EngineState s, int argc, StackPtr argv)
        {
            // Subfunction 12 sets/gets joystick repeat rate
            //debug(5, "Unimplemented syscall 'Joystick()'");
            return Register.NULL_REG;
        }

        private static Register kLocalToGlobal(EngineState s, int argc, StackPtr argv)
        {
            Register obj = argv[0];
            Register planeObject = argc > 1 ? argv[1] : Register.NULL_REG; // SCI32
            SegManager segMan = s._segMan;

            if (obj.Segment != 0)
            {
                short x = (short)SciEngine.ReadSelectorValue(segMan, obj, o => o.x);
                short y = (short)SciEngine.ReadSelectorValue(segMan, obj, o => o.y);

                SciEngine.Instance._gfxCoordAdjuster.KernelLocalToGlobal(ref x, ref y, planeObject);

                SciEngine.WriteSelectorValue(segMan, obj, o => o.x, (ushort)x);
                SciEngine.WriteSelectorValue(segMan, obj, o => o.y, (ushort)y);
            }

            return s.r_acc;
        }


        private static Register kMapKeyToDir(EngineState s, int argc, StackPtr argv)
        {
            Register obj = argv[0];
            SegManager segMan = s._segMan;

            if (SciEngine.ReadSelectorValue(segMan, obj, o => o.type) == SciEvent.SCI_EVENT_KEYBOARD)
            {
                // Keyboard
                ushort message = (ushort)SciEngine.ReadSelectorValue(segMan, obj, o => o.message);
                ushort eventType = SciEvent.SCI_EVENT_DIRECTION;
                // Check if the game is using cursor views. These games allowed control
                // of the mouse cursor via the keyboard controls (the so called
                // "PseudoMouse" functionality in script 933).
                if (SciEngine.Instance.Features.DetectSetCursorType() == SciVersion.V1_1)
                    eventType |= SciEvent.SCI_EVENT_KEYBOARD;

                if (keyToDirMap.ContainsKey(message))
                {
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.type, eventType);
                    SciEngine.WriteSelectorValue(segMan, obj, o => o.message, keyToDirMap[message]);
                    return Register.TRUE_REG; // direction mapped
                }

                return Register.NULL_REG; // unknown direction
            }

            return s.r_acc; // no keyboard event to map, leave accumulator unchanged
        }

        private static Register kGlobalToLocal32(EngineState s, int argc, StackPtr argv)
        {
            Register result = argv[0];
            Register planeObj = argv[1];

            bool visible = true;
            var plane = SciEngine.Instance._gfxFrameout.VisiblePlanes.FindByObject(planeObj);
            if (plane == null)
            {
                plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(planeObj);
                visible = false;
            }
            if (plane == null)
            {
                Error("kGlobalToLocal: Plane {0} not found", planeObj);
            }

            short x = (short)(SciEngine.ReadSelectorValue(s._segMan, result, o => o.x) - plane._gameRect.Left);
            short y = (short)(SciEngine.ReadSelectorValue(s._segMan, result, o => o.y) - plane._gameRect.Top);

            SciEngine.WriteSelectorValue(s._segMan, result, o => o.x, (ushort)x);
            SciEngine.WriteSelectorValue(s._segMan, result, o => o.y, (ushort)y);

            return Register.Make(0, visible);
        }

        private static Register kLocalToGlobal32(EngineState s, int argc, StackPtr argv)
        {
            Register result = argv[0];
            Register planeObj = argv[1];

            bool visible = true;
            var plane = SciEngine.Instance._gfxFrameout.VisiblePlanes.FindByObject(planeObj);
            if (plane == null)
            {
                plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(planeObj);
                visible = false;
            }
            if (plane == null)
            {
                Error("kLocalToGlobal: Plane {0} not found", planeObj);
            }

            short x = (short)(SciEngine.ReadSelectorValue(s._segMan, result, o => o.x) + plane._gameRect.Left);
            short y = (short)(SciEngine.ReadSelectorValue(s._segMan, result, o => o.y) + plane._gameRect.Top);

            SciEngine.WriteSelectorValue(s._segMan, result, o => o.x, (ushort)x);
            SciEngine.WriteSelectorValue(s._segMan, result, o => o.y, (ushort)y);

            return Register.Make(0, visible);
        }

        private static Register kSetHotRectangles(EngineState s, int argc, StackPtr argv)
        {
            if (argc == 1)
            {
                SciEngine.Instance.EventManager.SetHotRectanglesActive(argv[0].ToUInt16() != 0);
                return s.r_acc;
            }

            short numRects = argv[0].ToInt16();
            SciArray hotRects = s._segMan.LookupArray(argv[1]);

            Rect[] rects = new Rect[numRects];
            var p = new UShortAccess(hotRects.ByteAt(0));
            for (var i = 0; i < numRects; ++i)
            {
                rects[i].Left = (short)p[i * 4];
                rects[i].Top = (short)p[i * 4 + 1];
                rects[i].Right = (short)(p[i * 4 + 2] + 1);
                rects[i].Bottom = (short)(p[i * 4 + 3] + 1);
            }

            SciEngine.Instance.EventManager.SetHotRectanglesActive(true);
            SciEngine.Instance.EventManager.SetHotRectangles(rects);
            return s.r_acc;
        }
    }
}