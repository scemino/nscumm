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

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0x78)]
        protected override void PanCameraTo()
        {
            var y = (Game.Version >= 7) ? Pop() : 0;
            var x = Pop();
            PanCameraToCore(new Core.Graphics.Point((short)x, (short)y));
        }

        [OpCode(0x79)]
        protected override void ActorFollowCamera(int index)
        {
            if (Game.Version >= 7)
                SetCameraFollows(Actors[index]);
            else
                ActorFollowCameraEx(index);
        }

        [OpCode(0x7a)]
        protected override void SetCameraAt()
        {
            if (Game.Version >= 7)
            {
                Camera.ActorToFollow = 0;
                Variables[VariableCameraFollowedActor.Value] = 0;

                var y = Pop();
                var x = Pop();
                SetCameraAt(new Core.Graphics.Point((short)x, (short)y));
            }
            else
            {
                var x = Pop();
                SetCameraAtEx(x);
            }
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
    }
}

