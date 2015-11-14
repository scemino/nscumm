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
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void WaitForActor()
        {
            if (Game.GameId == Scumm.IO.GameId.Indy3)
            {
                var pos = CurrentPos - 1;
                var actor = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
                if (actor.Moving != MoveFlags.None)
                {
                    CurrentPos = pos;
                    BreakHere();
                }
            }
        }

        void PutActorAtObject()
        {
            Point p;
            var actor = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                p = GetObjectXYPos(obj);
            }
            else
            {
                p = new Point(240, 120);
            }
            actor.PutActor(p);
        }

        protected override void GetActorX()
        {
            GetResult();
            var actorIndex = Game.GameId == Scumm.IO.GameId.Indy3 ? GetVarOrDirectByte(OpCodeParameter.Param1) : GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjX(actorIndex));
        }

        protected override void GetActorY()
        {
            GetResult();
            var actorIndex = Game.GameId == Scumm.IO.GameId.Indy3 ? GetVarOrDirectByte(OpCodeParameter.Param1) : GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetObjY(actorIndex));
        }

        void GetAnimCounter()
        {
            GetResult();
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);
            var actor = Actors[index];
            SetResult(actor.Cost.AnimCounter);
        }

        void ActorFromPosition()
        {
            GetResult();
            var x = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            var actor = GetActorFromPos(new Point(x, y));
            SetResult(actor);
        }

        void GetActorWalkBox()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = Actors[act];
            SetResult(a.Walkbox);
        }

        protected override void WalkActorTo()
        {
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var x = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = (short)GetVarOrDirectWord(OpCodeParameter.Param3);
            a.StartWalk(new Point(x, y), -1);
        }

        void WalkActorToObject()
        {
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                int dir;
                Point p;
                GetObjectXYPos(obj, out p, out dir);
                a.StartWalk(p, dir);
            }
        }

        protected override void PutActor()
        {
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);
            var actor = Actors[index];
            var x = (short)GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = (short)GetVarOrDirectWord(OpCodeParameter.Param3);
            actor.PutActor(new Point(x, y));
        }

        void ActorOps()
        {
            var convertTable = new byte[] { 1, 0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20 };
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = Actors[act];
            int i, j;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                if (Game.Version < 5)
                {
                    _opCode = (byte)((_opCode & 0xE0) | convertTable[(_opCode & 0x1F) - 1]);
                }
                switch (_opCode & 0x1F)
                {
                    case 0:                                     /* dummy case */
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 1:         // SO_COSTUME
                        var cost = (ushort)GetVarOrDirectByte(OpCodeParameter.Param1);
                        a.SetActorCostume(cost);
                        break;

                    case 2:         // SO_STEP_DIST
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        a.SetActorWalkSpeed((uint)i, (uint)j);
                        break;

                    case 3:         // SOSound
                        a.Sound = GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 4:         // SO_WALK_ANIMATION
                        a.WalkFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 5:         // SO_TALK_ANIMATION
                        a.TalkStartFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        a.TalkStopFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
                        break;

                    case 6:         // SO_STAND_ANIMATION
                        a.StandFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 7:         // SO_ANIMATION
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        GetVarOrDirectByte(OpCodeParameter.Param2);
                        GetVarOrDirectByte(OpCodeParameter.Param3);
                        break;

                    case 8:         // SO_DEFAULT
                        a.Init(0);
                        break;

                    case 9:         // SO_ELEVATION
                        a.Elevation = GetVarOrDirectWord(OpCodeParameter.Param1);
                        break;

                    case 10:        // SO_ANIMATION_DEFAULT
                        a.ResetFrames();
                        break;

                    case 11:        // SO_PALETTE
                        i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        ScummHelper.AssertRange(0, i, 31, "o5_actorOps: palette slot");
                        a.SetPalette(i, (ushort)j);
                        break;

                    case 12:        // SO_TALK_COLOR
                        a.TalkColor = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 13:        // SO_ACTOR_NAME
                        a.Name = ReadCharacters();
                        break;

                    case 14:        // SO_INIT_ANIMATION
                        a.InitFrame = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 16:        // SO_ACTOR_WIDTH
                        a.Width = (uint)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 17:        // SO_ACTOR_SCALE
                        if (Game.Version == 4)
                        {
                            i = j = GetVarOrDirectByte(OpCodeParameter.Param1);
                        }
                        else
                        {
                            i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        }
                        a.BoxScale = (ushort)i;
                        a.SetScale(i, j);
                        break;

                    case 18:        // SO_NEVER_ZCLIP
                        a.ForceClip = 0;
                        break;

                    case 19:        // SO_ALWAYS_ZCLIP
                        a.ForceClip = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 20:        // SO_IGNORE_BOXES
                    case 21:        // SO_FOLLOW_BOXES
                        a.IgnoreBoxes = (_opCode & 1) == 0;
                        a.ForceClip = 0;
                        if (a.IsInCurrentRoom)
                            a.PutActor();
                        break;

                    case 22:        // SO_ANIMATION_SPEED
                        a.SetAnimSpeed((byte)GetVarOrDirectByte(OpCodeParameter.Param1));
                        break;

                    case 23:        // SO_SHADOW
                        a.ShadowMode = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        void GetActorElevation()
        {
            GetResult();
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = Actors[index];
            SetResult(a.Elevation);
        }

        void GetActorWidth()
        {
            GetResult();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var actor = Actors[act];
            SetResult((int)actor.Width);
        }

        protected override void PutActorInRoom()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            byte room = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[act];

            if (a.IsVisible && CurrentRoom != room && TalkingActor == a.Number)
            {
                StopTalk();
            }
            a.Room = room;
            if (room == 0)
                a.PutActor(new Point(), 0);
        }
    }
}

