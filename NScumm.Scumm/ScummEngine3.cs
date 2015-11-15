//
//  ScummEngine3.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    public partial class ScummEngine3: ScummEngine2
    {
        public ScummEngine3(GameSettings game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
        }

        protected override void ResetScummVars()
        {
            ResetScummVarsCore();

            Variables[VariableCurrentLights.Value] = (int)(LightModes.ActorUseBasePalette | LightModes.ActorUseColors | LightModes.RoomLightsOn);

            if (Game.GameId == GameId.Monkey1)
                Variables[74] = 1225;
        }

        protected void ResetScummVarsCore()
        {
            if (Game.Version <= 6)
            {
                // VAR_SOUNDCARD modes
                // 0 PC Speaker
                // 1 Tandy
                // 2 CMS
                // 3 AdLib
                // 4 Roland
                switch (Sound.MusicType)
                {
                    case MusicDriverTypes.None:
                    case MusicDriverTypes.PCSpeaker:
                        Variables[VariableSoundcard.Value] = 0;
                        break;
                    case MusicDriverTypes.PCjr:
                        Variables[VariableSoundcard.Value] = 1;
                        break;
                    case MusicDriverTypes.CMS:
                        Variables[VariableSoundcard.Value] = 2;
                        break;
                    case MusicDriverTypes.AdLib:
                        Variables[VariableSoundcard.Value] = 3;
                        break;
                    case MusicDriverTypes.Midi:
                        Variables[VariableSoundcard.Value] = 4;
                        break;
                    default:
                        if ((Game.GameId == GameId.Monkey1 && Game.Variant == "EGA") || (Game.GameId == GameId.Monkey1 && Game.Variant == "VGA")
                            || (Game.GameId == GameId.Loom && Game.Version == 3)) /*&&  (_game.platform == Common::kPlatformDOS)*/
                        {
                            Variables[VariableSoundcard.Value] = 4;
                        }
                        else
                        {
                            Variables[VariableSoundcard.Value] = 3;
                        }
                        break;
                }

                if (Game.Platform == Platform.FMTowns)
                    Variables[VariableVideoMode.Value] = 42;
                // Value only used by the Macintosh version of Indiana Jones and the Last Crusade
                else if (Game.Platform == Platform.Macintosh && Game.Version == 3)
                    Variables[VariableVideoMode.Value] = 50;
                // Value only used by the Amiga version of Monkey Island 2
                else if (Game.Platform == Platform.Amiga)
                    Variables[VariableVideoMode.Value] = 82;
                else
                    Variables[VariableVideoMode.Value] = 19;

                if ((Game.Platform == Platform.Macintosh) && Game.IsOldBundle)
                {
                    // Set screen size for the Macintosh version of Indy3/Loom
                    Variables[39] = 320;
                }
                if (Game.Platform == Platform.DOS && Game.GameId == GameId.Loom && Game.Version == 3)
                {
                    // Set number of sound resources
                    Variables[39] = 80;
                }
                if (Game.GameId == GameId.Loom || Game.Version >= 4)
                    Variables[VariableHeapSpace.Value] = 1400;

                if (Game.Version >= 4)
                    Variables[VariableFixedDisk.Value] = 1;

                if (Game.Version >= 5)
                    Variables[VariableInputMode.Value] = 3;
                if (Game.Version == 6)
                    Variables[VariableV6EMSSpace.Value] = 10000;
            }

            if (VariableVoiceMode.HasValue)
            {
                Variables[VariableVoiceMode.Value] = (int)VoiceMode.VoiceAndText;
            }

            if (VariableRoomWidth.HasValue && VariableRoomHeight.HasValue)
            {
                Variables[VariableRoomWidth.Value] = ScreenWidth;
                Variables[VariableRoomHeight.Value] = ScreenHeight;
            }

            if (VariableDebugMode.HasValue)
            {
                Variables[VariableDebugMode.Value] = (DebugMode ? 1 : 0);
            }

            if (VariableFadeDelay.HasValue)
                Variables[VariableFadeDelay.Value] = 3;

            Variables[VariableCharIncrement.Value] = 4;
            TalkingActor = 0;

            if (Game.Version >= 5 && Game.Version <= 7)
                Sound.SetupSound();
        }

        protected override void SetupVars()
        {
            VariableEgo = 1;
            VariableCameraPosX = 2;
            VariableHaveMessage = 3;
            VariableRoom = 4;
            VariableOverride = 5;
            VariableCurrentLights = 9;
            VariableTimer1 = 11;
            VariableTimer2 = 12;
            VariableTimer3 = 13;
            VariableMusicTimer = 14;
            VariableCameraMinX = 17;
            VariableCameraMaxX = 18;
            VariableTimerNext = 19;
            VariableVirtualMouseX = 20;
            VariableVirtualMouseY = 21;
            VariableRoomResource = 22;
            VariableLastSound = 23;
            VariableCutSceneExitKey = 24;
            VariableTalkActor = 25;
            VariableCameraFastX = 26;
            VariableEntryScript = 28;
            VariableEntryScript2 = 29;
            VariableExitScript = 30;
            VariableVerbScript = 32;
            VariableSentenceScript = 33;
            VariableInventoryScript = 34;
            VariableCutSceneStartScript = 35;
            VariableCutSceneEndScript = 36;
            VariableCharIncrement = 37;
            VariableWalkToObject = 38;
            VariableHeapSpace = 40;
            VariableMouseX = 44;
            VariableMouseY = 45;
            VariableTimer = 46;
            VariableTimerTotal = 47;
            VariableSoundcard = 48;
            VariableVideoMode = 49;
        }

        protected override int ReadVariable(uint var)
        {
            if (((var & 0x2000) != 0) && (Game.Version <= 5))
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                    var += (uint)ReadVariable((uint)(a & ~0x2000));
                else
                    var += a & 0xFFF;
                var = (uint)(var & ~0x2000);
            }

            if ((var & 0xF000) == 0)
            {
                if (!Settings.CopyProtection)
                {
                    if (var == 490 && Game.GameId == GameId.Monkey2)
                    {
                        var = 518;
                    }
                }
                return Variables[var];
            }

            if ((var & 0x8000) == 0x8000)
            {
                //                Debug.Write(string.Format("ReadVariable({0}) => ", var));
                if (Game.Version <= 3 && !(Game.GameId == GameId.Indy3 && Game.Platform == Platform.FMTowns) &&
                    !(Game.GameId == GameId.Loom && Game.Platform == Platform.PCEngine))
                {
                    int bit = (int)(var & 0xF);
                    var = (var >> 4) & 0xFF;

                    if (!Settings.CopyProtection)
                    {
                        if (Game.GameId == GameId.Loom && (Game.Platform == Platform.FMTowns) && var == 214 && bit == 15)
                        {
                            return 0;
                        }
                        else if (Game.GameId == GameId.Zak && (Game.Platform == Platform.FMTowns) && var == 151 && bit == 8)
                        {
                            return 0;
                        }
                    }

                    ScummHelper.AssertRange(0, var, _resManager.NumVariables - 1, "variable (reading)");
                    return (Variables[var] & (1 << bit)) > 0 ? 1 : 0;
                }
                var &= 0x7FFF;

                if (!Settings.CopyProtection)
                {
                    if (Game.GameId == GameId.Indy3 && (Game.Platform == Platform.FMTowns) && var == 1508)
                        return 0;
                }

                ScummHelper.AssertRange(0, var, _bitVars.Length - 1, "variable (reading)");
                //                Debug.WriteLine(_bitVars[var]);
                return _bitVars[(int)var] ? 1 : 0;
            }

            if ((var & 0x4000) == 0x4000)
            {
                //                Debug.Write(string.Format("ReadVariable({0}) => ", var));
                if (Game.Features.HasFlag(GameFeatures.FewLocals))
                {
                    var &= 0xF;
                }
                else
                {
                    var &= 0xFFF;
                }

                ScummHelper.AssertRange(0, var, 20, "local variable (reading)");
                //                Debug.WriteLine(_slots[CurrentScript].LocalVariables[var]);
                return Slots[CurrentScript].LocalVariables[var];
            }

            throw new NotSupportedException("Illegal varbits (r)");
        }

        protected override void WriteVariable(uint index, int value)
        {
            //            Console.WriteLine("SetResult({0},{1})", index, value);
            if ((index & 0xF000) == 0)
            {
                ScummHelper.AssertRange(0, index, _resManager.NumVariables - 1, "variable (writing)");
                Variables[index] = value;
                return;
            }

            if ((index & 0x8000) != 0)
            {
                if (Game.Version <= 3 && !(Game.GameId == GameId.Indy3 && Game.Platform == Platform.FMTowns) &&
                    !(Game.GameId == GameId.Loom && Game.Platform == Platform.PCEngine))
                {
                    var bit = (int)(index & 0xF);
                    index = (index >> 4) & 0xFF;
                    ScummHelper.AssertRange(0, index, _resManager.NumVariables - 1, "variable (writing)");
                    if (value > 0)
                        Variables[index] |= (1 << bit);
                    else
                        Variables[index] &= ~(1 << bit);
                }
                else
                {
                    index &= 0x7FFF;

                    ScummHelper.AssertRange(0, index, _bitVars.Length - 1, "bit variable (writing)");
                    _bitVars[(int)index] = value != 0;
                }
                return;
            }

            if ((index & 0x4000) != 0)
            {
                if (Game.Features.HasFlag(GameFeatures.FewLocals))
                {
                    index &= 0xF;
                }
                else
                {
                    index &= 0xFFF;
                }

                ScummHelper.AssertRange(0, index, 20, "local variable (writing)");
                //Console.WriteLine ("SetLocalVariables(script={0},var={1},value={2})", CurrentScript, index, value);
                Slots[CurrentScript].LocalVariables[index] = value;
                return;
            }
        }

        protected override int GetVar()
        {
            return ReadVariable(ReadWord());
        }

        protected override void GetResult()
        {
            _resultVarIndex = (int)ReadWord();
            if ((_resultVarIndex & 0x2000) == 0x2000)
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    _resultVarIndex += ReadVariable((uint)(a & ~0x2000));
                }
                else
                {
                    _resultVarIndex += (int)(a & 0xFFF);
                }
                _resultVarIndex &= ~0x2000;
            }
        }

        protected override void RunInventoryScript(int i)
        {
            if (Variables[VariableInventoryScript.Value] != 0)
            {
                RunScript(Variables[VariableInventoryScript.Value], false, false, new [] { i });
            }
        }

        protected override void RunInputScript(ClickArea clickArea, KeyCode code, int mode)
        {
            var verbScript = Variables[VariableVerbScript.Value];

            if (verbScript != 0)
            {
                RunScript(verbScript, false, false, new [] { (int)clickArea, (int)code, mode });
            }
        }

        protected override void SetBuiltinCursor(int idx)
        {
            var bpp = Surface.GetBytesPerPixel(_gfxManager.PixelFormat);
            var src = _cursorImages[_currentCursor];
            int color;

            if (bpp == 2)
            {
                if (Game.Platform == Platform.FMTowns)
                {
                    var palEntry = defaultCursorColors[idx] * 3;
                    color = ColorHelper.RGBToColor(_textPalette[palEntry],
                        _textPalette[palEntry + 1], _textPalette[palEntry + 2]);
                }
                else
                {
                    color = _16BitPalette[defaultCursorColors[idx]];
                }
            }
            else
            {
                color = defaultCursorColors[idx];
            }

            _cursor.Hotspot = new Point(
                _cursorHotspots[2 * _currentCursor] * TextSurfaceMultiplier,
                _cursorHotspots[2 * _currentCursor + 1] * TextSurfaceMultiplier);
            _cursor.Width = 16 * TextSurfaceMultiplier;
            _cursor.Height = 16 * TextSurfaceMultiplier;

            var pixels = new byte[_cursor.Width * _cursor.Height * bpp];

            int offset = 0;
            for (int w = 0; w < _cursor.Width; w++)
            {
                for (int h = 0; h < _cursor.Height; h++)
                {
                    int c;
                    if ((src[w] & (1 << h)) != 0)
                    {
                        c = color;
                    }
                    else
                    {
                        c = 0xFF;
                    }
                    if (bpp == 2)
                    {
                        pixels.WriteUInt16(offset, (ushort)c);
                        offset += 2;
                    }
                    else
                    {
                        pixels[offset++] = (byte)c;
                    }
                }
            }

            _gfxManager.SetCursor(pixels, _cursor.Width, _cursor.Height, _cursor.Hotspot);
        }

        #region OpCodes

        protected override void InitOpCodes()
        {
            _opCodes = new Dictionary<byte, Action>();
            /* 00 */
            _opCodes[0x00] = StopObjectCode;
            _opCodes[0x01] = PutActor;
            _opCodes[0x02] = StartMusic;
            _opCodes[0x03] = GetActorRoom;
            /* 04 */
            _opCodes[0x04] = IsGreaterEqual;
            _opCodes[0x05] = DrawObject;
            _opCodes[0x06] = GetActorElevation;
            _opCodes[0x07] = SetState;
            /* 08 */
            _opCodes[0x08] = IsNotEqual;
            _opCodes[0x09] = FaceActor;
            _opCodes[0x0A] = StartScript;
            _opCodes[0x0B] = GetVerbEntrypoint;
            /* 0C */
            _opCodes[0x0C] = ResourceRoutines;
            _opCodes[0x0D] = WalkActorToActor;
            _opCodes[0x0E] = PutActorAtObject;
            _opCodes[0x0F] = IfState;
            /* 10 */
            _opCodes[0x10] = GetObjectOwner;
            _opCodes[0x11] = AnimateActor;
            _opCodes[0x12] = PanCameraTo;
            _opCodes[0x13] = ActorOps;
            /* 14 */
            _opCodes[0x14] = Print;
            _opCodes[0x15] = ActorFromPosition;
            _opCodes[0x16] = GetRandomNumber;
            _opCodes[0x17] = And;
            /* 18 */
            _opCodes[0x18] = JumpRelative;
            _opCodes[0x19] = DoSentence;
            _opCodes[0x1A] = Move;
            _opCodes[0x1B] = Multiply;
            /* 1C */
            _opCodes[0x1C] = StartSound;
            _opCodes[0x1D] = IfClassOfIs;
            _opCodes[0x1E] = WalkActorTo;
            /* 20 */
            _opCodes[0x20] = StopMusic;
            _opCodes[0x21] = PutActor;
            _opCodes[0x22] = SaveLoadGame;
            _opCodes[0x23] = GetActorY;
            /* 24 */
            _opCodes[0x24] = LoadRoomWithEgo;
            _opCodes[0x25] = DrawObject;
            _opCodes[0x26] = SetVarRange;
            _opCodes[0x27] = StringOperations;
            /* 28 */
            _opCodes[0x28] = EqualZero;
            _opCodes[0x29] = SetOwnerOf;
            _opCodes[0x2A] = StartScript;
            _opCodes[0x2B] = DelayVariable;
            /* 2C */
            _opCodes[0x2C] = CursorCommand;
            _opCodes[0x2D] = PutActorInRoom;
            _opCodes[0x2E] = Delay;
            _opCodes[0x2F] = IfNotState;
            /* 30 */
            _opCodes[0x30] = SetBoxFlags;
            _opCodes[0x31] = GetInventoryCount;
            _opCodes[0x32] = SetCameraAt;
            _opCodes[0x33] = RoomOps;
            /* 34 */
            _opCodes[0x34] = GetDistance;
            _opCodes[0x35] = FindObject;
            _opCodes[0x36] = WalkActorToObject;
            _opCodes[0x37] = StartObject;
            /* 38 */
            _opCodes[0x38] = IsLessEqual;
            _opCodes[0x39] = DoSentence;
            _opCodes[0x3A] = Subtract;
            _opCodes[0x3B] = WaitForActor;
            /* 3C */
            _opCodes[0x3C] = StopSound;
            _opCodes[0x3D] = FindInventory;
            _opCodes[0x3E] = WalkActorTo;
            _opCodes[0x3F] = DrawBox;
            /* 40 */
            _opCodes[0x40] = CutScene;
            _opCodes[0x41] = PutActor;
            _opCodes[0x42] = ChainScript;
            _opCodes[0x43] = GetActorX;
            /* 44 */
            _opCodes[0x44] = IsLess;
            _opCodes[0x45] = DrawObject;
            _opCodes[0x46] = Increment;
            _opCodes[0x47] = SetState;
            /* 48 */
            _opCodes[0x48] = IsEqual;
            _opCodes[0x49] = FaceActor;
            _opCodes[0x4A] = StartScript;
            _opCodes[0x4B] = GetVerbEntrypoint;
            /* 4C */
            _opCodes[0x4C] = WaitForSentence;
            _opCodes[0x4D] = WalkActorToActor;
            _opCodes[0x4E] = PutActorAtObject;
            _opCodes[0x4F] = IfState;
            /* 50 */
            _opCodes[0x50] = PickupObject;
            _opCodes[0x51] = AnimateActor;
            _opCodes[0x52] = ActorFollowCamera;
            _opCodes[0x53] = ActorOps;
            /* 54 */
            _opCodes[0x54] = SetObjectName;
            _opCodes[0x55] = ActorFromPosition;
            _opCodes[0x56] = GetActorMoving;
            _opCodes[0x57] = Or;
            /* 58 */
            _opCodes[0x58] = BeginOverride;
            _opCodes[0x59] = DoSentence;
            _opCodes[0x5A] = Add;
            _opCodes[0x5B] = Divide;
            /* 5C */
            _opCodes[0x5C] = OldRoomEffect;
            _opCodes[0x5D] = SetClass;
            _opCodes[0x5E] = WalkActorTo;
            /* 60 */
            _opCodes[0x60] = FreezeScripts;
            _opCodes[0x61] = PutActor;
            _opCodes[0x62] = StopScript;
            _opCodes[0x63] = GetActorFacing;
            /* 64 */
            _opCodes[0x64] = LoadRoomWithEgo;
            _opCodes[0x65] = DrawObject;
            _opCodes[0x67] = GetStringWidth;
            /* 68 */
            _opCodes[0x68] = IsScriptRunning;
            _opCodes[0x69] = SetOwnerOf;
            _opCodes[0x6A] = StartScript;
            _opCodes[0x6B] = DebugOp;
            /* 6C */
            _opCodes[0x6C] = GetActorWidth;
            _opCodes[0x6D] = PutActorInRoom;
            _opCodes[0x6E] = StopObjectScript;
            _opCodes[0x6F] = IfNotState;
            /* 70 */
            _opCodes[0x70] = Lights;
            _opCodes[0x71] = GetActorCostume;
            _opCodes[0x72] = LoadRoom;
            _opCodes[0x73] = RoomOps;
            /* 74 */
            _opCodes[0x74] = GetDistance;
            _opCodes[0x75] = FindObject;
            _opCodes[0x76] = WalkActorToObject;
            _opCodes[0x77] = StartObject;
            /* 78 */
            _opCodes[0x78] = IsGreater;
            _opCodes[0x79] = DoSentence;
            _opCodes[0x7A] = VerbOps;
            _opCodes[0x7B] = GetActorWalkBox;
            /* 7C */
            _opCodes[0x7C] = IsSoundRunning;
            _opCodes[0x7D] = FindInventory;
            _opCodes[0x7E] = WalkActorTo;
            _opCodes[0x7F] = DrawBox;
            /* 80 */
            _opCodes[0x80] = BreakHere;
            _opCodes[0x81] = PutActor;
            _opCodes[0x82] = StartMusic;
            _opCodes[0x83] = GetActorRoom;
            /* 84 */
            _opCodes[0x84] = IsGreaterEqual;
            _opCodes[0x85] = DrawObject;
            _opCodes[0x86] = GetActorElevation;
            _opCodes[0x87] = SetState;
            /* 88 */
            _opCodes[0x88] = IsNotEqual;
            _opCodes[0x89] = FaceActor;
            _opCodes[0x8A] = StartScript;
            _opCodes[0x8B] = GetVerbEntrypoint;
            /* 8C */
            _opCodes[0x8C] = ResourceRoutines;
            _opCodes[0x8D] = WalkActorToActor;
            _opCodes[0x8E] = PutActorAtObject;
            _opCodes[0x8F] = IfState;
            /* 90 */
            _opCodes[0x90] = GetObjectOwner;
            _opCodes[0x91] = AnimateActor;
            _opCodes[0x92] = PanCameraTo;
            _opCodes[0x93] = ActorOps;
            /* 94 */
            _opCodes[0x94] = Print;
            _opCodes[0x95] = ActorFromPosition;
            _opCodes[0x96] = GetRandomNumber;
            _opCodes[0x97] = And;
            /* 98 */
            _opCodes[0x98] = SystemOps;
            _opCodes[0x99] = DoSentence;
            _opCodes[0x9A] = Move;
            _opCodes[0x9B] = Multiply;
            /* 9C */
            _opCodes[0x9C] = StartSound;
            _opCodes[0x9D] = IfClassOfIs;
            _opCodes[0x9E] = WalkActorTo;
            /* A0 */
            _opCodes[0xA0] = StopObjectCode;
            _opCodes[0xA1] = PutActor;
            _opCodes[0xA2] = SaveLoadGame;
            _opCodes[0xA3] = GetActorY;
            /* A4 */
            _opCodes[0xA4] = LoadRoomWithEgo;
            _opCodes[0xA5] = DrawObject;
            _opCodes[0xA6] = SetVarRange;
            _opCodes[0xA7] = SaveLoadVars;
            /* A8 */
            _opCodes[0xA8] = NotEqualZero;
            _opCodes[0xA9] = SetOwnerOf;
            _opCodes[0xAA] = StartScript;
            _opCodes[0xAB] = SaveRestoreVerbs;
            /* AC */
            _opCodes[0xAC] = Expression;
            _opCodes[0xAD] = PutActorInRoom;
            _opCodes[0xAE] = Wait;
            _opCodes[0xAF] = IfNotState;
            /* B0 */
            _opCodes[0xB0] = SetBoxFlags;
            _opCodes[0xB1] = GetInventoryCount;
            _opCodes[0xB2] = SetCameraAt;
            _opCodes[0xB3] = RoomOps;
            /* B4 */
            _opCodes[0xB4] = GetDistance;
            _opCodes[0xB5] = FindObject;
            _opCodes[0xB6] = WalkActorToObject;
            _opCodes[0xB7] = StartObject;
            /* B8 */
            _opCodes[0xB8] = IsLessEqual;
            _opCodes[0xB9] = DoSentence;
            _opCodes[0xBA] = Subtract;
            _opCodes[0xBB] = WaitForActor;
            /* BC */
            _opCodes[0xBC] = StopSound;
            _opCodes[0xBD] = FindInventory;
            _opCodes[0xBE] = WalkActorTo;
            _opCodes[0xBF] = DrawBox;
            /* C0 */
            _opCodes[0xC0] = EndCutsceneCore;
            _opCodes[0xC1] = PutActor;
            _opCodes[0xC2] = ChainScript;
            _opCodes[0xC3] = GetActorX;
            /* C4 */
            _opCodes[0xC4] = IsLess;
            _opCodes[0xC5] = DrawObject;
            _opCodes[0xC6] = Decrement;
            _opCodes[0xC7] = SetState;
            /* C8 */
            _opCodes[0xC8] = IsEqual;
            _opCodes[0xC9] = FaceActor;
            _opCodes[0xCA] = StartScript;
            _opCodes[0xCB] = GetVerbEntrypoint;
            /* CC */
            _opCodes[0xCC] = PseudoRoom;
            _opCodes[0xCD] = WalkActorToActor;
            _opCodes[0xCE] = PutActorAtObject;
            _opCodes[0xCF] = IfState;
            /* D0 */
            _opCodes[0xD0] = PickupObject;
            _opCodes[0xD1] = AnimateActor;
            _opCodes[0xD2] = ActorFollowCamera;
            _opCodes[0xD3] = ActorOps;
            /* D4 */
            _opCodes[0xD4] = SetObjectName;
            _opCodes[0xD5] = ActorFromPosition;
            _opCodes[0xD6] = GetActorMoving;
            _opCodes[0xD7] = Or;
            /* D8 */
            _opCodes[0xD8] = PrintEgo;
            _opCodes[0xD9] = DoSentence;
            _opCodes[0xDA] = Add;
            _opCodes[0xDB] = Divide;
            /* DC */
            _opCodes[0xDC] = OldRoomEffect;
            _opCodes[0xDD] = SetClass;
            _opCodes[0xDE] = WalkActorTo;
            /* E0 */
            _opCodes[0xE0] = FreezeScripts;
            _opCodes[0xE1] = PutActor;
            _opCodes[0xE2] = StopScript;
            _opCodes[0xE3] = GetActorFacing;
            /* E4 */
            _opCodes[0xE4] = LoadRoomWithEgo;
            _opCodes[0xE5] = DrawObject;
            _opCodes[0xE7] = GetStringWidth;
            /* E8 */
            _opCodes[0xE8] = IsScriptRunning;
            _opCodes[0xE9] = SetOwnerOf;
            _opCodes[0xEA] = StartScript;
            _opCodes[0xEB] = DebugOp;
            /* EC */
            _opCodes[0xEC] = GetActorWidth;
            _opCodes[0xED] = PutActorInRoom;
            _opCodes[0xEF] = IfNotState;
            /* F0 */
            _opCodes[0xF0] = Lights;
            _opCodes[0xF1] = GetActorCostume;
            _opCodes[0xF2] = LoadRoom;
            _opCodes[0xF3] = RoomOps;
            /* F4 */
            _opCodes[0xF4] = GetDistance;
            _opCodes[0xF5] = FindObject;
            _opCodes[0xF6] = WalkActorToObject;
            _opCodes[0xF7] = StartObject;
            /* F8 */
            _opCodes[0xF8] = IsGreater;
            _opCodes[0xF9] = DoSentence;
            _opCodes[0xFA] = VerbOps;
            _opCodes[0xFB] = GetActorWalkBox;
            /* FC */
            _opCodes[0xFC] = IsSoundRunning;
            _opCodes[0xFD] = FindInventory;
            _opCodes[0xFE] = WalkActorTo;
            _opCodes[0xFF] = DrawBox;
        }

        void SystemOps()
        {
            byte subOp = ReadByte();
            switch (subOp)
            {
                case 1:     // SO_RESTART
                    //restart();
                    break;
                case 2:     // SO_PAUSE
                    ShowMenu();
                    break;
                case 3:     // SO_QUIT
                    HasToQuit = true;
                    break;
            }
        }

        void DebugOp()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            System.Diagnostics.Debug.WriteLine("Debug: {0}", a);
        }

        void Wait()
        {
            var oldPos = CurrentPos - 1;
            if (Game.GameId == GameId.Indy3)
            {
                _opCode = 2;
            }
            else
            {
                _opCode = ReadByte();
            }

            switch (_opCode & 0x1F)
            {
                case 1:     // SO_WAIT_FOR_ACTOR
                    {
                        var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
                        if (a != null && a.Moving != 0)
                            break;
                        return;
                    }
                case 2:     // SO_WAIT_FOR_MESSAGE
                    if (Variables[VariableHaveMessage.Value] != 0)
                        break;
                    return;

                case 3:     // SO_WAIT_FOR_CAMERA
                    if (Camera.CurrentPosition.X / 8 != Camera.DestinationPosition.X / 8)
                        break;
                    return;

                case 4:     // SO_WAIT_FOR_SENTENCE
                    if (SentenceNum != 0)
                    {
                        if (Sentence[SentenceNum - 1].IsFrozen && !IsScriptInUse(Variables[VariableSentenceScript.Value]))
                            return;
                    }
                    else if (!IsScriptInUse(Variables[VariableSentenceScript.Value]))
                        return;
                    break;

                default:
                    throw new NotImplementedException("Wait: unknown subopcode" + (_opCode & 0x1F));
            }

            CurrentPos = oldPos;
            BreakHere();
        }

        protected override void Delay()
        {
            uint delay = ReadByte();
            delay |= (uint)(ReadByte() << 8);
            delay |= (uint)(ReadByte() << 16);
            Slots[CurrentScript].Delay = (int)delay;
            Slots[CurrentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        void ResourceRoutines()
        {
            int resId = 0;

            _opCode = ReadByte();
            if (_opCode != 17)
            {
                resId = GetVarOrDirectByte(OpCodeParameter.Param1);
            }

            if (Game.Platform != Platform.FMTowns)
            {
                // FIXME - this probably can be removed eventually, I don't think the following
                // check will ever be triggered, but then I could be wrong and it's better
                // to play it safe.
                if ((_opCode & 0x3F) != (_opCode & 0x1F))
                    Debug.WriteLine("Oops, this shouldn't happen: ResourceRoutines opcode {0}", _opCode);
            }

            int op = _opCode & 0x3F;
            switch (op)
            {
                case 1: // load script
                    ResourceManager.LoadScript(resId);
                    break;
                case 2: // load sound
                    ResourceManager.LoadSound(Sound.MusicType, resId);
                    break;
                case 3: // load costume
                    ResourceManager.LoadCostume(resId);
                    break;

                case 4: // load room
                    ResourceManager.LoadRoom(resId);
                    break;

                case 5:         // SO_NUKE_SCRIPT
                case 6:         // SO_NUKESound
                case 7:         // SO_NUKE_COSTUME
                case 8:         // SO_NUKE_ROOM
                    break;

                case 9:         // SO_LOCK_SCRIPT
                    if (resId >= ResourceManager.NumGlobalScripts)
                        break;
                    ResourceManager.LockScript(resId);
                    break;
                case 10:
                    // FIXME: Sound resources are currently missing
                    if (Game.GameId == GameId.Loom && Game.Platform == Platform.PCEngine)
                        break;
                    ResourceManager.LockSound(resId);
                    break;

                case 11:        // SO_LOCK_COSTUME
                    ResourceManager.LockCostume(resId);
                    break;

                case 12:        // SO_LOCK_ROOM
                    if (resId > 0x7F)
                        resId = _resourceMapper[resId & 0x7F];
                    ResourceManager.LockRoom(resId);
                    break;

                case 13:        // SO_UNLOCK_SCRIPT
                    if (resId >= ResourceManager.NumGlobalScripts)
                        break;
                    ResourceManager.UnlockScript(resId);
                    break;

                case 14:        // SO_UNLOCKSound
                                // FIXME: Sound resources are currently missing
                    if (Game.GameId == GameId.Loom && Game.Platform == Platform.PCEngine)
                        break;
                    ResourceManager.UnlockSound(resId);
                    break;

                case 15:        // SO_UNLOCK_COSTUME
                    ResourceManager.UnlockCostume(resId);
                    break;

                case 16:        // SO_UNLOCK_ROOM
                    if (resId > 0x7F)
                        resId = _resourceMapper[resId & 0x7F];
                    ResourceManager.UnlockRoom(resId);
                    break;

                case 17:
                    // SO_CLEAR_HEAP
                    //heapClear(0);
                    //unkHeapProc2(0, 0);
                    break;

                case 18:
                    LoadCharset(resId);
                    break;

                case 20:        // SO_LOAD_OBJECT
                    LoadFlObject(GetVarOrDirectWord(OpCodeParameter.Param2), resId);
                    break;

            // TODO: For the following see also Hibarnatus' information on bug #805691.
                case 32:
                    // TODO (apparently never used in FM-TOWNS)
                    Debug.WriteLine("o5_resourceRoutines {0} not yet handled (script {1})", op, Slots[CurrentScript].Number);
                    break;
                case 33:
                    // TODO (apparently never used in FM-TOWNS)
                    Debug.WriteLine("o5_resourceRoutines {0} not yet handled (script {1})", op, Slots[CurrentScript].Number);
                    break;
                case 35:
                    if (TownsPlayer != null)
                        TownsPlayer.SetVolumeCD(GetVarOrDirectByte(OpCodeParameter.Param2), resId);
                    break;
                case 36:
                    var foo = GetVarOrDirectByte(OpCodeParameter.Param2);
                    var bar = ReadByte();
                    if (TownsPlayer != null)
                        TownsPlayer.SetSoundVolume(resId, foo, bar);
                    break;
                case 37:
                    if (TownsPlayer != null)
                        TownsPlayer.SetSoundNote(resId, GetVarOrDirectByte(OpCodeParameter.Param2));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        protected void LoadFlObject(int obj, int room)
        {
            // Don't load an already loaded object
            if (GetObjectIndex(obj) != -1)
                return;

            var r = ResourceManager.GetRoom((byte)room);
            var od = r.Objects.First(o => o.Number == obj);
            for (int i = 1; i < _objs.Length; i++)
            {
                if (_objs[i].Number == 0)
                {
                    _objs[i] = od;
                    od.FloatingObjectIndex = 1;
                    return;
                }
            }
        }

        #endregion OpCodes
    }
    
}
