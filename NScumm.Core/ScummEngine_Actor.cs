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
        protected Actor[] _actors = new Actor[13];
        protected int _actorToPrintStrFor;
        bool _useTalkAnims;
        bool _haveActorSpeechMsg;

        void InitActors()
        {
            for (byte i = 0; i < _actors.Length; i++)
            {
                _actors[i] = _game.Version == 3 ? new Actor3(this, i) : new Actor(this, i);
                _actors[i].Init(-1);
            }
        }

        protected bool IsValidActor(int id)
        {
            return id >= 0 && id < _actors.Length && _actors[id].Number == id;
        }

        protected void ActorTalk(byte[] msg)
        {
            ConvertMessageToString(msg, _charsetBuffer, 0);

            // WORKAROUND for bugs #770039 and #770049
            if (_game.Id == "loom")
            {
                if (_charsetBuffer[0] == 0)
                    return;
            }

            if (_actorToPrintStrFor == 0xFF)
            {
                if (!_keepText)
                {
                    StopTalk();
                }
                TalkingActor = 0xFF;
            }
            else
            {
                int oldact;

                // WORKAROUND bug #770724
                if (_game.Id == "loom" && _roomResource == 23 &&
                    _slots[_currentScript].Number == 232 && _actorToPrintStrFor == 0)
                {
                    _actorToPrintStrFor = 2;    // Could be anything from 2 to 5. Maybe compare to original?
                }

                var a = _actors[_actorToPrintStrFor];
                if (!a.IsInCurrentRoom)
                {
                    oldact = 0xFF;
                }
                else
                {
                    if (!_keepText)
                    {
                        StopTalk();
                    }
                    TalkingActor = a.Number;

                    if (!_string[0].NoTalkAnim)
                    {
                        a.RunTalkScript(a.TalkStartFrame);
                        _useTalkAnims = true;
                    }
                    oldact = TalkingActor;
                }
                if (oldact >= 0x80)
                    return;
            }

            if (TalkingActor > 0x7F)
            {
                _charsetColor = _string[0].Color;
            }
            else
            {
                var a = _actors[TalkingActor];
                _charsetColor = a.TalkColor;
            }

            _charsetBufPos = 0;
            _talkDelay = 0;
            _haveMsg = 0xFF;
            _variables[VariableHaveMessage.Value] = 0xFF;

            _haveActorSpeechMsg = true;
            Charset();
        }

        internal int TalkingActor
        {
            get { return _variables[VariableTalkActor.Value]; }
            set { _variables[VariableTalkActor.Value] = value; }
        }

        internal void StopTalk()
        {
            //_sound.StopTalkSound();

            _haveMsg = 0;
            _talkDelay = 0;

            var act = TalkingActor;
            if (act != 0 && act < 0x80)
            {
                var a = _actors[act];
                if (a.IsInCurrentRoom && _useTalkAnims)
                {
                    a.RunTalkScript(a.TalkStopFrame);
                    _useTalkAnims = false;
                }
                TalkingActor = 0xFF;
            }

            _keepText = false;
            RestoreCharsetBg();
        }

        void ShowActors()
        {
            for (int i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom)
                    _actors[i].Show();
            }
        }

        void WalkActors()
        {
            for (int i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].IsInCurrentRoom)
                    _actors[i].Walk();
            }
        }

        void ProcessActors()
        {
            var actors = from actor in _actors
                                  where actor.IsInCurrentRoom
                                  where (Game.Version != 8 || actor.Layer >= 0)
                                  orderby actor.Position.Y - actor.Layer*2000
                                  select actor;

            foreach (var actor in actors)
            {
                if (actor.Costume != 0)
                {
                    actor.DrawCostume();
                    actor.AnimateCostume();
                }
            }
        }

        void HandleActors()
        {
            SetActorRedrawFlags();
            ResetActorBgs();

            if (Game.Version < 6)
            {
                var mode = GetCurrentLights();
                if (!mode.HasFlag(LightModes.RoomLightsOn) && mode.HasFlag(LightModes.FlashlightOn))
                {
                    // TODO:
                    //drawFlashlight();
                    SetActorRedrawFlags();
                }
            }

            ProcessActors();
        }

        void ResetActorBgs()
        {
            for (int i = 0; i < Gdi.NumStrips; i++)
            {
                int strip = _screenStartStrip + i;
                Gdi.ClearGfxUsageBit(strip, Gdi.UsageBitDirty);
                Gdi.ClearGfxUsageBit(strip, Gdi.UsageBitRestored);
                for (int j = 1; j < _actors.Length; j++)
                {
                    if (Gdi.TestGfxUsageBit(strip, j) &&
                        ((_actors[j].Top != 0x7fffffff && _actors[j].NeedRedraw) || _actors[j].NeedBackgroundReset))
                    {
                        Gdi.ClearGfxUsageBit(strip, j);
                        if ((_actors[j].Bottom - _actors[j].Top) >= 0)
                            Gdi.ResetBackground(_actors[j].Top, _actors[j].Bottom, i);
                    }
                }
            }

            for (int i = 1; i < _actors.Length; i++)
            {
                _actors[i].NeedBackgroundReset = false;
            }
        }

        void SetActorRedrawFlags()
        {
            // Redraw all actors if a full redraw was requested.
            // Also redraw all actors in COMI (see bug #1066329 for details).
            if (_fullRedraw)
            {
                for (int j = 0; j < _actors.Length; j++)
                {
                    _actors[j].NeedRedraw = true;
                }
            }
            else
            {
                for (int i = 0; i < Gdi.NumStrips; i++)
                {
                    int strip = _screenStartStrip + i;
                    if (Gdi.TestGfxAnyUsageBits(strip))
                    {
                        for (int j = 1; j < _actors.Length; j++)
                        {
                            if (Gdi.TestGfxUsageBit(strip, j) && Gdi.TestGfxOtherUsageBits(strip, j))
                            {
                                _actors[j].NeedRedraw = true;
                            }
                        }
                    }
                }
            }
        }

        protected int GetActorFromPos(Point p)
        {
            if (!Gdi.TestGfxAnyUsageBits(p.X / 8))
                return 0;

            for (var i = 1; i < _actors.Length; i++)
            {
                if (Gdi.TestGfxUsageBit(p.X / 8, i) && !GetClass(i, ObjectClass.Untouchable) &&
                    p.Y >= _actors[i].Top && p.Y <= _actors[i].Bottom)
                {
                    return i;
                }
            }

            return 0;
        }

        protected int GetObjActToObjActDist(int a, int b)
        {
            Actor acta = null;
            Actor actb = null;

            if (a < _actors.Length)
                acta = _actors[a];

            if (b < _actors.Length)
                actb = _actors[b];

            if ((acta != null) && (actb != null) && (acta.Room == actb.Room) && (acta.Room != 0) && !acta.IsInCurrentRoom)
                return 0;

            Point pA;
            if (!GetObjectOrActorXY(a, out pA))
                return 0xFF;

            Point pB;
            if (!GetObjectOrActorXY(b, out pB))
                return 0xFF;

            // Perform adjustXYToBeInBox() *only* if the first item is an
            // actor and the second is an object. This used to not check
            // whether the second item is a non-actor, which caused bug
            // #853874).
            if (acta != null && actb == null)
            {
                var r = acta.AdjustXYToBeInBox(pB);
                pB = r.Position;
            }

            // Now compute the distance between the two points
            return ScummMath.GetDistance(pA, pB);
        }


    }
}

