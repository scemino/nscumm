//
//  ScriptParser3.cs
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
using NScumm.Scumm.IO;
using System;
using NScumm.Core;
using System.Collections.Generic;
using System.Linq;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser0 : ScriptParser
    {
        public ScriptParser0(GameInfo game)
            : base(game)
        {
            KnownVariables = new Dictionary<int, string>
            {
                { 0,"VariableEgo" },
                { 2,"VariableCameraPosX" },
                { 3,"VariableHaveMessage" },
                { 4,"VariableRoom" },
                { 5,"VariableActiveObject2" },
                { 6,"VariableOverride" },
                { 8,"VariableIsSoundRunning" },
                { 9,"VariableActiveVerb" },
                { 10,"VariableCharCount" }
            };
        }

        protected override void InitOpCodes()
        {
            opCodes = new Dictionary<int, Func<Statement>>();
            /* 00 */
            opCodes[0x00] = StopObjectCode;
            opCodes[0x01] = PutActor;
            opCodes[0x02] = StartMusic;
            opCodes[0x03] = DoSentence;
            /* 04 */
            opCodes[0x04] = IsGreaterEqual;
            opCodes[0x05] = StopCurrentScript;
            opCodes[0x06] = GetDistance;
            opCodes[0x07] = GetActorRoom;
            /* 08 */
            opCodes[0x08] = IsNotEqual;
            opCodes[0x09] = StopCurrentScript;
            opCodes[0x0a] = StopCurrentScript;
            opCodes[0x0b] = SetActorBitVar;
            /* 0C */
            opCodes[0x0c] = LoadSound;
            opCodes[0x0d] = PrintEgo;
            opCodes[0x0e] = PutActorAtObject;
            opCodes[0x0f] = ClearState02;
            /* 10 */
            opCodes[0x10] = BreakHere;
            opCodes[0x11] = AnimateActorCore;
            opCodes[0x12] = PanCameraTo;
            opCodes[0x13] = LockCostume;
            /* 14 */
            opCodes[0x14] = Print;
            opCodes[0x15] = WalkActorToActor;
            opCodes[0x16] = GetRandomNumber;
            opCodes[0x17] = ClearState08;
            /* 18 */
            opCodes[0x18] = JumpRelative;
            opCodes[0x19] = StopCurrentScript;
            opCodes[0x1a] = Move;
            opCodes[0x1b] = GetActorBitVar;
            /* 1C */
            opCodes[0x1c] = StartSound;
            opCodes[0x1d] = SetBitVar;
            opCodes[0x1e] = WalkActorTo;
            opCodes[0x1f] = IfState04;
            /* 20 */
            opCodes[0x20] = StopMusic;
            opCodes[0x21] = PutActor;
            opCodes[0x22] = SaveLoadGame;
            opCodes[0x23] = StopCurrentScript;
            /* 24 */
            opCodes[0x24] = IfNotEqualActiveObject2;
            opCodes[0x25] = LoadRoom;
            opCodes[0x26] = GetClosestActor;
            opCodes[0x27] = GetActorY;
            /* 28 */
            opCodes[0x28] = EqualZero;
            opCodes[0x29] = SetOwnerOf;
            opCodes[0x2a] = Delay;
            opCodes[0x2b] = SetActorBitVar;
            /* 2C */
            opCodes[0x2c] = StopCurrentScript;
            opCodes[0x2d] = PutActorInRoom;
            opCodes[0x2e] = Print;
            opCodes[0x2f] = IfState08;
            /* 30 */
            opCodes[0x30] = LoadCostume;
            opCodes[0x31] = GetBitVar;
            opCodes[0x32] = SetCameraAt;
            opCodes[0x33] = LockScript;
            /* 34 */
            opCodes[0x34] = GetDistance;
            opCodes[0x35] = StopCurrentScript;
            opCodes[0x36] = WalkActorToObject;
            opCodes[0x37] = ClearState04;
            /* 38 */
            opCodes[0x38] = IsLessEqual;
            opCodes[0x39] = StopCurrentScript;
            opCodes[0x3a] = Subtract;
            opCodes[0x3b] = StopCurrentScript;
            /* 3C */
            opCodes[0x3c] = StopSound;
            opCodes[0x3d] = SetBitVar;
            opCodes[0x3e] = WalkActorTo;
            opCodes[0x3f] = IfState02;
            /* 40 */
            opCodes[0x40] = Cutscene;
            opCodes[0x41] = PutActor;
            opCodes[0x42] = StartScript;
            opCodes[0x43] = DoSentence;
            /* 44 */
            opCodes[0x44] = IsLess;
            opCodes[0x45] = StopCurrentScript;
            opCodes[0x46] = Increment;
            opCodes[0x47] = GetActorX;
            /* 48 */
            opCodes[0x48] = IsEqual;
            opCodes[0x49] = StopCurrentScript;
            opCodes[0x4a] = LoadRoom;
            opCodes[0x4b] = SetActorBitVar;
            /* 4C */
            opCodes[0x4c] = LoadScript;
            opCodes[0x4d] = LockRoom;
            opCodes[0x4e] = PutActorAtObject;
            opCodes[0x4f] = ClearState02;
            /* 50 */
            opCodes[0x50] = Nop;
            opCodes[0x51] = AnimateActorCore;
            opCodes[0x52] = ActorFollowCamera;
            opCodes[0x53] = LockSound;
            /* 54 */
            opCodes[0x54] = SetObjectName;
            opCodes[0x55] = WalkActorToActor;
            opCodes[0x56] = GetActorMoving;
            opCodes[0x57] = ClearState08;
            /* 58 */
            opCodes[0x58] = BeginOverride;
            opCodes[0x59] = StopCurrentScript;
            opCodes[0x5a] = Add;
            opCodes[0x5b] = GetActorBitVar;
            /* 5C */
            opCodes[0x5c] = StartSound;
            opCodes[0x5d] = SetBitVar;
            opCodes[0x5e] = WalkActorTo;
            opCodes[0x5f] = IfState04;
            /* 60 */
            opCodes[0x60] = SetMode;
            opCodes[0x61] = PutActor;
            opCodes[0x62] = StopScript;
            opCodes[0x63] = StopCurrentScript;
            /* 64 */
            opCodes[0x64] = IfEqualActiveObject2;
            opCodes[0x65] = StopCurrentScript;
            opCodes[0x66] = GetClosestActor;
            opCodes[0x67] = GetActorFacing;
            /* 68 */
            opCodes[0x68] = IsScriptRunning;
            opCodes[0x69] = SetOwnerOf;
            opCodes[0x6a] = StopCurrentScript;
            opCodes[0x6b] = SetActorBitVar;
            /* 6C */
            opCodes[0x6c] = StopCurrentScript;
            opCodes[0x6d] = PutActorInRoom;
            opCodes[0x6e] = ScreenPrepare;
            opCodes[0x6f] = IfState08;
            /* 70 */
            opCodes[0x70] = Lights;
            opCodes[0x71] = GetBitVar;
            opCodes[0x72] = Nop;
            opCodes[0x73] = GetObjectOwner;
            /* 74 */
            opCodes[0x74] = GetDistance;
            opCodes[0x75] = PrintEgo;
            opCodes[0x76] = WalkActorToObject;
            opCodes[0x77] = ClearState04;
            /* 78 */
            opCodes[0x78] = IsGreater;
            opCodes[0x79] = StopCurrentScript;
            opCodes[0x7a] = StopCurrentScript;
            opCodes[0x7b] = StopCurrentScript;
            /* 7C */
            opCodes[0x7c] = IsSoundRunning;
            opCodes[0x7d] = SetBitVar;
            opCodes[0x7e] = WalkActorTo;
            opCodes[0x7f] = IfNotState02;
            /* 80 */
            opCodes[0x80] = StopCurrentScript;
            opCodes[0x81] = PutActor;
            opCodes[0x82] = StopCurrentScript;
            opCodes[0x83] = DoSentence;
            /* 84 */
            opCodes[0x84] = IsGreaterEqual;
            opCodes[0x85] = StopCurrentScript;
            opCodes[0x86] = Nop;
            opCodes[0x87] = GetActorRoom;
            /* 88 */
            opCodes[0x88] = IsNotEqual;
            opCodes[0x89] = StopCurrentScript;
            opCodes[0x8a] = StopCurrentScript;
            opCodes[0x8b] = SetActorBitVar;
            /* 8C */
            opCodes[0x8c] = LoadSound;
            opCodes[0x8d] = StopCurrentScript;
            opCodes[0x8e] = PutActorAtObject;
            opCodes[0x8f] = SetState02;
            /* 90 */
            opCodes[0x90] = PickupObject;
            opCodes[0x91] = AnimateActorCore;
            opCodes[0x92] = PanCameraTo;
            opCodes[0x93] = UnlockCostume;
            /* 94 */
            opCodes[0x94] = Print;
            opCodes[0x95] = ActorFromPos;
            opCodes[0x96] = StopCurrentScript;
            opCodes[0x97] = SetState08;
            /* 98 */
            opCodes[0x98] = Restart;
            opCodes[0x99] = StopCurrentScript;
            opCodes[0x9a] = Move;
            opCodes[0x9b] = GetActorBitVar;
            /* 9C */
            opCodes[0x9c] = StartSound;
            opCodes[0x9d] = SetBitVar;
            opCodes[0x9e] = WalkActorTo;
            opCodes[0x9f] = IfNotState04;
            /* A0 */
            opCodes[0xa0] = StopObjectCode;
            opCodes[0xa1] = PutActor;
            opCodes[0xa2] = SaveLoadGame;
            opCodes[0xa3] = StopCurrentScript;
            /* A4 */
            opCodes[0xa4] = IfNotEqualActiveObject2;
            opCodes[0xa5] = LoadRoom;
            opCodes[0xa6] = StopCurrentScript;
            opCodes[0xa7] = GetActorY;
            /* A8 */
            opCodes[0xa8] = NotEqualZero;
            opCodes[0xa9] = SetOwnerOf;
            opCodes[0xaa] = StopCurrentScript;
            opCodes[0xab] = SetActorBitVar;
            /* AC */
            opCodes[0xac] = StopCurrentScript;
            opCodes[0xad] = PutActorInRoom;
            opCodes[0xae] = Print;
            opCodes[0xaf] = IfNotState08;
            /* B0 */
            opCodes[0xb0] = LoadCostume;
            opCodes[0xb1] = GetBitVar;
            opCodes[0xb2] = SetCameraAt;
            opCodes[0xb3] = UnlockScript;
            /* B4 */
            opCodes[0xb4] = GetDistance;
            opCodes[0xb5] = StopCurrentScript;
            opCodes[0xb6] = WalkActorToObject;
            opCodes[0xb7] = SetState04;
            /* B8 */
            opCodes[0xb8] = IsLessEqual;
            opCodes[0xb9] = StopCurrentScript;
            opCodes[0xba] = Subtract;
            opCodes[0xbb] = StopCurrentScript;
            /* BC */
            opCodes[0xbc] = StopSound;
            opCodes[0xbd] = SetBitVar;
            opCodes[0xbe] = WalkActorTo;
            opCodes[0xbf] = IfNotState02;
            /* C0 */
            opCodes[0xc0] = EndCutscene;
            opCodes[0xc1] = PutActor;
            opCodes[0xc2] = StartScript;
            opCodes[0xc3] = DoSentence;
            /* C4 */
            opCodes[0xc4] = IsLess;
            opCodes[0xc5] = StopCurrentScript;
            opCodes[0xc6] = Decrement;
            opCodes[0xc7] = GetActorX;
            /* C8 */
            opCodes[0xc8] = IsEqual;
            opCodes[0xc9] = StopCurrentScript;
            opCodes[0xca] = LoadRoom;
            opCodes[0xcb] = SetActorBitVar;
            /* CC */
            opCodes[0xcc] = LoadScript;
            opCodes[0xcd] = UnlockRoom;
            opCodes[0xce] = PutActorAtObject;
            opCodes[0xcf] = SetState02;
            /* D0 */
            opCodes[0xd0] = Nop;
            opCodes[0xd1] = AnimateActorCore;
            opCodes[0xd2] = ActorFollowCamera;
            opCodes[0xd3] = UnlockSound;
            /* D4 */
            opCodes[0xd4] = SetObjectName;
            opCodes[0xd5] = ActorFromPos;
            opCodes[0xd6] = GetActorMoving;
            opCodes[0xd7] = SetState08;
            /* D8 */
            opCodes[0xd8] = StopCurrentScript;
            opCodes[0xd9] = StopCurrentScript;
            opCodes[0xda] = Add;
            opCodes[0xdb] = GetActorBitVar;
            /* DC */
            opCodes[0xdc] = StartSound;
            opCodes[0xdd] = SetBitVar;
            opCodes[0xde] = WalkActorTo;
            opCodes[0xdf] = IfNotState04;
            /* E0 */
            opCodes[0xe0] = SetMode;
            opCodes[0xe1] = PutActor;
            opCodes[0xe2] = StopScript;
            opCodes[0xe3] = StopCurrentScript;
            /* E4 */
            opCodes[0xe4] = IfEqualActiveObject2;
            opCodes[0xe5] = LoadRoomWithEgo;
            opCodes[0xe6] = StopCurrentScript;
            opCodes[0xe7] = GetActorFacing;
            /* E8 */
            opCodes[0xe8] = IsScriptRunning;
            opCodes[0xe9] = SetOwnerOf;
            opCodes[0xea] = StopCurrentScript;
            opCodes[0xeb] = SetActorBitVar;
            /* EC */
            opCodes[0xec] = StopCurrentScript;
            opCodes[0xed] = PutActorInRoom;
            opCodes[0xee] = Dummy;
            opCodes[0xef] = IfNotState08;
            /* F0 */
            opCodes[0xf0] = Lights;
            opCodes[0xf1] = GetBitVar;
            opCodes[0xf2] = Nop;
            opCodes[0xf3] = GetObjectOwner;
            /* F4 */
            opCodes[0xf4] = GetDistance;
            opCodes[0xf5] = StopCurrentScript;
            opCodes[0xf6] = WalkActorToObject;
            opCodes[0xf7] = SetState04;
            /* F8 */
            opCodes[0xf8] = IsGreater;
            opCodes[0xf9] = StopCurrentScript;
            opCodes[0xfa] = StopCurrentScript;
            opCodes[0xfb] = StopCurrentScript;
            /* FC */
            opCodes[0xfc] = IsSoundRunning;
            opCodes[0xfd] = SetBitVar;
            opCodes[0xfe] = WalkActorTo;
            opCodes[0xff] = IfState02;
        }

        private Statement Dummy()
        {
            return new MethodInvocation("Dummy").ToStatement();
        }

        private Statement LoadRoomWithEgo()
        {
            var obj = ReadByte();
            var room = ReadByte();

            return new MethodInvocation("LoadRoomWithEgo").AddArguments(obj.ToLiteral(), room.ToLiteral()).ToStatement();
        }

        private Statement UnlockSound()
        {
            int resid = ReadByte();
            return new MethodInvocation("UnlockSound").AddArgument(resid).ToStatement();
        }

        private Statement UnlockRoom()
        {
            int resid = ReadByte();
            return new MethodInvocation("UnlockRoom").AddArgument(resid).ToStatement();
        }

        private Statement Decrement()
        {
            var index = GetResultIndex();
            return new UnaryExpression(GetVariableAt(index), Operator.PostDecrement).ToStatement();
        }

        private Statement EndCutscene()
        {
            return new MethodInvocation("EndCutscene").ToStatement();
        }

        private Statement SetState04()
        {
            return SetStateCommon(ObjectStateV2.Locked);
        }

        private Statement UnlockScript()
        {
            int resid = ReadByte();
            return new MethodInvocation("UnlockScript").AddArgument(resid).ToStatement();
        }

        private Statement IfNotState08()
        {
            return IfNotStateCommon(ObjectStateV2.State8);
        }

        Statement ClearState(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            return new BinaryExpression(
                new MemberAccess(obj, type.ToString()), Operator.Assignment, false.ToLiteral())
                .ToStatement();
        }

        Statement IfNotStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            return JumpRelative(
                new UnaryExpression(new MemberAccess(obj, type.ToString()), Operator.Not));
        }

        Statement IfStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            return JumpRelative(new MemberAccess(obj, type.ToString()));
        }

        Statement SetStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            return new BinaryExpression(
                new MemberAccess(obj, type.ToString()), Operator.Assignment, true.ToLiteral()).ToStatement();
        }

        private Statement NotEqualZero()
        {
            var a = GetVar();
            return JumpRelative(new BinaryExpression(a, Operator.Inequals, 0.ToLiteral()));
        }

        private Statement IfNotState04()
        {
            return IfNotStateCommon(ObjectStateV2.Locked);
        }

        private Statement Restart()
        {
            return new MethodInvocation("Restart").ToStatement();
        }

        private Statement SetState08()
        {
            return SetStateCommon(ObjectStateV2.State8);
        }

        private Statement ActorFromPos()
        {
            var index = GetResultIndex();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1);
            var y = GetVarOrDirectByte(OpCodeParameter.Param2);
            return SetResult(index, new MethodInvocation("GetActorFromPos").AddArguments(x, y)).ToStatement();
        }

        private Statement UnlockCostume()
        {
            int resid = ReadByte();
            return new MethodInvocation("UnlockCostume").AddArgument(resid).ToStatement();
        }

        private Statement PickupObject()
        {
            var obj = ReadByte();
            return new MethodInvocation("PickupObject").AddArgument(obj).ToStatement();
        }

        private Statement SetState02()
        {
            return SetStateCommon(ObjectStateV2.Untouchable);
        }

        private Statement IfNotState02()
        {
            return IfNotStateCommon(ObjectStateV2.Untouchable);
        }

        private Statement IsSoundRunning()
        {
            var index = GetResultIndex();
            var snd = GetVarOrDirectByte(OpCodeParameter.Param1);
            return SetResult(index, new MethodInvocation("IsSoundRunning").AddArgument(snd)).ToStatement();
        }

        private Statement IsGreater()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.Greater, a));
        }

        private Statement GetObjectOwner()
        {
            var index = GetResultIndex();
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResult(index, new MethodInvocation("GetOwnerCore").AddArgument(obj)).ToStatement();
        }

        private Statement Lights()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("Lights").AddArgument(a).ToStatement();
        }

        private Statement ScreenPrepare()
        {
            return new MethodInvocation("ScreenPrepare").ToStatement();
        }

        private Statement IsScriptRunning()
        {
            var exp = GetResultIndex();
            return SetResult(exp,
                new MethodInvocation("IsScriptRunning")
                .AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1))).ToStatement();
        }

        private Statement GetActorFacing()
        {
            var index = GetResultIndex();
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index, new MemberAccess(act, "Facing")).ToStatement();
        }

        private Statement IfEqualActiveObject2()
        {
            var equal = IfEqualActiveObject2Common((_opCode & 0x80) == 0);
            return JumpRelative(equal);
        }

        Expression IfEqualActiveObject2Common(bool checkType)
        {
            var obj = ReadByte();
            return new MethodInvocation("IfEqualActiveObject2").AddArguments(obj.ToLiteral(), checkType.ToLiteral());
        }

        private Statement StopScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StopScript").AddArgument(script).ToStatement();
        }

        private Statement SetMode()
        {
            var mode = (Engine0Mode)ReadByte();
            return new MethodInvocation("SetMode").AddArgument(mode.ToLiteral()).ToStatement();
        }

        private Statement Add()
        {
            return BinaryExpression(Operator.AddAssignment);
        }

        private Statement BinaryExpression(Operator op)
        {
            var index = GetResultIndex();
            var exp = GetVariableAt(index);
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new BinaryExpression(exp, op, a).ToStatement();
        }

        private Statement BeginOverride()
        {
            // Skip the jump instruction following the override instruction
            var jmp = ReadByte();
            var over = FetchScriptWord();
            return new MethodInvocation("BeginOverride").AddArguments(jmp.ToLiteral(), over.ToLiteral()).ToStatement();
        }

        private Statement GetActorMoving()
        {
            var index = GetResultIndex();
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index,
                new MemberAccess(act, "Moving")).ToStatement();
        }

        private Statement SetObjectName()
        {
            var obj = ReadByte();
            var name = ReadCharacters();
            return new MethodInvocation("SetObjectName").AddArguments(obj.ToLiteral(), name).ToStatement();
        }

        private Statement LockSound()
        {
            return new MethodInvocation("LockSound")
                .AddArgument(ReadByte().ToLiteral()).ToStatement();
        }

        private Statement ActorFollowCamera()
        {
            var actor = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return new MethodInvocation("FollowCamera").AddArgument(actor).ToStatement();
        }

        private Statement Nop()
        {
            return new MethodInvocation("Nop").ToStatement();
        }

        private Statement LockRoom()
        {
            var resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("LockRoom").AddArgument(resid).ToStatement();
        }

        private Statement LoadScript()
        {
            var resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("LoadScript").AddArgument(resid).ToStatement();
        }

        private Statement LoadRoom()
        {
            var resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("LoadRoom").AddArgument(resid).ToStatement();
        }

        private Statement IsEqual()
        {
            var varNum = ReadByte();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(a, Operator.Equals, b));
        }

        Statement JumpRelative(Expression condition)
        {
            var offset = (short)FetchScriptWord();
            var binExp = condition as BinaryExpression;
            var unExp = condition as UnaryExpression;
            if (binExp != null)
            {
                condition = binExp.Not();
            }
            else if (unExp != null)
            {
                condition = unExp.Not();
            }
            else
            {
                condition = condition.Not();
            }
            return new JumpStatement(condition, (int)_br.BaseStream.Position + offset);
        }

        protected uint FetchScriptWord()
        {
            var word = _br.ReadUInt16();
            return word;
        }

        private Statement GetActorX()
        {
            var index = GetResultIndex();
            var a = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index, new MemberAccess(a, "X")).ToStatement();
        }

        private Statement Increment()
        {
            var index = GetResultIndex();
            return new UnaryExpression(GetVariableAt(index), Operator.PostIncrement).ToStatement();
        }

        private Statement IsLess()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.Lower, a));
        }

        private Statement StartScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StartScript").AddArgument(script).ToStatement();
        }

        private Statement Cutscene()
        {
            return new MethodInvocation("Cutscene").ToStatement();
        }

        private Statement IfState02()
        {
            return IfStateCommon(ObjectStateV2.Untouchable);
        }

        private Statement StopSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StopSound").AddArgument(sound).ToStatement();
        }

        private Statement Subtract()
        {
            var index = GetResultIndex();
            var exp = GetVariableAt(index);
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new BinaryExpression(exp, Operator.SubtractionAssignment, a).ToStatement();
        }

        private Statement IsLessEqual()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.LowerOrEquals, a));
        }

        private Statement ClearState04()
        {
            return ClearState(ObjectStateV2.Locked);
        }

        private Statement WalkActorToObject()
        {
            var actor = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            return new MethodInvocation(new MemberAccess(actor, "WalkTo")).AddArgument(obj).ToStatement();
        }

        private Statement LockScript()
        {
            var resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("LockScript").AddArgument(resid).ToStatement();
        }

        private Statement SetCameraAt()
        {
            var at = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("SetCameraAt").AddArgument(at).ToStatement();
        }

        private Statement GetBitVar()
        {
            var index = GetResultIndex();
            var flag = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);
            return SetResult(index,
                new MethodInvocation("GetBitVar").
                AddArguments(flag, mask)).ToStatement();
        }

        private Statement LoadCostume()
        {
            var resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("LoadCostume").AddArgument(resid).ToStatement();
        }

        private Statement IfState08()
        {
            return IfStateCommon(ObjectStateV2.State8);
        }

        private Statement PutActorInRoom()
        {
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);

            return new MethodInvocation(new MemberAccess(act, "PutInRoom")).AddArguments(room).ToStatement();
        }

        private Statement Delay()
        {
            int delay = ReadByte();
            delay |= ReadByte() << 8;
            delay |= ReadByte() << 16;
            delay = 0xFFFFFF - delay;

            return new MethodInvocation("Delay").AddArgument(delay.ToLiteral()).ToStatement();
        }

        private Statement SetOwnerOf()
        {
            var obj = GetObjectAt(GetVarOrDirectWord(OpCodeParameter.Param1));
            var owner = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param2));
            return new BinaryExpression(
                new MemberAccess(obj, "Owner"), Operator.Assignment, owner).ToStatement();
        }

        private Statement EqualZero()
        {
            var a = GetVar();
            return JumpRelative(new BinaryExpression(a, Operator.Equals, 0.ToLiteral()));
        }

        private Statement GetActorY()
        {
            var index = GetResultIndex();
            var a = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index, new MemberAccess(a, "Y")).ToStatement();
        }

        private Statement GetClosestActor()
        {
            var index = GetResultIndex();
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index, new MethodInvocation("GetClosest").AddArgument(act)).ToStatement();
        }

        private Statement IfNotEqualActiveObject2()
        {
            var equal = IfEqualActiveObject2Common((_opCode & 0x80) == 0);
            return JumpRelative(equal.Not());
        }

        private Statement SaveLoadGame()
        {
            var index = GetResultIndex();
            var exp = GetVarOrDirectByte(OpCodeParameter.Param1);
            //var a = (IntegerLiteralExpression)exp;
            //_opCode = (byte)(a.Value & 0xE0);
            return SetResult(index, new MethodInvocation("SaveLoadGame").AddArgument(exp)).ToStatement();
        }

        private Statement StopMusic()
        {
            return new MethodInvocation("StopMusic").ToStatement();
        }

        private Statement IfState04()
        {
            return IfStateCommon(ObjectStateV2.Locked);
        }

        private Statement WalkActorTo()
        {
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var x = GetVarOrDirectByte(OpCodeParameter.Param2);
            var y = GetVarOrDirectByte(OpCodeParameter.Param3);
            return new MethodInvocation(new MemberAccess(act, "WalkTo")).AddArguments(x, y).ToStatement();
        }

        private Statement SetBitVar()
        {
            var flag = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);
            var mod = GetVarOrDirectByte(OpCodeParameter.Param3);
            return new MethodInvocation("SetBitVar").AddArguments(flag, mask, mod).ToStatement();
        }

        private Statement StartSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StartSound").AddArgument(sound).ToStatement();
        }

        private Statement GetActorBitVar()
        {
            var index = GetResultIndex();
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);
            return SetResult(index, new MethodInvocation(new MemberAccess(act, "GetBitVar")).AddArguments(mask)).ToStatement();
        }

        private Statement Move()
        {
            var index = GetResultIndex();
            var value = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResult(index, value).ToStatement();
        }

        protected Expression GetVarOrDirectByte(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return new IntegerLiteralExpression(ReadByte());
        }

        protected Expression GetVarOrDirectWord(OpCodeParameter param)
        {
            return GetVarOrDirectByte(param);
        }

        protected override int ReadWord()
        {
            return ReadByte();
        }

        protected override int ReadWordSigned()
        {
            return ReadByte();
        }

        Expression GetVar()
        {
            return ReadVariable(ReadByte());
        }

        protected override Expression ReadVariable(int var)
        {
            return GetVariableAt(var);
        }

        int GetResultIndex()
        {
            return ReadByte();
        }

        Expression SetResult(int index, Expression value)
        {
            return new BinaryExpression(
                GetVariableAt(index), Operator.Assignment, value);
        }

        static ElementAccess GetVariableAt(int index)
        {
            return new ElementAccess("Variables", index);
        }

        static ElementAccess GetObjectAt(Expression index)
        {
            return new ElementAccess("Objects", index);
        }

        static readonly string[] v0ActorNames_English =
            {
                "Syd",
                "Razor",
                "Dave",
                "Michael",
                "Bernard",
                "Wendy",
                "Jeff",
                "Radiation Suit",
                "Dr Fred",
                "Nurse Edna",
                "Weird Ed",
                "Dead Cousin Ted",
                "Purple Tentacle",
                "Green Tentacle",
                "Meteor Police",
                "Meteor",
                "Mark Eteer",
                "Talkshow Host",
                "Plant",
                "Meteor Radiation",
                "Edsel", // (small, outro)
                "Meteor", // (small, intro)
                "Sandy", // (Lab)
                "Sandy", // (Cut-Scene)
            };

        static Expression GetActorAt(Expression index)
        {
            var ile = index as IntegerLiteralExpression;
            if (ile != null)
            {
                var id = ile.Value & 0x7F;
                if (id > 0 && id <= v0ActorNames_English.Length)
                {
                    return new SimpleName(v0ActorNames_English[id - 1]);
                }
            }
            return new ElementAccess("Actors", index);
        }

        private Statement JumpRelative()
        {
            return JumpRelative(false.ToLiteral());
        }

        protected Expression GetActiveObject()
        {
            if ((_opCode & (byte)OpCodeParameter.Param2) != 0)
                return new SimpleName("ActiveObject");

            return ReadByte().ToLiteral();
        }

        private Statement ClearState08()
        {
            return ClearState(ObjectStateV2.State8);
        }

        private Statement GetRandomNumber()
        {
            var index = GetResultIndex();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);
            return SetResult(index, new MethodInvocation("GetRandomNumber").AddArgument(max)).ToStatement();
        }

        private Statement WalkActorToActor()
        {
            var nr = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var nr2 = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param2));
            var dist = ReadByte();
            return new MethodInvocation(new MemberAccess(nr, "WalkTo")).AddArguments(nr2, dist.ToLiteral()).ToStatement();
        }

        private Statement Print()
        {
            var actor = GetActorAt(ReadByte().ToLiteral());
            var text = ReadCharacters();
            return new MethodInvocation(new MemberAccess(actor, "Print")).AddArgument(text).ToStatement();
        }

        private Statement LockCostume()
        {
            var resid = ReadByte();
            return new MethodInvocation("LockCostume").AddArgument(resid).ToStatement();
        }

        private Statement PanCameraTo()
        {
            return new MethodInvocation("PanCameraToCore").AddArgument(
                GetVarOrDirectByte(OpCodeParameter.Param1)).ToStatement();
        }

        private Statement AnimateActorCore()
        {
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var anim = GetVarOrDirectByte(OpCodeParameter.Param2);
            var repeat = ReadByte().ToLiteral();

            return new MethodInvocation(new MemberAccess(act, "Animate")).AddArguments(anim, repeat).ToStatement();
        }

        private Statement BreakHere()
        {
            return new MethodInvocation("BreakHere").ToStatement();
        }

        private Statement ClearState02()
        {
            return ClearState(ObjectStateV2.Untouchable);
        }

        private Statement PutActorAtObject()
        {
            var a = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            return new MethodInvocation(new MemberAccess(a, "PutAtObject")).AddArguments(obj).ToStatement();
        }

        private Statement PrintEgo()
        {
            return new MethodInvocation("PrintEgo")
                .AddArgument(ReadCharacters()).ToStatement();
        }

        protected override Expression ReadCharacters()
        {
            var sb = new List<byte>();
            byte character;
            while ((character = (byte)ReadByte()) != 0)
            {
                var insertSpace = (character & 0x80) != 0;
                character &= 0x7f;
                if (character == '/')
                {
                    sb.AddRange("{NewLine}".ToCharArray().Select(c => (byte)c));
                }
                else if (character < 32)
                {
                    sb.AddRange(string.Format("{{0}}", character).ToCharArray().Select(c => (byte)c));
                }
                else
                {
                    sb.Add(character);
                }

                if (insertSpace)
                    sb.Add((byte)' ');
            }
            return new StringLiteralExpression(sb.ToArray());
        }

        private Statement LoadSound()
        {
            return new MethodInvocation("LoadSound")
                .AddArgument(ReadByte().ToLiteral()).ToStatement();
        }

        private Statement SetActorBitVar()
        {
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);
            var mod = GetVarOrDirectByte(OpCodeParameter.Param3);
            return new MethodInvocation(new MemberAccess(act, "SetVar")).AddArguments(mask, mod).ToStatement();
        }

        private Statement IsNotEqual()
        {
            var varNum = ReadByte();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(a, Operator.Inequals, b));
        }

        private Statement GetActorRoom()
        {
            var index = GetResultIndex();
            var actor = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            return SetResult(index, new MemberAccess(actor, "Room")).ToStatement();
        }

        private Statement GetDistance()
        {
            var index = GetResultIndex();
            var o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var o2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            return SetResult(index, new MethodInvocation("GetDistance").AddArguments(o1, o2)).ToStatement();
        }

        private Statement StopCurrentScript()
        {
            return new MethodInvocation("StopCurrentScript").ToStatement();
        }

        private Statement IsGreaterEqual()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.GreaterOrEquals, a));
        }

        private Statement DoSentence()
        {
            var verb = ReadByte();
            if (verb == 0xFE)
            {
                return new MethodInvocation("DoSentence").AddArguments(verb.ToLiteral()).ToStatement();
            }
            var obj = ReadByte().ToLiteral();
            var obj2 = ReadByte().ToLiteral();
            return new MethodInvocation("DoSentence").AddArguments(new EnumExpression((VerbsV0)verb), obj, obj2).ToStatement();
        }

        private Statement StartMusic()
        {
            return new MethodInvocation("StartMusic").AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1)).ToStatement();
        }

        private Statement PutActor()
        {
            var act = GetActorAt(GetVarOrDirectByte(OpCodeParameter.Param1));
            var x = GetVarOrDirectByte(OpCodeParameter.Param2);
            var y = GetVarOrDirectByte(OpCodeParameter.Param3);

            return new MethodInvocation(new MemberAccess(act, "PutAt")).AddArguments(x, y).ToStatement();
        }

        Statement StopObjectCode()
        {
            return new MethodInvocation("StopObjectCode").ToStatement();
        }
    }
}

