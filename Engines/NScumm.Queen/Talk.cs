//
//  Talk.cs
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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    public enum StateGrab
    {
        NONE,
        DOWN,
        UP,
        MID
    }

    class SpeechParameters
    {
        public string name;
        public sbyte state, faceDirection;
        public sbyte body, bf, rf, af;
        public string animation;
        public sbyte ff;

        public SpeechParameters(string name, sbyte state, sbyte faceDirection, sbyte body, sbyte bf, sbyte rf, sbyte af, string animation, sbyte ff)
        {
            if (animation.Contains(" "))
                throw new ArgumentException(nameof(animation), "Invalid anim");

            this.name = name;
            this.state = state;
            this.faceDirection = faceDirection;
            this.body = body;
            this.bf = bf;
            this.rf = rf;
            this.af = af;
            this.animation = animation;
            this.ff = ff;
        }
    }

    struct DialogueNode
    {
        public short head;
        public short dialogueNodeValue1;
        public short gameStateIndex;
        public short gameStateValue;
    }

    class Talk
    {
        const int SPEAK_DEFAULT = 0;
        const int SPEAK_FACE_LEFT = -1;
        const int SPEAK_FACE_RIGHT = -2;
        const int SPEAK_FACE_FRONT = -3;
        const int SPEAK_FACE_BACK = -4;
        const int SPEAK_ORACLE = -5;
        const int SPEAK_UNKNOWN_6 = -6;
        const int SPEAK_AMAL_ON = -7;
        const int SPEAK_PAUSE = -8;
        const int SPEAK_NONE = -9;

        const int LINE_HEIGHT = 10;
        const int MAX_STRING_LENGTH = 255;
        const int MAX_TEXT_WIDTH = (320 - 18);
        const int PUSHUP = 4;
        const int ARROW_ZONE_UP = 5;
        const int ARROW_ZONE_DOWN = 6;
        const int DOG_HEADER_SIZE = 20;
        const int OPTION_TEXT_MARGIN = 24;

        QueenEngine _vm;
        bool _talkHead;
        int _oldSelectedSentenceIndex;
        int _oldSelectedSentenceValue;
        /// <summary>
        /// IDs for sentences.
        /// </summary>
        DialogueNode[][] _dialogueTree = new DialogueNode[18][];
        string[] _talkString = new string[5];
        string[] _joeVoiceFilePrefix = new string[5];
        //! String data
        ushort _person1PtrOff;

        //! Cutaway data
        ushort _cutawayPtrOff;

        //! Data used if we have talked to the person before
        ushort _person2PtrOff;

        //! Data used if we haven't talked to the person before
        ushort _joePtrOff;

        //! Greeting from person Joe has talked to before
        string _person2String;

        //! Number of dialogue levels
        short _levelMax;

        //! Unique key for this dialogue
        short _uniqueKey;

        //! Used to select voice files
        short _talkKey;

        short _jMax;

        //! Used by findDialogueString
        short _pMax;

        // Update game state efter dialogue
        short[] _gameState = new short[2];
        short[] _testValue = new short[2];
        short[] _itemNumber = new short[2];

        //! Raw .dog file data (without 20 byte header)
        byte[] _fileData;

        public TalkSelected TalkSelected
        {
            get
            {
                return _vm.Logic.TalkSelected[_uniqueKey];
            }
        }

        public bool HasTalkedTo { get { return TalkSelected.hasTalkedTo; } }

        private Talk(QueenEngine vm)
        {
            _vm = vm;
            for (int i = 0; i < 18; i++)
            {
                _dialogueTree[i] = new DialogueNode[6];
            }
            _vm.Input.TalkQuitReset();
        }

        /// <summary>
        /// Read a string from ptr and update offset.
        /// </summary>
        /// <param name="ptr">Ptr.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="str">String.</param>
        /// <param name="maxLength">Max length.</param>
        /// <param name="align">Align.</param>
        public static void GetString(byte[] ptr, ref ushort offset, out string str, int maxLength, int align = 2)
        {
            Debug.Assert((align & 1) == 0);
            str = string.Empty;
            int length = ptr[offset];
            ++offset;

            if (length > maxLength)
            {
                throw new InvalidOperationException($"String too long. Length = {length}, maxLength = {maxLength}");
            }
            else if (length != 0)
            {
                str = System.Text.Encoding.UTF8.GetString(ptr, offset, length);
                offset = (ushort)((offset + length + (align - 1)) & ~(align - 1));
            }
        }

        public static bool Speak(string sentence, Person person, string voiceFilePrefix, QueenEngine vm)
        {
            Talk talk = new Talk(vm);
            bool result;
            if (sentence != null)
                result = talk.Speak(sentence, person, voiceFilePrefix);
            else
                result = false;
            return result;
        }

        private bool Speak(string sentence, Person person, string voiceFilePrefix)
        {
            // Function SPEAK, lines 1266-1384 in talk.c
            bool personWalking = false;
            ushort segmentIndex = 0;
            ushort segmentStart = 0;
            ushort i;

            Person joe_person;
            ActorData joe_actor;

            _vm.Logic.JoeWalk = JoeWalkMode.SPEAK;

            if (person == null)
            {
                // Fill in values for use by speakSegment() etc.
                joe_person = new Person();
                joe_actor = new ActorData();

                joe_actor.bobNum = 0;
                joe_actor.color = 14;
                joe_actor.bankNum = 7;

                joe_person.actor = joe_actor;
                joe_person.name = "JOE";

                person = joe_person;
            }

            D.Debug(6, $"Sentence '{sentence}' is said by person '{person.name}' and voice files with prefix '{voiceFilePrefix}' played");

            if (sentence.Length == 0)
            {
                return personWalking;
            }

            if (string.Equals(person.name, "FAYE-H") ||
                string.Equals(person.name, "FRANK-H") ||
                string.Equals(person.name, "AZURA-H") ||
                string.Equals(person.name, "X3_RITA") ||
                (string.Equals(person.name, "JOE") && _vm.Logic.CurrentRoom == Defines.FAYE_HEAD) ||
                (string.Equals(person.name, "JOE") && _vm.Logic.CurrentRoom == Defines.AZURA_HEAD) ||
                (string.Equals(person.name, "JOE") && _vm.Logic.CurrentRoom == Defines.FRANK_HEAD))
                _talkHead = true;
            else
                _talkHead = false;

            for (i = 0; i < sentence.Length;)
            {
                if (sentence[i] == '*')
                {
                    int segmentLength = i - segmentStart;

                    i++;
                    int command = GetSpeakCommand(person, sentence, ref i);

                    if (SPEAK_NONE != command)
                    {
                        SpeakSegment(
                            sentence, segmentStart,
                            segmentLength,
                            person,
                            command,
                            voiceFilePrefix,
                            segmentIndex);
                        // XXX if (JOEWALK == 2) break
                    }

                    segmentIndex++;
                    segmentStart = i;
                }
                else
                    i++;

                if (_vm.Input.CutawayQuit || _vm.Input.TalkQuit)
                    return personWalking;
            }

            if (segmentStart != i)
            {
                SpeakSegment(
                    sentence, segmentStart,
                    i - segmentStart,
                    person,
                    0,
                    voiceFilePrefix,
                    segmentIndex);
            }

            return personWalking;
        }

        private int GetSpeakCommand(Person person, string sentence, ref ushort index)
        {
            // Lines 1299-1362 in talk.c
            int commandCode = SPEAK_DEFAULT;
            var id = sentence.Substring(index, 2);
            switch (id)
            {
                case "AO":
                    commandCode = SPEAK_AMAL_ON;
                    break;
                case "FL":
                    commandCode = SPEAK_FACE_LEFT;
                    break;
                case "FF":
                    commandCode = SPEAK_FACE_FRONT;
                    break;
                case "FB":
                    commandCode = SPEAK_FACE_BACK;
                    break;
                case "FR":
                    commandCode = SPEAK_FACE_RIGHT;
                    break;
                case "GD":
                    _vm.Logic.JoeGrab(StateGrab.DOWN);
                    commandCode = SPEAK_NONE;
                    break;
                case "GM":
                    _vm.Logic.JoeGrab(StateGrab.MID);
                    commandCode = SPEAK_NONE;
                    break;
                case "WT":
                    commandCode = SPEAK_PAUSE;
                    break;
                case "XY":
                    // For example *XY00(237,112)
                    {
                        commandCode = int.Parse(sentence.Substring(index + 2, 2));
                        int x = int.Parse(sentence.Substring(index + 5, 3));
                        int y = int.Parse(sentence.Substring(index + 9, 3));
                        if (string.Equals(person.name, "JOE"))
                            _vm.Walk.MoveJoe(0, (short)x, (short)y, _vm.Input.CutawayRunning);
                        else
                            _vm.Walk.MovePerson(person, (short)x, (short)y, _vm.Graphics.NumFrames, 0);
                        index += 11;
                        // if (JOEWALK==3) CUTQUIT=0;
                        // XXX personWalking = true;
                    }
                    break;
                default:
                    if (sentence[index + 0] >= '0' && sentence[index + 0] <= '9' &&
                        sentence[index + 1] >= '0' && sentence[index + 1] <= '9')
                    {
                        commandCode = (sentence[index] - '0') * 10 + (sentence[index + 1] - '0');
                    }
                    else
                    {
                        D.Warning($"Unknown command string: '{sentence.Substring(index)}'");
                    }
                    break;
            }

            index += 2;

            return commandCode;
        }

        public static void DoTalk(string filename, int personInRoom, out string cutawayFilename, QueenEngine vm)
        {
            Talk talk = new Talk(vm);
            talk.DoTalk(filename, personInRoom, out cutawayFilename);
        }

        private void DoTalk(string filename, int personInRoom, out string cutawayFilename)
        {
            int i;
            _oldSelectedSentenceIndex = 0;
            _oldSelectedSentenceValue = 0;

            D.Debug(6, $"----- talk(\"{filename}\") -----");

            cutawayFilename = string.Empty;

            Load(filename);

            Person person = new Person();
            _vm.Logic.InitPerson((ushort)personInRoom, "", false, out person);

            if (null == person.name)
            {
                throw new InvalidOperationException("Invalid person object");
            }

            short oldLevel = 0;

            // Lines 828-846 in talk.c
            for (i = 1; i <= 4; i++)
            {
                if (SelectedValue(i) > 0)
                {
                    // This option has been redefined so display new dialogue option
                    _dialogueTree[1][i].head = SelectedValue(i);
                }
                else if (SelectedValue(i) == -1)
                {
                    // Already selected so don't redisplay
                    if (_dialogueTree[1][i].gameStateIndex >= 0)
                    {
                        _dialogueTree[1][i].head = -1;
                        _dialogueTree[1][i].dialogueNodeValue1 = -1;
                        _dialogueTree[1][i].gameStateIndex = -1;
                        _dialogueTree[1][i].gameStateValue = -1;
                    }
                }
            }

            InitialTalk();

            // Lines 906-? in talk.c
            _vm.Display.ShowMouseCursor(true);

            short level = 1, retval = 0;
            short head = _dialogueTree[level][0].head;
            short index;

            // TODO: split this loop in several functions
            while (retval != -1)
            {
                string otherVoiceFilePrefix;

                _talkString[0] = string.Empty;

                if (HasTalkedTo && head == 1)
                    _talkString[0] = _person2String;
                else
                    FindDialogueString(_person1PtrOff, head, _pMax, out _talkString[0]);

                if (HasTalkedTo && head == 1)
                    otherVoiceFilePrefix = $"{_talkKey:D2}XXXXP";
                else
                    otherVoiceFilePrefix = $"{_talkKey:D2}{head:x4}P";

                if (_talkString[0].Length == 0 && retval > 1)
                {
                    FindDialogueString(_person1PtrOff, retval, _pMax, out _talkString[0]);
                    otherVoiceFilePrefix = $"{_talkKey:D2}{retval:x4}P";
                }

                // Joe dialogue
                for (i = 1; i <= 4; i++)
                {
                    FindDialogueString(_joePtrOff, _dialogueTree[level][i].head, _jMax, out _talkString[i]);

                    index = _dialogueTree[level][i].gameStateIndex;

                    if (index < 0 && _vm.Logic.GameState[Math.Abs(index)] != _dialogueTree[level][i].gameStateValue)
                        _talkString[i] = string.Empty;

                    _joeVoiceFilePrefix[i] = $"{_talkKey:D2}{_dialogueTree[level][i].head:x4}J";
                }

                // Check to see if (all the dialogue options have been selected.
                // if this is the case, and the last one left is the exit option,
                // then automatically set S to that and exit.

                int choicesLeft = 0;
                int selectedSentence = 0;

                for (i = 1; i <= 4; i++)
                {
                    if (_talkString[i].Length != 0)
                    {
                        choicesLeft++;
                        selectedSentence = i;
                    }
                }

                D.Debug(6, $"choicesLeft = {choicesLeft}");

                if (1 == choicesLeft)
                {
                    // Automatically run the final dialogue option
                    Speak(_talkString[0], person, otherVoiceFilePrefix);

                    if (_vm.Input.TalkQuit)
                        break;

                    Speak(_talkString[selectedSentence], null, _joeVoiceFilePrefix[selectedSentence]);
                }
                else
                {
                    if (person.actor.bobNum > 0)
                    {
                        Speak(_talkString[0], person, otherVoiceFilePrefix);
                        selectedSentence = SelectSentence();
                    }
                    else
                    {
                        D.Warning("bobBum is %i", person.actor.bobNum);
                        selectedSentence = 0;
                    }
                }

                if (_vm.Input.TalkQuit || _vm.HasToQuit)
                    break;

                retval = _dialogueTree[level][selectedSentence].dialogueNodeValue1;
                head = _dialogueTree[level][selectedSentence].head;
                oldLevel = level;
                level = 0;

                // Set LEVEL to the selected child in dialogue tree

                for (i = 1; i <= _levelMax; i++)
                    if (_dialogueTree[i][0].head == head)
                        level = (short)i;

                if (0 == level)
                {
                    // No new level has been selected, so lets set LEVEL to the
                    // tree path pointed to by the RETVAL

                    for (i = 1; i <= _levelMax; i++)
                        for (int j = 0; j <= 5; j++)
                            if (_dialogueTree[i][j].head == retval)
                                level = (short)i;

                    DisableSentence(oldLevel, selectedSentence);
                }
                else
                { // 0 != level
                  // Check to see if Person Return value is positive, if it is, then
                  // change the selected dialogue option to the Return value

                    if (_dialogueTree[level][0].dialogueNodeValue1 > 0)
                    {
                        if (1 == oldLevel)
                        {
                            _oldSelectedSentenceIndex = selectedSentence;
                            _oldSelectedSentenceValue = SelectedValue(selectedSentence);
                            SelectedValue(selectedSentence, _dialogueTree[level][0].dialogueNodeValue1);
                        }

                        _dialogueTree[oldLevel][selectedSentence].head = _dialogueTree[level][0].dialogueNodeValue1;
                        _dialogueTree[level][0].dialogueNodeValue1 = -1;
                    }
                    else
                    {
                        DisableSentence(oldLevel, selectedSentence);
                    }
                }

                // Check selected person to see if any Gamestates need setting

                index = _dialogueTree[level][0].gameStateIndex;
                if (index > 0)
                    _vm.Logic.GameState[index] = _dialogueTree[level][0].gameStateValue;

                // if the selected dialogue line has a POSITIVE game state value
                // then set gamestate to Value = TALK(OLDLEVEL,S,3)

                index = _dialogueTree[oldLevel][selectedSentence].gameStateIndex;
                if (index > 0)
                    _vm.Logic.GameState[index] = _dialogueTree[oldLevel][selectedSentence].gameStateValue;

                // check to see if person has something final to say
                if (-1 == retval)
                {
                    FindDialogueString(_person1PtrOff, head, _pMax, out _talkString[0]);
                    if (_talkString[0].Length != 0)
                    {
                        otherVoiceFilePrefix = $"{_talkKey:D2}{head:x4}P";
                        Speak(_talkString[0], person, otherVoiceFilePrefix);
                    }
                }
            }

            cutawayFilename = string.Empty;

            for (i = 0; i < 2; i++)
            {
                if (_gameState[i] > 0)
                {
                    if (_vm.Logic.GameState[_gameState[i]] == _testValue[i])
                    {
                        if (_itemNumber[i] > 0)
                            _vm.Logic.InventoryInsertItem((NScumm.Queen.Item)_itemNumber[i]);
                        else
                            _vm.Logic.InventoryDeleteItem((NScumm.Queen.Item)Math.Abs(_itemNumber[i]));
                    }
                }
            }

            _vm.Grid.SetupPanel();

            ushort offset = _cutawayPtrOff;

            short cutawayGameState = _fileData.ToInt16BigEndian(offset); offset += 2;
            short cutawayTestValue = _fileData.ToInt16BigEndian(offset); offset += 2;

            if (_vm.Logic.GameState[cutawayGameState] == cutawayTestValue)
            {
                GetString(_fileData, ref offset, out cutawayFilename, 20);
                if (cutawayFilename.Length > 0)
                {
                    //CR 2 - 7/3/95, If we're executing a cutaway scene, then make sure
                    // Joe can talk, so set TALKQUIT to 0 just in case we exit on the
                    // line that set's the cutaway game states.
                    _vm.Input.TalkQuitReset();
                }
            }
            if (_vm.Input.TalkQuit)
            {
                if (_oldSelectedSentenceIndex > 0)
                    SelectedValue(_oldSelectedSentenceIndex, (short)_oldSelectedSentenceValue);
                _vm.Input.TalkQuitReset();
                _vm.Display.ClearTexts(0, 198);
                _vm.Logic.MakeJoeSpeak(15, false);
            }
            else
            {
                SetHasTalkedTo();
            }

            _vm.Logic.JoeFace();

            if (cutawayFilename.Length == 0)
            {
                BobSlot pbs = _vm.Graphics.Bobs[person.actor.bobNum];

                pbs.x = (short)person.actor.x;
                pbs.y = (short)person.actor.y;

                // Better kick start the persons anim sequence
                _vm.Graphics.ResetPersonAnim((ushort)person.actor.bobNum);
            }

            _vm.Logic.JoeWalk = JoeWalkMode.NORMAL;
        }

        short SelectSentence()
        {
            int selectedSentence = 0;

            int startOption = 1;
            int optionLines = 0;
            string[] optionText = new string[5];
            int[] talkZone = new int[5];
            int i;

            _vm.Display.TextCurrentColor(_vm.Display.GetInkColor(InkColor.INK_TALK_NORMAL));

            _vm.Graphics.SetupArrows();
            BobSlot arrowBobUp = _vm.Graphics.Bobs[Graphics.ARROW_BOB_UP];
            arrowBobUp.active = false;
            BobSlot arrowBobDown = _vm.Graphics.Bobs[Graphics.ARROW_BOB_DOWN];
            arrowBobDown.active = false;

            bool rezone = true;

            while (rezone)
            {
                rezone = false;

                // Set zones for UP/DOWN text arrows when not English version

                _vm.Grid.Clear(GridScreen.PANEL);

                if (_vm.Resource.Language != Language.EN_ANY)
                {
                    _vm.Grid.SetZone(GridScreen.PANEL, ARROW_ZONE_UP, MAX_TEXT_WIDTH + 1, 0, 319, 24);
                    _vm.Grid.SetZone(GridScreen.PANEL, ARROW_ZONE_DOWN, MAX_TEXT_WIDTH + 1, 25, 319, 49);
                }

                _vm.Display.ClearTexts(151, 199);

                int sentenceCount = 0;
                int yOffset = 1;

                for (i = startOption; i <= 4; i++)
                {
                    talkZone[i] = 0;

                    if (_talkString[i].Length != 0)
                    {
                        sentenceCount++;
                        optionLines = SplitOption(_talkString[i], optionText);

                        if (yOffset < 5)
                        {
                            _vm.Grid.SetZone(
                                GridScreen.PANEL,
                                (short)i,
                                    0,
                                (short)(yOffset * LINE_HEIGHT - PUSHUP),
                                (short)((_vm.Resource.Language == Language.EN_ANY) ? 319 : MAX_TEXT_WIDTH),
                                (short)((yOffset + optionLines) * LINE_HEIGHT - PUSHUP));
                        }

                        int j;
                        for (j = 0; j < optionLines; j++)
                        {
                            if (yOffset < 5)
                            {
                                _vm.Display.SetText(
                                    (ushort)((j == 0) ? 0 : OPTION_TEXT_MARGIN),
                                    (ushort)(150 - PUSHUP + yOffset * LINE_HEIGHT),
                                        optionText[j]);
                            }
                            yOffset++;
                        }

                        talkZone[i] = sentenceCount;
                    }
                }

                yOffset--;

                // Up and down dialogue arrows

                if (_vm.Resource.Language != Language.EN_ANY)
                {
                    arrowBobUp.active = (startOption > 1);
                    arrowBobDown.active = (yOffset > 4);
                }

                _vm.Input.ClearKeyVerb();
                _vm.Input.ClearMouseButton();

                if (sentenceCount > 0)
                {
                    int oldZone = 0;

                    while (0 == selectedSentence && !_vm.Input.TalkQuit && !_vm.HasToQuit)
                    {
                        _vm.Update();

                        Point mouse = _vm.Input.MousePos;
                        int zone = _vm.Grid.FindZoneForPos(GridScreen.PANEL, (ushort)mouse.X, (ushort)mouse.Y);

                        int mouseButton = _vm.Input.MouseButton;
                        _vm.Input.ClearMouseButton();

                        if (ARROW_ZONE_UP == zone || ARROW_ZONE_DOWN == zone)
                        {
                            if (oldZone > 0)
                            {
                                short y;
                                Box b = _vm.Grid.Zone(GridScreen.PANEL, (ushort)oldZone);
                                for (y = b.y1; y < b.y2; y += 10)
                                    _vm.Display.TextColor((ushort)(150 + y), _vm.Display.GetInkColor(InkColor.INK_TALK_NORMAL));
                                oldZone = 0;
                            }
                            if (mouseButton != 0)
                            {
                                if (zone == ARROW_ZONE_UP && arrowBobUp.active)
                                {
                                    startOption--;
                                }
                                else if (zone == ARROW_ZONE_DOWN && arrowBobDown.active)
                                {
                                    startOption++;
                                }
                            }
                            rezone = true;
                            break;
                        }
                        else
                        {
                            if (oldZone != zone)
                            {
                                // Changed zone, change text colors
                                int y;

                                D.Debug(6, "Changed zone. oldZone = %i, zone = %i",
                                        oldZone, zone);

                                if (zone > 0)
                                {
                                    Box b = _vm.Grid.Zone(GridScreen.PANEL, (ushort)zone);
                                    for (y = b.y1; y < b.y2; y += 10)
                                        _vm.Display.TextColor((ushort)(150 + y), _vm.Display.GetInkColor(InkColor.INK_JOE));
                                }

                                if (oldZone > 0)
                                {
                                    Box b = _vm.Grid.Zone(GridScreen.PANEL, (ushort)oldZone);
                                    for (y = b.y1; y < b.y2; y += 10)
                                        _vm.Display.TextColor((ushort)(150 + y), _vm.Display.GetInkColor(InkColor.INK_TALK_NORMAL));
                                }

                                oldZone = zone;
                            }

                        }

                        Verb v = _vm.Input.KeyVerb;
                        if (v >= Verb.DIGIT_FIRST && v <= Verb.DIGIT_LAST)
                        {
                            int n = v - Verb.DIGIT_FIRST + 1;
                            for (i = 1; i <= 4; i++)
                            {
                                if (talkZone[i] == n)
                                {
                                    selectedSentence = i;
                                    break;
                                }
                            }

                            _vm.Input.ClearKeyVerb();
                        }
                        else if (mouseButton != 0)
                        {
                            selectedSentence = zone;
                        }

                    } // while ()
                }
            }

            _vm.Input.ClearKeyVerb();
            _vm.Input.ClearMouseButton();

            D.Debug(6, $"Selected sentence {selectedSentence}");

            arrowBobUp.active = false;
            arrowBobDown.active = false;

            if (selectedSentence > 0)
            {
                _vm.Display.ClearTexts(0, 198);

                Speak(_talkString[selectedSentence], null, _joeVoiceFilePrefix[selectedSentence]);
            }

            _vm.Display.ClearTexts(151, 151);

            return (short)selectedSentence;
        }

        int SplitOption(string str, string[] optionText)
        {
            string option = str;
            // option text ends at '*' char
            var p = option.IndexOf('*');
            if (p != -1)
            {
                option = option.Substring(0, p);
            }
            int lines;
            if (_vm.Resource.Language == Language.EN_ANY || _vm.Display.TextWidth(option) <= MAX_TEXT_WIDTH)
            {
                optionText[0] = option;
                lines = 1;
            }
            else if (_vm.Resource.Language == Language.HE_ISR)
            {
                lines = SplitOptionHebrew(option, optionText);
            }
            else
            {
                lines = SplitOptionDefault(option, optionText);
            }
            return lines;
        }

        int SplitOptionDefault(string option, string[] optionText)
        {
            throw new NotImplementedException();
        }

        int SplitOptionHebrew(string option, string[] optionText)
        {
            throw new NotImplementedException();
        }

        void DisableSentence(short oldLevel, int selectedSentence)
        {
            // Mark off selected option

            if (1 == oldLevel)
            {
                if (_dialogueTree[oldLevel][selectedSentence].dialogueNodeValue1 != -1)
                {
                    // Make sure choice is not exit option
                    _oldSelectedSentenceIndex = selectedSentence;
                    _oldSelectedSentenceValue = SelectedValue(selectedSentence);
                    SelectedValue(selectedSentence, -1);
                }
            }

            // Cancel selected dialogue line, so that its no longer displayed
            _dialogueTree[oldLevel][selectedSentence].head = -1;
            _dialogueTree[oldLevel][selectedSentence].dialogueNodeValue1 = -1;
        }

        void FindDialogueString(ushort offset, short id, short max, out string str)
        {
            str = string.Empty;
            for (int i = 1; i <= max; i++)
            {
                offset += 2;
                short currentId = _fileData.ToInt16BigEndian(offset);
                offset += 2;
                if (id == currentId)
                {
                    GetString(_fileData, ref offset, out str, MAX_STRING_LENGTH, 4);
                    break;
                }
                else
                {
                    string tmp;
                    GetString(_fileData, ref offset, out tmp, MAX_STRING_LENGTH, 4);
                }
            }
        }

        void SetHasTalkedTo()
        {
            TalkSelected.hasTalkedTo = true;
        }

        void InitialTalk()
        {
            // Lines 848-903 in talk.c

            ushort offset = (ushort)(_joePtrOff + 2);
            ushort hasNotString = _fileData.ToUInt16BigEndian(offset); offset += 2;

            string joeString;
            if (hasNotString == 0)
            {
                GetString(_fileData, ref offset, out joeString, MAX_STRING_LENGTH);
            }
            else
            {
                joeString = string.Empty;
            }

            offset = _person2PtrOff;
            string joe2String;
            GetString(_fileData, ref offset, out _person2String, MAX_STRING_LENGTH);
            GetString(_fileData, ref offset, out joe2String, MAX_STRING_LENGTH);

            if (!HasTalkedTo)
            {
                // Not yet talked to this person
                if (joeString[0] != '0')
                {
                    string voiceFilePrefix = $"{_talkKey:D2}SSSSJ";
                    Speak(joeString, null, voiceFilePrefix);
                }
            }
            else
            {
                // Already spoken to them, choose second response
                if (joe2String[0] != '0')
                {
                    string voiceFilePrefix = $"{_talkKey:D2}XXXXJ";
                    Speak(joe2String, null, voiceFilePrefix);
                }
            }
        }

        short SelectedValue(int index)
        {
            return TalkSelected.values[index - 1];
        }

        void SelectedValue(int index, short value)
        {
            TalkSelected.values[index - 1] = value;
        }

        void Load(string filename)
        {
            int i;
            _fileData = LoadDialogFile(filename);
            int ptr = 0;

            // Load talk header

            _levelMax = _fileData.ToInt16BigEndian(ptr); ptr += 2;

            if (_levelMax < 0)
            {
                _levelMax = (short)-_levelMax;
                _vm.Input.CanQuit = false;
            }
            else
            {
                _vm.Input.CanQuit = true;
            }

            _uniqueKey = _fileData.ToInt16BigEndian(ptr); ptr += 2;
            _talkKey = _fileData.ToInt16BigEndian(ptr); ptr += 2;
            _jMax = _fileData.ToInt16BigEndian(ptr); ptr += 2;
            _pMax = _fileData.ToInt16BigEndian(ptr); ptr += 2;

            for (i = 0; i < 2; i++)
            {
                _gameState[i] = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                _testValue[i] = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                _itemNumber[i] = _fileData.ToInt16BigEndian(ptr); ptr += 2;
            }

            _person1PtrOff = _fileData.ToUInt16BigEndian(ptr); ptr += 2;
            _cutawayPtrOff = _fileData.ToUInt16BigEndian(ptr); ptr += 2;
            _person2PtrOff = _fileData.ToUInt16BigEndian(ptr); ptr += 2;
            _joePtrOff = (ushort)(32 + _levelMax * 96);

            // Load dialogue tree
            ptr = 32;
            _dialogueTree[0][0] = new DialogueNode();
            for (i = 1; i <= _levelMax; i++)
                for (int j = 0; j <= 5; j++)
                {
                    ptr += 2;
                    _dialogueTree[i][j].head = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                    ptr += 2;
                    _dialogueTree[i][j].dialogueNodeValue1 = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                    ptr += 2;
                    _dialogueTree[i][j].gameStateIndex = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                    ptr += 2;
                    _dialogueTree[i][j].gameStateValue = _fileData.ToInt16BigEndian(ptr); ptr += 2;
                }
        }

        struct DogFile
        {
            public string filename;
            public Language language;
        }

        private static readonly DogFile[] dogFiles = {
            new DogFile { filename = "CHIEF1.DOG", language = Language.FR_FRA},
            new DogFile { filename = "CHIEF2.DOG", language = Language.FR_FRA },
            new DogFile { filename = "BUD1.DOG",   language = Language.IT_ITA }
        };

        byte[] LoadDialogFile(string filename)
        {
            for (int i = 0; i < dogFiles.Length; ++i)
            {
                if (string.Equals(filename, dogFiles[i].filename, StringComparison.OrdinalIgnoreCase) &&
                    _vm.Resource.Language == dogFiles[i].language)
                {
                    var path = ScummHelper.LocatePath(ServiceLocator.FileStorage.GetDirectoryName(_vm.Settings.Game.Path), filename);
                    if (path != null)
                    {
                        var fdog = ServiceLocator.FileStorage.OpenFileRead(path);
                        D.Debug(6, $"Loading dog file '{filename}' from game data path");
                        int size = (int)(fdog.Length - DOG_HEADER_SIZE);
                        byte[] buf = new byte[size];
                        fdog.Seek(DOG_HEADER_SIZE, System.IO.SeekOrigin.Begin);
                        fdog.Read(buf, 0, size);
                        return buf;
                    }
                }
            }
            return _vm.Resource.LoadFile(filename, DOG_HEADER_SIZE);
        }

        private void SpeakSegment(
            string segmentStart,
            int offset,
            int length,
            Person person,
            int command,
            string voiceFilePrefix,
            int index)
        {
            int i;

            string segment = segmentStart.Substring(offset, length);
            string voiceFileName = $"{voiceFilePrefix}{(index + 1):x1}";

            // French talkie version has a useless voice file ; c30e_102 file is the same as c30e_101,
            // so there is no need to play it. This voice was used in room 30 (N8) when talking to Klunk.
            if (!(_vm.Resource.Language == Language.FR_FRA && voiceFileName == "c30e_102"))
                _vm.Sound.PlaySpeech(voiceFileName);

            int faceDirectionCommand = 0;

            switch (command)
            {
                case SPEAK_PAUSE:
                    for (i = 0; i < 10 && !_vm.Input.TalkQuit && !_vm.HasToQuit; i++)
                    {
                        _vm.Update();
                    }
                    return;

                case SPEAK_FACE_LEFT:
                case SPEAK_FACE_RIGHT:
                case SPEAK_FACE_FRONT:
                case SPEAK_FACE_BACK:
                    faceDirectionCommand = command;
                    command = 0;
                    break;
            }

            bool isJoe = (0 == person.actor.bobNum);

            short bobNum = person.actor.bobNum;
            ushort color = person.actor.color;
            ushort bankNum = person.actor.bankNum;

            BobSlot bob = _vm.Graphics.Bobs[bobNum];

            bool oracle = false;
            int textX = 0;
            int textY = 0;

            if (!isJoe)
            {
                if (SPEAK_AMAL_ON == command)
                {
                    // It's the oracle!
                    // Don't turn AMAL animation off, and don't manually anim person
                    command = SPEAK_ORACLE;
                    oracle = true;
                    ushort frameNum = (ushort)_vm.Graphics.PersonFrames[bobNum];
                    for (i = 5; i <= 8; ++i)
                    {
                        _vm.BankMan.Unpack((uint)i, frameNum, bankNum);
                        ++frameNum;
                    }
                }
                else
                {
                    bob.animating = false;
                    bob.frameNum = (ushort)(31 + bobNum);
                }
            }

            if (_talkHead)
            {
                // talk.c lines 1491-1533
                switch (_vm.Logic.CurrentRoom)
                {
                    case Defines.FAYE_HEAD:
                        textX = 15;
                        if (_vm.Resource.Platform == Platform.Amiga)
                        {
                            color = (ushort)(isJoe ? 15 : 29);
                        }
                        break;
                    case Defines.AZURA_HEAD:
                        textX = 15;
                        if (_vm.Resource.Platform == Platform.Amiga)
                        {
                            color = (ushort)(isJoe ? 6 : 30);
                        }
                        break;
                    default: // FRANK_HEAD
                        textX = 150;
                        if (_vm.Resource.Platform == Platform.Amiga)
                        {
                            color = 17;
                        }
                        break;
                }
                textY = isJoe ? 30 : 60;
            }
            else
            {
                textX = bob.x;
                textY = bob.y;
            }

            // Set the focus rectangle
            // FIXME: This may not be correct!
            BobFrame pbf = _vm.BankMan.FetchFrame(bob.frameNum);

            int height = (pbf.height * bob.scale) / 100;

            Rect focus = new Rect(textX - 96, textY - height - 64, textX + 96, textY + height + 64);
            _vm.Display.SetFocusRect(focus);

            //int SF = _vm.grid().findScale(textX, textY);

            SpeechParameters parameters = null;
            int startFrame = 0;

            if (_talkHead && isJoe)
            {
                if (_vm.Subtitles)
                    _vm.Graphics.SetBobText(bob, segment, textX, (short)textY, color, 1);
                DefaultAnimation(segment, isJoe, parameters, startFrame, bankNum);
            }
            else
            {
                if (SPEAK_UNKNOWN_6 == command)
                    return;

                if (isJoe)
                {
                    if (_vm.Logic.CurrentRoom == 108)
                        parameters = FindSpeechParameters("JOE-E", command, 0);
                    else
                        parameters = FindSpeechParameters("JOE", command, _vm.Logic.JoeFacing);
                }
                else
                    parameters = FindSpeechParameters(person.name, command, 0);

                startFrame = 31 + bobNum;
                Direction faceDirection = 0;

                if (isJoe && _vm.Logic.JoeFacing == Direction.LEFT)
                    faceDirection = Direction.LEFT;
                else if (!isJoe)
                {
                    ObjectData data = _vm.Logic.ObjectData[_vm.Logic.ObjectForPerson((ushort)bobNum)];

                    if (data.image == -3)
                        faceDirection = Direction.LEFT;

                    if (faceDirectionCommand == SPEAK_FACE_LEFT)
                        data.image = -3;
                    else if (faceDirectionCommand == SPEAK_FACE_RIGHT)
                        data.image = -4;
                }

                if (faceDirectionCommand != 0)
                {
                    switch (faceDirectionCommand)
                    {
                        case SPEAK_FACE_LEFT:
                            faceDirection = Direction.LEFT;
                            break;
                        case SPEAK_FACE_RIGHT:
                            faceDirection = Direction.RIGHT;
                            break;
                        case SPEAK_FACE_FRONT:
                            faceDirection = Direction.FRONT;
                            break;
                        case SPEAK_FACE_BACK:
                            faceDirection = Direction.BACK;
                            break;
                    }
                    if (isJoe)
                        _vm.Logic.JoeFacing = faceDirection;
                }

                if (!isJoe)
                {
                    bob.xflip = (faceDirection == Direction.LEFT);
                }

                // Run animated sequence if SANIMstr is primed

                if (_talkHead)
                {
                    // talk.c lines 1612-1635
                    HeadStringAnimation(parameters, bobNum, bankNum);
                }

                if (_vm.Subtitles)
                    _vm.Graphics.SetBobText(bob, segment, textX, (short)textY, color, _talkHead ? 1 : 0);

                if (parameters.animation.Length != 0 && parameters.animation[0] != 'E')
                {
                    StringAnimation(parameters, startFrame, bankNum);
                }
                else
                {
                    _vm.BankMan.Unpack((uint)parameters.body, (uint)startFrame, bankNum);

                    if (length == 0 && !isJoe && parameters.bf > 0)
                    {
                        _vm.BankMan.Overpack((uint)parameters.bf, (uint)startFrame, bankNum);
                        _vm.Update();
                    }

                    if (-1 == parameters.rf)
                    {
                        // Setup the Torso frames
                        _vm.BankMan.Overpack((uint)parameters.bf, (uint)startFrame, bankNum);
                        if (isJoe)
                            parameters = FindSpeechParameters(person.name, 0, _vm.Logic.JoeFacing);
                        else
                            parameters = FindSpeechParameters(person.name, 0, 0);
                    }

                    if (-2 == parameters.rf)
                    {
                        // Setup the Torso frames
                        _vm.BankMan.Overpack((uint)parameters.bf, (uint)startFrame, bankNum);
                        if (isJoe)
                            parameters = FindSpeechParameters(person.name, 14, _vm.Logic.JoeFacing);
                        else
                            parameters = FindSpeechParameters(person.name, 14, 0);
                    }

                    DefaultAnimation(segment, isJoe, parameters, startFrame, bankNum);
                }
            }

            // Moved here so that Text is cleared when a Torso command done!
            _vm.Display.ClearTexts(0, 198);

            if (oracle)
            {
                ushort frameNum = (ushort)_vm.Graphics.PersonFrames[bobNum];
                for (i = 1; i <= 4; ++i)
                {
                    _vm.BankMan.Unpack((uint)i, frameNum, bankNum);
                    ++frameNum;
                }
            }

            // Ensure that the correct buffer frame is selected

            if (isJoe && !_talkHead)
            {
                if (_vm.Logic.JoeFacing == Direction.FRONT ||
                    _vm.Logic.JoeFacing == Direction.BACK)
                {
                    // Joe is facing either Front or Back!
                    //  - Don't FACE_JOE in room 69, because Joe is probably
                    //       holding the Dino Ray gun.
                    if (_vm.Logic.CurrentRoom != 69)
                        _vm.Logic.JoeFace();
                }
                else
                {
                    if (command == SPEAK_DEFAULT ||
                            command == 6 ||
                            command == 7)
                    {
                        _vm.Logic.JoeFace();
                    }
                    else if (command != 5)
                    {
                        // 7/11/94, Ensure that correct mouth closed frame is used!
                        if (parameters.rf != -1)
                            // XXX should really be just "bf", but it is not always calculated... :-(
                            _vm.BankMan.Overpack((uint)parameters.bf, (uint)startFrame, bankNum);

                        if (parameters.ff == 0)
                            _vm.BankMan.Overpack(10, (uint)startFrame, bankNum);
                        else
                            _vm.BankMan.Overpack((uint)parameters.ff, (uint)startFrame, bankNum);
                    }
                }
            }

            _vm.Update();
        }

        private void StringAnimation(SpeechParameters parameters, int startFrame, int bankNum)
        {
            // lines 1639-1690 in talk.c

            int offset = 0;
            bool torso;

            if (parameters.animation[0] == 'T')
            {
                // Torso animation
                torso = true;
                _vm.BankMan.Overpack((uint)parameters.body, (uint)startFrame, (uint)bankNum);
                offset++;
            }
            else if (parameters.animation[0] == 'E')
            {
                // Talking head animation
                return;
            }
            else if (!char.IsDigit(parameters.animation[0]))
            {
                D.Debug(6, $"Error in speak string animation: '{parameters.animation}'");
                return;
            }
            else
                torso = false;

            for (;;)
            {
                ushort frame = ushort.Parse(parameters.animation.Substring(offset, 3));
                if (frame == 0)
                    break;

                offset += 4;

                if (frame > 500)
                {
                    frame -= 500;
                    _vm.Sound.PlaySfx(_vm.Logic.CurrentRoomSfx);
                }

                if (torso)
                {
                    _vm.BankMan.Overpack(frame, (uint)startFrame, (uint)bankNum);
                }
                else
                {
                    _vm.BankMan.Unpack(frame, (uint)startFrame, (uint)bankNum);
                    // XXX bobs[BNUM].scale=SF;
                }

                _vm.Update();
            }
        }

        private void HeadStringAnimation(SpeechParameters parameters, int bobNum, int bankNum)
        {
            // talk.c lines 1612-1635
            BobSlot bob2 = _vm.Graphics.Bobs[2];

            if (parameters.animation.Length > 0 && parameters.animation[0] == 'E')
            {
                int offset = 1;

                BobSlot bob = _vm.Graphics.Bobs[bobNum];
                short x = bob.x;
                short y = bob.y;

                for (;;)
                {
                    ushort frame;

                    frame = ushort.Parse(parameters.animation.Substring(offset, 3));
                    if (frame == 0)
                        break;

                    offset += 4;

                    _vm.BankMan.Unpack(frame, _vm.Graphics.NumFrames, (uint)bankNum);

                    bob2.frameNum = _vm.Graphics.NumFrames;
                    bob2.scale = 100;
                    bob2.active = true;
                    bob2.x = x;
                    bob2.y = y;

                    _vm.Update();
                }
            }
            else
                bob2.active = false;
        }

        /// <summary>
        /// Get special parameters for speech.
        /// </summary>
        /// <returns>The speech parameters.</returns>
        /// <param name="name">Name.</param>
        /// <param name="state">State.</param>
        /// <param name="faceDirection">Face direction.</param>
        private SpeechParameters FindSpeechParameters(string name, int state, Direction faceDirection)
        {
            var iterator = 0;
            if (faceDirection == Direction.RIGHT)
                faceDirection = Direction.LEFT;
            while (_speechParameters[iterator].name[0] != '*')
            {
                if (string.Equals(_speechParameters[iterator].name, name, StringComparison.OrdinalIgnoreCase) &&
                        _speechParameters[iterator].state == state &&
                    _speechParameters[iterator].faceDirection == (sbyte)faceDirection)
                    break;
                iterator++;
            }
            return _speechParameters[iterator];
        }

        private int CountSpaces(string segment)
        {
            int tmp = 0;

            while (tmp < segment.Length)
                tmp++;

            if (tmp < 10)
                tmp = 10;

            return (tmp * 2) / (_vm.TalkSpeed / 3);
        }

        private void DefaultAnimation(string segment, bool isJoe, SpeechParameters parameters, int startFrame, ushort bankNum)
        {
            if (segment.Length > 0)
            {

                // Why on earth would someone name a variable qzx?
                short qzx = 0;

                int len = CountSpaces(segment);
                while (true)
                {
                    if (parameters != null)
                    {

                        int bf;
                        if (segment[0] == ' ')
                            bf = 0;
                        else
                            bf = parameters.bf;

                        int head;
                        if (parameters.rf > 0)
                            head = bf + _vm.Randomizer.Next(1 + parameters.rf);
                        else
                            head = bf;

                        if (bf > 0)
                        {
                            // Make the head move
                            qzx ^= 1;
                            if (parameters.af != 0 && qzx != 0)
                                _vm.BankMan.Overpack((uint)(parameters.af + head), (uint)startFrame, bankNum);
                            else
                            {
                                _vm.BankMan.Overpack((uint)head, (uint)startFrame, bankNum);
                            }
                        }
                        else
                        {
                            D.Debug(6, "[Talk::defaultAnimation] Body action");
                            // Just do a body action
                            _vm.BankMan.Overpack((uint)parameters.body, (uint)startFrame, bankNum);
                        }

                        if (!_talkHead)
                            _vm.Update();
                    }
                    else
                    { // (_talkHead && isJoe)
                        _vm.Update();
                    }

                    if (_vm.Input.TalkQuit)
                        break;

                    if (_vm.Logic.JoeWalk == JoeWalkMode.SPEAK)
                    {
                        _vm.Update();
                    }
                    else
                    {
                        _vm.Update(true);
                        if (_vm.Logic.JoeWalk == JoeWalkMode.EXECUTE)
                            // Selected a command, so exit
                            break;
                    }

                    // Skip through text more quickly
                    if (_vm.Input.KeyVerb == Verb.SKIP_TEXT)
                    {
                        _vm.Input.ClearKeyVerb();
                        _vm.Sound.StopSpeech();
                        break;
                    }

                    if (_vm.Sound.SpeechOn && _vm.Sound.SpeechSfxExists)
                    {
                        // sfx is finished, stop the speak animation
                        if (!_vm.Sound.IsSpeechActive)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // no sfx, stop the animation when speak segment 'length' is 0
                        --len;
                        if (len <= 0)
                        {
                            break;
                        }
                    }
                }
            }

            // Make sure that Person closes their mouth
            if (!isJoe && parameters != null && parameters.ff > 0)
                _vm.BankMan.Overpack((uint)parameters.ff, (uint)startFrame, bankNum);

        }

        static readonly SpeechParameters[] _speechParameters =
        {
        new SpeechParameters("JOE", 0, 1, 1, 10, 2, 3, "", 0),
        new SpeechParameters("JOE", 0, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 0, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 1, 1, 1, 45, -1, 0, "", 0),
        new SpeechParameters("JOE", 1, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 1, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 2, 1, 1, 46, -1, 0, "", 0),
        new SpeechParameters("JOE", 2, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 2, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 3, 1, 1, 47, -1, 0, "", 0),
        new SpeechParameters("JOE", 3, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 3, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 4, 1, 1, 50, -1, 0, "", 0),
        new SpeechParameters("JOE", 4, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 4, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 5, 1, 2, 0, 0, 0, "", 0),
        new SpeechParameters("JOE", 5, 3, 4, 0, 0, 0, "", 0),
        new SpeechParameters("JOE", 5, 4, 6, 0, 0, 0, "", 0),

        new SpeechParameters("JOE", 6, 1, 1, 48, 0, 1, "", 0),
        new SpeechParameters("JOE", 6, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 6, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 7, 1, 1, 51, 0, 1, "", 0),
        new SpeechParameters("JOE", 7, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 7, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 8, 1, 1, 26, 0, 0, "", 0),
        new SpeechParameters("JOE", 8, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 8, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 9, 1, 1, 29, 0, 0, "", 0),
        new SpeechParameters("JOE", 9, 3, 3, 28, 0, 0, "", 0),
        new SpeechParameters("JOE", 9, 4, 5, 38, 0, 0, "", 0),

        new SpeechParameters("JOE", 10, 1, 1, 12, 0, 0,
          "T046,010,010,010,012,012,012,012,012,012,012,012,012,012,012,012,012,012,010,000", 0),
        new SpeechParameters("JOE", 10, 3, 3, 18, 0, 0, "", 0),
        new SpeechParameters("JOE", 10, 4, 5, 44, 0, 0, "", 0),

        new SpeechParameters("JOE", 11, 1, 1, 53, -1, 0, "", 0),
        new SpeechParameters("JOE", 11, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 11, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 12, 1, 1, 10, 2, 3, "", 0),
        new SpeechParameters("JOE", 12, 3, 3, 28, 2, 0, "", 0),
        new SpeechParameters("JOE", 12, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 13, 1, 1, 10, 2, 3, "T012,013,019,019,019,019,019,019,019,019,019,019,013,010,000",
          0),
        new SpeechParameters("JOE", 13, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 13, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 14, 1, 1, 16, 2, 3, "", 16),
        new SpeechParameters("JOE", 14, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 14, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 15, 1, 1, 58, -1, 0, "", 0),
        new SpeechParameters("JOE", 15, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 15, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 16, 1, 1, 59, -1, 0, "", 0),
        new SpeechParameters("JOE", 16, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 16, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 17, 1, 1, 56, -1, 0, "", 0),
        new SpeechParameters("JOE", 17, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 17, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 18, 1, 56, 16, 2, 3, "T056,057,057,057,056,056,000", 0),
        new SpeechParameters("JOE", 18, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 18, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 19, 1, 54, 16, 2, 3, "T054,055,057,056,000", 0),
        new SpeechParameters("JOE", 19, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 19, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 20, 1, 56, 16, 2, 3, "T056,057,055,054,001,000", 0),
        new SpeechParameters("JOE", 20, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 20, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 21, 1, 1, 60, -1, 0, "", 0),
        new SpeechParameters("JOE", 21, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 21, 4, 61, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 22, 1, 1, 16, 2, 3, "T063,060,000", 0),
        new SpeechParameters("JOE", 22, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 22, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 23, 1, 1, 16, 2, 3, "T060,063,001,000", 0),
        new SpeechParameters("JOE", 23, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 23, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("JOE", 24, 1, 1, 47, -2, 0, "", 0),
        new SpeechParameters("JOE", 24, 3, 3, 28, 2, 3, "", 0),
        new SpeechParameters("JOE", 24, 4, 5, 38, 1, 0, "", 0),

        new SpeechParameters("RICO", 0, 0, 1, 7, 1, 3, "", 7),
        new SpeechParameters("RICO", 1, 0, 1, 5, -1, 0, "", 7),
        new SpeechParameters("RICO", 2, 0, 1, 9, 0, 3, "", 7),
        new SpeechParameters("RICO", 3, 0, 1, 4, -1, 0, "", 7),

        new SpeechParameters("EDDY", 0, 0, 14, 18, 1, 3, "", 18),
        new SpeechParameters("EDDY", 1, 0, 14, 0, 0, 0, "T016,017,017,016,016,017,017,016,016,017,017,000", 18),

        new SpeechParameters("MIKE", 0, 0, 1, 2, 2, 3, "", 2),

        new SpeechParameters("LOLA", 0, 0, 30, 33, 2, 3, "", 33),
        new SpeechParameters("LOLA", 1, 0, 9, 10, 2, 3, "", 33),
        new SpeechParameters("LOLA", 2, 0, 30, 33, 2, 3, "", 33),
        new SpeechParameters("LOLA", 3, 0, 32, 33, 2, 3, "", 33),
        new SpeechParameters("LOLA", 4, 0, 8, 0, 0, 0, "", 33),
        new SpeechParameters("LOLA", 5, 0, 31, 0, 0, 0, "", 0),
        new SpeechParameters("LOLA", 6, 0, 31, 0, 0, 0, "047,048,049,050,000", 33),

        new SpeechParameters("LOLA_SHOWER", 0, 0, 7, 10, 2, 3, "", 10),
        new SpeechParameters("LOLA_SHOWER", 1, 0, 9, 10, 2, 3, "", 10),
        new SpeechParameters("LOLA_SHOWER", 2, 0, 30, 33, 2, 3, "", 10),
        new SpeechParameters("LOLA_SHOWER", 3, 0, 32, 33, 2, 3, "", 10),
        new SpeechParameters("LOLA_SHOWER", 4, 0, 8, 0, 0, 0, "", 0),
        new SpeechParameters("LOLA_SHOWER", 5, 0, 61, 0, 0, 0, "", 0),
        new SpeechParameters("LOLA_SHOWER", 6, 0, 64, 10, 2, 3, "", 0),
        new SpeechParameters("LOLA_SHOWER", 7, 0, 31, 0, 0, 0, "062,063,064,000", 0),

        new SpeechParameters("SECRETARY", 0, 0, 1, 12, 2, 3, "", 12),
        new SpeechParameters("SECRETARY", 1, 0, 1, 12, 2, 0, "", 12),
        new SpeechParameters("SECRETARY", 2, 0, 1, 12, 2, 0, "", 12),

        new SpeechParameters("SPARKY", 0, 0, 21, 23, 2, 3, "", 23),
        new SpeechParameters("SPARKY", 1, 0, 21, 22, 0, 0, "", 0),
        new SpeechParameters("SPARKY", 2, 0, 21, 22, 0, 0, "021,042,043,000", 0),
        new SpeechParameters("SPARKY", 3, 0, 21, 22, 0, 0, "043,042,021,000", 0),
        new SpeechParameters("SPARKY", 4, 0, 43, 43, 1, 0, "", 0),
        new SpeechParameters("SPARKY", 14, 0, 21, 29, 5, 0, "", 29),

        new SpeechParameters("SPARKY-F", 0, 0, 45, 23, 5, 0, "", 23),
        new SpeechParameters("SPARKY-F", 1, 0, 45, 47, 0, 0, "", 0),
        new SpeechParameters("SPARKY-F", 2, 0, 45, 23, 5, 0, "045,046,046,045,000", 23),
        new SpeechParameters("SPARKY-F", 14, 0, 45, 29, 5, 0, "", 29),

        new SpeechParameters("FAYE", 0, 0, 19, 35, 2, 3, "", 35),
        new SpeechParameters("FAYE", 1, 0, 19, 41, 2, 3, "", 35),
        new SpeechParameters("FAYE", 2, 0, 19, 28, -1, 0, "", 35),
        new SpeechParameters("FAYE", 3, 0, 19, 20, 0, 0, "", 0),
        new SpeechParameters("FAYE", 4, 0, 19, 27, -1, 0, "", 35),
        new SpeechParameters("FAYE", 5, 0, 19, 29, -1, 0, "", 35),
        new SpeechParameters("FAYE", 6, 0, 59, 35, 2, 3, "", 35),
        new SpeechParameters("FAYE", 7, 0, 19, 30, -1, 0, "", 35),
        new SpeechParameters("FAYE", 8, 0, 19, 31, -1, 0, "", 35),

        new SpeechParameters("BOB", 0, 0, 27, 34, 2, 3, "", 34),
        new SpeechParameters("BOB", 1, 0, 27, 28, -1, 0, "", 34),
        new SpeechParameters("BOB", 2, 0, 30, 0, 0, 0, "", 0),
        new SpeechParameters("BOB", 3, 0, 31, 0, 0, 0, "", 0),
        new SpeechParameters("BOB", 4, 0, 27, 61, -1, 0, "", 34),
        new SpeechParameters("BOB", 5, 0, 27, 42, 1, 0, "", 42),

        new SpeechParameters("PYGMY", 0, 0, 20, 21, 2, 6, "", 0),
        new SpeechParameters("PYGMY", 1, 0, 20, 21, 2, 6, "020,068,068,068,068,068,068,068,068,020,000", 0),
        new SpeechParameters("PYGMY", 2, 0, 20, 21, 2, 6, "T028,029,030,031,031,031,031,030,029,028,035,000", 0),
        new SpeechParameters("PYGMY", 3, 0, 20, 21, 2, 6, "T035,036,037,038,037,038,037,038,036,035,000", 0),
        new SpeechParameters("PYGMY", 4, 0, 20, 21, 2, 6, "T032,033,032,033,032,033,035,000", 0),
        new SpeechParameters("PYGMY", 5, 0, 20, 21, 2, 6, "T023,022,021,022,023,024,025,026,027,026,025,024,023,000", 0),
        new SpeechParameters("PYGMY", 6, 0, 20, 21, 2, 6, "T034,034,034,035,000", 0),

        new SpeechParameters("WITCH", 0, 0, 39, 40, 2, 6, "", 40),
        new SpeechParameters("WITCH", 1, 0, 39, 40, 2, 6, "073,074,000", 40),
        new SpeechParameters("WITCH", 2, 0, 39, 40, 2, 6, "074,073,000", 40),
        new SpeechParameters("WITCH", 3, 0, 39, 40, 2, 6, "T047,048,049,050,051,000", 40),
        new SpeechParameters("WITCH", 4, 0, 39, 40, 2, 6,
          "T052,053,054,055,056,057,058,057,056,056,056,055,054,053,052,000", 40),
        new SpeechParameters("WITCH", 5, 0, 39, 40, 2, 6, "069,070,071,072,073,074,000", 40),
        new SpeechParameters("WITCH", 6, 0, 39, 40, 2, 6, "074,073,072,071,070,069,070,000", 40),
        new SpeechParameters("WITCH", 7, 0, 39, 51, -1, 0, "", 40),
        new SpeechParameters("WITCH", 8, 0, 39, 40, 2, 6, "T051,050,049,048,047,000", 40),

        new SpeechParameters("CHIEF", 0, 0, 1, 7, 1, 7, "", 3),
        new SpeechParameters("CHIEF", 1, 0, 1, 2, 2, 6, "062,063,064,065,066,000", 0),
        new SpeechParameters("CHIEF", 2, 0, 1, 2, 2, 6, "066,065,064,063,062,000", 0),
        new SpeechParameters("CHIEF", 3, 0, 1, 17, -1, 0, "", 3),
        new SpeechParameters("CHIEF", 4, 0, 1, 18, -1, 0, "", 3),
        new SpeechParameters("CHIEF", 5, 0, 1, 19, -1, 0, "", 3),

        new SpeechParameters("NAOMI", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("NAOMI", 1, 0, 1, 2, 2, 6, "048,049,050,051,052,053,054,055,000", 0),
        new SpeechParameters("NAOMI", 2, 0, 1, 2, 2, 6, "055,054,053,052,051,050,049,048,000", 0),
        new SpeechParameters("NAOMI", 3, 0, 1, 13, -1, 0, "", 2),
        new SpeechParameters("NAOMI", 4, 0, 1, 14, -1, 0, "", 2),
        new SpeechParameters("NAOMI", 5, 0, 1, 10, -1, 0, "", 2),
        new SpeechParameters("NAOMI", 6, 0, 1, 12, -1, 0, "", 2),
        new SpeechParameters("NAOMI", 7, 0, 1, 12, -1, 0, "T008,008,008,002,000", 2),

        new SpeechParameters("WEDGEWOOD", 0, 0, 8, 1, 2, 0, "", 8),
        new SpeechParameters("WEDGEWOOD", 1, 0, 1, 1, 3, 0, "", 1),

        new SpeechParameters("BUD", 0, 0, 1, 2, 3, 2, "", 2),
        new SpeechParameters("BUD", 1, 0, 1, 2, 4, 2, "T017,018,000", 2),
        new SpeechParameters("BUD", 2, 0, 1, 21, -1, 0, "", 2),
        new SpeechParameters("BUD", 3, 0, 1, 14, -1, 0, "", 2),
        new SpeechParameters("BUD", 4, 0, 1, 15, -1, 0, "", 2),
        new SpeechParameters("BUD", 5, 0, 1, 20, -1, 0, "", 2),
        new SpeechParameters("BUD", 6, 0, 1, 16, -1, 0, "", 2),
        new SpeechParameters("BUD", 7, 0, 1, 19, -1, 0, "", 2),
        new SpeechParameters("BUD", 8, 0, 1, 17, -1, 0, "", 2),
        new SpeechParameters("BUD", 9, 0, 1, 14, -1, 0, "T014,008,008,003,003,008,008,003,003,010,010,012,012,000", 2),

        new SpeechParameters("LOU", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("LOU", 1, 0, 1, 2, 4, 2, "013,014,015,016,017,018,000", 2),
        new SpeechParameters("LOU", 2, 0, 1, 2, 4, 2, "018,017,016,015,014,013,000", 2),

        new SpeechParameters("JIMMY", 0, 0, 16, 17, 2, 3, "", 17),
        new SpeechParameters("JIMMY", 1, 0, 16, 25, -1, 0, "", 17),
        new SpeechParameters("JIMMY", 2, 0, 16, 26, -1, 0, "", 17),
        new SpeechParameters("JIMMY", 3, 0, 16, 27, -1, 0, "", 17),
        new SpeechParameters("JIMMY", 4, 0, 16, 28, -1, 0, "", 17),
        new SpeechParameters("JIMMY", 5, 0, 16, 29, -1, 0, "", 17),

        new SpeechParameters("TAMMY", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("TAMMY", 1, 0, 1, 2, 2, 3, "T008,008,009,009,008,008,009,009,008,008,009,009,002,000", 2),
        new SpeechParameters("TAMMY", 2, 0, 1, 2, 2, 3, "T002,010,010,010,002,000", 2),
        new SpeechParameters("TAMMY", 3, 0, 1, 2, 2, 3, "T011,011,011,011,011,002,000", 2),
        new SpeechParameters("TAMMY", 4, 0, 1, 2, 2, 3, "T013,014,015,013,014,015,013,009,001,000", 2),

        new SpeechParameters("SKULL", 0, 0, 9, 9, 4, 0, "", 0),
        new SpeechParameters("SKULL", 1, 0, 1, 9, 4, 0, "001,002,003,004,005,006,007,008,009,000", 0),
        new SpeechParameters("SKULL", 2, 0, 1, 9, 4, 0, "009,008,007,006,005,004,003,002,001,000", 0),

        new SpeechParameters("APE", 0, 0, 1, 6, 7, 0, "", 6),
        new SpeechParameters("APE", 1, 0, 1, 6, 7, 0, "002,001,000", 6),
        new SpeechParameters("APE", 2, 0, 1, 6, 7, 0, "002,003,001,000", 6),
        new SpeechParameters("APE", 3, 0, 1, 6, 7, 0, "004,005,004,005,004,001,000", 6),
        new SpeechParameters("APE", 4, 0, 1, 6, 7, 0, "001,003,005,004,005,004,001,000", 6),

        new SpeechParameters("APE1", 0, 0, 15, 16, 7, 0, "", 16),
        new SpeechParameters("APE2", 0, 0, 14, 6, 7, 0, "", 6),

        new SpeechParameters("SHOWERAM", 0, 0, 1, 2, 3, 0, "", 2),
        new SpeechParameters("SHOWERAM", 1, 0, 1, 2, 3, 0, "026,027,028,029,001,000", 2),
        new SpeechParameters("SHOWERAM", 2, 0, 1, 2, 3, 0, "001,029,028,027,026,000", 2),

        new SpeechParameters("PRINCESS1", 0, 0, 19, 23, 2, 3, "", 23),
        new SpeechParameters("PRINCESS1", 1, 0, 19, 41, -1, 0, "", 23),
        new SpeechParameters("PRINCESS1", 2, 0, 19, 42, -1, 0, "", 23),
        new SpeechParameters("PRINCESS1", 3, 0, 19, 45, -1, 0, "", 23),
        new SpeechParameters("PRINCESS1", 4, 0, 19, 40, -1, 0, "", 23),
        new SpeechParameters("PRINCESS1", 5, 0, 19, 45, 2, 3, "T40,043,044,045,000", 45),
        new SpeechParameters("PRINCESS1", 6, 0, 19, 45, -1, 0, "T041,038,000", 38),
        new SpeechParameters("PRINCESS1", 7, 0, 22, 0, 0, 0, "", 0),
        new SpeechParameters("PRINCESS1", 8, 0, 19, 45, 2, 3, "T045,044,043,040,039,000", 39),

        new SpeechParameters("PRINCESS2", 0, 0, 46, 23, 2, 3, "", 23),
        new SpeechParameters("PRINCESS2", 1, 0, 46, 29, 2, 3, "", 29),
        new SpeechParameters("PRINCESS2", 2, 0, 46, 29, 2, 3, "T029,036,035,000", 35),

        new SpeechParameters("GUARDS", 0, 0, 7, 7, 0, 0, "", 7),

        new SpeechParameters("AMGUARD", 0, 0, 19, 22, 2, 3, "", 22),

        new SpeechParameters("MAN1", 0, 0, 2, 3, 2, 3, "", 3),
        new SpeechParameters("MAN2", 0, 0, 9, 10, 1, 2, "", 10),

        new SpeechParameters("DOG", 0, 0, 6, 6, 1, 0, "", 0),
        new SpeechParameters("DOG", 1, 0, 6, 6, 1, 0, "010,009,008,000", 0),
        new SpeechParameters("DOG", 2, 0, 6, 6, 1, 0, "008,009,010,000", 0),

        new SpeechParameters("CHEF", 0, 0, 5, 6, 2, 3, "", 6),

        new SpeechParameters("HENRY", 0, 0, 7, 9, 2, 3, "", 9),
        new SpeechParameters("HENRY", 1, 0, 7, 21, -1, 0, "", 9),
        new SpeechParameters("HENRY", 2, 0, 7, 19, -1, 0, "", 9),
        new SpeechParameters("HENRY", 3, 0, 7, 20, -1, 0, "", 9),
        new SpeechParameters("HENRY", 4, 0, 8, 9, 2, 3, "", 9),
        new SpeechParameters("HENRY", 5, 0, 23, 9, -1, 0, "", 9),
        new SpeechParameters("HENRY", 6, 0, 7, 9, 2, 3, "T019,015,017,017,017,017,017,017,017,015,009,000", 9),
        new SpeechParameters("HENRY", 7, 0, 7, 9, 2, 3, "T018,010,000", 10),
        new SpeechParameters("HENRY", 8, 0, 7, 9, 2, 3, "T018,016,000", 16),
        new SpeechParameters("HENRY", 9, 0, 7, 9, 2, 3, "T018,011,000", 11),
        new SpeechParameters("HENRY", 10, 0, 29, 33, 1, 1, "", 33),
        new SpeechParameters("HENRY", 11, 0, 7, 30, 2, 0, "", 9),
        new SpeechParameters("HENRY", 12, 0, 7, 9, 2, 3, "025,026,000", 26),
        new SpeechParameters("HENRY", 13, 0, 7, 9, 2, 3, "027,028,027,028,000", 28),
        new SpeechParameters("HENRY", 14, 0, 7, 9, 2, 3, "026,025,007,000", 9),

        new SpeechParameters("JOHAN", 0, 0, 1, 15, 2, 3, "", 15),
        new SpeechParameters("JOHAN", 1, 0, 1, 0, 0, 0, "T006,007,008,000", 15),
        new SpeechParameters("JOHAN", 2, 0, 1, 15, 2, 3,
          "T002,003,004,005,004,005,004,005,004,005,004,005,004,003,002,000", 15),
        new SpeechParameters("JOHAN", 3, 0, 1, 8, -1, 0, "", 15),
        new SpeechParameters("JOHAN", 4, 0, 1, 0, 0, 0, "T008,007,006,001,000", 15),

        new SpeechParameters("KLUNK", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("KLUNK", 1, 0, 1, 2, 2, 3, "019,020,021,022,001,000", 2),
        new SpeechParameters("KLUNK", 2, 0, 1, 2, 2, 3, "001,022,021,020,019,016,517,000", 2),
        new SpeechParameters("KLUNK", 3, 0, 1, 2, 2, 3, "T010,011,010,011,010,011,009,000", 2),

        new SpeechParameters("FRANK", 0, 0, 13, 14, 2, 3, "", 14),
        new SpeechParameters("FRANK", 1, 0, 13, 20, 0, 1, "", 14),
        new SpeechParameters("FRANK", 2, 0, 13, 14, 2, 3,
          "025,026,027,027,027,026,026,026,027,027,026,026,027,025,013,000", 14),
        new SpeechParameters("FRANK", 3, 0, 28, 14, 2, 3, "", 14),

        new SpeechParameters("DEATH", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("DEATH", 1, 0, 1, 2, 2, 3, "013,014,015,016,017,001,000", 0),
        new SpeechParameters("DEATH", 2, 0, 1, 2, 2, 3, "001,017,016,015,014,013,000", 0),
        new SpeechParameters("DEATH", 3, 0, 1, 2, 2, 3,
          "T018,019,020,021,021,022,022,020,021,022,020,021,022,023,024,524,000", 2),
        new SpeechParameters("DEATH", 4, 0, 1, 2, 2, 3, "T025,026,027,028,028,028,028,028,028,028,028,028,029,035,000",
          2),
        new SpeechParameters("DEATH", 5, 0, 1, 2, 2, 3, "T030,031,032,033,033,033,033,033,033,033,033,033,034,035,000",
          2),
        new SpeechParameters("DEATH", 6, 0, 1, 2, 2, 3, "T023,022,020,019,018,001,000", 2),

        new SpeechParameters("JASPAR", 0, 0, 1, 1, 22, 0, "026,027,028,029,028,029,028,029,030,023,000", 0),
        new SpeechParameters("JASPAR", 1, 0, 1, 1, 22, 0, "023,026,000", 0),

        new SpeechParameters("ORACLE", 0, 0, 1, 5, 3, 0, "", 0),

        new SpeechParameters("ZOMBIE", 0, 0, 1, 5, 2, 3, "", 5),
        new SpeechParameters("ZOMBIE", 1, 0, 1, 12, -1, 0, "", 5),
        new SpeechParameters("ZOMBIE", 2, 0, 1, 13, -1, 0, "", 5),
        new SpeechParameters("ZOMBIE", 3, 0, 1, 1, 5, 5, "", 5),

        new SpeechParameters("ZOMBIE2", 0, 0, 14, 14, 0, 0, "", 0),
        new SpeechParameters("ZOMBIE3", 0, 0, 18, 18, 0, 0, "", 0),

        new SpeechParameters("ANDERSON", 0, 0, 7, 8, 2, 3, "", 8),
        new SpeechParameters("ANDERSON", 1, 0, 7, 8, 1, 0, "", 8),
        new SpeechParameters("ANDERSON", 2, 0, 7, 16, -1, 0, "", 8),
        new SpeechParameters("ANDERSON", 3, 0, 7, 18, -1, 0, "", 8),
        new SpeechParameters("ANDERSON", 4, 0, 7, 19, -1, 0, "", 8),
        new SpeechParameters("ANDERSON", 5, 0, 7, 20, -1, 0, "", 8),
        new SpeechParameters("ANDERSON", 6, 0, 7, 21, 1, 0, "", 8),

        new SpeechParameters("COMPY", 0, 0, 12, 12, -1, 0, "", 0),
        new SpeechParameters("COMPY", 1, 0, 10, 10, 10, 0, "010,011,012,012,013,014,014,000", 0),
        new SpeechParameters("COMPY", 2, 0, 10, 10, 10, 0, "014,013,012,000", 0),

        new SpeechParameters("DEINO", 0, 0, 13, 13, -1, 0, "", 0),
        new SpeechParameters("DEINO", 1, 0, 9, 9, 9, 0, "009,010,000", 0),

        new SpeechParameters("TMPD", 0, 0, 19, 22, 2, 3, "", 22),

        new SpeechParameters("IAN", 0, 0, 7, 9, 2, 3, "", 9),
        new SpeechParameters("IAN", 1, 0, 8, 25, 3, 0, "", 25),
        new SpeechParameters("IAN", 2, 0, 7, 21, -1, 0, "", 9),
        new SpeechParameters("IAN", 3, 0, 7, 22, 1, 0, "", 9),
        new SpeechParameters("IAN", 4, 0, 7, 22, -1, 0, "", 9),
        new SpeechParameters("IAN", 5, 0, 7, 24, -1, 0, "", 9),
        new SpeechParameters("IAN", 6, 0, 7, 9, 2, 3, "034,034,034,035,035,036,036,035,035,036,035,036,035,000", 9),
        new SpeechParameters("IAN", 7, 0, 7, 31, -1, 0, "", 9),

        new SpeechParameters("FAYE-H", 0, 0, 1, 1, 4, 1, "", 1),
        new SpeechParameters("FAYE-H", 1, 0, 1, 1, 4, 1, "007,000", 7),
        new SpeechParameters("FAYE-H", 2, 0, 1, 1, 4, 1, "009,010,011,009,001,000", 1),
        new SpeechParameters("FAYE-H", 3, 0, 1, 1, 4, 1, "E012,013,000", 1),
        new SpeechParameters("FAYE-H", 4, 0, 1, 1, 4, 1, "E015,000", 1),
        new SpeechParameters("FAYE-H", 5, 0, 1, 1, 4, 1, "E014,000", 1),

        new SpeechParameters("AZURA-H", 0, 0, 1, 1, 4, 1, "", 1),
        new SpeechParameters("AZURA-H", 1, 0, 1, 1, 4, 1, "007,000", 7),
        new SpeechParameters("AZURA-H", 2, 0, 1, 1, 4, 1, "009,010,011,009,001,000", 1),
        new SpeechParameters("AZURA-H", 3, 0, 1, 1, 4, 1, "E012,013,000", 1),
        new SpeechParameters("AZURA-H", 4, 0, 1, 1, 4, 1, "E015,000", 1),
        new SpeechParameters("AZURA-H", 5, 0, 1, 1, 4, 1, "E014,000", 1),

        new SpeechParameters("FRANK-H", 0, 0, 1, 1, 4, 1, "", 1),
        new SpeechParameters("FRANK-H", 1, 0, 1, 1, 4, 1, "E009,000", 1),
        new SpeechParameters("FRANK-H", 2, 0, 1, 1, 4, 1, "E007,000", 1),
        new SpeechParameters("FRANK-H", 3, 0, 1, 1, 4, 1, "010,011,012,013,014,015,010,000", 1),

        new SpeechParameters("JOE-E", 0, 0, 1, 2, 4, 1, "", 2),
        new SpeechParameters("JOE-E", 6, 0, 1, 2, 4, 1, "008,009,008,002,000", 2),

        new SpeechParameters("AZURA-E", 0, 0, 1, 1, 5, 1, "", 1),
        new SpeechParameters("AZURA-E", 1, 0, 1, 1, 5, 1, "009,010,009,008,000", 1),

        new SpeechParameters("FAYE-E", 0, 0, 1, 4, 4, 1, "", 1),
        new SpeechParameters("FAYE-E", 1, 0, 1, 4, 4, 1, "002,003,002,001,000", 1),

        new SpeechParameters("ANDSON-E", 0, 0, 1, 3, 4, 1, "", 1),
        new SpeechParameters("ANDSON-E", 1, 0, 1, 3, 4, 1, "002,001,000", 1),

        new SpeechParameters("JOE-H", 0, 0, 1, 1, 4, 4, "", 1),
        new SpeechParameters("JOE-H", 1, 0, 1, 1, 2, 3, "012,013,014,000", 14),
        new SpeechParameters("JOE-H", 2, 0, 1, 1, 2, 3, "010,011,000", 11),
        new SpeechParameters("JOE-H", 3, 0, 1, 1, 2, 3, "014,013,012,001,000", 1),
        new SpeechParameters("JOE-H", 4, 0, 1, 13, 1, 0, "", 13),

        new SpeechParameters("RITA-H", 0, 0, 7, 1, 2, 3, "", 1),
        new SpeechParameters("RITA-H", 1, 0, 7, 0, 0, 0, "009,010,011,012,013,000", 13),
        new SpeechParameters("RITA-H", 2, 0, 7, 0, 0, 0, "014,015,016,000", 16),
        new SpeechParameters("RITA-H", 3, 0, 7, 0, 0, 0, "013,012,011,010,000", 10),
        new SpeechParameters("RITA-H", 4, 0, 7, 0, 0, 0, "009,007,008,007,009,000", 9),
        new SpeechParameters("RITA-H", 5, 0, 7, 0, 0, 0, "016,015,014,000", 14),

        new SpeechParameters("RITA", 0, 0, 1, 4, 2, 3, "", 4),
        new SpeechParameters("RITA", 1, 0, 2, 4, 2, 3, "", 4),

        new SpeechParameters("SPARKY-H", 0, 0, 1, 1, 2, 3, "", 1),

        new SpeechParameters("HUGH", 0, 0, 1, 1, 2, 3, "", 1),
        new SpeechParameters("HUGH", 1, 0, 7, 7, 2, 3, "", 7),

        new SpeechParameters("X2_JOE", 0, 0, 1, 1, 2, 3, "", 1),
        new SpeechParameters("X2_JOE", 1, 0, 1, 1, 2, 3, "001,007,008,008,007,001,000", 1),

        new SpeechParameters("X2_RITA", 0, 0, 1, 1, 2, 3, "", 1),
        new SpeechParameters("X2_RITA", 1, 0, 1, 1, 2, 3, "001,007,008,008,007,001,000", 1),

        new SpeechParameters("X3_RITA", 0, 0, 1, 1, 4, 1, "", 1),
        new SpeechParameters("X3_RITA", 1, 0, 1, 1, 4, 1, "007,000", 7),
        new SpeechParameters("X3_RITA", 2, 0, 1, 1, 4, 1, "009,010,011,009,001,000", 1),
        new SpeechParameters("X3_RITA", 3, 0, 1, 1, 4, 1, "E012,013,000", 1),
        new SpeechParameters("X3_RITA", 4, 0, 1, 1, 4, 1, "E015,000", 1),
        new SpeechParameters("X3_RITA", 5, 0, 1, 1, 4, 1, "E014,000", 1),

        new SpeechParameters("X4_JOE", 0, 0, 1, 1, 3, 4, "", 1),
        new SpeechParameters("X4_JOE", 1, 0, 1, 13, 2, 3, "", 13),
        new SpeechParameters("X4_JOE", 2, 0, 1, 1, 3, 4, "009,010,011,012,013,000", 13),
        new SpeechParameters("X4_JOE", 3, 0, 1, 1, 3, 4, "012,011,010,009,000", 9),
        new SpeechParameters("X4_JOE", 4, 0, 1, 1, 3, 4, "001,019,000", 19),

        new SpeechParameters("X4_RITA", 0, 0, 1, 1, 0, 1, "", 1),
        new SpeechParameters("X4_RITA", 1, 0, 1, 7, 0, 1, "", 7),
        new SpeechParameters("X4_RITA", 2, 0, 1, 1, 3, 4, "004,005,006,006,006,006,007,000", 7),
        new SpeechParameters("X4_RITA", 3, 0, 1, 1, 3, 4, "005,004,001,000", 1),
        new SpeechParameters("X4_RITA", 4, 0, 1, 1, 3, 4, "001,003,000", 3),

        new SpeechParameters("X5_SPARKY", 0, 0, 1, 1, 2, 3, "", 1),
        new SpeechParameters("X5_SPARKY", 1, 0, 1, 1, 2, 3, "001,010,011,011,001,000", 1),
        new SpeechParameters("X5_SPARKY", 2, 0, 1, 1, 2, 3, "001,007,008,009,000", 9),

        new SpeechParameters("X6_HUGH", 0, 0, 1, 1, 2, 3, "", 1),
        new SpeechParameters("X6_HUGH", 1, 0, 1, 1, 2, 3, "007,007,007,007,,001,000", 1),
        new SpeechParameters("X6_HUGH", 2, 0, 1, 1, 2, 3, "008,008,008,008,008,009,009,008,008,008,009,008,000", 8),

        new SpeechParameters("X10_JOE", 0, 0, 1, 2, 2, 3, "", 2),
        new SpeechParameters("X10_JOE", 1, 0, 1, 8, 2, 3, "", 8),
        new SpeechParameters("X10_JOE", 2, 0, 1, 2, 2, 3, "014,014,014,015,015,014,014,015,015,000", 2),

        new SpeechParameters("X10_RITA", 0, 0, 1, 2, 2, 3, "", 2),

        new SpeechParameters("X11_JOE", 0, 0, 1, 2, 0, 1, "", 2),

        new SpeechParameters("X11_RITA", 0, 0, 1, 2, 0, 1, "", 2),
        new SpeechParameters("X11_RITA", 1, 0, 1, 2, 1, 0, "003,004,000", 4),

        new SpeechParameters("JOHN", 0, 0, 1, 2, 2, 3, "", 1),
        new SpeechParameters("JOHN", 1, 0, 1, 15, -1, 0, "", 1),
        new SpeechParameters("JOHN", 2, 0, 1, 16, -1, 0, "", 1),
        new SpeechParameters("JOHN", 3, 0, 1, 17, -1, 0, "", 1),

        new SpeechParameters("STEVE", 0, 0, 8, 2, 2, 3, "", 2),
        new SpeechParameters("STEVE", 1, 0, 8, 16, -1, 0, "", 2),
        new SpeechParameters("STEVE", 2, 0, 9, 18, -1, 0, "T016,017,017,016,008,000", 2),
        new SpeechParameters("STEVE", 3, 0, 8, 18, -1, 0, "", 2),

        new SpeechParameters("TONY", 0, 0, 1, 2, 2, 3, "", 1),
        new SpeechParameters("TONY", 1, 0, 1, 12, -1, 0, "", 1),

        new SpeechParameters("*", 0, 0, 0, 0, 0, 0, "", 0)
      };
    }
}
