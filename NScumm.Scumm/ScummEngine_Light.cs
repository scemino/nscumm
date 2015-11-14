//
//  ScummEngine_Light.cs
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
using System.Diagnostics;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    [Flags]
    enum LightModes
    {
        /**
         * Lighting flag that indicates whether the normal palette, or the 'dark'
         * palette shall be used to draw actors.
         * Apparantly only used in very old games (so far only NESCostumeRenderer
         * checks it).
         */
        ActorUseBasePalette = 1,
        /**
         * Lighting flag that indicates whether the room is currently lit. Normally
         * always on. Used for rooms in which the light can be switched "off".
         */
        RoomLightsOn = 2,
        /**
         * Lighting flag that indicates whether a flashlight like device is active.
         * Used in Loom (flashlight follows the actor) and Indy 3 (flashlight
         * follows the mouse). Only has any effect if the room lights are off.
         */
        FlashlightOn = 4,
        /**
         * Lighting flag that indicates whether actors are to be drawn with their
         * own custom palette, or using a fixed 'dark' palette. This is the
         * modern successor of LIGHTMODE_actor_use_base_palette.
         * Note: It is tempting to 'merge' these two flags, but since flags can
         * check their values, this is probably not a good idea.
         */
        ActorUseColors = 8
    }

    partial class ScummEngine
    {
        protected FlashLight _flashlight;

        internal virtual LightModes GetCurrentLights()
        {
            if (Game.Version >= 6)
                return LightModes.RoomLightsOn | LightModes.ActorUseColors;
            else
                return (LightModes)_variables[VariableCurrentLights.Value];
        }

        internal bool IsLightOn()
        {
            return GetCurrentLights().HasFlag(LightModes.RoomLightsOn);
        }

        void DrawFlashlight()
        {
            int i, j, x, y;
            var vs = MainVirtScreen;

            // Remove the flash light first if it was previously drawn
            if (_flashlight.IsDrawn)
            {
                MarkRectAsDirty(MainVirtScreen, _flashlight.X, _flashlight.X + _flashlight.W,
                    _flashlight.Y, _flashlight.Y + _flashlight.H, Gdi.UsageBitDirty);

                if (_flashlight.PixelNavigator.HasValue)
                {
                    Gdi.Fill(_flashlight.PixelNavigator.Value, 0, _flashlight.W, _flashlight.H);
                }
                _flashlight.IsDrawn = false;
            }

            if (_flashlight.XStrips == 0 || _flashlight.YStrips == 0)
                return;

            // Calculate the area of the flashlight
            if (Game.GameId == GameId.Zak || Game.GameId == GameId.Maniac)
            {
                x = _mousePos.X + vs.XStart;
                y = _mousePos.Y - vs.TopLine;
            }
            else
            {
                var a = Actors[Variables[VariableEgo.Value]];
                x = a.Position.X;
                y = a.Position.Y;
            }
            _flashlight.W = _flashlight.XStrips * 8;
            _flashlight.H = _flashlight.YStrips * 8;
            _flashlight.X = x - _flashlight.W / 2 - _screenStartStrip * 8;
            _flashlight.Y = y - _flashlight.H / 2;

            if (Game.GameId == GameId.Loom)
                _flashlight.Y -= 12;

            // Clip the flashlight at the borders
            if (_flashlight.X < 0)
                _flashlight.X = 0;
            else if (_flashlight.X + _flashlight.W > Gdi.NumStrips * 8)
                _flashlight.X = Gdi.NumStrips * 8 - _flashlight.W;
            if (_flashlight.Y < 0)
                _flashlight.Y = 0;
            else if (_flashlight.Y + _flashlight.H > vs.Height)
                _flashlight.Y = vs.Height - _flashlight.H;

            // Redraw any actors "under" the flashlight
            for (i = _flashlight.X / 8; i < (_flashlight.X + _flashlight.W) / 8; i++)
            {
                Debug.Assert(0 <= i && i < Gdi.NumStrips);
                Gdi.SetGfxUsageBit(_screenStartStrip + i, Gdi.UsageBitDirty);
                vs.TDirty[i] = 0;
                vs.BDirty[i] = vs.Height;
            }

            var pn = new PixelNavigator(vs.Surfaces[0]);
            pn.GoTo(_flashlight.X + vs.XStart, _flashlight.Y);
            pn = new PixelNavigator(pn);
            _flashlight.PixelNavigator = pn;
            var bgbak = new PixelNavigator(vs.Surfaces[1]);
            bgbak.GoTo(_flashlight.X + vs.XStart, _flashlight.Y);

            Gdi.Blit(pn, bgbak, _flashlight.W, _flashlight.H);

            // Round the corners. To do so, we simply hard-code a set of nicely
            // rounded corners.
            var corner_data = new [] { 8, 6, 4, 3, 2, 2, 1, 1 };
            int minrow = 0;
            int maxcol = (_flashlight.W - 1);
            int maxrow = (_flashlight.H - 1);

            for (i = 0; i < 8; i++, minrow++, maxrow--)
            {
                int d = corner_data[i];

                for (j = 0; j < d; j++)
                {
                    if (vs.BytesPerPixel == 2)
                    {
                        pn.GoTo(j, minrow);
                        pn.WriteUInt16(0);
                        pn.GoTo(maxcol - j, minrow);
                        pn.WriteUInt16(0);
                        pn.GoTo(j, maxrow);
                        pn.WriteUInt16(0);
                        pn.GoTo(maxcol - j, maxrow);
                        pn.WriteUInt16(0);
                    }
                    else
                    {
                        pn.GoTo(j, minrow);
                        pn.Write(0);
                        pn.GoTo(maxcol - j, minrow);
                        pn.Write(0);
                        pn.GoTo(j, maxrow);
                        pn.Write(0);
                        pn.GoTo(maxcol - j, maxrow);
                        pn.Write(0);
                    }
                }
            }

            _flashlight.IsDrawn = true;
        }

        void ClearFlashlight()
        {
            _flashlight.IsDrawn = false;
            _flashlight.PixelNavigator = null;
        }
    }
}

