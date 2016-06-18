//
//  QueenEngine.cs
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
using System.Linq;
using D = NScumm.Core.DebugHelper;
using NScumm.Core.Graphics;

namespace NScumm.Queen
{
    public class Input
    {
        public const int DELAY_SHORT = 10;
        public const int DELAY_NORMAL = 100;
        public const int DELAY_SCREEN_BLANKER = 5 * 60 * 1000;

        public const int MOUSE_LBUTTON = 1;
        public const int MOUSE_RBUTTON = 2;

        private string _currentCommandKeys;

        ISystem _system;

        bool _quickSave;
        bool _quickLoad;
        bool _dialogueRunning;
        bool _talkQuit;

        /// <summary>
        /// the current verb received from keyboard.
        /// </summary>
        Verb _keyVerb;
        KeyCode _inKey;

        int _mouseButton;

        private static readonly string[] _commandKeys = {
            "ocmglptu", // English
			"osbgpnre", // German
			"ofdnepau", // French
			"acsdgpqu", // Italian
			"ocmglptu", // Hebrew
			"acodmthu"  // Spanish
		};

        private static readonly Verb[] _verbKeys = {
            Verb.OPEN,
            Verb.CLOSE,
            Verb.MOVE,
            Verb.GIVE,
            Verb.LOOK_AT,
            Verb.PICK_UP,
            Verb.TALK_TO,
            Verb.USE
        };

        public bool CutawayQuit { get; private set; }

        public bool CanQuit { get; set; }

        public bool TalkQuit { get { return _talkQuit; } }

        /// <summary>
        /// Gets user idle time.
        /// </summary>
        public int IdleTime { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a cutaway is running.
        /// </summary>
        /// <value><c>true</c> if a cutaway is running; otherwise, <c>false</c>.</value>
        public bool CutawayRunning { get; set; }

        /// <summary>
        /// Some cutaways require update() run faster.
        /// </summary>
        public bool FastMode { get; set; }

        public Verb KeyVerb { get { return _keyVerb; } }

        public Point MousePos { get { return _system.InputManager.GetMousePosition(); } }

        public int MouseButton { get { return _mouseButton; } }

        public bool QuickSave { get { return _quickSave; } }

        public bool QuickLoad { get { return _quickLoad; } }

        public Input(Language language, ISystem system)
        {
            _system = system;
            switch (language)
            {
                case Language.EN_ANY:
                case Language.GR_GRE:
                case Language.RU_RUS:
                    _currentCommandKeys = _commandKeys[0];
                    break;
                case Language.DE_DEU:
                    _currentCommandKeys = _commandKeys[1];
                    break;
                case Language.FR_FRA:
                    _currentCommandKeys = _commandKeys[2];
                    break;
                case Language.IT_ITA:
                    _currentCommandKeys = _commandKeys[3];
                    break;
                case Language.HE_ISR:
                    _currentCommandKeys = _commandKeys[4];
                    break;
                case Language.ES_ESP:
                    _currentCommandKeys = _commandKeys[5];
                    break;
                default:
                    throw new InvalidOperationException("Unknown language");
            }
        }

        public void ClearMouseButton() { _mouseButton = 0; }

        public void DialogueRunning(bool running) { _dialogueRunning = running; }

        public void QuickSaveReset()
        {
            _quickSave = false;
        }

        public void QuickLoadReset()
        {
            _quickLoad = false;
        }

        public void CutawayQuitReset()
        {
            CutawayQuit = false;
        }

        public void CheckKeys()
        {
            if (_inKey != KeyCode.None)
                D.Debug(6, $"[Input::checkKeys] _inKey = {_inKey}");

            switch (_inKey)
            {
                case KeyCode.Space:
                    _keyVerb = Verb.SKIP_TEXT;
                    break;
                case KeyCode.Comma:
                    _keyVerb = Verb.SCROLL_UP;
                    break;
                case KeyCode.OemPeriod:
                    _keyVerb = Verb.SCROLL_DOWN;
                    break;
                case KeyCode.D1:
                    _keyVerb = Verb.DIGIT_1;
                    break;
                case KeyCode.D2:
                    _keyVerb = Verb.DIGIT_2;
                    break;
                case KeyCode.D3:
                    _keyVerb = Verb.DIGIT_3;
                    break;
                case KeyCode.D4:
                    _keyVerb = Verb.DIGIT_4;
                    break;
                case KeyCode.Escape: // skip cutaway / dialogue
                    if (CanQuit)
                    {
                        if (CutawayRunning)
                        {
                            D.Debug(6, "[Input::checkKeys] Setting _cutawayQuit to true");
                            CutawayQuit = true;
                        }
                        if (_dialogueRunning)
                            _talkQuit = true;
                    }
                    break;
                case KeyCode.F1: // use Journal
                case KeyCode.F5:
                    if (CutawayRunning)
                    {
                        if (CanQuit)
                        {
                            _keyVerb = Verb.USE_JOURNAL;
                            CutawayQuit = _talkQuit = true;
                        }
                    }
                    else
                    {
                        _keyVerb = Verb.USE_JOURNAL;
                        if (CanQuit)
                            _talkQuit = true;
                    }
                    break;
                case KeyCode.F11: // quicksave
                    _quickSave = true;
                    break;
                case KeyCode.F12: // quickload
                    _quickLoad = true;
                    break;
                default:
                    for (int i = 0; i < _verbKeys.Length; ++i)
                    {
                        if (ToChar(_inKey) == _currentCommandKeys[i])
                        {
                            _keyVerb = _verbKeys[i];
                            break;
                        }
                    }
                    break;
            }

            _inKey = KeyCode.None;   // reset
        }

        public void TalkQuitReset()
        {
            _talkQuit = false;
        }

        public static bool IsLetterOrDigit(KeyCode key)
        {
            return (key >= KeyCode.A && key <= KeyCode.Z) ||
               (key >= KeyCode.D0 && key <= KeyCode.D0);
        }

        public static char ToChar(KeyCode key)
        {
            return (char)('a' + (int)(key - KeyCode.A));
        }

        public void Delay(int amount)
        {
            if (FastMode && amount > DELAY_SHORT)
            {
                amount = DELAY_SHORT;
            }
            if (IdleTime < DELAY_SCREEN_BLANKER)
            {
                IdleTime += amount;
            }
            var end = Environment.TickCount + amount;
            do
            {
                var state = _system.InputManager.GetState();
                var keys = state.GetKeys();
                IdleTime = 0;

                if (keys.Count > 0)
                {
                    if (keys.Contains(KeyCode.LeftControl))
                    {
                        if (keys.Contains(KeyCode.D))
                        {
                            //_debugger = true;
                        }
                        else if (keys.Contains(KeyCode.F))
                        {
                            FastMode = !FastMode;
                        }
                    }
                    else
                    {
                        _inKey = keys.First();
                    }

                    if (keys.Contains(KeyCode.Escape) && CutawayRunning)
                        CutawayQuit = true;
                }

                if (state.IsLeftButtonDown)
                    _mouseButton |= MOUSE_LBUTTON;

                if (state.IsRightButtonDown)
                    _mouseButton |= MOUSE_RBUTTON;

                _system.InputManager.ResetKeys();

                _system.GraphicsManager.UpdateScreen();

                if (amount == 0)
                    break;

                ServiceLocator.Platform.Sleep((amount > 10) ? 10 : amount);
            } while (Environment.TickCount < end);
        }

        public void ClearKeyVerb()
        {
            _keyVerb = Verb.NONE;
        }
    }
}

