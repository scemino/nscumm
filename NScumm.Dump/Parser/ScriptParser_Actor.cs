using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        IEnumerable<Statement> GetActorX()
        {
            var resultIndexExp = GetResultIndexExpression();
            var actorIndex = Game.Id == "indy3" ? GetVarOrDirectByte(OpCodeParameter.Param1) : GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(resultIndexExp,
                new MemberAccess(
                    new ElementAccess(
                        new SimpleName("Actors"),
                        actorIndex),
                    "X")).ToStatement();
        }

        IEnumerable<Statement> GetActorY()
        {
            var resultIndexExp = GetResultIndexExpression();
            var actorIndex = Game.Id == "indy3" ? GetVarOrDirectByte(OpCodeParameter.Param1) : GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(
                resultIndexExp,
                new MemberAccess(
                    new ElementAccess(
                        new SimpleName("Actors"),
                        actorIndex),
                    "Y")).ToStatement();
        }

        IEnumerable<Statement> GetActorElevation()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Elevation")).ToStatement();
        }

        IEnumerable<Statement> GetActorWalkBox()
        {
            var index = GetResultIndexExpression();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", act),
                    "Walkbox")).ToStatement();
        }

        IEnumerable<Statement> PutActorAtObject()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return new MethodInvocation("PutActorAtObject").AddArguments(actor, obj).ToStatement();
        }

        IEnumerable<Statement> GetActorMoving()
        {
            var index = GetResultIndexExpression();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", act),
                    "Moving")).ToStatement();
        }

        IEnumerable<Statement> GetActorFacing()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Facing")).ToStatement();
        }

        IEnumerable<Statement> GetActorCostume()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, 
                new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Costume")).ToStatement();
        }

        IEnumerable<Statement> ActorFromPosition()
        {
            var index = GetResultIndexExpression();
            var x = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return SetResultExpression(index, new MethodInvocation("GetActorFromPosition").AddArguments(x, y)).ToStatement();
        }

        IEnumerable<Statement> PutActorInRoom()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Rooms", room),
                    "PutActor")).AddArgument(new ElementAccess("Actors", actor)).ToStatement();
        }

        IEnumerable<Statement> GetActorWidth()
        {
            var indexExp = GetResultIndexExpression();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new MemberAccess(
                    new ElementAccess("Actors", act),
                    "Width")).ToStatement();
        }

        IEnumerable<Statement> ActorFollowCamera()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return new MethodInvocation(
                new MemberAccess(
                    "Camera",
                    "Follow")).AddArguments(
                new ElementAccess("Actors", actor)).ToStatement();
        }

        IEnumerable<Statement> AnimateActor()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var anim = GetVarOrDirectByte(OpCodeParameter.Param2);

            yield return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Animate")).AddArguments(anim).ToStatement();
        }

        IEnumerable<Statement> PutActor()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var x = GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = GetVarOrDirectWord(OpCodeParameter.Param3);
            yield return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Actors", a),
                    "MoveTo")).AddArguments(x, y).ToStatement();
        }

        IEnumerable<Statement> FaceActor()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return new MethodInvocation(new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "FaceTo")).AddArgument(obj).ToStatement();
        }

        IEnumerable<Statement> WalkActorToObject()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "WalkTo")).AddArgument(
                new ElementAccess("Objects", obj)).ToStatement();
        }

        IEnumerable<Statement> WalkActorToActor()
        {
            var nr = GetVarOrDirectByte(OpCodeParameter.Param1);
            var nr2 = GetVarOrDirectByte(OpCodeParameter.Param2);
            var dist = ReadByte();
            yield return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Actors", nr),
                    "WalkTo")).AddArguments(
                new ElementAccess("Actors", nr2),
                dist.ToLiteral()).ToStatement();
        }

        IEnumerable<Statement> GetActorRoom()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);

            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Room")).ToStatement();
        }

        IEnumerable<Statement> WalkActorTo()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var x = GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = GetVarOrDirectWord(OpCodeParameter.Param3);
            yield return new MethodInvocation(new MemberAccess(
                    new ElementAccess("Actors", a),
                    "WalkTo")).AddArguments(x, y).ToStatement();
        }

        IEnumerable<Statement> IsActorInBox()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var box = GetVarOrDirectByte(OpCodeParameter.Param2);

            yield return JumpRelative(new MethodInvocation("IsActorInBox").AddArguments(act, box));
        }

        IEnumerable<Statement> ActorOps()
        {
            var convertTable = new byte[] { 1, 0, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20 };
            Expression actor = new ElementAccess("Actors", GetVarOrDirectByte(OpCodeParameter.Param1));
            while ((_opCode = ReadByte()) != 0xFF)
            {
                if (Game.Version < 5)
                {
                    _opCode = ((_opCode & 0xE0) | convertTable[(_opCode & 0x1F) - 1]);
                }
                switch (_opCode & 0x1F)
                {
                    case 0:
                        {
                            /*						 dummy case */
                            var dummy = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(
                                new MemberAccess(actor, 
                                    "ActorOpUnk0"))
                                        .AddArgument(dummy);
                            break;
                        }
                    case 1:
                        {         // SO_COSTUME
                            var cost = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "Costume")).AddArgument(cost);
                        }
                        break;

                    case 2:
                        {         // SO_STEP_DIST
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetWalkSpeed")).AddArguments(i, j);
                        }
                        break;

                    case 3:
                        {         // SO_SOUND
                            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetSound")).AddArgument(sound);
                        }
                        break;
                    case 4:
                        {         // SO_WALK_ANIMATION
                            var walkFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetWalkAnimation")).AddArgument(walkFrame);
                        }
                        break;

                    case 5:
                        {        // SO_TALK_ANIMATION
                            var talkStartFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var talkStopFrame = GetVarOrDirectByte(OpCodeParameter.Param2);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetTalkAnimation")).AddArguments(talkStartFrame, talkStopFrame);
                        }
                        break;

                    case 6:
                        {        // SO_STAND_ANIMATION
                            var standFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetStandAnimation")).AddArgument(standFrame);
                        }
                        break;

                    case 7:
                        {         // SO_ANIMATION
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            var k = GetVarOrDirectByte(OpCodeParameter.Param3);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetAnimation")).AddArguments(i, j, k);
                        }
                        break;

                    case 8:         // SO_DEFAULT
                        actor = new MethodInvocation(new MemberAccess(actor, "SetDefaultAnimation"));
                        break;

                    case 9:
                        {       // SO_ELEVATION
                            var elevation = GetVarOrDirectWord(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetElevation")).AddArgument(elevation);
                        }
                        break;

                    case 10:        // SO_ANIMATION_DEFAULT
                        actor = new MethodInvocation(new MemberAccess(actor, "ResetAnimation"));
                        break;

                    case 11:
                        {       // SO_PALETTE
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetPalette")).AddArguments(i, j);
                        }
                        break;

                    case 12:
                        {       // SO_TALK_COLOR
                            var talkColor = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetTalkColor")).AddArgument(talkColor);
                        }
                        break;

                    case 13:
                        {        // SO_ACTOR_NAME
                            var name = ReadCharacters();
                            actor = new MethodInvocation(new MemberAccess(actor, "SetName")).AddArgument(name);
                        }
                        break;

                    case 14:
                        {       // SO_INIT_ANIMATION
                            var initFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "SetInitAnimation")).AddArgument(initFrame);
                        }
                        break;

                    case 16:
                        {        // SO_ACTOR_WIDTH
                            var width = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "Width")).AddArgument(width);
                        }
                        break;

                    case 17:
                        {       // SO_ACTOR_SCALE
                            var args = new List<Expression>();
                            if (Game.Version == 4)
                            {
                                var arg = GetVarOrDirectByte(OpCodeParameter.Param1);
                                args.Add(arg);
                                args.Add(arg);
                            }
                            else
                            {
                                args.Add(GetVarOrDirectByte(OpCodeParameter.Param1));
                                args.Add(GetVarOrDirectByte(OpCodeParameter.Param2));
                            }
                            actor = new MethodInvocation(new MemberAccess(actor, "Scale")).AddArguments(args);
                        }
                        break;

                    case 18:
                        {       // SO_NEVER_ZCLIP
                            actor = new MethodInvocation(new MemberAccess(actor, "NeverZClip"));
                        }
                        break;

                    case 19:
                        {       // SO_ALWAYS_ZCLIP
                            var clip = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "ForceCLip")).AddArgument(clip);
                        }
                        break;

                    case 20:        // SO_IGNORE_BOXES
                    case 21:
                        {       // SO_FOLLOW_BOXES
                            var ignoreBoxes = (_opCode & 1) == 0;
                            actor = new MethodInvocation(new MemberAccess(actor, "IgnoreBoxes")).AddArgument(ignoreBoxes.ToLiteral());
                        }
                        break;

                    case 22:
                        {       // SO_ANIMATION_SPEED
                            var animSpeed = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "AnimationSpeed")).AddArgument(animSpeed);
                        }
                        break;

                    case 23:
                        {        // SO_SHADOW
                            var shadowMode = GetVarOrDirectByte(OpCodeParameter.Param1);
                            actor = new MethodInvocation(new MemberAccess(actor, "ShadowMode")).AddArgument(shadowMode);
                        }
                        break;

                    default:
                        throw new NotImplementedException(string.Format("ActorOps #{0} is not yet implemented, sorry :(", _opCode & 0x1F));
                }
            }
            yield return actor.ToStatement();
        }
    }
}

