//
//  ScummEngine_Actor.cs
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
using System.Linq;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
	partial class ScummEngine
	{
		Actor[] _actors = new Actor[13];
		int _actorToPrintStrFor;
		bool _useTalkAnims;
		bool _haveActorSpeechMsg;

		void InitActors ()
		{
			for (byte i = 0; i < _actors.Length; i++) {
				_actors [i] = new Actor (this, i);
				_actors [i].Init (-1);
			}
		}

		void GetActorCostume ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			Actor a = _actors [act];
			SetResult (a.Costume);
		}

		void GetActorMoving ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			Actor a = _actors [act];
			SetResult ((int)a.Moving);
		}

		void PutActorAtObject ()
		{
			int obj;
			Point p;
			Actor a = _actors [GetVarOrDirectByte (OpCodeParameter.Param1)];
			obj = GetVarOrDirectWord (OpCodeParameter.Param2);
			if (GetWhereIsObject (obj) != WhereIsObject.NotFound) {
				p = GetObjectXYPos (obj);
			} else {
				p = new Point (240, 120);
			}
			a.PutActor (p);
		}

		void WalkActorToActor ()
		{
			int x, y;
			int nr = GetVarOrDirectByte (OpCodeParameter.Param1);
			int nr2 = GetVarOrDirectByte (OpCodeParameter.Param2);
			int dist = ReadByte ();

			var a = _actors [nr];
			if (!a.IsInCurrentRoom)
				return;

			var a2 = _actors [nr2];
			if (!a2.IsInCurrentRoom)
				return;

			if (dist == 0xFF) {
				dist = (int)(a.ScaleX * a.Width / 0xFF);
				dist += (int)(a2.ScaleX * a2.Width / 0xFF) / 2;
			}
			x = a2.Position.X;
			y = a2.Position.Y;
			if (x < a.Position.X)
				x += dist;
			else
				x -= dist;

			a.StartWalk (new Point ((short)x, (short)y), -1);
		}

		void GetActorX ()
		{
			GetResult ();
			int a = GetVarOrDirectWord (OpCodeParameter.Param1);
			SetResult (GetObjX (a));
		}

		void GetActorY ()
		{
			GetResult ();
			int a = GetVarOrDirectWord (OpCodeParameter.Param1);
			SetResult (GetObjY (a));
		}

		void ActorFromPosition ()
		{
			GetResult ();
			var x = (short)GetVarOrDirectWord (OpCodeParameter.Param1);
			var y = (short)GetVarOrDirectWord (OpCodeParameter.Param2);
			var actor = GetActorFromPos (new Point (x, y));
			SetResult (actor);
		}

		void GetActorWalkBox ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			Actor a = _actors [act];
			SetResult (a.Walkbox);
		}

		void WalkActorTo ()
		{
			var a = _actors [GetVarOrDirectByte (OpCodeParameter.Param1)];
			var x = (short)GetVarOrDirectWord (OpCodeParameter.Param2);
			var y = (short)GetVarOrDirectWord (OpCodeParameter.Param3);
			a.StartWalk (new Point (x, y), -1);
		}

		void WalkActorToObject ()
		{
			var a = _actors [GetVarOrDirectByte (OpCodeParameter.Param1)];
			var obj = GetVarOrDirectWord (OpCodeParameter.Param2);
			if (GetWhereIsObject (obj) != WhereIsObject.NotFound) {
				int dir;
				Point p;
				GetObjectXYPos (obj, out p, out dir);
				a.StartWalk (p, dir);
			}
		}

		void FaceActor ()
		{
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			int obj = GetVarOrDirectWord (OpCodeParameter.Param2);
			var a = _actors [act];
			a.FaceToObject (obj);
		}

		void PutActor ()
		{
			Actor a = _actors [GetVarOrDirectByte (OpCodeParameter.Param1)];
			short x = (short)GetVarOrDirectWord (OpCodeParameter.Param2);
			short y = (short)GetVarOrDirectWord (OpCodeParameter.Param3);
			a.PutActor (new Point (x, y));
		}

		void AnimateActor ()
		{
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			int anim = GetVarOrDirectByte (OpCodeParameter.Param2);

			Actor a = _actors [act];
			a.Animate (anim);
		}

		void ActorOps ()
		{
			var convertTable = new byte[20] { 1, 0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20 };
			var act = GetVarOrDirectByte (OpCodeParameter.Param1);
			var a = _actors [act];
			int i, j;

			while ((_opCode = ReadByte ()) != 0xFF) {
				_opCode = (byte)((_opCode & 0xE0) | convertTable [(_opCode & 0x1F) - 1]);
				switch (_opCode & 0x1F) {
				case 0:                                     /* dummy case */
					GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 1:         // SO_COSTUME
					var cost = (ushort)GetVarOrDirectByte (OpCodeParameter.Param1);
					a.SetActorCostume (cost);
					break;

				case 2:         // SO_STEP_DIST
					i = GetVarOrDirectByte (OpCodeParameter.Param1);
					j = GetVarOrDirectByte (OpCodeParameter.Param2);
					a.SetActorWalkSpeed ((uint)i, (uint)j);
					break;

				case 3:         // SO_SOUND
					a.Sound [0] = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 4:         // SO_WALK_ANIMATION
					a.WalkFrame = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 5:         // SO_TALK_ANIMATION
					a.TalkStartFrame = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					a.TalkStopFrame = (byte)GetVarOrDirectByte (OpCodeParameter.Param2);
					break;

				case 6:         // SO_STAND_ANIMATION
					a.StandFrame = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 7:         // SO_ANIMATION
					GetVarOrDirectByte (OpCodeParameter.Param1);
					GetVarOrDirectByte (OpCodeParameter.Param2);
					GetVarOrDirectByte (OpCodeParameter.Param3);
					break;

				case 8:         // SO_DEFAULT
					a.Init (0);
					break;

				case 9:         // SO_ELEVATION
					a.Elevation = GetVarOrDirectWord (OpCodeParameter.Param1);
					break;

				case 10:        // SO_ANIMATION_DEFAULT
					a.ResetFrames ();
					break;

				case 11:        // SO_PALETTE
					i = GetVarOrDirectByte (OpCodeParameter.Param1);
					j = GetVarOrDirectByte (OpCodeParameter.Param2);
					ScummHelper.AssertRange (0, i, 31, "o5_actorOps: palette slot");
					a.SetPalette (i, (ushort)j);
					break;

				case 12:        // SO_TALK_COLOR
					a.TalkColor = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 13:        // SO_ACTOR_NAME
					a.Name = ReadCharacters ();
					break;

				case 14:        // SO_INIT_ANIMATION
					a.InitFrame = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 16:        // SO_ACTOR_WIDTH
					a.Width = (uint)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				case 17:        // SO_ACTOR_SCALE
					i = j = GetVarOrDirectByte (OpCodeParameter.Param1);
					a.BoxScale = (ushort)i;
					a.SetScale (i, j);
					break;

				case 18:        // SO_NEVER_ZCLIP
					a.ForceClip = false;
					break;

				case 19:        // SO_ALWAYS_ZCLIP
					a.ForceClip = GetVarOrDirectByte (OpCodeParameter.Param1) > 0;
					break;

				case 20:        // SO_IGNORE_BOXES
				case 21:        // SO_FOLLOW_BOXES
					a.IgnoreBoxes = (_opCode & 1) == 0;
					a.ForceClip = false;
					if (a.IsInCurrentRoom)
						a.PutActor ();
					break;

				case 22:        // SO_ANIMATION_SPEED
					a.SetAnimSpeed ((byte)GetVarOrDirectByte (OpCodeParameter.Param1));
					break;

				case 23:        // SO_SHADOW
					a.ShadowMode = (byte)GetVarOrDirectByte (OpCodeParameter.Param1);
					break;

				default:
					throw new NotImplementedException ();
				}
			}
		}

		void GetActorFacing ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			var a = _actors [act];
			SetResult (ScummHelper.NewDirToOldDir (a.Facing));
		}

		void GetActorElevation ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			Actor a = _actors [act];
			SetResult (a.Elevation);
		}

		void GetActorRoom ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);

			Actor a = _actors [act];
			SetResult (a.Room);
		}

		void GetActorWidth ()
		{
			GetResult ();
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			Actor a = _actors [act];
			SetResult ((int)a.Width);
		}

		void PutActorInRoom ()
		{
			int act = GetVarOrDirectByte (OpCodeParameter.Param1);
			byte room = (byte)GetVarOrDirectByte (OpCodeParameter.Param2);

			var a = _actors [act];

			if (a.IsVisible && _currentRoom != room && TalkingActor == a.Number) {
				StopTalk ();
			}
			a.Room = room;
			if (room == 0)
				a.PutActor (new Point (), 0);
		}

		void ActorTalk (byte[] msg)
		{
			ConvertMessageToString (msg, _charsetBuffer, 0);

			// WORKAROUND for bugs #770039 and #770049
			if (_game.Id == "loom") {
				if (_charsetBuffer [0] == 0)
					return;
			}

			if (_actorToPrintStrFor == 0xFF) {
				if (!_keepText) {
					StopTalk ();
				}
				TalkingActor = 0xFF;
			} else {
				int oldact;

				// WORKAROUND bug #770724
				if (_game.Id == "loom" && _roomResource == 23 &&
					_slots [_currentScript].Number == 232 && _actorToPrintStrFor == 0) {
					_actorToPrintStrFor = 2;	// Could be anything from 2 to 5. Maybe compare to original?
				}

				var a = _actors [_actorToPrintStrFor];
				if (!a.IsInCurrentRoom) {
					oldact = 0xFF;
				} else {
					if (!_keepText) {
						StopTalk ();
					}
					TalkingActor = a.Number;

					if (!_string [0].NoTalkAnim) {
						a.RunTalkScript (a.TalkStartFrame);
						_useTalkAnims = true;
					}
					oldact = TalkingActor;
				}
				if (oldact >= 0x80)
					return;
			}

			if (TalkingActor > 0x7F) {
				_charsetColor = _string [0].Color;
			} else {
				var a = _actors [TalkingActor];
				_charsetColor = a.TalkColor;
			}

			_charsetBufPos = 0;
			_talkDelay = 0;
			_haveMsg = 0xFF;
			_variables [VariableHaveMessage] = 0xFF;

			_haveActorSpeechMsg = true;
			Charset ();
		}

		internal int TalkingActor {
			get { return _variables [VariableTalkActor]; }
			set { _variables [VariableTalkActor] = value; }
		}

		internal void StopTalk ()
		{
			//_sound->stopTalkSound();

			_haveMsg = 0;
			_talkDelay = 0;

			var act = TalkingActor;
			if (act != 0 && act < 0x80) {
				var a = _actors [act];
				if (a.IsInCurrentRoom && _useTalkAnims) {
					a.RunTalkScript (a.TalkStopFrame);
					_useTalkAnims = false;
				}
				TalkingActor = 0xFF;
			}

			_keepText = false;
			RestoreCharsetBg ();
		}

		void ShowActors ()
		{
			for (int i = 0; i < _actors.Length; i++) {
				if (_actors [i].IsInCurrentRoom)
					_actors [i].Show ();
			}
		}

		void WalkActors ()
		{
			for (int i = 0; i < _actors.Length; i++) {
				if (_actors [i].IsInCurrentRoom)
					_actors [i].Walk ();
			}
		}

		void ProcessActors ()
		{
			var actors = from actor in _actors
					where actor.IsInCurrentRoom
				orderby actor.Position.Y
				select actor;

			foreach (var actor in actors) {
				if (actor.Costume != 0) {
					actor.DrawCostume ();
					actor.AnimateCostume ();
				}
			}
		}

		void HandleActors ()
		{
			SetActorRedrawFlags ();
			ResetActorBgs ();

			var mode = GetCurrentLights ();
			if (!mode.HasFlag (LightModes.RoomLightsOn) && mode.HasFlag (LightModes.FlashlightOn)) {
				// TODO:
				//drawFlashlight();
				SetActorRedrawFlags ();
			}

			ProcessActors ();
		}

		void ResetActorBgs ()
		{
			for (int i = 0; i < Gdi.NumStrips; i++) {
				int strip = _screenStartStrip + i;
				Gdi.ClearGfxUsageBit (strip, Gdi.UsageBitDirty);
				Gdi.ClearGfxUsageBit (strip, Gdi.UsageBitRestored);
				for (int j = 0; j < _actors.Length; j++) {
					if (Gdi.TestGfxUsageBit (strip, j) &&
						((_actors [j].Top != 0x7fffffff && _actors [j].NeedRedraw) || _actors [j].NeedBackgroundReset)) {
						Gdi.ClearGfxUsageBit (strip, j);
						if ((_actors [j].Bottom - _actors [j].Top) >= 0)
							Gdi.ResetBackground (_actors [j].Top, _actors [j].Bottom, i);
					}
				}
			}

			for (int i = 0; i < _actors.Length; i++) {
				_actors [i].NeedBackgroundReset = false;
			}
		}

		void SetActorRedrawFlags ()
		{
			// Redraw all actors if a full redraw was requested.
			// Also redraw all actors in COMI (see bug #1066329 for details).
			if (_fullRedraw) {
				for (int j = 0; j < _actors.Length; j++) {
					_actors [j].NeedRedraw = true;
				}
			} else {
				for (int i = 0; i < Gdi.NumStrips; i++) {
					int strip = _screenStartStrip + i;
					if (Gdi.TestGfxAnyUsageBits (strip)) {
						for (int j = 1; j < _actors.Length; j++) {
							if (Gdi.TestGfxUsageBit (strip, j) && Gdi.TestGfxOtherUsageBits (strip, j)) {
								_actors [j].NeedRedraw = true;
							}
						}
					}
				}
			}
		}

		int GetActorFromPos (Point p)
		{
			for (int i = 1; i < _actors.Length; i++) {
				if (!GetClass (i, ObjectClass.Untouchable) && p.Y >= _actors [i].Top &&
					p.Y <= _actors [i].Bottom) {
					return i;
				}
			}

			return 0;
		}

		int GetObjActToObjActDist (int a, int b)
		{
			Actor acta = null;
			Actor actb = null;

			if (a < _actors.Length)
				acta = _actors [a];

			if (b < _actors.Length)
				actb = _actors [b];

			if ((acta != null) && (actb != null) && (acta.Room == actb.Room) && (acta.Room != 0) && !acta.IsInCurrentRoom)
				return 0;

			Point pA;
			if (!GetObjectOrActorXY (a, out pA))
				return 0xFF;

			Point pB;
			if (!GetObjectOrActorXY (b, out pB))
				return 0xFF;

			// Perform adjustXYToBeInBox() *only* if the first item is an
			// actor and the second is an object. This used to not check
			// whether the second item is a non-actor, which caused bug
			// #853874).
			if (acta != null && actb == null) {
				var r = acta.AdjustXYToBeInBox (pB);
				pB = r.Position;
			}

			// Now compute the distance between the two points
			return ScummMath.GetDistance (pA, pB);
		}


	}
}

