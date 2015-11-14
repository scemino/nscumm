//
//  ScummEngine0_Verb.cs
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

using System.Linq;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine0
    {
        void ResetVerbs()
        {
            var virt = VerbVirtScreen;

            VerbSettings[] vtable;

            switch (Game.Culture.TwoLetterISOLanguageName)
            {
                case "de":
                    vtable = v0VerbTable_German;
                    break;
                default:
                    vtable = v0VerbTable_English;
                    break;
            }

            for (var i = 1; i < 16; i++)
                KillVerb(i);

            for (var i = 1; i < 16; i++)
            {
                var vs = Verbs[i];
                vs.VerbId = (ushort)vtable[i - 1].id;
                vs.Color = 5;
                vs.HiColor = 7;
                vs.DimColor = 11;
                vs.Type = VerbType.Text;
                vs.CharsetNr = String[0].Default.Charset;
                vs.CurMode = 1;
                vs.SaveId = 0;
                vs.Key = 0;
                vs.Center = false;
                vs.ImgIndex = 0;
                vs.Prep = (byte)VerbPrepIdType(vtable[i - 1].id);
                vs.CurRect.Left = vtable[i - 1].x_pos * 8;
                vs.CurRect.Top = vtable[i - 1].y_pos * 8 + virt.TopLine + 8;
                vs.Text = System.Text.Encoding.UTF8.GetBytes(vtable[i - 1].name);
            }
        }

        VerbPrepsV0 GetVerbPrepId()
        {
            if (Verbs[(int)_activeVerb].Prep != 0xFF)
            {
                return (VerbPrepsV0)Verbs[(int)_activeVerb].Prep;
            }
            else
            {
                var obj = _invData.Concat(_objs).First(o => o.Number == _activeObject);
                return (VerbPrepsV0)obj.Preposition;
            }
        }

        VerbPrepsV0 ActiveVerbPrep()
        {
            if (_activeVerb == VerbsV0.None || _activeObject == 0)
                return 0;
            return GetVerbPrepId();
        }

        VerbPrepsV0 VerbPrepIdType(VerbsV0 verbid)
        {
            switch (verbid)
            {
                case VerbsV0.Use: // depends on object1
                    return VerbPrepsV0.Object;
                case VerbsV0.Give:
                    return VerbPrepsV0.To;
                case VerbsV0.Unlock:
                case VerbsV0.Fix:
                    return VerbPrepsV0.With;
                default:
                    return VerbPrepsV0.None;
            }
        }

        void VerbExec()
        {
            SentenceNum = 0;
            _sentenceNestedCount = 0;

            if (_activeVerb == VerbsV0.WhatIs)
                return;

            if (!(_activeVerb == VerbsV0.WalkTo && _activeObject == 0))
            {
                DoSentence((byte)_activeVerb, (ushort)_activeObject, (ushort)_activeObject2);
                if (_activeVerb != VerbsV0.WalkTo)
                {
                    _activeVerb = VerbsV0.WalkTo;
                    _activeObject = 0;
                    _activeObject2 = 0;
                }
                _walkToObjectState = WalkToObjectState.Done;
                return;
            }

            var a = (Actor0)Actors[Variables[VariableEgo.Value]];
            int x = (_mousePos.X + MainVirtScreen.XStart) / Actor2.V12_X_MULTIPLIER;
            int y = (_mousePos.Y - MainVirtScreen.TopLine) / Actor2.V12_Y_MULTIPLIER;

            // 0xB31
            Variables[6] = x;
            Variables[7] = y;

            if (a.MiscFlags.HasFlag(ActorV0MiscFlags.Freeze))
                return;

            a.StartWalk(new Point(Variables[6], Variables[7]), -1);
        }

        protected override void CheckExecVerbs()
        {
            var a = (Actor0)Actors[Variables[VariableEgo.Value]];
            var zone = FindVirtScreen(_mousePos.Y);

            bool execute = false;

            if ((((ScummMouseButtonState)mouseAndKeyboardStat) & ScummMouseButtonState.MouseMask) != 0)
            {
                var over = (VerbsV0)FindVerbAtPos(_mousePos);
                // click region: verbs
                if (over != 0)
                {
                    if (_activeVerb != over)
                    { // new verb
                        // keep first object if no preposition is used yet
                        if (ActiveVerbPrep() != 0)
                            _activeObject = 0;
                        _activeObject2 = 0;
                        _activeVerb = over;
                        _redrawSentenceLine = true;
                    }
                    else
                    {
                        // execute sentence if complete
                        if (CheckSentenceComplete())
                            execute = true;
                    }
                }
            }

            if (a.MiscFlags.HasFlag(ActorV0MiscFlags.Hide))
            {
                if (_activeVerb != VerbsV0.NewKid)
                {
                    _activeVerb = VerbsV0.None;
                }
            }

            if (_currentMode != Engine0Mode.Cutscene)
            {
                if (_currentMode == Engine0Mode.Keypad)
                {
                    _activeVerb = VerbsV0.Push;
                }

                if (mouseAndKeyboardStat > 0 && ((ScummMouseButtonState)mouseAndKeyboardStat) < ScummMouseButtonState.MaxKey)
                {
                    // keys already checked by input handler
                }
                else if ((((ScummMouseButtonState)mouseAndKeyboardStat) & ScummMouseButtonState.MouseMask) != 0 || _activeVerb == VerbsV0.WhatIs)
                {
                    // click region: sentence line
                    if (zone == VerbVirtScreen && _mousePos.Y <= zone.TopLine + 8)
                    {
                        if (_activeVerb == VerbsV0.NewKid)
                        {
                            if (_currentMode == Engine0Mode.Normal)
                            {
                                int kid;
                                int lineX = _mousePos.X >> V12_X_SHIFT;
                                if (lineX < 11)
                                    kid = 0;
                                else if (lineX < 25)
                                    kid = 1;
                                else
                                    kid = 2;
                                _activeVerb = VerbsV0.WalkTo;
                                _redrawSentenceLine = true;
                                DrawSentenceLine();
                                SwitchActor(kid);
                            }
                            _activeVerb = VerbsV0.WalkTo;
                            _redrawSentenceLine = true;
                            return;
                        }
                        else
                        {
                            // execute sentence if complete
                            if (CheckSentenceComplete())
                                execute = true;
                        }
                        // click region: inventory or main screen
                    }
                    else if ((zone == VerbVirtScreen && _mousePos.Y > zone.TopLine + 32) ||
                        (zone == MainVirtScreen))
                    {
                        int obj = 0;

                        // click region: inventory
                        if (zone == VerbVirtScreen && _mousePos.Y > zone.TopLine + 32)
                        {
                            // click into inventory
                            int invOff = _inventoryOffset;
                            obj = CheckV2Inventory(_mousePos.X, _mousePos.Y);
                            if (invOff != _inventoryOffset)
                            {
                                // inventory position changed (arrows pressed, do nothing)
                                return;
                            }
                            // the second object of a give-to command has to be an actor
                            if (_activeVerb == VerbsV0.Give && _activeObject != 0)
                                obj = 0;
                            // click region: main screen
                        }
                        else if (zone == MainVirtScreen)
                        {
                            int x = _mousePos.X + MainVirtScreen.XStart;
                            int y = _mousePos.Y - MainVirtScreen.TopLine;
                            // click into main screen
                            if (_activeVerb == VerbsV0.Give && _activeObject != 0)
                            {
                                int actor = GetActorFromPos(new Point(x, y));
                                if (actor != 0)
                                    obj = OBJECT_V0(actor, ObjectV0Type.Actor);
                            }
                            else
                            {
                                obj = FindObjectCore(x, y);
                            }
                        }

                        if (obj == 0)
                        {
                            if (_activeVerb == VerbsV0.WalkTo)
                            {
                                _activeObject = 0;
                                _activeObject2 = 0;
                            }
                        }
                        else
                        {
                            if (ActiveVerbPrep() == VerbPrepsV0.None)
                            {
                                if (obj == _activeObject)
                                    execute = true;
                                else
                                    _activeObject = obj;
                                // immediately execute action in keypad/selection mode
                                if (_currentMode == Engine0Mode.Keypad)
                                    execute = true;
                            }
                            else
                            {
                                if (obj == _activeObject2)
                                    execute = true;
                                if (obj != _activeObject)
                                {
                                    _activeObject2 = obj;
                                    if (_currentMode == Engine0Mode.Keypad)
                                        execute = true;
                                }
                            }
                        }

                        _redrawSentenceLine = true;
                        if (_activeVerb == VerbsV0.WalkTo && zone == MainVirtScreen)
                        {
                            _walkToObjectState = WalkToObjectState.Done;
                            execute = true;
                        }
                    }
                }
            }

            if (_drawDemo && Game.Features.HasFlag(GameFeatures.Demo))
            {
                VerbDemoMode();
            }

            if (_redrawSentenceLine)
                DrawSentenceLine();

            if (!execute || _activeVerb == 0)
                return;

            if (_activeVerb == VerbsV0.WalkTo)
                VerbExec();
            else if (_activeObject != 0)
            {
                // execute if we have a 1st object and either have or do not need a 2nd
                if (ActiveVerbPrep() == VerbPrepsV0.None || _activeObject2 != 0)
                    VerbExec();
            }
        }

        private void VerbDemoMode()
        {
            for (var i = 1; i < 16; i++)
                KillVerb(i);

            for (var i = 0; i < 6; i++)
            {
                VerbDrawDemoString(i);
            }
        }

        void VerbDrawDemoString(int VerbDemoNumber)
        {
            var str = new byte[80];
            var ptr = v0DemoStr[VerbDemoNumber].Text;
            int i = 0, len = 0;

            // Maximum length of printable characters
            int maxChars = 40;
            foreach (var p in ptr)
            {
                if (p != '@')
                    len++;
                if (len > maxChars)
                {
                    break;
                }

                str[i++] = (byte)p;
            }
            str[i] = 0;

            String[2].Charset = 1;
            String[2].Position = new Point(0, VerbVirtScreen.TopLine + (8 * VerbDemoNumber));
            String[2].Right = (short)(VerbVirtScreen.Width - 1);
            String[2].Color = (byte)v0DemoStr[VerbDemoNumber].Color;
            DrawString(2, str);
        }

        class VerbDemo
        {
            public int Color { get; private set; }
            public string Text { get; private set; }

            public VerbDemo(int color, string str)
            {
                Color = color;
                Text = str;
            }
        }

        static readonly VerbDemo[] v0DemoStr = new VerbDemo[]
        {
            new VerbDemo(7,  "        MANIAC MANSION DEMO DISK        "),
            new VerbDemo(5,  "          from Lucasfilm Games          "),
            new VerbDemo(5,  "    Copyright = 1987 by Lucasfilm Ltd.  "),
            new VerbDemo(5,  "           All Rights Reserved.         "),
            new VerbDemo(0,  "                                        "),
            new VerbDemo(16, "       Press F7 to return to menu.      ")
        };

        struct VerbSettings
        {
            public VerbsV0 id;
            public int x_pos;
            public int y_pos;
            public string name;

            public VerbSettings(VerbsV0 id, int x_pos, int y_pos, string name)
            {
                this.id = id;
                this.x_pos = x_pos;
                this.y_pos = y_pos;
                this.name = name;
            }
        }

        static readonly VerbSettings[] v0VerbTable_English =
            {
                new VerbSettings(VerbsV0.Open, 8, 0, "Open"),
                new VerbSettings(VerbsV0.Close, 8, 1, "Close"),
                new VerbSettings(VerbsV0.Give, 0, 2, "Give"),
                new VerbSettings(VerbsV0.TurnOn, 32, 0, "Turn on"),
                new VerbSettings(VerbsV0.TurnOff, 32, 1, "Turn off"),
                new VerbSettings(VerbsV0.Fix, 32, 2, "Fix"),
                new VerbSettings(VerbsV0.NewKid, 24, 0, "New Kid"),
                new VerbSettings(VerbsV0.Unlock, 24, 1, "Unlock"),
                new VerbSettings(VerbsV0.Push, 0, 0, "Push"),
                new VerbSettings(VerbsV0.Pull, 0, 1, "Pull"),
                new VerbSettings(VerbsV0.Use, 24, 2, "Use"),
                new VerbSettings(VerbsV0.Read, 8, 2, "Read"),
                new VerbSettings(VerbsV0.WalkTo, 15, 0, "Walk to"),
                new VerbSettings(VerbsV0.PickUp, 15, 1, "Pick up"),
                new VerbSettings(VerbsV0.WhatIs, 15, 2, "What is")
            };

        static readonly VerbSettings[] v0VerbTable_German =
            {
                new VerbSettings(VerbsV0.Open, 7, 0, "$ffne"),
                new VerbSettings(VerbsV0.Close, 13, 1, "Schlie*e"),
                new VerbSettings(VerbsV0.Give, 0, 2, "Gebe"),
                new VerbSettings(VerbsV0.TurnOn, 37, 1, "Ein"),
                new VerbSettings(VerbsV0.TurnOff, 37, 0, "Aus"),
                new VerbSettings(VerbsV0.Fix, 23, 1, "Repariere"),
                new VerbSettings(VerbsV0.NewKid, 34, 2, "Person"),
                new VerbSettings(VerbsV0.Unlock, 23, 0, "Schlie*e auf"),
                new VerbSettings(VerbsV0.Push, 0, 0, "Dr<cke"),
                new VerbSettings(VerbsV0.Pull, 0, 1, "Ziehe"),
                new VerbSettings(VerbsV0.Use, 23, 2, "Benutz"),
                new VerbSettings(VerbsV0.Read, 7, 2, "Lese"),
                new VerbSettings(VerbsV0.WalkTo, 13, 0, "Gehe zu"),
                new VerbSettings(VerbsV0.PickUp, 7, 1, "Nimm"),
                new VerbSettings(VerbsV0.WhatIs, 13, 2, "Was ist")
            };
    }
}

