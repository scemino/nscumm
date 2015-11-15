//
//  ScriptParser8.cs
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
    partial class ScriptParser8: ScriptParser6
    {
        public ScriptParser8(GameInfo game)
            : base(game)
        {
            KnownVariables = new Dictionary<int, string>
            {
                { 1, "VariableRoomWidth" },
                { 2, "VariableRoomHeight" },
                { 3, "VariableMouseX" },
                { 4, "VariableMouseY" },
                { 5, "VariableVirtualMouseX" },
                { 6, "VariableVirtualMouseY" },

                { 7, "VariableCursorState" },
                { 8, "VariableUserPut" },

                { 9, "VariableCameraPosX" },
                { 10, "VariableCameraPosY" },
                { 11, "VariableCameraDestX" },
                { 12, "VariableCameraDestY" },
                { 13, "VariableCameraFollowedActor" },

                { 14, "VariableTalkActor" },
                { 15, "VariableHaveMessage" },

                { 16, "VariableLeftButtonDown" },
                { 17, "VariableRightButtonDown" },
                { 18, "VariableLeftButtonHold" },
                { 19, "VariableRightButtonHold" },

                { 24, "VariableTimeDateYear" },
                { 25, "VariableTimeDateMonth" },
                { 26, "VariableTimeDateDay" },
                { 27, "VariableTimeDateHour" },
                { 28, "VariableTimeDateMinute" },
                { 29, "VariableTimeDateSecond" },

                { 30, "VariableOverride" },
                { 31, "VariableRoom" },
                { 32, "VariableNewRoom" },
                { 33, "VariableWalkToObject" },
                { 34, "VariableTimer" },

                { 39, "VariableVoiceMode" },
                { 40, "VariableGameLoaded" },
                { 41, "VariableLanguage" },

                { 42, "VariableCurrentDisk" },
                { 45, "VariableMusicBundleLoaded" },
                { 46, "VariableVoiceBundleLoaded" },
             
                { 50, "VariableScrollScript" },
                { 51, "VariableEntryScript" },
                { 52, "VariableEntryScript2" },
                { 53, "VariableExitScript" },
                { 54, "VariableExitScript2" },
                { 55, "VariableVerbScript" },
                { 56, "VariableSentenceScript" },
                { 57, "VariableInventoryScript" },
                { 58, "VariableCutSceneStartScript" },
                { 59, "VariableCutSceneEndScript" },

                { 62, "VariableCutSceneExitKey" },

                { 64, "VariablePauseKey" },
                { 65, "VariableMainMenu" },
                { 66, "VariableVersionKey" },
                { 67, "VariableTalkStopKey" },

                { 111, "VariableCustomScaleTable" },

                { 112, "VariableTimerNext" },
                { 113, "VariableTimer1" },
                { 114, "VariableTimer2" },
                { 115, "VariableTimer3" },
               
                { 116, "VariableCameraMinX" },
                { 117, "VariableCameraMaxX" },
                { 118, "VariableCameraMinY" },
                { 119, "VariableCameraMaxY" },
                { 120, "VariableCameraSpeedX" },
                { 121, "VariableCameraSpeedY" },
                { 122, "VariableCameraAccelX" },
                { 123, "VariableCameraAccelY" },
                { 124, "VariableCameraThresholdX" },
                { 125, "VariableCameraThresholdY" },

                { 126, "VariableEgo" },

                { 128, "VariableDefaultTalkDelay" },
                { 129, "VariableCharIncrement" },

                { 130, "VariableDebugMode" },
                { 132, "VariableKeyPress" },
                { 133, "VariableBlastAboveText" },
                { 134, "VariableSync" },
            };
        }

        #region implemented abstract members of ScriptParser

        protected override void InitOpCodes()
        {
            opCodes = new Dictionary<int, Func<Statement>>();

            /* 00 */
            opCodes[0x01] = PushWord;
            opCodes[0x02] = PushWordVar;
            opCodes[0x03] = WordArrayRead;
            /* 04 */
            opCodes[0x04] = WordArrayIndexedRead;
            opCodes[0x05] = Dup;
            opCodes[0x06] = PopStatement;
            opCodes[0x07] = Not;
            /* 08 */
            opCodes[0x08] = Eq;
            opCodes[0x09] = NEq;
            opCodes[0x0a] = Gt;
            opCodes[0x0b] = Lt;
            /* 0C */
            opCodes[0x0c] = Le;
            opCodes[0x0d] = Ge;
            opCodes[0x0e] = Add;
            opCodes[0x0f] = Sub;
            /* 10 */
            opCodes[0x10] = Mul;
            opCodes[0x11] = Div;
            opCodes[0x12] = Land;
            opCodes[0x13] = Lor;
            /* 14 */
            opCodes[0x14] = Band;
            opCodes[0x15] = Bor;
            opCodes[0x16] = Mod;
            /* 18 */
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
            /* 44 */
            /* 4C */
            /* 50 */
            /* 54 */
            /* 58 */
            /* 5C */
            /* 60 */
            /* 64 */
            opCodes[0x64] = If;
            opCodes[0x65] = IfNot;
            opCodes[0x66] = Jump;
            opCodes[0x67] = BreakHere;
            /* 68 */
            opCodes[0x68] = DelayFrames;
            opCodes[0x69] = Wait;
            opCodes[0x6a] = Delay;
            opCodes[0x6b] = DelaySeconds;
            /* 6C */
            opCodes[0x6c] = DelayMinutes;
            opCodes[0x6d] = WriteWordVar;
            opCodes[0x6e] = WordVarInc;
            opCodes[0x6f] = WordVarDec;
            /* 70 */
            opCodes[0x70] = DimArray;
            opCodes[0x71] = WordArrayWrite;
            opCodes[0x72] = WordArrayInc;
            opCodes[0x73] = WordArrayDec;
            /* 74 */
            opCodes[0x74] = Dim2DimArray;
            opCodes[0x75] = WordArrayIndexedWrite;
            opCodes[0x76] = ArrayOps;
            /* 78 */
            opCodes[0x79] = StartScript;
            opCodes[0x7a] = StartScriptQuick;
            opCodes[0x7b] = StopObjectCode;
            /* 7C */
            opCodes[0x7c] = StopScript;
            opCodes[0x7d] = JumpToScript;
            opCodes[0x7e] = Dummy;
            opCodes[0x7f] = StartObject;
            /* 80 */
            opCodes[0x80] = StopObjectCode;
            opCodes[0x81] = Cutscene;
            opCodes[0x82] = EndCutscene;
            opCodes[0x83] = FreezeUnfreeze;
            /* 84 */
            opCodes[0x84] = BeginOverride;
            opCodes[0x85] = EndOverride;
            opCodes[0x86] = StopSentence;
            /* 88 */
            opCodes[0x89] = SetClass;
            opCodes[0x8a] = SetState;
            opCodes[0x8b] = SetOwner;
            /* 8C */
            opCodes[0x8c] = PanCameraTo;
            opCodes[0x8d] = ActorFollowCamera;
            opCodes[0x8e] = SetCameraAt;
            opCodes[0x8f] = PrintActor;
            /* 90 */
            opCodes[0x90] = PrintEgo;
            opCodes[0x91] = TalkActor;
            opCodes[0x92] = TalkEgo;
            opCodes[0x93] = PrintLine;
            /* 94 */
            opCodes[0x94] = PrintText;
            opCodes[0x95] = PrintDebug;
            opCodes[0x96] = PrintSystem;
            opCodes[0x97] = BlastText;
            /* 98 */
            opCodes[0x98] = DrawObject;
            /* 9C */
            opCodes[0x9c] = CursorCommand;
            opCodes[0x9d] = LoadRoom;
            opCodes[0x9e] = LoadRoomWithEgo;
            opCodes[0x9f] = WalkActorToObj;
            /* A0 */
            opCodes[0xa0] = WalkActorTo;
            opCodes[0xa1] = PutActorAtXY;
            opCodes[0xa2] = PutActorAtObject;
            opCodes[0xa3] = FaceActor;
            /* A4 */
            opCodes[0xa4] = AnimateActor;
            opCodes[0xa5] = DoSentence;
            opCodes[0xa6] = PickupObject;
            opCodes[0xa7] = SetBoxFlags;
            /* A8 */
            opCodes[0xa8] = CreateBoxMatrix;
            opCodes[0xaa] = ResourceRoutines;
            opCodes[0xaB] = RoomOps;
            /* AC */
            opCodes[0xac] = ActorOps;
            opCodes[0xad] = CameraOps;
            opCodes[0xae] = VerbOps;
            opCodes[0xaf] = StartSound;
            /* B0 */
            opCodes[0xb0] = StartMusic;
            opCodes[0xb1] = StopSound;
            opCodes[0xb2] = SoundKludge;
            opCodes[0xb3] = SystemOps;
            /* B4 */
            opCodes[0xb4] = SaveRestoreVerbs;
            opCodes[0xb5] = SetObjectName;
            opCodes[0xb6] = GetDateTime;
            opCodes[0xb7] = DrawBox;
            /* B8 */
            opCodes[0xb9] = StartVideo;
            opCodes[0xba] = KernelSetFunctions;
            /* BC */
            /* C0 */
            /* C4 */
            /* C8 */
            opCodes[0xc8] = StartScriptQuick2;
            opCodes[0xc9] = StartObjectQuick;
            opCodes[0xca] = PickOneOf;
            opCodes[0xcb] = PickOneOfDefault;
            /* CC */
            opCodes[0xcd] = IsAnyOf;
            opCodes[0xce] = GetRandomNumber;
            opCodes[0xcf] = GetRandomNumberRange;
            /* D0 */
            opCodes[0xd0] = IfClassOfIs;
            opCodes[0xd1] = GetState;
            opCodes[0xd2] = GetOwner;
            opCodes[0xd3] = IsScriptRunning;
            /* D4 */
            opCodes[0xd5] = IsSoundRunning;
            opCodes[0xd6] = Abs;
            /* D8 */
            opCodes[0xd8] = KernelGetFunctions;
            opCodes[0xd9] = IsActorInBox;
            opCodes[0xda] = GetVerbEntrypoint;
            opCodes[0xdb] = GetActorFromXY;
            /* DC */
            opCodes[0xdc] = FindObject;
            opCodes[0xdd] = GetVerbFromXY;
            opCodes[0xdf] = FindInventory;
            /* E0 */
            opCodes[0xe0] = GetInventoryCount;
            opCodes[0xe1] = GetActorAnimateVariable;
            opCodes[0xe2] = GetActorRoom;
            opCodes[0xe3] = GetActorWalkBox;
            /* E4 */
            opCodes[0xe4] = GetActorMoving;
            opCodes[0xe5] = GetActorCostume;
            opCodes[0xe6] = GetActorScaleX;
            opCodes[0xe7] = GetActorLayer;
            /* E8 */
            opCodes[0xe8] = GetActorElevation;
            opCodes[0xe9] = GetActorWidth;
            opCodes[0xea] = GetObjectNewDir;
            opCodes[0xeb] = GetObjectX;
            /* EC */
            opCodes[0xec] = GetObjectY;
            opCodes[0xed] = GetActorChore;
            opCodes[0xee] = DistObjectObject;
            opCodes[0xef] = DistPtPt;
            /* F0 */
            opCodes[0xf0] = GetObjectImageX;
            opCodes[0xf1] = GetObjectImageY;
            opCodes[0xf2] = GetObjectImageWidth;
            opCodes[0xf3] = GetObjectImageHeight;
            /* F4 */
            opCodes[0xf6] = GetStringWidth;
            opCodes[0xf7] = GetActorZPlane;
        }

        #endregion

        protected override int ReadWord()
        {
            return (int)_br.ReadUInt32();
        }

        protected override int ReadWordSigned()
        {
            return _br.ReadInt32();
        }

        protected override Statement CursorCommand()
        {
            var exp = new MethodInvocation("CursorCommand");

            var subOp = ReadByte();

            switch (subOp)
            {
                case 0xDC:              // SO_CURSOR_ON Turn cursor on
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursor")).AddArgument(true);
                    break;
                case 0xDD:              // SO_CURSOR_OFF Turn cursor off
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursor")).AddArgument(false);
                    break;
                case 0xDE:              // SO_CURSOR_SOFT_ON Turn soft cursor on
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorSoft")).AddArgument(true);
                    break;
                case 0xDF:              // SO_CURSOR_SOFT_OFF Turn soft cursor off
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorSoft")).AddArgument(false);
                    break;
                case 0xE0:              // SO_USERPUT_ON
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInput")).AddArgument(true);
                    break;
                case 0xE1:              // SO_USERPUT_OFF
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInput")).AddArgument(false);
                    break;
                case 0xE2:              // SO_USERPUT_SOFT_ON
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInputSoft")).AddArgument(true);
                    break;
                case 0xE3:              // SO_USERPUT_SOFT_OFF
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInputSoft")).AddArgument(false);
                    break;
                case 0xE4:              // SO_CURSOR_IMAGE Set cursor image
                    {
                        var imageIndex = Pop();
                        var obj = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetCursorImage")).AddArguments(obj, imageIndex);
                    }
                    break;
                case 0xE5:              // SO_CURSOR_HOTSPOT Set cursor hotspot
                    {
                        var y = Pop();
                        var x = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetCursorHotspot")).AddArguments(x, y);
                    }
                    break;
                case 0xE6:              // SO_CURSOR_TRANSPARENT Set cursor transparent color
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorTransparentColor")).AddArgument(Pop());
                    break;
                case 0xE7:              // SO_CHARSET_SET
                    exp = new MethodInvocation(new MemberAccess(exp, "InitCharset")).AddArgument(Pop());
                    break;
                case 0xE8:              // SO_CHARSET_COLOR
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCharsetColor")).AddArgument(GetStackList(4));
                    break;
                case 0xE9:              // SO_CURSOR_PUT
                    {
                        var y = Pop();
                        var x = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetCursorPosition")).AddArguments(x, y);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("CursorCommand: default case {0:X2}", subOp));
            }
            return exp.ToStatement();
        }

        protected override Expression DecodeParseString(Expression target, bool withActor)
        {
            var b = ReadByte();

            switch (b)
            {
                case 0xC8:      // SO_PRINT_BASEOP
                    target = new MethodInvocation(new MemberAccess(target, "LoadDefault"));
                    if (withActor)
                    {
                        var actor = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "Actor")).AddArgument(actor);
                    }
                    break;
                case 0xC9:      // SO_PRINT_END
                    target = new MethodInvocation(new MemberAccess(target, "SaveDefault"));
                    break;
                case 0xCA:                // SO_PRINT_AT
                    {
                        var y = Pop();
                        var x = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "At")).AddArguments(x, y);
                    }
                    break;
                case 0xCB:                // SO_COLOR
                    {
                        var color = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "Color")).AddArguments(color);
                    }
                    break;
                case 0xCC:                // SO_PRINT_CENTER
                    target = new MethodInvocation(new MemberAccess(target, "Center"));
                    break;
                case 0xCD:                // SO_PRINT_CHARSET Set print character set
                    target = new MethodInvocation(new MemberAccess(target, "Charset")).AddArgument(Pop());
                    break;
                case 0xCE:                // SO_PRINT_LEFT
                    target = new MethodInvocation(new MemberAccess(target, "Left"));
                    break;
                case 0xCF:                // SO_OVERHEAD
                    target = new MethodInvocation(new MemberAccess(target, "OverHead"));
                    break;
                case 0xD0:                // SO_MUMBLE
                    target = new MethodInvocation(new MemberAccess(target, "Mumble"));
                    break;
                case 0xD1:                // SO_TEXTSTRING
                    target = new MethodInvocation(new MemberAccess(target, "Text")).AddArgument(ReadCharacters());
                    break;
                case 0xD2:                // SO_PRINT_WRAP Set print wordwrap
                    target = new MethodInvocation(new MemberAccess(target, "WordWrap"));
                    break;
                default:
                    throw new NotSupportedException(string.Format("DecodeParseString: default case 0x{0:X}", b));
            }
            return target;
        }

        protected override Statement ResourceRoutines()
        {
            var subOp = ReadByte();
            var exp = new MethodInvocation("ResourceRoutines");
            var resId = Pop();
            switch (subOp)
            {
                case 0x3C:               // Dummy case
                    {
                    }
                    break;
                case 0x3D:               // SO_HEAP_LOAD_COSTUME Load costume to heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadCostume")).AddArgument(resId);
                    }
                    break;
                case 0x3E:               // SO_HEAP_LOAD_OBJECT Load object to heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadObjectToHeap")).AddArgument(resId);
                    }
                    break;
                case 0x3F:               // SO_HEAP_LOAD_ROOM Load room to heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadRoom")).AddArgument(resId);
                    }
                    break;
                case 0x40:               // SO_HEAP_LOAD_SCRIPT Load script to heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadScript")).AddArgument(resId);
                    }
                    break;
                case 0x41:               // SO_HEAP_LOAD_SOUND Load sound to heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadSound")).AddArgument(resId);
                    }
                    break;
                case 0x42:               // SO_HEAP_LOCK_COSTUME Lock costume in heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LockCostume")).AddArgument(resId);
                    }
                    break;
                case 0x43:               // SO_HEAP_LOCK_ROOM Lock room in heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LockRoom")).AddArgument(resId);
                    }
                    break;
                case 0x44:               // SO_HEAP_LOCK_SCRIPT Lock script in heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LockScript")).AddArgument(resId);
                    }
                    break;
                case 0x45:               // SO_HEAP_LOCK_SOUND Lock sound in heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LockSound")).AddArgument(resId);
                    }
                    break;
                case 0x46:               // SO_HEAP_UNLOCK_COSTUME Unlock costume
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockCostume")).AddArgument(resId);
                    }
                    break;
                case 0x47:               // SO_HEAP_UNLOCK_ROOM Unlock room
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockRoom")).AddArgument(resId);
                    }
                    break;
                case 0x48:               // SO_HEAP_UNLOCK_SCRIPT Unlock script
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockScript")).AddArgument(resId);
                    }
                    break;
                case 0x49:               // SO_UNLOCKSound
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockSound")).AddArgument(resId);
                    }
                    break;
                
                case 0x4A:               // SO_HEAP_NUKE_COSTUME Remove costume from heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeCostume")).AddArgument(resId);
                    }
                    break;
                case 0x4B:               // SO_HEAP_NUKE_ROOM Remove room from heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeRoom")).AddArgument(resId);
                        break;
                    }
                case 0x4C:               // SO_HEAP_NUKE_SCRIPT Remove script from heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeRoom")).AddArgument(resId);
                        break;
                    }
                case 0x4D:               // SO_HEAP_NUKE_SOUND Remove sound from heap
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeSound")).AddArgument(resId);
                        break;
                    }
                default:
                    throw new NotSupportedException(string.Format("ResourceRoutines: default case {0}", subOp));
            }
            return exp.ToStatement();
        }

        protected override Statement DimArray()
        {
            var subOp = ReadByte();
            var array = ReadWord();

            switch (subOp)
            {
                case 0x0A:               // SO_ARRAY_SCUMMVAR
                    return new MethodInvocation("DefineArray").AddArguments(array.ToLiteral(), "int".ToLiteral(), 0.ToLiteral(), Pop()).ToStatement();
                case 0x0B:               // SO_BIT_ARRAY
                    return new MethodInvocation("DefineArray").AddArguments(array.ToLiteral(), "string".ToLiteral(), 0.ToLiteral(), Pop()).ToStatement();
                case 0x0C:               // SO_ARRAY_UNDIM
                    return new MethodInvocation("NukeArray").AddArgument(array).ToStatement();
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }
        }

        protected override Statement ArrayOps()
        {
            var subOp = ReadByte();
            var array = ReadWord();

            var exp = new MethodInvocation("ArrayOps");

            switch (subOp)
            {
                case 0x14:               // SO_ASSIGN_STRING
                    {
                        var index = Pop();
                        var text = ReadCharacters();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetString")).AddArguments(array.ToLiteral(), index, text);
                    }
                    break;
                case 0x15:               // SO_ASSIGN_INT_LIST
                    {
                        var b = Pop();
                        var c = Pop();
                        var d = ReadVariable(array);
                        exp = new MethodInvocation(new MemberAccess(exp, "SetInt")).AddArguments(array.ToLiteral(), d, b, c);
                    }
                    break;
                case 0x16:               // SO_ASSIGN_2DIM_LIST
                    {
                        var b = Pop();
                        var len = GetStackList(128);
                        var d = ReadVariable(array);
                        var c = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetInt2D")).AddArguments(array.ToLiteral(), d, b, c, len);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("o6_arrayOps: default case {0} (array {1})", subOp, array));
            }
            return exp.ToStatement();
        }

        protected override Statement RoomOps()
        {
            var subOp = ReadByte();
            var exp = new MethodInvocation("RoomOps");
            switch (subOp)
            {
                case 0x52:               // SO_ROOM_PALETTE Set room palette
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetPalette")).AddArguments(d, a, b, c);
                    }
                    break;
                case 0x57:      // SO_ROOM_FADE Fade room
                    {
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "FadeRoom")).AddArguments(a);
                    }
                    break;
                case 0x58:      // SO_ROOM_RGB_INTENSITY Set room color intensity
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "DarkenPalette")).AddArguments(a, b, c, d, e);
                    }
                    break;
                case 0x59:      // SO_ROOM_TRANSFORM Transform room
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "RoomTransform")).AddArguments(a, b, c, d);
                    }
                    break;
                case 0x5C:      // SO_ROOM_NEW_PALETTE New palette
                    { 
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetCurrentPalette")).AddArguments(a);
                    }
                    break;
                case 0x5D:      // SO_ROOM_SAVE_GAME Save game
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "SaveGame"));
                    }
                    break;
                case 0x5E:      // SO_ROOM_LOAD_GAME Load game
                    {
                        var saveSound = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadGame")).AddArgument(saveSound);
                    }
                    break;
                case 0x5F:      // SO_ROOM_SATURATION Set saturation of room colors
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "DesaturatePalette")).AddArguments(a, b, c, d, e);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("RoomOps: default case {0:X2}", subOp));
            }
            return exp.ToStatement();
        }

        protected override Statement VerbOps()
        {
            var subOp = ReadByte();
            if (subOp == 0x96)
            {
                return new BinaryExpression(CurrentVerb, Operator.Assignment, Pop()).ToStatement();
            }
            var verb = (Expression)CurrentVerb;
            switch (subOp)
            {
                case 0x97:      // SO_VERB_NEW New verb
                    verb = new MethodInvocation(new MemberAccess(verb, "New"));
                    break;
                case 0x98:      // SO_VERB_DELETE Delete verb
                    verb = new MethodInvocation(new MemberAccess(verb, "Delete"));
                    break;
                case 0x99:      // SO_VERB_NAME Set verb name
                    {
                        var name = ReadCharacters();
                        verb = new MethodInvocation(new MemberAccess(verb, "SetName")).AddArgument(name);
                    }
                    break;
                case 0x9A:      // SO_VERB_AT Set verb (X,Y) placement
                    {
                        var y = Pop();
                        var x = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "At")).AddArguments(x, y);
                    }
                    break;
                case 0x9B:      // SO_VERB_ON Turn verb on
                    verb = new MethodInvocation(new MemberAccess(verb, "On"));
                    break;
                case 0x9C:      // SO_VERB_OFF Turn verb off
                    verb = new MethodInvocation(new MemberAccess(verb, "Off"));
                    break;
                case 0x9D:      // SO_VERB_COLOR Set verb color
                    verb = new MethodInvocation(new MemberAccess(verb, "Color")).AddArgument(Pop());
                    break;
                case 0x9E:      // SO_VERB_HICOLOR Set verb highlighted color
                    verb = new MethodInvocation(new MemberAccess(verb, "HilightedColor")).AddArgument(Pop());
                    break;
                case 0xA0:      // SO_VERB_DIMCOLOR Set verb dimmed (disabled) color
                    verb = new MethodInvocation(new MemberAccess(verb, "DimColor")).AddArgument(Pop());
                    break;
                case 0xA1:      // SO_VERB_DIM
                    verb = new MethodInvocation(new MemberAccess(verb, "Dim"));
                    break;
                case 0xA2:      // SO_VERB_KEY Set keypress to associate with verb
                    verb = new MethodInvocation(new MemberAccess(verb, "Key")).AddArgument(Pop());
                    break;
                case 0xA3:      // SO_VERB_IMAGE Set verb image
                    {
                        var b = Pop();
                        var a = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "SetImage")).AddArguments(a, b);
                    }
                    break;
                case 0xA4:      // SO_VERB_NAME_STR Set verb name
                    {
                        var a = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "Name")).AddArguments(a, ReadCharacters());
                    }
                    break;
                case 0xA5:      // SO_VERB_CENTER Center verb
                    verb = new MethodInvocation(new MemberAccess(verb, "Center"));
                    break;
                case 0xA6:      // SO_VERB_CHARSET Choose charset for verb
                    verb = new MethodInvocation(new MemberAccess(verb, "Charset")).AddArgument(Pop());
                    break;
                case 0xA7:      // SO_VERB_LINE_SPACING Choose linespacing for verb
                    verb = new MethodInvocation(new MemberAccess(verb, "LineSpacing")).AddArgument(Pop());
                    break;
                default:
                    throw new NotSupportedException(string.Format("VerbOps: default case {0}", subOp));
            }
            return verb.ToStatement();
        }

        protected override Statement ActorOps()
        {
            var subOp = ReadByte();
            if (subOp == 0x7A)
            {
                return new BinaryExpression(CurrentActor, Operator.Assignment, Pop()).ToStatement();
            }

            var actor = (Expression)CurrentActor;

            switch (subOp)
            {
                case 0x64:                // SO_ACTOR_COSTUME Set actor costume
                    actor = new MethodInvocation(new MemberAccess(actor, "SetCostume")).AddArgument(Pop());
                    break;
                case 0x65:                // SO_ACTOR_STEP_DIST Set actor width of steps
                    {
                        var j = Pop();
                        var i = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "WalkSpeed")).AddArguments(i, j);
                    }
                    break;
                case 0x67:                // SO_ACTOR_ANIMATION_DEFAULT Set actor animation to default
                    actor = new MethodInvocation(new MemberAccess(actor, "Default"));
                    break;
                case 0x68:      // SO_ACTOR_ANIMATION_INIT Initialize animation
                    {
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "Init")).AddArgument(a);
                    }
                    break;
                case 0x69:      // SO_ACTOR_ANIMATION_TALK Set actor animation to talk animation
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetTalkAnim")).AddArguments(a, b);
                    }
                    break;
                case 0x6A:      // SO_ACTOR_ANIMATION_WALK Set actor animation to walk animation
                    {
                        var f = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetWalkFrame")).AddArguments(f);
                    }
                    break;
                case 0x6B:      // SO_ACTOR_ANIMATION_STAND Set actor animation to standing animation
                    {
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetStandAnim")).AddArgument(a);
                    }
                    break;
                case 0x6C:      // SO_ACTOR_ANIMATION_SPEED Set speed of animation
                    {
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetSpeed")).AddArgument(a);
                    }
                    break;
                case 0x6D:      // SO_ACTOR_DEFAULT
                    {
                        actor = new MethodInvocation(new MemberAccess(actor, "InitDefault"));
                    }
                    break;
                case 0x6E:      // SO_ACTOR_ELEVATION
                    actor = new MethodInvocation(new MemberAccess(actor, "Elevation")).AddArgument(Pop());
                    break;
                case 0x6F:      // SO_ACTOR_PALETTE Set actor palette
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetPalette")).AddArguments(a, b);
                    }
                    break;
                case 0x70:      // SO_ACTOR_TALK_COLOR Set actor talk color
                    actor = new MethodInvocation(new MemberAccess(actor, "TalkColor")).AddArgument(Pop());
                    break;
                case 0x71:      // SO_ACTOR_NAME Set name of actor
                    actor = new MethodInvocation(new MemberAccess(actor, "Name")).AddArgument(ReadCharacters());
                    break;
                case 0x72:      // SO_ACTOR_WIDTH Set width of actor
                    actor = new MethodInvocation(new MemberAccess(actor, "Width")).AddArgument(Pop());
                    break;
                case 0x73:      // SO_ACTOR_SCALE Set scaling of actor
                    actor = new MethodInvocation(new MemberAccess(actor, "Scale")).AddArgument(Pop());
                    break;
                case 0x74:      // SO_ACTOR_NEVER_ZCLIP
                    actor = new MethodInvocation(new MemberAccess(actor, "NoZClip"));
                    break;
                case 0x75:      // SO_ACTOR_ALWAYS_ZCLIP
                    actor = new MethodInvocation(new MemberAccess(actor, "ZClip")).AddArgument(Pop());
                    break;
                case 0x76:      // SO_ACTOR_IGNORE_BOXES Make actor ignore boxes
                    actor = new MethodInvocation(new MemberAccess(actor, "IgnoreBoxes"));
                    break;
                case 0x77:      // SO_ACTOR_FOLLOW_BOXES Make actor follow boxes
                    actor = new MethodInvocation(new MemberAccess(actor, "FollowBoxes"));
                    break;
                case 0x78:      // SO_ACTOR_SPECIAL_DRAW
                    actor = new MethodInvocation(new MemberAccess(actor, "SpecialDraw")).AddArgument(Pop());
                    break;
                case 0x79:      // SO_ACTOR_TEXT_OFFSET Set text offset relative to actor
                    {
                        var y = Pop();
                        var x = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "Offset")).AddArguments(x, y);
                    }
                    break;
                case 0x7B:      // SO_ACTOR_VARIABLE Set actor variable
                    {
                        var b = Pop();
                        var a = Pop();
                        actor = new MethodInvocation(new MemberAccess(actor, "SetAnimVar")).AddArguments(a, b);
                    }
                    break;
                case 0x7C:      // SO_ACTOR_IGNORE_TURNS_ON Make actor ignore turns
                    actor = new MethodInvocation(new MemberAccess(actor, "IgnoreTurns")).AddArgument(true);
                    break;
                case 0x7D:      // SO_ACTOR_IGNORE_TURNS_OFF Make actor follow turns
                    actor = new MethodInvocation(new MemberAccess(actor, "IgnoretTurns")).AddArgument(false);
                    break;
                case 0x7E:      // SO_ACTOR_NEW New actor
                    actor = new MethodInvocation(new MemberAccess(actor, "New"));
                    break;
                case 0x7F:      // SO_ACTOR_DEPTH Set actor Z position
                    actor = new MethodInvocation(new MemberAccess(actor, "Depth")).AddArgument(Pop());
                    break;
                case 0x80:      // SO_ACTOR_STOP
                    actor = new MethodInvocation(new MemberAccess(actor, "Stop"));
                    break;
                case 0x81:      // SO_ACTOR_FACE Make actor face angle
                    actor = new MethodInvocation(new MemberAccess(actor, "SetDirection")).AddArgument(Pop());
                    break;
                case 0x82:      // SO_ACTOR_TURN Turn actor
                    actor = new MethodInvocation(new MemberAccess(actor, "Turn")).AddArgument(Pop());
                    break;
                case 0x83:      // SO_ACTOR_WALK_SCRIPT Set walk script for actor?
                    actor = new MethodInvocation(new MemberAccess(actor, "WalkScript")).AddArguments(Pop());
                    break;
                case 0x84:      // SO_ACTOR_TALK_SCRIPT Set talk script for actor?
                    actor = new MethodInvocation(new MemberAccess(actor, "TalkScript")).AddArguments(Pop());
                    break;
                case 0x85:      // SO_ACTOR_WALK_PAUSE
                    actor = new MethodInvocation(new MemberAccess(actor, "Pause"));
                    break;
                case 0x86:      // SO_ACTOR_WALK_RESUME
                    actor = new MethodInvocation(new MemberAccess(actor, "Resume"));
                    break;
                case 0x87:      // SO_ACTOR_VOLUME Set volume of actor speech
                    actor = new MethodInvocation(new MemberAccess(actor, "Volume")).AddArgument(Pop());
                    break;
                case 0x88:      // SO_ACTOR_FREQUENCY Set frequency of actor speech
                    actor = new MethodInvocation(new MemberAccess(actor, "Frequency")).AddArgument(Pop());
                    break;
                case 0x89:      // SO_ACTOR_PAN
                    actor = new MethodInvocation(new MemberAccess(actor, "Pan")).AddArgument(Pop());
                    break;
                default:
                    throw new NotSupportedException(string.Format("o6_actorOps: default case {0}", subOp));
            }
            return actor.ToStatement();
        }

        protected override Statement Dim2DimArray()
        {
            var subOp = ReadByte();
            var array = ReadWord();
            
            switch (subOp)
            {
                case 0x0A:      // SO_ARRAY_SCUMMVAR
                    {
                        var b = Pop();
                        var a = Pop();
                        return new MethodInvocation("DefineArray").AddArguments(array.ToLiteral(), "int".ToLiteral(), a, b).ToStatement();
                    }
                case 0x0B:      // SO_ARRAY_STRING
                    {
                        var b = Pop();
                        var a = Pop();
                        return new MethodInvocation("DefineArray").AddArguments(array.ToLiteral(), "string".ToLiteral(), a, b).ToStatement();
                    }
                case 0x0C:      // SO_ARRAY_UNDIM
                    return new MethodInvocation("NukeArray").AddArgument(array.ToLiteral()).ToStatement();
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }
        }

        protected override Statement Wait()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 0x1E:      // SO_WAIT_FOR_ACTOR Wait for actor (to finish current action?)
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForActor").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                case 0x1F:      // SO_WAIT_FOR_MESSAGE Wait for message
                    return new MethodInvocation("WaitForMessage").ToStatement();
                case 0x20:      // SO_WAIT_FOR_CAMERA Wait for camera (to finish current action?)
                    return new MethodInvocation("WaitForCamera").ToStatement();
                case 0x21:      // SO_WAIT_FOR_SENTENCE
                    return new MethodInvocation("WaitForSentence").ToStatement();
                case 0x22:      // SO_WAIT_FOR_ANIMATION
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForAnimation").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                case 0x23:      // SO_WAIT_FOR_TURN
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForTurn").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                default:
                    throw new NotSupportedException(string.Format("Wait: default case 0x{0:X}", subOp));
            }
        }

        protected override Statement KernelSetFunctions()
        {
            return new MethodInvocation("KernelSetFunctions").AddArgument(GetStackList(30)).ToStatement();
        }

        protected override Statement KernelGetFunctions()
        {
            return new MethodInvocation("KernelGetFunctions").AddArgument(GetStackList(30)).ToStatement();
        }

        Statement BlastText()
        {
            return DecodeParseString(new MethodInvocation("BlastText"), false).ToStatement();
        }

        Statement CameraOps()
        {
            var subOp = ReadByte();

            switch (subOp)
            {
                case 0x32:      // SO_CAMERA_PAUSE
                    return new MethodInvocation("CameraPause").ToStatement();
                case 0x33:      // SO_CAMERA_RESUME
                    return new MethodInvocation("CameraResume").ToStatement();
                default:
                    throw new InvalidOperationException(string.Format("CameraOps8: default case 0x{0:X}", subOp));
            }
        }

        Statement StartVideo()
        {
            return new MethodInvocation("StartVideo").AddArgument(ReadCharacters()).ToStatement();
        }

        Statement GetActorChore()
        {
            return Push(new MemberAccess(GetActor(Pop()), "Frame"));
        }

        Statement GetActorZPlane()
        {
            return Push(new MemberAccess(GetActor(Pop()), "ZPlane"));
        }

        Statement GetObjectImageX()
        {
            return Push(new MemberAccess(Object(Pop()), "X"));
        }

        Statement GetObjectImageY()
        {
            return Push(new MemberAccess(Object(Pop()), "Y"));
        }

        Statement GetObjectImageWidth()
        {
            return Push(new MemberAccess(Object(Pop()), "Width"));
        }

        Statement GetObjectImageHeight()
        {
            return Push(new MemberAccess(Object(Pop()), "Height"));
        }

        Statement GetStringWidth()
        {
            return Push(new MethodInvocation("GetStringWidth").AddArgument(ReadCharacters()));
        }

    }
}


