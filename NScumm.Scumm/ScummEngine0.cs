//
//  ScummEngine0.cs
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
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;
using System.Collections.Generic;
using System.Diagnostics;

namespace NScumm.Core
{
    public enum Engine0Mode
    {
        /// <summary>
        /// Cutscene active.
        /// </summary>
        Cutscene = 0,
        /// <summary>
        /// Kid selection / dial pad / save-load dialog.
        /// </summary>
        Keypad = 1,
        /// <summary>
        /// Verb "new kid" disabled (e.g. when entering lab)
        /// </summary>
        NoNewKid = 2,
        /// <summary>
        /// Normal playing mode.
        /// </summary>
        Normal = 3
    }

    public enum VerbsV0
    {
        None = 0,
        Open = 1,
        Close = 2,
        Give = 3,
        TurnOn = 4,
        TurnOff = 5,
        Fix = 6,
        NewKid = 7,
        Unlock = 8,
        Push = 9,
        Pull = 10,
        Use = 11,
        Read = 12,
        WalkTo = 13,
        PickUp = 14,
        WhatIs = 15
    }

    enum VerbPrepsV0
    {
        None = 0,
        In = 1,
        With = 2,
        On = 3,
        To = 4,
        /// <summary>
        /// prep depends on object (USE)
        /// </summary>
        Object = 0xFF
    }

    enum WalkToObjectState
    {
        Done = 0,
        Walk = 1,
        Turn = 2
    }

    enum ObjectV0Type
    {
        /// <summary>
        /// foreground object
        /// - with owner/state, might (but has not to) be pickupable
        /// . with entry in _objectOwner/StateTable
        /// . all objects in _inventory have this type
        /// - image can be exchanged (background overlay)
        /// </summary>
        Foreground = 0,
        /// <summary>
        /// background object
        ///   - without owner/state, not pickupable  (room only)
        ///     . without entry in _objectOwner/StateTable
        ///   - image cannot be exchanged (part of background image)
        /// </summary>
        Background = 1,
        /// <summary>
        /// Object is an actor.
        /// </summary>
        Actor = 2
    }

    public enum ActorV0MiscFlags
    {
        Strong = 0x01,
        // Kid is strong (Hunk-O-Matic used)
        GTFriend = 0x02,
        // Kid is green tentacle's friend (recording contract)
        WatchedTV = 0x04,
        // Kid knows publisher's address (watched TV)
        EdsEnemy = 0x08,
        // Kid is not Weird Ed's friend
        _10 = 0x10,
        // ???
        _20 = 0x20,
        // ???
        Freeze = 0x40,
        // Stop moving
        Hide = 0x80
        // Kid is invisible (dead or in radiation suit)
    }

    public partial class ScummEngine0 : ScummEngine2
    {
        bool _drawDemo;
        bool _redrawSentenceLine;
        Engine0Mode _currentMode;
        byte _currentLights;

        /// <summary>
        /// Selected verb.
        /// </summary>
        VerbsV0 _activeVerb;
        /// <summary>
        /// 1st selected object (see OBJECT_V0())
        /// </summary>
        int _activeObject;
        /// <summary>
        /// 2nd selected object or actor (see OBJECT_V0())
        /// </summary>
        int _activeObject2;

        /// <summary>
        /// Script verb.
        /// </summary>
        VerbsV0 _cmdVerb;
        /// <summary>
        /// 1st script object (see OBJECT_V0()).
        /// </summary>
        int _cmdObject;
        /// <summary>
        /// 2nd script object or actor (see OBJECT_V0()).
        /// </summary>
        int _cmdObject2;
        int _sentenceNestedCount;

        int _walkToObject;
        WalkToObjectState _walkToObjectState;

        int? VariableIsSoundRunning;

        public ScummEngine0(GameSettings game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            VariableActiveObject2 = null;
            VariableIsSoundRunning = null;
            VariableActiveVerb = null;

            ResetVerbs();
        }

        protected override TimeSpan Loop()
        {
            Variables[VariableIsSoundRunning.Value] = (Sound.LastSound != 0) && Sound.IsSoundRunning(Sound.LastSound) ? 1 : 0;

            return base.Loop();
        }

