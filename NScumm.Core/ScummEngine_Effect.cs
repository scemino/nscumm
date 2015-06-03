//
//  ScummEngine_Effect.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using NScumm.Core.Graphics;
using System.Threading;
using NScumm.Core.IO;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        bool _shakeEnabled;
        int _shakeFrame;
        static readonly int[] ShakePositions = { 0, 1 * 2, 2 * 2, 1 * 2, 0 * 2, 2 * 2, 3 * 2, 1 * 2 };
        protected byte _newEffect = 129, _switchRoomEffect2, _switchRoomEffect;
        protected bool _disableFadeInEffect;
        bool _doEffect;
        bool _screenEffectFlag;

        protected void FadeIn(byte effect)
        {
            if (_disableFadeInEffect)
            {
                // fadeIn() calls can be disabled in TheDig after a SMUSH movie
                // has been played. Like the original interpreter, we introduce
                // an extra flag to handle 
                _disableFadeInEffect = false;
                _doEffect = false;
                _screenEffectFlag = true;
                return;
            }

            UpdatePalette();

            switch (effect)
            {
                case 0:
				// seems to do nothing
                    break;

                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
				// Some of the transition effects won't work properly unless
				// the screen is marked as clean first. At first I thought I
				// could safely do this every time fadeIn() was called, but
				// that broke the FOA intro. Probably other things as well.
				//
				// Hopefully it's safe to do it at this point, at least.
                    MainVirtScreen.SetDirtyRange(0, 0);
                    DoTransitionEffect(effect - 1);
                    break;

                case 128:
                    UnkScreenEffect6();
                    break;

                case 129:
                    break;

                case 130:
                case 131:
                case 132:
                case 133:
                    ScrollEffect(133 - effect);
                    break;

                case 134:
                    DissolveEffect(1, 1);
                    break;

                case 135:
                    DissolveEffect(1, MainVirtScreen.Height);
                    break;

                default:
                    throw new NotSupportedException(string.Format("Unknown screen effect {0}", effect));
            }
            _screenEffectFlag = true;
        }

        protected void FadeOut(int effect)
        {
            _mainVirtScreen.SetDirtyRange(0, 0);

            if (Game.Version < 7)
                Camera.LastPosition.X = Camera.CurrentPosition.X;

            if (Game.Version == 3 && _game.Platform == Platform.FMTowns)
                Gdi.Fill(TextSurface, 
                    new Rect(0, MainVirtScreen.TopLine * _textSurfaceMultiplier, 
                        _textSurface.Pitch, (MainVirtScreen.TopLine + MainVirtScreen.Height) * _textSurfaceMultiplier), 0);

            if ((Game.Version == 7 || _screenEffectFlag) && effect != 0)
            {
                // Fill screen 0 with black
                var pixNav = new PixelNavigator(_mainVirtScreen.Surfaces[0]);
                pixNav.OffsetX(_mainVirtScreen.XStart);
                Gdi.Fill(pixNav, 0, _mainVirtScreen.Width, _mainVirtScreen.Height);

                // Fade to black with the specified effect, if any.
                switch (effect)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        DoTransitionEffect(effect - 1);
                        break;
                    case 128:
                        UnkScreenEffect6();
                        break;

                    case 129:
					// Just blit screen 0 to the display (i.e. display will be black)
                        _mainVirtScreen.SetDirtyRange(0, _mainVirtScreen.Height);
                        UpdateDirtyScreen(_mainVirtScreen);
                        if (_townsScreen != null)
                            _townsScreen.Update();
                        break;
				
                    default:
                        throw new NotImplementedException(string.Format("fadeOut: case {0}", effect));
                }
            }

            // Update the palette at the end (once we faded to black) to avoid
            // some nasty effects when the palette is changed
            UpdatePalette();

            _screenEffectFlag = false;
        }

        protected void SetShake(bool enabled)
        {
            if (_shakeEnabled != enabled)
                _fullRedraw = true;

            _shakeEnabled = enabled;
            _shakeFrame = 0;
            _gfxManager.SetShakePos(0);
        }

        void HandleEffects()
        {
            if (Game.Version >= 4)
            {
                CyclePalette();
            }
            PalManipulate();
            if (_doEffect)
            {
                _doEffect = false;
                FadeIn(_newEffect);
                ClearClickedStatus();
            }
        }

        void ScrollEffect(int dir)
        {
            var vs = MainVirtScreen;

            int step;
            var delay = VariableFadeDelay.HasValue ? Variables[VariableFadeDelay.Value] * FadeDelay : PictureDelay;

            if ((dir == 0) || (dir == 1))
                step = vs.Height;
            else
                step = vs.Width;

            step = (step * delay) / Scrolltime;

            int m = _textSurfaceMultiplier;
            int vsPitch = vs.Pitch;

            switch (dir)
            {
                case 0:
                    //up
                    {
                        var y = 1 + step;
                        while (y < vs.Height)
                        {
                            if (_townsScreen != null)
                            {
                                TownsDrawStripToScreen(vs, 0, vs.TopLine + vs.Height - step, 0, y - step, vs.Width, step);
                            }
                            else
                            {
                                MoveScreen(0, -step, vs.Height);

                                var src = vs.Surfaces[0].Pixels;
                                _gfxManager.CopyRectToScreen(src, vsPitch,
                                    0, y - step,
                                    0, (vs.Height - step) * m,
                                    vs.Width * m, step * m);
                                _gfxManager.UpdateScreen();
                            }
                            WaitForTimer(delay);
                            y += step;
                        }
                    }
                    break;
                case 1:
                    // down
                    {
                        var y = 1 + step;
                        while (y < vs.Height)
                        {
                            MoveScreen(0, step, vs.Height);
                            if (_townsScreen != null)
                            {
                                TownsDrawStripToScreen(vs, 0, vs.TopLine, 0, vs.Height - y, vs.Width, step);
                            }
                            else
                            {
                                var src = vs.Surfaces[0].Pixels;
                                _gfxManager.CopyRectToScreen(src,
                                    vsPitch,
                                    0, vs.Height - y,
                                    0, 0,
                                    vs.Width * m, step * m);
                                _gfxManager.UpdateScreen();
                            }
                            WaitForTimer(delay);
                            y += step;
                        }
                    }
                    break;
                case 2:
                    // left
                    {
                        var x = 1 + step;
                        while (x < vs.Width)
                        {
                            MoveScreen(-step, 0, vs.Height);

                            if (_townsScreen != null)
                            {
                                TownsDrawStripToScreen(vs, vs.Width - step, vs.TopLine, x - step, 0, step, vs.Height);
                            }
                            else
                            {
                                var src = vs.Surfaces[0].Pixels;
                                _gfxManager.CopyRectToScreen(src,
                                    vsPitch,
                                    x - step, 0,
                                    (vs.Width - step) * m, 0,
                                    step * m, vs.Height * m);
                                _gfxManager.UpdateScreen();
                            }

                            WaitForTimer(delay);
                            x += step;
                        }
                    }
                    break;
                case 3:
                    // right
                    {
                        var x = 1 + step;
                        while (x < vs.Width)
                        {
                            MoveScreen(step, 0, vs.Height);
                            if (_townsScreen != null)
                            {
                                TownsDrawStripToScreen(vs, 0, vs.TopLine, vs.Width - x, 0, step, vs.Height);
                            }
                            else
                            {
                                var src = vs.Surfaces[0].Pixels;
                                _gfxManager.CopyRectToScreen(src,
                                    vsPitch,
                                    vs.Width - x, 0,
                                    0, 0,
                                    step, vs.Height);
                                _gfxManager.UpdateScreen();
                            }

                            WaitForTimer(delay);
                            x += step;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Moves the screen content by the offset specified via dx/dy.
        /// Only the region from x=0 till x=height-1 is affected.
        /// </summary>
        /// <param name="dx">The horizontal offset.</param>
        /// <param name="dy">The vertical offset.</param>
        /// <param name="height">The number of lines which in which the move will be done.</param>
        void MoveScreen(int dx, int dy, int height)
        {
            // Short circuit check - do we have to do anything anyway?
            if ((dx == 0 && dy == 0) || height <= 0)
                return;

            var screen = _gfxManager.Capture();
            if (screen == null)
                return;

            screen.Move(dx, dy, height);
            _gfxManager.CopyRectToScreen(screen.Pixels, screen.Pitch, 0, 0, screen.Width, screen.Height);
        }

        /// <summary>
        /// Update width*height areas of the screen, in random order, until the whole
        /// screen has been updated. For instance:
        ///
        /// dissolveEffect(1, 1) produces a pixel-by-pixel dissolve
        /// dissolveEffect(8, 8) produces a square-by-square dissolve
        /// dissolveEffect(virtsrc[0].width, 1) produces a line-by-line dissolve
        /// </summary>
        /// <param name='width'>
        /// Width.
        /// </param>
        /// <param name='height'>
        /// Height.
        /// </param>
        protected void DissolveEffect(int width, int height)
        {
            var vs = MainVirtScreen;
            int[] offsets;
            int blits_before_refresh, blits;
            int x, y;
            int w, h;
            int i;
            var rnd = new Random();

            // There's probably some less memory-hungry way of doing  But
            // since we're only dealing with relatively small images, it shouldn't
            // be too bad.

            w = vs.Width / width;
            h = vs.Height / height;

            // When used correctly, vs.width % width and vs.height % height
            // should both be zero, but just to be safe...

            if ((vs.Width % width) != 0)
                w++;

            if ((vs.Height % height) != 0)
                h++;

            offsets = new int[w * h];

            // Create a permutation of offsets into the frame buffer

            if (width == 1 && height == 1)
            {
                // Optimized case for pixel-by-pixel dissolve

                for (i = 0; i < vs.Width * vs.Height; i++)
                    offsets[i] = i;

                for (i = 1; i < w * h; i++)
                {
                    int j;

                    j = rnd.Next(i);
                    offsets[i] = offsets[j];
                    offsets[j] = i;
                }
            }
            else
            {
                int[] offsets2;

                for (i = 0, x = 0; x < vs.Width; x += width)
                    for (y = 0; y < vs.Height; y += height)
                        offsets[i++] = y * vs.Pitch + x;

                offsets2 = new int[w * h];

                Array.Copy(offsets, offsets2, offsets.Length);

                for (i = 1; i < w * h; i++)
                {
                    int j;

                    j = rnd.Next(i);
                    offsets[i] = offsets[j];
                    offsets[j] = offsets2[i];
                }
            }

            // Blit the image piece by piece to the screen. The idea here is that
            // the whole update should take about a quarter of a second, assuming
            // most of the time is spent in waitForTimer(). It looks good to me,
            // but might still need some tuning.

            blits = 0;
            blits_before_refresh = (3 * w * h) / 25;

            // Speed up the effect for CD Loom since it uses it so often. I don't
            // think the original had any delay at all, so on modern hardware it
            // wasn't even noticeable.
            if (_game.GameId == NScumm.Core.IO.GameId.Loom)
                blits_before_refresh *= 2;

            for (i = 0; i < w * h; i++)
            {
                x = offsets[i] % vs.Pitch;
                y = offsets[i] / vs.Pitch;

                if (_game.Platform == Platform.FMTowns)
                    TownsDrawStripToScreen(vs, x, y + vs.TopLine, x, y, width, height);
                else
                    _gfxManager.CopyRectToScreen(vs.Surfaces[0].Pixels, vs.Pitch,
                        x, y, width, height);


                if (++blits >= blits_before_refresh)
                {
                    blits = 0;
                    ServiceLocator.Platform.Sleep(30);
                }
            }

            if (blits != 0)
            {
                ServiceLocator.Platform.Sleep(30);
            }
        }

        void UnkScreenEffect6()
        {
            DissolveEffect(8, 4);
        }

        /// <summary>
        /// Perform a transition effect. There are four different effects possible:
        /// 0: Iris effect
        /// 1: Box wipe (a black box expands from the upper-left corner to the lower-right corner)
        /// 2: Box wipe (a black box expands from the lower-right corner to the upper-left corner)
        /// 3: Inverse box wipe
        /// </summary>
        /// <remarks>
        /// All effects operate on 8x8 blocks of the screen. These blocks are updated
        /// in a certain order; the exact order determines how the effect appears to the user.
        /// </remarks>
        /// <param name="a">The transition effect to perform.</param>
        void DoTransitionEffect(int a)
        {
            int[] delta = new int[16];                              // Offset applied during each iteration
            int[] tab_2 = new int[16];
            int i, j;
            int bottom;
            int l, t, r, b;
            var height = Math.Min(MainVirtScreen.Height, ScreenHeight);
            var delay = VariableFadeDelay.HasValue ? Variables[VariableFadeDelay.Value] * FadeDelay : PictureDelay;

            for (i = 0; i < 16; i++)
            {
                delta[i] = transitionEffects[a].deltaTable[i];
                j = transitionEffects[a].stripTable[i];
                if (j == 24)
                    j = height / 8 - 1;
                tab_2[i] = j;
            }

            bottom = height / 8;
            for (j = 0; j < transitionEffects[a].numOfIterations; j++)
            {
                for (i = 0; i < 4; i++)
                {
                    l = tab_2[i * 4];
                    t = tab_2[i * 4 + 1];
                    r = tab_2[i * 4 + 2];
                    b = tab_2[i * 4 + 3];

                    if (t == b)
                    {
                        while (l <= r)
                        {
                            if (l >= 0 && l < Gdi.NumStrips && t < bottom)
                            {
                                MainVirtScreen.TDirty[l] = ScreenTop + t * 8;
                                MainVirtScreen.BDirty[l] = ScreenTop + (b + 1) * 8;
                            }
                            l++;
                        }
                    }
                    else
                    {
                        if (l < 0 || l >= Gdi.NumStrips || b <= t)
                            continue;
                        if (b > bottom)
                            b = bottom;
                        if (t < 0)
                            t = 0;
                        MainVirtScreen.TDirty[l] = ScreenTop + t * 8;
                        MainVirtScreen.BDirty[l] = ScreenTop + (b + 1) * 8;
                    }
                    UpdateDirtyScreen(MainVirtScreen);
                }

                for (i = 0; i < 16; i++)
                    tab_2[i] += delta[i];

                // Draw the current state to the screen and wait a few secs so the
                // user can watch the effect taking place.
                WaitForTimer(delay);
            }
        }

        public void WaitForTimer(int msec_delay)
        {
            //            if (_fastMode & 2)
            //                msec_delay = 0;
            //            else if (_fastMode & 1)
            //                msec_delay = 10;

            var start_time = Environment.TickCount;

            while (!HasToQuit)
            {
                //        _sound.updateCD(); // Loop CD Audio if needed
                ParseEvents();

                if (_townsScreen != null)
                    _townsScreen.Update();

                _gfxManager.UpdateScreen();
                if (Environment.TickCount >= start_time + msec_delay)
                    break;
                ServiceLocator.Platform.Sleep(10);
            }
        }

        /**
 * The following structs define four basic fades/transitions used by
 * transitionEffect(), each looking differently to the user.
 * Note that the stripTables contain strip numbers, and they assume
 * that the screen has 40 vertical strips (i.e. 320 pixel), and 25 horizontal
 * strips (i.e. 200 pixel). There is a hack in transitionEffect that
 * makes it work correctly in games which have a different screen height
 * (for example, 240 pixel), but nothing is done regarding the width, so this
 * code won't work correctly in COMI. Also, the number of iteration depends
 * on min(vertStrips, horizStrips}. So the 13 is derived from 25/2, rounded up.
 * And the 25 = min(25,40). Hence for Zak256 instead of 13 and 25, the values
 * 15 and 30 should be used, and for COMI probably 30 and 60.
 */
        struct TransitionEffect
        {
            public byte numOfIterations;
            public sbyte[] deltaTable;
            // four times l / t / r / b
            public byte[] stripTable;
            // ditto
        }

        static readonly TransitionEffect[] transitionEffects =
            {
                // Iris effect (looks like an opening/closing camera iris)
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 13,
                    deltaTable = new sbyte[]
                    {
                        1,  1, -1,  1,
                        -1,  1, -1, -1,
                        1, -1, -1, -1,
                        1,  1,  1, -1
                    },
                    stripTable = new byte[]
                    {
                        0,  0, 39,  0,
                        39,  0, 39, 24,
                        0, 24, 39, 24,
                        0,  0,  0, 24
                    }
                },

                // Box wipe (a box expands from the upper-left corner to the lower-right corner)
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 25,     // Number of iterations
                    deltaTable = new sbyte[]
                    {
                        0,  1,  2,  1,
                        2,  0,  2,  1,
                        2,  0,  2,  1,
                        0,  0,  0,  0
                    },
                    stripTable = new byte[]
                    {
                        0,  0,  0,  0,
                        0,  0,  0,  0,
                        1,  0,  1,  0,
                        255,  0,  0,  0
                    }
                },

                // Box wipe (a box expands from the lower-right corner to the upper-left corner)
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 25,     // Number of iterations
                    deltaTable = new sbyte[]
                    {
                        -2, -1,  0, -1,
                        -2, -1, -2,  0,
                        -2, -1, -2,  0,
                        0,  0,  0,  0
                    },
                    stripTable = new byte[]
                    {
                        39, 24, 39, 24,
                        39, 24, 39, 24,
                        38, 24, 38, 24,
                        255,  0,  0,  0
                    }
                },

                // Inverse box wipe
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 25,     // Number of iterations
                    deltaTable = new sbyte[]
                    {
                        0, -1, -2, -1,
                        -2,  0, -2, -1,
                        -2,  0, -2, -1,
                        0,  0,  0,  0
                    },
                    stripTable = new byte[]
                    {
                        0, 24, 39, 24,
                        39,  0, 39, 24,
                        38,  0, 38, 24,
                        255,  0,  0,  0
                    }
                },

                // Inverse iris effect, specially tailored for V1/V2 games
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 9,      // Number of iterations
                    deltaTable = new sbyte[]
                    {
                        -1, -1,  1, -1,
                        -1,  1,  1,  1,
                        -1, -1, -1,  1,
                        1, -1,  1,  1
                    },
                    stripTable = new byte[]
                    {
                        7, 7, 32, 7,
                        7, 8, 32, 8,
                        7, 8,  7, 8,
                        32, 7, 32, 8
                    }
                },

                // Horizontal wipe (a box expands from left to right side). For MM NES
                new ScummEngine.TransitionEffect
                {
                    numOfIterations = 16,     // Number of iterations
                    deltaTable = new sbyte[]
                    {
                        2,  0,  2,  0,
                        2,  0,  2,  0,
                        0,  0,  0,  0,
                        0,  0,  0,  0
                    },
                    stripTable = new byte[]
                    {
                        0, 0,  0,  15,
                        1, 0,  1,  15,
                        255, 0,  0,  0,
                        255, 0,  0,  0
                    }
                }

            };

    }
}

