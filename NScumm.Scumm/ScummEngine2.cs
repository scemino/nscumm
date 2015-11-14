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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using System.Collections.Generic;
using System;
using NScumm.Core.Audio;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NScumm.Core
{
    [Flags]
    public enum ObjectStateV2 : byte
    {
        Pickupable = 1,
        Untouchable = 2,
        Locked = 4,

        // FIXME: Not quite sure how to name state 8. It seems to mark some kind
        // of "activation state" for the given object. E.g. is a door open?
        // Is a drawer extended? In addition it is used to toggle the look
        // of objects that the user can "pick up" (i.e. it is set in
        // pickupObject together with Untouchable). So in a sense,
        // it can also mean "invisible" in some situations.
        State8 = 8
    }

    [Flags]
    public enum UserStates
    {
        SetFreeze = 0x01,
        // freeze scripts if FREEZE_ON is set, unfreeze otherwise
        SetCursor = 0x02,
        // shows cursor if CURSOR_ON is set, hides it otherwise
        SetIFace = 0x04,
        // change user-interface (sentence-line, inventory, verb-area)
        FreezeOn = 0x08,
        // only interpreted if SET_FREEZE is set
        CursorOn = 0x10,
        // only interpreted if SET_CURSOR is set
        IFaceSentence = 0x20,
        // only interpreted if SET_IFACE is set
        IFaceInventory = 0x40,
        // only interpreted if SET_IFACE is set
        IFaceVerbs = 0x80,
        // only interpreted if SET_IFACE is set
        IFaceAll = (IFaceSentence | IFaceInventory | IFaceVerbs)
    }

    public partial class ScummEngine2 : ScummEngine
    {
        const int SENTENCE_SCRIPT = 2;
        const int InventoryUpArrow = 4;
        const int InventoryDownArrow = 5;
        const int SentenceLine = 6;

        public int? VariableMachineSpeed;
        public int? VariableNumActor;
        public int? VariableCurrentDrive;
        public int? VariableActorRangeMin;
        public int? VariableActorRangeMax;
        public int? VariableKeyPress;

        public int? VariableSentenceVerb;
        public int? VariableSentenceObject1;
        public int? VariableSentenceObject2;
        public int? VariableSentencePreposition;
        public int? VariableBackupVerb;

        public int? VariableClickArea;
        public int? VariableClickVerb;
        public int? VariableClickObject;

        protected string _sentenceBuf;
        bool _completeScreenRedraw;
        sbyte _mouseOverBoxV2;
        protected ushort _inventoryOffset;

        struct V2MouseoverBox
        {
            public Rect rect;
            public byte color;
            public byte hicolor;
        }

        V2MouseoverBox[] _mouseOverBoxesV2 = new V2MouseoverBox[7];

        public ScummEngine2(GameSettings game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            _flashlight.XStrips = 7;
            _flashlight.YStrips = 7;

            /*if (Game.Platform == Platform.NES) {
                InitNESMouseOver();
                _switchRoomEffect2 = _switchRoomEffect = 6;
            } else*/
            {
                InitV2MouseOver();
                // Seems in V2 there was only a single room effect (iris),
                // so we set that here.
                _switchRoomEffect2 = 1;
                _switchRoomEffect = 5;
            }

            _inventoryOffset = 0;

            VariableSentenceVerb = null;
            VariableSentenceObject1 = null;
            VariableSentenceObject2 = null;
            VariableSentencePreposition = null;
            VariableBackupVerb = null;

            VariableClickArea = null;
            VariableClickVerb = null;
            VariableClickObject = null;
        }

        protected override void SetupVars()
        {
            VariableEgo = 0;
            VariableCameraPosX = 2;
            VariableHaveMessage = 3;
            VariableRoom = 4;
            VariableOverride = 5;
            VariableMachineSpeed = 6;
            VariableCharCount = 7;
            VariableActiveVerb = 8;
            VariableActiveObject1 = 9;
            VariableActiveObject2 = 10;
            VariableNumActor = 11;
            VariableCurrentLights = 12;
            VariableCurrentDrive = 13;
            VariableMusicTimer = 17;
            VariableVerbAllowed = 18;
            VariableActorRangeMin = 19;
            VariableActorRangeMax = 20;
            VariableCursorState = 21;
            VariableCameraMinX = 23;
            VariableCameraMaxX = 24;
            VariableTimerNext = 25;
            VariableSentenceVerb = 26;
            VariableSentenceObject1 = 27;
            VariableSentenceObject2 = 28;
            VariableSentencePreposition = 29;
            VariableVirtualMouseX = 30;
            VariableVirtualMouseY = 31;
            VariableClickArea = 32;
            VariableClickVerb = 33;
            VariableClickObject = 35;
            VariableRoomResource = 36;
            VariableLastSound = 37;
            VariableBackupVerb = 38;
            VariableKeyPress = 39;
            VariableCutSceneExitKey = 40;
            VariableTalkActor = 41;
        }

        protected override void ResetScummVars()
        {
            // This needs to be at least greater than 40 to get the more
            // elaborate version of the EGA Zak into. I don't know where
            // else it makes any difference.
            if (Game.GameId == GameId.Zak)
                Variables[VariableMachineSpeed.Value] = 0x7FFF;
        }

        protected override bool IsActor(int id)
        {
            return id < Actors.Length;
        }

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
            _opCodes[0x07] = SetState08;
            /* 08 */
            _opCodes[0x08] = IsNotEqual;
            _opCodes[0x09] = FaceActor;
            _opCodes[0x0a] = AssignVarWordIndirect;
            _opCodes[0x0b] = SetObjPreposition;
            /* 0C */
            _opCodes[0x0c] = ResourceRoutines;
            _opCodes[0x0d] = WalkActorToActor;
            _opCodes[0x0e] = PutActorAtObject;
            _opCodes[0x0f] = IfNotState08;
            /* 10 */
            _opCodes[0x10] = GetObjectOwner;
            _opCodes[0x11] = AnimateActor;
            _opCodes[0x12] = PanCameraTo;
            _opCodes[0x13] = ActorOps;
            /* 14 */
            _opCodes[0x14] = Print;
            _opCodes[0x15] = ActorFromPos;
            _opCodes[0x16] = GetRandomNumber;
            _opCodes[0x17] = ClearState02;
            /* 18 */
            _opCodes[0x18] = JumpRelative;
            _opCodes[0x19] = DoSentence;
            _opCodes[0x1A] = Move;
            _opCodes[0x1b] = SetBitVar;
            /* 1C */
            _opCodes[0x1c] = StartSound;
            _opCodes[0x1d] = IfClassOfIs;
            _opCodes[0x1e] = WalkActorTo;
            _opCodes[0x1f] = IfState02;
            /* 20 */
            _opCodes[0x20] = StopMusic;
            _opCodes[0x21] = PutActor;
            _opCodes[0x22] = SaveLoadGame;
            _opCodes[0x23] = GetActorY;
            /* 24 */
            _opCodes[0x24] = LoadRoomWithEgo;
            _opCodes[0x25] = DrawObject;
            _opCodes[0x26] = SetVarRange;
            _opCodes[0x27] = SetState04;
            /* 28 */
            _opCodes[0x28] = EqualZero;
            _opCodes[0x29] = SetOwnerOf;
            _opCodes[0x2a] = AddIndirect;
            _opCodes[0x2b] = DelayVariable;
            /* 2C */
            _opCodes[0x2c] = AssignVarByte;
            _opCodes[0x2d] = PutActorInRoom;
            _opCodes[0x2e] = Delay;
            _opCodes[0x2f] = IfNotState04;
            /* 30 */
            _opCodes[0x30] = SetBoxFlags;
            _opCodes[0x31] = GetBitVar;
            _opCodes[0x32] = SetCameraAt;
            _opCodes[0x33] = RoomOps;
            /* 34 */
            _opCodes[0x34] = GetDistance;
            _opCodes[0x35] = FindObject;
            _opCodes[0x36] = WalkActorToObject;
            _opCodes[0x37] = SetState01;
            /* 38 */
            _opCodes[0x38] = IsLessEqual;
            _opCodes[0x39] = DoSentence;
            _opCodes[0x3a] = Subtract;
            _opCodes[0x3b] = WaitForActor;
            /* 3C */
            _opCodes[0x3c] = StopSound;
            _opCodes[0x3d] = SetActorElevation;
            _opCodes[0x3e] = WalkActorTo;
            _opCodes[0x3f] = IfNotState01;
            /* 40 */
            _opCodes[0x40] = Cutscene;
            _opCodes[0x41] = PutActor;
            _opCodes[0x42] = StartScript;
            _opCodes[0x43] = GetActorX;
            /* 44 */
            _opCodes[0x44] = IsLess;
            _opCodes[0x45] = DrawObject;
            _opCodes[0x46] = Increment;
            _opCodes[0x47] = ClearState08;
            /* 48 */
            _opCodes[0x48] = IsEqual;
            _opCodes[0x49] = FaceActor;
            _opCodes[0x4a] = ChainScript;
            _opCodes[0x4b] = SetObjPreposition;
            /* 4C */
            _opCodes[0x4c] = WaitForSentence;
            _opCodes[0x4d] = WalkActorToActor;
            _opCodes[0x4e] = PutActorAtObject;
            _opCodes[0x4f] = IfState08;
            /* 50 */
            _opCodes[0x50] = PickupObject;
            _opCodes[0x51] = AnimateActor;
            _opCodes[0x52] = ActorFollowCamera;
            _opCodes[0x53] = ActorOps;
            /* 54 */
            _opCodes[0x54] = SetObjectName;
            _opCodes[0x55] = ActorFromPos;
            _opCodes[0x56] = GetActorMoving;
            _opCodes[0x57] = SetState02;
            /* 58 */
            _opCodes[0x58] = BeginOverride;
            _opCodes[0x59] = DoSentence;
            _opCodes[0x5a] = Add;
            _opCodes[0x5b] = SetBitVar;
            /* 5C */
            _opCodes[0x5c] = Dummy;
            _opCodes[0x5d] = IfClassOfIs;
            _opCodes[0x5e] = WalkActorTo;
            _opCodes[0x5f] = IfNotState02;
            /* 60 */
            _opCodes[0x60] = CursorCommand;
            _opCodes[0x61] = PutActor;
            _opCodes[0x62] = StopScript;
            _opCodes[0x63] = GetActorFacing;
            /* 64 */
            _opCodes[0x64] = LoadRoomWithEgo;
            _opCodes[0x65] = DrawObject;
            _opCodes[0x66] = GetClosestObjActor;
            _opCodes[0x67] = ClearState04;
            /* 68 */
            _opCodes[0x68] = IsScriptRunning;
            _opCodes[0x69] = SetOwnerOf;
            _opCodes[0x6a] = SubIndirect;
            _opCodes[0x6b] = Dummy;
            /* 6C */
            _opCodes[0x6c] = GetObjPreposition;
            _opCodes[0x6d] = PutActorInRoom;
            _opCodes[0x6e] = Dummy;
            _opCodes[0x6f] = IfState04;
            /* 70 */
            _opCodes[0x70] = Lights;
            _opCodes[0x71] = GetActorCostume;
            _opCodes[0x72] = LoadRoom;
            _opCodes[0x73] = RoomOps;
            /* 74 */
            _opCodes[0x74] = GetDistance;
            _opCodes[0x75] = FindObject;
            _opCodes[0x76] = WalkActorToObject;
            _opCodes[0x77] = ClearState01;
            /* 78 */
            _opCodes[0x78] = IsGreater;
            _opCodes[0x79] = DoSentence;
            _opCodes[0x7a] = VerbOps;
            _opCodes[0x7b] = GetActorWalkBox;
            /* 7C */
            _opCodes[0x7c] = IsSoundRunning;
            _opCodes[0x7d] = SetActorElevation;
            _opCodes[0x7e] = WalkActorTo;
            _opCodes[0x7f] = IfState01;
            /* 80 */
            _opCodes[0x80] = BreakHere;
            _opCodes[0x81] = PutActor;
            _opCodes[0x82] = StartMusic;
            _opCodes[0x83] = GetActorRoom;
            /* 84 */
            _opCodes[0x84] = IsGreaterEqual;
            _opCodes[0x85] = DrawObject;
            _opCodes[0x86] = GetActorElevation;
            _opCodes[0x87] = SetState08;
            /* 88 */
            _opCodes[0x88] = IsNotEqual;
            _opCodes[0x89] = FaceActor;
            _opCodes[0x8a] = AssignVarWordIndirect;
            _opCodes[0x8b] = SetObjPreposition;
            /* 8C */
            _opCodes[0x8c] = ResourceRoutines;
            _opCodes[0x8d] = WalkActorToActor;
            _opCodes[0x8e] = PutActorAtObject;
            _opCodes[0x8f] = IfNotState08;
            /* 90 */
            _opCodes[0x90] = GetObjectOwner;
            _opCodes[0x91] = AnimateActor;
            _opCodes[0x92] = PanCameraTo;
            _opCodes[0x93] = ActorOps;
            /* 94 */
            _opCodes[0x94] = Print;
            _opCodes[0x95] = ActorFromPos;
            _opCodes[0x96] = GetRandomNumber;
            _opCodes[0x97] = ClearState02;
            /* 98 */
            _opCodes[0x98] = Restart;
            _opCodes[0x99] = DoSentence;
            _opCodes[0x9a] = Move;
            _opCodes[0x9b] = SetBitVar;
            /* 9C */
            _opCodes[0x9c] = StartSound;
            _opCodes[0x9d] = IfClassOfIs;
            _opCodes[0x9e] = WalkActorTo;
            _opCodes[0x9f] = IfState02;
            /* A0 */
            _opCodes[0xa0] = StopObjectCode;
            _opCodes[0xa1] = PutActor;
            _opCodes[0xa2] = SaveLoadGame;
            _opCodes[0xa3] = GetActorY;
            /* A4 */
            _opCodes[0xa4] = LoadRoomWithEgo;
            _opCodes[0xa5] = DrawObject;
            _opCodes[0xa6] = SetVarRange;
            _opCodes[0xa7] = SetState04;
            /* A8 */
            _opCodes[0xa8] = NotEqualZero;
            _opCodes[0xa9] = SetOwnerOf;
            _opCodes[0xaa] = AddIndirect;
            _opCodes[0xab] = SwitchCostumeSet;
            /* AC */
            _opCodes[0xac] = DrawSentence;
            _opCodes[0xad] = PutActorInRoom;
            _opCodes[0xae] = WaitForMessage;
            _opCodes[0xaf] = IfNotState04;
            /* B0 */
            _opCodes[0xb0] = SetBoxFlags;
            _opCodes[0xb1] = GetBitVar;
            _opCodes[0xb2] = SetCameraAt;
            _opCodes[0xb3] = RoomOps;
            /* B4 */
            _opCodes[0xb4] = GetDistance;
            _opCodes[0xb5] = FindObject;
            _opCodes[0xb6] = WalkActorToObject;
            _opCodes[0xb7] = SetState01;
            /* B8 */
            _opCodes[0xb8] = IsLessEqual;
            _opCodes[0xb9] = DoSentence;
            _opCodes[0xba] = Subtract;
            _opCodes[0xbb] = WaitForActor;
            /* BC */
            _opCodes[0xbc] = StopSound;
            _opCodes[0xbd] = SetActorElevation;
            _opCodes[0xbe] = WalkActorTo;
            _opCodes[0xbf] = IfNotState01;
            /* C0 */
            _opCodes[0xc0] = EndCutscene;
            _opCodes[0xc1] = PutActor;
            _opCodes[0xc2] = StartScript;
            _opCodes[0xc3] = GetActorX;
            /* C4 */
            _opCodes[0xc4] = IsLess;
            _opCodes[0xc5] = DrawObject;
            _opCodes[0xc6] = Decrement;
            _opCodes[0xc7] = ClearState08;
            /* C8 */
            _opCodes[0xc8] = IsEqual;
            _opCodes[0xc9] = FaceActor;
            _opCodes[0xca] = ChainScript;
            _opCodes[0xcb] = SetObjPreposition;
            /* CC */
            _opCodes[0xcc] = PseudoRoom;
            _opCodes[0xcd] = WalkActorToActor;
            _opCodes[0xce] = PutActorAtObject;
            _opCodes[0xcf] = IfState08;
            /* D0 */
            _opCodes[0xd0] = PickupObject;
            _opCodes[0xd1] = AnimateActor;
            _opCodes[0xd2] = ActorFollowCamera;
            _opCodes[0xd3] = ActorOps;
            /* D4 */
            _opCodes[0xd4] = SetObjectName;
            _opCodes[0xd5] = ActorFromPos;
            _opCodes[0xd6] = GetActorMoving;
            _opCodes[0xd7] = SetState02;
            /* D8 */
            _opCodes[0xd8] = PrintEgo;
            _opCodes[0xd9] = DoSentence;
            _opCodes[0xda] = Add;
            _opCodes[0xdb] = SetBitVar;
            /* DC */
            _opCodes[0xdc] = Dummy;
            _opCodes[0xdd] = IfClassOfIs;
            _opCodes[0xde] = WalkActorTo;
            _opCodes[0xdf] = IfNotState02;
            /* E0 */
            _opCodes[0xe0] = CursorCommand;
            _opCodes[0xe1] = PutActor;
            _opCodes[0xe2] = StopScript;
            _opCodes[0xe3] = GetActorFacing;
            /* E4 */
            _opCodes[0xe4] = LoadRoomWithEgo;
            _opCodes[0xe5] = DrawObject;
            _opCodes[0xe6] = GetClosestObjActor;
            _opCodes[0xe7] = ClearState04;
            /* E8 */
            _opCodes[0xe8] = IsScriptRunning;
            _opCodes[0xe9] = SetOwnerOf;
            _opCodes[0xea] = SubIndirect;
            _opCodes[0xeb] = Dummy;
            /* EC */
            _opCodes[0xec] = GetObjPreposition;
            _opCodes[0xed] = PutActorInRoom;
            _opCodes[0xee] = Dummy;
            _opCodes[0xef] = IfState04;
            /* F0 */
            _opCodes[0xf0] = Lights;
            _opCodes[0xf1] = GetActorCostume;
            _opCodes[0xf2] = LoadRoom;
            _opCodes[0xf3] = RoomOps;
            /* F4 */
            _opCodes[0xf4] = GetDistance;
            _opCodes[0xf5] = FindObject;
            _opCodes[0xf6] = WalkActorToObject;
            _opCodes[0xf7] = ClearState01;
            /* F8 */
            _opCodes[0xf8] = IsGreater;
            _opCodes[0xf9] = DoSentence;
            _opCodes[0xfa] = VerbOps;
            _opCodes[0xfb] = GetActorWalkBox;
            /* FC */
            _opCodes[0xfc] = IsSoundRunning;
            _opCodes[0xfd] = SetActorElevation;
            _opCodes[0xfe] = WalkActorTo;
            _opCodes[0xff] = IfState01;
        }

        static readonly byte[] default_v1_cursor_colors =
            {
                1, 1, 12, 11
            };

        protected override void SetBuiltinCursor(int idx)
        {
            var color = (Game.Version <= 1) ? default_v1_cursor_colors[idx] : defaultCursorColors[idx];

            byte[] pixels;
            if (Game.Platform == Platform.Amiga)
            {
                _cursor.Width = 15;
                _cursor.Height = 15;
                _cursor.Hotspot = new Point(7, 7);

                pixels = new byte[_cursor.Width * _cursor.Height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = 0xFF;
                }
                var offs = _cursor.Hotspot.Y * _cursor.Width + _cursor.Hotspot.X;

                // Crosshair, symmetric
                // TODO: Instead of setting this up via code, we should simply extend
                //       default_cursor_images to contain this shape.
                for (var i = 0; i < 5; i++)
                {
                    pixels[offs - 3 - i] = color;
                    pixels[offs + 3 + i] = color;
                    pixels[offs - _cursor.Width * (3 + i)] = color;
                    pixels[offs + _cursor.Width * (3 + i)] = color;
                }

                // Arrow heads, diagonal lines
                for (var i = 1; i <= 2; i++)
                {
                    pixels[offs - _cursor.Width * i - (3 + i)] = color;
                    pixels[offs + _cursor.Width * i - (3 + i)] = color;
                    pixels[offs - _cursor.Width * i + (3 + i)] = color;
                    pixels[offs + _cursor.Width * i + (3 + i)] = color;
                    pixels[offs - _cursor.Width * (3 + i) - i] = color;
                    pixels[offs + _cursor.Width * (3 + i) - i] = color;
                    pixels[offs - _cursor.Width * (3 + i) + i] = color;
                    pixels[offs + _cursor.Width * (3 + i) + i] = color;
                }
            }
            else
            {
                _cursor.Hotspot = new Point(11, 10);
                _cursor.Width = 23;
                _cursor.Height = 21;

                pixels = new byte[_cursor.Width * _cursor.Height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = 0xFF;
                }
                // Crosshair, slightly assymetric
                // TODO: Instead of setting this up via code, we should simply extend
                //       default_cursor_images to contain this shape.

                var offs = _cursor.Hotspot.Y * _cursor.Width + _cursor.Hotspot.X;
                for (var i = 0; i < 7; i++)
                {
                    pixels[offs - 5 - i] = color;
                    pixels[offs + 5 + i] = color;
                }
                for (var i = 0; i < 8; i++)
                {
                    pixels[offs - _cursor.Width * (3 + i)] = color;
                    pixels[offs + _cursor.Width * (3 + i)] = color;
                }

                // Arrow heads, diagonal lines
                for (var i = 1; i <= 3; i++)
                {
                    pixels[offs - _cursor.Width * i - 5 - i] = color;
                    pixels[offs + _cursor.Width * i - 5 - i] = color;
                    pixels[offs - _cursor.Width * i + 5 + i] = color;
                    pixels[offs + _cursor.Width * i + 5 + i] = color;
                    pixels[offs - _cursor.Width * (i + 3) - i] = color;
                    pixels[offs - _cursor.Width * (i + 3) + i] = color;
                    pixels[offs + _cursor.Width * (i + 3) - i] = color;
                    pixels[offs + _cursor.Width * (i + 3) + i] = color;
                }

                // Final touches
                pixels[offs - _cursor.Width - 7] = color;
                pixels[offs - _cursor.Width + 7] = color;
                pixels[offs + _cursor.Width - 7] = color;
                pixels[offs + _cursor.Width + 7] = color;
                pixels[offs - (_cursor.Width * 5) - 1] = color;
                pixels[offs - (_cursor.Width * 5) + 1] = color;
                pixels[offs + (_cursor.Width * 5) - 1] = color;
                pixels[offs + (_cursor.Width * 5) + 1] = color;
            }

            _gfxManager.SetCursor(pixels, _cursor.Width, _cursor.Height, _cursor.Hotspot);
        }

        protected override void SaveOrLoad(Serializer serializer)
        {
            base.SaveOrLoad(serializer);

            if (Game.Version <= 2)
            {
                var v2Entries = new[]
                {
                    LoadAndSaveEntry.Create(reader => _inventoryOffset = reader.ReadUInt16(), writer => writer.WriteUInt16(_inventoryOffset), 39)
                };
                v2Entries.ForEach(entry => entry.Execute(serializer));

                // In old saves we didn't store _inventoryOffset -> reset it to
                // a sane default when loading one of those.
                if (serializer.Version < 79 && serializer.IsLoading)
                {
                    _inventoryOffset = 0;
                }
            }
        }

        protected override void HandleMouseOver(bool updateInventory)
        {
            base.HandleMouseOver(updateInventory);

            if (updateInventory)
            {
                // FIXME/TODO: Reset and redraw the sentence line
                _inventoryOffset = 0;
            }
            if (_completeScreenRedraw || updateInventory)
            {
                RedrawV2Inventory();
            }
            CheckV2MouseOver(_mousePos);
        }

        protected override void CheckExecVerbs()
        {
            int i, over;
            VerbSlot vs;

            if (_userPut <= 0 || mouseAndKeyboardStat == 0)
                return;

            if (mouseAndKeyboardStat < (KeyCode)ScummMouseButtonState.MaxKey)
            {
                /* Check keypresses */
                for (i = 0; i < Verbs.Length; i++)
                {
                    vs = Verbs[i];
                    if (vs.VerbId != 0 && vs.SaveId == 0 && vs.CurMode == 1)
                    {
                        if ((int)mouseAndKeyboardStat == vs.Key)
                        {
                            // Trigger verb as if the user clicked it
                            RunInputScript(ClickArea.Verb, (KeyCode)vs.VerbId, 1);
                            return;
                        }
                    }
                }

                // Simulate inventory picking and scrolling keys
                int obj = -1;

                switch (mouseAndKeyboardStat)
                {
                    case KeyCode.U: // arrow up
                        if (_inventoryOffset >= 2)
                        {
                            _inventoryOffset -= 2;
                            RedrawV2Inventory();
                        }
                        return;
                    case KeyCode.J: // arrow down
                        if (_inventoryOffset + 4 < GetInventoryCountCore(Variables[VariableEgo.Value]))
                        {
                            _inventoryOffset += 2;
                            RedrawV2Inventory();
                        }
                        return;
                    case KeyCode.I: // object
                        obj = 0;
                        break;
                    case KeyCode.O:
                        obj = 1;
                        break;
                    case KeyCode.K:
                        obj = 2;
                        break;
                    case KeyCode.L:
                        obj = 3;
                        break;
                }

                if (obj != -1)
                {
                    obj = FindInventoryCore(Variables[VariableEgo.Value], obj + 1 + _inventoryOffset);
                    if (obj > 0)
                        RunInputScript(ClickArea.Inventory, (KeyCode)obj, 0);
                    return;
                }

                // Generic keyboard input
                RunInputScript(ClickArea.Key, mouseAndKeyboardStat, 1);
            }
            else if ((mouseAndKeyboardStat & (KeyCode)ScummMouseButtonState.MouseMask) != 0)
            {
                var zone = FindVirtScreen(_mousePos.Y);
                int code = (mouseAndKeyboardStat & (KeyCode)ScummMouseButtonState.LeftClick) != 0 ? 1 : 2;
                const int inventoryArea = /*(_game.platform == Common::kPlatformNES) ? 48: */32;

                // This could be kUnkVirtScreen.
                // Fixes bug #1536932: "MANIACNES: Crash on click in speechtext-area"
                if (zone == null)
                    return;

                if (zone == VerbVirtScreen && _mousePos.Y <= zone.TopLine + 8)
                {
                    // Click into V2 sentence line
                    RunInputScript(ClickArea.Sentence, 0, 0);
                }
                else if (zone == VerbVirtScreen && _mousePos.Y > zone.TopLine + inventoryArea)
                {
                    // Click into V2 inventory
                    var obj = CheckV2Inventory(_mousePos.X, _mousePos.Y);
                    if (obj > 0)
                        RunInputScript(ClickArea.Inventory, (KeyCode)obj, 0);
                }
                else
                {
                    over = FindVerbAtPos(_mousePos);
                    if (over != 0)
                    {
                        // Verb was clicked
                        RunInputScript(ClickArea.Verb, (KeyCode)Verbs[over].VerbId, code);
                    }
                    else
                    {
                        // Scene was clicked
                        RunInputScript((zone == MainVirtScreen) ? ClickArea.Scene : ClickArea.Verb, 0, code);
                    }
                }
            }
        }

        protected override void RunInputScript(ClickArea clickArea, KeyCode code, int mode)
        {
            int verbScript;

            verbScript = 4;
            Variables[VariableClickArea.Value] = (int)clickArea;
            switch (clickArea)
            {
                case ClickArea.Verb:        // Verb clicked
                    Variables[VariableClickVerb.Value] = (int)code;
                    break;
                case ClickArea.Inventory:       // Inventory clicked
                    Variables[VariableClickObject.Value] = (int)code;
                    break;
            }

            var args = new[] { (int)clickArea, (int)code, mode };

            if (verbScript != 0)
                RunScript(verbScript, false, false, args);
        }

        protected override void RunInventoryScript(int i)
        {
            RedrawV2Inventory();
        }

        protected override void GetResult()
        {
            _resultVarIndex = ReadByte();
        }

        protected override int GetVar()
        {
            return ReadVariable(ReadByte());
        }

        protected override int ReadVariable(uint var)
        {
            if (Game.Version >= 1 && var >= 14 && var <= 16)
                var = (uint)Variables[var];

            ScummHelper.AssertRange(0, var, Variables.Length - 1, "variable (reading)");
            this.Trace().Write("VARS", "ReadVariable({0}) = {1}", var, Variables[var]);
            return Variables[var];
        }

        protected override void WriteVariable(uint index, int value)
        {
            this.Trace().Write("VARS", "WriteVariable({0}) = {1}", index, value);

            if (VariableCutSceneExitKey.HasValue && index == VariableCutSceneExitKey.Value)
            {
                // Remap the cutscene exit key in earlier games
                if (value == 4 || value == 13 || value == 64)
                    value = 27;
            }

            Variables[index] = value;
        }

        protected void IsSoundRunning()
        {
            GetResult();
            var snd = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (snd != 0)
            {
                snd = Sound.IsSoundRunning(snd) ? 1 : 0;
            }
            SetResult(snd);
        }

        protected virtual void PrintEgo()
        {
            _actorToPrintStrFor = Variables[VariableEgo.Value];
            DecodeParseString();
        }

        protected void GetActorMoving()
        {
            GetResult();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = Actors[act];
            SetResult((int)a.Moving);
        }

        protected virtual void SetObjectName()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetObjectNameCore(obj);
        }

        protected void IsScriptRunning()
        {
            GetResult();
            SetResult(IsScriptRunningCore(GetVarOrDirectByte(OpCodeParameter.Param1)) ? 1 : 0);
        }

        protected void Decrement()
        {
            GetResult();
            SetResult(ReadVariable((uint)_resultVarIndex) - 1);
        }

        protected void PseudoRoom()
        {
            int i = ReadByte(), j;
            while ((j = ReadByte()) != 0)
            {
                if (j >= 0x80)
                {
                    _resourceMapper[j & 0x7F] = (byte)i;
                }
            }
        }

        protected void ActorFollowCamera()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            ActorFollowCamera(actor);
        }

        protected void StopSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            Sound.StopSound(sound);
        }

        protected void NotEqualZero()
        {
            var a = GetVar();
            JumpRelative(a != 0);
        }

        protected void GetDistance()
        {
            GetResult();
            var o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var o2 = GetVarOrDirectWord(OpCodeParameter.Param2);

            int r;
            if (Game.Version == 0) // in v0 both parameters are always actor IDs, never objects
                r = GetObjActToObjActDist(ActorToObj(o1), ActorToObj(o2));
            else
                r = GetObjActToObjActDist(o1, o2);

            // TODO: WORKAROUND bug #795937 ?
            //if ((_game.id == GID_MONKEY_EGA || _game.id == GID_PASS) && o1 == 1 && o2 == 307 && vm.slot[CurrentScript].number == 205 && r == 2)
            //    r = 3;

            SetResult(r);
        }

        protected void SetCameraAtEx(int at)
        {
            if (Game.Version < 7)
            {
                Camera.Mode = CameraMode.Normal;
                Camera.CurrentPosition.X = at;
                SetCameraAt(new Point(at, 0));
                Camera.MovingToActor = false;
            }
        }

        protected void SetBoxFlags()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte();
            SetBoxFlags(a, b);
        }

        protected void SaveLoadGame()
        {
            GetResult();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var result = 0;

            var slot = a & 0x1F;
            // Slot numbers in older games start with 0, in newer games with 1
            if (Game.Version <= 2)
                slot++;
            _opCode = (byte)(a & 0xE0);

            switch (_opCode)
            {
                case 0x00: // num slots available
                    result = 100;
                    break;
                case 0x20: // drive
                    if (Game.Version <= 3)
                    {
                        // 0 = ???
                        // [1,2] = disk drive [A:,B:]
                        // 3 = hard drive
                        result = 3;
                    }
                    else
                    {
                        // set current drive
                        result = 1;
                    }
                    break;
                case 0x40: // load
                    if (LoadState(slot, false))
                        result = 3; // sucess
                    else
                        result = 5; // failed to load
                    break;
                case 0x80: // save
                    if (Game.Version <= 3)
                    {
                        string name;
                        if (Game.Version <= 2)
                        {
                            // use generic name
                            name = string.Format("Game {0}", (char)('A' + slot - 1));
                        }
                        else
                        {
                            // use name entered by the user
                            var firstSlot = StringIdSavename1;
                            name = Encoding.UTF8.GetString(_strings[slot + firstSlot - 1]);
                        }

                        if (SavePreparedSavegame(slot, name))
                            result = 0;
                        else
                            result = 2;
                    }
                    else
                    {
                        result = 2; // failed to save
                    }
                    break;
                case 0xC0: // test if save exists
                    {
                        var availSaves = ListSavegames(100);
                        var filename = MakeSavegameName(slot, false);
                        var directory = ServiceLocator.FileStorage.GetDirectoryName(Game.Path);
                        if (availSaves[slot] && (ServiceLocator.FileStorage.FileExists(ServiceLocator.FileStorage.Combine(directory, filename))))
                        {
                            result = 6; // save file exists
                        }
                        else
                        {
                            result = 7; // save file does not exist
                        }
                    }
                    break;
                    //                default:
                    //                    error("o4_saveLoadGame: unknown subopcode %d", _opcode);
            }

            SetResult(result);
        }

        protected virtual void StopMusic()
        {
            Sound.StopAllSounds();
        }

        protected void StartSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (VariableMusicTimer.HasValue)
            {
                Variables[VariableMusicTimer.Value] = 0;
            }
            Sound.AddSoundToQueue(sound);
        }

        protected void JumpRelative()
        {
            JumpRelative(false);
        }

        protected void Increment()
        {
            GetResult();
            SetResult(ReadVariable((uint)_resultVarIndex) + 1);
        }

        protected void IsEqual()
        {
            uint varNum;
            if (Game.Version <= 2)
                varNum = ReadByte();
            else
                varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a == b);
        }

        protected void DelayVariable()
        {
            Slots[CurrentScript].Delay = GetVar();
            Slots[CurrentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        protected virtual void SetOwnerOf()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var owner = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetOwnerOf(obj, owner);
        }

        protected void EqualZero()
        {
            int a = GetVar();
            JumpRelative(a == 0);
        }

        protected virtual void BreakHere()
        {
            UpdateScriptPointer();
            CurrentScript = 0xFF;
        }

        private void UpdateScriptPointer()
        {
            if (CurrentScript == 0xFF)
                return;

            Slots[CurrentScript].Offset = (uint)CurrentPos;
        }

        protected void LoadRoom()
        {
            var room = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);

            // For small header games, we only call startScene if the room
            // actually changed. This avoid unwanted (wrong) fades in Zak256
            // and others. OTOH, it seems to cause a problem in newer games.
            if ((Game.Version >= 5) || room != CurrentRoom)
            {
                StartScene(room);
            }
            _fullRedraw = true;
        }

        protected void GetActorCostume()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            Actor a = Actors[act];
            SetResult(a.Costume);
        }

        protected void GetActorFacing()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = Actors[act];
            SetResult(ScummHelper.NewDirToOldDir(a.Facing));
        }

        protected void GetRandomNumber()
        {
            GetResult();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);
            var rnd = new Random();
            var value = rnd.Next(max + 1);
            SetResult(value);
        }

        protected virtual void Print()
        {
            _actorToPrintStrFor = GetVarOrDirectByte(OpCodeParameter.Param1);
            DecodeParseString();
        }

        protected void AnimateActor()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var anim = GetVarOrDirectByte(OpCodeParameter.Param2);
            var actor = Actors[act];
            actor.Animate(anim);
        }

        protected virtual void GetObjectOwner()
        {
            GetResult();
            SetResult(GetOwnerCore(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        protected void WalkActorToActor()
        {
            var nr = GetVarOrDirectByte(OpCodeParameter.Param1);
            var nr2 = GetVarOrDirectByte(OpCodeParameter.Param2);
            int dist = ReadByte();

            if (Game.GameId == GameId.Indy4 && nr == 1 && nr2 == 106 &&
                dist == 255 && Slots[CurrentScript].Number == 210)
            {
                // WORKAROUND bug: Work around an invalid actor bug when using the
                // camel in Fate of Atlantis, the "wits" path. The room-65-210 script
                // contains this:
                //   walkActorToActor(1,106,255)
                // Once again this is either a script bug, or there is some hidden
                // or unknown meaning to this odd walk request...
                return;
            }

            var a = Actors[nr];
            if (!a.IsInCurrentRoom)
                return;

            var a2 = Actors[nr2];
            if (!a2.IsInCurrentRoom)
                return;

            WalkActorToActor(nr, nr2, dist);
        }

        protected void WalkActorToActor(int actor, int toActor, int dist)
        {
            var a = Actors[actor];
            var to = Actors[toActor];

            if (Game.Version <= 2)
            {
                dist *= Actor2.V12_X_MULTIPLIER;
            }
            else if (dist == 0xFF)
            {
                dist = (int)(a.ScaleX * a.Width / 0xFF);
                dist += (int)(to.ScaleX * to.Width / 0xFF) / 2;
            }
            int x = to.Position.X;
            int y = to.Position.Y;
            if (x < a.Position.X)
                x += dist;
            else
                x -= dist;

            if (Game.Version <= 2)
            {
                x /= Actor2.V12_X_MULTIPLIER;
                y /= Actor2.V12_Y_MULTIPLIER;
            }
            if (Game.Version <= 3)
            {
                var abr = a.AdjustXYToBeInBox(new Point(x, y));
                x = abr.Position.X;
                y = abr.Position.Y;
            }

            a.StartWalk(new Point(x, y), -1);
        }

        protected void FaceActor()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            var actor = Actors[act];
            actor.FaceToObject(obj);
        }

        protected void IsNotEqual()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(a != b);
        }

        protected void StartMusic()
        {
            if (Game.Platform == Platform.FMTowns && Game.Version == 3)
            {
                // In FM-TOWNS games this is some kind of Audio CD status query function.
                // See also bug #762589 (thanks to Hibernatus for providing the information).
                GetResult();
                int b = GetVarOrDirectByte(OpCodeParameter.Param1);
                int result = 0;
                switch (b)
                {
                    case 0:
                        result = Sound.PollCD() == 0 ? 1 : 0;
                        break;
                    case 0xFC:
                        // TODO: Unpause (resume) audio track. We'll have to extend Sound and OSystem for this.
                        break;
                    case 0xFD:
                        // TODO: Pause audio track. We'll have to extend Sound and OSystem for this.
                        break;
                    case 0xFE:
                        // TODO: FMTOWNS
                        //                        result = Sound.GetCurrentCDSound();
                        break;
                    case 0xFF:
                        result = TownsPlayer.GetCurrentCdaVolume();
                        break;
                    default:
                        // TODO: return track length in seconds. We'll have to extend Sound and OSystem for this.
                        // To check scummvm returns the right track length you
                        // can look at the global script #9 (0x888A in 49.LFL).
                        break;
                }
                Debug.WriteLine("o5_startMusic({0})", b);
                SetResult(result);
            }
            else
            {
                Sound.AddSoundToQueue(GetVarOrDirectByte(OpCodeParameter.Param1));
            }
        }

        protected void GetActorRoom()
        {
            GetResult();
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);

            // WORKAROUND bug #746349. This is a really odd bug in either the script
            // or in our script engine. Might be a good idea to investigate this
            // further by e.g. looking at the FOA engine a bit closer.
            if (Game.GameId == GameId.Indy4 && _roomResource == 94 && Slots[CurrentScript].Number == 206 && !IsValidActor(index))
            {
                SetResult(0);
                return;
            }

            var actor = Actors[index];
            SetResult(actor.Room);
        }

        protected void Move()
        {
            GetResult();
            var result = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(result);
        }

        protected void SetVarRange()
        {
            GetResult();
            var a = ReadByte();
            int b;
            do
            {
                if ((_opCode & 0x80) == 0x80)
                    b = ReadWordSigned();
                else
                    b = ReadByte();
                SetResult(b);
                _resultVarIndex++;
            } while ((--a) > 0);
        }

        void InitV2MouseOver()
        {
            int i;
            int arrow_color, color, hi_color;

            if (Game.Version == 2)
            {
                color = 13;
                hi_color = 14;
                arrow_color = 1;
            }
            else
            {
                color = 16;
                hi_color = 7;
                arrow_color = 6;
            }

            _mouseOverBoxV2 = -1;

            // Inventory items

            for (i = 0; i < 2; i++)
            {
                _mouseOverBoxesV2[2 * i].rect.Left = 0;
                _mouseOverBoxesV2[2 * i].rect.Right = 144;
                _mouseOverBoxesV2[2 * i].rect.Top = 32 + 8 * i;
                _mouseOverBoxesV2[2 * i].rect.Bottom = _mouseOverBoxesV2[2 * i].rect.Top + 8;

                _mouseOverBoxesV2[2 * i].color = (byte)color;
                _mouseOverBoxesV2[2 * i].hicolor = (byte)hi_color;

                _mouseOverBoxesV2[2 * i + 1].rect.Left = 176;
                _mouseOverBoxesV2[2 * i + 1].rect.Right = 320;
                _mouseOverBoxesV2[2 * i + 1].rect.Top = _mouseOverBoxesV2[2 * i].rect.Top;
                _mouseOverBoxesV2[2 * i + 1].rect.Bottom = _mouseOverBoxesV2[2 * i].rect.Bottom;

                _mouseOverBoxesV2[2 * i + 1].color = (byte)color;
                _mouseOverBoxesV2[2 * i + 1].hicolor = (byte)hi_color;
            }

            // Inventory arrows

            _mouseOverBoxesV2[InventoryUpArrow].rect.Left = 144;
            _mouseOverBoxesV2[InventoryUpArrow].rect.Right = 176;
            _mouseOverBoxesV2[InventoryUpArrow].rect.Top = 32;
            _mouseOverBoxesV2[InventoryUpArrow].rect.Bottom = 40;

            _mouseOverBoxesV2[InventoryUpArrow].color = (byte)arrow_color;
            _mouseOverBoxesV2[InventoryUpArrow].hicolor = (byte)hi_color;

            _mouseOverBoxesV2[InventoryDownArrow].rect.Left = 144;
            _mouseOverBoxesV2[InventoryDownArrow].rect.Right = 176;
            _mouseOverBoxesV2[InventoryDownArrow].rect.Top = 40;
            _mouseOverBoxesV2[InventoryDownArrow].rect.Bottom = 48;

            _mouseOverBoxesV2[InventoryDownArrow].color = (byte)arrow_color;
            _mouseOverBoxesV2[InventoryDownArrow].hicolor = (byte)hi_color;

            // Sentence line

            _mouseOverBoxesV2[SentenceLine].rect.Left = 0;
            _mouseOverBoxesV2[SentenceLine].rect.Right = 320;
            _mouseOverBoxesV2[SentenceLine].rect.Top = 0;
            _mouseOverBoxesV2[SentenceLine].rect.Bottom = 8;

            _mouseOverBoxesV2[SentenceLine].color = (byte)color;
            _mouseOverBoxesV2[SentenceLine].hicolor = (byte)hi_color;
        }

        void GetObjPreposition()
        {
            GetResult();
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                var objFound = _objs.Concat(_invData).FirstOrDefault(o => o.Number == obj);
                SetResult(objFound.Preposition);
            }
            else
            {
                SetResult(0xFF);
            }
        }

        void GetActorWalkBox()
        {
            GetResult();
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            SetResult(a.IsInCurrentRoom ? a.Walkbox : 0xFF);
        }

        void GetClosestObjActor()
        {
            int obj;
            int act;
            int dist;

            // This code can't detect any actors farther away than 255 units
            // (pixels in newer games, characters in older ones.) But this is
            // perfectly OK, as it is exactly how the original behaved.

            int closest_obj = 0xFF, closest_dist = 0xFF;

            GetResult();

            act = GetVarOrDirectWord(OpCodeParameter.Param1);
            obj = Variables[VariableActorRangeMax.Value];

            do
            {
                dist = GetObjActToObjActDist(act, obj);
                if (dist < closest_dist)
                {
                    closest_dist = dist;
                    closest_obj = obj;
                }
            } while (--obj >= Variables[VariableActorRangeMin.Value]);

            SetResult(closest_obj);
        }

        void EndCutscene()
        {
            cutScene.StackPointer = 0;

            Variables[VariableOverride.Value] = 0;
            cutScene.Data[0].Script = 0;
            cutScene.Data[0].Pointer = 0;

            Variables[VariableCursorState.Value] = cutScene.Data[1].Data;

            // Reset user state to values before cutscene
            SetUserState((UserStates)cutScene.Data[0].Data | UserStates.SetIFace | UserStates.SetCursor | UserStates.SetFreeze);

            if ((Game.GameId == GameId.Maniac) /* && !(_game.platform == Common::kPlatformNES)*/)
            {
                Camera.Mode = (CameraMode)cutScene.Data[3].Data;
                if (Camera.Mode == CameraMode.FollowActor)
                {
                    ActorFollowCamera(Variables[VariableEgo.Value]);
                }
                else if (cutScene.Data[2].Data != CurrentRoom)
                {
                    StartScene((byte)cutScene.Data[2].Data);
                }
            }
            else
            {
                ActorFollowCamera(Variables[VariableEgo.Value]);
            }
        }

        void WaitForMessage()
        {
            if (Variables[VariableHaveMessage.Value] != 0)
            {
                CurrentPos--;
                BreakHere();
            }
        }

        void VerbOps()
        {
            int verb = ReadByte();
            int slot, state;

            switch (verb)
            {
                case 0:     // SO_DELETE_VERBS
                    slot = GetVarOrDirectByte(OpCodeParameter.Param1) + 1;
                    KillVerb(slot);
                    break;

                case 0xFF:  // Verb On/Off
                    verb = ReadByte();
                    state = ReadByte();
                    slot = GetVerbSlot(verb, 0);
                    Verbs[slot].CurMode = (byte)state;
                    break;

                default:
                    {  // New Verb
                        int x = ReadByte() * 8;
                        int y = ReadByte() * 8;
                        slot = GetVarOrDirectByte(OpCodeParameter.Param1) + 1;
                        int prep = ReadByte(); // Only used in V1?
                        // V1 Maniac verbs are relative to the 'verb area' - under the sentence
                        /*if (_game.platform == Common::kPlatformNES)
                                    x += 8;
                                else*/
                        if ((Game.GameId == GameId.Maniac) && (Game.Version == 1))
                            y += 8;

                        VerbSlot vs = Verbs[slot];
                        vs.VerbId = (ushort)verb;
                        /*if (_game.platform == Common::kPlatformNES) {
                                    vs.color = 1;
                                    vs.hicolor = 1;
                                    vs.dimcolor = 1;
                                } else*/
                        if (Game.Version == 1)
                        {
                            vs.Color = (byte)((Game.GameId == GameId.Maniac && (Game.Features.HasFlag(GameFeatures.Demo))) ? 16 : 5);
                            vs.HiColor = 7;
                            vs.DimColor = 11;
                        }
                        else
                        {
                            vs.Color = (byte)((Game.GameId == GameId.Maniac && (Game.Features.HasFlag(GameFeatures.Demo))) ? 13 : 2);
                            vs.HiColor = 14;
                            vs.DimColor = 8;
                        }
                        vs.Type = VerbType.Text;
                        vs.CharsetNr = String[0].Default.Charset;
                        vs.CurMode = 1;
                        vs.SaveId = 0;
                        vs.Key = 0;
                        vs.Center = false;
                        vs.ImgIndex = 0;
                        vs.Prep = (byte)prep;

                        vs.CurRect.Left = x;
                        vs.CurRect.Top = y;

                        // FIXME: these keyboard map depends on the language of the game.
                        // E.g. a german keyboard has 'z' and 'y' swapped, while a french
                        // keyboard starts with "azerty", etc.
                        /*if (Game.Platform == Platform.NES) {
                                    static const char keyboard[] = {
                                        'q','w','e','r',
                                        'a','s','d','f',
                                        'z','x','c','v'
                                    };
                                    if (1 <= slot && slot <= ARRAYSIZE(keyboard))
                                        vs.key = keyboard[slot - 1];
                                } else*/
                        {
                            char[] keyboard =
                                {
                                    'q', 'w', 'e', 'r', 't',
                                    'a', 's', 'd', 'f', 'g',
                                    'z', 'x', 'c', 'v', 'b'
                                };
                            if (1 <= slot && slot <= keyboard.Length)
                                vs.Key = (byte)keyboard[slot - 1];
                        }

                        // It follows the verb name
                        vs.Text = ReadCharacters();
                    }
                    break;
            }

            // Force redraw of the modified verb slot
            DrawVerb(slot, 0);
            VerbMouseOver(0);
        }

        protected void Restart()
        {
            // TODO: restart
            //            RestartCore();
        }

        protected virtual void ActorFollowCamera(int act)
        {
            if (Game.Version < 7)
            {
                var old = Camera.ActorToFollow;
                SetCameraFollows(Actors[act]);
                if (Camera.ActorToFollow != old)
                    RunInventoryScript(0);

                Camera.MovingToActor = false;
            }
        }

        void PickupObject()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (obj < 1)
            {
                throw new InvalidOperationException(
                    string.Format("pickupObject received invalid index {0} (script {1})", obj, Slots[CurrentScript].Number));
            }

            if (GetObjectIndex(obj) == -1)
                return;

            if (GetWhereIsObject(obj) == WhereIsObject.Inventory)    /* Don't take an */
                return;                                         /* object twice */

            AddObjectToInventory(obj, _roomResource);
            MarkObjectRectAsDirty(obj);
            PutOwner(obj, (byte)Variables[VariableEgo.Value]);
            PutState(obj, GetStateCore(obj) | (byte)ObjectStateV2.State8 | (byte)ObjectStateV2.Untouchable);
            ClearDrawObjectQueue();

            RunInventoryScript(1);
            //            if (Game.Platform == Platform.NES)
            //                Sound.AddSoundToQueue(51);    // play 'pickup' sound
        }

        void WaitForSentence()
        {
            if (SentenceNum == 0 && !IsScriptInUse(SENTENCE_SCRIPT))
                return;

            CurrentPos--;
            BreakHere();
        }

        void SetActorElevation()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int elevation = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[act];
            a.Elevation = elevation;
        }

        void SwitchCostumeSet()
        {
            // NES version of maniac uses this to switch between the two
            // groups of costumes it has
            //            if (Game.Platform == Platform.NES)
            //                NES_loadCostumeSet(ReadByte());
            /*else*/
            if (Game.Platform == Platform.C64)
                ReadByte();
            else
                Dummy();
        }

        protected virtual void Dummy()
        {
            // Opcode 0xEE is used in maniac and zak but has no purpose
            //            if (_opCode != 0xEE)
            //                Console.WriteLine("dummy invoked (opcode {0})", _opCode);
        }

        void WaitForActor()
        {
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            if (a.Moving != MoveFlags.None)
            {
                CurrentPos -= 2;
                BreakHere();
            }
        }

        protected virtual void IsLessEqual()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b <= a);
        }

        void WalkActorToObject()
        {
            int actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            int obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                WalkActorToObject(actor, obj);
            }
        }

        protected void WalkActorToObject(int actor, int obj)
        {
            int dir;
            Point p;
            GetObjectXYPos(obj, out p, out dir);

            var a = Actors[actor];
            var r = a.AdjustXYToBeInBox(p);
            p = r.Position;

            a.StartWalk(p, dir);
        }

        void FindObject()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1) * Actor2.V12_X_MULTIPLIER;
            int y = GetVarOrDirectByte(OpCodeParameter.Param2) * Actor2.V12_Y_MULTIPLIER;
            var obj = FindObjectCore(x, y);
            /*if (obj == 0 && (Game.Platform == Platform.NES) && (_userState & USERSTATE_IFACE_INVENTORY)) {
                if (_mouseOverBoxV2 >= 0 && _mouseOverBoxV2 < 4)
                    obj = findInventory(VAR(VAR_EGO), _mouseOverBoxV2 + _inventoryOffset + 1);
            }*/
            SetResult(obj);
        }

        void CheckV2MouseOver(Point pos)
        {
            var vs = VerbVirtScreen;
            Rect rect;
            int i, x, y, new_box = -1;

            // Don't do anything unless the inventory is active
            if (!_userState.HasFlag(UserStates.IFaceInventory))
            {
                _mouseOverBoxV2 = -1;
                return;
            }

            if (_cursor.State > 0)
            {
                for (i = 0; i < _mouseOverBoxesV2.Length; i++)
                {
                    if (_mouseOverBoxesV2[i].rect.Contains(pos.X, pos.Y - vs.TopLine))
                    {
                        new_box = i;
                        break;
                    }
                }
            }

            if ((new_box != _mouseOverBoxV2) || (Game.Version == 0))
            {
                if (_mouseOverBoxV2 != -1)
                {
                    rect = _mouseOverBoxesV2[_mouseOverBoxV2].rect;

                    var dst = new PixelNavigator(vs.Surfaces[0]);
                    dst.GoTo(rect.Left, rect.Top);

                    // Remove highlight.
                    for (y = rect.Height - 1; y >= 0; y--)
                    {
                        for (x = rect.Width - 1; x >= 0; x--)
                        {
                            if (dst.Read() == _mouseOverBoxesV2[_mouseOverBoxV2].hicolor)
                                dst.Write(_mouseOverBoxesV2[_mouseOverBoxV2].color);
                            dst.OffsetX(1);
                        }
                        dst.Offset(-rect.Width, 1);
                    }

                    MarkRectAsDirty(VerbVirtScreen, rect);
                }

                if (new_box != -1)
                {
                    rect = _mouseOverBoxesV2[new_box].rect;

                    var dst = new PixelNavigator(vs.Surfaces[0]);
                    dst.GoTo(rect.Left, rect.Top);

                    // Apply highlight
                    for (y = rect.Height - 1; y >= 0; y--)
                    {
                        for (x = rect.Width - 1; x >= 0; x--)
                        {
                            if (dst.Read() == _mouseOverBoxesV2[new_box].color)
                                dst.Write(_mouseOverBoxesV2[new_box].hicolor);
                            dst.OffsetX(1);
                        }
                        dst.Offset(-rect.Width, 1);
                    }

                    MarkRectAsDirty(VerbVirtScreen, rect);
                }

                _mouseOverBoxV2 = (sbyte)new_box;
            }
        }

        void RedrawV2Inventory()
        {
            var vs = VerbVirtScreen;
            int i;
            int max_inv;
            Rect inventoryBox;
            int inventoryArea = /*(Game.Platform == Platform.NES) ? 48:*/ 32;
            int maxChars = /*(Game.Platform == Platform.NES) ? 13:*/ 18;

            _mouseOverBoxV2 = -1;

            if (!_userState.HasFlag(UserStates.IFaceInventory))  // Don't draw inventory unless active
                return;

            // Clear on all invocations
            inventoryBox.Top = vs.TopLine + inventoryArea;
            inventoryBox.Bottom = vs.TopLine + vs.Height;
            inventoryBox.Left = 0;
            inventoryBox.Right = vs.Width;
            RestoreBackground(inventoryBox);

            String[1].Charset = 1;

            max_inv = GetInventoryCountCore(Variables[VariableEgo.Value]) - _inventoryOffset;
            if (max_inv > 4)
                max_inv = 4;
            for (i = 0; i < max_inv; i++)
            {
                int obj = FindInventoryCore(Variables[VariableEgo.Value], i + 1 + _inventoryOffset);
                if (obj == 0)
                    break;

                String[1].Position = new Point(_mouseOverBoxesV2[i].rect.Left,
                    _mouseOverBoxesV2[i].rect.Top + vs.TopLine);
                String[1].Right = (short)(_mouseOverBoxesV2[i].rect.Right - 1);
                String[1].Color = _mouseOverBoxesV2[i].color;

                byte[] tmp = GetObjectOrActorName(obj);

                // Prevent inventory entries from overflowing by truncating the text
                byte[] msg = new byte[20];
                msg[maxChars] = 0;
                Array.Copy(tmp, msg, Math.Min(tmp.Length, maxChars));

                // Draw it
                DrawString(1, msg);
            }


            // If necessary, draw "up" arrow
            if (_inventoryOffset > 0)
            {
                String[1].Position = new Point(
                    _mouseOverBoxesV2[InventoryUpArrow].rect.Left,
                    _mouseOverBoxesV2[InventoryUpArrow].rect.Top + vs.TopLine);
                String[1].Right = (short)(_mouseOverBoxesV2[InventoryUpArrow].rect.Right - 1);
                String[1].Color = _mouseOverBoxesV2[InventoryUpArrow].color;
                /*if (_game.platform == Common::kPlatformNES)
                    drawString(1, (const byte *)"\x7E");
                else*/
                DrawString(1, new byte[] { (byte)' ', 1, 2 });
            }

            // If necessary, draw "down" arrow
            if (_inventoryOffset + 4 < GetInventoryCountCore(Variables[VariableEgo.Value]))
            {
                String[1].Position = new Point(
                    _mouseOverBoxesV2[InventoryDownArrow].rect.Left,
                    _mouseOverBoxesV2[InventoryDownArrow].rect.Top + vs.TopLine);
                String[1].Right = (short)(_mouseOverBoxesV2[InventoryDownArrow].rect.Right - 1);
                String[1].Color = _mouseOverBoxesV2[InventoryDownArrow].color;
                /*if (Game.Platform == Platform.NES)
                    DrawString(1, "\x7F");/
                else*/
                DrawString(1, new byte[] { (byte)' ', 3, 4 });
            }
        }

        protected int CheckV2Inventory(int x, int y)
        {
            int inventoryArea = /*(_game.platform == Common::kPlatformNES) ? 48: */32;
            int obj = 0;

            y -= VerbVirtScreen.TopLine;

            if ((y < inventoryArea) || !((mouseAndKeyboardStat & (KeyCode)ScummMouseButtonState.LeftClick) != 0))
                return 0;

            if (_mouseOverBoxesV2[InventoryUpArrow].rect.Contains(x, y))
            {
                if (_inventoryOffset >= 2)
                {
                    _inventoryOffset -= 2;
                    RedrawV2Inventory();
                }
            }
            else if (_mouseOverBoxesV2[InventoryDownArrow].rect.Contains(x, y))
            {
                if (_inventoryOffset + 4 < GetInventoryCountCore(Variables[VariableEgo.Value]))
                {
                    _inventoryOffset += 2;
                    RedrawV2Inventory();
                }
            }

            for (obj = 0; obj < 4; obj++)
            {
                if (_mouseOverBoxesV2[obj].rect.Contains(x, y))
                {
                    break;
                }
            }

            if (obj >= 4)
                return 0;

            return FindInventoryCore(Variables[VariableEgo.Value], obj + 1 + _inventoryOffset);
        }

        protected virtual void SetCameraAt()
        {
            SetCameraAtEx(GetVarOrDirectByte(OpCodeParameter.Param1) * Actor2.V12_X_MULTIPLIER);
        }

        void GetBitVar()
        {
            GetResult();
            var var = ReadWord();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);

            int bit_var = (int)(var + a);
            int bit_offset = bit_var & 0x0f;
            bit_var >>= 4;

            SetResult((Variables[bit_var] & (1 << bit_offset)) != 0 ? 1 : 0);
        }

        void IfClassOfIs()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var clsop = GetVarOrDirectByte(OpCodeParameter.Param2);

            var ob = _objs.Concat(_invData).FirstOrDefault(o => o.Number == obj);
            var obcd = ob.Script.Data;

            if (obcd == null)
            {
                JumpRelative();
                return;
            }

            var cls = obcd.Length < 7 ? 0 : obcd[6];
            JumpRelative((cls & clsop) == clsop);
        }

        protected virtual void WalkActorTo()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);

            // WORKAROUND bug #1252606
            if (Game.GameId == GameId.Zak && Game.Version == 1 && Slots[CurrentScript].Number == 115 && act == 249)
            {
                act = Variables[VariableEgo.Value];
            }

            var a = Actors[act];
            var x = GetVarOrDirectByte(OpCodeParameter.Param2);
            var y = GetVarOrDirectByte(OpCodeParameter.Param3);

            a.StartWalk(new Point(x, y), -1);
        }

        protected virtual void IsLess()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);

            JumpRelative(b < a);
        }

        void ChainScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            StopScript(Slots[CurrentScript].Number);
            CurrentScript = 0xFF;
            RunScript(script, false, false, new int[0]);
        }

        void SetBitVar()
        {
            var var = ReadWord();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);

            int bit_var = (int)(var + a);
            int bit_offset = bit_var & 0x0f;
            bit_var >>= 4;

            if (GetVarOrDirectByte(OpCodeParameter.Param2) != 0)
                Variables[bit_var] |= (1 << bit_offset);
            else
                Variables[bit_var] &= ~(1 << bit_offset);
        }

        void AddIndirect()
        {
            GetResultPosIndirect();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            Variables[_resultVarIndex] += a;
        }

        void SubIndirect()
        {
            GetResultPosIndirect();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            Variables[_resultVarIndex] -= a;
        }

        protected virtual void Add()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            Variables[_resultVarIndex] += a;
        }

        protected virtual void Subtract()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            Variables[_resultVarIndex] -= a;
        }

        protected virtual void BeginOverride()
        {
            cutScene.Data[0].Pointer = CurrentPos;
            cutScene.Data[0].Script = CurrentScript;

            // Skip the jump instruction following the override instruction
            ReadByte();
            FetchScriptWord();
        }

        void AssignVarByte()
        {
            GetResult();
            SetResult(ReadByte());
        }

        protected virtual void PutActorInRoom()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[act];

            a.Room = (byte)room;
            if (room == 0)
            {
                if (Game.GameId == GameId.Maniac && Game.Version <= 1 /*&& Game.Platform != Platform.NES*/)
                    a.Facing = 180;

                a.PutActor(new Point(0, 0), 0);
            }
        }

        protected virtual void Delay()
        {
            int delay = ReadByte();
            delay |= ReadByte() << 8;
            delay |= ReadByte() << 16;
            delay = 0xFFFFFF - delay;

            Slots[CurrentScript].Delay = delay;
            Slots[CurrentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        void IfState01()
        {
            IfStateCommon(ObjectStateV2.Pickupable);
        }

        protected void IfState02()
        {
            IfStateCommon(ObjectStateV2.Untouchable);
        }

        protected void IfState04()
        {
            IfStateCommon(ObjectStateV2.Locked);
        }

        protected void IfState08()
        {
            IfStateCommon(ObjectStateV2.State8);
        }

        void IfNotState01()
        {
            IfNotStateCommon(ObjectStateV2.Pickupable);
        }

        protected void IfNotState02()
        {
            IfNotStateCommon(ObjectStateV2.Untouchable);
        }

        protected void IfNotState04()
        {
            IfNotStateCommon(ObjectStateV2.Locked);
        }

        protected void IfNotState08()
        {
            IfNotStateCommon(ObjectStateV2.State8);
        }

        void RoomOps()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);

            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:         // SO_ROOM_SCROLL
                    a *= 8;
                    b *= 8;
                    if (a < (ScreenWidth / 2))
                        a = (ScreenWidth / 2);
                    if (b < (ScreenWidth / 2))
                        b = (ScreenWidth / 2);
                    if (a > roomData.Header.Width - (ScreenWidth / 2))
                        a = roomData.Header.Width - (ScreenWidth / 2);
                    if (b > roomData.Header.Width - (ScreenWidth / 2))
                        b = roomData.Header.Width - (ScreenWidth / 2);
                    Variables[VariableCameraMinX.Value] = a;
                    Variables[VariableCameraMaxX.Value] = b;
                    break;
                case 2:         // SO_ROOM_COLOR
                    if (Game.Version == 1)
                    {
                        // V1 zak needs to know when room color is changed
                        Gdi.RoomPalette[0] = 255;
                        Gdi.RoomPalette[1] = (byte)a;
                        Gdi.RoomPalette[2] = (byte)b;
                    }
                    else
                    {
                        Gdi.RoomPalette[b] = (byte)a;
                    }
                    _fullRedraw = true;
                    break;
            }
        }

        void Lights()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte();
            var c = ReadByte();

            if (c == 0)
            {
                if (Game.GameId == GameId.Maniac && Game.Version == 1 /*&& !(Game.Platform == Platform.NES)*/)
                {
                    // Convert older light mode values into
                    // equivalent values of later games.
                    // 0 Darkness
                    // 1 Flashlight
                    // 2 Lighted area
                    if (a == 2)
                        Variables[VariableCurrentLights.Value] = 11;
                    else if (a == 1)
                        Variables[VariableCurrentLights.Value] = 4;
                    else
                        Variables[VariableCurrentLights.Value] = 0;
                }
                else
                    Variables[VariableCurrentLights.Value] = a;
            }
            else if (c == 1)
            {
                _flashlight.XStrips = (ushort)a;
                _flashlight.YStrips = b;
            }
            _fullRedraw = true;
        }

        protected void StopScriptCommon(int script)
        {
            if (Game.GameId == GameId.Maniac && _roomResource == 26 && Slots[CurrentScript].Number == 10001)
            {
                // FIXME: Nasty hack for bug #915575
                // Don't let the exit script for room 26 stop the script (116), when
                // switching to the dungeon (script 89)
                if (Game.Version >= 1 && script == 116 && IsScriptRunningCore(89))
                    return;
                // Script numbers are different in V0
                if (Game.Version == 0 && script == 111 && IsScriptRunningCore(84))
                    return;
            }

            if (script == 0)
                script = Slots[CurrentScript].Number;

            if (CurrentScript != 0 && Slots[CurrentScript].Number == script)
                StopObjectCode();
            else
                StopScript(script);
        }

        protected virtual void StopScript()
        {
            StopScriptCommon(GetVarOrDirectByte(OpCodeParameter.Param1));
        }

        void CursorCommand()
        {   // TODO: Define the magic numbers
            var cmd = GetVarOrDirectWord(OpCodeParameter.Param1);
            var state = cmd >> 8;

            if ((cmd & 0xFF) != 0)
            {
                Variables[VariableCursorState.Value] = cmd & 0xFF;
            }

            SetUserState((UserStates)state);
        }

        protected virtual void GetActorX()
        {
            GetResult();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            SetResult(GetObjX(ActorToObj(a)));
        }

        protected virtual void GetActorY()
        {
            GetResult();
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            SetResult(GetObjY(ActorToObj(a)));
        }

        protected virtual void StartScript()
        {
            int script = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (!Settings.CopyProtection)
            {
                // The enhanced version of Zak McKracken included in the
                // SelectWare Classic Collection bundle used CD check instead
                // of the usual key code check at airports.
                if ((Game.GameId == GameId.Zak) && (script == 15) && (_roomResource == 45))
                    return;
            }

            // WORKAROUND bug #1447058: In Maniac Mansion, when the door bell
            // rings, then this normally causes Ted Edison to leave his room.
            // This is controlled by script 87. On the other hand, when the
            // player enters Ted's room while Ted is in it, then Ted captures
            // the player and puts his active ego into the cellar prison.
            //
            // Unfortunately, the two events can collide: If the cutscene is
            // playing in which Ted captures the player (controlled by script
            // 88) and simultaneously the door bell rings (due to package
            // delivery...) then this leads to an assertion (in ScummVM, due to
            // its stricter validity checking), or to unexpected / strange
            // behavior (in the original engine). The script writers apparently
            // anticipated the possibility of the door bell ringing: Before
            // script 91 starts script 88, it explicitly stops script 87.
            // Unfortunately, this is not quite enough, as script 87 can be
            // started while script 88 is already running -- specifically, by
            // the package delivery sequence.
            //
            // Now, one can easily suppress this particular assertion, but then
            // one still gets odd behavior: Ted is in the process of
            // incarcerating the player, when the door bell rings; Ted promptly
            // leaves to get the package, leaving the player alone (!), but then
            // moments later we cut to the cellar, where Ted just put the
            // player. That seems weird and irrational (the Edisons may be mad,
            // but they are not stupid when it comes to putting people into
            // their dungeon ;)
            //
            // To avoid this, we use a somewhat more elaborate workaround: If
            // script 88 or 89 are running (which control the capture resp.
            // imprisonment of the player), then any attempt to start script 87
            // (which makes Ted go answer the door bell) is simply ignored. This
            // way, the door bell still chimes, but Ted ignores it.
            if (Game.GameId == GameId.Maniac)
            {
                if (Game.Version >= 1 && script == 87)
                {
                    if (IsScriptRunningCore(88) || IsScriptRunningCore(89))
                        return;
                }
                // Script numbers are different in V0
                if (Game.Version == 0 && script == 82)
                {
                    if (IsScriptRunningCore(83) || IsScriptRunningCore(84))
                        return;
                }
            }

            RunScript(script, false, false, new int[0]);
        }

        void Cutscene()
        {
            cutScene.Data[0].Data = ((int)_userState | (_userPut != 0 ? 16 : 0));
            cutScene.Data[1].Data = Variables[VariableCursorState.Value];
            cutScene.Data[2].Data = CurrentRoom;
            cutScene.Data[3].Data = (int)Camera.Mode;

            Variables[VariableCursorState.Value] = 200;

            // Hide inventory, freeze scripts, hide cursor
            SetUserState(UserStates.SetIFace |
                UserStates.SetCursor |
                UserStates.SetFreeze | UserStates.FreezeOn);

            SentenceNum = 0;
            StopScript(SENTENCE_SCRIPT);
            ResetSentence();

            cutScene.Data[0].Pointer = 0;
        }

        protected void SetUserState(UserStates state)
        {
            if (state.HasFlag(UserStates.SetIFace))
            {          // Userface
                //                if (Game.Platform == Platform.NES)
                //                    _userState = (_userState & ~USERSTATE_IFACE_ALL) | (state & USERSTATE_IFACE_ALL);
                //                else
                _userState = state & UserStates.IFaceAll;
            }

            if (state.HasFlag(UserStates.SetFreeze))
            {     // Freeze
                if (state.HasFlag(UserStates.FreezeOn))
                    FreezeScripts(0);
                else
                    UnfreezeScripts();
            }

            if (state.HasFlag(UserStates.SetCursor))
            {         // Cursor Show/Hide
                //                if (_game.Platform == Common::kPlatformNES)
                //                    _userState = (_userState & ~USERSTATE_CURSOR_ON) | (state & USERSTATE_CURSOR_ON);
                if (state.HasFlag(UserStates.CursorOn))
                {
                    _userPut = 1;
                    _cursor.State = 1;
                }
                else
                {
                    _userPut = 0;
                    _cursor.State = 0;
                }
            }

            // Hide all verbs and inventory
            Rect rect;
            rect.Top = VerbVirtScreen.TopLine;
            rect.Bottom = VerbVirtScreen.TopLine + 8 * 88;
            rect.Right = VerbVirtScreen.Width - 1;
            //            if (_game.platform == Common::kPlatformNES)
            //            {
            //                rect.left = 16;
            //            }
            //            else
            {
                rect.Left = 0;
            }
            RestoreBackground(rect);

            // Draw all verbs and inventory
            RedrawVerbs();
            RunInventoryScript(1);
        }

        void DoSentence()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (a == 0xFC)
            {
                SentenceNum = 0;
                StopScript(SENTENCE_SCRIPT);
                return;
            }
            if (a == 0xFB)
            {
                ResetSentence();
                return;
            }

            var st = Sentence[SentenceNum++] = new Sentence(
                         (byte)a,
                         (ushort)GetVarOrDirectWord(OpCodeParameter.Param2),
                         (ushort)GetVarOrDirectWord(OpCodeParameter.Param3));

            // Execute or print the sentence
            _opCode = ReadByte();
            switch (_opCode)
            {
                case 0:
                    // Do nothing (besides setting up the sentence above)
                    break;
                case 1:
                    // Execute the sentence
                    SentenceNum--;

                    if (st.Verb == 254)
                    {
                        StopObjectScriptCore(st.ObjectA);
                    }
                    else
                    {
                        bool isBackgroundScript;
                        bool isSpecialVerb;
                        if (st.Verb != 253 && st.Verb != 250)
                        {
                            Variables[VariableActiveVerb.Value] = st.Verb;
                            Variables[VariableActiveObject1.Value] = st.ObjectA;
                            Variables[VariableActiveObject2.Value] = st.ObjectB;

                            isBackgroundScript = false;
                            isSpecialVerb = false;
                        }
                        else
                        {
                            isBackgroundScript = (st.Verb == 250);
                            isSpecialVerb = true;
                            st = Sentence[SentenceNum] = new Sentence(
                                253,
                                st.ObjectA,
                                st.ObjectB);
                        }

                        // Check if an object script for this object is already running. If
                        // so, reuse its script slot. Note that we abuse two script flags:
                        // freezeResistant and recursive. We use them to track two
                        // script flags used in V1/V2 games. The main reason we do it this
                        // ugly evil way is to avoid having to introduce yet another save
                        // game revision.
                        int slot = -1;
                        for (var i = 0; i < Slots.Length; i++)
                        {
                            var ss = Slots[i];
                            if (st.ObjectA == ss.Number &&
                                ss.FreezeResistant == isBackgroundScript &&
                                ss.Recursive == isSpecialVerb &&
                                (ss.Where == WhereIsObject.Room || ss.Where == WhereIsObject.Inventory || ss.Where == WhereIsObject.FLObject))
                            {
                                slot = i;
                                break;
                            }
                        }

                        RunObjectScript(st.ObjectA, st.Verb, isBackgroundScript, isSpecialVerb, new int[0], slot);
                    }
                    break;
                case 2:
                    // Print the sentence
                    SentenceNum--;

                    Variables[VariableSentenceVerb.Value] = st.Verb;
                    Variables[VariableSentenceObject1.Value] = st.ObjectA;
                    Variables[VariableSentenceObject2.Value] = st.ObjectB;

                    DrawSentence();
                    break;
                default:
                    throw new NotSupportedException(string.Format("DoSentence: unknown subopcode {0}", _opCode));
            }
        }

        void DrawSentence()
        {
            Rect sentenceline;

            int slot = GetVerbSlot(Variables[VariableSentenceVerb.Value], 0);

            if (!(_userState.HasFlag(UserStates.IFaceSentence) ||
                (/*Game.Platform == Platform.NES*/ false && _userState.HasFlag(UserStates.IFaceAll))))
                return;

            if (Verbs[slot] != null && Verbs[slot].Text != null)
                _sentenceBuf = Encoding.UTF8.GetString(Verbs[slot].Text);
            else
                return;

            if (Variables[VariableSentenceObject1.Value] > 0)
            {
                var temp = GetObjectOrActorName(Variables[VariableSentenceObject1.Value]);
                if (temp != null)
                {
                    _sentenceBuf += " ";
                    _sentenceBuf += Encoding.UTF8.GetString(temp);
                }

                //                 For V1 games, the engine must compute the preposition.
                //                 In all other Scumm versions, this is done by the sentence script.
                if ((Game.GameId == GameId.Maniac && Game.Version == 1 /*&& !(Game.Platform == Platform.NES)*/) && (Variables[VariableSentencePreposition.Value] == 0))
                {
                    if (Verbs[slot].Prep == 0xFF)
                    {
                        var obj = _invData.Concat(_objs).First(o => o.Number == Variables[VariableSentenceObject1.Value]);
                        Variables[VariableSentencePreposition.Value] = obj.Preposition;
                    }
                    else
                        Variables[VariableSentencePreposition.Value] = Verbs[slot].Prep;
                }
            }

            if (0 < Variables[VariableSentencePreposition.Value] && Variables[VariableSentencePreposition.Value] <= 4)
            {
                DrawPreposition(Variables[VariableSentencePreposition.Value]);
            }

            if (Variables[VariableSentenceObject2.Value] > 0)
            {
                var temp = GetObjectOrActorName(Variables[VariableSentenceObject2.Value]);
                if (temp != null)
                {
                    _sentenceBuf += " ";
                    _sentenceBuf += Encoding.UTF8.GetString(temp);
                }
            }

            String[2].Charset = 1;
            String[2].Position = new Point(0, VerbVirtScreen.TopLine);
            String[2].Right = (short)(VerbVirtScreen.Width - 1);
            /*if (Game.Platform == Platform.NES) {
                String[2].Position.X = 16;
                String[2].Color = 0;
            } else*/
            if (Game.Version == 1)
                String[2].Color = 16;
            else
                String[2].Color = 13;

            byte[] str = new byte[80];
            int i = 0, len = 0;

            // Maximum length of printable characters
            int maxChars = /*(Game.Platform == Platform.NES) ? 60 :*/ 40;
            for (i = 0; i < _sentenceBuf.Length; i++)
            {
                if (_sentenceBuf[i] != '@')
                    len++;
                if (len > maxChars)
                {
                    break;
                }
                str[i] = (byte)_sentenceBuf[i];
            }
            str[i] = 0;

            /*if (Game.Platform == Platform.NES) {
                sentenceline.Top = _virtscr[kVerbVirtScreen].topline;
                sentenceline.Bottom = _virtscr[kVerbVirtScreen].topline + 16;
                sentenceline.left  = 16;
                sentenceline.Right = _virtscr[kVerbVirtScreen].w - 1;
            } else*/
            {
                sentenceline.Top = VerbVirtScreen.TopLine;
                sentenceline.Bottom = VerbVirtScreen.TopLine + 8;
                sentenceline.Left = 0;
                sentenceline.Right = VerbVirtScreen.Width - 1;
            }
            RestoreBackground(sentenceline);

            DrawString(2, str);
        }

        protected void DrawPreposition(int index)
        {
            // The prepositions, like the fonts, were hard code in the engine. Thus
            // we have to do that, too, and provde localized versions for all the
            // languages MM/Zak are available in.
            var prepositions = new string[5, 5]
            {
                { " ", " in", " with", " on", " to" },   // English
                { " ", " mit", " mit", " mit", " zu" },  // German
                { " ", " dans", " avec", " sur", " <" }, // French
                { " ", " in", " con", " su", " a" },     // Italian
                { " ", " en", " con", " en", " a" },     // Spanish
            };
            int lang;
            switch (Game.Culture.TwoLetterISOLanguageName)
            {
                case "de":
                    lang = 1;
                    break;
                case "fr":
                    lang = 2;
                    break;
                case "it":
                    lang = 3;
                    break;
                case "es":
                    lang = 4;
                    break;
                default:
                    lang = 0;   // Default to english
                    break;
            }

            /*if (Game.Platform == Common::kPlatformNES) {
                _sentenceBuf += (const char *)(getResourceAddress(rtCostume, 78) + VAR(VAR_SENTENCE_PREPOSITION) * 8 + 2);
            } else*/
            _sentenceBuf += prepositions[lang, index];
        }

        protected void ActorFromPos()
        {
            GetResult();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1) * Actor2.V12_X_MULTIPLIER;
            var y = GetVarOrDirectByte(OpCodeParameter.Param2) * Actor2.V12_Y_MULTIPLIER;
            SetResult(GetActorFromPos(new Point(x, y)));
        }

        void ActorOps()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int arg = GetVarOrDirectByte(OpCodeParameter.Param2);
            int i;

            _opCode = ReadByte();
            if (act == 0 && _opCode == 5)
            {
                // This case happens in the Zak/MM bootscripts, to set the default talk color (9).
                String[0].Color = (byte)arg;
                return;
            }

            var a = Actors[act];

            switch (_opCode)
            {
                case 1:     // SO_SOUND
                    a.Sound = arg;
                    break;
                case 2:     // SO_PALETTE
                    if (Game.Version == 1)
                        i = act;
                    else
                        i = ReadByte();

                    a.SetPalette(i, (ushort)arg);
                    break;
                case 3:     // SO_ACTOR_NAME
                    a.Name = ReadCharacters();
                    break;
                case 4:     // SO_COSTUME
                    a.SetActorCostume((ushort)arg);
                    break;
                case 5:     // SO_TALK_COLOR
                    if (Game.GameId == GameId.Maniac && Game.Version == 2 && Game.Features.HasFlag(GameFeatures.Demo) && arg == 1)
                        a.TalkColor = 15;
                    else
                        a.TalkColor = (byte)arg;
                    break;
                default:
                    throw new NotSupportedException(string.Format("ActorOps: opcode {0} not yet supported", _opCode));
            }
        }

        protected virtual void PanCameraTo()
        {
            PanCameraToCore(new Point(GetVarOrDirectByte(OpCodeParameter.Param1) * Actor2.V12_X_MULTIPLIER, 0));
        }

        protected void SetState04()
        {
            SetStateCommon(ObjectStateV2.Locked);
        }

        protected void ClearState04()
        {
            ClearStateCommon(ObjectStateV2.Locked);
        }

        protected void SetState02()
        {
            SetStateCommon(ObjectStateV2.Untouchable);
        }

        protected void ClearState02()
        {
            ClearStateCommon(ObjectStateV2.Untouchable);
        }

        void SetState01()
        {
            SetStateCommon(ObjectStateV2.Pickupable);
        }

        void ClearState01()
        {
            ClearStateCommon(ObjectStateV2.Pickupable);
        }

        protected void ClearState08()
        {
            var obj = GetActiveObject();
            PutState(obj, GetStateCore(obj) & ~(byte)ObjectStateV2.State8);
            MarkObjectRectAsDirty(obj);
            ClearDrawObjectQueue();
        }

        void IfStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            JumpRelative((GetStateCore(obj) & (byte)type) != 0);
        }

        void IfNotStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            JumpRelative((GetStateCore(obj) & (byte)type) == 0);
        }

        void SetStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            PutState(obj, GetStateCore(obj) | (byte)type);
        }

        void ClearStateCommon(ObjectStateV2 type)
        {
            var obj = GetActiveObject();
            PutState(obj, GetStateCore(obj) & ~(byte)type);
        }

        void PutActorAtObject()
        {
            Point p;
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];
            var obj = GetVarOrDirectWord(OpCodeParameter.Param2);
            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                p = GetObjectXYPos(obj);
                var r = a.AdjustXYToBeInBox(p);
                p = r.Position;
            }
            else
            {
                p = new Point(30, 60);
            }

            a.PutActor(p);
        }

        void ResourceRoutines()
        {

            ResType[] resTypes =
                {
                    ResType.Invalid,
                    ResType.Invalid,
                    ResType.Costume,
                    ResType.Room,
                    ResType.Invalid,
                    ResType.Script,
                    ResType.Sound
                };

            int resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            int opcode = ReadByte();

            ResType type = ResType.Invalid;
            if (0 <= (opcode >> 4) && (opcode >> 4) < resTypes.Length)
                type = resTypes[opcode >> 4];

            if ((opcode & 0x0f) == 0 || type == ResType.Invalid)
                return;

            // HACK V2 Maniac Mansion tries to load an invalid sound resource in demo script.
            if (Game.GameId == GameId.Maniac && Game.Version == 2 && Slots[CurrentScript].Number == 9 && type == ResType.Sound && resid == 1)
                return;

            if ((opcode & 0x0f) == 1)
            {
                switch (type)
                {
                    case ResType.Room:
                        ResourceManager.LoadRoom(resid);
                        break;
                    case ResType.Script:
                        ResourceManager.LoadScript(resid);
                        break;
                    case ResType.Costume:
                        ResourceManager.LoadCostume(resid);
                        break;
                    case ResType.Sound:
                        ResourceManager.LoadSound(Sound.MusicType, resid);
                        break;
                }
            }
            else
            {
                // TODO: vs lock/unlock
                //                if (opcode & 1)
                //                    _res.lock(type, resid);
                //                else
                //                    _res.unlock(type, resid);
            }
        }

        void SetObjPreposition()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int unk = ReadByte();

            //            if (Game.platform == Common::kPlatformNES)
            //                return;

            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                // FIXME: this might not work properly the moment we save and restore the game.
                throw new NotSupportedException();
            }
        }

        void GetResultPosIndirect()
        {
            _resultVarIndex = Variables[ReadByte()];
        }

        void AssignVarWordIndirect()
        {
            GetResultPosIndirect();
            SetResult(GetVarOrDirectWord(OpCodeParameter.Param1));
        }

        protected void SetState08()
        {
            int obj = GetActiveObject();
            PutState(obj, GetStateCore(obj) | (byte)ObjectStateV2.State8);
            MarkObjectRectAsDirty(obj);
            ClearDrawObjectQueue();
        }

        protected virtual int GetActiveObject()
        {
            return GetVarOrDirectWord(OpCodeParameter.Param1);
        }

        void GetActorElevation()
        {
            GetResult();
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = Actors[act];
            SetResult(a.Elevation);
        }

        void DrawObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var xpos = GetVarOrDirectByte(OpCodeParameter.Param2);
            var ypos = GetVarOrDirectByte(OpCodeParameter.Param3);

            var idx = GetObjectIndex(obj);
            if (idx == -1)
                return;

            var od = _objs[idx];
            if (xpos != 0xFF)
            {
                od.Walk = new Point(od.Walk.X + (xpos * 8) - od.Position.X, od.Walk.Y + (ypos * 8) - od.Position.Y);
                od.Position = new Point(xpos * 8, ypos * 8);
            }
            AddObjectToDrawQue((byte)idx);

            var x = od.Position.X;
            var y = od.Position.Y;
            var w = od.Width;
            var h = od.Height;

            var i = _objs.Length;
            while ((i--) != 0)
            {
                if (_objs[i].Number != 0 && _objs[i].Position.X == x && _objs[i].Position.Y == y && _objs[i].Width == w && _objs[i].Height == h)
                    PutState(_objs[i].Number, GetStateCore(_objs[i].Number) & ~(byte)ObjectStateV2.State8);
            }

            PutState(obj, GetStateCore(od.Number) | (byte)ObjectStateV2.State8);
        }

        protected virtual void IsGreater()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b > a);
        }

        protected virtual void IsGreaterEqual()
        {
            var a = GetVar();
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b >= a);
        }

        protected virtual void PutActor()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var a = Actors[act];
            var x = GetVarOrDirectByte(OpCodeParameter.Param2);
            var y = GetVarOrDirectByte(OpCodeParameter.Param3);

            if (Game.GameId == GameId.Maniac && Game.Version <= 1 /*&& Game.Platform != Platform.NES*/)
                a.Facing = 180;

            a.PutActor(new Point(x, y));
        }

        protected virtual void DecodeParseString()
        {
            byte[] buffer = new byte[512];
            var ptr = 0;
            byte c;
            bool insertSpace;

            while ((c = ReadByte()) != 0)
            {
                insertSpace = (c & 0x80) != 0;
                c &= 0x7f;

                if (c < 8)
                {
                    // Special codes as seen in CHARSET_1 etc. My guess is that they
                    // have a similar function as the corresponding embedded stuff in modern
                    // games. Hence for now we convert them to the modern format.
                    // This might allow us to reuse the existing code.
                    buffer[ptr++] = 0xFF;
                    buffer[ptr++] = c;
                    if (c > 3)
                    {
                        buffer[ptr++] = ReadByte();
                        buffer[ptr++] = 0;
                    }
                }
                else
                    buffer[ptr++] = c;

                if (insertSpace)
                    buffer[ptr++] = (byte)' ';

            }
            buffer[ptr++] = 0;

            const int textSlot = 0;
            String[textSlot].Position = new Point();
            String[textSlot].Right = (short)(ScreenWidth - 1);
            String[textSlot].Center = false;
            String[textSlot].Overhead = false;

            if (Game.GameId == GameId.Maniac && _actorToPrintStrFor == 0xFF)
            {
                if (Game.Version == 0)
                {
                    String[textSlot].Color = 14;
                }
                else if (Game.Features.HasFlag(GameFeatures.Demo))
                {
                    String[textSlot].Color = (byte)((Game.Version == 2) ? 15 : 1);
                }
            }

            ActorTalk(buffer);
        }

        void LoadRoomWithEgo()
        {
            int x, y, dir;

            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var room = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = Actors[Variables[VariableEgo.Value]];

            // The original interpreter sets the actors new room X/Y to the last rooms X/Y
            // This fixes a problem with MM: script 161 in room 12, the 'Oomph!' script
            // This scripts runs before the actor position is set to the correct room entry location
            if ((Game.GameId == GameId.Maniac) /*&& (Game.Platform != Platform.NES)*/)
            {
                a.PutActor(a.Position, room);
            }
            else
            {
                a.PutActor(room);
            }
            EgoPositioned = false;

            x = (sbyte)ReadByte();
            y = (sbyte)ReadByte();

            StartScene(a.Room, a, obj);

            Point p2;
            GetObjectXYPos(obj, out p2, out dir);
            AdjustBoxResult r = a.AdjustXYToBeInBox(p2);
            p2 = r.Position;
            a.PutActor(p2, CurrentRoom);
            a.SetDirection(dir + 180);

            Camera.DestinationPosition.X = Camera.CurrentPosition.X = a.Position.X;
            SetCameraAt(a.Position);
            SetCameraFollows(a);

            _fullRedraw = true;

            ResetSentence();

            if (x >= 0 && y >= 0)
            {
                a.StartWalk(new Point(x, y), -1);
            }
            RunScript(5, false, false, new int[0]);
        }

        void ResetSentence()
        {
            Variables[VariableSentenceVerb.Value] = Variables[VariableBackupVerb.Value];
            Variables[VariableSentenceObject1.Value] = 0;
            Variables[VariableSentenceObject2.Value] = 0;
            Variables[VariableSentencePreposition.Value] = 0;
        }
    }
}