        protected override void InitOpCodes()
        {
            _opCodes = new Dictionary<byte, Action>();

            /* 00 */
            _opCodes[0x00] = StopObjectCode;
            _opCodes[0x01] = PutActor;
            _opCodes[0x02] = StartMusic;
            _opCodes[0x03] = DoSentence;
            /* 04 */
            _opCodes[0x04] = IsGreaterEqual;
            _opCodes[0x05] = StopCurrentScript;
            _opCodes[0x06] = GetDistance;
            _opCodes[0x07] = GetActorRoom;
            /* 08 */
            _opCodes[0x08] = IsNotEqual;
            _opCodes[0x09] = StopCurrentScript;
            _opCodes[0x0a] = StopCurrentScript;
            _opCodes[0x0b] = SetActorBitVar;
            /* 0C */
            _opCodes[0x0c] = LoadSound;
            _opCodes[0x0d] = PrintEgo;
            _opCodes[0x0e] = PutActorAtObject;
            _opCodes[0x0f] = ClearState02;
            /* 10 */
            _opCodes[0x10] = BreakHere;
            _opCodes[0x11] = AnimateActorCore;
            _opCodes[0x12] = PanCameraTo;
            _opCodes[0x13] = LockCostume;
            /* 14 */
            _opCodes[0x14] = Print;
            _opCodes[0x15] = WalkActorToActor;
            _opCodes[0x16] = GetRandomNumber;
            _opCodes[0x17] = ClearState08;
            /* 18 */
            _opCodes[0x18] = JumpRelative;
            _opCodes[0x19] = StopCurrentScript;
            _opCodes[0x1a] = Move;
            _opCodes[0x1b] = GetActorBitVar;
            /* 1C */
            _opCodes[0x1c] = StartSound;
            _opCodes[0x1d] = SetBitVar;
            _opCodes[0x1e] = WalkActorTo;
            _opCodes[0x1f] = IfState04;
            /* 20 */
            _opCodes[0x20] = StopMusic;
            _opCodes[0x21] = PutActor;
            _opCodes[0x22] = SaveLoadGame;
            _opCodes[0x23] = StopCurrentScript;
            /* 24 */
            _opCodes[0x24] = IfNotEqualActiveObject2;
            _opCodes[0x25] = LoadRoom;
            _opCodes[0x26] = GetClosestActor;
            _opCodes[0x27] = GetActorY;
            /* 28 */
            _opCodes[0x28] = EqualZero;
            _opCodes[0x29] = SetOwnerOf;
            _opCodes[0x2a] = Delay;
            _opCodes[0x2b] = SetActorBitVar;
            /* 2C */
            _opCodes[0x2c] = StopCurrentScript;
            _opCodes[0x2d] = PutActorInRoom;
            _opCodes[0x2e] = Print;
            _opCodes[0x2f] = IfState08;
            /* 30 */
            _opCodes[0x30] = LoadCostume;
            _opCodes[0x31] = GetBitVar;
            _opCodes[0x32] = SetCameraAt;
            _opCodes[0x33] = LockScript;
            /* 34 */
            _opCodes[0x34] = GetDistance;
            _opCodes[0x35] = StopCurrentScript;
            _opCodes[0x36] = WalkActorToObject;
            _opCodes[0x37] = ClearState04;
            /* 38 */
            _opCodes[0x38] = IsLessEqual;
            _opCodes[0x39] = StopCurrentScript;
            _opCodes[0x3a] = Subtract;
            _opCodes[0x3b] = StopCurrentScript;
            /* 3C */
            _opCodes[0x3c] = StopSound;
            _opCodes[0x3d] = SetBitVar;
            _opCodes[0x3e] = WalkActorTo;
            _opCodes[0x3f] = IfState02;
            /* 40 */
            _opCodes[0x40] = Cutscene;
            _opCodes[0x41] = PutActor;
            _opCodes[0x42] = StartScript;
            _opCodes[0x43] = DoSentence;
            /* 44 */
            _opCodes[0x44] = IsLess;
            _opCodes[0x45] = StopCurrentScript;
            _opCodes[0x46] = Increment;
            _opCodes[0x47] = GetActorX;
            /* 48 */
            _opCodes[0x48] = IsEqual;
            _opCodes[0x49] = StopCurrentScript;
            _opCodes[0x4a] = LoadRoomCore;
            _opCodes[0x4b] = SetActorBitVar;
            /* 4C */
            _opCodes[0x4c] = LoadScript;
            _opCodes[0x4d] = LockRoom;
            _opCodes[0x4e] = PutActorAtObject;
            _opCodes[0x4f] = ClearState02;
            /* 50 */
            _opCodes[0x50] = Nop;
            _opCodes[0x51] = AnimateActorCore;
            _opCodes[0x52] = ActorFollowCamera;
            _opCodes[0x53] = LockSound;
            /* 54 */
            _opCodes[0x54] = SetObjectName;
            _opCodes[0x55] = WalkActorToActor;
            _opCodes[0x56] = GetActorMoving;
            _opCodes[0x57] = ClearState08;
            /* 58 */
            _opCodes[0x58] = BeginOverride;
            _opCodes[0x59] = StopCurrentScript;
            _opCodes[0x5a] = Add;
            _opCodes[0x5b] = GetActorBitVar;
            /* 5C */
            _opCodes[0x5c] = StartSound;
            _opCodes[0x5d] = SetBitVar;
            _opCodes[0x5e] = WalkActorTo;
            _opCodes[0x5f] = IfState04;
            /* 60 */
            _opCodes[0x60] = SetMode;
            _opCodes[0x61] = PutActor;
            _opCodes[0x62] = StopScript;
            _opCodes[0x63] = StopCurrentScript;
            /* 64 */
            _opCodes[0x64] = IfEqualActiveObject2;
            _opCodes[0x65] = StopCurrentScript;
            _opCodes[0x66] = GetClosestActor;
            _opCodes[0x67] = GetActorFacing;
            /* 68 */
            _opCodes[0x68] = IsScriptRunning;
            _opCodes[0x69] = SetOwnerOf;
            _opCodes[0x6a] = StopCurrentScript;
            _opCodes[0x6b] = SetActorBitVar;
            /* 6C */
            _opCodes[0x6c] = StopCurrentScript;
            _opCodes[0x6d] = PutActorInRoom;
            _opCodes[0x6e] = ScreenPrepare;
            _opCodes[0x6f] = IfState08;
            /* 70 */
            _opCodes[0x70] = Lights;
            _opCodes[0x71] = GetBitVar;
            _opCodes[0x72] = Nop;
            _opCodes[0x73] = GetObjectOwner;
            /* 74 */
            _opCodes[0x74] = GetDistance;
            _opCodes[0x75] = PrintEgo;
            _opCodes[0x76] = WalkActorToObject;
            _opCodes[0x77] = ClearState04;
            /* 78 */
            _opCodes[0x78] = IsGreater;
            _opCodes[0x79] = StopCurrentScript;
            _opCodes[0x7a] = StopCurrentScript;
            _opCodes[0x7b] = StopCurrentScript;
            /* 7C */
            _opCodes[0x7c] = IsSoundRunning;
            _opCodes[0x7d] = SetBitVar;
            _opCodes[0x7e] = WalkActorTo;
            _opCodes[0x7f] = IfNotState02;
            /* 80 */
            _opCodes[0x80] = StopCurrentScript;
            _opCodes[0x81] = PutActor;
            _opCodes[0x82] = StopCurrentScript;
            _opCodes[0x83] = DoSentence;
            /* 84 */
            _opCodes[0x84] = IsGreaterEqual;
            _opCodes[0x85] = StopCurrentScript;
            _opCodes[0x86] = Nop;
            _opCodes[0x87] = GetActorRoom;
            /* 88 */
            _opCodes[0x88] = IsNotEqual;
            _opCodes[0x89] = StopCurrentScript;
            _opCodes[0x8a] = StopCurrentScript;
            _opCodes[0x8b] = SetActorBitVar;
            /* 8C */
            _opCodes[0x8c] = LoadSound;
            _opCodes[0x8d] = StopCurrentScript;
            _opCodes[0x8e] = PutActorAtObject;
            _opCodes[0x8f] = SetState02;
            /* 90 */
            _opCodes[0x90] = PickupObject;
            _opCodes[0x91] = AnimateActorCore;
            _opCodes[0x92] = PanCameraTo;
            _opCodes[0x93] = UnlockCostume;
            /* 94 */
            _opCodes[0x94] = Print;
            _opCodes[0x95] = ActorFromPos;
            _opCodes[0x96] = StopCurrentScript;
            _opCodes[0x97] = SetState08;
            /* 98 */
            _opCodes[0x98] = Restart;
            _opCodes[0x99] = StopCurrentScript;
            _opCodes[0x9a] = Move;
            _opCodes[0x9b] = GetActorBitVar;
            /* 9C */
            _opCodes[0x9c] = StartSound;
            _opCodes[0x9d] = SetBitVar;
            _opCodes[0x9e] = WalkActorTo;
            _opCodes[0x9f] = IfNotState04;
            /* A0 */
            _opCodes[0xa0] = StopObjectCode;
            _opCodes[0xa1] = PutActor;
            _opCodes[0xa2] = SaveLoadGame;
            _opCodes[0xa3] = StopCurrentScript;
            /* A4 */
            _opCodes[0xa4] = IfNotEqualActiveObject2;
            _opCodes[0xa5] = LoadRoom;
            _opCodes[0xa6] = StopCurrentScript;
            _opCodes[0xa7] = GetActorY;
            /* A8 */
            _opCodes[0xa8] = NotEqualZero;
            _opCodes[0xa9] = SetOwnerOf;
            _opCodes[0xaa] = StopCurrentScript;
            _opCodes[0xab] = SetActorBitVar;
            /* AC */
            _opCodes[0xac] = StopCurrentScript;
            _opCodes[0xad] = PutActorInRoom;
            _opCodes[0xae] = Print;
            _opCodes[0xaf] = IfNotState08;
            /* B0 */
            _opCodes[0xb0] = LoadCostume;
            _opCodes[0xb1] = GetBitVar;
            _opCodes[0xb2] = SetCameraAt;
            _opCodes[0xb3] = UnlockScript;
            /* B4 */
            _opCodes[0xb4] = GetDistance;
            _opCodes[0xb5] = StopCurrentScript;
            _opCodes[0xb6] = WalkActorToObject;
            _opCodes[0xb7] = SetState04;
            /* B8 */
            _opCodes[0xb8] = IsLessEqual;
            _opCodes[0xb9] = StopCurrentScript;
            _opCodes[0xba] = Subtract;
            _opCodes[0xbb] = StopCurrentScript;
            /* BC */
            _opCodes[0xbc] = StopSound;
            _opCodes[0xbd] = SetBitVar;
            _opCodes[0xbe] = WalkActorTo;
            _opCodes[0xbf] = IfNotState02;
            /* C0 */
            _opCodes[0xc0] = EndCutscene;
            _opCodes[0xc1] = PutActor;
            _opCodes[0xc2] = StartScript;
            _opCodes[0xc3] = DoSentence;
            /* C4 */
            _opCodes[0xc4] = IsLess;
            _opCodes[0xc5] = StopCurrentScript;
            _opCodes[0xc6] = Decrement;
            _opCodes[0xc7] = GetActorX;
            /* C8 */
            _opCodes[0xc8] = IsEqual;
            _opCodes[0xc9] = StopCurrentScript;
            _opCodes[0xca] = LoadRoomCore;
            _opCodes[0xcb] = SetActorBitVar;
            /* CC */
            _opCodes[0xcc] = LoadScript;
            _opCodes[0xcd] = UnlockRoom;
            _opCodes[0xce] = PutActorAtObject;
            _opCodes[0xcf] = SetState02;
            /* D0 */
            _opCodes[0xd0] = Nop;
            _opCodes[0xd1] = AnimateActorCore;
            _opCodes[0xd2] = ActorFollowCamera;
            _opCodes[0xd3] = UnlockSound;
            /* D4 */
            _opCodes[0xd4] = SetObjectName;
            _opCodes[0xd5] = ActorFromPos;
            _opCodes[0xd6] = GetActorMoving;
            _opCodes[0xd7] = SetState08;
            /* D8 */
            _opCodes[0xd8] = StopCurrentScript;
            _opCodes[0xd9] = StopCurrentScript;
            _opCodes[0xda] = Add;
            _opCodes[0xdb] = GetActorBitVar;
            /* DC */
            _opCodes[0xdc] = StartSound;
            _opCodes[0xdd] = SetBitVar;
            _opCodes[0xde] = WalkActorTo;
            _opCodes[0xdf] = IfNotState04;
            /* E0 */
            _opCodes[0xe0] = SetMode;
            _opCodes[0xe1] = PutActor;
            _opCodes[0xe2] = StopScript;
            _opCodes[0xe3] = StopCurrentScript;
            /* E4 */
            _opCodes[0xe4] = IfEqualActiveObject2;
            _opCodes[0xe5] = LoadRoomWithEgo;
            _opCodes[0xe6] = StopCurrentScript;
            _opCodes[0xe7] = GetActorFacing;
            /* E8 */
            _opCodes[0xe8] = IsScriptRunning;
            _opCodes[0xe9] = SetOwnerOf;
            _opCodes[0xea] = StopCurrentScript;
            _opCodes[0xeb] = SetActorBitVar;
            /* EC */
            _opCodes[0xec] = StopCurrentScript;
            _opCodes[0xed] = PutActorInRoom;
            _opCodes[0xee] = Dummy;
            _opCodes[0xef] = IfNotState08;
            /* F0 */
            _opCodes[0xf0] = Lights;
            _opCodes[0xf1] = GetBitVar;
            _opCodes[0xf2] = Nop;
            _opCodes[0xf3] = GetObjectOwner;
            /* F4 */
            _opCodes[0xf4] = GetDistance;
            _opCodes[0xf5] = StopCurrentScript;
            _opCodes[0xf6] = WalkActorToObject;
            _opCodes[0xf7] = SetState04;
            /* F8 */
            _opCodes[0xf8] = IsGreater;
            _opCodes[0xf9] = StopCurrentScript;
            _opCodes[0xfa] = StopCurrentScript;
            _opCodes[0xfb] = StopCurrentScript;
            /* FC */
            _opCodes[0xfc] = IsSoundRunning;
            _opCodes[0xfd] = SetBitVar;
            _opCodes[0xfe] = WalkActorTo;
            _opCodes[0xff] = IfState02;
        }

