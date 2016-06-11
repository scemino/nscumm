//
//  Cutaway.cs
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
    class CutawayAnim
    {
        public short @object;
        public short unpackFrame;          // Frame to unpack
        public short speed;
        public short bank;
        public short mx;
        public short my;
        public short cx;
        public short cy;
        public short scale;
        public short currentFrame;  // Index to Current Frame
        public short originalFrame;  // Index to Original Object Frame
        public short song;
        public bool flip;      // set this if unpackFrame is negative
    }

    //! Data for a cutaway @object
    public class CutawayObject
    {
        public short @objectNumber;
        // 0 = JOE, -1 = MESSAGE
        public short moveToX;
        public short moveToY;
        public short bank;
        // 0 = PBOB, 13 = Joe Bank, else BANK NAMEstr()
        public short animList;
        public short execute;
        // 1 Yes, 0 No
        public short limitBobX1;
        public short limitBobY1;
        public short limitBobX2;
        public short limitBobY2;
        public short specialMove;
        public short animType;
        // 0 - Packet, 1 - Amal, 2 - Unpack
        public short fromObject;
        public short bobStartX;
        public short bobStartY;
        public short room;
        public short scale;
        // Variables derived from the variables above
        public int song;

        //! People to turn on
        public int[] person = new int[Cutaway.MAX_PERSON_COUNT];

        //! Number of elements used in _person array
        public int personCount;
    }

    class PersonFace
    {
        public short index;
        public short image;
    }

    //! Different kinds of cutaway @objects
    enum ObjectType
    {
        OBJECT_TYPE_ANIMATION = 0,
        OBJECT_TYPE_PERSON = 1,
        OBJECT_TYPE_NO_ANIMATION = 2,
        OBJECT_TYPE_TEXT_SPEAK = 3,
        OBJECT_TYPE_TEXT_DISPLAY_AND_SPEAK = 4,
        OBJECT_TYPE_TEXT_DISPLAY = 5
    }

    class ObjectDataBackup
    {
        public int index;
        public short name;
        public short image;
    }

    public class Cutaway
    {
        const int PREVIOUS_ROOM = 0;
        const int CURRENT_ROOM = 0;
        const int OBJECT_ROOMFADE = -1;
        const int PERSON_JOE = -1;
        const int OBJECT_JOE = 0;
        public const int MAX_PERSON_COUNT = 6;
        const int CUTAWAY_BANK = 8;
        const int MAX_BANK_NAME_COUNT = 5;
        const int MAX_FILENAME_LENGTH = 12;
        const int MAX_FILENAME_SIZE = (MAX_FILENAME_LENGTH + 1);
        const int MAX_PERSON_FACE_COUNT = 13;
        const int MAX_STRING_LENGTH = 255;
        const int MAX_STRING_SIZE = (MAX_STRING_LENGTH + 1);
        const int LEFT = 1;
        const int RIGHT = 2;
        const int FRONT = 3;
        const int BACK = 4;

        readonly QueenEngine _vm;
        byte[] _fileData;
        string _basename;
        ushort _comPanel;
        short _cutawayObjectCount;
        int _finalRoom;
        bool _anotherCutaway;
        ByteAccess _gameStatePtr;
        ushort _nextSentenceOff;
        ByteAccess _objectData;
        string _talkFile;
        short _talkTo;
        ushort _currentImage;
        ushort _temporaryRoom;
        ushort _initialRoom;
        /// <summary>
        /// Number of entries in _personFace array.
        /// </summary>
        int _personFaceCount;
        bool _roomFade;
        /// <summary>
        /// Song played before running comic.cut.
        /// </summary>
        short _songBeforeComic;
        short _lastSong;
        /// <summary>
        /// Number of elements used in _personData array
        /// </summary>
        int _personDataCount;
        /// <summary>
        /// Used by changeRooms.
        /// </summary>
        ObjectDataBackup[] _personData;
        PersonFace[] _personFace;
        readonly string[] _bankNames = new string[MAX_BANK_NAME_COUNT];

        public static void Run(string filename, out string nextFilename, QueenEngine vm)
        {
            var cutaway = new Cutaway(filename, vm);
            cutaway.Run(out nextFilename);
        }

        private Cutaway(string filename, QueenEngine vm)
        {
            _vm = vm;
            _personData = new ObjectDataBackup[MAX_PERSON_COUNT];
            for (int i = 0; i < _personData.Length; i++)
            {
                _personData[i] = new ObjectDataBackup();
            }
            _personFace = new PersonFace[MAX_PERSON_FACE_COUNT];
            for (int i = 0; i < _personFace.Length; i++)
            {
                _personFace[i] = new PersonFace();
            }
            _vm.Input.CutawayQuitReset();
            Load(filename);
        }

        private void ClearPersonFace()
        {
            for (int i = 0; i < _personFace.Length; i++)
            {
                _personFace[i] = new PersonFace();
            }
        }

        private void Run(out string nextFilename)
        {
            int i;
            nextFilename = string.Empty;

            _currentImage = _vm.Graphics.NumFrames;

            BobSlot joeBob = _vm.Graphics.Bobs[0];
            int initialJoeX = joeBob.x;
            int initialJoeY = joeBob.y;

            D.Debug(6, $"[Cutaway::run] Joe started at ({initialJoeX}, {initialJoeY})");

            _vm.Input.CutawayRunning = true;

            _initialRoom = _temporaryRoom = _vm.Logic.CurrentRoom;

            _vm.Display.ScreenMode(_comPanel, true);

            if (_comPanel == 0 || _comPanel == 2)
            {
                _vm.Logic.SceneStart();
            }

            ClearPersonFace();
            _personFaceCount = 0;

            var ptr = new ByteAccess(_objectData);

            for (i = 0; i < _cutawayObjectCount; i++)
            {
                CutawayObject @object = new CutawayObject();
                ptr = GetCutawayObject(ptr, @object);
                //dumpCutawayObject(i, @object);

                if (@object.moveToX == 0 &&
                    @object.moveToY == 0 &&
                    @object.specialMove > 0 &&
                    @object.objectNumber >= 0)
                {
                    _vm.Logic.ExecuteSpecialMove(@object.specialMove);
                    @object.specialMove = 0;
                }

                if (CURRENT_ROOM == @object.room)
                {
                    // Get current room
                    @object.room = (short)_vm.Logic.CurrentRoom;
                }
                else
                {
                    // Change current room
                    _vm.Logic.CurrentRoom = (ushort)@object.room;
                }

                ptr = TurnOnPeople(ptr, @object);

                LimitBob(@object);

                string sentence;
                Talk.GetString(_fileData, ref _nextSentenceOff, out sentence, MAX_STRING_LENGTH);

                if (OBJECT_ROOMFADE == @object.objectNumber)
                {
                    _roomFade = true;
                    @object.objectNumber = OBJECT_JOE;
                }
                else
                {
                    _roomFade = false;
                }

                if (@object.room != _temporaryRoom)
                    ChangeRooms(@object);

                ObjectType @objectType = GetObjectType(@object);

                if (@object.song != 0)
                    _vm.Sound.PlaySong((short)@object.song);

                switch (objectType)
                {
                    case ObjectType.OBJECT_TYPE_ANIMATION:
                        ptr = HandleAnimation(ptr, @object);
                        break;
                    case ObjectType.OBJECT_TYPE_PERSON:
                        HandlePersonRecord(i + 1, @object, sentence);
                        break;
                    case ObjectType.OBJECT_TYPE_NO_ANIMATION:
                        // Do nothing?
                        break;
                    case ObjectType.OBJECT_TYPE_TEXT_SPEAK:
                    case ObjectType.OBJECT_TYPE_TEXT_DISPLAY_AND_SPEAK:
                    case ObjectType.OBJECT_TYPE_TEXT_DISPLAY:
                        HandleText(i + 1, @objectType, @object, sentence);
                        break;
                    default:
                        D.Warning($"Unhandled object type: {@objectType}");
                        break;
                }

                if (_vm.Input.CutawayQuit)
                    break;

                if (_roomFade)
                {
                    _vm.Update();
                    BobSlot j = _vm.Graphics.Bobs[0];
                    _vm.Display.PalFadeIn(_vm.Logic.CurrentRoom, j.active, j.x, j.y);
                    _roomFade = false;
                }

            } // for ()

            _vm.Display.ClearTexts(0, 198);
            // XXX lines 1887-1895 in cutaway.c

            Stop();

            UpdateGameState();

            _vm.BankMan.Close(CUTAWAY_BANK);

            TalkCore(out nextFilename);

            if (_comPanel == 0 || (_comPanel == 2 && !_anotherCutaway))
            {
                _vm.Logic.SceneStop();
                _comPanel = 0;
            }

            if (nextFilename.Length == 0 && !_anotherCutaway && _vm.Logic.CurrentRoom != Defines.ROOM_ENDING_CREDITS)
            {
                _vm.Display.Fullscreen = false;

                // Lines 2138-2182 in cutaway.c
                if (_finalRoom != 0)
                {
                    _vm.Logic.NewRoom = 0;
                    _vm.Logic.EntryObj = 0;
                }
                else
                {
                    // No need to stay in current room, so return to previous room
                    //  if one exists. Reset Joe's X,Y coords to those when first entered

                    RestorePersonData();

                    D.Debug(6, $"_vm.Logic.entryObj() = { _vm.Logic.EntryObj}");
                    if (_vm.Logic.EntryObj > 0)
                    {
                        _initialRoom = _vm.Logic.ObjectData[_vm.Logic.EntryObj].room;
                    }
                    else
                    {
                        // We're not returning to new room, so return to old Joe X,Y coords
                        D.Debug(6, $"[Cutaway::run] Moving joe to ({initialJoeX}, {initialJoeY})");
                        _vm.Logic.JoePos((ushort)initialJoeX, (ushort)initialJoeY);
                    }

                    if (_vm.Logic.CurrentRoom != _initialRoom)
                    {
                        _vm.Logic.CurrentRoom = _initialRoom;
                        _vm.Logic.ChangeRoom();
                        if (_vm.Logic.CurrentRoom == _vm.Logic.NewRoom)
                        {
                            _vm.Logic.NewRoom = 0;
                        }
                    }
                    _vm.Logic.JoePos(0, 0);
                }

                _vm.Logic.JoeCutFacing = 0;
                _comPanel = 0;

                int k = 0;
                for (i = _vm.Logic.RoomData[_vm.Logic.CurrentRoom];
                    i <= _vm.Logic.RoomData[_vm.Logic.CurrentRoom + 1]; i++)
                {

                    ObjectData @object = _vm.Logic.ObjectData[i];
                    if (@object.image == -3 || @object.image == -4)
                    {
                        k++;
                        if (@object.name > 0)
                        {
                            _vm.Graphics.ResetPersonAnim((ushort)k);
                        }
                    }
                }

                _vm.Logic.RemoveHotelItemsFromInventory();
            }

            joeBob.animating = false;
            joeBob.moving = false;

            // if the cutaway has been cancelled, we must stop the speech and the sfx as well
            if (_vm.Input.CutawayQuit)
            {
                if (_vm.Sound.IsSpeechActive)
                    _vm.Sound.StopSpeech();
                _vm.Sound.StopSfx();
            }

            _vm.Input.CutawayRunning = false;
            _vm.Input.CutawayQuitReset();
            _vm.Input.QuickSaveReset();
            _vm.Input.QuickLoadReset();

            if (_songBeforeComic > 0)
                _vm.Sound.PlaySong(_songBeforeComic);
            else if (_lastSong > 0)
                _vm.Sound.PlaySong(_lastSong);
        }

        private void TalkCore(out string nextFilename)
        {
            nextFilename = string.Empty;
            var p = ServiceLocator.FileStorage.GetExtension(_talkFile);
            if (string.Equals(p, ".DOG", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: assert(_talkTo > 0);
                int personInRoom = _talkTo - _vm.Logic.RoomData[_vm.Logic.CurrentRoom];
                _vm.Logic.StartDialogue(_talkFile, personInRoom, out nextFilename);
            }
        }

        private void RestorePersonData()
        {
            for (int i = 0; i < _personDataCount; i++)
            {
                int index = _personData[i].index;
                ObjectData objectData = _vm.Logic.ObjectData[index];
                objectData.name = _personData[i].name;
                objectData.image = _personData[i].image;
            }
        }

        private void HandleText(int index, ObjectType type, CutawayObject @object, string sentence)
        {
            // lines 1776-1863 in cutaway.c

            int spaces = CountSpaces(type, sentence);

            int x;
            int flags;

            if (ObjectType.OBJECT_TYPE_TEXT_DISPLAY == type)
            {
                x = _vm.Display.TextCenterX(sentence);
                flags = 2;
            }
            else
            {
                x = @object.bobStartX;
                flags = 1;
            }

            BobSlot bob =
                _vm.Graphics.Bobs[_vm.Logic.FindBob(Math.Abs(@object.objectNumber))];

            _vm.Graphics.SetBobText(bob, sentence, x, @object.bobStartY, @object.specialMove, flags);

            if (ObjectType.OBJECT_TYPE_TEXT_SPEAK == type || ObjectType.OBJECT_TYPE_TEXT_DISPLAY_AND_SPEAK == type)
            {
                if (_vm.Sound.SpeechOn)
                {
                    string voiceFileName = FindCdCut(_basename, index) + 1;
                    _vm.Sound.PlaySpeech(voiceFileName);
                }

                if (ObjectType.OBJECT_TYPE_TEXT_SPEAK == type && _vm.Sound.SpeechOn && !_vm.Subtitles)
                    _vm.Display.ClearTexts(0, 150);
            }

            while (true)
            {
                _vm.Update();

                if (_vm.Input.CutawayQuit)
                    return;

                if (_vm.Input.KeyVerb == Verb.SKIP_TEXT)
                {
                    _vm.Input.ClearKeyVerb();
                    break;
                }

                if ((ObjectType.OBJECT_TYPE_TEXT_SPEAK == type || ObjectType.OBJECT_TYPE_TEXT_DISPLAY_AND_SPEAK == type) && _vm.Sound.SpeechOn && _vm.Sound.SpeechSfxExists)
                {
                    if (!_vm.Sound.IsSpeechActive)
                    {
                        break;
                    }
                }
                else
                {
                    --spaces;
                    if (spaces <= 0)
                    {
                        break;
                    }
                }
            }

            _vm.Display.ClearTexts(0, 198);
            _vm.Update();
        }

        private int CountSpaces(ObjectType type, string sentence)
        {
            int tmp = 0;

            while (tmp < sentence.Length && sentence[tmp] != 0)
                tmp++;

            if (tmp < 50)
                tmp = 50;

            if (ObjectType.OBJECT_TYPE_TEXT_DISPLAY == type)
                tmp *= 3;

            return (tmp * 2) / (_vm.TalkSpeed / 3);
        }

        private void HandlePersonRecord(int index, CutawayObject @object, string sentence)
        {
            // Lines 1455-1516 in cutaway.c

            Person p = null;

            if (@object.objectNumber == OBJECT_JOE)
            {
                if (@object.moveToX != 0 || @object.moveToY != 0)
                {
                    _vm.Walk.MoveJoe(0, @object.moveToX, @object.moveToY, true);
                }
            }
            else
            {
                _vm.Logic.InitPerson(
                    (ushort)(@object.objectNumber - _vm.Logic.CurrentRoomData),
                    "", true, out p);

                if (@object.bobStartX != 0 || @object.bobStartY != 0)
                {
                    BobSlot bob = _vm.Graphics.Bobs[p.actor.bobNum];
                    bob.scale = (ushort)Scale(@object);
                    bob.x = @object.bobStartX;
                    bob.y = @object.bobStartY;
                }

                if (@object.moveToX != 0 || @object.moveToY != 0)
                    _vm.Walk.MovePerson(p, @object.moveToX, @object.moveToY, (ushort)(_currentImage + 1),
                    _vm.Logic.ObjectData[@object.objectNumber].image);
            }

            if (_vm.Input.CutawayQuit)
                return;

            if (sentence != "*")
            {
                if (sentence[0] == '#')
                {
                    D.Debug(4, $"Starting credits '{sentence.Substring(1)}'");
                    _vm.Logic.StartCredits(sentence.Substring(1));
                }
                else
                {
                    if (@object.objectNumber > 0)
                    {
                        bool foundPerson = false;

                        for (int i = 1; i <= _personFaceCount; i++)
                        {
                            if (_personFace[i].index == @object.objectNumber)
                            {
                                foundPerson = true;
                                break;
                            }
                        }

                        if (!foundPerson)
                        {
                            _personFaceCount++;
                            // TODO: assert (_personFaceCount < MAX_PERSON_FACE_COUNT);
                            _personFace[_personFaceCount].index = @object.objectNumber;
                            _personFace[_personFaceCount].image = _vm.Logic.ObjectData[@object.objectNumber].image;
                        }
                    }

                    var voiceFilePrefix = FindCdCut(_basename, index);
                    _vm.Logic.MakePersonSpeak(sentence, (@object.objectNumber == OBJECT_JOE) ? null : p, voiceFilePrefix);
                }

            }

            if (_vm.Input.CutawayQuit)
                return;
        }

        private static string FindCdCut(string basename, int index)
        {
            string result = basename.PadRight(5, '_');
            return $"{result}{index:D02}";
        }

        private int Scale(CutawayObject @object)
        {
            int scaling = 100;

            if (@object.scale > 0)
                scaling = @object.scale;
            else if (@object.objectNumber == 0)
            {
                // Only scale Joe
                int x, y;

                if (@object.bobStartX > 0 || @object.bobStartY > 0)
                {
                    x = @object.bobStartX;
                    y = @object.bobStartY;
                }
                else
                {
                    BobSlot bob = _vm.Graphics.Bobs[0];
                    x = bob.x;
                    y = bob.y;
                }

                int zone = _vm.Grid.FindAreaForPos(GridScreen.ROOM, (ushort)x, (ushort)y);
                if (zone > 0)
                {
                    Area area = _vm.Grid.Areas[_vm.Logic.CurrentRoom][zone];
                    scaling = area.CalcScale((short)y);
                }
            }

            return scaling;
        }

        private ByteAccess HandleAnimation(ByteAccess ptr, CutawayObject @object)
        {
            // lines 1517-1770 in cutaway.c
            int frameCount = 0;
            int i;

            CutawayAnim[] objAnim = new CutawayAnim[56];
            for (int j = 0; j < objAnim.Length; j++)
            {
                objAnim[j] = new CutawayAnim();
            }
            // Read animation frames
            for (;;)
            {

                int header = ptr.ToInt16BigEndian();
                ptr.Offset += 2;

                if (-2 == header)
                    break;

                //debug(6, "Animation frame %i, header = %i", frameCount, header);

                if (header > 1000)
                    throw new InvalidOperationException("Header too large");

                ptr = GetCutawayAnim(ptr, header, objAnim[frameCount]);
                //dumpCutawayAnim(objAnim[frameCount]);

                frameCount++;

                if (_vm.Input.CutawayQuit)
                    return null;
            }

            if (@object.animType == 1)
            {
                // lines 1615-1636 in cutaway.c

                D.Debug(6, $"----- Complex cutaway animation (animType = {@object.animType}) -----");

                if ((_vm.Logic.CurrentRoom == 47 || _vm.Logic.CurrentRoom == 63) &&
                    objAnim[0].@object == 1)
                {
                    //CR 2 - 3/3/95, Special harcoded section to make Oracle work...
                    MakeComplexAnimation((short)(_vm.Graphics.PersonFrames[1] - 1), objAnim, frameCount);
                }
                else
                {
                    _currentImage = (ushort)MakeComplexAnimation((short)_currentImage, objAnim, frameCount);
                }

                if (@object.bobStartX != 0 || @object.bobStartY != 0)
                {
                    BobSlot bob = _vm.Graphics.Bobs[objAnim[0].@object];
                    bob.x = @object.bobStartX;
                    bob.y = @object.bobStartY;
                }
            }

            // Setup the SYNCHRO bob channels

            for (i = 0; i < frameCount; i++)
            {
                if (objAnim[i].mx != 0 || objAnim[i].my != 0)
                {
                    BobSlot bob = _vm.Graphics.Bobs[objAnim[i].@object];
                    bob.frameNum = (ushort)objAnim[i].originalFrame;
                    bob.Move(objAnim[i].mx, objAnim[i].my, (short)((@object.specialMove > 0) ? @object.specialMove : 4));
                    // Boat room hard coded
                    if (_vm.Logic.CurrentRoom == Defines.ROOM_TEMPLE_OUTSIDE)
                    {
                        BobSlot bobJoe = _vm.Graphics.Bobs[0];
                        if (bobJoe.x < 320)
                        {
                            bobJoe.Move((short)(bobJoe.x + 346), bobJoe.y, 4);
                        }
                    }
                }
            }

            // Normal cutaway

            if (@object.animType != 1)
            {
                // lines 1657-1761 in cutaway.c

                D.Debug(6, $"----- Normal cutaway animation (animType = {@object.animType}) -----");

                for (i = 0; i < frameCount; i++)
                {
                    D.Debug(6, $"===== Animating frame {i} =====");
                    //dumpCutawayAnim(objAnim[i]);

                    BobSlot bob = _vm.Graphics.Bobs[objAnim[i].@object];
                    bob.active = true;
                    if (bob.animating)
                    {
                        bob.animating = false;
                        bob.frameNum = (ushort)objAnim[i].originalFrame;
                    }

                    if (objAnim[i].@object < 4)
                        bob.frameNum = (ushort)(31 + objAnim[i].@object);

                    if (objAnim[i].unpackFrame == 0)
                    {
                        // Turn off the bob
                        bob.active = false;
                    }
                    else
                    {
                        if (@object.animType == 2 || @object.animType == 0)
                        {
                            // Unpack animation, but do not unpack moving people

                            if (!((objAnim[i].mx > 0 || objAnim[i].my > 0) && InRange(objAnim[i].@object, 1, 3)))
                            {
                                _vm.BankMan.Unpack(
                                    (uint)objAnim[i].unpackFrame,
                                    (uint)objAnim[i].originalFrame,
                                    (uint)objAnim[i].bank);
                            }

                            if (0 == objAnim[i].@object)
                            {
                                // Scale Joe
                                bob.scale = (ushort)Scale(@object);
                            }
                        }

                        if (objAnim[i].cx != 0 || objAnim[i].cy != 0)
                        {
                            bob.x = objAnim[i].cx;
                            bob.y = objAnim[i].cy;
                        }

                        // Only flip if we are not moving or it is not a person object
                        if (!(objAnim[i].@object > 0 && objAnim[i].@object < 4) ||
                                !(objAnim[i].mx != 0 || objAnim[i].my != 0))
                            bob.xflip = objAnim[i].flip;

                        // Add frame alteration
                        if (!(objAnim[i].@object > 0 && objAnim[i].@object < 4))
                        {
                            bob.frameNum = (ushort)objAnim[i].originalFrame;
                        }

                        for (var j = 0; j < objAnim[i].speed; j++)
                            _vm.Update();
                    }

                    if (_vm.Input.CutawayQuit)
                        return null;

                    if (objAnim[i].song > 0)
                        _vm.Sound.PlaySong(objAnim[i].song);

                } // for ()
            }

            bool moving = true;

            while (moving)
            {
                moving = false;
                _vm.Update();

                for (i = 0; i < frameCount; i++)
                {
                    BobSlot bob = _vm.Graphics.Bobs[objAnim[i].@object];
                    if (bob.moving)
                    {
                        moving = true;
                        break;
                    }
                }

                if (_vm.Input.CutawayQuit)
                    return null;
            }

            return ptr;
        }

        private ByteAccess GetCutawayAnim(ByteAccess ptr, int header, CutawayAnim anim)
        {
            // lines 1531-1607 in cutaway.c
            D.Debug(6, $"[Cutaway::getCutawayAnim] header={header}");

            anim.currentFrame = 0;
            anim.originalFrame = 0;

            if (-1 == header)
                header = 0;

            if (0 == header)
            {
                anim.@object = 0;
                anim.originalFrame = 31;
            }
            else
            {
                anim.@object = (short)_vm.Logic.FindBob((short)header);
                anim.originalFrame = (short)_vm.Logic.FindFrame((short)header);
            }

            anim.unpackFrame = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            anim.speed = (short)(ptr.ToInt16BigEndian() / 3 + 1);
            ptr.Offset += 2;

            anim.bank = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            if (anim.bank == 0)
            {
                anim.bank = 15;
            }
            else
            {
                if (anim.bank != 13)
                {
                    // TODO: assert(anim.bank - 1 < MAX_BANK_NAME_COUNT);
                    _vm.BankMan.Load(_bankNames[anim.bank - 1], CUTAWAY_BANK);
                    anim.bank = 8;
                }
                else
                {
                    // Make sure we ref correct JOE bank (7)
                    anim.bank = 7;
                }
            }

            anim.mx = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            anim.my = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            anim.cx = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            anim.cy = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            anim.scale = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            if ((_vm.Resource.IsDemo && _vm.Resource.Platform == Platform.DOS) ||
                (_vm.Resource.IsInterview && _vm.Resource.Platform == Platform.Amiga))
            {
                anim.song = 0;
            }
            else
            {
                anim.song = ptr.ToInt16BigEndian();
                ptr.Offset += 2;
            }

            // Extract information that depend on the signedness of values
            if (anim.unpackFrame < 0)
            {
                anim.flip = true;
                anim.unpackFrame = (short)-anim.unpackFrame;
            }
            else
                anim.flip = false;

            return ptr;
        }

        private int MakeComplexAnimation(short currentImage, CutawayAnim[] objAnim, int frameCount)
        {
            int[] frameIndex = new int[256];
            int i;
            // TODO: assert(frameCount < 30);
            AnimFrame[] cutAnim = new AnimFrame[30];

            D.Debug(6, $"[Cutaway::makeComplexAnimation] currentImage = {currentImage}");

            for (i = 0; i < frameCount; i++)
            {
                cutAnim[i].frame = (ushort)objAnim[i].unpackFrame;
                cutAnim[i].speed = (ushort)objAnim[i].speed;
                frameIndex[objAnim[i].unpackFrame] = 1;
            }

            cutAnim[frameCount].frame = 0;
            cutAnim[frameCount].speed = 0;

            int nextFrameIndex = 1;

            for (i = 1; i < 256; i++)
                if (frameIndex[i] != 0)
                    frameIndex[i] = nextFrameIndex++;

            for (i = 0; i < frameCount; i++)
            {
                cutAnim[i].frame = (ushort)(currentImage + frameIndex[objAnim[i].unpackFrame]);
            }

            for (i = 1; i < 256; i++)
            {
                if (frameIndex[i] != 0)
                {
                    currentImage++;
                    _vm.BankMan.Unpack((uint)i, (uint)currentImage, (uint)objAnim[0].bank);
                }
            }

            _vm.Graphics.SetBobCutawayAnim((ushort)objAnim[0].@object, objAnim[0].flip, cutAnim, (byte)(frameCount + 1));
            return currentImage;
        }

        private void UpdateGameState()
        {
            // Lines 2047-2115 in cutaway.c
            var ptr = _gameStatePtr;
            var p = 0;

            int gameStateCount = ptr.ToInt16BigEndian(p);
            p += 2;

            for (int i = 0; i < gameStateCount; i++)
            {
                short stateIndex = ptr.ToInt16BigEndian(p);
                p += 2;
                short stateValue = ptr.ToInt16BigEndian(p);
                p += 2;
                short @objectIndex = ptr.ToInt16BigEndian(p);
                p += 2;
                short areaIndex = ptr.ToInt16BigEndian(p);
                p += 2;
                short areaSubIndex = ptr.ToInt16BigEndian(p);
                p += 2;
                short fromObject = ptr.ToInt16BigEndian(p);
                p += 2;

                bool update = false;

                if (stateIndex > 0)
                {
                    if (_vm.Logic.GameState[stateIndex] == stateValue)
                        update = true;
                }
                else
                {
                    _vm.Logic.GameState[Math.Abs(stateIndex)] = stateValue;
                    update = true;
                }

                if (update)
                {

                    if (objectIndex > 0)
                    {                    // Show the @object
                        ObjectData @objectData = _vm.Logic.ObjectData[objectIndex];
                        objectData.name = Math.Abs(objectData.name);
                        if (fromObject > 0)
                            _vm.Logic.ObjectCopy(fromObject, @objectIndex);
                        _vm.Graphics.RefreshObject((ushort)objectIndex);
                    }
                    else if (objectIndex < 0)
                    {               // Hide the @object
                        objectIndex = (short)-objectIndex;
                        ObjectData @objectData = _vm.Logic.ObjectData[objectIndex];
                        objectData.name = (short)-Math.Abs(objectData.name);
                        _vm.Graphics.RefreshObject((ushort)objectIndex);
                    }

                    if (areaIndex > 0)
                    {

                        // Turn area on or off

                        if (areaSubIndex > 0)
                        {
                            Area area = _vm.Grid.Areas[areaIndex][areaSubIndex];
                            area.mapNeighbors = Math.Abs(area.mapNeighbors);
                        }
                        else
                        {
                            Area area = _vm.Grid.Areas[areaIndex][Math.Abs(areaSubIndex)];
                            area.mapNeighbors = (short)-Math.Abs(area.mapNeighbors);
                        }
                    }

                }
            } // for ()
        }

        private void Stop()
        {
            // Lines 1901-2032 in cutaway.c
            var ptr = _gameStatePtr;
            var p = 0;

            // Skipping GAMESTATE data
            int gameStateCount = ptr.ToInt16BigEndian(p);
            p += 2;
            if (gameStateCount > 0)
                p += (gameStateCount * 12);

            // Get the final room and Joe's final position

            short joeRoom = ptr.ToInt16BigEndian(p);
            p += 2;
            short joeX = ptr.ToInt16BigEndian(p);
            p += 2;
            short joeY = ptr.ToInt16BigEndian(p);
            p += 2;

            D.Debug(6, $"[Cutaway::stop] Final position is room {joeRoom} and coordinates ({joeX}, {joeY})");

            if ((!_vm.Input.CutawayQuit || (!_anotherCutaway && joeRoom == _finalRoom)) &&
                joeRoom != _temporaryRoom &&
                joeRoom != 0)
            {

                D.Debug(6, $"[Cutaway::stop] Changing rooms and moving Joe");

                _vm.Logic.JoePos((ushort)joeX, (ushort)joeY);
                _vm.Logic.CurrentRoom = (ushort)joeRoom;
                _vm.Logic.OldRoom = _initialRoom;
                _vm.Logic.DisplayRoom(_vm.Logic.CurrentRoom, RoomDisplayMode.RDM_FADE_JOE_XY, 0, _comPanel, true);
            }

            if (_vm.Input.CutawayQuit)
            {
                // Lines 1927-2032 in cutaway.c
                int i;

                // Stop the credits from running
                _vm.Logic.StopCredits();

                _vm.Graphics.StopBobs();

                for (i = 1; i <= _personFaceCount; i++)
                {
                    int index = _personFace[i].index;
                    if (index > 0)
                    {
                        _vm.Logic.ObjectData[_personFace[i].index].image = _personFace[i].image;

                        _vm.Graphics.Bobs[_vm.Logic.FindBob((short)index)].xflip =
                            (_personFace[i].image != -4);
                    }
                }

                int quitObjectCount = ptr.ToInt16BigEndian(p);
                p += 2;

                for (i = 0; i < quitObjectCount; i++)
                {
                    short @objectIndex = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short fromIndex = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short x = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short y = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short room = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short frame = ptr.ToInt16BigEndian(p);
                    p += 2;
                    short bank = ptr.ToInt16BigEndian(p);
                    p += 2;

                    int bobIndex = _vm.Logic.FindBob(objectIndex);
                    ObjectData @object = _vm.Logic.ObjectData[objectIndex];

                    if (fromIndex > 0)
                    {
                        if (fromIndex == @objectIndex)
                        {
                            // Enable @object
                            @object.name = Math.Abs(@object.name);
                        }
                        else
                        {
                            _vm.Logic.ObjectCopy(fromIndex, @objectIndex);

                            ObjectData from = _vm.Logic.ObjectData[fromIndex];
                            if (@object.image != 0 && from.image == 0 && bobIndex != 0 && _vm.Logic.CurrentRoom == @object.room)
                                _vm.Graphics.ClearBob(bobIndex);
                        }

                        if (_vm.Logic.CurrentRoom == room)
                            _vm.Graphics.RefreshObject((ushort)objectIndex);
                    }

                    if (_vm.Logic.CurrentRoom == @object.room)
                    {
                        BobSlot pbs = _vm.Graphics.Bobs[bobIndex];

                        if (x != 0 || y != 0)
                        {
                            pbs.x = x;
                            pbs.y = y;
                            if (InRange(@object.image, -4, -3))
                                pbs.scale = _vm.Grid.FindScale((ushort)x, (ushort)y);
                        }

                        if (frame != 0)
                        {
                            if (0 == bank)
                                bank = 15;
                            else if (bank != 13)
                            {
                                _vm.BankMan.Load(_bankNames[bank - 1], CUTAWAY_BANK);
                                bank = 8;
                            }

                            int @objectFrame = _vm.Logic.FindFrame(objectIndex);

                            if (objectFrame == 1000)
                            {
                                _vm.Graphics.ClearBob(bobIndex);
                            }
                            else if (objectFrame != 0)
                            {
                                _vm.BankMan.Unpack((uint)Math.Abs(frame), (uint)objectFrame, (uint)bank);
                                pbs.frameNum = (ushort)objectFrame;
                                if (frame < 0)
                                    pbs.xflip = true;

                            }
                        }
                    }
                } // for ()

                short specialMove = ptr.ToInt16BigEndian(p);
                p += 2;
                if (specialMove > 0)
                    _vm.Logic.ExecuteSpecialMove(specialMove);

                _lastSong = ptr.ToInt16BigEndian(p);
                p += 2;
            }

            if (joeRoom == _temporaryRoom &&
                joeRoom != 37 && joeRoom != 105 && joeRoom != 106 &&
                (joeX != 0 || joeY != 0))
            {
                BobSlot joeBob = _vm.Graphics.Bobs[0];

                D.Debug(6, "[Cutaway::stop] Moving Joe");

                joeBob.x = joeX;
                joeBob.y = joeY;
                _vm.Logic.JoeScale = _vm.Grid.FindScale((ushort)joeX, (ushort)joeY);
                _vm.Logic.JoeFace();
            }

        }

        public bool InRange(short x, short l, short h) { return (x <= h && x >= l); }

        private void ChangeRooms(CutawayObject @object)
        {
            // Lines 1291-1385 in cutaway.c

            D.Debug(6, $"Changing from room {_temporaryRoom} to room {@object.room}");

            RestorePersonData();
            _personDataCount = 0;

            if (_finalRoom != @object.room)
            {
                int firstObjectInRoom = _vm.Logic.RoomData[@object.room] + 1;
                int lastObjectInRoom = _vm.Logic.RoomData[@object.room] + _vm.Grid.ObjMax[@object.room];

                for (int i = firstObjectInRoom; i <= lastObjectInRoom; i++)
                {
                    ObjectData objectData = _vm.Logic.ObjectData[i];

                    if (objectData.image == -3 || @objectData.image == -4)
                    {

                        // TODO: assert(_personDataCount < MAX_PERSON_COUNT);
                        //  The @object is a person! So record the details...
                        _personData[_personDataCount].index = i;
                        _personData[_personDataCount].name = @objectData.name;
                        _personData[_personDataCount].image = @objectData.image;
                        _personDataCount++;

                        // Now, check to see if we need to keep the person on
                        bool on = false;
                        for (int j = 0; j < @object.personCount; j++)
                        {
                            if (@object.person[j] == i)
                            {
                                on = true;
                                break;
                            }
                        }

                        if (on)
                        {
                            // It is needed, so ensure it's ON
                            objectData.name = Math.Abs(objectData.name);
                        }
                        else
                        {
                            // Not needed, so switch off!
                            objectData.name = (short)-Math.Abs(objectData.name);
                        }

                    }
                } // for ()
            }

            // set coordinates for Joe if he is on screen

            _vm.Logic.JoePos(0, 0);

            for (int i = 0; i < @object.personCount; i++)
            {
                if (PERSON_JOE == @object.person[i])
                {
                    _vm.Logic.JoePos((ushort)@object.bobStartX, (ushort)@object.bobStartY);
                }
            }

            _vm.Logic.OldRoom = _initialRoom;

            // FIXME: Cutaway c41f is played at the end of the command 0x178. This command
            // setups some persons and associates bob slots to them. They should be hidden as
            // their y coordinate is > 150, but they aren't ! As a workaround, we display the room
            // with the panel area enabled. We do the same problem for cutaway c62c.
            short comPanel = (short)_comPanel;
            if ((_basename == "c41f" && _temporaryRoom == 106 && @object.room == 41) ||
                (_basename == "c62c" && _temporaryRoom == 105 && @object.room == 41))
            {
                comPanel = 1;
            }

            // Hide panel before displaying the 'head room' (ie. before palette fading). This doesn't
            // match the original engine, but looks better to me.
            if (@object.room == Defines.FAYE_HEAD || @object.room == Defines.AZURA_HEAD || @object.room == Defines.FRANK_HEAD)
            {
                comPanel = 2;
            }

            RoomDisplayMode mode;

            if (_vm.Logic.JoeX == 0 && _vm.Logic.JoeY == 0)
            {
                mode = RoomDisplayMode.RDM_FADE_NOJOE;
            }
            else
            {
                // We need to display Joe on screen
                if (_roomFade)
                    mode = RoomDisplayMode.RDM_NOFADE_JOE;
                else
                    mode = RoomDisplayMode.RDM_FADE_JOE_XY;
            }

            _vm.Logic.DisplayRoom(_vm.Logic.CurrentRoom, mode, (ushort)@object.scale, comPanel, true);

            _currentImage = _vm.Graphics.NumFrames;

            _temporaryRoom = _vm.Logic.CurrentRoom;

            RestorePersonData();
        }

        private void LimitBob(CutawayObject @object)
        {
            if (@object.limitBobX1 != 0)
            {

                if (@object.objectNumber < 0)
                {
                    D.Warning($"QueenCutaway::limitBob called with @objectNumber = {@object.objectNumber}");
                    return;
                }

                BobSlot bob =
                    _vm.Graphics.Bobs[_vm.Logic.FindBob(@object.objectNumber)];

                if (bob == null)
                {
                    D.Warning("Failed to find bob");
                    return;
                }

                bob.box.x1 = @object.limitBobX1;
                bob.box.y1 = @object.limitBobY1;
                bob.box.x2 = @object.limitBobX2;
                bob.box.y2 = @object.limitBobY2;
            }
        }

        private ByteAccess TurnOnPeople(ByteAccess ptr, CutawayObject @object)
        {
            // Lines 1248-1259 in cutaway.c
            @object.personCount = ptr.ToInt16BigEndian();
            ptr.Offset += 2;

            if (@object.personCount > MAX_PERSON_COUNT)
                throw new InvalidOperationException("[Cutaway::turnOnPeople] @object.personCount > MAX_PERSON_COUNT");

            for (int i = 0; i < @object.personCount; i++)
            {
                @object.person[i] = ptr.ToInt16BigEndian();
                ptr.Offset += 2;
                D.Debug(7, $"[{i}] Turn on person {@object.person[i]}");
            }

            return ptr;
        }

        private ObjectType GetObjectType(CutawayObject @object)
        {
            // Lines 1387-1449 in cutaway.c

            ObjectType @objectType = ObjectType.OBJECT_TYPE_ANIMATION;

            if (@object.objectNumber > 0)
            {
                if (@object.animList == 0)
                {
                    // No anim frames, so treat as a PERSON, ie. allow to speak/walk
                    ObjectData @objectData = _vm.Logic.ObjectData[@object.objectNumber];
                    if (objectData.image == -3 || @objectData.image == -4)
                        objectType = ObjectType.OBJECT_TYPE_PERSON;
                }
            }
            else if (@object.objectNumber == OBJECT_JOE)
            {
                // It's Joe. See if he's to be treated as a person.
                if (@object.animList == 0)
                {
                    // There's no animation list, so Joe must be talking.
                    objectType = ObjectType.OBJECT_TYPE_PERSON;
                }
            }

            if (@object.fromObject > 0)
            {
                /* Copy FROM_OBJECT into @object */

                if (@object.objectNumber != @object.fromObject)
                {
                    _vm.Logic.ObjectCopy(@object.fromObject, @object.objectNumber);
                }
                else
                {
                    // Same @object, so just turn it on!
                    ObjectData @objectData = _vm.Logic.ObjectData[@object.objectNumber];
                    objectData.name = Math.Abs(objectData.name);
                }

                _vm.Graphics.RefreshObject((ushort)@object.objectNumber);

                // Skip doing any anim stuff
                objectType = ObjectType.OBJECT_TYPE_NO_ANIMATION;
            }

            switch (@object.objectNumber)
            {
                case -2:
                    // Text to be spoken
                    objectType = ObjectType.OBJECT_TYPE_TEXT_SPEAK;
                    break;
                case -3:
                    // Text to be displayed AND spoken
                    objectType = ObjectType.OBJECT_TYPE_TEXT_DISPLAY_AND_SPEAK;
                    break;
                case -4:
                    // Text to be displayed only (not spoken)
                    objectType = ObjectType.OBJECT_TYPE_TEXT_DISPLAY;
                    break;
            }

            if (ObjectType.OBJECT_TYPE_ANIMATION == @objectType && @object.execute == 0)
            {
                // Execute is not on, and it's an @object, so ignore any Anims
                objectType = ObjectType.OBJECT_TYPE_NO_ANIMATION;
            }

            return @objectType;
        }

        public static ByteAccess GetCutawayObject(ByteAccess ptr, CutawayObject @object)
        {
            var oldOffs = ptr.Offset;

            @object.objectNumber = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.moveToX = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.moveToY = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.bank = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.animList = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.execute = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.limitBobX1 = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.limitBobY1 = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.limitBobX2 = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.limitBobY2 = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.specialMove = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.animType = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.fromObject = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.bobStartX = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.bobStartY = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.room = ptr.ToInt16BigEndian(); ptr.Offset += 2;
            @object.scale = ptr.ToInt16BigEndian(); ptr.Offset += 2;

            if ((ptr.Offset - oldOffs) != 17 * sizeof(short))
                throw new InvalidOperationException("Wrong number of values read");

            // Make ugly reuse of data less ugly
            if (@object.limitBobX1 < 0)
            {
                @object.song = -@object.limitBobX1;
                @object.limitBobX1 = 0;
            }
            else
                @object.song = 0;

            return ptr;
        }

        private void Load(string filename)
        {
            D.Debug(6, $"----- Cutaway::load(\"{filename}\") -----");

            var ptr = _fileData = _vm.Resource.LoadFile(filename, 20);
            var p = 0;

            if (string.Equals(filename, "COMIC.CUT", StringComparison.OrdinalIgnoreCase))
                _songBeforeComic = _vm.Sound.LastOverride;

            _basename = filename.Substring(0, filename.Length - 4);

            _comPanel = ptr.ToUInt16BigEndian(p);
            p += 2;
            D.Debug(6, $"_comPanel = {_comPanel}");
            _cutawayObjectCount = ptr.ToInt16BigEndian(p);
            p += 2;

            D.Debug(6, $"_cutawayObjectCount = {_cutawayObjectCount}");

            if (_cutawayObjectCount < 0)
            {
                _cutawayObjectCount = (short)-_cutawayObjectCount;
                _vm.Input.CanQuit = false;
            }
            else
            {
                _vm.Input.CanQuit = true;
            }

            short flags1 = ptr.ToInt16BigEndian(p);
            p += 2;
            D.Debug(6, $"flags1 = {flags1}");

            if (flags1 < 0)
            {
                _vm.Logic.EntryObj = 0;
                _finalRoom = -flags1;
            }
            else
                _finalRoom = PREVIOUS_ROOM;

            _anotherCutaway = (flags1 == 1);

            D.Debug(6, $"[Cutaway::load] _finalRoom      = {_finalRoom}");
            D.Debug(6, $"[Cutaway::load] _anotherCutaway = {_anotherCutaway}");

            // Pointers to other places in the cutaway data
            _gameStatePtr = new ByteAccess(_fileData, ptr.ToUInt16BigEndian(p));
            p += 2;

            _nextSentenceOff = ptr.ToUInt16BigEndian(p);
            p += 2;

            ushort bankNamesOff = ptr.ToUInt16BigEndian(p);
            p += 2;

            _objectData = new ByteAccess(ptr, p);

            LoadStrings(bankNamesOff);

            if (_bankNames[0].Length > 0)
            {
                D.Debug(6, $"Loading bank '{_bankNames[0]}'");
                _vm.BankMan.Load(_bankNames[0], CUTAWAY_BANK);
            }

            string entryString;
            Talk.GetString(_fileData, ref _nextSentenceOff, out entryString, MAX_STRING_LENGTH);
            D.Debug(6, $"Entry string = '{entryString}'");

            _vm.Logic.JoeCutFacing = _vm.Logic.JoeFacing;
            _vm.Logic.JoeFace();

            if (entryString.Length == 3 &&
                entryString[0] == '*' &&
                entryString[1] == 'F')
            {
                switch (entryString[2])
                {
                    case 'L':
                        _vm.Logic.JoeCutFacing = Direction.LEFT;
                        break;
                    case 'R':
                        _vm.Logic.JoeCutFacing = Direction.RIGHT;
                        break;
                    case 'F':
                        _vm.Logic.JoeCutFacing = Direction.FRONT;
                        break;
                    case 'B':
                        _vm.Logic.JoeCutFacing = Direction.BACK;
                        break;
                }
            }
        }

        private void LoadStrings(ushort offset)
        {
            int bankNameCount = _fileData.ToUInt16BigEndian(offset);
            offset += 2;

            D.Debug(6, $"Bank name count = {bankNameCount}");

            /*
				 The _bankNames zero-based array is the one-based BANK_NAMEstr array in
				 the original source code.
			 */

            for (int i = 0, j = 0; i < bankNameCount; i++)
            {
                Talk.GetString(_fileData, ref offset, out _bankNames[j], MAX_FILENAME_LENGTH);
                if (_bankNames[j].Length > 0)
                {
                    D.Debug(6, $"Bank name {j} = '{_bankNames[j]}'");
                    j++;
                }
            }

            D.Debug(6, "Getting talk file");
            Talk.GetString(_fileData, ref offset, out _talkFile, MAX_FILENAME_LENGTH);
            D.Debug(6, $"Talk file = '{_talkFile}'");

            _talkTo = _fileData.ToInt16BigEndian(offset);
            D.Debug(6, $"_talkTo = {_talkTo}");
        }
    }

}
