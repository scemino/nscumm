//
//  ScummEngine7.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine8 : ScummEngine7
    {
        int _keyScriptKey, _keyScriptNo;

        int VariableSync;
        int VariableLanguage;

        public ScummEngine8(GameSettings game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
        }

        protected override void SetupVars()
        {
            VariableRoomWidth = 1;
            VariableRoomHeight = 2;

            VariableMouseX = 3;
            VariableMouseY = 4;
            VariableVirtualMouseX = 5;
            VariableVirtualMouseY = 6;

            VariableCursorState = 7;
            VariableUserPut = 8;

            VariableCameraPosX = 9;
            VariableCameraPosY = 10;
            VariableCameraDestX = 11;
            VariableCameraDestY = 12;
            VariableCameraFollowedActor = 13;

            VariableTalkActor = 14;
            VariableHaveMessage = 15;

            VariableLeftButtonDown = 16;
            VariableRightButtonDown = 17;
            VariableLeftButtonHold = 18;
            VariableRightButtonHold = 19;

            VariableTimeDateYear = 24;
            VariableTimeDateMonth = 25;
            VariableTimeDateDay = 26;
            VariableTimeDateHour = 27;
            VariableTimeDateMinute = 28;
            VariableTimeDateSecond = 29;

            VariableOverride = 30;
            VariableRoom = 31;
            VariableNewRoom = 32;
            VariableWalkToObject = 33;
            VariableTimer = 34;

            VariableVoiceMode = 39; // 0 is voice, 1 is voice+text, 2 is text only
            VariableGameLoaded = 40;
            VariableLanguage = 41;

            VariableCurrentDisk = 42;
            VariableMusicBundleLoaded = 45;
            VariableVoiceBundleLoaded = 46;

            VariableScrollScript = 50;
            VariableEntryScript = 51;
            VariableEntryScript2 = 52;
            VariableExitScript = 53;
            VariableExitScript2 = 54;
            VariableVerbScript = 55;
            VariableSentenceScript = 56;
            VariableInventoryScript = 57;
            VariableCutSceneStartScript = 58;
            VariableCutSceneEndScript = 59;

            VariableCutSceneExitKey = 62;

            VariablePauseKey = 64;
            VariableMainMenu = 65;
            VariableVersionKey = 66;
            VariableTalkStopKey = 67;

            VariableCustomScaleTable = 111;

            VariableTimerNext = 112;
            VariableTimer1 = 113;
            VariableTimer2 = 114;
            VariableTimer3 = 115;

            VariableCameraMinX = 116;
            VariableCameraMaxX = 117;
            VariableCameraMinY = 118;
            VariableCameraMaxY = 119;
            VariableCameraSpeedX = 120;
            VariableCameraSpeedY = 121;
            VariableCameraAccelX = 122;
            VariableCameraAccelY = 123;
            VariableCameraThresholdX = 124;
            VariableCameraThresholdY = 125;

            VariableEgo = 126;

            VariableDefaultTalkDelay = 128;
            VariableCharIncrement = 129;

            VariableDebugMode = 130;
            VariableKeyPress = 132;
            VariableBlastAboveText = 133;
            VariableSync = 134;
        }

        protected override void ResetScummVars()
        {
            base.ResetScummVars();

            Variables[VariableCurrentDisk.Value] = 1;
        }

        protected override uint ReadWord()
        {
            var word = BitConverter.ToUInt32(_currentScriptData, CurrentPos);
            CurrentPos += 4;
            return word;
        }

        protected override int ReadWordSigned()
        {
            var word = BitConverter.ToInt32(_currentScriptData, CurrentPos);
            CurrentPos += 4;
            return word;
        }

        protected override int ReadVariable(uint var)
        {
            if ((var & 0xF0000000) == 0)
            {
                //                Debug.WriteLine("ReadVar({0})", var);
                ScummHelper.AssertRange(0, var, Variables.Length - 1, "variable");
                return Variables[var];
            }

            if ((var & 0x80000000) != 0)
            {
                var &= 0x7FFFFFFF;
                //                Debug.WriteLine("Read BitVars({0})", var);
                ScummHelper.AssertRange(0, var, _bitVars.Length - 1, "bit variable (reading)");
                return _bitVars[(int)var] ? 1 : 0;
            }

            if ((var & 0x40000000) != 0)
            {
                var &= 0xFFFFFFF;
                //                Debug.WriteLine("Read LocalVariables({0})", var);
                ScummHelper.AssertRange(0, var, 25, "local variable (reading)");
                return Slots[CurrentScript].LocalVariables[var];
            }

            throw new NotSupportedException("Illegal varbits (r)");
        }

        protected override void WriteVariable(uint index, int value)
        {
            if ((index & 0xF0000000) == 0)
            {
                //                Debug.WriteLine("WriteVar({0}, {1})", var, value);
                ScummHelper.AssertRange(0, index, Variables.Length - 1, "variable (writing)");

                //                if (var == VAR_CHARINC)
                //                {
                //                    // Did the user override the talkspeed manually? Then use that.
                //                    // Otherwise, use the value specified by the game script.
                //                    // Note: To determine whether there was a user override, we only
                //                    // look at the target specific settings, assuming that any global
                //                    // value is likely to be bogus. See also bug #2251765.
                //                    if (ConfMan.hasKey("talkspeed", _targetName))
                //                    {
                //                        value = getTalkSpeed();
                //                    }
                //                    else
                //                    {
                //                        // Save the new talkspeed value to ConfMan
                //                        setTalkSpeed(value);
                //                    }
                //                }

                Variables[index] = value;

                return;
            }

            if ((index & 0x80000000) != 0)
            {
                index &= 0x7FFFFFFF;
                //                Debug.WriteLine("Write BitVars({0}, {1})", var, value);
                ScummHelper.AssertRange(0, index, _bitVars.Length - 1, "bit variable (writing)");

                _bitVars[(int)index] = value != 0;
                return;
            }

            if ((index & 0x40000000) != 0)
            {
                index &= 0xFFFFFFF;
                //                Debug.WriteLine("Write LocalVariables({0}, {1})", var, value);
                ScummHelper.AssertRange(0, index, 25, "local variable (writing)");
                Slots[CurrentScript].LocalVariables[index] = value;
                return;
            }

            throw new NotSupportedException("Illegal varbits (w)");
        }

        protected override void DecodeParseString(int m, int n)
        {
            byte b = ReadByte();

            switch (b)
            {
                case 0xC8:      // SO_PRINT_BASEOP
                    String[m].LoadDefault();
                    if (n != 0)
                        _actorToPrintStrFor = Pop();
                    break;
                case 0xC9:      // SO_PRINT_END
                    String[m].SaveDefault();
                    break;
                case 0xCA:      // SO_PRINT_AT
                    {
                        var y = Pop();
                        var x = Pop();
                        String[m].Position = new Point(x, y);
                        String[m].Overhead = false;
                    }
                    break;
                case 0xCB:      // SO_PRINT_COLOR
                    String[m].Color = (byte)Pop();
                    break;
                case 0xCC:      // SO_PRINT_CENTER
                    String[m].Center = true;
                    String[m].Overhead = false;
                    break;
                case 0xCD:      // SO_PRINT_CHARSET Set print character set
                    String[m].Charset = (byte)Pop();
                    break;
                case 0xCE:      // SO_PRINT_LEFT
                    String[m].Wrapping = false;
                    String[m].Overhead = false;
                    break;
                case 0xCF:      // SO_PRINT_OVERHEAD
                    String[m].Overhead = true;
                    String[m].NoTalkAnim = false;
                    break;
                case 0xD0:      // SO_PRINT_MUMBLE
                    String[m].NoTalkAnim = true;
                    break;
                case 0xD1:      // SO_PRINT_STRING
                    PrintString(m, ReadCharacters());
                    break;
                case 0xD2:      // SO_PRINT_WRAP Set print wordwrap
                    String[m].Wrapping = true;
                    String[m].Overhead = false;
                    break;
                default:
                    throw new NotSupportedException(string.Format("DecodeParseString: default case 0x{0:X}", b));
            }
        }

        [OpCode(0x02)]
        protected override void PushWordVar()
        {
            base.PushWordVar();
        }

        [OpCode(0x03)]
        protected override void WordArrayRead(int @base)
        {
            base.WordArrayRead(@base);
        }

        [OpCode(0x04)]
        protected override void WordArrayIndexedRead(int index, int @base)
        {
            base.WordArrayIndexedRead(index, @base);
        }

        [OpCode(0x05)]
        protected override void Dup(int value)
        {
            base.Dup(value);
        }

        [OpCode(0x06)]
        protected override void Pop6()
        {
            base.Pop6();
        }

        [OpCode(0x07)]
        protected override void Not(int value)
        {
            base.Not(value);
        }

        [OpCode(0x08)]
        protected override void Eq(int a, int b)
        {
            base.Eq(a, b);
        }

        [OpCode(0x09)]
        protected override void NEq(int a, int b)
        {
            base.NEq(a, b);
        }

        [OpCode(0x0A)]
        protected override void Gt(int a, int b)
        {
            base.Gt(a, b);
        }

        [OpCode(0x0B)]
        protected override void Lt(int a, int b)
        {
            base.Lt(a, b);
        }

        [OpCode(0x0C)]
        protected override void Le(int a, int b)
        {
            base.Le(a, b);
        }

        [OpCode(0x0D)]
        protected override void Ge(int a, int b)
        {
            base.Ge(a, b);
        }

        [OpCode(0x0E)]
        protected override void Add(int a, int b)
        {
            base.Add(a, b);
        }

        [OpCode(0x0F)]
        protected override void Sub(int a, int b)
        {
            base.Sub(a, b);
        }

        [OpCode(0x10)]
        protected override void Mul(int a, int b)
        {
            base.Mul(a, b);
        }

        [OpCode(0x11)]
        protected override void Div(int a, int b)
        {
            base.Div(a, b);
        }

        [OpCode(0x12)]
        protected override void Land(int a, int b)
        {
            base.Land(a, b);
        }

        [OpCode(0x13)]
        protected override void Lor(int a, int b)
        {
            base.Lor(a, b);
        }

        [OpCode(0x14)]
        protected override void BAnd(int a, int b)
        {
            base.BAnd(a, b);
        }

        [OpCode(0x15)]
        protected override void Bor(int a, int b)
        {
            base.Bor(a, b);
        }

        [OpCode(0x16)]
        void Mod()
        {
            var a = Pop();
            Push(Pop() % a);
        }

        [OpCode(0x64)]
        protected override void If(int condition)
        {
            base.If(condition);
        }

        [OpCode(0x65)]
        protected override void IfNot(int condition)
        {
            base.IfNot(condition);
        }

        [OpCode(0x66)]
        protected override void Jump()
        {
            base.Jump();
        }

        [OpCode(0x67)]
        protected override void BreakHere()
        {
            base.BreakHere();
        }

        [OpCode(0x68)]
        protected override void DelayFrames()
        {
            base.DelayFrames();
        }

        [OpCode(0x69)]
        void Wait8()
        {
            int actnum;
            int offs = -2;
            byte subOp = ReadByte();

            switch (subOp)
            {
                case 0x1E:      // SO_WAIT_FOR_ACTOR Wait for actor (to finish current action?)
                    {
                        offs = ReadWordSigned();
                        actnum = Pop();
                        var a = Actors[actnum];
                        if (a.IsInCurrentRoom && a.Moving != 0)
                            break;
                    }
                    return;
                case 0x1F:      // SO_WAIT_FOR_MESSAGE Wait for message
                    if (Variables[VariableHaveMessage.Value] != 0)
                        break;
                    return;
                case 0x20:      // SO_WAIT_FOR_CAMERA Wait for camera (to finish current action?)
                    if (Camera.DestinationPosition != Camera.CurrentPosition)
                        break;
                    return;
                case 0x21:      // SO_WAIT_FOR_SENTENCE
                    if (SentenceNum != 0)
                    {
                        if (Sentence[SentenceNum - 1].IsFrozen && !IsScriptInUse(Variables[VariableSentenceScript.Value]))
                            return;
                        break;
                    }
                    if (!IsScriptInUse(Variables[VariableSentenceScript.Value]))
                        return;
                    break;
                case 0x22:      // SO_WAIT_FOR_ANIMATION
                    {
                        offs = ReadWordSigned();
                        actnum = Pop();
                        var a = Actors[actnum];
                        if (a.IsInCurrentRoom && a.NeedRedraw)
                            break;
                    }
                    return;
                case 0x23:      // SO_WAIT_FOR_TURN
                    {
                        offs = ReadWordSigned();
                        actnum = Pop();
                        var a = Actors[actnum];
                        if (a.IsInCurrentRoom && a.Moving.HasFlag(MoveFlags.Turn))
                            break;
                    }
                    return;
                default:
                    throw new NotSupportedException(string.Format("Wait8: default case 0x{0:X}", subOp));
            }

            CurrentPos += offs;
            BreakHere();
        }

        [OpCode(0x6A)]
        protected override void Delay(int delay)
        {
            base.Delay(delay);
        }

        [OpCode(0x6B)]
        protected override void DelaySeconds(int seconds)
        {
            base.DelaySeconds(seconds);
        }

        [OpCode(0x6C)]
        protected override void DelayMinutes(int minutes)
        {
            base.DelayMinutes(minutes);
        }

        [OpCode(0x6D)]
        protected override void WriteWordVar(int value)
        {
            base.WriteWordVar(value);
        }

        [OpCode(0x6E)]
        protected override void WordVarInc()
        {
            base.WordVarInc();
        }

        [OpCode(0x6F)]
        protected override void WordVarDec()
        {
            base.WordVarDec();
        }

        [OpCode(0x70)]
        protected override void DimArray()
        {
            byte subOp = ReadByte();
            var array = ReadWord();

            switch (subOp)
            {
                case 0x0A:      // SO_ARRAY_SCUMMVAR
                    DefineArray(array, ArrayType.IntArray, 0, Pop());
                    break;
                case 0x0B:      // SO_ARRAY_STRING
                    DefineArray(array, ArrayType.StringArray, 0, Pop());
                    break;
                case 0x0C:      // SO_ARRAY_UNDIM
                    NukeArray(array);
                    break;
                default:
                    throw new NotSupportedException(string.Format("DimArray8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0x71)]
        protected override void WordArrayWrite(int @base, int value)
        {
            base.WordArrayWrite(@base, value);
        }

        [OpCode(0x72)]
        protected override void WordArrayInc(int @base)
        {
            base.WordArrayInc(@base);
        }

        [OpCode(0x73)]
        protected override void WordArrayDec(int @base)
        {
            base.WordArrayDec(@base);
        }

        [OpCode(0x74)]
        void Dim2DimArray()
        {
            byte subOp = ReadByte();
            var array = ReadWord();
            int a, b;

            switch (subOp)
            {
                case 0x0A:      // SO_ARRAY_SCUMMVAR
                    b = Pop();
                    a = Pop();
                    DefineArray(array, ArrayType.IntArray, a, b);
                    break;
                case 0x0B:      // SO_ARRAY_STRING
                    b = Pop();
                    a = Pop();
                    DefineArray(array, ArrayType.StringArray, a, b);
                    break;
                case 0x0C:      // SO_ARRAY_UNDIM
                    NukeArray(array);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Dim2dimArray8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0x75)]
        protected override void WordArrayIndexedWrite(int index, int @base, int value)
        {
            base.WordArrayIndexedWrite(index, @base, value);
        }

        [OpCode(0x76)]
        protected override void ArrayOps()
        {
            byte subOp = ReadByte();
            var array = ReadWord();

            switch (subOp)
            {
                case 0x14:      // SO_ASSIGN_STRING
                    {
                        var b = Pop();
                        var text = ReadCharacters();
                        var data = DefineArray(array, ArrayType.StringArray, 0, text.Length + 1);
                        for (int i = 0; i < text.Length; i++)
                        {
                            data.Write(b + i, text[i]);
                        }
                    }
                    break;
                case 0x15:      // SO_ASSIGN_SCUMMVAR_LIST
                    {
                        var b = Pop();
                        var list = GetStackList(128);
                        var d = ReadVariable(array);
                        if (d == 0)
                        {
                            DefineArray(array, ArrayType.IntArray, 0, b + list.Length);
                        }
                        for (int i = 0; i < list.Length; i++)
                        {
                            WriteArray(array, 0, b + i, list[i]);
                        }
                    }
                    break;
                case 0x16:      // SO_ASSIGN_2DIM_LIST
                    {
                        var b = Pop();
                        var list = GetStackList(128);
                        var d = ReadVariable(array);
                        if (d == 0)
                            throw new InvalidOperationException("Must DIM a two dimensional array before assigning");
                        var c = Pop();
                        for (int i = 0; i < list.Length; i++)
                        {
                            WriteArray(array, c, b + i, list[i]);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format("o8_arrayOps: default case 0x{0:X} (array {1})", subOp, array));
            }
        }

        [OpCode(0x79)]
        protected override void StartScript(int flags, int script, int[] args)
        {
            base.StartScript(flags, script, args);
        }

        [OpCode(0x7A)]
        protected override void StartScriptQuick(int script, int[] args)
        {
            base.StartScriptQuick(script, args);
        }

        [OpCode(0x7B)]
        protected override void StopObjectCode6()
        {
            base.StopObjectCode6();
        }

        [OpCode(0x7C)]
        protected override void StopScript6(int script)
        {
            base.StopScript6(script);
        }

        [OpCode(0x7D)]
        protected override void JumpToScript(int flags, int script, int[] args)
        {
            base.JumpToScript(flags, script, args);
        }

        [OpCode(0x7E)]
        protected override void Dummy()
        {
            base.Dummy();
        }

        [OpCode(0x7F)]
        protected override void StartObject(int flags, int script, byte entryp, int[] args)
        {
            base.StartObject(flags, script, entryp, args);
        }

        [OpCode(0x80)]
        protected override void StopObjectScript(ushort script)
        {
            base.StopObjectScript(script);
        }

        [OpCode(0x81)]
        protected override void Cutscene(int[] args)
        {
            base.Cutscene(args);
        }

        [OpCode(0x82)]
        protected override void EndCutscene()
        {
            base.EndCutscene();
        }

        [OpCode(0x83)]
        protected override void FreezeUnfreeze(int script)
        {
            base.FreezeUnfreeze(script);
        }

        [OpCode(0x84)]
        protected override void BeginOverride()
        {
            base.BeginOverride();
        }

        [OpCode(0x85)]
        protected override void EndOverride()
        {
            base.EndOverride();
        }

        [OpCode(0x86)]
        protected override void StopSentence()
        {
            base.StopSentence();
        }

        [OpCode(0x89)]
        protected override void SetClass(int obj, int[] args)
        {
            base.SetClass(obj, args);
        }

        [OpCode(0x8A)]
        protected override void SetState(int obj, int state)
        {
            base.SetState(obj, state);
        }

        [OpCode(0x8B)]
        protected override void SetOwner(int obj, int owner)
        {
            base.SetOwner(obj, owner);
        }

        [OpCode(0x8C)]
        protected override void PanCameraTo()
        {
            base.PanCameraTo();
        }

        [OpCode(0x8D)]
        protected override void ActorFollowCamera(int index)
        {
            base.ActorFollowCamera(index);
        }

        [OpCode(0x8E)]
        protected override void SetCameraAt()
        {
            base.SetCameraAt();
        }

        [OpCode(0x8F)]
        protected override void PrintActor()
        {
            base.PrintActor();
        }

        [OpCode(0x90)]
        protected override void PrintEgo()
        {
            base.PrintEgo();
        }

        [OpCode(0x91)]
        protected override void TalkActor(int actor)
        {
            base.TalkActor(actor);
        }

        [OpCode(0x92)]
        protected override void TalkEgo()
        {
            base.TalkEgo();
        }

        [OpCode(0x93)]
        protected override void PrintLine()
        {
            base.PrintLine();
        }

        [OpCode(0x94)]
        protected override void PrintText()
        {
            base.PrintText();
        }

        [OpCode(0x95)]
        protected override void PrintDebug()
        {
            base.PrintDebug();
        }

        [OpCode(0x96)]
        protected override void PrintSystem()
        {
            base.PrintSystem();
        }

        [OpCode(0x97)]
        void BlastText()
        {
            // Original V8 interpreter uses StringSlot 2 for o_blastText and 4 for o_printDebug.
            // Since slot 2 is already mapped to printDebug for V6 (see ScummEngine::printString()),
            // we just "swap" the slots, and use slot 4 here.
            DecodeParseString(4, 0);
        }

        [OpCode(0x98)]
        void DrawObject(int obj, int x, int y, int state)
        {
            SetObjectState(obj, state, x, y);
        }

        [OpCode(0x9C)]
        protected override void CursorCommand()
        {
            byte subOp = ReadByte();
            int a;

            switch (subOp)
            {
                case 0xDC:      // SO_CURSOR_ON Turn cursor on
                    _cursor.State = 1;
                    VerbMouseOver(0);
                    break;
                case 0xDD:      // SO_CURSOR_OFF Turn cursor off
                    _cursor.State = 0;
                    VerbMouseOver(0);
                    break;
                case 0xDE:      // SO_CURSOR_SOFT_ON Turn soft cursor on
                    _cursor.State++;
                    VerbMouseOver(0);
                    break;
                case 0xDF:      // SO_CURSOR_SOFT_OFF Turn soft cursor off
                    _cursor.State--;
                    VerbMouseOver(0);
                    break;
                case 0xE0:      // SO_USERPUT_ON
                    _userPut = 1;
                    break;
                case 0xE1:      // SO_USERPUT_OFF
                    _userPut = 0;
                    break;
                case 0xE2:      // SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;
                case 0xE3:      // SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;
                case 0xE4:      // SO_CURSOR_IMAGE Set cursor image
                    {
                        int idx = Pop();
                        int room, obj;
                        PopRoomAndObj(out room, out obj);
                        SetCursorFromImg(obj, room, idx);
                    }
                    break;
                case 0xE5:      // SO_CURSOR_HOTSPOT Set cursor hotspot
                    a = Pop();
                    SetCursorHotspot(new Point((short)Pop(), (short)a));
                    break;
                case 0xE6:      // SO_CURSOR_TRANSPARENT Set cursor transparent color
                    SetCursorTransparency(Pop());
                    break;
                case 0xE7:      // SO_CHARSET_SET
                    String[0].Default.Charset = (byte)Pop();
                    break;
                case 0xE8:      // SO_CHARSET_COLOR
                    var args = GetStackList(4);
                    // This opcode does nothing (confirmed with disasm)
                    break;
                case 0xE9:      // SO_CURSOR_PUT
                    {
                        int y = Pop();
                        int x = Pop();

                        // TODO:
                        //                        _system.warpMouse(x, y);
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format("CursorCommand: default case 0x{0:X}", subOp));
            }

            Variables[VariableCursorState.Value] = _cursor.State;
            Variables[VariableUserPut.Value] = _userPut;
        }

        [OpCode(0x9D)]
        protected override void LoadRoom(byte room)
        {
            base.LoadRoom(room);
        }

        [OpCode(0x9E)]
        protected override void LoadRoomWithEgo(int x, int y)
        {
            base.LoadRoomWithEgo(x, y);
        }

        [OpCode(0x9F)]
        protected override void WalkActorToObj(int index, int obj, int dist)
        {
            base.WalkActorToObj(index, obj, dist);
        }

        [OpCode(0xA0)]
        protected override void WalkActorTo(int index, int x, int y)
        {
            base.WalkActorTo(index, x, y);
        }

        [OpCode(0xA1)]
        protected override void PutActorAtXY(int actorIndex, int x, int y, int room)
        {
            base.PutActorAtXY(actorIndex, x, y, room);
        }

        [OpCode(0xA2)]
        protected override void PutActorAtObject()
        {
            base.PutActorAtObject();
        }

        [OpCode(0xA3)]
        protected override void FaceActor(int index, int obj)
        {
            base.FaceActor(index, obj);
        }

        [OpCode(0xA4)]
        protected override void AnimateActor(int index, int anim)
        {
            base.AnimateActor(index, anim);
        }

        [OpCode(0xA5)]
        void DoSentence8(byte verb, ushort objectA, ushort objectB)
        {
            base.DoSentence(verb, objectA, 0, objectB);
        }

        [OpCode(0xA6)]
        protected override void PickupObject()
        {
            base.PickupObject();
        }

        [OpCode(0xA7)]
        protected override void SetBoxFlags(int[] args, int value)
        {
            base.SetBoxFlags(args, value);
        }

        [OpCode(0xA8)]
        protected override void CreateBoxMatrix()
        {
            base.CreateBoxMatrix();
        }

        [OpCode(0xAA)]
        protected override void ResourceRoutines()
        {
            byte subOp = ReadByte();
            int resid = Pop();

            switch (subOp)
            {
                case 0x3C:      // Dummy case
                    break;
                case 0x3D:      // SO_HEAP_LOAD_COSTUME Load costume to heap
                    ResourceManager.LoadCostume(resid);
                    break;
                case 0x3E:      // SO_HEAP_LOAD_OBJECT Load object to heap
                    {
                        int room = GetObjectRoom(resid);
                        LoadFlObject(resid, room);
                    }
                    break;
                case 0x3F:      // SO_HEAP_LOAD_ROOM Load room to heap
                    ResourceManager.LoadRoom(resid);
                    break;
                case 0x40:      // SO_HEAP_LOAD_SCRIPT Load script to heap
                    ResourceManager.LoadScript(resid);
                    break;
                case 0x41:      // SO_HEAP_LOAD_SOUND Load sound to heap
                    ResourceManager.LoadSound(Sound.MusicType, resid);
                    break;

                case 0x42:      // SO_HEAP_LOCK_COSTUME Lock costume in heap
                    ResourceManager.LockCostume(resid);
                    break;
                case 0x43:      // SO_HEAP_LOCK_ROOM Lock room in heap
                    ResourceManager.LockRoom(resid);
                    break;
                case 0x44:      // SO_HEAP_LOCK_SCRIPT Lock script in heap
                    ResourceManager.LockScript(resid);
                    break;
                case 0x45:      // SO_HEAP_LOCK_SOUND Lock sound in heap
                    ResourceManager.LockSound(resid);
                    break;
                case 0x46:      // SO_HEAP_UNLOCK_COSTUME Unlock costume
                    ResourceManager.UnlockCostume(resid);
                    break;
                case 0x47:      // SO_HEAP_UNLOCK_ROOM Unlock room
                    ResourceManager.UnlockRoom(resid);
                    break;
                case 0x48:      // SO_HEAP_UNLOCK_SCRIPT Unlock script
                    ResourceManager.UnlockScript(resid);
                    break;
                case 0x49:      // SO_HEAP_UNLOCK_SOUND Unlock sound
                    ResourceManager.UnlockSound(resid);
                    break;
                case 0x4A:      // SO_HEAP_NUKE_COSTUME Remove costume from heap
                    ResourceManager.SetCostumeCounter(resid, 0x7F);
                    break;
                case 0x4B:      // SO_HEAP_NUKE_ROOM Remove room from heap
                    ResourceManager.SetRoomCounter(resid, 0x7F);
                    break;
                case 0x4C:      // SO_HEAP_NUKE_SCRIPT Remove script from heap
                    ResourceManager.SetScriptCounter(resid, 0x7F);
                    break;
                case 0x4D:      // SO_HEAP_NUKE_SOUND Remove sound from heap
                    ResourceManager.SetSoundCounter(resid, 0x7F);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("ResourceRoutines8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0xAB)]
        protected override void RoomOps()
        {
            byte subOp = ReadByte();
            int a, b, c, d, e;

            switch (subOp)
            {
                case 0x52:      // SO_ROOM_PALETTE Set room palette
                    d = Pop();
                    c = Pop();
                    b = Pop();
                    a = Pop();
                    SetPalColor(d, a, b, c);
                    break;
                case 0x57:      // SO_ROOM_FADE Fade room
                    a = Pop();
                    if (a != 0)
                    {
                        _switchRoomEffect = (byte)(a);
                        _switchRoomEffect2 = (byte)(a >> 8);
                    }
                    else
                    {
                        FadeIn(_newEffect);
                    }
                    break;
                case 0x58:      // SO_ROOM_RGB_INTENSITY Set room color intensity
                    e = Pop();
                    d = Pop();
                    c = Pop();
                    b = Pop();
                    a = Pop();
                    DarkenPalette(a, b, c, d, e);
                    break;
                case 0x59:      // SO_ROOM_TRANSFORM Transform room
                    d = Pop();
                    c = Pop();
                    b = Pop();
                    a = Pop();
                    PalManipulateInit(a, b, c, d);
                    break;
                case 0x5C:      // SO_ROOM_NEW_PALETTE New palette
                    a = Pop();
                    SetCurrentPalette(a);
                    break;
                case 0x5D:      // SO_ROOM_SAVE_GAME Save game
                    _saveSound = false;
                    _saveTemporaryState = true;
                    _saveLoadSlot = 1;
                    _saveLoadFlag = 1;
                    break;
                case 0x5E:      // SO_ROOM_LOAD_GAME Load game
                    _saveSound = Pop() != 0;
                    if (_saveLoadFlag == 0)
                    {
                        _saveTemporaryState = true;
                        _saveLoadSlot = 1;
                        _saveLoadFlag = 2;
                    }
                    break;
                case 0x5F:      // SO_ROOM_SATURATION Set saturation of room colors
                    e = Pop();
                    d = Pop();
                    c = Pop();
                    b = Pop();
                    a = Pop();
                    DesaturatePalette(a, b, c, d, e);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("RoomOps8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0xAC)]
        protected override void ActorOps()
        {
            byte subOp = ReadByte();
            int i, j;

            if (subOp == 0x7A)
            {
                _curActor = Pop();
                return;
            }

            var a = Actors[_curActor];
            if (a == null)
                return;

            switch (subOp)
            {
                case 0x64:      // SO_ACTOR_COSTUME Set actor costume
                    a.SetActorCostume((ushort)Pop());
                    break;
                case 0x65:      // SO_ACTOR_STEP_DIST Set actor width of steps
                    j = Pop();
                    i = Pop();
                    a.SetActorWalkSpeed((uint)i, (uint)j);
                    break;
                case 0x67:      // SO_ACTOR_ANIMATION_DEFAULT Set actor animation to default
                    a.InitFrame = 1;
                    a.WalkFrame = 2;
                    a.StandFrame = 3;
                    a.TalkStartFrame = 4;
                    a.TalkStopFrame = 5;
                    break;
                case 0x68:      // SO_ACTOR_ANIMATION_INIT Initialize animation
                    a.InitFrame = (byte)Pop();
                    break;
                case 0x69:      // SO_ACTOR_ANIMATION_TALK Set actor animation to talk animation
                    a.TalkStopFrame = (byte)Pop();
                    a.TalkStartFrame = (byte)Pop();
                    break;
                case 0x6A:      // SO_ACTOR_ANIMATION_WALK Set actor animation to walk animation
                    a.WalkFrame = (byte)Pop();
                    break;
                case 0x6B:      // SO_ACTOR_ANIMATION_STAND Set actor animation to standing animation
                    a.StandFrame = (byte)Pop();
                    break;
                case 0x6C:      // SO_ACTOR_ANIMATION_SPEED Set speed of animation
                    a.SetAnimSpeed((byte)Pop());
                    break;
                case 0x6D:      // SO_ACTOR_DEFAULT
                    a.Init(0);
                    break;
                case 0x6E:      // SO_ACTOR_ELEVATION
                    a.Elevation = Pop();
                    break;
                case 0x6F:      // SO_ACTOR_PALETTE Set actor palette
                    j = Pop();
                    i = Pop();
                    ScummHelper.AssertRange(0, i, 31, "o8_actorOps: palette slot");
                    a.SetPalette(i, (ushort)j);
                    break;
                case 0x70:      // SO_ACTOR_TALK_COLOR Set actor talk color
                    a.TalkColor = (byte)Pop();
                    break;
                case 0x71:      // SO_ACTOR_NAME Set name of actor
                    a.Name = ReadCharacters();
                    break;
                case 0x72:      // SO_ACTOR_WIDTH Set width of actor
                    a.Width = (uint)Pop();
                    break;
                case 0x73:      // SO_ACTOR_SCALE Set scaling of actor
                    i = Pop();
                    a.SetScale(i, i);
                    break;
                case 0x74:      // SO_ACTOR_NEVER_ZCLIP
                    a.ForceClip = 0;
                    break;
                case 0x75:      // SO_ACTOR_ALWAYS_ZCLIP
                    a.ForceClip = (byte)Pop();
                    // V8 uses 255 where we used to use 100
                    if (a.ForceClip == 255)
                        a.ForceClip = 100;
                    break;
                case 0x76:      // SO_ACTOR_IGNORE_BOXES Make actor ignore boxes
                    a.IgnoreBoxes = true;
                    a.ForceClip = 100;
                    if (a.IsInCurrentRoom)
                        a.PutActor();
                    break;
                case 0x77:      // SO_ACTOR_FOLLOW_BOXES Make actor follow boxes
                    a.IgnoreBoxes = false;
                    a.ForceClip = 100;
                    if (a.IsInCurrentRoom)
                        a.PutActor();
                    break;
                case 0x78:      // SO_ACTOR_SPECIAL_DRAW
                    a.ShadowMode = (byte)Pop();
                    break;
                case 0x79:      // SO_ACTOR_TEXT_OFFSET Set text offset relative to actor
                    {
                        var y = Pop();
                        var x = Pop();
                        a.TalkPosition = new Point(x, y);
                    }
                    break;
                //  case 0x7A:      // SO_ACTOR_INIT Set current actor (handled above)
                case 0x7B:      // SO_ACTOR_VARIABLE Set actor variable
                    i = Pop();
                    a.SetAnimVar(Pop(), i);
                    break;
                case 0x7C:      // SO_ACTOR_IGNORE_TURNS_ON Make actor ignore turns
                    a.IgnoreTurns = true;
                    break;
                case 0x7D:      // SO_ACTOR_IGNORE_TURNS_OFF Make actor follow turns
                    a.IgnoreTurns = false;
                    break;
                case 0x7E:      // SO_ACTOR_NEW New actor
                    a.Init(2);
                    break;
                case 0x7F:      // SO_ACTOR_DEPTH Set actor Z position
                    a.Layer = Pop();
                    break;
                case 0x80:      // SO_ACTOR_STOP
                    a.StopActorMoving();
                    a.StartAnimActor(a.StandFrame);
                    break;
                case 0x81:      // SO_ACTOR_FACE Make actor face angle
                    a.Moving &= ~MoveFlags.Turn;
                    a.SetDirection(Pop());
                    break;
                case 0x82:      // SO_ACTOR_TURN Turn actor
                    a.TurnToDirection(Pop());
                    break;
                case 0x83:      // SO_ACTOR_WALK_SCRIPT Set walk script for actor?
                    a.WalkScript = (ushort)Pop();
                    break;
                case 0x84:      // SO_ACTOR_TALK_SCRIPT Set talk script for actor?
                    a.TalkScript = (ushort)Pop();
                    break;
                case 0x85:      // SO_ACTOR_WALK_PAUSE
                    a.Moving |= MoveFlags.Frozen;
                    break;
                case 0x86:      // SO_ACTOR_WALK_RESUME
                    a.Moving &= ~MoveFlags.Frozen;
                    break;
                case 0x87:      // SO_ACTOR_VOLUME Set volume of actor speech
                    a.TalkVolume = Pop();
                    break;
                case 0x88:      // SO_ACTOR_FREQUENCY Set frequency of actor speech
                    a.TalkFrequency = Pop();
                    break;
                case 0x89:      // SO_ACTOR_PAN
                    a.TalkPan = Pop();
                    break;
                default:
                    throw new InvalidOperationException(string.Format("ActorOps8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0xAD)]
        void CameraOps()
        {
            byte subOp = ReadByte();

            switch (subOp)
            {
                case 0x32:      // SO_CAMERA_PAUSE
                    Debug.WriteLine("freezeCamera NYI");
                    break;
                case 0x33:      // SO_CAMERA_RESUME
                    Debug.WriteLine("unfreezeCamera NYI");
                    break;
                default:
                    throw new InvalidOperationException(string.Format("CameraOps8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0xAE)]
        protected override void VerbOps()
        {
            byte subOp = ReadByte();

            if (subOp == 0x96)
            {
                _curVerb = Pop();
                _curVerbSlot = GetVerbSlot(_curVerb, 0);
                ScummHelper.AssertRange(0, _curVerbSlot, Verbs.Length - 1, "new verb slot");
                return;
            }

            Debug.Assert(0 <= _curVerbSlot && _curVerbSlot < Verbs.Length);
            var vs = Verbs[_curVerbSlot];

            switch (subOp)
            {
                case 0x96:      // SO_VERB_INIT Choose verb number for editing
                    // handled above!
                    break;
                case 0x97:      // SO_VERB_NEW New verb
                    if (_curVerbSlot == 0)
                    {
                        int slot;
                        for (slot = 1; slot < Verbs.Length; slot++)
                        {
                            if (Verbs[slot].VerbId == 0)
                                break;
                        }
                        if (slot >= Verbs.Length)
                        {
                            throw new InvalidOperationException("Too many verbs");
                        }
                        _curVerbSlot = slot;
                    }
                    vs = Verbs[_curVerbSlot];
                    vs.VerbId = (ushort)_curVerb;
                    vs.Color = 2;
                    vs.HiColor = 0;
                    vs.DimColor = 8;
                    vs.Type = VerbType.Text;
                    vs.CharsetNr = String[0].Default.Charset;
                    vs.CurMode = 0;
                    vs.SaveId = 0;
                    vs.Key = 0;
                    vs.Center = false;
                    vs.ImgIndex = 0;
                    break;
                case 0x98:      // SO_VERB_DELETE Delete verb
                    KillVerb(_curVerbSlot);
                    break;
                case 0x99:      // SO_VERB_NAME Set verb name
                    Verbs[_curVerbSlot].Text = ReadCharacters();
                    vs.Type = VerbType.Text;
                    vs.ImgIndex = 0;
                    break;
                case 0x9A:      // SO_VERB_AT Set verb (X,Y) placement
                    vs.CurRect.Top = Pop();
                    vs.CurRect.Left = Pop();
                    break;
                case 0x9B:      // SO_VERB_ON Turn verb on
                    vs.CurMode = 1;
                    break;
                case 0x9C:      // SO_VERB_OFF Turn verb off
                    vs.CurMode = 0;
                    break;
                case 0x9D:      // SO_VERB_COLOR Set verb color
                    vs.Color = (byte)Pop();
                    break;
                case 0x9E:      // SO_VERB_HICOLOR Set verb highlighted color
                    vs.HiColor = (byte)Pop();
                    break;
                case 0xA0:      // SO_VERB_DIMCOLOR Set verb dimmed (disabled) color
                    vs.DimColor = (byte)Pop();
                    break;
                case 0xA1:      // SO_VERB_DIM
                    vs.CurMode = 2;
                    break;
                case 0xA2:      // SO_VERB_KEY Set keypress to associate with verb
                    vs.Key = (byte)Pop();
                    break;
                case 0xA3:      // SO_VERB_IMAGE Set verb image
                    {
                        var b = Pop();
                        var a = Pop();
                        if (_curVerbSlot != 0 && a != vs.ImgIndex)
                        {
                            SetVerbObject((byte)b, a, _curVerbSlot);
                            vs.Type = VerbType.Image;
                            vs.ImgIndex = (ushort)a;
                        }
                    }
                    break;
                case 0xA4:      // SO_VERB_NAME_STR Set verb name
                    {
                        var a = Pop();
                        if (a == 0)
                        {
                            Verbs[_curVerbSlot].Text = new byte[0];
                        }
                        else
                        {
                            Verbs[_curVerbSlot].Text = GetStringAddress(a);
                        }
                        vs.Type = VerbType.Text;
                        vs.ImgIndex = 0;
                    }
                    break;
                case 0xA5:      // SO_VERB_CENTER Center verb
                    vs.Center = true;
                    break;
                case 0xA6:      // SO_VERB_CHARSET Choose charset for verb
                    vs.CharsetNr = (byte)Pop();
                    break;
                case 0xA7:      // SO_VERB_LINE_SPACING Choose linespacing for verb
                    _verbLineSpacing = Pop();
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Verbops8: default case 0x{0:X}", subOp));
            }
        }

        [OpCode(0xAF)]
        protected override void StartSound(int sound)
        {
            base.StartSound(sound);
        }

        [OpCode(0xB0)]
        protected override void StartMusic(int sound)
        {
            base.StartMusic(sound);
        }

        [OpCode(0xB1)]
        protected override void StopSound(int sound)
        {
            base.StopSound(sound);
        }

        [OpCode(0xB2)]
        protected override void SoundKludge(int[] args)
        {
            base.SoundKludge(args);
        }

        [OpCode(0xB3)]
        protected override void SystemOps()
        {
            base.SystemOps();
        }

        [OpCode(0xB4)]
        protected override void SaveRestoreVerbs(int a, int b, int c)
        {
            base.SaveRestoreVerbs(a, b, c);
        }

        [OpCode(0xB5)]
        protected override void SetObjectName(int obj)
        {
            base.SetObjectName(obj);
        }

        [OpCode(0xB6)]
        protected override void GetDateTime()
        {
            base.GetDateTime();
        }

        [OpCode(0xB7)]
        protected override void DrawBox(int x, int y, int x2, int y2, int color)
        {
            base.DrawBox(x, y, x2, y2, color);
        }

        [OpCode(0xB9)]
        void StartVideo()
        {
            var filename = System.Text.Encoding.UTF8.GetString(ReadCharacters());
            SmushPlayer.Play(filename, 12);
        }

        [OpCode(0xBA)]
        void KernelSetFunction()
        {
            // TODO
            var args = GetStackList(30);

            switch (args[0])
            {
                case 11:
                    {  // lockObject
                        int objidx = GetObjectIndex(args[1]);
                        Debug.Assert(objidx != -1);
                        _objs[objidx].IsLocked = true;
                        break;
                    }
                case 12:
                    {  // unlockObject
                        int objidx = GetObjectIndex(args[1]);
                        Debug.Assert(objidx != -1);
                        _objs[objidx].IsLocked = false;
                        break;
                    }
                case 13:    // remapCostume
                    {
                        var a = Actors[args[1]];
                        a.RemapActorPalette(args[2], args[3], args[4], -1);
                    }
                    break;
                case 14:    // remapCostumeInsert
                    {
                        var a = Actors[args[1]];
                        a.RemapActorPalette(args[2], args[3], args[4], args.Length == 5 ? 0 : args[5]);
                    }
                    break;
                case 15:    // setVideoFrameRate
                            // not used anymore (was smush frame rate)
                    break;
                case 20:    // setBoxScaleSlot
                    SetBoxScaleSlot(args[1], args[2]);
                    break;
                case 21:    // setScaleSlot
                    SetScaleSlot(args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                    break;
                case 22:    // setBannerColors
                            //      debug(0, "o8_kernelSetFunctions: setBannerColors(%d, %d, %d, %d)", args[1], args[2], args[3], args[4]);
                    break;
                case 23:    // setActorChoreLimbFrame
                    {
                        var a = Actors[args[1]];

                        a.StartAnimActor(args[2]);
                        a.AnimateLimb(args[3], args[4]);
                    }
                    break;
                case 24:    // clearTextQueue
                    RemoveBlastTexts();
                    break;
                case 25:
                    {  // saveGameReadName
                       //                        string name;
                       //                        if (GetSavegameName(args[1], out name))
                       //                        {
                       //                            int size = name.Length + 1;
                       //                            _res.nukeResource(rtString, args[2]);
                       //
                       //                            var ah = _res.createResource(rtString, args[2], size + sizeof(ArrayHeader));
                       //                            ah.type = TO_LE_16(kStringArray);
                       //                            ah.dim1 = TO_LE_16(size + 1);
                       //                            ah.dim2 = TO_LE_16(1);
                       //
                       //                            memcpy(getStringAddress(args[2]), name.c_str(), size);
                       //                        }
                        throw new NotImplementedException("saveGameReadName");
                        //break;
                    }
                case 26:
                    { // saveGameWrite
                        // FIXME: This doesn't work
                        var address = GetStringAddress(args[2]);
                        Debug.WriteLine("KernelSetFunctions8: saveGame({0}, {1})", args[1], address);
                    }
                    break;

                case 27: // saveGameRead
                    _saveLoadSlot = args[1];
                    _saveLoadFlag = 2;
                    _saveTemporaryState = false;
                    break;
                case 28:    // saveGameStampScreenshot
                    Debug.WriteLine("KernelSetFunctions8: saveGameStampScreenshot(%d, %d, %d, %d, %d, %d)", args[1], args[2], args[3], args[4], args[5], args[6]);
                    break;
                case 29:    // setKeyScript
                    _keyScriptKey = args[1];
                    _keyScriptNo = args[2];
                    break;
                case 30:    // killAllScriptsButMe
                    KillAllScriptsExceptCurrent();
                    break;
                case 31:    // stopAllVideo
                    Debug.WriteLine("kernelSetFunctions8: StopAllVideo()");
                    break;
                case 32:    // writeRegistryValue
                    {
                        int idx = args[1];
                        int value = args[2];
                        var str = GetStringAddress(idx);

                        Debug.WriteLine("KernelSetFunctions8: WriteRegistryValue({0}, {1})", str, value);
                    }
                    break;
                case 33:    // paletteSetIntensity
                    Debug.WriteLine("KernelSetFunctions8: paletteSetIntensity({0}, {1})", args[1], args[2]);
                    break;
                case 34:    // queryQuit
                            // TODO: query quit
                            //                    if (ConfMan.getBool("confirm_exit"))
                            //                        confirmExitDialog();
                            //                    else
                            //                        quitGame();
                    break;
                case 108:   // buildPaletteShadow
                    SetShadowPalette(args[1], args[2], args[3], args[4], args.Length < 6 ? 0 : args[5], args.Length < 7 ? 0 : args[6]);
                    break;
                case 109:   // setPaletteShadow
                    SetShadowPalette(0, args[1], args[2], args[3], args[4], args[5]);
                    break;
                case 118:   // blastShadowObject
                    EnqueueObject(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], 3);
                    break;
                case 119:   // superBlastObject
                    EnqueueObject(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], 0);
                    break;

                default:
                    throw new InvalidOperationException(string.Format("KernelSetFunctions8: default case 0x{0:X} (len = {1})", args[0], args.Length));
            }
        }

        [OpCode(0xC8)]
        protected override void StartScriptQuick2(int script, int[] args)
        {
            base.StartScriptQuick2(script, args);
        }

        [OpCode(0xC9)]
        protected override void StartObjectQuick(int script, byte entryp, int[] args)
        {
            base.StartObjectQuick(script, entryp, args);
        }

        [OpCode(0xCA)]
        protected override void PickOneOf(int i, int[] args)
        {
            base.PickOneOf(i, args);
        }

        [OpCode(0xCB)]
        protected override void PickOneOfDefault(int i, int[] args, int def)
        {
            base.PickOneOfDefault(i, args, def);
        }

        [OpCode(0xCD)]
        protected override void IsAnyOf(int value, int[] args)
        {
            base.IsAnyOf(value, args);
        }

        [OpCode(0xCE)]
        protected override void GetRandomNumber(int max)
        {
            base.GetRandomNumber(max);
        }

        [OpCode(0xCF)]
        protected override void GetRandomNumberRange(int min, int max)
        {
            base.GetRandomNumberRange(min, max);
        }

        [OpCode(0xD0)]
        protected override void IfClassOfIs(int obj, int[] args)
        {
            base.IfClassOfIs(obj, args);
        }

        [OpCode(0xD1)]
        protected override void GetState(int obj)
        {
            base.GetState(obj);
        }

        [OpCode(0xD2)]
        protected override void GetOwner(int obj)
        {
            base.GetOwner(obj);
        }

        [OpCode(0xD3)]
        protected override void IsScriptRunning(int script)
        {
            base.IsScriptRunning(script);
        }

        [OpCode(0xD5)]
        protected override void IsSoundRunning(int sound)
        {
            base.IsSoundRunning(sound);
        }

        [OpCode(0xD6)]
        protected override void Abs(int value)
        {
            base.Abs(value);
        }

        [OpCode(0xD8)]
        protected override void KernelGetFunctions()
        {
            var args = GetStackList(30);

            switch (args[0])
            {
                case 0x73:  // getWalkBoxAt
                    Push(GetSpecialBox(new Point(args[1], args[2])));
                    break;
                case 0x74:  // isPointInBox
                    Push(CheckXYInBoxBounds(args[3], new Point(args[1], args[2])));
                    break;
                case 0xD3:      // getKeyState
                    Push(GetKeyState(args[1]));
                    break;
                case 0xCE:      // getRGBSlot
                    Push(RemapPaletteColor(args[1], args[2], args[3], -1));
                    break;
                case 0xD7:      // getBox
                    Push(CheckXYInBoxBounds(args[3], new Point(args[1], args[2])));
                    break;
                case 0xD8:
                    {        // findBlastObject
                        int x = args[1] + (Camera.CurrentPosition.X & 7);
                        int y = args[2] + ScreenTop;

                        for (int i = _blastObjectQueuePos - 1; i >= 0; i--)
                        {
                            var eo = _blastObjectQueue[i];

                            if (eo.Rect.Contains(x, y) && !GetClass(eo.Number, ObjectClass.Untouchable))
                            {
                                Push(eo.Number);
                                return;
                            }
                        }
                        Push(0);
                        break;
                    }
                case 0xD9:
                    {   // actorHit - used, for example, to detect ship collision
                        // during ship-to-ship combat.
                        var a = Actors[args[1]];
                        Push(a.ActorHitTest(new Point(args[2], args[3] + ScreenTop)));
                        break;
                    }
                case 0xDA:      // lipSyncWidth
                    Push(IMuseDigital.GetCurVoiceLipSyncWidth());
                    break;
                case 0xDB:      // lipSyncHeight
                    Push(IMuseDigital.GetCurVoiceLipSyncHeight());
                    break;
                case 0xDC:      // actorTalkAnimation
                    {
                        var a = Actors[args[1]];
                        Push(a.TalkStartFrame);
                    }
                    break;
                case 0xDD:      // getGroupSfxVol
                    Push(Mixer.GetVolumeForSoundType(SoundType.SFX) / 2);
                    break;
                case 0xDE:      // getGroupVoiceVol
                    Push(Mixer.GetVolumeForSoundType(SoundType.Speech) / 2);
                    break;
                case 0xDF:      // getGroupMusicVol
                    Push(Mixer.GetVolumeForSoundType(SoundType.Music) / 2);
                    break;
                case 0xE0:      // readRegistryValue
                    {
                        int idx = args[1];
                        var str = System.Text.Encoding.UTF8.GetString(GetStringAddress(idx));
                        // TODO:
                        //                        if (str=="SFX Volume")
                        //                            Push(ConfMan.getInt("sfx_volume") / 2);
                        //                        else if (!strcmp(str, "Voice Volume"))
                        //                            push(ConfMan.getInt("speech_volume") / 2);
                        //                        else if (!strcmp(str, "Music Volume"))
                        //                            push(ConfMan.getInt("music_volume") / 2);
                        //                        else if (!strcmp(str, "Text Status"))
                        //                            push(ConfMan.getBool("subtitles"));
                        //                        else if (!strcmp(str, "Object Names"))
                        //                            push(ConfMan.getBool("object_labels"));
                        //                        else if (!strcmp(str, "Saveload Page"))
                        //                            push(14);
                        //                        else        // Use defaults
                        Push(-1);
                        Debug.WriteLine("KernelGetFunctions8: ReadRegistryValue({0})", str);
                    }
                    break;
                case 0xE1:      // imGetMusicPosition
                    Push(IMuseDigital.GetCurMusicPosInMs());
                    break;
                case 0xE2:      // musicLipSyncWidth
                    Push(IMuseDigital.GetCurMusicLipSyncWidth(args[1]));
                    break;
                case 0xE3:      // musicLipSyncHeight
                    Push(IMuseDigital.GetCurMusicLipSyncHeight(args[1]));
                    break;
                default:
                    throw new InvalidOperationException(string.Format("KernelGetFunctions8: default case 0x{0:X} (len = {1})", args[0], args.Length));
            }
        }

        [OpCode(0xD9)]
        protected override void IsActorInBox(int index, int box)
        {
            base.IsActorInBox(index, box);
        }

        [OpCode(0xDA)]
        protected override void GetVerbEntrypoint(int verb, int entryp)
        {
            base.GetVerbEntrypoint(verb, entryp);
        }

        [OpCode(0xDB)]
        protected override void GetActorFromXY(int x, int y)
        {
            base.GetActorFromXY(x, y);
        }

        [OpCode(0xDC)]
        protected override void FindObject(int x, int y)
        {
            base.FindObject(x, y);
        }

        [OpCode(0xDD)]
        protected override void GetVerbFromXY(int x, int y)
        {
            base.GetVerbFromXY(x, y);
        }

        [OpCode(0xDF)]
        protected override void FindInventory(int owner, int index)
        {
            base.FindInventory(owner, index);
        }

        [OpCode(0xE0)]
        protected override void GetInventoryCount(int owner)
        {
            base.GetInventoryCount(owner);
        }

        [OpCode(0xE1)]
        protected override void GetAnimateVariable(int index, int variable)
        {
            base.GetAnimateVariable(index, variable);
        }

        [OpCode(0xE2)]
        protected override void GetActorRoom(int index)
        {
            base.GetActorRoom(index);
        }

        [OpCode(0xE3)]
        protected override void GetActorWalkBox(int index)
        {
            base.GetActorWalkBox(index);
        }

        [OpCode(0xE4)]
        protected override void GetActorMoving(int index)
        {
            base.GetActorMoving(index);
        }

        [OpCode(0xE5)]
        protected override void GetActorCostume(int index)
        {
            base.GetActorCostume(index);
        }

        [OpCode(0xE6)]
        protected override void GetActorScaleX(int index)
        {
            base.GetActorScaleX(index);
        }

        [OpCode(0xE7)]
        protected override void GetActorLayer(int index)
        {
            base.GetActorLayer(index);
        }

        [OpCode(0xE8)]
        protected override void GetActorElevation(int index)
        {
            base.GetActorElevation(index);
        }

        [OpCode(0xE9)]
        protected override void GetActorWidth(int index)
        {
            base.GetActorWidth(index);
        }

        [OpCode(0xEA)]
        protected override void GetObjectNewDir(int index)
        {
            base.GetObjectNewDir(index);
        }

        [OpCode(0xEB)]
        protected override void GetObjectX(int index)
        {
            base.GetObjectX(index);
        }

        [OpCode(0xEC)]
        protected override void GetObjectY(int index)
        {
            base.GetObjectY(index);
        }

        [OpCode(0xED)]
        void GetActorChore()
        {
            var actnum = Pop();
            var a = Actors[actnum];
            Push(a.Frame);
        }

        [OpCode(0xEE)]
        protected override void DistObjectObject(int a, int b)
        {
            base.DistObjectObject(a, b);
        }

        [OpCode(0xEF)]
        protected override void DistObjectPtPt(int a, int b, int c, int d)
        {
            base.DistObjectPtPt(a, b, c, d);
        }

        [OpCode(0xF0)]
        void GetObjectImageX()
        {
            var i = GetObjectIndex(Pop());
            Debug.Assert(i != 0);
            Push(_objs[i].Position.X);
        }

        [OpCode(0xF1)]
        void GetObjectImageY()
        {
            var i = GetObjectIndex(Pop());
            Debug.Assert(i != 0);
            Push(_objs[i].Position.Y);
        }

        [OpCode(0xF2)]
        void GetObjectImageWidth()
        {
            var i = GetObjectIndex(Pop());
            Debug.Assert(i != 0);
            Push(_objs[i].Width);
        }

        [OpCode(0xF3)]
        void GetObjectImageHeight()
        {
            var i = GetObjectIndex(Pop());
            Debug.Assert(i != 0);
            Push(_objs[i].Height);
        }

        [OpCode(0xF6)]
        void GetStringWidth()
        {
            int charset = Pop();
            int oldID = _charset.GetCurId();
            int width;
            var msg = ReadCharacters();

            var transBuf = TranslateText(msg);
            msg = transBuf;

            // Temporary set the specified charset id
            _charset.SetCurID(charset);
            // Determine the strings width
            width = _charset.GetStringWidth(0, msg, 0);
            // Revert to old font
            _charset.SetCurID(oldID);

            Push(width);
        }

        [OpCode(0xF7)]
        void GetActorZPlane()
        {
            var actnum = Pop();
            var a = Actors[actnum];

            int z = a.ForceClip;
            if (z == 100)
            {
                z = GetBoxMask(a.Walkbox);
                if (z > Gdi.NumZBuffer - 1)
                    z = Gdi.NumZBuffer - 1;
            }

            Push(z);
        }

        void SetBoxScaleSlot(int box, int slot)
        {
            var b = GetBoxBase(box);
            b.ScaleSlot = slot;
        }

        protected override void PrintString(int m, byte[] msg)
        {
            if (m == 4)
            {
                var st = String[m];
                EnqueueText(msg, st.Position.X, st.Position.Y, st.Color, st.Charset, st.Center);
            }
            else
            {
                base.PrintString(m, msg);
            }
        }

        /// <summary>
        /// This function scales the HSL (Hue, Saturation and Lightness)
        /// components of the palette colors. It's used in CMI when Guybrush
        /// walks from the beach towards the swamp.
        /// </summary>
        /// <param name="hueScale">Hue scale.</param>
        /// <param name="satScale">Sat scale.</param>
        /// <param name="lightScale">Light scale.</param>
        /// <param name="startColor">Start color.</param>
        /// <param name="endColor">End color.</param>
        void DesaturatePalette(int hueScale, int satScale, int lightScale, int startColor, int endColor)
        {
            if (startColor <= endColor)
            {
                for (var i = startColor; i <= endColor; i++)
                {
                    int R = _darkenPalette.Colors[i].R;
                    int G = _darkenPalette.Colors[i].G;
                    int B = _darkenPalette.Colors[i].B;

                    // RGB to HLS (Foley and VanDam)

                    int min = Math.Min(R, Math.Min(G, B));
                    int max = Math.Max(R, Math.Max(G, B));
                    int diff = (max - min);
                    int sum = (max + min);

                    if (diff != 0)
                    {
                        int H, S, L;

                        if (sum <= 255)
                            S = 255 * diff / sum;
                        else
                            S = 255 * diff / (255 * 2 - sum);

                        if (R == max)
                            H = 60 * (G - B) / diff;
                        else if (G == max)
                            H = 120 + 60 * (B - R) / diff;
                        else
                            H = 240 + 60 * (R - G) / diff;

                        if (H < 0)
                            H = H + 360;

                        // Scale the result

                        H = (H * hueScale) / 255;
                        S = (S * satScale) / 255;
                        L = (sum * lightScale) / 255;

                        // HLS to RGB (Foley and VanDam)

                        int m1, m2;
                        if (L <= 255)
                            m2 = L * (255 + S) / (255 * 2);
                        else
                            m2 = L * (255 - S) / (255 * 2) + S;

                        m1 = L - m2;

                        R = HSL2RGBHelper(m1, m2, H + 120);
                        G = HSL2RGBHelper(m1, m2, H);
                        B = HSL2RGBHelper(m1, m2, H - 120);
                    }
                    else
                    {
                        // Maximal color = minimal color -> R=G=B -> it's a grayscale.
                        R = G = B = (R * lightScale) / 255;
                    }

                    CurrentPalette.Colors[i] = Color.FromRgb(R, G, B);
                }

                SetDirtyColors(startColor, endColor);
            }
        }

        static int HSL2RGBHelper(int n1, int n2, int hue)
        {
            if (hue > 360)
                hue = hue - 360;
            else if (hue < 0)
                hue = hue + 360;

            if (hue < 60)
                return n1 + (n2 - n1) * hue / 60;
            if (hue < 180)
                return n2;
            if (hue < 240)
                return n1 + (n2 - n1) * (240 - hue) / 60;
            return n1;
        }
    }
}