        void AnimateActorCore()
        {
            int act = GetVarOrDirectByte(OpCodeParameter.Param1);
            int anim = GetVarOrDirectByte(OpCodeParameter.Param2);
            sbyte repeat = (sbyte)ReadByte();

            var a = (Actor0)Actors[act];

            a.AnimFrameRepeat = repeat;

            switch (anim)
            {

                case 0xFE:
                    // 0x6993
                    a.Speaking = 0x80;    // Enabled, but not switching
                    return;

                case 0xFD:
                    // 0x69A3
                    a.Speaking = 0x00;
                    return;

                case 0xFF:
                    a.StopActorMoving();
                    return;
            }

            a.Animate(anim);
        }

        protected override void DecodeParseString()
        {
            byte[] buffer = new byte[512];
            var ptr = 0;
            byte c;
            bool insertSpace;

            while ((c = ReadByte()) != 0)
            {
                insertSpace = (c & 0x80) != 0;
                c &= 0x7f;

                if (c == '/')
                {
                    buffer[ptr++] = 13;
                }
                else
                {
                    buffer[ptr++] = c;
                }

                if (insertSpace)
                    buffer[ptr++] = (byte)' ';

            }
            buffer[ptr++] = 0;

            const int textSlot = 0;
            String[textSlot].Position = new Point();
            String[textSlot].Right = (short)(ScreenWidth - 1);
            String[textSlot].Center = false;
            String[textSlot].Overhead = false;

            if (_actorToPrintStrFor == 0xFF)
                String[textSlot].Color = 14;

            ActorTalk(buffer);
        }

