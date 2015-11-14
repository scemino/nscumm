//
//  ScriptParser6.cs
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
using System.Collections.Generic;

namespace NScumm.Dump
{
    partial class ScriptParser6: ScriptParser
    {
        public ScriptParser6(GameInfo game)
            : base(game)
        {
            KnownVariables = new Dictionary<int, string>();
        }

        #region implemented abstract members of ScriptParser

        protected override void InitOpCodes()
        {
            opCodes = new Dictionary<int, Func<Statement>>();

            /* 00 */
            opCodes[0x00] = PushByte;
            opCodes[0x01] = PushWord;
            opCodes[0x02] = PushByteVar;
            opCodes[0x03] = PushWordVar;
            /* 04 */
            opCodes[0x06] = ByteArrayRead;
            opCodes[0x07] = WordArrayRead;
            /* 08 */
            opCodes[0x0a] = ByteArrayIndexedRead;
            opCodes[0x0b] = WordArrayIndexedRead;
            /* 0C */
            opCodes[0x0c] = Dup;
            opCodes[0x0d] = Not;
            opCodes[0x0e] = Eq;
            opCodes[0x0f] = NEq;
            /* 10 */
            opCodes[0x10] = Gt;
            opCodes[0x11] = Lt;
            opCodes[0x12] = Le;
            opCodes[0x13] = Ge;
            /* 14 */
            opCodes[0x14] = Add;
            opCodes[0x15] = Sub;
            opCodes[0x16] = Mul;
            opCodes[0x17] = Div;
            /* 18 */
            opCodes[0x18] = Land;
            opCodes[0x19] = Lor;
            opCodes[0x1a] = PopStatement;
            /* 1C */
            /* 20 */
            /* 24 */
            /* 28 */
            /* 2C */
            /* 30 */
            /* 34 */
            /* 38 */
            /* 3C */
            /* 40 */
            opCodes[0x42] = WriteByteVar;
            opCodes[0x43] = WriteWordVar;
            /* 44 */
            opCodes[0x46] = ByteArrayWrite;
            opCodes[0x47] = WordArrayWrite;
            /* 4C */
            opCodes[0x4e] = ByteVarInc;
            opCodes[0x4f] = WordVarInc;
            /* 50 */
            opCodes[0x52] = ByteArrayInc;
            opCodes[0x53] = WordArrayInc;
            /* 54 */
            opCodes[0x56] = ByteVarDec;
            opCodes[0x57] = WordVarDec;
            /* 58 */
            opCodes[0x5a] = ByteArrayDec;
            opCodes[0x5b] = WordArrayDec;
            /* 5C */
            opCodes[0x5c] = If;
            opCodes[0x5d] = IfNot;
            opCodes[0x5e] = StartScript;
            opCodes[0x5f] = StartScriptQuick;
            /* 60 */
            opCodes[0x60] = StartObject;
            opCodes[0x61] = DrawObject;
            opCodes[0x62] = DrawObjectAt;
            opCodes[0x63] = DrawBlastObject;
            /* 64 */
            opCodes[0x64] = SetBlastObjectWindow;
            opCodes[0x65] = StopObjectCode;
            opCodes[0x66] = StopObjectCode;
            opCodes[0x67] = EndCutscene;
            /* 68 */
            opCodes[0x68] = Cutscene;
            opCodes[0x69] = StopMusic;
            opCodes[0x6a] = FreezeUnfreeze;
            opCodes[0x6b] = CursorCommand;
            /* 6C */
            opCodes[0x6c] = BreakHere;
            opCodes[0x6d] = IfClassOfIs;
            opCodes[0x6e] = SetClass;
            opCodes[0x6f] = GetState;
            /* 70 */
            opCodes[0x70] = SetState;
            opCodes[0x71] = SetOwner;
            opCodes[0x72] = GetOwner;
            opCodes[0x73] = Jump;
            /* 78 */
            opCodes[0x78] = PanCameraTo;
            opCodes[0x79] = ActorFollowCamera;
            opCodes[0x7a] = SetCameraAt;
            opCodes[0x7b] = LoadRoom;
            /* 74 */
            opCodes[0x74] = StartSound;
            opCodes[0x75] = StopSound;
            opCodes[0x76] = StartMusic;
            opCodes[0x77] = StopObjectScript;
            /* 78 */
            opCodes[0x78] = PanCameraTo;
            opCodes[0x79] = ActorFollowCamera;
            opCodes[0x7a] = SetCameraAt;
            opCodes[0x7b] = LoadRoom;
            /* 7C */
            opCodes[0x7c] = StopScript;
            opCodes[0x7d] = WalkActorToObj;
            opCodes[0x7e] = WalkActorTo;
            opCodes[0x7f] = PutActorAtXY;
            /* 80 */
            opCodes[0x80] = PutActorAtObject;
            opCodes[0x81] = FaceActor;
            opCodes[0x82] = AnimateActor;
            opCodes[0x83] = DoSentence;
            /* 84 */
            opCodes[0x84] = PickupObject;
            opCodes[0x85] = LoadRoomWithEgo;
            opCodes[0x87] = GetRandomNumber;
            /* 88 */
            opCodes[0x88] = GetRandomNumberRange;
            opCodes[0x8a] = GetActorMoving;
            opCodes[0x8b] = IsScriptRunning;
            /* 8C */
            opCodes[0x8c] = GetActorRoom;
            opCodes[0x8d] = GetObjectX;
            opCodes[0x8e] = GetObjectY;
            opCodes[0x8f] = GetObjectOldDir;
            /* 90 */
            opCodes[0x90] = GetActorWalkBox;
            opCodes[0x91] = GetActorCostume;
            opCodes[0x92] = FindInventory;
            opCodes[0x93] = GetInventoryCount;
            /* 94 */
            opCodes[0x94] = GetVerbFromXY;
            opCodes[0x95] = BeginOverride;
            opCodes[0x96] = EndOverride;
            opCodes[0x97] = SetObjectName;

            /* 98 */
            opCodes[0x98] = IsSoundRunning;
            opCodes[0x99] = SetBoxFlags;
            opCodes[0x9a] = CreateBoxMatrix;
            opCodes[0x9b] = ResourceRoutines;
            /* 9C */
            opCodes[0x9c] = RoomOps;
            opCodes[0x9d] = ActorOps;
            opCodes[0x9e] = VerbOps;
            opCodes[0x9f] = GetActorFromXY;
            /* A0 */
            opCodes[0xa0] = FindObject;
            opCodes[0xa1] = PseudoRoom;
            opCodes[0xa2] = GetActorElevation;
            opCodes[0xa3] = GetVerbEntrypoint;
            /* A4 */
            opCodes[0xa4] = ArrayOps;
            opCodes[0xa5] = SaveRestoreVerbs;
            opCodes[0xa6] = DrawBox;
            opCodes[0xa7] = PopStatement;
            /* A8 */
            opCodes[0xa8] = GetActorWidth;
            opCodes[0xa9] = Wait;
            opCodes[0xaa] = GetActorScaleX;
            opCodes[0xab] = GetActorAnimCounter;
            /* AC */
            opCodes[0xac] = SoundKludge;
            opCodes[0xad] = IsAnyOf;
            opCodes[0xae] = SystemOps;
            opCodes[0xaf] = IsActorInBox;
            /* B0 */
            opCodes[0xb0] = Delay;
            opCodes[0xb1] = DelaySeconds;
            opCodes[0xb2] = DelayMinutes;
            opCodes[0xb3] = StopSentence;
            /* B4 */
            opCodes[0xb4] = PrintLine;
            opCodes[0xb5] = PrintText;
            opCodes[0xb6] = PrintDebug;
            opCodes[0xb7] = PrintSystem;
            /* B8 */
            opCodes[0xb8] = PrintActor;
            opCodes[0xb9] = PrintEgo;
            opCodes[0xba] = TalkActor;
            opCodes[0xbb] = TalkEgo;
            /* BC */
            opCodes[0xbc] = DimArray;
            opCodes[0xbd] = Dummy;
            opCodes[0xbe] = StartObjectQuick;
            opCodes[0xbf] = StartScriptQuick2;
            /* C0 */
            opCodes[0xc0] = Dim2DimArray;
            /* C4 */
            opCodes[0xc4] = Abs;
            opCodes[0xc5] = DistObjectObject;
            opCodes[0xc6] = DistObjectPt;
            opCodes[0xc7] = DistPtPt;
            /* C8 */
            opCodes[0xc8] = KernelGetFunctions;
            opCodes[0xc9] = KernelSetFunctions;
            opCodes[0xca] = DelayFrames;
            opCodes[0xcb] = PickOneOf;
            /* CC */
            opCodes[0xcc] = PickOneOfDefault;
            opCodes[0xcd] = StampObject;
            /* D0 */
            opCodes[0xd0] = GetDateTime;
            opCodes[0xd1] = StopTalking;
            opCodes[0xd2] = GetAnimateVariable;
            /* D4 */
            opCodes[0xd4] = Shuffle;
            opCodes[0xd5] = JumpToScript;
            opCodes[0xd6] = Band;
            opCodes[0xd7] = Bor;
            /* D8 */
            opCodes[0xd8] = IsRoomScriptRunning;
            /* DC */
            opCodes[0xdd] = FindAllObjects;
            /* E0 */
            opCodes[0xe1] = GetPixel;
            opCodes[0xe3] = PickVarRandom;
            /* E4 */
            opCodes[0xe4] = SetBoxSet;
            /* E8 */
            /* EC */
            opCodes[0xec] = GetActorLayer;
            opCodes[0xed] = GetObjectNewDir;

        }

        #endregion
    }
}

