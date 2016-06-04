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
using NScumm.Core;
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

        QueenEngine _vm;
        bool _talkHead;

        private Talk(QueenEngine vm)
        {
            _vm = vm;
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
            // TODO: assert((align & 1) == 0);
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

            _vm.Logic.JoeWalk(JoeWalkMode.SPEAK);

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

            if (sentence[0] == '\0')
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
                            sentence + segmentStart,
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
                    sentence + segmentStart,
                    i - segmentStart,
                    person,
                    0,
                    voiceFilePrefix,
                    segmentIndex);
            }

            return personWalking;
        }

        int GetSpeakCommand(Person person, string sentence, ref ushort index)
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
                        commandCode = int.Parse(sentence + index + 2);
                        int x = int.Parse(sentence + index + 5);
                        int y = int.Parse(sentence + index + 9);
                        if (string.Equals(person.name, "JOE"))
                            _vm.Walk.MoveJoe(0, (ushort)x, (ushort)y, _vm.Input.CutawayRunning);
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
                        // TODO: warning ("Unknown command string: '%2s'", sentence + index);
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

        void DoTalk(string filename, int personInRoom, out string cutawayFilename)
        {
            throw new NotImplementedException();
        }

        void SpeakSegment(
            string segmentStart,
            int length,
            Person person,
            int command,
            string voiceFilePrefix,
            int index)
        {
            //TODO:
        }
    }
}
