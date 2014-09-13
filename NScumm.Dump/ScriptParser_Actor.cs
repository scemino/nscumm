using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
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
                    "X"));
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
                    "Y"));
        }

        IEnumerable<Statement> GetActorElevation()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Elevation"));
        }

        IEnumerable<Statement> GetActorWalkBox()
        {
            var index = GetResultIndexExpression();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", act),
                    "Walkbox"));
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
                    "Moving"));
        }

        IEnumerable<Statement> GetActorFacing()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Facing"));
        }

        IEnumerable<Statement> GetActorCostume()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(index, 
                new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Costume"));
        }

        IEnumerable<Statement> ActorFromPosition()
        {
            var index = GetResultIndexExpression();
            var x = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return SetResultExpression(index, new MethodInvocation("GetActorFromPosition").AddArguments(x, y));
        }

        IEnumerable<Statement> PutActorInRoom()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return new MemberAccess(
                new ElementAccess("Rooms", room),
                new MethodInvocation("PutActor").AddArgument(
                    new ElementAccess("Actors", actor))).ToStatement();
        }

        IEnumerable<Statement> GetActorWidth()
        {
            var indexExp = GetResultIndexExpression();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new MemberAccess(
                    new ElementAccess("Actors", act),
                    "Width"));
        }

        IEnumerable<Statement> ActorFollowCamera()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return new MemberAccess(
                "Camera",
                new MethodInvocation("Follow").AddArguments(
                    new ElementAccess("Actors", actor))).ToStatement();
        }

        IEnumerable<Statement> AnimateActor()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var anim = GetVarOrDirectByte(OpCodeParameter.Param2);

            yield return new MemberAccess(
                new ElementAccess("Actors", actor),
                new MethodInvocation("Animate").AddArguments(anim)).ToStatement();
        }

        IEnumerable<Statement> PutActor()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var x = GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = GetVarOrDirectWord(OpCodeParameter.Param3);
            yield return new MemberAccess(
                new ElementAccess("Actors", a),
                new MethodInvocation("MoveTo").AddArguments(x, y)).ToStatement();
        }

        IEnumerable<Statement> FaceActor()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return new MemberAccess(
                new ElementAccess("Actors", actor),
                new MethodInvocation("FaceTo").AddArgument(obj)).ToStatement();
        }

        IEnumerable<Statement> WalkActorToObject()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            yield return new MemberAccess(
                new ElementAccess("Actors", actor),
                new MethodInvocation("WalkTo").AddArgument(
                    new ElementAccess("Objects", obj))).ToStatement();
        }

        IEnumerable<Statement> WalkActorToActor()
        {
            var nr = GetVarOrDirectByte(OpCodeParameter.Param1);
            var nr2 = GetVarOrDirectByte(OpCodeParameter.Param2);
            var dist = ReadByte();
            yield return new MemberAccess(
                new ElementAccess("Actors", nr),
                new MethodInvocation("WalkTo").AddArguments(
                    new ElementAccess("Actors", nr2),
                    dist.ToLiteral())).ToStatement();
        }

        IEnumerable<Statement> GetActorRoom()
        {
            var index = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);

            yield return SetResultExpression(index, new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Room"));
        }

        IEnumerable<Statement> WalkActorTo()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var x = GetVarOrDirectWord(OpCodeParameter.Param2);
            var y = GetVarOrDirectWord(OpCodeParameter.Param3);
            yield return new MemberAccess(
                new ElementAccess("Actors", a),
                new MethodInvocation("WalkTo").AddArguments(x, y)).ToStatement();
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
            var actor = new ElementAccess("Actors", GetVarOrDirectByte(OpCodeParameter.Param1));
            while ((_opCode = ReadByte()) != 0xFF)
            {
                if (Game.Version < 5)
                {
                    _opCode = (byte)((_opCode & 0xE0) | convertTable[(_opCode & 0x1F) - 1]);
                }
                switch (_opCode & 0x1F)
                {
                    case 0:
                        {
                            /*						 dummy case */
                            var dummy = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("ActorOpUnk0").
							AddArgument(dummy)).ToStatement();
                            break;
                        }
                    case 1:
                        {         // SO_COSTUME
                            var cost = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "Costume"), 
                                Operator.Assignment,
                                cost).ToStatement();
                        }
                        break;

                    case 2:
                        {         // SO_STEP_DIST
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("SetWalkSpeed").
							AddArguments(i, j)).ToStatement();
                        }
                        break;

                    case 3:
                        {         // SO_SOUND
                            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "Sound"), 
                                Operator.Assignment,
                                sound).ToStatement();
                        }
                        break;
                    case 4:
                        {         // SO_WALK_ANIMATION
                            var walkFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "WalkAnimation"), 
                                Operator.Assignment,
                                walkFrame).ToStatement();
                        }
                        break;

                    case 5:
                        {        // SO_TALK_ANIMATION
                            var talkStartFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var talkStopFrame = GetVarOrDirectByte(OpCodeParameter.Param2);
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("SetWalkSpeed").
							AddArguments(talkStartFrame, talkStopFrame)).ToStatement();
                        }
                        break;

                    case 6:
                        {        // SO_STAND_ANIMATION
                            var standFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "StandAnimation"), 
                                Operator.Assignment,
                                standFrame).ToStatement();
                        }
                        break;

                    case 7:
                        {         // SO_ANIMATION
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            var k = GetVarOrDirectByte(OpCodeParameter.Param3);
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("SetAnimation").
							AddArguments(i, j, k)).ToStatement();
                        }
                        break;

                    case 8:         // SO_DEFAULT
                        yield return new MemberAccess(actor, 
                            new MethodInvocation("SetDefaultAnimation")).ToStatement();
                        break;

                    case 9:
                        {       // SO_ELEVATION
                            var elevation = GetVarOrDirectWord(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "Elevation"), 
                                Operator.Assignment,
                                elevation).ToStatement();
                        }
                        break;

                    case 10:        // SO_ANIMATION_DEFAULT
                        yield return new MemberAccess(actor, 
                            new MethodInvocation("ResetAnimation")).ToStatement();
                        break;

                    case 11:
                        {       // SO_PALETTE
                            var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("SetPalette").AddArguments(i, j)).ToStatement();
                        }
                        break;

                    case 12:
                        {       // SO_TALK_COLOR
                            var talkColor = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "TalkColor"), 
                                Operator.Assignment,
                                talkColor).ToStatement();
                        }
                        break;

                    case 13:
                        {        // SO_ACTOR_NAME
                            var name = ReadCharacters();
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "Name"), 
                                Operator.Assignment,
                                name).ToStatement();
                        }
                        break;

                    case 14:
                        {       // SO_INIT_ANIMATION
                            var initFrame = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "InitFrame"), 
                                Operator.Assignment,
                                initFrame).ToStatement();
                        }
                        break;

                    case 16:
                        {        // SO_ACTOR_WIDTH
                            var width = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "Width"), 
                                Operator.Assignment,
                                width).ToStatement();
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
                            yield return new MemberAccess(actor, 
                                new MethodInvocation("Scale").AddArguments(args)).ToStatement();
                        }
                        break;

                    case 18:
                        {       // SO_NEVER_ZCLIP
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "ForceClip"), 
                                Operator.Assignment,
                                0.ToLiteral()).ToStatement();
                        }
                        break;

                    case 19:
                        {       // SO_ALWAYS_ZCLIP
                            var clip = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "ForceClip"), 
                                Operator.Assignment,
                                clip).ToStatement();
                        }
                        break;

                    case 20:        // SO_IGNORE_BOXES
                    case 21:
                        {       // SO_FOLLOW_BOXES
                            var ignoreBoxes = (_opCode & 1) == 0;
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "IgnoreBoxes"), 
                                Operator.Assignment,
                                ignoreBoxes.ToLiteral()).ToStatement();
                        }
                        break;

                    case 22:
                        {       // SO_ANIMATION_SPEED
                            var animSpeed = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "AnimationSpeed"), 
                                Operator.Assignment,
                                animSpeed).ToStatement();
                        }
                        break;

                    case 23:
                        {        // SO_SHADOW
                            var shadowMode = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new BinaryExpression(
                                new MemberAccess(actor, "ShadowMode"), 
                                Operator.Assignment,
                                shadowMode).ToStatement();
                        }
                        break;

                    default:
                        throw new NotImplementedException(string.Format("ActorOps #{0} is not yet implemented, sorry :(", _opCode & 0x1F));
                }
            }
        }
    }
}

