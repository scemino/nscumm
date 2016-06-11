//
//  Logic.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    public enum JoeWalkMode
    {
        NORMAL = 0,
        MOVE = 1,
        EXECUTE = 2,
        SPEAK = 3
    }

    public enum RoomDisplayMode
    {
        RDM_FADE_NOJOE = 0,
        // fade in, hide Joe
        RDM_FADE_JOE = 1,
        // fade in, display Joe
        RDM_NOFADE_JOE = 2,
        // screen does not dissolve into view
        RDM_FADE_JOE_XY = 3
        // display Joe at the current X, Y coords
    }

    class Joe
    {
        public ushort x, y;
        public Direction facing, cutFacing, prevFacing;
        public JoeWalkMode walk;
        public ushort scale;
    }

    static class Jso
    {
        public const int JSO_OBJECT_DESCRIPTION = 0;
        public const int JSO_OBJECT_NAME = 1;
        public const int JSO_ROOM_NAME = 2;
        public const int JSO_VERB_NAME = 3;
        public const int JSO_JOE_RESPONSE = 4;
        public const int JSO_ACTOR_ANIM = 5;
        public const int JSO_ACTOR_NAME = 6;
        public const int JSO_ACTOR_FILE = 7;
        public const int JSO_COUNT = 8;
    }

    public abstract class Logic
    {
        const int JOE_RESPONSE_MAX = 40;
        const int GAME_STATE_COUNT = 211;

        protected QueenEngine _vm;
        Joe _joe;
        int _puzzleAttemptCount;
        protected Journal _journal;
        ushort _numRooms;
        ushort _numNames;
        ushort _numObjects;
        ushort _numDescriptions;
        ushort[] _roomData;
        ushort[] _sfxName;
        ushort _numItems;
        ItemData[] _itemData;
        ushort _numGraphics;
        GraphicData[] _graphicData;
        ushort _numWalkOffs;
        WalkOffData[] _walkOffData;
        ushort _numObjDesc;
        ObjectDescription[] _objectDescription;
        ushort _numFurniture;
        FurnitureData[] _furnitureData;
        ushort _numActors;
        ushort _numAAnim;
        ushort _numAName;
        ushort _numAFile;
        ActorData[] _actorData; 
        ushort _numGraphicAnim;
        GraphicAnim[] _graphicAnim;
        System.Collections.Generic.List<string> _jasStringList;
        int[] _jasStringOffset;

        Item[] _inventoryItem = new Item[4];

        Credits _credits;

        /// <summary>
        /// Cutscene counter.
        /// </summary>
        int _scene;

        protected Action[] _specialMoves;

        public ObjectData[] ObjectData { get; private set; }

        public ItemData[] ItemData { get { return _itemData; } }

        public ushort EntryObj { get; set; }

        public short[] GameState { get; private set; }

        public ushort CurrentRoomSfx { get { return _sfxName[CurrentRoom]; } }

        public ushort NewRoom
        {
            get;
            set;
        }

        public ushort OldRoom
        {
            get;
            set;
        }

        public ushort CurrentRoom
        {
            get;
            set;
        }

        public ushort[] RoomData { get { return _roomData; } }

        public ushort CurrentRoomData { get { return _roomData[CurrentRoom]; } }

        public GraphicAnim[] GraphicAnim { get { return _graphicAnim; } }

        public ObjectDescription[] ObjectDescription { get { return _objectDescription; } }

        public int ObjectDescriptionCount { get { return _numObjDesc; } }

        public ushort GraphicAnimCount { get { return _numGraphicAnim; } }

        public ushort JoeX { get { return _joe.x; } }

        public ushort JoeY { get { return _joe.y; } }

        public Direction JoePrevFacing
        {
            get { return _joe.prevFacing; }
            set { _joe.prevFacing = value; }
        }

        public Direction JoeFacing
        {
            get { return _joe.facing; }
            set { _joe.facing = value; }
        }

        public Direction JoeCutFacing
        {
            get { return _joe.cutFacing; }
            set { _joe.cutFacing = value; }
        }

        public void JoePos(ushort x, ushort y)
        {
            _joe.x = x;
            _joe.y = y;
        }

        public ushort JoeScale
        {
            get { return _joe.scale; }
            set { _joe.scale = value; }
        }

        public GraphicData[] GraphicData
        {
            get { return _graphicData; }
        }

        public ushort NumItemsInventory
        {
            get
            {
                ushort count = 0;
                for (int i = 1; i < _numItems; i++)
                    if (_itemData[i].name > 0)
                        count++;

                return count;
            }
        }

        protected Logic(QueenEngine vm)
        {
            _vm = vm;
            _joe = new Joe();
            _joe.scale = 100;
            _joe.walk = JoeWalkMode.NORMAL;
            GameState = new short[GAME_STATE_COUNT];
            _puzzleAttemptCount = 0;
            _journal = new Journal(vm);
            _specialMoves = new Action[40];
            ReadQueenJas();
        }

        public string VerbName(Verb v)
        {
            if (v == 0)
            {
                return string.Empty;
            }
            return _jasStringList[_jasStringOffset[Jso.JSO_VERB_NAME] + (int)v - 1];
        }

        public string ObjectName(ushort objNum)
        {
            //assert(objNum >= 1 && objNum <= _numNames);
            return _jasStringList[_jasStringOffset[Jso.JSO_OBJECT_NAME] + objNum - 1];
        }

        public ushort FindInventoryItem(int invSlot)
        {
            // queen.c l.3894-3898
            if (invSlot >= 0 && invSlot < 4)
            {
                return (ushort)_inventoryItem[invSlot];
            }
            return 0;
        }

        public void JoeGrab(StateGrab grabState)
        {
            ushort frame = 0;
            BobSlot bobJoe = _vm.Graphics.Bobs[0];

            switch (grabState)
            {
                case StateGrab.NONE:
                    break;
                case StateGrab.MID:
                    if (JoeFacing == Direction.BACK)
                    {
                        frame = 6;
                    }
                    else if (JoeFacing == Direction.FRONT)
                    {
                        frame = 4;
                    }
                    else
                    {
                        frame = 2;
                    }
                    break;
                case StateGrab.DOWN:
                    if (JoeFacing == Direction.BACK)
                    {
                        frame = 9;
                    }
                    else
                    {
                        frame = 8;
                    }
                    break;
                case StateGrab.UP:
                    // turn back
                    _vm.BankMan.Unpack(5, 31, 7);
                    bobJoe.xflip = (JoeFacing == Direction.LEFT);
                    bobJoe.scale = JoeScale;
                    _vm.Update();
                    // grab up
                    _vm.BankMan.Unpack(7, 31, 7);
                    bobJoe.xflip = (JoeFacing == Direction.LEFT);
                    bobJoe.scale = JoeScale;
                    _vm.Update();
                    // turn back
                    frame = 7;
                    break;
            }

            if (frame != 0)
            {
                _vm.BankMan.Unpack(frame, 31, 7);
                bobJoe.xflip = (JoeFacing == Direction.LEFT);
                bobJoe.scale = JoeScale;
                _vm.Update();

                // extra delay for grab down
                if (grabState == StateGrab.DOWN)
                {
                    _vm.Update();
                    _vm.Update();
                }
            }
        }

        public JoeWalkMode JoeWalk
        {
            set
            {
                _joe.walk = value;
                // Do this so that Input doesn't need to know the walk value
                _vm.Input.DialogueRunning(JoeWalkMode.SPEAK == value);
            }
            get { return _joe.walk; }
        }

        /// <summary>
        /// handle the pinnacle room (== room chooser in the jungle).
        /// </summary>
        /// <returns>The pinnacle room.</returns>
        public void HandlePinnacleRoom()
        {
            throw new NotImplementedException();
        }

        public void InventoryScroll(ushort count, bool up)
        {
            if (!(NumItemsInventory > 4))
                return;
            while ((count--) != 0)
            {
                if (up)
                {
                    for (int i = 3; i > 0; i--)
                        _inventoryItem[i] = _inventoryItem[i - 1];
                    _inventoryItem[0] = (NScumm.Queen.Item)PreviousInventoryItem((short)_inventoryItem[0]);
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                        _inventoryItem[i] = _inventoryItem[i + 1];
                    _inventoryItem[3] = NextInventoryItem(_inventoryItem[3]);
                }
            }

            InventoryRefresh();
        }

        public void StartDialogue(string dlgFile, int personInRoom, out string cutaway)
        {
            cutaway = null;
            ObjectData data = ObjectData[_roomData[CurrentRoom] + personInRoom];
            if (data.name > 0 && data.entryObj <= 0)
            {
                if (State.FindTalk(data.state) == StateTalk.MUTE)
                {
                    // 'I can't talk to that'
                    MakeJoeSpeak((ushort)(24 + _vm.Randomizer.Next(1 + 2)));
                }
                else
                {
                    _vm.Display.Fullscreen = true;
                    Talk.DoTalk(dlgFile, personInRoom, out cutaway, _vm);
                    if (cutaway.Length == 0)
                    {
                        _vm.Display.Fullscreen = false;
                    }
                }
            }
        }

        public void MakeJoeSpeak(ushort descNum, bool objectType = false)
        {
            string text = objectType ? ObjectTextualDescription(descNum) : JoeResponse(descNum);
            if (objectType)
            {
                descNum += JOE_RESPONSE_MAX;
            }
            string descFilePrefix = $"JOE{descNum:D04}";
            MakePersonSpeak(text, null, descFilePrefix);
        }

        public string ObjectTextualDescription(ushort objNum)
        {
            return _jasStringList[_jasStringOffset[Jso.JSO_OBJECT_DESCRIPTION] + objNum - 1];
        }

        private string JoeResponse(int i)
        {
            return _jasStringList[_jasStringOffset[Jso.JSO_JOE_RESPONSE] + i - 1];
        }

        public void StopCredits()
        {
            if (_credits != null)
            {
                _vm.Display.ClearTexts(0, 199);
                _credits = null;
            }
        }

        public void MakePersonSpeak(string sentence, Person person, string voiceFilePrefix)
        {
            _vm.Command.Clear(false);
            Talk.Speak(sentence, person, voiceFilePrefix, _vm);
        }

        public int FindFrame(short obj)
        {
            ushort framenum = 0;
            ushort room = ObjectData[obj].room;
            short img = ObjectData[obj].image;
            if (img == -3 || img == -4)
            {
                ushort bobnum = FindPersonNumber((ushort)obj, room);
                if (bobnum <= 3)
                {
                    framenum = (ushort)(31 + bobnum);
                }
            }
            else
            {
                ushort idx = 0;
                for (ushort i = (ushort)(_roomData[room] + 1); i < obj; ++i)
                {
                    img = ObjectData[i].image;
                    if (img <= -10)
                    {
                        GraphicData pgd = _graphicData[-(img + 10)];
                        if (pgd.lastFrame != 0)
                        {
                            // skip all the frames of the animation
                            idx = (ushort)(idx + Math.Abs(pgd.lastFrame) - pgd.firstFrame + 1);
                        }
                        else
                        {
                            // static bob, skip one frame
                            ++idx;
                        }
                    }
                    else if (img == -1)
                    {
                        ++idx;
                    }
                    else if (img > 0)
                    {
                        if (img > 5000)
                        {
                            img -= 5000;
                        }
                        GraphicData pgd = _graphicData[img];
                        ushort lastFrame = (ushort)Math.Abs(pgd.lastFrame);
                        if (pgd.firstFrame < 0)
                        {
                            idx += lastFrame;
                        }
                        else if (lastFrame != 0)
                        {
                            idx = (ushort)(idx + (lastFrame - pgd.firstFrame) + 1);
                        }
                        else
                        {
                            ++idx;
                        }
                    }
                }

                img = ObjectData[obj].image;
                if (img <= -10)
                {
                    GraphicData pgd = _graphicData[-(img + 10)];
                    if (pgd.lastFrame != 0)
                    {
                        idx = (ushort)(idx + Math.Abs(pgd.lastFrame) - pgd.firstFrame + 1);
                    }
                    else
                    {
                        ++idx;
                    }
                }
                else if (img == -1 || img > 0)
                {
                    ++idx;
                }

                // calculate only if there are person frames
                if (idx > 0)
                {
                    framenum = (ushort)(Defines.FRAMES_JOE + _vm.Graphics.NumFurnitureFrames + idx);
                }
            }
            return framenum;
        }

        public void HandleSpecialArea(Direction facing, ushort areaNum, ushort walkDataNum)
        {
            // queen.c l.2838-2911
            D.Debug(9, $"handleSpecialArea({facing}, {areaNum}, {walkDataNum})");

            // Stop animating Joe
            _vm.Graphics.Bobs[0].animating = false;

            // Make Joe face the right direction
            JoeFacing = facing;
            JoeFace();

            NewRoom = 0;
            EntryObj = 0;

            string nextCut = string.Empty;

            switch (CurrentRoom)
            {
                case Defines.ROOM_JUNGLE_BRIDGE:
                    MakeJoeSpeak(16);
                    break;
                case Defines.ROOM_JUNGLE_GORILLA_1:
                    PlayCutaway("C6C.CUT", out nextCut);
                    break;
                case Defines.ROOM_JUNGLE_GORILLA_2:
                    PlayCutaway("C14B.CUT", out nextCut);
                    break;
                case Defines.ROOM_AMAZON_ENTRANCE:
                    if (areaNum == 3)
                    {
                        PlayCutaway("C16A.CUT", out nextCut);
                    }
                    break;
                case Defines.ROOM_AMAZON_HIDEOUT:
                    if (walkDataNum == 4)
                    {
                        PlayCutaway("C17A.CUT", out nextCut);
                    }
                    else if (walkDataNum == 2)
                    {
                        PlayCutaway("C17B.CUT", out nextCut);
                    }
                    break;
                case Defines.ROOM_FLODA_OUTSIDE:
                    PlayCutaway("C22A.CUT", out nextCut);
                    break;
                case Defines.ROOM_FLODA_KITCHEN:
                    PlayCutaway("C26B.CUT", out nextCut);
                    break;
                case Defines.ROOM_FLODA_KLUNK:
                    PlayCutaway("C30A.CUT", out nextCut);
                    break;
                case Defines.ROOM_FLODA_HENRY:
                    PlayCutaway("C32C.CUT", out nextCut);
                    break;
                case Defines.ROOM_TEMPLE_ZOMBIES:
                    if (areaNum == 6)
                    {
                        switch (GameState[Defines.VAR_BYPASS_ZOMBIES])
                        {
                            case 0:
                                PlayCutaway("C50D.CUT", out nextCut);
                                while (nextCut[0] != '\0')
                                {
                                    PlayCutaway(nextCut, out nextCut);
                                }
                                GameState[Defines.VAR_BYPASS_ZOMBIES] = 1;
                                break;
                            case 1:
                                PlayCutaway("C50H.CUT", out nextCut);
                                break;
                        }
                    }
                    break;
                case Defines.ROOM_TEMPLE_SNAKE:
                    PlayCutaway("C53B.CUT", out nextCut);
                    break;
                case Defines.ROOM_TEMPLE_LIZARD_LASER:
                    MakeJoeSpeak(19);
                    break;
                case Defines.ROOM_HOTEL_DOWNSTAIRS:
                    MakeJoeSpeak(21);
                    break;
                case Defines.ROOM_HOTEL_LOBBY:
                    switch (GameState[Defines.VAR_HOTEL_ESCAPE_STATE])
                    {
                        case 0:
                            PlayCutaway("C73A.CUT");
                            JoeUseUnderwear();
                            JoeFace();
                            GameState[Defines.VAR_HOTEL_ESCAPE_STATE] = 1;
                            break;
                        case 1:
                            PlayCutaway("C73B.CUT");
                            GameState[Defines.VAR_HOTEL_ESCAPE_STATE] = 2;
                            break;
                        case 2:
                            PlayCutaway("C73C.CUT");
                            break;
                    }
                    break;
                case Defines.ROOM_TEMPLE_MAZE_5:
                    if (areaNum == 7)
                    {
                        MakeJoeSpeak(17);
                    }
                    break;
                case Defines.ROOM_TEMPLE_MAZE_6:
                    if (areaNum == 5 && GameState[187] == 0)
                    {
                        PlayCutaway("C101B.CUT", out nextCut);
                    }
                    break;
                case Defines.ROOM_FLODA_FRONTDESK:
                    if (areaNum == 3)
                    {
                        switch (GameState[Defines.VAR_BYPASS_FLODA_RECEPTIONIST])
                        {
                            case 0:
                                PlayCutaway("C103B.CUT", out nextCut);
                                GameState[Defines.VAR_BYPASS_FLODA_RECEPTIONIST] = 1;
                                break;
                            case 1:
                                PlayCutaway("C103E.CUT", out nextCut);
                                break;
                        }
                    }
                    break;
            }

            while (nextCut.Length > 4 &&
                   string.Compare(nextCut, nextCut.Length - 4, ".CUT", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
            {
                PlayCutaway(nextCut, out nextCut);
            }
        }

        public abstract void UseJournal();

        public void StartCredits(string filename)
        {
            StopCredits();
            _credits = new Credits(_vm, filename);
        }

        public ushort FindBob(short obj)
        {
            // TODO: assert(obj <= _numObjects);

            ushort room = ObjectData[obj].room;
            //TODO: assert(room <= _numRooms);

            ushort bobnum = 0;
            short img = ObjectData[obj].image;
            if (img != 0)
            {
                if (img == -3 || img == -4)
                {
                    // a person object
                    bobnum = FindPersonNumber((ushort)obj, room);
                }
                else
                {
                    ushort bobtype = 0; // 1 for animated, 0 for static

                    if (img <= -10)
                    {
                        // object has been turned off, but the image order hasn't been updated
                        if (_graphicData[-(img + 10)].lastFrame != 0)
                        {
                            bobtype = 1;
                        }
                    }
                    else if (img == -2)
                    {
                        // -1 static, -2 animated
                        bobtype = 1;
                    }
                    else if (img > 0)
                    {
                        if (_graphicData[img].lastFrame != 0)
                        {
                            bobtype = 1;
                        }
                    }

                    ushort idxAnimated = 0;
                    ushort idxStatic = 0;
                    for (ushort i = (ushort)(_roomData[room] + 1); i <= obj; ++i)
                    {
                        img = ObjectData[i].image;
                        if (img <= -10)
                        {
                            if (_graphicData[-(img + 10)].lastFrame != 0)
                            {
                                ++idxAnimated;
                            }
                            else
                            {
                                ++idxStatic;
                            }
                        }
                        else if (img > 0)
                        {
                            if (img > 5000)
                            {
                                img -= 5000;
                            }

                            // TODO: assert(img <= _numGraphics);

                            if (_graphicData[img].lastFrame != 0)
                            {
                                ++idxAnimated;
                            }
                            else
                            {
                                ++idxStatic;
                            }
                        }
                        else if (img == -1)
                        {
                            ++idxStatic;
                        }
                        else if (img == -2)
                        {
                            ++idxAnimated;
                        }
                    }
                    if (bobtype == 0)
                    {
                        // static bob
                        if (idxStatic > 0)
                        {
                            bobnum = (ushort)(19 + _vm.Graphics.NumStaticFurniture + idxStatic);
                        }
                    }
                    else
                    {
                        // animated bob
                        if (idxAnimated > 0)
                        {
                            bobnum = (ushort)(4 + _vm.Graphics.NumAnimatedFurniture + idxAnimated);
                        }
                    }
                }
            }
            return bobnum;
        }

        public ushort ObjectForPerson(ushort bobNum)
        {
            ushort bobcur = 0;
            // first object number in the room
            ushort cur = (ushort)(CurrentRoomData + 1);
            // last object number in the room
            ushort last = _roomData[CurrentRoom + 1];
            for (; cur <= last; ++cur)
            {
                short image = ObjectData[cur].image;
                if (image == -3 || image == -4)
                {
                    // the object is a bob
                    ++bobcur;
                }
                if (bobcur == bobNum)
                {
                    return cur;
                }
            }
            return 0;
        }

        public void ObjectCopy(short dummyObjectIndex, short realObjectIndex)
        {
            // copy data from dummy object to real object, if COPY_FROM object
            // images are greater than COPY_TO Object images then swap the objects around.

            ObjectData dummyObject = ObjectData[dummyObjectIndex];
            ObjectData realObject = ObjectData[realObjectIndex];

            int fromState = (dummyObject.name < 0) ? -1 : 0;

            int frameCountReal = 1;
            int frameCountDummy = 1;

            int graphic = realObject.image;
            if (graphic > 0)
            {
                if (graphic > 5000)
                    graphic -= 5000;

                GraphicData data = GraphicData[graphic];

                if (data.lastFrame > 0)
                    frameCountReal = data.lastFrame - data.firstFrame + 1;

                graphic = dummyObject.image;
                if (graphic > 0)
                {
                    if (graphic > 5000)
                        graphic -= 5000;

                    data = GraphicData[graphic];

                    if (data.lastFrame > 0)
                        frameCountDummy = data.lastFrame - data.firstFrame + 1;
                }
            }

            ObjectData temp = realObject;
            realObject = dummyObject;

            if (frameCountDummy > frameCountReal)
                dummyObject = temp;

            realObject.name = Math.Abs(realObject.name);

            if (fromState == -1)
                dummyObject.name = (short)-Math.Abs(dummyObject.name);

            for (int i = 1; i <= _numWalkOffs; i++)
            {
                WalkOffData walkOff = _walkOffData[i];
                if (walkOff.entryObj == (short)dummyObjectIndex)
                {
                    walkOff.entryObj = (short)realObjectIndex;
                    break;
                }
            }

        }

        public void RemoveHotelItemsFromInventory()
        {
            if (CurrentRoom == 1 && GameState[Defines.VAR_HOTEL_ITEMS_REMOVED] == 0)
            {
                InventoryDeleteItem(Item.ITEM_CROWBAR, false);
                InventoryDeleteItem(Item.ITEM_DRESS, false);
                InventoryDeleteItem(Item.ITEM_CLOTHES, false);
                InventoryDeleteItem(Item.ITEM_HAY, false);
                InventoryDeleteItem(Item.ITEM_OIL, false);
                InventoryDeleteItem(Item.ITEM_CHICKEN, false);
                GameState[Defines.VAR_HOTEL_ITEMS_REMOVED] = 1;
                InventoryRefresh();
            }
        }

        public void InventoryDeleteItem(Item itemNum, bool refresh = true)
        {
            Item item = itemNum;
            _itemData[(int)itemNum].name = (short)-Math.Abs(_itemData[(int)itemNum].name);    //set invisible
            for (int i = 0; i < 4; i++)
            {
                item = NextInventoryItem(item);
                _inventoryItem[i] = item;
                RemoveDuplicateItems();
            }

            if (refresh)
                InventoryRefresh();
        }

        private void RemoveDuplicateItems()
        {
            for (int i = 0; i < 4; i++)
                for (int j = i + 1; j < 4; j++)
                    if (_inventoryItem[i] == _inventoryItem[j])
                        _inventoryItem[j] = Item.ITEM_NONE;
        }

        private Item NextInventoryItem(Item first)
        {
            short i;
            for (i = (short)(first + 1); i < _numItems; i++)
                if (_itemData[i].name > 0)
                    return (Item)i;
            for (i = 1; i < (int)first; i++)
                if (_itemData[i].name > 0)
                    return (Item)i;

            return 0;   //nothing found
        }

        public void SceneStop()
        {
            D.Debug(6, $"[Logic::sceneStop] _scene = {_scene}");
            _scene--;

            if (_scene > 0)
                return;

            _vm.Display.PalSetAllDirty();
            _vm.Display.ShowMouseCursor(true);
            _vm.Grid.SetupPanel();
        }

        public void ExecuteSpecialMove(short sm)
        {
            D.Debug(6, $"Special move: {sm}");
            if (sm < _specialMoves.Length && _specialMoves[sm] != null)
            {
                _specialMoves[sm]();
            }
        }

        public void SceneStart()
        {
            D.Debug(6, $"[Logic::sceneStart] _scene = {_scene}");
            _scene++;

            _vm.Display.ShowMouseCursor(false);

            if (1 == _scene)
            {
                _vm.Display.PalGreyPanel();
            }

            _vm.Update();
        }

        public bool InitPerson(ushort noun, string name, bool loadBank, out Person pp)
        {
            pp = null;
            ActorData pad = FindActor(noun, name);
            if (pad != null)
            {
                pp = new Person();
                pp.actor = pad;
                pp.name = ActorName(pad.name);
                if (pad.anim != 0)
                {
                    pp.anim = ActorAnim(pad.anim);
                }
                else
                {
                    pp.anim = null;
                }
                if (loadBank && pad.file != 0)
                {
                    _vm.BankMan.Load(ActorFile(pad.file), pad.bankNum);
                    // if there is no valid actor file (ie pad.file is 0), the person
                    // data is already loaded as it is included in objects room bank (.bbk)
                }
                pp.bobFrame = (ushort)(31 + pp.actor.bobNum);
            }
            return pad != null;
        }

        public ushort JoeFace()
        {
            D.Debug(9, $"Logic::joeFace() - curFace = {_joe.facing}, prevFace = {_joe.prevFacing}");
            BobSlot pbs = _vm.Graphics.Bobs[0];
            ushort frame;
            if (CurrentRoom == 108)
            {
                frame = 1;
            }
            else
            {
                frame = 35;
                if (JoeFacing == Direction.FRONT)
                {
                    if (JoePrevFacing == Direction.BACK)
                    {
                        pbs.frameNum = 35;
                        _vm.Update();
                    }
                    frame = 36;
                }
                else if (JoeFacing == Direction.BACK)
                {
                    if (JoePrevFacing == Direction.FRONT)
                    {
                        pbs.frameNum = 35;
                        _vm.Update();
                    }
                    frame = 37;
                }
                else if ((JoeFacing == Direction.LEFT && JoePrevFacing == Direction.RIGHT)
                         || (JoeFacing == Direction.RIGHT && JoePrevFacing == Direction.LEFT))
                {
                    pbs.frameNum = 36;
                    _vm.Update();
                }
                pbs.frameNum = frame;
                pbs.scale = JoeScale;
                pbs.xflip = (JoeFacing == Direction.LEFT);
                _vm.Update();
                JoePrevFacing = JoeFacing;
                switch (frame)
                {
                    case 35:
                        frame = 1;
                        break;
                    case 36:
                        frame = 3;
                        break;
                    case 37:
                        frame = 5;
                        break;
                }
            }
            pbs.frameNum = 31;
            _vm.BankMan.Unpack(frame, pbs.frameNum, 7);
            return frame;
        }

        public void PlayCutaway(string cutFile)
        {
            string next;
            PlayCutaway(cutFile, out next);
        }

        public void PlayCutaway(string cutFile, out string next)
        {
            _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
            Cutaway.Run(cutFile, out next, _vm);
        }

        private string ActorFile(ushort num)
        {
            return _jasStringList[_jasStringOffset[Jso.JSO_ACTOR_FILE] + num - 1];
        }

        private string ActorName(int num)
        {
            return _jasStringList[_jasStringOffset[Jso.JSO_ACTOR_NAME] + num - 1];
        }

        private string ActorAnim(int num)
        {
            return _jasStringList[_jasStringOffset[Jso.JSO_ACTOR_ANIM] + num - 1];
        }

        private ActorData FindActor(ushort noun, string name)
        {
            ushort obj = (ushort)(CurrentRoomData + noun);
            short img = ObjectData[obj].image;
            if (img != -3 && img != -4)
            {
                D.Warning($"Logic::findActor() - Object {obj} is not a person");
                return null;
            }

            // search Bob number for the person
            ushort bobNum = FindPersonNumber(obj, CurrentRoom);

            // search for a matching actor
            if (bobNum > 0)
            {
                for (ushort i = 1; i <= _numActors; ++i)
                {
                    var pad = _actorData[i];
                    if (pad.room == CurrentRoom && GameState[pad.gsSlot] == pad.gsValue)
                    {
                        if (bobNum == pad.bobNum || (name != null && string.Equals(ActorName(pad.name), name)))
                        {
                            return pad;
                        }
                    }
                }
            }
            return null;
        }

        public ushort FindPersonNumber(ushort obj, ushort room)
        {
            ushort num = 0;
            for (ushort i = (ushort)(_roomData[room] + 1); i <= obj; ++i)
            {
                short img = ObjectData[i].image;
                if (img == -3 || img == -4)
                {
                    ++num;
                }
            }
            return num;
        }

        public void SceneReset()
        {
            _scene = 0;
        }

        public void ChangeRoom()
        {
            if (!ChangeToSpecialRoom())
                DisplayRoom(CurrentRoom, RoomDisplayMode.RDM_FADE_JOE, 100, 1, false);
            _vm.Display.ShowMouseCursor(true);
        }

        public void Update()
        {
            if (_credits != null)
                _credits.Update();

            // TODO: debugger
            //              if (_vm.debugger().flags() & Debugger::DF_DRAW_AREAS) {
            //                  _vm.grid().drawZones();
            //              }
        }

        public void Start()
        {
            SetupSpecialMoveTable();
            _vm.Command.Clear(false);
            _vm.Display.SetupPanel();
            _vm.Graphics.UnpackControlBank();
            _vm.Graphics.SetupMouseCursor();
            SetupJoe();
            _vm.Grid.SetupPanel();
            InventorySetup();

            OldRoom = 0;
            NewRoom = CurrentRoom;
        }

        protected void InventoryRefresh()
        {
            ushort x = 182;
            for (int i = 0; i < 4; ++i)
            {
                ushort itemNum = (ushort)_inventoryItem[i];
                if (itemNum != 0)
                {
                    ushort dstFrame = (ushort)((i == 0) ? 8 : 9);
                    // unpack frame for object and draw it
                    _vm.BankMan.Unpack(_itemData[itemNum].frame, dstFrame, 14);
                    _vm.Graphics.DrawInventoryItem(dstFrame, x, 14);
                }
                else
                {
                    // no object, clear the panel
                    _vm.Graphics.DrawInventoryItem(0, x, 14);
                }
                x += 35;
            }
        }

        public void DisplayRoom(ushort room, RoomDisplayMode mode, ushort scale, int comPanel, bool inCutaway)
        {
            D.Debug(6, $"Logic::displayRoom({room}, {mode}, {scale}, {comPanel}, {inCutaway})");

            EraseRoom();

            if (_credits != null)
                _credits.NextRoom();

            SetupRoom(RoomName(room), comPanel, inCutaway);
            if (mode != RoomDisplayMode.RDM_FADE_NOJOE)
            {
                SetupJoeInRoom(mode != RoomDisplayMode.RDM_FADE_JOE_XY, scale);
            }
            if (mode != RoomDisplayMode.RDM_NOFADE_JOE)
            {
                _vm.Update();
                var joe = _vm.Graphics.Bobs[0];
                _vm.Display.PalFadeIn(CurrentRoom, joe.active, joe.x, joe.y);
            }
            if (mode != RoomDisplayMode.RDM_FADE_NOJOE && JoeX != 0 && JoeY != 0)
            {
                ushort jx = JoeX;
                ushort jy = JoeY;
                JoePos(0, 0);
                _vm.Walk.MoveJoe(0, (short)jx, (short)jy, inCutaway);
            }
        }

        protected abstract bool ChangeToSpecialRoom();

        private void SetupJoeInRoom(bool autoPosition, ushort scale)
        {
            D.Debug(9, $"Logic::setupJoeInRoom({autoPosition}, {scale}) joe.x={_joe.x} joe.y={_joe.y}");

            short oldx, oldy;
            if (!autoPosition || JoeX != 0 || JoeY != 0)
            {
                oldx = (short)JoeX;
                oldy = (short)JoeY;
                JoePos(0, 0);
            }
            else
            {
                ObjectData pod = ObjectData[EntryObj];
                // find the walk off point for the entry object and make
                // Joe walking to that point
                WalkOffData pwo = WalkOffPointForObject(EntryObj);
                if (pwo != null)
                {
                    oldx = (short)pwo.x;
                    oldy = (short)pwo.y;
                    // entryObj has a walk off point, then walk from there to object x,y
                    JoePos(pod.x, pod.y);
                }
                else
                {
                    // no walk off point, use object position
                    oldx = (short)pod.x;
                    oldy = (short)pod.y;
                    JoePos(0, 0);
                }
            }

            D.Debug(6, $"Logic::setupJoeInRoom() - oldx={oldx}, oldy={oldy} scale={scale}");

            if (scale > 0 && scale < 100)
            {
                JoeScale = scale;
            }
            else
            {
                ushort a = _vm.Grid.FindAreaForPos(GridScreen.ROOM, (ushort)oldx, (ushort)oldy);
                if (a > 0)
                {
                    JoeScale = (_vm.Grid.Areas[CurrentRoom][a].CalcScale(oldy));
                }
                else
                {
                    JoeScale = 100;
                }
            }

            if (JoeCutFacing > 0)
            {
                JoeFacing = JoeCutFacing;
                JoeCutFacing = 0;
            }
            else
            {
                // check to see which way Joe entered room
                ObjectData pod = ObjectData[EntryObj];
                switch (State.FindDirection(pod.state))
                {
                    case Direction.BACK:
                        JoeFacing = Direction.FRONT;
                        break;
                    case Direction.FRONT:
                        JoeFacing = Direction.BACK;
                        break;
                    case Direction.LEFT:
                        JoeFacing = Direction.RIGHT;
                        break;
                    case Direction.RIGHT:
                        JoeFacing = Direction.LEFT;
                        break;
                }
            }
            JoePrevFacing = JoeFacing;

            BobSlot pbs = _vm.Graphics.Bobs[0];
            pbs.scale = JoeScale;

            if (CurrentRoom == 108)
            {
                _vm.Graphics.PutCameraOnBob(-1);
                _vm.BankMan.Load("JOE_E.ACT", 7);
                _vm.BankMan.Unpack(2, 31, 7);

                _vm.Display.HorizontalScroll = 320;

                JoeFacing = Direction.RIGHT;
                JoeCutFacing = Direction.RIGHT;
                JoePrevFacing = Direction.RIGHT;
            }

            JoeFace();
            pbs.CurPos(oldx, oldy);
            pbs.frameNum = 31;
        }

        public WalkOffData WalkOffPointForObject(ushort obj)
        {
            for (ushort i = 1; i <= _numWalkOffs; ++i)
            {
                if (_walkOffData[i].entryObj == obj)
                {
                    return _walkOffData[i];
                }
            }
            return null;
        }

        private string RoomName(ushort roomNum)
        {
            //assert(roomNum >= 1 && roomNum <= _numRooms);
            return _jasStringList[_jasStringOffset[Jso.JSO_ROOM_NAME] + roomNum - 1];
        }

        private void SetupRoom(string room, int comPanel, bool inCutaway)
        {
            // load backdrop image, init dynalum, setup colors
            _vm.Display.SetupNewRoom(room, CurrentRoom);

            // setup graphics to enter fullscreen/panel mode
            _vm.Display.ScreenMode(comPanel, inCutaway);

            _vm.Grid.SetupNewRoom(CurrentRoom, _roomData[CurrentRoom]);

            short[] furn = new short[9];
            ushort furnTot = 0;
            for (ushort i = 1; i <= _numFurniture; ++i)
            {
                if (_furnitureData[i].room == CurrentRoom)
                {
                    ++furnTot;
                    furn[furnTot] = _furnitureData[i].objNum;
                }
            }
            _vm.Graphics.SetupNewRoom(room, CurrentRoom, furn, furnTot);

            _vm.Display.ForceFullRefresh();
        }

        private void EraseRoom()
        {
            _vm.BankMan.EraseFrames(false);
            _vm.BankMan.Close(15);
            _vm.BankMan.Close(11);
            _vm.BankMan.Close(10);
            _vm.BankMan.Close(12);

            _vm.Display.PalFadeOut(CurrentRoom);

            // invalidates all persons animations
            _vm.Graphics.ClearPersonFrames();
            _vm.Graphics.EraseAllAnims();

            ushort cur = (ushort)(_roomData[OldRoom] + 1);
            ushort last = _roomData[OldRoom + 1];
            for (; cur <= last; ++cur)
            {
                var pod = ObjectData[cur];
                if (pod.name == 0)
                {
                    // object has been deleted, invalidate image
                    pod.image = 0;
                }
                else if (pod.image > -4000 && pod.image <= -10)
                {
                    if (_graphicData[Math.Abs(pod.image + 10)].lastFrame == 0)
                    {
                        // static Bob
                        pod.image = -1;
                    }
                    else
                    {
                        // animated Bob
                        pod.image = -2;
                    }
                }
            }
        }

        private void SetupJoe()
        {
            LoadJoeBanks("JOE_A.BBK", "JOE_B.BBK");
            JoePrevFacing = Direction.FRONT;
            JoeFacing = Direction.FRONT;
        }

        private void LoadJoeBanks(string animBank, string standBank)
        {
            _vm.BankMan.Load(animBank, 13);
            for (uint i = 11; i < 31; ++i)
            {
                _vm.BankMan.Unpack((uint)(i - 10), i, 13);
            }
            _vm.BankMan.Close(13);

            _vm.BankMan.Load(standBank, 7);
            _vm.BankMan.Unpack(1, 35, 7);
            _vm.BankMan.Unpack(3, 36, 7);
            _vm.BankMan.Unpack(5, 37, 7);
        }

        private void InventorySetup()
        {
            _vm.BankMan.Load("OBJECTS.BBK", 14);
            if (_vm.Resource.IsInterview)
            {
                _inventoryItem[0] = (Item)1;
                _inventoryItem[1] = (Item)2;
                _inventoryItem[2] = (Item)3;
                _inventoryItem[3] = (Item)4;
            }
            else
            {
                _inventoryItem[0] = Item.ITEM_BAT;
                _inventoryItem[1] = Item.ITEM_JOURNAL;
                _inventoryItem[2] = Item.ITEM_NONE;
                _inventoryItem[3] = Item.ITEM_NONE;
            }
        }

        private void ReadQueenJas()
        {
            short i;

            uint size;
            var jas = _vm.Resource.LoadFile("QUEEN.JAS", 20, out size);
            var ptr = 0;

            _numRooms = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numNames = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numObjects = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numDescriptions = jas.ToUInt16BigEndian(ptr);
            ptr += 2;

            ObjectData = new ObjectData[_numObjects + 1];
            ObjectData[0] = new ObjectData();
            for (i = 1; i <= _numObjects; i++)
            {
                ObjectData[i] = new ObjectData();
                ObjectData[i].ReadFromBE(jas, ref ptr);
            }

            _roomData = new ushort[_numRooms + 2];
            _roomData[0] = 0;
            for (i = 1; i <= (_numRooms + 1); i++)
            {
                _roomData[i] = jas.ToUInt16BigEndian(ptr);
                ptr += 2;
            }
            _roomData[_numRooms + 1] = _numObjects;

            if ((_vm.Resource.IsDemo && _vm.Resource.Platform == Platform.DOS) ||
                (_vm.Resource.IsInterview && _vm.Resource.Platform == Platform.Amiga))
            {
                _sfxName = null;
            }
            else
            {
                _sfxName = new ushort[_numRooms + 1];
                _sfxName[0] = 0;
                for (i = 1; i <= _numRooms; i++)
                {
                    _sfxName[i] = jas.ToUInt16BigEndian(ptr);
                    ptr += 2;
                }
            }

            _numItems = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _itemData = new ItemData[_numItems + 1];
            for (i = 1; i <= _numItems; i++)
            {
                _itemData[i] = new ItemData();
                _itemData[i].ReadFromBE(jas, ref ptr);
            }

            _numGraphics = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _graphicData = new GraphicData[_numGraphics + 1];
            for (i = 1; i <= _numGraphics; i++)
            {
                _graphicData[i] = new GraphicData();
                _graphicData[i].ReadFromBE(jas, ref ptr);
            }

            _vm.Grid.ReadDataFrom(_numObjects, _numRooms, jas, ref ptr);

            _numWalkOffs = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _walkOffData = new WalkOffData[_numWalkOffs + 1];
            for (i = 1; i <= _numWalkOffs; i++)
            {
                _walkOffData[i] = new WalkOffData();
                _walkOffData[i].ReadFromBE(jas, ref ptr);
            }

            _numObjDesc = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _objectDescription = new ObjectDescription[_numObjDesc + 1];
            for (i = 1; i <= _numObjDesc; i++)
            {
                _objectDescription[i] = new ObjectDescription();
                _objectDescription[i].ReadFromBE(jas, ref ptr);
            }

            _vm.Command.ReadCommandsFrom(jas, ref ptr);

            EntryObj = jas.ToUInt16BigEndian(ptr);
            ptr += 2;

            _numFurniture = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _furnitureData = new FurnitureData[_numFurniture + 1];
            for (i = 1; i <= _numFurniture; i++)
            {
                _furnitureData[i] = new FurnitureData();
                _furnitureData[i].ReadFromBE(jas, ref ptr);
            }

            // Actors
            _numActors = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numAAnim = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numAName = jas.ToUInt16BigEndian(ptr);
            ptr += 2;
            _numAFile = jas.ToUInt16BigEndian(ptr);
            ptr += 2;

            _actorData = new ActorData[_numActors + 1];
            for (i = 1; i <= _numActors; i++)
            {
                _actorData[i] = new ActorData();
                _actorData[i].ReadFromBE(jas, ref ptr);
            }

            _numGraphicAnim = jas.ToUInt16BigEndian(ptr);
            ptr += 2;

            _graphicAnim = new GraphicAnim[_numGraphicAnim + 1];
            if (_numGraphicAnim == 0)
            {
                _graphicAnim[0] = new GraphicAnim();
                _graphicAnim[0].ReadFromBE(jas, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numGraphicAnim; i++)
                {
                    _graphicAnim[i] = new GraphicAnim();
                    _graphicAnim[i].ReadFromBE(jas, ref ptr);
                }
            }

            CurrentRoom = ObjectData[EntryObj].room;
            EntryObj = 0;

            if (System.Text.Encoding.UTF8.GetString(jas, ptr, 5) != _vm.Resource.JASVersion)
            {
                D.Warning("Unexpected queen.jas file format");
            }

            _jasStringList = _vm.Resource.LoadTextFile("QUEEN2.JAS");
            _jasStringOffset = new int[8];
            _jasStringOffset[0] = 0;
            _jasStringOffset[1] = _jasStringOffset[0] + _numDescriptions;
            _jasStringOffset[2] = _jasStringOffset[1] + _numNames;
            _jasStringOffset[3] = _jasStringOffset[2] + _numRooms;
            _jasStringOffset[4] = _jasStringOffset[3] + 12;
            _jasStringOffset[5] = _jasStringOffset[4] + JOE_RESPONSE_MAX;
            _jasStringOffset[6] = _jasStringOffset[5] + _numAAnim;
            _jasStringOffset[7] = _jasStringOffset[6] + _numAName;

            // Patch for German text bug
            if (_vm.Resource.Language == Language.DE_DEU)
            {
                _jasStringList[_jasStringOffset[Jso.JSO_OBJECT_DESCRIPTION] + 296 - 1] = "Es bringt nicht viel, das festzubinden.";
            }
        }

        private short PreviousInventoryItem(short first)
        {
            int i;
            for (i = first - 1; i >= 1; i--)
                if (_itemData[i].name > 0)
                    return (short)i;
            for (i = _numItems; i > first; i--)
                if (_itemData[i].name > 0)
                    return (short)i;

            return 0;   //nothing found
        }

        protected abstract void SetupSpecialMoveTable();

        protected void AsmMakeJoeUseDress()
        {
            JoeUseDress(false);
        }

        protected void AsmMakeJoeUseNormalClothes()
        {
            JoeUseClothes(false);
        }

        protected void AsmMakeJoeUseUnderwear()
        {
            JoeUseUnderwear();
        }

        protected void AsmSwitchToDressPalette()
        {
            _vm.Display.PalSetJoeDress();
        }

        protected void AsmSwitchToNormalPalette()
        {
            _vm.Display.PalSetJoeNormal();
        }

        protected void AsmStartCarAnimation()
        {
            _vm.Bam._flag = BamFlags.F_PLAY;
            _vm.Bam.PrepareAnimation();
        }

        protected void AsmStopCarAnimation()
        {
            _vm.Bam._flag = BamFlags.F_STOP;
            _vm.Graphics.Bobs[FindBob(594)].active = false; // oil object
            _vm.Graphics.Bobs[7].active = false; // gun shots
        }

        protected void AsmStartFightAnimation()
        {
            _vm.Bam._flag = BamFlags.F_PLAY;
            _vm.Bam.PrepareAnimation();
            GameState[148] = 1;
        }

        protected void AsmWaitForFrankPosition()
        {
            _vm.Bam._flag = BamFlags.F_REQ_STOP;
            while (_vm.Bam._flag != BamFlags.F_STOP)
            {
                _vm.Update();
            }
        }

        protected void AsmMakeFrankGrowing()
        {
            _vm.BankMan.Unpack(1, 38, 15);
            BobSlot bobFrank = _vm.Graphics.Bobs[5];
            bobFrank.frameNum = 38;
            if (_vm.Resource.Platform == Platform.Amiga)
            {
                bobFrank.active = true;
                bobFrank.x = 160;
                bobFrank.scale = 100;
                for (int i = 350; i >= 200; i -= 5)
                {
                    bobFrank.y = (short)i;
                    _vm.Update();
                }
            }
            else
            {
                bobFrank.CurPos(160, 200);
                for (int i = 10; i <= 100; i += 4)
                {
                    bobFrank.scale = (ushort)i;
                    _vm.Update();
                }
            }
            for (int i = 0; i <= 20; ++i)
            {
                _vm.Update();
            }

            ObjectData[521].name = Math.Abs(ObjectData[521].name); // Dinoray
            ObjectData[526].name = Math.Abs(ObjectData[526].name); // Frank obj
            ObjectData[522].name = (short)-Math.Abs(ObjectData[522].name); // TMPD object off
            ObjectData[525].name = (short)-Math.Abs(ObjectData[525].name); // Floda guards off
            ObjectData[523].name = (short)-Math.Abs(ObjectData[523].name); // Sparky object off
            GameState[157] = 1; // No more Ironstein
        }

        protected void AsmMakeRobotGrowing()
        {
            _vm.BankMan.Unpack(1, 38, 15);
            BobSlot bobRobot = _vm.Graphics.Bobs[5];
            bobRobot.frameNum = 38;
            if (_vm.Resource.Platform == Platform.Amiga)
            {
                bobRobot.active = true;
                bobRobot.x = 160;
                bobRobot.scale = 100;
                for (int i = 350; i >= 200; i -= 5)
                {
                    bobRobot.y = (short)i;
                    _vm.Update();
                }
            }
            else
            {
                bobRobot.CurPos(160, 200);
                for (int i = 10; i <= 100; i += 4)
                {
                    bobRobot.scale = (ushort)i;
                    _vm.Update();
                }
            }
            for (int i = 0; i <= 20; ++i)
            {
                _vm.Update();
            }

            ObjectData[524].name = (short)-Math.Abs(ObjectData[524].name); // Azura object off
            ObjectData[526].name = (short)-Math.Abs(ObjectData[526].name); // Frank object off
        }

        protected void AsmScaleTitle()
        {
            BobSlot bob = _vm.Graphics.Bobs[5];
            bob.animating = false;
            bob.x = 161;
            bob.y = 200;
            bob.scale = 100;

            int i;
            for (i = 5; i <= 100; i += 5)
            {
                bob.scale = (ushort)i;
                bob.y -= 4;
                _vm.Update();
            }
        }

        protected void AsmPanRightToHugh()
        {
            BobSlot bob_thugA1 = _vm.Graphics.Bobs[20];
            BobSlot bob_thugA2 = _vm.Graphics.Bobs[21];
            BobSlot bob_thugA3 = _vm.Graphics.Bobs[22];
            BobSlot bob_hugh1 = _vm.Graphics.Bobs[1];
            BobSlot bob_hugh2 = _vm.Graphics.Bobs[23];
            BobSlot bob_hugh3 = _vm.Graphics.Bobs[24];
            BobSlot bob_thugB1 = _vm.Graphics.Bobs[25];
            BobSlot bob_thugB2 = _vm.Graphics.Bobs[26];

            _vm.Graphics.PutCameraOnBob(-1);
            _vm.Input.FastMode = true;
            _vm.Update();

            // Adjust thug1 gun so it matches rest of body
            bob_thugA1.x += 160 - 45;
            bob_thugA2.x += 160;
            bob_thugA3.x += 160;

            bob_hugh1.x += 160 * 2;
            bob_hugh2.x += 160 * 2;
            bob_hugh3.x += 160 * 2;

            bob_thugB1.x += 160 * 3;
            bob_thugB2.x += 160 * 3;

            int horizontalScroll = 0;
            while (horizontalScroll < 160 && !_vm.Input.CutawayQuit)
            {

                horizontalScroll += 8;
                if (horizontalScroll > 160)
                    horizontalScroll = 160;

                _vm.Display.HorizontalScroll = (short)horizontalScroll;

                bob_thugA1.x -= 16;
                bob_thugA2.x -= 16;
                bob_thugA3.x -= 16;

                bob_hugh1.x -= 24;
                bob_hugh2.x -= 24;
                bob_hugh3.x -= 24;

                bob_thugB1.x -= 32;
                bob_thugB2.x -= 32;

                _vm.Update();
            }

            _vm.Input.FastMode = false;
        }

        protected void AsmPanRightToJoeAndRita()
        { // cdint.cut
            BobSlot bob_box = _vm.Graphics.Bobs[20];
            BobSlot bob_beam = _vm.Graphics.Bobs[21];
            BobSlot bob_crate = _vm.Graphics.Bobs[22];
            BobSlot bob_clock = _vm.Graphics.Bobs[23];
            BobSlot bob_hands = _vm.Graphics.Bobs[24];

            _vm.Graphics.PutCameraOnBob(-1);
            _vm.Input.FastMode = true;

            _vm.Update();

            bob_box.x += 280 * 2;
            bob_beam.x += 30;
            bob_crate.x += 180 * 3;

            int horizontalScroll = _vm.Display.HorizontalScroll;

            while (horizontalScroll < 290 && !_vm.Input.CutawayQuit)
            {

                ++horizontalScroll;
                if (horizontalScroll > 290)
                    horizontalScroll = 290;

                _vm.Display.HorizontalScroll = (short)horizontalScroll;

                bob_box.x -= 2;
                bob_beam.x -= 1;
                bob_crate.x -= 3;
                bob_clock.x -= 2;
                bob_hands.x -= 2;

                _vm.Update();
            }
            _vm.Input.FastMode = false;
        }

        protected void AsmMakeWhiteFlash()
        {
            _vm.Display.PalCustomFlash();
        }

        protected void AsmShakeScreen()
        {
            _vm.Display.Shake = false;
            _vm.Update();
            _vm.Display.Shake = true;
            _vm.Update();
        }

        protected void AsmPanLeftToBomb()
        {
            BobSlot bob21 = _vm.Graphics.Bobs[21];
            BobSlot bob22 = _vm.Graphics.Bobs[22];

            _vm.Graphics.PutCameraOnBob(-1);
            _vm.Input.FastMode = true;

            int horizontalScroll = _vm.Display.HorizontalScroll;

            while ((horizontalScroll > 0 || bob21.x < 136) && !_vm.Input.CutawayQuit)
            {

                horizontalScroll -= 5;
                if (horizontalScroll < 0)
                    horizontalScroll = 0;

                _vm.Display.HorizontalScroll = (short)horizontalScroll;

                if (horizontalScroll < 272 && bob21.x < 136)
                    bob21.x += 2;

                bob22.x += 5;

                _vm.Update();
            }

            _vm.Input.FastMode = false;
        }

        protected void AsmShrinkRobot()
        {
            int i;
            BobSlot robot = _vm.Graphics.Bobs[6];
            for (i = 100; i >= 35; i -= 5)
            {
                robot.scale = (ushort)i;
                _vm.Update();
            }
        }

        protected void AsmAltIntroPanRight()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            _vm.Input.FastMode = true;
            _vm.Update();
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx < 285 && !_vm.Input.CutawayQuit)
            {
                ++scrollx;
                if (scrollx > 285)
                {
                    scrollx = 285;
                }
                _vm.Display.HorizontalScroll = scrollx;
                _vm.Update();
            }
            _vm.Input.FastMode = false;
        }

        protected void AsmAltIntroPanLeft()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            _vm.Input.FastMode = true;
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx > 0 && !_vm.Input.CutawayQuit)
            {
                scrollx -= 4;
                if (scrollx < 0)
                {
                    scrollx = 0;
                }
                _vm.Display.HorizontalScroll = scrollx;
                _vm.Update();
            }
            _vm.Input.FastMode = false;
        }

        protected void AsmSmooch()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            BobSlot bobAzura = _vm.Graphics.Bobs[5];
            BobSlot bobJoe = _vm.Graphics.Bobs[6];
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx < 320)
            {
                scrollx += 8;
                _vm.Display.HorizontalScroll = scrollx;
                if (bobJoe.x - bobAzura.x > 128)
                {
                    bobAzura.x += 10;
                    bobJoe.x += 6;
                }
                else
                {
                    bobAzura.x += 8;
                    bobJoe.x += 8;
                }
                _vm.Update();
            }
        }

        protected void AsmEndGame()
        {
            int n = 40;
            while ((n--) != 0)
            {
                _vm.Update();
            }
            //  debug("Game completed.");
            // TODO: _vm.QuitGame();
        }

        protected void AsmEndDemo()
        {
            //  debug("Flight of the Amazon Queen, released January 95.");
            // TODO: _vm.QuitGame();
        }

        protected void AsmPutCameraOnDino()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx < 320)
            {
                scrollx += 16;
                if (scrollx > 320)
                {
                    scrollx = 320;
                }
                _vm.Display.HorizontalScroll = scrollx;
                _vm.Update();
            }
            _vm.Graphics.PutCameraOnBob(1);
        }

        protected void AsmPutCameraOnJoe()
        {
            _vm.Graphics.PutCameraOnBob(0);
        }

        protected void AsmSetAzuraInLove()
        {
            GameState[Defines.VAR_AZURA_IN_LOVE] = 1;
        }

        protected void AsmPanRightFromJoe()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx < 320)
            {
                scrollx += 16;
                if (scrollx > 320)
                {
                    scrollx = 320;
                }
                _vm.Display.HorizontalScroll = scrollx;
                _vm.Update();
            }
        }

        protected void AsmSetLightsOff()
        {
            _vm.Display.PalCustomLightsOff(CurrentRoom);
        }

        protected void AsmSetLightsOn()
        {
            _vm.Display.PalCustomLightsOn(CurrentRoom);
        }

        protected void AsmSetManequinAreaOn()
        {
            Area a = _vm.Grid.Areas[Defines.ROOM_FLODA_FRONTDESK][7];
            a.mapNeighbors = Math.Abs(a.mapNeighbors);
        }

        protected void AsmPanToJoe()
        {
            int i = _vm.Graphics.Bobs[0].x - 160;
            if (i < 0)
            {
                i = 0;
            }
            else if (i > 320)
            {
                i = 320;
            }
            _vm.Graphics.PutCameraOnBob(-1);
            short scrollx = _vm.Display.HorizontalScroll;
            if (i < scrollx)
            {
                while (scrollx > i)
                {
                    scrollx -= 16;
                    if (scrollx < i)
                    {
                        scrollx = (short)i;
                    }
                    _vm.Display.HorizontalScroll = scrollx;
                    _vm.Update();
                }
            }
            else
            {
                while (scrollx < i)
                {
                    scrollx += 16;
                    if (scrollx > i)
                    {
                        scrollx = (short)i;
                    }
                    _vm.Display.HorizontalScroll = scrollx;
                    _vm.Update();
                }
                _vm.Update();
            }
            _vm.Graphics.PutCameraOnBob(0);
        }

        protected void AsmTurnGuardOn()
        {
            GameState[Defines.VAR_GUARDS_TURNED_ON] = 1;
        }

        protected void AsmPanLeft320To144()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            short scrollx = _vm.Display.HorizontalScroll;
            while (scrollx > 144)
            {
                scrollx -= 8;
                if (scrollx < 144)
                {
                    scrollx = 144;
                }
                _vm.Display.HorizontalScroll = scrollx;
                _vm.Update();
            }
        }

        protected void AsmSmoochNoScroll()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            BobSlot bobAzura = _vm.Graphics.Bobs[5];
            BobSlot bobJoe = _vm.Graphics.Bobs[6];
            for (int i = 0; i < 320; i += 8)
            {
                if (bobJoe.x - bobAzura.x > 128)
                {
                    bobAzura.x += 2;
                    bobJoe.x -= 2;
                }
                _vm.Update();
            }
        }

        protected void AsmMakeLightningHitPlane()
        {
            _vm.Graphics.PutCameraOnBob(-1);
            short iy = 0, x, ydir = -1, j, k;

            BobSlot planeBob = _vm.Graphics.Bobs[5];
            BobSlot lightningBob = _vm.Graphics.Bobs[20];

            planeBob.y = 135;

            if (_vm.Resource.Platform == Platform.Amiga)
            {
                planeBob.scale = 100;
            }
            else
            {
                planeBob.scale = 20;
            }

            for (x = 660; x > 163; x -= 6)
            {
                planeBob.x = x;
                planeBob.y = (short)(135 + iy);

                iy -= ydir;
                if (iy < -9 || iy > 9)
                    ydir = (short)-ydir;

                planeBob.scale++;
                if (planeBob.scale > 100)
                    planeBob.scale = 100;

                int scrollX = x - 163;
                if (scrollX > 320)
                    scrollX = 320;
                _vm.Display.HorizontalScroll = (short)scrollX;
                _vm.Update();
            }

            planeBob.scale = 100;
            _vm.Display.HorizontalScroll = 0;

            planeBob.x += 8;
            planeBob.y += 6;

            lightningBob.x = 160;
            lightningBob.y = 0;

            _vm.Sound.PlaySfx(CurrentRoomSfx);

            _vm.BankMan.Unpack(18, lightningBob.frameNum, 15);
            _vm.BankMan.Unpack(4, planeBob.frameNum, 15);

            // Plane plunges into the jungle!
            BobSlot fireBob = _vm.Graphics.Bobs[6];

            fireBob.animating = true;
            fireBob.x = planeBob.x;
            fireBob.y = (short)(planeBob.y + 10);

            _vm.BankMan.Unpack(19, fireBob.frameNum, 15);
            _vm.Update();

            k = 20;
            j = 1;

            for (x = 163; x > -30; x -= 10)
            {
                planeBob.y += 4;
                fireBob.y += 4;
                planeBob.x = fireBob.x = x;

                if (k < 40)
                {
                    _vm.BankMan.Unpack((uint)j, planeBob.frameNum, 15);
                    _vm.BankMan.Unpack((uint)k, fireBob.frameNum, 15);
                    k++;
                    j++;

                    if (j == 4)
                        j = 1;
                }

                _vm.Update();
            }

            _vm.Graphics.PutCameraOnBob(0);
        }

        protected void AsmScaleBlimp()
        {
            short z = 256;
            BobSlot bob = _vm.Graphics.Bobs[7];
            short x = bob.x;
            short y = bob.y;
            bob.scale = 100;
            while (bob.x > 150 && !_vm.HasToQuit)
            {
                bob.x = (short)(x * 256 / z + 150);
                bob.y = (short)(y * 256 / z + 112);
                if (_vm.Resource.Platform != Platform.Amiga)
                {
                    bob.scale = (ushort)(100 * 256 / z);
                }
                ++z;
                if (z % 6 == 0)
                {
                    --x;
                }

                _vm.Update();
            }
        }

        protected void AsmScaleEnding()
        {
            _vm.Graphics.Bobs[7].active = false; // Turn off blimp
            BobSlot b = _vm.Graphics.Bobs[20];
            b.CurPos(160, 100);
            if (_vm.Resource.Platform != Platform.Amiga)
            {
                for (int i = 5; i <= 100; i += 5)
                {
                    b.scale = (ushort)i;
                    _vm.Update();
                }
            }
            for (int i = 0; i < 50; ++i)
            {
                _vm.Update();
            }
            _vm.Display.PalFadeOut(CurrentRoom);
        }

        protected void AsmWaitForCarPosition()
        {
            // Wait for car to reach correct position before pouring oil
            while (_vm.Bam._index != 60)
            {
                _vm.Update();
            }
        }

        protected void AsmAttemptPuzzle()
        {
            ++_puzzleAttemptCount;
            if (_puzzleAttemptCount == 4)
            {
                MakeJoeSpeak(226, true);
                _puzzleAttemptCount = 0;
            }
        }

        protected void AsmScrollTitle()
        {
            BobSlot bob = _vm.Graphics.Bobs[5];
            bob.animating = false;
            bob.x = 161;
            bob.y = 300;
            bob.scale = 100;
            while (bob.y >= 120)
            {
                _vm.Update();
                bob.y -= 4;
            }
        }

        protected void AsmInterviewIntro()
        {
            // put camera on airship
            _vm.Graphics.PutCameraOnBob(5);
            BobSlot bas = _vm.Graphics.Bobs[5];

            bas.CurPos(-30, 40);

            bas.Move(700, 10, 3);
            int scale = 450;
            while (bas.moving && !_vm.Input.CutawayQuit)
            {
                bas.scale = (ushort)(256 * 100 / scale);
                --scale;
                if (scale < 256)
                {
                    scale = 256;
                }
                _vm.Update();
            }

            bas.scale = 90;
            bas.xflip = true;

            bas.Move(560, 25, 4);
            while (bas.moving && !_vm.Input.CutawayQuit)
            {
                _vm.Update();
            }

            bas.Move(545, 65, 2);
            while (bas.moving && !_vm.Input.CutawayQuit)
            {
                _vm.Update();
            }

            bas.Move(540, 75, 2);
            while (bas.moving && !_vm.Input.CutawayQuit)
            {
                _vm.Update();
            }

            // put camera on Joe
            _vm.Graphics.PutCameraOnBob(0);
        }

        protected void AsmEndInterview()
        {
            //  debug("Interactive Interview copyright (c) 1995, IBI.");
            // TODO: _vm.QuitGame();
        }

        public void JoeUseDress(bool showCut)
        {
            if (showCut)
            {
                JoeFacing = Direction.FRONT;
                JoeFace();
                if (GameState[Defines.VAR_JOE_DRESSING_MODE] == 0)
                {
                    PlayCutaway("CDRES.CUT");
                    InventoryInsertItem(Item.ITEM_CLOTHES);
                }
                else
                {
                    PlayCutaway("CUDRS.CUT");
                }
            }
            _vm.Display.PalSetJoeDress();
            LoadJoeBanks("JOED_A.BBK", "JOED_B.BBK");
            InventoryDeleteItem(Item.ITEM_DRESS);
            GameState[Defines.VAR_JOE_DRESSING_MODE] = 2;
        }

        public void InventoryInsertItem(Item itemNum, bool refresh = true)
        {
            Item item = _inventoryItem[0] = itemNum;
            _itemData[(int)itemNum].name = Math.Abs(_itemData[(int)itemNum].name); //set visible
            for (int i = 1; i < 4; i++)
            {
                item = NextInventoryItem(item);
                _inventoryItem[i] = item;
                RemoveDuplicateItems();
            }

            if (refresh)
                InventoryRefresh();
        }

        public void JoeUseClothes(bool showCut)
        {
            if (showCut)
            {
                JoeFacing = Direction.FRONT;
                JoeFace();
                PlayCutaway("CDCLO.CUT");
                InventoryInsertItem(Item.ITEM_DRESS);
            }
            _vm.Display.PalSetJoeNormal();
            LoadJoeBanks("JOE_A.BBK", "JOE_B.BBK");
            InventoryDeleteItem(Item.ITEM_CLOTHES);
            GameState[Defines.VAR_JOE_DRESSING_MODE] = 0;
        }

        public void JoeUseUnderwear()
        {
            _vm.Display.PalSetJoeNormal();
            LoadJoeBanks("JOEU_A.BBK", "JOEU_B.BBK");
            GameState[Defines.VAR_JOE_DRESSING_MODE] = 1;
        }
    }

}