        protected override void CheckAndRunSentenceScript()
        {
            if (CheckPendingWalkAction())
                return;

            if (SentenceNum == 0 || Sentence[SentenceNum - 1].IsFrozen)
                return;

            var st = Sentence[SentenceNum - 1];

            if (st.Preposition && st.ObjectB == st.ObjectA)
            {
                SentenceNum--;
                return;
            }

            CurrentScript = 0xFF;

            //            assert(st.objectA);

            // If two objects are involved, at least one must be in the actors inventory
            if (st.ObjectB != 0 &&
                (OBJECT_V0_TYPE(st.ObjectA) != ObjectV0Type.Foreground || ResourceManager.ObjectOwnerTable[st.ObjectA] != Variables[VariableEgo.Value]) &&
                (OBJECT_V0_TYPE(st.ObjectB) != ObjectV0Type.Foreground || ResourceManager.ObjectOwnerTable[st.ObjectB] != Variables[VariableEgo.Value]))
            {
                if (GetVerbEntrypointCore(st.ObjectA, (int)VerbsV0.PickUp) != 0)
                    DoSentence((int)VerbsV0.PickUp, st.ObjectA, 0);
                else if (GetVerbEntrypointCore(st.ObjectB, (int)VerbsV0.PickUp) != 0)
                    DoSentence((int)VerbsV0.PickUp, st.ObjectB, 0);
                else
                    SentenceNum--;
                return;
            }

            _cmdVerb = (VerbsV0)st.Verb;
            _cmdObject = st.ObjectA;
            _cmdObject2 = st.ObjectB;
            SentenceNum--;

            // abort sentence execution if the number of nested scripts is too high.
            // This might happen for instance if the sentence command depends on an
            // object that the actor has to pick-up in a nested doSentence() call.
            // If the actor is not able to pick-up the object (e.g. because it is not
            // reachable or pickupable) a nested pick-up command is triggered again
            // and again, so the actual sentence command will never be executed.
            // In this case the sentence command has to be aborted.
            _sentenceNestedCount++;
            if (_sentenceNestedCount > 6)
            {
                _sentenceNestedCount = 0;
                SentenceNum = 0;
                return;
            }

            if (GetWhereIsObject(st.ObjectA) != WhereIsObject.Inventory)
            {
                if (_currentMode != Engine0Mode.Keypad)
                {
                    WalkToActorOrObject(st.ObjectA);
                    return;
                }
            }
            else if (st.ObjectB != 0 && GetWhereIsObject(st.ObjectB) != WhereIsObject.Inventory)
            {
                WalkToActorOrObject(st.ObjectB);
                return;
            }

            RunSentenceScript();
            if (_currentMode == Engine0Mode.Keypad)
            {
                _walkToObjectState = WalkToObjectState.Done;
            }
        }

