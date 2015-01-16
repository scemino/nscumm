//
//  ScummEngine6_Camera.cs
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0x78)]
        protected void PanCameraTo(int x)
        {
            // TODO: scumm7
//            if (Game.Version >= 7)
//            {
//                var y = Pop();
//                PanCameraToCore(x, y);
//            }
//            else
            {
                PanCameraToCore(x);
            }
        }

        [OpCode(0x79)]
        protected void ActorFollowCamera(int index)
        {
            if (Game.Version >= 7)
                SetCameraFollows(Actors[index]);
            else
                ActorFollowCameraEx(index);
        }

        [OpCode(0x7a)]
        protected void SetCameraAt(int x)
        {
            // TODO:scumm7
//            if (Game.Version >= 7) {
//
//                    Camera.Follows = 0;
//                    Variables[VARiableCAMERAFOLLOWEDACTOR] = 0;
//
//                    y = pop();
//
//                                SetCameraAt(new NScumm.Core.Graphics.Point(x, y));
//            } else {
            SetCameraAtEx(x);
//                }
        }

        void ActorFollowCameraEx(int act)
        {
            if (Game.Version < 7)
            {
                var old = Camera.ActorToFollow;
                SetCameraFollows(Actors[act]);
                if (Camera.ActorToFollow != old)
                    RunInventoryScript(0);

                Camera.MovingToActor = false;
            }
        }

        void SetCameraAtEx(int at)
        {
            if (Game.Version < 7)
            {
                Camera.Mode = CameraMode.Normal;
                Camera.CurrentPosition.X = (short)at;
                SetCameraAt(new NScumm.Core.Graphics.Point((short)at, 0));
                Camera.MovingToActor = false;
            }
        }
    }
}

