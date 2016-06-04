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
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;

namespace NScumm.Queen
{
	public class Input
	{
		public const int DELAY_SHORT = 10;
		public const int DELAY_NORMAL = 100;
		public const int DELAY_SCREEN_BLANKER = 5 * 60 * 1000;

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

		private static readonly string[] _commandKeys = {
			"ocmglptu", // English
			"osbgpnre", // German
			"ofdnepau", // French
			"acsdgpqu", // Italian
			"ocmglptu", // Hebrew
			"acodmthu"  // Spanish
		};

		public bool CutawayQuit { get; private set;}

		public bool CanQuit { get; set; }

		public bool TalkQuit { get{ return _talkQuit; } }

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

        public Verb KeyVerb { get { return _keyVerb;} }

        public Input (Language language, ISystem system)
		{
			_system = system;
			switch (language) {
			case Language.EN_ANY:
			case Language.GR_GRE:
			case Language.RU_RUS:
				_currentCommandKeys = _commandKeys [0];
				break;
			case Language.DE_DEU:
				_currentCommandKeys = _commandKeys [1];
				break;
			case Language.FR_FRA:
				_currentCommandKeys = _commandKeys [2];
				break;
			case Language.IT_ITA:
				_currentCommandKeys = _commandKeys [3];
				break;
			case Language.HE_ISR:
				_currentCommandKeys = _commandKeys [4];
				break;
			case Language.ES_ESP:
				_currentCommandKeys = _commandKeys [5];
				break;
			default:
				throw new InvalidOperationException ("Unknown language");
			}
		}

		public void DialogueRunning(bool running) { _dialogueRunning = running; }

		public void QuickSaveReset ()
		{
			_quickSave = false;
		}

		public void QuickLoadReset ()
		{
			_quickLoad = false;
		}

		public void CutawayQuitReset ()
		{
			CutawayQuit = false;
		}

		public void CheckKeys ()
		{
			// TODO: 
		}

		public void Delay (int amount)
		{
			if (FastMode && amount > DELAY_SHORT) {
				amount = DELAY_SHORT;
			}
			if (IdleTime < DELAY_SCREEN_BLANKER) {
				IdleTime += amount;
			}
			var end = Environment.TickCount + amount;
			do {
//				Common::Event @event;
//				while (_eventMan.pollEvent(@event)) {
//					_idleTime = 0;
//					switch (@event.type) {
//					case Common::EVENT_KEYDOWN:
//						if (@event.kbd.hasFlags(Common::KBD_CTRL)) {
//							if (@event.kbd.keycode == Common::KEYCODE_d) {
//								_debugger = true;
//							} else if (@event.kbd.keycode == Common::KEYCODE_f) {
//								_fastMode = !_fastMode;
//							}
//						} else {
//							_inKey = @event.kbd.keycode;
//						}
//						break;
//
//					case Common::EVENT_LBUTTONDOWN:
//						_mouseButton |= MOUSE_LBUTTON;
//						break;
//
//					case Common::EVENT_RBUTTONDOWN:
//						_mouseButton |= MOUSE_RBUTTON;
//						break;
//					case Common::EVENT_RTL:
//					case Common::EVENT_QUIT:
//						if (_cutawayRunning)
//							_cutawayQuit = true;
//						return;
//
//					default:
//						break;
//					}
//				}

				_system.GraphicsManager.UpdateScreen ();

				if (amount == 0)
					break;

				ServiceLocator.Platform.Sleep ((amount > 10) ? 10 : amount);
			} while (Environment.TickCount < end);
		}

        public void ClearKeyVerb()
        {
            _keyVerb = Verb.NONE;
        }
    }
}

