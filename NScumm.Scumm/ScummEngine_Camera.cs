//
//  ScummEngine_Camera.cs
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

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        readonly Camera _camera = new Camera();

        internal Camera Camera { get { return _camera; } }

        protected virtual void PanCameraToCore(Point pos)
        {
            _camera.DestinationPosition.X = pos.X;
            _camera.Mode = CameraMode.Panning;
            _camera.MovingToActor = false;
        }

        protected virtual void SetCameraAt(Point pos)
        {
            if (_camera.Mode != CameraMode.FollowActor || Math.Abs(pos.X - _camera.CurrentPosition.X) > (ScreenWidth / 2))
            {
                _camera.CurrentPosition.X = pos.X;
            }
            _camera.DestinationPosition.X = pos.X;

            if (VariableCameraMinX.HasValue && _camera.CurrentPosition.X < _variables[VariableCameraMinX.Value])
                _camera.CurrentPosition.X = _variables[VariableCameraMinX.Value];

            if (VariableCameraMaxX.HasValue && _camera.CurrentPosition.X > _variables[VariableCameraMaxX.Value])
                _camera.CurrentPosition.X = _variables[VariableCameraMaxX.Value];

            if (VariableScrollScript.HasValue && _variables[VariableScrollScript.Value] != 0)
            {
                _variables[VariableCameraPosX.Value] = _camera.CurrentPosition.X;
                RunScript(_variables[VariableScrollScript.Value], false, false, new int[0]);
            }

            // If the camera moved and text is visible, remove it
            if (_camera.CurrentPosition.X != _camera.LastPosition.X && _charset.HasMask)
                StopTalk();
        }

        internal virtual void SetCameraFollows(Actor actor, bool setCamera = false)
        {
            _camera.Mode = CameraMode.FollowActor;
            _camera.ActorToFollow = actor.Number;

            if (!actor.IsInCurrentRoom)
            {
                StartScene(actor.Room);
                _camera.Mode = CameraMode.FollowActor;
                _camera.CurrentPosition.X = actor.Position.X;
                SetCameraAt(new Point(_camera.CurrentPosition.X, 0));
            }

            int t = actor.Position.X / 8 - _screenStartStrip;

            if (t < _camera.LeftTrigger || t > _camera.RightTrigger || setCamera)
                SetCameraAt(new Point(actor.Position.X, 0));

            for (int i = 1; i < Actors.Length; i++)
            {
                if (Actors[i].IsInCurrentRoom)
                    Actors[i].NeedRedraw = true;
            }
            RunInventoryScript(0);
        }

        protected virtual void MoveCamera()
        {
            int pos = _camera.CurrentPosition.X;
            int t;
            Actor a = null;
            bool snapToX = /*_snapScroll ||*/ VariableCameraFastX.HasValue && _variables[VariableCameraFastX.Value] != 0;

            _camera.CurrentPosition.X = (_camera.CurrentPosition.X & 0xFFF8);

            if (VariableCameraMinX.HasValue && _camera.CurrentPosition.X < _variables[VariableCameraMinX.Value])
            {
                if (snapToX)
                    _camera.CurrentPosition.X = _variables[VariableCameraMinX.Value];
                else
                    _camera.CurrentPosition.X += 8;

                CameraMoved();
                return;
            }

            if (VariableCameraMaxX.HasValue && _camera.CurrentPosition.X > _variables[VariableCameraMaxX.Value])
            {
                if (snapToX)
                    _camera.CurrentPosition.X = _variables[VariableCameraMaxX.Value];
                else
                    _camera.CurrentPosition.X -= 8;

                CameraMoved();
                return;
            }

            if (_camera.Mode == CameraMode.FollowActor)
            {
                a = Actors[_camera.ActorToFollow];

                int actorx = a.Position.X;
                t = actorx / 8 - _screenStartStrip;

                if (t < _camera.LeftTrigger || t > _camera.RightTrigger)
                {
                    if (snapToX)
                    {
                        if (t > 40 - 5)
                            _camera.DestinationPosition.X = (actorx + 80);
                        if (t < 5)
                            _camera.DestinationPosition.X = (actorx - 80);
                    }
                    else
                        _camera.MovingToActor = true;
                }
            }

            if (_camera.MovingToActor)
            {
                a = Actors[_camera.ActorToFollow];
                _camera.DestinationPosition.X = a.Position.X;
            }

            if (VariableCameraMinX.HasValue && _camera.DestinationPosition.X < _variables[VariableCameraMinX.Value])
                _camera.DestinationPosition.X = _variables[VariableCameraMinX.Value];

            if (VariableCameraMaxX.HasValue && _camera.DestinationPosition.X > _variables[VariableCameraMaxX.Value])
                _camera.DestinationPosition.X = _variables[VariableCameraMaxX.Value];

            if (snapToX)
            {
                _camera.CurrentPosition.X = _camera.DestinationPosition.X;
            }
            else
            {
                if (_camera.CurrentPosition.X < _camera.DestinationPosition.X)
                    _camera.CurrentPosition.X += 8;
                if (_camera.CurrentPosition.X > _camera.DestinationPosition.X)
                    _camera.CurrentPosition.X -= 8;
            }

            /* Actor 'a' is set a bit above */
            if (_camera.MovingToActor && (_camera.CurrentPosition.X / 8) == (a.Position.X / 8))
            {
                _camera.MovingToActor = false;
            }

            CameraMoved();

            if (VariableScrollScript.HasValue && _variables[VariableScrollScript.Value] != 0 && pos != _camera.CurrentPosition.X)
            {
                _variables[VariableCameraPosX.Value] = _camera.CurrentPosition.X;
                RunScript(_variables[VariableScrollScript.Value], false, false, new int[0]);
            }
        }

        protected void CameraMoved()
        {
            int screenLeft;

            if (_camera.CurrentPosition.X < (ScreenWidth / 2))
            {
                _camera.CurrentPosition.X = (ScreenWidth / 2);
            }
            else if (_camera.CurrentPosition.X > (CurrentRoomData.Header.Width - (ScreenWidth / 2)))
            {
                _camera.CurrentPosition.X = (CurrentRoomData.Header.Width - (ScreenWidth / 2));
            }

            _screenStartStrip = _camera.CurrentPosition.X / 8 - Gdi.NumStrips / 2;
            _screenEndStrip = _screenStartStrip + Gdi.NumStrips - 1;

            ScreenTop = _camera.CurrentPosition.Y - (ScreenHeight / 2);
            screenLeft = _screenStartStrip * 8;

            _mainVirtScreen.XStart = (ushort)screenLeft;
        }
    }
}

