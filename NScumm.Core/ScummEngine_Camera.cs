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

using NScumm.Core.Graphics;
using System;

namespace NScumm.Core
{
	partial class ScummEngine
	{
		readonly Camera _camera = new Camera ();

		void PanCameraTo ()
		{
			PanCameraTo (GetVarOrDirectWord (OpCodeParameter.Param1));
		}

		void SetCameraAt ()
		{
			short at = (short)GetVarOrDirectWord (OpCodeParameter.Param1);
			_camera.Mode = CameraMode.Normal;
			_camera.CurrentPosition.X = at;
			SetCameraAt (new Point (at, 0));
			_camera.MovingToActor = false;
		}

		void PanCameraTo (int x)
		{
			_camera.DestinationPosition.X = (short)x;
			_camera.Mode = CameraMode.Panning;
			_camera.MovingToActor = false;
		}

		void SetCameraAt (Point pos)
		{
			if (_camera.Mode != CameraMode.FollowActor || Math.Abs (pos.X - _camera.CurrentPosition.X) > (ScreenWidth / 2)) {
				_camera.CurrentPosition.X = pos.X;
			}
			_camera.DestinationPosition.X = pos.X;

			if (_camera.CurrentPosition.X < _variables [VariableCameraMinX])
				_camera.CurrentPosition.X = (short)_variables [VariableCameraMinX];

			if (_camera.CurrentPosition.X > _variables [VariableCameraMaxX])
				_camera.CurrentPosition.X = (short)_variables [VariableCameraMaxX];

			if (_variables [VariableScrollScript] != 0) {
				_variables [VariableCameraPosX] = _camera.CurrentPosition.X;
				RunScript ((byte)_variables [VariableScrollScript], false, false, new int[0]);
			}

			// If the camera moved and text is visible, remove it
			if (_camera.CurrentPosition.X != _camera.LastPosition.X && _charset.HasMask)
				StopTalk ();
		}

		void ActorFollowCamera ()
		{
			var actor = GetVarOrDirectByte (OpCodeParameter.Param1);
			var old = _camera.ActorToFollow;
			SetCameraFollows (_actors [actor], false);

			if (_camera.ActorToFollow != old)
				RunInventoryScript (0);

			_camera.MovingToActor = false;
		}

		void SetCameraFollows (Actor actor, bool setCamera)
		{
			_camera.Mode = CameraMode.FollowActor;
			_camera.ActorToFollow = actor.Number;

			if (!actor.IsInCurrentRoom) {
				StartScene (actor.Room);
				_camera.Mode = CameraMode.FollowActor;
				_camera.CurrentPosition.X = actor.Position.X;
				SetCameraAt (new Point (_camera.CurrentPosition.X, 0));
			}

			int t = actor.Position.X / 8 - _screenStartStrip;

			if (t < _camera.LeftTrigger || t > _camera.RightTrigger || setCamera)
				SetCameraAt (new Point (actor.Position.X, 0));

			for (int i = 1; i < _actors.Length; i++) {
				if (_actors [i].IsInCurrentRoom)
					_actors [i].NeedRedraw = true;
			}
			RunInventoryScript (0);
		}

		void MoveCamera ()
		{
			int pos = _camera.CurrentPosition.X;
			int t;
			Actor a = null;
			bool snapToX = /*_snapScroll ||*/ _variables [VariableCameraFastX] != 0;

			_camera.CurrentPosition.X = (short)(_camera.CurrentPosition.X & 0xFFF8);

			if (_camera.CurrentPosition.X < _variables [VariableCameraMinX]) {
				if (snapToX)
					_camera.CurrentPosition.X = (short)_variables [VariableCameraMinX];
				else
					_camera.CurrentPosition.X += 8;

				CameraMoved ();
				return;
			}

			if (_camera.CurrentPosition.X > _variables [VariableCameraMaxX]) {
				if (snapToX)
					_camera.CurrentPosition.X = (short)_variables [VariableCameraMaxX];
				else
					_camera.CurrentPosition.X -= 8;

				CameraMoved ();
				return;
			}

			if (_camera.Mode == CameraMode.FollowActor) {
				a = _actors [_camera.ActorToFollow];

				int actorx = a.Position.X;
				t = actorx / 8 - _screenStartStrip;

				if (t < _camera.LeftTrigger || t > _camera.RightTrigger) {
					if (snapToX) {
						if (t > 40 - 5)
							_camera.DestinationPosition.X = (short)(actorx + 80);
						if (t < 5)
							_camera.DestinationPosition.X = (short)(actorx - 80);
					} else
						_camera.MovingToActor = true;
				}
			}

			if (_camera.MovingToActor) {
				a = _actors [_camera.ActorToFollow];
				_camera.DestinationPosition.X = a.Position.X;
			}

			if (_camera.DestinationPosition.X < _variables [VariableCameraMinX])
				_camera.DestinationPosition.X = (short)_variables [VariableCameraMinX];

			if (_camera.DestinationPosition.X > _variables [VariableCameraMaxX])
				_camera.DestinationPosition.X = (short)_variables [VariableCameraMaxX];

			if (snapToX) {
				_camera.CurrentPosition.X = _camera.DestinationPosition.X;
			} else {
				if (_camera.CurrentPosition.X < _camera.DestinationPosition.X)
					_camera.CurrentPosition.X += 8;
				if (_camera.CurrentPosition.X > _camera.DestinationPosition.X)
					_camera.CurrentPosition.X -= 8;
			}

			/* Actor 'a' is set a bit above */
			if (_camera.MovingToActor && (_camera.CurrentPosition.X / 8) == (a.Position.X / 8)) {
				_camera.MovingToActor = false;
			}

			CameraMoved ();

			if (_variables [VariableScrollScript] != 0 && pos != _camera.CurrentPosition.X) {
				_variables [VariableCameraPosX] = _camera.CurrentPosition.X;
				RunScript ((byte)_variables [VariableScrollScript], false, false, new int[0]);
			}
		}

		void CameraMoved ()
		{
			int screenLeft;

			if (_camera.CurrentPosition.X < (ScreenWidth / 2)) {
				_camera.CurrentPosition.X = (short)(ScreenWidth / 2);
			} else if (_camera.CurrentPosition.X > (CurrentRoomData.Header.Width - (ScreenWidth / 2))) {
				_camera.CurrentPosition.X = (short)(CurrentRoomData.Header.Width - (ScreenWidth / 2));
			}

			_screenStartStrip = _camera.CurrentPosition.X / 8 - Gdi.NumStrips / 2;
			_screenEndStrip = _screenStartStrip + Gdi.NumStrips - 1;

			ScreenTop = _camera.CurrentPosition.Y - (ScreenHeight / 2);
			screenLeft = _screenStartStrip * 8;

			_mainVirtScreen.XStart = (ushort)screenLeft;
		}
	}
}

