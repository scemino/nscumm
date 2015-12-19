//
//  ScriptParser6_Actor.cs
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

namespace NScumm.Dump
{
    partial class ScriptParser6
    {
        protected readonly SimpleName CurrentActor = new SimpleName("CurrentActor");
        readonly SimpleName Actors = new SimpleName("Actors");

        protected Statement PutActorAtXY()
        {
            var room = Pop();
            var y = Pop();
            var x = Pop();
            var act = Pop();
            return new MethodInvocation("PutActor").AddArguments(act, x, y, room).ToStatement();
        }

        protected virtual Statement ActorOps()
        {
            var subOp = ReadByte();
            if (subOp == 197)
            {
                return new BinaryExpression(CurrentActor, Operator.Assignment, Pop()).ToStatement();
            }

            var actor = (Expression)CurrentActor;

            switch (subOp)
            {
                case 76:                // SO_COSTUME
                    actor = new MethodInvocation(new MemberAccess(actor, "SetCostume")).AddArgument(Pop());
                    break;
                case 77:                // SO_STEP_DIST
                    {
                        var j = Pop();
                        var i = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "WalkSpeed")).AddArguments(i, j);
                    }
                    break;
                case 78:                // SOSound
                    {
                        var k = GetStackList(8);
                        actor = new MethodInvocation(new MemberAccess(actor, "Sound")).AddArgument(k);
                    }
                    break;
                case 79:                // SO_WALK_ANIMATION
                    {
                        var f = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetWalkFrame")).AddArguments(f);
                    }
                    break;
                case 80:                // SO_TALK_ANIMATION
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetTalkAnim")).AddArguments(a, b);
                    }
                    break;
                case 81:                // SO_STAND_ANIMATION
                    {
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetStandAnim")).AddArgument(a);
                    }
                    break;
                case 82:                // SO_ANIMATION
                    {
                        // dummy case in scumm6
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "Animation")).AddArguments(a, b, c);
                    }
                    break;
                case 83:                // SO_DEFAULT
                    actor = new MethodInvocation(new MemberAccess(actor, "Default"));
                    break;
                case 84:                // SO_ELEVATION
                    actor = new MethodInvocation(new MemberAccess(actor, "Elevation")).AddArgument(Pop());
                    break;
                case 85:                // SO_ANIMATION_DEFAULT
                    actor = new MethodInvocation(new MemberAccess(actor, "AnimationDefault"));
                    break;
                case 86:                // SO_PALETTE
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetPalette")).AddArguments(a, b);
                    }
                    break;
                case 87:                // SO_TALK_COLOR
                    actor = new MethodInvocation(new MemberAccess(actor, "TalkColor")).AddArgument(Pop());
                    break;
                case 88:                // SO_ACTOR_NAME
                    actor = new MethodInvocation(new MemberAccess(actor, "Name")).AddArgument(ReadCharacters());
                    break;
                case 89:                // SO_INIT_ANIMATION
                    actor = new MethodInvocation(new MemberAccess(actor, "InitAnim")).AddArgument(Pop());
                    break;
                case 91:                // SO_ACTOR_WIDTH
                    actor = new MethodInvocation(new MemberAccess(actor, "Width")).AddArgument(Pop());
                    break;
                case 92:                // SO_SCALE
                    actor = new MethodInvocation(new MemberAccess(actor, "Scale")).AddArgument(Pop());
                    break;
                case 93:                // SO_NEVER_ZCLIP
                    actor = new MethodInvocation(new MemberAccess(actor, "NoZClip"));
                    break;
                case 225:               // SO_ALWAYS_ZCLIP
                case 94:                // SO_ALWAYS_ZCLIP
                    actor = new MethodInvocation(new MemberAccess(actor, "ZClip")).AddArgument(Pop());
                    break;
                case 95:                // SO_IGNORE_BOXES
                    actor = new MethodInvocation(new MemberAccess(actor, "IgnoreBoxes"));
                    break;
                case 96:                // SO_FOLLOW_BOXES
                    actor = new MethodInvocation(new MemberAccess(actor, "FollowBoxes"));
                    break;
                case 97:                // SO_ANIMATION_SPEED
                    actor = new MethodInvocation(new MemberAccess(actor, "AnimSpeed")).AddArgument(Pop());
                    break;
                case 98:                // SO_SHADOW
                    actor = new MethodInvocation(new MemberAccess(actor, "Shadow")).AddArgument(Pop());
                    break;
                case 99:                // SO_TEXT_OFFSET
                    {
                        var y = Pop();
                        var x = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "Offset")).AddArguments(x, y);
                    }
                    break;
                case 198:               // SO_ACTOR_VARIABLE
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "AnimVar")).AddArguments(a, b);
                    }
                    break;
                case 215:               // SO_ACTOR_IGNORE_TURNS_ON
                    actor = new MethodInvocation(new MemberAccess(actor, "IgnoreTurns"));
                    break;
                case 216:               // SO_ACTOR_IGNORE_TURNS_OFF
                    actor = new MethodInvocation(new MemberAccess(actor, "AcceptTurns"));
                    break;
                case 217:               // SO_ACTOR_NEW
                    actor = new MethodInvocation(new MemberAccess(actor, "New"));
                    break;
                case 227:               // SO_ACTOR_DEPTH
                    actor = new MethodInvocation(new MemberAccess(actor, "Depth")).AddArgument(Pop());
                    break;
                case 228:               // SO_ACTOR_WALK_SCRIPT
                    actor = new MethodInvocation(new MemberAccess(actor, "WalkScript")).AddArgument(Pop());
                    break;
                case 229:               // SO_ACTOR_STOP
                    actor = new MethodInvocation(new MemberAccess(actor, "Stop"));
                    break;
                case 230:                                                                               /* set direction */
                    actor = new MethodInvocation(new MemberAccess(actor, "SetDirection")).AddArgument(Pop());
                    break;
                case 231:                                                                               /* turn to direction */
                    actor = new MethodInvocation(new MemberAccess(actor, "TurnToDirection")).AddArgument(Pop());
                    break;
                case 233:               // SO_ACTOR_WALK_PAUSE
                    actor = new MethodInvocation(new MemberAccess(actor, "Pause"));
                    break;
                case 234:               // SO_ACTOR_WALK_RESUME
                    actor = new MethodInvocation(new MemberAccess(actor, "Resume"));
                    break;
                case 235:               // SO_ACTOR_TALK_SCRIPT
                    actor = new MethodInvocation(new MemberAccess(actor, "TalkScript")).AddArgument(Pop());
                    break;
                default:
                    throw new NotSupportedException(string.Format("o6_actorOps: default case {0}", subOp));
            }
            return actor.ToStatement();
        }

        protected virtual void PopRoomAndObject(out Expression room, out Expression obj)
        {
            room = Pop();
            obj = Pop();
        }

        protected Expression GetActor(Expression index)
        {
            return new ElementAccess(Actors, index);
        }

        protected Statement PutActorAtObject()
        {
            Expression room;
            Expression obj;
            PopRoomAndObject(out room, out obj);
            var actor = Pop();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "PutAtObject")).AddArguments(room, obj).ToStatement();
        }

        protected Statement FaceActor()
        {
            var obj = Pop();
            var actor = Pop();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "FaceToObject")).AddArgument(obj).ToStatement();
        }

        protected Statement AnimateActor()
        {
            var anim = Pop();
            var actor = Pop();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "Animate")).AddArgument(anim).ToStatement();
        }

        protected Statement GetActorMoving()
        {
            var actor = Pop();
            return Push(new MemberAccess(GetActor(actor), "IsMoving"));
        }

        protected Statement GetActorRoom()
        {
            var actor = Pop();
            return Push(new MemberAccess(GetActor(actor), "Room"));
        }

        protected Statement GetActorAnimateVariable()
        {
            var variable = Pop();
            var actor = Pop();
            return Push(new MethodInvocation(new MemberAccess(GetActor(actor), "AnimateVariable")).AddArgument(variable));
        }

        protected Statement GetActorWalkBox()
        {
            return Push(new MemberAccess(GetActor(Pop()), "WalkBox"));
        }

        protected Statement GetActorCostume()
        {
            return Push(new MemberAccess(GetActor(Pop()), "Costume"));
        }

        protected Statement GetActorElevation()
        {
            return Push(new MemberAccess(GetActor(Pop()), "Elevation"));
        }

        protected Statement GetActorWidth()
        {
            return Push(new MemberAccess(GetActor(Pop()), "Width"));
        }

        protected Statement GetActorScaleX()
        {
            return Push(new MemberAccess(GetActor(Pop()), "ScaleX"));
        }

        protected Statement GetActorLayer()
        {
            return Push(new MemberAccess(GetActor(Pop()), "Layer"));
        }

        protected Statement GetActorAnimCounter()
        {
            return Push(new MemberAccess(GetActor(Pop()), "AnimCounter"));
        }

        protected Statement IsActorInBox()
        {
            var box = Pop();
            var actor = Pop();
            return Push(new MethodInvocation(new MemberAccess(GetActor(actor), "IsActorInBox")).AddArgument(box));
        }

        protected Statement WalkActorToObj()
        {
            var dist = Pop();
            var obj = Pop();
            var actor = Pop();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "WalkToObject")).AddArguments(obj, dist).ToStatement();
        }

        protected Statement WalkActorTo()
        {
            var y = Pop();
            var x = Pop();
            var actor = Pop();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "WalkTo")).AddArguments(x, y).ToStatement();
        }

        protected Statement GetActorFromXY()
        {
            var y = Pop();
            var x = Pop();
            return Push(new MethodInvocation("GetActorFrom").AddArguments(x, y));
        }

        protected Statement TalkActor()
        {
            var actor = Pop();
            var text = ReadCharacters();
            return new MethodInvocation(new MemberAccess(GetActor(actor), "Talk")).AddArgument(text).ToStatement();
        }

        protected Statement TalkEgo()
        {
            var text = ReadCharacters();
            return new MethodInvocation(new MemberAccess("Ego", "Talk")).AddArgument(text).ToStatement();
        }

        protected Statement StopTalking()
        {
            return new MethodInvocation("StopTalk").ToStatement();
        }

        protected Statement GetAnimateVariable()
        {
            var variable = Pop();
            var index = Pop();
            return new MethodInvocation(
                new MemberAccess(GetActor(index), "GetAnimateVariable")
            ).AddArgument(variable).ToStatement();
        }
    }
}

