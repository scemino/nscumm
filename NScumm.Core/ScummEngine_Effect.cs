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

namespace NScumm.Core
{
    partial class ScummEngine
    {
        bool _shakeEnabled;
        int _shakeFrame;
        static readonly int[] ShakePositions = { 0, 1 * 2, 2 * 2, 1 * 2, 0 * 2, 2 * 2, 3 * 2, 1 * 2 };
        byte _newEffect = 129, _switchRoomEffect2, _switchRoomEffect;
        bool _disableFadeInEffect;
        bool _doEffect;
        bool _screenEffectFlag;

        void FadeIn(byte effect)
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
//                    MainVirtScreen.SetDirtyRange(0, 0);
                    MainVirtScreen.SetDirtyRange(0, MainVirtScreen.Height);
                    UpdateDirtyScreen(MainVirtScreen);
				//TransitionEffect(effect - 1);
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
                    throw new NotImplementedException(string.Format("Unknown screen effect {0}", effect));
            }
            _screenEffectFlag = true;
        }

        void ScrollEffect(int dir)
        {
            // TODO: ScrollEffect
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
        void DissolveEffect(int width, int height)
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

            // When used correctly, vs->width % width and vs->height % height
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
            if (_game.Id == "loom")
                blits_before_refresh *= 2;

            for (i = 0; i < w * h; i++)
            {
                x = offsets[i] % vs.Pitch;
                y = offsets[i] / vs.Pitch;

                _gfxManager.CopyRectToScreen(vs.Surfaces[0].Pixels, vs.Pitch,
                    x, y, width, height);


                if (++blits >= blits_before_refresh)
                {
                    blits = 0;
                    System.Threading.Thread.Sleep(30);
                }
            }

            if (blits != 0)
            {
                System.Threading.Thread.Sleep(30);
            }
        }

        void UnkScreenEffect6()
        {
            DissolveEffect(8, 4);
        }

        void FadeOut(int effect)
        {
            _mainVirtScreen.SetDirtyRange(0, 0);

            _camera.LastPosition.X = _camera.CurrentPosition.X;

            if (_screenEffectFlag && effect != 0)
            {
                // Fill screen 0 with black
                var l_pixNav = new PixelNavigator(_mainVirtScreen.Surfaces[0]);
                l_pixNav.OffsetX(_mainVirtScreen.XStart);
                Gdi.Fill(l_pixNav, 0, _mainVirtScreen.Width, _mainVirtScreen.Height);

                // Fade to black with the specified effect, if any.
                switch (effect)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
					//    transitionEffect(effect - 1);
                        Console.Error.WriteLine(string.Format("fadeOut: case {0} not implemented", effect));
                        break;
                    case 128:
                        UnkScreenEffect6();
                        break;

                    case 129:
					// Just blit screen 0 to the display (i.e. display will be black)
                        _mainVirtScreen.SetDirtyRange(0, _mainVirtScreen.Height);
                        UpdateDirtyScreen(_mainVirtScreen);
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

        void SetShake(bool enabled)
        {
            if (_shakeEnabled != enabled)
                _fullRedraw = true;

            _shakeEnabled = enabled;
            _shakeFrame = 0;
            _gfxManager.SetShakePos(0);
        }

        void StopCycle(int i)
        {
            ScummHelper.AssertRange(0, i, 16, "stopCycle: cycle");
            if (i != 0)
            {
                _colorCycle[i - 1].Delay = 0;
                return;
            }

            for (i = 0; i < 16; i++)
            {
                var cycl = _colorCycle[i];
                cycl.Delay = 0;
            }
        }

        void HandleEffects()
        {
            if (Game.Version >= 4)
            {
                CyclePalette();
            }
            //PalManipulate();
            if (_doEffect)
            {
                _doEffect = false;
                FadeIn(_newEffect);
                // TODO:
                //clearClickedStatus();
            }
        }
    }
}

