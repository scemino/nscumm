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

using System.Collections.Generic;
using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        internal protected int _actorToPrintStrFor;
        bool _useTalkAnims;
        protected bool _haveActorSpeechMsg;
        static readonly byte[] v0MMActorTalkColor =
            {
                1, 7, 2, 14, 8, 15, 3, 7, 7, 15, 1, 13, 1, 4, 5, 5, 4, 3, 1, 5, 1, 1, 1, 1, 7
            };
        static readonly byte[] v1MMActorTalkColor =
            {
                1, 7, 2, 14, 8, 1, 3, 7, 7, 12, 1, 13, 1, 4, 5, 5, 4, 3, 1, 5, 1, 1, 1, 7, 7
            };

        internal Actor[] Actors { get; private set; }

        void InitActors()
        {
            int numActors;

            if (Game.Version == 8)
            {
                numActors = 80;
            }
            else if (Game.Version == 7 || Game.GameId == GameId.SamNMax)
            {
                numActors = 30;
            }
            else if (Game.GameId == GameId.Maniac)
            {
                numActors = 25;
            }
            else
            {
                numActors = 13;
            }

            Actors = new Actor[numActors];
            for (byte i = 0; i < Actors.Length; i++)
            {
                if (Game.Version == 0)
                {
                    Actors[i] = new Actor0(this, i);
                }
                else if (Game.Version <= 2)
                {
                    Actors[i] = new Actor2(this, i);
                }
                else if (Game.Version == 3)
                {
                    Actors[i] = new Actor3(this, i);
                }
                else
                {
                    Actors[i] = new Actor(this, i);
                }
                Actors[i].Init(-1);

                // this is from IDB
                if ((_game.Version <= 1) || (Game.GameId == GameId.Maniac && (Game.Features.HasFlag(GameFeatures.Demo))))
                    Actors[i].SetActorCostume(i);
            }

            if (Game.GameId == GameId.Maniac && Game.Version <= 1)
            {
                ResetV1ActorTalkColor();
            }
        }

        void ResetV1ActorTalkColor()
        {
            for (var i = 1; i < Actors.Length; i++)
            {
                if (_game.Version == 0)
                {
                    Actors[i].TalkColor = v0MMActorTalkColor[i];
                }
                else
                {
                    Actors[i].TalkColor = v1MMActorTalkColor[i];
                }
            }
        }

        protected bool IsValidActor(int id)
        {
            return id >= 0 && id < Actors.Length && Actors[id].Number == id;
        }

        protected virtual int ActorToObj(int actor)
        {
            return actor;
        }

        protected virtual void ActorTalk(byte[] msg)
        {
            ConvertMessageToString(msg, _charsetBuffer, 0);

            // WORKAROUND for bugs #770039 and #770049
            if (_game.GameId == GameId.Loom)
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
                if (_game.GameId == GameId.Loom && _roomResource == 23 &&
                    _slots[CurrentScript].Number == 232 && _actorToPrintStrFor == 0)
                {
                    _actorToPrintStrFor = 2;    // Could be anything from 2 to 5. Maybe compare to original?
                }

                var a = Actors[_actorToPrintStrFor];
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
                var a = Actors[TalkingActor];
                _charsetColor = a.TalkColor;
            }

            _charsetBufPos = 0;
            _talkDelay = 0;
            _haveMsg = 0xFF;
            _variables[VariableHaveMessage.Value] = 0xFF;

            _haveActorSpeechMsg = true;
            Charset();
        }

        int _v1TalkingActor;

        internal int TalkingActor
        {
            get
            {
                if (_game.GameId == GameId.Maniac && Game.Version <= 1 /*&& !(Game.platform == Platform.NES)*/)
                    return _v1TalkingActor;
                else
                    return _variables[VariableTalkActor.Value];
            }
            set
            { 
                if (_game.GameId == GameId.Maniac && Game.Version <= 1 /*&& !(Game.platform == Platform.NES)*/)
                    _v1TalkingActor = value;
                else
                    _variables[VariableTalkActor.Value] = value;
            }
        }

        internal void StopTalk()
        {
            Sound.StopTalkSound();

            _haveMsg = 0;
            _talkDelay = 0;

            var act = TalkingActor;
            if (act != 0 && act < 0x80)
            {
                var a = Actors[act];
                if ((Game.Version >= 7 && !_string[0].NoTalkAnim) || (Game.Version <= 6 && a.IsInCurrentRoom && _useTalkAnims))
                {
                    a.RunTalkScript(a.TalkStopFrame);
                    _useTalkAnims = false;
                }
                if (Game.Version <= 7)
                {
                    TalkingActor = 0xFF;
                }
            }

            if (Game.GameId == GameId.Dig || _game.GameId == GameId.CurseOfMonkeyIsland)
            {
                TalkingActor = 0;
                Variables[VariableHaveMessage.Value] = 0;
            }

            _keepText = false;
            if (Game.Version >= 7)
            {
                ((ScummEngine7)this).ClearSubtitleQueue();
            }
            else if (_game.Platform == Platform.FMTowns)
            {
                TownsRestoreCharsetBg();
            }
            else
            {
                RestoreCharsetBg();
            }
        }

        void TownsRestoreCharsetBg()
        {
            if (_curStringRect.Left != -1)
            {
                RestoreBackground(_curStringRect, 0);
                _curStringRect.Left = -1;
                _charset.HasMask = false;
                _nextLeft = _string[0].Position.X;
            }

            _nextLeft = _string[0].Position.X;
            _nextTop = _string[0].Position.Y;
        }

        void ShowActors()
        {
            for (int i = 1; i < Actors.Length; i++)
            {
                if (Actors[i].IsInCurrentRoom)
                    Actors[i].Show();
            }
        }

        void WalkActors()
        {
            for (int i = 1; i < Actors.Length; i++)
            {
                if (Actors[i].IsInCurrentRoom)
                    Actors[i].Walk();
            }
        }

        IEnumerable<Actor> GetOrderedActors()
        {
            if (Game.GameId == GameId.SamNMax)
            {
                return from actor in Actors.Skip(1)
                                   where actor.IsInCurrentRoom
                                   where actor.Layer >= 0
                                   orderby actor.Position.Y, actor.Number
                                   select actor;
            }
            else if (Game.Version == 0)
            {
                return from actor in Actors.Skip(1)
                                   where actor.IsInCurrentRoom
                                   orderby actor.Number==19? 0 : actor.Position.Y
                                   select actor;
            }
            return from actor in Actors.Skip(1)
                            where actor.IsInCurrentRoom
                            where (Game.Version != 8 || actor.Layer >= 0)
                            orderby actor.Position.Y - actor.Layer*2000
                            select actor;
        }

        internal protected virtual void ProcessActors()
        {
            var actors = GetOrderedActors();
            foreach (var actor in actors)
            {
                if (_game.Version == 0)
                {
                    // 0x057B
                    var a0 = (Actor0)actor;
                    if ((a0.Speaking & 1) != 0)
                        a0.Speaking ^= 0xFE;

                    // 0x22B5
                    if (a0.MiscFlags.HasFlag(ActorV0MiscFlags.Hide))
                        continue;

                    // Sound
                    if (a0.Moving != MoveFlags.InLeg && CurrentRoom != 1 && CurrentRoom != 44)
                    {
                        if (a0.Cost.SoundPos == 0)
                            a0.Cost.SoundCounter++;

                        // Is this the correct location?
                        // 0x073C
                        if ((a0.Sound & 0x3F) != 0)
                            a0.Cost.SoundPos = (byte)((a0.Cost.SoundPos + 1) % 3);
                    }
                }

                if (actor.Costume != 0)
                {
                    if (Game.Version == 0)
                    {
                        actor.AnimateCostume();
                        actor.DrawCostume();
                    }
                    else
                    {
                        actor.DrawCostume();
                        actor.AnimateCostume();
                    }
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
                    DrawFlashlight();
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
                for (int j = 1; j < Actors.Length; j++)
                {
                    if (Gdi.TestGfxUsageBit(strip, j) &&
                        ((Actors[j].Top != 0x7fffffff && Actors[j].NeedRedraw) || Actors[j].NeedBackgroundReset))
                    {
                        Gdi.ClearGfxUsageBit(strip, j);
                        if ((Actors[j].Bottom - Actors[j].Top) >= 0)
                            Gdi.ResetBackground(Actors[j].Top, Actors[j].Bottom, i);
                    }
                }
            }

            for (int i = 1; i < Actors.Length; i++)
            {
                Actors[i].NeedBackgroundReset = false;
            }
        }

        protected void SetActorRedrawFlags()
        {
            // Redraw all actors if a full redraw was requested.
            // Also redraw all actors in COMI (see bug #1066329 for details).
            if (_fullRedraw || _game.Version == 8)
            {
                for (int j = 1; j < Actors.Length; j++)
                {
                    Actors[j].NeedRedraw = true;
                }
            }
            else
            {
                for (int i = 0; i < Gdi.NumStrips; i++)
                {
                    int strip = _screenStartStrip + i;
                    if (Gdi.TestGfxAnyUsageBits(strip))
                    {
                        for (int j = 1; j < Actors.Length; j++)
                        {
                            if (Gdi.TestGfxUsageBit(strip, j) && Gdi.TestGfxOtherUsageBits(strip, j))
                            {
                                Actors[j].NeedRedraw = true;
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

            for (var i = 1; i < Actors.Length; i++)
            {
                if (Gdi.TestGfxUsageBit(p.X / 8, i) && !GetClass(i, ObjectClass.Untouchable) &&
                    p.Y >= Actors[i].Top && p.Y <= Actors[i].Bottom)
                {
                    if (i != Variables[VariableEgo.Value])
                        return i;
                }
            }

            return 0;
        }

        protected int GetObjActToObjActDist(int a, int b)
        {
            Actor acta = null;
            Actor actb = null;

            if (IsActor(a))
                acta = Actors[ObjToActor(a)];

            if (IsActor(b))
                actb = Actors[ObjToActor(b)];

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

