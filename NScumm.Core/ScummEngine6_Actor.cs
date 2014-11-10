//
//  ScummEngine6_Actor.cs
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
using NScumm.Core.Graphics;
using System;

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        int _curActor;

        [OpCode(0x7f)]
        void PutActorAtXY(int actorIndex, short x, short y, int room)
        {
            var actor = _actors[actorIndex];
            if (room == 0xFF || room == 0x7FFFFFFF)
            {
                room = actor.Room;
            }
            else
            {
                if (actor.IsVisible && CurrentRoom != room && TalkingActor == actor.Number)
                {
                    StopTalk();
                }
                if (room != 0)
                {
                    actor.Room = (byte)room;
                }
            }
            actor.PutActor(new Point(x, y), (byte)room);
        }

        [OpCode(0x9d)]
        void ActorOps()
        {
            var subOp = ReadByte();
            if (subOp == 197)
            {
                _curActor = Pop();
                return;
            }

            var a = _actors[_curActor];
            if (a == null)
                return;

            switch (subOp)
            {
                case 76:                // SO_COSTUME
                    a.SetActorCostume((ushort)Pop());
                    break;
                case 77:                // SO_STEP_DIST
                    {
                        var j = (uint)Pop();
                        var i = (uint)Pop();
                        a.SetActorWalkSpeed(i, j);
                    }
                    break;
//                case 78:                // SO_SOUND
//                    {
//                        var args = GetStackList(8);
//                        for (var i = 0; i < args.Length; i++)
//                            a.Sound[i] = args[i];
//                        break;
//                    }
                case 79:                // SO_WALK_ANIMATION
                    a.WalkFrame = (byte)Pop();
                    break;
                case 80:                // SO_TALK_ANIMATION
                    a.TalkStopFrame = (byte)Pop();
                    a.TalkStartFrame = (byte)Pop();
                    break;
                case 81:                // SO_STAND_ANIMATION
                    a.StandFrame = (byte)Pop();
                    break;
                case 82:                // SO_ANIMATION
                    // dummy case in scumm6
                    Pop();
                    Pop();
                    Pop();
                    break;
                case 83:                // SO_DEFAULT
                    a.Init(0);
                    break;
                case 84:                // SO_ELEVATION
                    a.Elevation = Pop();
                    break;
                case 85:                // SO_ANIMATION_DEFAULT
                    a.InitFrame = 1;
                    a.WalkFrame = 2;
                    a.StandFrame = 3;
                    a.TalkStartFrame = 4;
                    a.TalkStopFrame = 5;
                    break;
                case 86:                // SO_PALETTE
                    {
                        var j = (ushort)Pop();
                        var i = Pop();
                        ScummHelper.AssertRange(0, i, 255, "o6_actorOps: palette slot");
                        a.SetPalette(i, j);
                    }
                    break;
                case 87:                // SO_TALK_COLOR
                    a.TalkColor = (byte)Pop();
                    break;
                case 88:                // SO_ACTOR_NAME
                    a.Name = ReadCharacters();
                    break;
                case 89:                // SO_INIT_ANIMATION
                    a.InitFrame = (byte)Pop();
                    break;
                case 91:                // SO_ACTOR_WIDTH
                    a.Width = (uint)Pop();
                    break;
                case 92:                // SO_SCALE
                    {
                        var i = Pop();
                        a.SetScale(i, i);
                    }
                    break;
                case 93:                // SO_NEVER_ZCLIP
                    a.ForceClip = 0;
                    break;
                case 225:               // SO_ALWAYS_ZCLIP
                case 94:                // SO_ALWAYS_ZCLIP
                    a.ForceClip = (byte)Pop();
                    break;
                case 95:                // SO_IGNORE_BOXES
                    a.IgnoreBoxes = true;
                    a.ForceClip = (Game.Version >= 7) ? (byte)100 : (byte)0;
                    if (a.IsInCurrentRoom)
                        a.PutActor();
                    break;
                case 96:                // SO_FOLLOW_BOXES
                    a.IgnoreBoxes = false;
                    a.ForceClip = (Game.Version >= 7) ? (byte)100 : (byte)0;
                    if (a.IsInCurrentRoom)
                        a.PutActor();
                    break;
                case 97:                // SO_ANIMATION_SPEED
                    a.SetAnimSpeed((byte)Pop());
                    break;
                case 98:                // SO_SHADOW
                    a.ShadowMode = (byte)Pop();
                    break;
                case 99:                // SO_TEXT_OFFSET
                    {
                        var y = (short)Pop();
                        var x = (short)Pop();
                        a.TalkPosition = new Point(x, y);
                    }
                    break;
                case 198:               // SO_ACTOR_VARIABLE
                    {
                        var i = Pop();
                        a.SetAnimVar(Pop(), i);
                    }
                    break;
                case 215:               // SO_ACTOR_IGNORE_TURNS_ON
                    a.IgnoreTurns = true;
                    break;
                case 216:               // SO_ACTOR_IGNORE_TURNS_OFF
                    a.IgnoreTurns = false;
                    break;
                case 217:               // SO_ACTOR_NEW
                    a.Init(2);
                    break;
                case 227:               // SO_ACTOR_DEPTH
                    a.Layer = Pop();
                    break;
                case 228:               // SO_ACTOR_WALK_SCRIPT
                    a.WalkScript = (ushort)Pop();
                    break;
                case 229:               // SO_ACTOR_STOP
                    a.StopActorMoving();
                    a.StartAnimActor(a.StandFrame);
                    break;
                case 230:                                                                               /* set direction */
                    a.Moving &= ~MoveFlags.Turn;
                    a.SetDirection(Pop());
                    break;
                case 231:                                                                               /* turn to direction */
                    a.TurnToDirection(Pop());
                    break;
                case 233:               // SO_ACTOR_WALK_PAUSE
                    a.Moving |= MoveFlags.Frozen;
                    break;
                case 234:               // SO_ACTOR_WALK_RESUME
                    a.Moving &= ~MoveFlags.Frozen;
                    break;
                case 235:               // SO_ACTOR_TALK_SCRIPT
                    a.TalkScript = (ushort)Pop();
                    break;
                default:
                    throw new NotSupportedException(string.Format("ActorOps: default case {0}", subOp));
            }
        }

        [OpCode(0x82)]
        void AnimateActor(int index, int anim)
        {
            _actors[index].Animate(anim);
        }
    }
}