        protected override uint ReadWord()
        {
            return ReadByte();
        }

        protected override int ReadWordSigned()
        {
            return ReadByte();
        }

        protected override int GetActiveObject()
        {
            if ((_opCode & (byte)OpCodeParameter.Param2) != 0)
                return OBJECT_V0_ID(_cmdObject);

            return ReadByte();
        }

        protected override void Print()
        {
            _actorToPrintStrFor = ReadByte();
            DecodeParseString();
        }

        protected override void PrintEgo()
        {
            _actorToPrintStrFor = (byte)Variables[VariableEgo.Value];
            DecodeParseString();
        }

        protected override void SetObjectName()
        {
            int obj;
            int objId = ReadByte();
            if (objId == 0)
            {
                obj = _cmdObject;
            }
            else
            {
                if ((_opCode & 0x80) != 0)
                    obj = OBJECT_V0(objId, ObjectV0Type.Background);
                else
                    obj = OBJECT_V0(objId, ObjectV0Type.Foreground);
            }
            SetObjectNameCore(obj);
        }

        protected override void GetObjectOwner()
        {
            GetResult();
            var owner = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetOwnerCore(owner != 0 ? owner : _cmdObject));
        }

        protected override void SetOwnerOf()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var owner = GetVarOrDirectByte(OpCodeParameter.Param2);
            if (obj==0)
                obj = _cmdObject;
            SetOwnerOf(obj, owner);
        }

        internal override LightModes GetCurrentLights()
        {
            return (LightModes)_currentLights;
        }

        protected override bool AreBoxesNeighbors(byte box1nr, byte box2nr)
        {
            var boxm = roomData.BoxMatrix;
            // TODO: what are the first bytes for (mostly 0)?
            var index = 4;
            // For each box, the matrix contains an arbitrary number
            // of box indices that are linked with the box (neighbors).
            // Each list is separated by 0xFF (|).
            // E.g. "1 | 0 3 | 3 | 1 2" means:
            //   0 -> 1, 1 -> 0/3, 2 -> 3, 3 -> 1/2

            // Skip up to the matrix data for box 'box1nr'
            for (var i = 0; i < box1nr; i++)
            {
                while (boxm[index] != 0xFF)
                    index++;
                index++;
            }

            // Now search for the entry for box 'box2nr'
            while (boxm[index] != 0xFF)
            {
                if (boxm[index] == box2nr)
                    return true;
                index++;
            }

            return false;
        }

        internal byte WalkboxFindTarget(Actor a, int destbox, Point walkdest)
        {
            var actor = (Actor0)a;

            byte nextBox = (byte)GetNextBox(a.Walkbox, (byte)destbox);

            if (nextBox != 0xFF && nextBox == destbox && AreBoxesNeighbors(a.Walkbox, nextBox))
            {
                actor.NewWalkTo = walkdest;
                return nextBox;
            }

            if (nextBox != 0xFF && nextBox != a.Walkbox)
            {
                Point p;
                ScummMath.GetClosestPtOnBox(GetBoxCoordinates(nextBox), a.Position, out p);
                actor.NewWalkTo = p;
            }
            else
            {
                if (walkdest.X == -1)
                    actor.NewWalkTo = actor.CurrentWalkTo;
                else
                    actor.NewWalkTo = walkdest;
            }
            return nextBox;
        }

        void LoadRoomWithEgo()
        {
            var obj = ReadByte();
            var room = ReadByte();

            var a = (Actor0)Actors[Variables[VariableEgo.Value]];

            //0x634F
            if (a.MiscFlags.HasFlag(ActorV0MiscFlags.Freeze))
            {
                StopObjectCode();
                return;
            }

            // The original interpreter sets the actors new room X/Y to the last rooms X/Y
            // This fixes a problem with MM: script 158 in room 12, the 'Oomph!' script
            // This scripts runs before the actor position is set to the correct room entry location
            a.PutActor(a.Position, room);
            EgoPositioned = false;

            StartScene(a.Room, a, obj);

            Point p;
            int dir;
            GetObjectXYPos(obj, out p, out dir);
            AdjustBoxResult r = a.AdjustXYToBeInBox(p);
            p = r.Position;
            a.PutActor(p, CurrentRoom);

            Camera.DestinationPosition.X = Camera.CurrentPosition.X = a.Position.X;
            SetCameraAt(a.Position);
            SetCameraFollows(a);

            _fullRedraw = true;

            ResetSentence();

            if (p.X >= 0 && p.Y >= 0)
            {
                a.StartWalk(p, -1);
            }
        }

        bool CheckSentenceComplete()
        {
            if (_activeVerb != VerbsV0.None && _activeVerb != VerbsV0.WalkTo && _activeVerb != VerbsV0.WhatIs)
            {
                if ((_activeObject != 0) && ((ActiveVerbPrep() == 0) || _activeObject2 != 0))
                    return true;
            }
            return false;
        }

        void ClearSentenceLine()
        {
            Rect sentenceline;
            sentenceline.Top = VerbVirtScreen.TopLine;
            sentenceline.Bottom = VerbVirtScreen.TopLine + 8;
            sentenceline.Left = 0;
            sentenceline.Right = VerbVirtScreen.Width - 1;
            RestoreBackground(sentenceline);
        }

        void FlushSentenceLine()
        {
            byte[] buffer = new byte[80];
            int i = 0, len = 0;

            // Maximum length of printable characters
            int maxChars = 40;
            for (i = 0; i < _sentenceBuf.Length; i++)
            {
                var chr = _sentenceBuf[i];
                if (chr != '@')
                    buffer[i] = (byte)chr;
                if (len > maxChars)
                {
                    break;
                }
            }
            buffer[i] = 0;

            String[2].Charset = 1;
            String[2].Position = new Point(0, VerbVirtScreen.TopLine);
            String[2].Right = (short)(VerbVirtScreen.Width - 1);
            String[2].Color = 16;
            DrawString(2, buffer);
        }

        void DrawSentenceObject(int obj)
        {
            var temp = GetObjectOrActorName(obj);
            if (temp != null)
            {
                _sentenceBuf += " ";
                _sentenceBuf += System.Text.Encoding.UTF8.GetString(temp);
            }
        }

        void DrawSentenceLine()
        {
            _redrawSentenceLine = false;

            if (!_userState.HasFlag(UserStates.IFaceSentence))
                return;

            ClearSentenceLine();

            if (_activeVerb == VerbsV0.NewKid)
            {
                _sentenceBuf = "";
                for (int i = 0; i < 3; ++i)
                {
                    string actorName;
                    int actorId = Variables[97 + i];
                    if (actorId == 0)
                    {
                        // after usage of the radiation suit, kid vars are set to 0
                        actorName = " ";
                    }
                    else
                    {
                        var a = Actors[actorId];
                        actorName = System.Text.Encoding.UTF8.GetString(a.Name);
                    }
                    _sentenceBuf += string.Format("{0,-13}", actorName);
                }
                FlushSentenceLine();
                return;
            }

            // Current Verb
            if (_activeVerb == VerbsV0.None)
                _activeVerb = VerbsV0.WalkTo;

            var verbName = Verbs[(int)_activeVerb].Text;
            _sentenceBuf = System.Text.Encoding.UTF8.GetString(verbName);

            if (_activeObject != 0)
            {
                // Draw the 1st active object
                DrawSentenceObject(_activeObject);

                // Append verb preposition
                var sentencePrep = ActiveVerbPrep();
                if (sentencePrep != 0)
                {
                    DrawPreposition((int)sentencePrep);

                    // Draw the 2nd active object
                    if (_activeObject2 != 0)
                        DrawSentenceObject(_activeObject2);
                }
            }

            FlushSentenceLine();
        }

        bool CheckPendingWalkAction()
        {
            // before a sentence script is executed, it might be necessary to walk to
            // and pickup objects before. Check if such an action is pending and handle
            // it if available.
            if (_walkToObjectState == WalkToObjectState.Done)
                return false;

            var actor = Variables[VariableEgo.Value];
            var a = Actors[actor];

            // wait until walking or turning action is finished
            if (a.Moving != MoveFlags.InLeg)
                return true;

            // after walking and turning finally execute the script
            if (_walkToObjectState == WalkToObjectState.Turn)
            {
                RunSentenceScript();
                // change actor facing
            }
            else
            {
                int distX, distY;
                Point p;
                if (IsActor(_walkToObject))
                {
                    var b = Actors[ObjToActor(_walkToObject)];
                    p = b.RealPosition;
                    if (p.X < a.RealPosition.X)
                        p.X += 4;
                    else
                        p.X -= 4;
                }
                else
                {
                    p = GetObjectXYPos(_walkToObject);
                }
                var abr = a.AdjustXYToBeInBox(p);
                distX = Math.Abs(a.RealPosition.X - abr.Position.X);
                distY = Math.Abs(a.RealPosition.Y - abr.Position.Y);

                if (distX <= 4 && distY <= 8)
                {
                    if (IsActor(_walkToObject))
                    { // walk to actor finished
                        // make actors turn to each other
                        a.FaceToObject(_walkToObject);
                        int otherActor = ObjToActor(_walkToObject);
                        // ignore the plant
                        if (otherActor != 19)
                        {
                            var b = Actors[otherActor];
                            b.FaceToObject(ActorToObj(actor));
                        }
                    }
                    else
                    { // walk to object finished
                        int dir;
                        Point pTmp;
                        GetObjectXYPos(_walkToObject, out pTmp, out dir);
                        a.TurnToDirection(dir);
                    }
                    _walkToObjectState = WalkToObjectState.Turn;
                    return true;
                }
            }

            _walkToObjectState = WalkToObjectState.Done;
            return false;
        }

        void RunSentenceScript()
        {
            _redrawSentenceLine = true;

            if (GetVerbEntrypointCore(_cmdObject, (int)_cmdVerb) != 0)
            {
                // do not read in the dark
                if (!(_cmdVerb == VerbsV0.Read && _currentLights == 0))
                {
                    Variables[VariableActiveObject2.Value] = OBJECT_V0_ID(_cmdObject2);
                    RunObjectScript(_cmdObject, (byte)_cmdVerb, false, false, new int[0]);
                    return;
                }
            }
            else
            {
                if (_cmdVerb == VerbsV0.Give)
                {
                    // no "give to"-script: give to other kid or ignore
                    int actor = OBJECT_V0_ID(_cmdObject2);
                    if (actor < 8)
                        SetOwnerOf(_cmdObject, actor);
                    return;
                }
            }

            if (_cmdVerb != VerbsV0.WalkTo)
            {
                // perform verb's fallback action
                Variables[VariableActiveVerb.Value] = (int)_cmdVerb;
                RunScript(3, false, false, new int[0]);
            }
        }

        void Nop()
        {
        }

        void ScreenPrepare()
        {
        }

        void PickupObject()
        {
            int obj = ReadByte();
            if (obj == 0)
                obj = _cmdObject;

            /* Don't take an object twice */
            if (GetWhereIsObject(obj) == WhereIsObject.Inventory)
                return;

            AddObjectToInventory(obj, _roomResource);
            MarkObjectRectAsDirty(obj);
            PutOwner(obj, (byte)Variables[VariableEgo.Value]);
            PutState(obj, GetStateCore(obj) | (byte)ObjectStateV2.State8 | (byte)ObjectStateV2.Untouchable);
            ClearDrawObjectQueue();

            RunInventoryScript(1);
        }

        bool IfEqualActiveObject2Common(bool checkType)
        {
            byte obj = ReadByte();
            if (!checkType || (OBJECT_V0_TYPE(_cmdObject2) == ObjectV0Type.Foreground))
                return (obj == OBJECT_V0_ID(_cmdObject2));
            return false;
        }

        void IfEqualActiveObject2()
        {
            bool equal = IfEqualActiveObject2Common((_opCode & 0x80) == 0);
            JumpRelative(equal);
        }

        void IfNotEqualActiveObject2()
        {
            bool equal = IfEqualActiveObject2Common((_opCode & 0x80) == 0);
            JumpRelative(!equal);
        }

        void EndCutscene()
        {
            cutScene.StackPointer = 0;

            Variables[VariableOverride.Value] = 0;
            cutScene.Data[0].Script = 0;
            cutScene.Data[0].Pointer = 0;

            SetMode((Engine0Mode)cutScene.Data[0].Data);

            if (_currentMode == Engine0Mode.Keypad)
            {
                StartScene((byte)cutScene.Data[2].Data);
                // in contrast to the normal keypad behavior we unfreeze scripts here
                UnfreezeScripts();
            }
            else
            {
                UnfreezeScripts();
                ActorFollowCamera(Variables[VariableEgo.Value]);
                // set mode again to have the freeze mode right
                SetMode((Engine0Mode)cutScene.Data[0].Data);
                _redrawSentenceLine = true;
            }
        }

        void LoadCostume()
        {
            int resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            ResourceManager.LoadCostume(resid);
        }

        void LoadRoomCore()
        {
            int resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            ResourceManager.LoadRoom(resid);
        }

        void LoadScript()
        {
            int resid = GetVarOrDirectByte(OpCodeParameter.Param1);
            ResourceManager.LoadScript(resid);
        }

        void LoadSound()
        {
            int resid = ReadByte();
            ResourceManager.LoadSound(Sound.MusicType, resid);
        }

        void Lights()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            // Convert older light mode values into
            // equivalent values.of later games
            // 0 Darkness
            // 1 Flashlight
            // 2 Lighted area
            if (a == 2)
                _currentLights = 11;
            else if (a == 1)
                _currentLights = 4;
            else
                _currentLights = 0;

            _fullRedraw = true;
        }

        void LockCostume()
        {
            int resid = ReadByte();
            // TODO: lock costume
            //            _res.lock(rtCostume, resid);
        }

        void UnlockCostume()
        {
            int resid = ReadByte();
            // TODO: unlock costume
            //            _res.unlock(rtCostume, resid);
        }

        void LockSound()
        {
            int resid = ReadByte();
            // TODO: lock sound
            //            _res.lock(rtSound, resid);
        }

        void UnlockSound()
        {
            int resid = ReadByte();
            // TODO: unlock sound
            //            _res.unlock(rtSound, resid);
        }

        void LockScript()
        {
            int resid = ReadByte();
            // TODO: lock script
            //            _res.lock(rtScript, resid);
        }

        void UnlockScript()
        {
            int resid = ReadByte();
            // TODO: unlock script
            //            _res.unlock(rtScript, resid);
        }

        void LockRoom()
        {
            int resid = ReadByte();
            // TODO: lock room
            //            _res.lock(rtRoom, resid);
        }

        void UnlockRoom()
        {
            int resid = ReadByte();
            // TODO: unlock room
            //            _res.unlock(rtRoom, resid);
        }

        void ResetSentence()
        {
            _activeVerb = VerbsV0.WalkTo;
            _activeObject = 0;
            _activeObject2 = 0;

            _walkToObjectState = WalkToObjectState.Done;
            _redrawSentenceLine = true;

            SentenceNum = 0;
            _sentenceNestedCount = 0;
        }

        void Cutscene()
        {
            cutScene.Data[0].Data = (byte)_currentMode;
            cutScene.Data[2].Data = CurrentRoom;

            FreezeScripts(0);
            SetMode(Engine0Mode.Cutscene);

            SentenceNum = 0;
            ResetSentence();

            cutScene.Data[0].Pointer = 0;
            cutScene.Data[0].Script = 0;
        }

        void SetMode()
        {
            SetMode((Engine0Mode)ReadByte());
        }

        void StopCurrentScript()
        {
            StopScriptCommon(0);
        }

        void SetMode(Engine0Mode mode)
        {
            UserStates state;

            _currentMode = mode;

            switch (_currentMode)
            {
                case Engine0Mode.Cutscene:
                    if (Game.Features.HasFlag(GameFeatures.Demo))
                    {
                        if (Variables[11] != 0)
                            _drawDemo = true;
                    }
                    _redrawSentenceLine = false;
                    // Note: do not change freeze state here
                    state = UserStates.SetIFace | UserStates.SetCursor;

                    break;
                case Engine0Mode.Keypad:
                    if (Game.Features.HasFlag(GameFeatures.Demo))
                    {
                        if (Game.Features.HasFlag(GameFeatures.Demo))
                            _drawDemo = true;
                    }
                    _redrawSentenceLine = false;
                    state = UserStates.SetIFace |
                    UserStates.SetCursor | UserStates.CursorOn |
                    UserStates.SetFreeze | UserStates.FreezeOn;
                    break;
                case Engine0Mode.Normal:
                case Engine0Mode.NoNewKid:
                    if (Game.Features.HasFlag(GameFeatures.Demo))
                    {
                        ResetVerbs();
                        _activeVerb = VerbsV0.WalkTo;
                        _redrawSentenceLine = true;
                        _drawDemo = false;
                    }
                    state = UserStates.SetIFace | UserStates.IFaceAll |
                    UserStates.SetCursor | UserStates.CursorOn |
                    UserStates.SetFreeze;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Invalid mode: {0}", mode));
            }

            SetUserState(state);
        }

        void DoSentence()
        {
            byte verb = ReadByte();
            int obj, obj2;
            byte b;

            b = ReadByte();
            if (b == 0xFF)
            {
                obj = _cmdObject2;
            }
            else if (b == 0xFE)
            {
                obj = _cmdObject;
            }
            else
            {
                obj = OBJECT_V0(b, (_opCode & 0x80) != 0 ? ObjectV0Type.Background : ObjectV0Type.Foreground);
            }

            b = ReadByte();
            if (b == 0xFF)
            {
                obj2 = _cmdObject2;
            }
            else if (b == 0xFE)
            {
                obj2 = _cmdObject;
            }
            else
            {
                obj2 = OBJECT_V0(b, (_opCode & 0x40) != 0 ? ObjectV0Type.Background : ObjectV0Type.Foreground);
            }

            DoSentence(verb, (ushort)obj, (ushort)obj2);
        }

        static int OBJECT_V0_ID(int obj)
        {
            return obj & 0xFF;
        }

        static int OBJECT_V0(int id, ObjectV0Type type)
        {
            Debug.Assert(id < 256);
            return ((int)type << 8 | id);
        }

    }
}

