//
//  ScummEngine6_Misc.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Diagnostics;
using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0xae)]
        protected virtual void SystemOps()
        {
            //TODO: Restart
            var subOp = ReadByte();

            switch (subOp)
            {
                case 158:               // SO_RESTART
//                    Restart();
                    break;
                case 159:               // SO_PAUSE
                    ShowMenu();
                    break;
                case 160:               // SO_QUIT
                    HasToQuit = true;
                    break;
                default:
                    throw new NotSupportedException(string.Format("SystemOps invalid case {0}", subOp));
            }
        }

        [OpCode(0xbd)]
        protected override void Dummy()
        {
        }

        [OpCode(0xc8)]
        protected virtual void KernelGetFunctions()
        {
            var vs = MainVirtScreen;

            var args = GetStackList(30);

            switch (args[0])
            {
                case 113:
                    // WORKAROUND for bug #899249: The scripts used for screen savers
                    // in Sam & Max use hard coded values for the maximum height and width.
                    // This causes problems in rooms (ie. Credits) where their values are
                    // lower, so we set result to zero if out of bounds.
                    if (args[1] >= 0 && args[1] <= vs.Width && args[2] >= 0 && args[2] <= vs.Height)
                    {
                        var nav = new PixelNavigator(vs.Surfaces[0]);
                        nav.GoTo(args[1], args[2]);
                        var pixel = nav.Read();
                        Push(pixel);
                    }
                    else
                    {
                        Push(0);
                    }
                    break;
                case 115:
                    Push(GetSpecialBox(new Point((short)args[1], (short)args[2])));
                    break;
                case 116:
                    Push(CheckXYInBoxBounds(args[3], new Point((short)args[1], (short)args[2])));
                    break;
                case 206:
                    Push(RemapPaletteColor(args[1], args[2], args[3], -1));
                    break;
                case 207:
                    {
                        var i = GetObjectIndex(args[1]);
                        Debug.Assert(i != 0);
                        Push(_objs[i].Position.X);
                    }
                    break;
                case 208:
                    {
                        var i = GetObjectIndex(args[1]);
                        Debug.Assert(i != 0);
                        Push(_objs[i].Position.Y);
                    }
                    break;
                case 209:
                    {
                        var i = GetObjectIndex(args[1]);
                        Debug.Assert(i != 0);
                        Push(_objs[i].Width);
                    }
                    break;
                case 210:
                    {
                        var i = GetObjectIndex(args[1]);
                        Debug.Assert(i != 0);
                        Push(_objs[i].Height);
                    }
                    break;
                case 211:
                    /*
                   13 = thrust
                   336 = thrust
                   328 = thrust
                   27 = abort
                   97 = left
                   331 = left
                   115 = right
                   333 = right
                 */

                    Push(GetKeyState(args[1]));
                    break;
                case 212:
                    {
                        var a = Actors[args[1]];
                        // This is used by walk scripts
                        Push(a.Frame);
                    }
                    break;
                case 213:
                    {
                        var slot = GetVerbSlot(args[1], 0);
                        Push(Verbs[slot].CurRect.Left);
                    }
                    break;
                case 214:
                    {
                        var slot = GetVerbSlot(args[1], 0);
                        Push(Verbs[slot].CurRect.Top);
                    }
                    break;
                case 215:
                    if ((_extraBoxFlags[args[1]] & 0x00FF) == 0x00C0)
                    {
                        Push(_extraBoxFlags[args[1]]);
                    }
                    else
                    {
                        Push((int)GetBoxFlags((byte)args[1]));
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("KernelGetFunctions: default case {0}", args[0]));
            }
        }

        [OpCode(0xc9)]
        protected virtual void KernelSetFunctions()
        {
            var args = GetStackList(30);

            switch (args[0])
            {
                case 3:
                    // Dummy case
                    break;
                case 4:
                    GrabCursor(args[1], args[2], args[3], args[4]);
                    break;
                case 5:
                    FadeOut(args[1]);
                    break;
                case 6:
                    _fullRedraw = true;
                    RedrawBGAreas();
                    SetActorRedrawFlags();
                    ProcessActors();
                    FadeIn((byte)args[1]);
                    break;
            // TODO: scumm6: start maniac
//                case 8:
//                    StartManiac();
//                    break;
                case 9:
                    KillAllScriptsExceptCurrent();
                    break;
            // TODO:scumm6: NukeFlObjects
//                case 104:                                                                       /* samnmax */
//                    NukeFlObjects(args[2], args[3]);
//                    break;
                case 107:                                                                       /* set actor scale */
                    {
                        var a = Actors[args[1]];
                        a.SetScale(args[2], -1);
                    }
                    break;
                case 108:                                                                       /* create proc_special_palette */
                case 109:
                    // Case 108 and 109 share the same function
                    if (args.Length != 6)
                        throw new InvalidOperationException(string.Format("KernelSetFunctions sub op {0}: expected 6 params but got {1}", args[0], args.Length));
                    SetShadowPalette(args[3], args[4], args[5], args[1], args[2], 0, 256);
                    break;
                case 110:
                    Gdi.ClearMaskBuffer(0);
                    break;
                case 111:
                    {
                        var a = Actors[args[1]];
                        var modes = new int[2];
                        Array.Copy(args, 2, modes, 0, args.Length - 2);
                        a.ShadowMode = (byte)(modes[0] + modes[1]);
                    }
                    break;
                case 112:                                                                       /* palette shift? */
                    SetShadowPalette(args[3], args[4], args[5], args[1], args[2], args[6], args[7]);
                    break;
                case 114:
                    // Sam & Max film noir mode
                    if (Game.GameId == Scumm.IO.GameId.SamNMax)
                    {
                        // At this point ScummVM will already have set
                        // variable 0x8000 to indicate that the game is
                        // in film noir mode. All we have to do here is
                        // to mark the palette as "dirty", because
                        // updatePalette() will desaturate the colors
                        // as they are uploaded to the backend.
                        //
                        // This actually works better than the original
                        // interpreter, where actors would sometimes
                        // still be drawn in color.
                        SetDirtyColors(0, 255);
                    }
                    else
                        throw new InvalidOperationException("stub KernelSetFunctions_114()");
                    break;
                case 117:
                    // Sam & Max uses this opcode in script-43, right
                    // before a screensaver is selected.
                    //
                    // Sam & Max uses variable 132 to specify the number of
                    // minutes of inactivity (no mouse movements) before
                    // starting the screensaver, so setting it to 0 will
                    // help in debugging.
                    FreezeScripts(0x80);
                    break;
                case 119:
                    EnqueueObject(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], 0);
                    break;
                case 120:
                    SwapPalColors(args[1], args[2]);
                    break;
                case 122:
                    Variables[VariableSoundResult.Value] = IMuse.DoCommand(args.Length - 1, args.Skip(1).ToArray());
                    break;
                case 123:
                    CopyPalColor(args[2], args[1]);
                    break;
                case 124:
                    _saveSound = args[1] != 0;
                    break;
                default:
                    throw new NotSupportedException(string.Format("KernelSetFunctions: default case {0} (param count {1})", args[0], args.Length));
            }
        }

        [OpCode(0xD0)]
        protected virtual void GetDateTime()
        {
            var dt = DateTime.Now;

            Variables[VariableTimeDateYear] = dt.Year;
            Variables[VariableTimeDateMonth] = dt.Month;
            Variables[VariableTimeDateDay] = dt.Day;
            Variables[VariableTimeDateHour] = dt.Hour;
            Variables[VariableTimeDateMinute] = dt.Minute;

            if (Game.Version == 8)
                Variables[VariableTimeDateSecond.Value] = dt.Second;
        }

        [OpCode(0xe1)]
        protected void GetPixel(int x, int y)
        {
            var vs = FindVirtScreen(y);
            if (vs == null || x > ScreenWidth - 1 || x < 0)
            {
                Push(-1);
                return;
            }

            var nav = new PixelNavigator(vs.Surfaces[0]);
            nav.GoTo(x, y - vs.TopLine);
            var pixel = nav.Read();
            Push(pixel);
        }

        protected int GetSpecialBox(Point p)
        {
            var numOfBoxes = GetNumBoxes() - 1;

            for (var i = numOfBoxes; i >= 0; i--)
            {
                var flag = GetBoxFlags((byte)i);

                if (!flag.HasFlag(BoxFlags.Invisible) && (flag.HasFlag(BoxFlags.PlayerOnly)))
                    return -1;

                if (CheckXYInBoxBounds(i, p))
                    return i;
            }

            return -1;
        }

        protected internal int RemapPaletteColor(int r, int g, int b, int threshold)
        {
            int ar, ag, ab;
            int sum, bestsum, bestitem = 0;

            var startColor = (Game.Version == 8) ? 24 : 1;

            if (r > 255)
                r = 255;
            if (g > 255)
                g = 255;
            if (b > 255)
                b = 255;

            bestsum = 0x7FFFFFFF;

            r &= ~3;
            g &= ~3;
            b &= ~3;

            for (var i = startColor; i < 255; i++)
            {
                var palColor = CurrentPalette.Colors[i];
                // TODO: vs
//                if (Game.Version == 7 && _colorUsedByCycle[i])
//                    continue;

                ar = palColor.R & ~3;
                ag = palColor.G & ~3;
                ab = palColor.B & ~3;
                if (ar == r && ag == g && ab == b)
                    return i;

                sum = ColorWeight(ar - r, ag - g, ab - b);

                if (sum < bestsum)
                {
                    bestsum = sum;
                    bestitem = i;
                }
            }
            if (threshold != -1 && bestsum > ColorWeight(threshold, threshold, threshold))
            {
                // Best match exceeded threshold. Try to find an unused palette entry and
                // use it for our purpose.
                for (var i = 254; i > 48; i--)
                {
                    var palColor = CurrentPalette.Colors[i];
                    if (palColor.R >= 252 && palColor.G >= 252 && palColor.B >= 252)
                    {
                        SetPalColor(i, r, g, b);
                        return i;
                    }
                }
            }

            return bestitem;
        }

        static int ColorWeight(int red, int green, int blue)
        {
            return 3 * red * red + 6 * green * green + 2 * blue * blue;
        }

        protected override void PalManipulateInit(int resID, int start, int end, int time)
        {
            var newPal = roomData.Palettes[resID];

            _palManipStart = start;
            _palManipEnd = end;
            _palManipCounter = 0;

            if (_palManipPalette == null)
                _palManipPalette = new Palette();
            if (_palManipIntermediatePal == null)
                _palManipIntermediatePal = new Palette();

            for (int i = start; i < end; ++i)
            {
                _palManipPalette.Colors[i] = newPal.Colors[i];
                _palManipIntermediatePal.Colors[i] = Color.FromRgb(
                    CurrentPalette.Colors[i].R << 8,
                    CurrentPalette.Colors[i].G << 8,
                    CurrentPalette.Colors[i].B << 8);
            }

            _palManipCounter = time;
        }

        protected void KillAllScriptsExceptCurrent()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (i != CurrentScript)
                {
                    Slots[i].Status = ScriptStatus.Dead;
                    Slots[i].CutSceneOverride = 0;
                }
            }
        }

        void SwapPalColors(int a, int b)
        {
            if (a >= 256 || b >= 256)
                throw new InvalidOperationException(string.Format("SwapPalColors: invalid values, {0}, {1}", a, b));

            var tmp = CurrentPalette.Colors[a];
            CurrentPalette.Colors[a] = CurrentPalette.Colors[b];
            CurrentPalette.Colors[b] = tmp;

            SetDirtyColors(a, a);
            SetDirtyColors(b, b);
        }

        void CopyPalColor(int dst, int src)
        {
            if (dst >= 256 || src >= 256)
                throw new InvalidOperationException(string.Format("copyPalColor: invalid values, {0}, {1}", dst, src));

            CurrentPalette.Colors[dst] = CurrentPalette.Colors[src];
            SetDirtyColors(dst, dst);
        }
    }
}
