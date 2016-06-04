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
using NScumm.Core;
using System;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
	public enum Verb
	{
		NONE = 0,

		PANEL_COMMAND_FIRST = 1,
		OPEN = 1,
		CLOSE = 2,
		MOVE = 3,
		// no verb 4
		GIVE = 5,
		USE = 6,
		PICK_UP = 7,
		LOOK_AT = 9,
		TALK_TO = 8,
		PANEL_COMMAND_LAST = 9,

		WALK_TO = 10,
		SCROLL_UP = 11,
		SCROLL_DOWN = 12,

		DIGIT_FIRST = 13,
		DIGIT_1 = 13,
		DIGIT_2 = 14,
		DIGIT_3 = 15,
		DIGIT_4 = 16,
		DIGIT_LAST = 16,

		INV_FIRST = DIGIT_FIRST,
		INV_1 = DIGIT_1,
		INV_2 = DIGIT_2,
		INV_3 = DIGIT_3,
		INV_4 = DIGIT_4,
		INV_LAST = DIGIT_LAST,

		USE_JOURNAL = 20,
		SKIP_TEXT = 101,

		PREP_WITH = 11,
		PREP_TO = 12
	}


	class CmdText
	{
		const int MAX_COMMAND_LEN = 256;
		public const int COMMAND_Y_POS = 151;

		byte _y;
		QueenEngine _vm;
		char[] _command = new char[MAX_COMMAND_LEN];

		public CmdText (byte y, QueenEngine vm)
		{
			_y = y;
			_vm = vm;
		}

		public void Clear ()
		{
			Array.Clear (_command, 0, _command.Length);
		}

		public static CmdText MakeCmdTextInstance (byte y, QueenEngine vm)
		{
			switch (vm.Resource.Language) {
			case Language.HE_ISR:
				return new CmdTextHebrew (y, vm);
			case Language.GR_GRE:
				return new CmdTextGreek (y, vm);
			default:
				return new CmdText (y, vm);
			}
		}
	}

	class CmdTextHebrew : CmdText
	{
		public CmdTextHebrew (byte y, QueenEngine vm)
			: base (y, vm)
		{
		}
	}

	class CmdTextGreek : CmdText
	{
		public CmdTextGreek (byte y, QueenEngine vm)
			: base (y, vm)
		{
		}
	}

	class CmdState
	{
		public void Init ()
		{
			commandLevel = 1;
			oldVerb = verb = action = Verb.NONE;
			oldNoun = noun = subject [0] = subject [1] = 0;

			selAction = Verb.NONE;
			selNoun = 0;
		}

		public Verb oldVerb, verb;
		public Verb action;
		public short oldNoun, noun;
		public int commandLevel;
		public short[] subject = new short[2];

		public Verb selAction;
		public short selNoun;
	}


	public class Command
	{
		QueenEngine _vm;

		/// <summary>
		/// Textual form of the command (displayed between room and panel areas).
		/// </summary>
		CmdText _cmdText;

		/// <summary>
		/// Commands list for each possible action.
		/// </summary>
		CmdListData[] _cmdList;
		ushort _numCmdList;

		/// <summary>
		/// Commands list for areas.
		/// </summary>
		CmdArea[] _cmdArea;
		ushort _numCmdArea;

		/// <summary>
		/// Commands list for objects.
		/// </summary>
		CmdObject[] _cmdObject;
		ushort _numCmdObject;

		/// <summary>
		/// Commands list for inventory.
		/// </summary>
		CmdInventory[] _cmdInventory;
		ushort _numCmdInventory;

		/// <summary>
		/// Commands list for gamestate.
		/// </summary>
		CmdGameState[] _cmdGameState;
		ushort _numCmdGameState;

		/// <summary>
		/// Flag indicating that the current command is fully constructed.
		/// </summary>
		bool _parse;

		/// <summary>
		/// State of current constructed command.
		/// </summary>
		CmdState _state = new CmdState ();

		public Command (QueenEngine vm)
		{
			_vm = vm;
			_cmdText = CmdText.MakeCmdTextInstance (CmdText.COMMAND_Y_POS, vm);
		}

		public void UpdatePlayer ()
		{
			throw new NotImplementedException ();
		}

		public void ReadCommandsFrom (byte[] data, ref int ptr)
		{
			ushort i;

			_numCmdList = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			_cmdList = new CmdListData[_numCmdList + 1];
			if (_numCmdList == 0) {
				_cmdList [0] = new CmdListData ();
				_cmdList [0].ReadFromBE (data, ref ptr);
			} else {
				for (i = 1; i <= _numCmdList; i++) {
					_cmdList [i] = new CmdListData ();
					_cmdList [i].ReadFromBE (data, ref ptr);
				}
			}

			_numCmdArea = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			_cmdArea = new CmdArea[_numCmdArea + 1];
			if (_numCmdArea == 0) {
				_cmdArea [0] = new CmdArea ();
				_cmdArea [0].ReadFromBE (data, ref ptr);
			} else {
				for (i = 1; i <= _numCmdArea; i++) {
					_cmdArea [i] = new CmdArea ();
					_cmdArea [i].ReadFromBE (data, ref ptr);
				}
			}

			_numCmdObject = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			_cmdObject = new CmdObject[_numCmdObject + 1];
			if (_numCmdObject == 0) {
				_cmdObject [0] = new CmdObject ();
				_cmdObject [0].ReadFromBE (data, ref ptr);
			} else {
				for (i = 1; i <= _numCmdObject; i++) {
					_cmdObject [i] = new CmdObject ();
					_cmdObject [i].ReadFromBE (data, ref ptr);

					// WORKAROUND bug #1858081: Fix an off by one error in the object
					// command 175. Object 309 should be copied to 308 (disabled).
					//
					// _objectData[307].name = -195
					// _objectData[308].name = 50
					// _objectData[309].name = -50

					if (i == 175 && _cmdObject [i].id == 320 && _cmdObject [i].dstObj == 307 && _cmdObject [i].srcObj == 309) {
						_cmdObject [i].dstObj = 308;
					}
				}
			}

			_numCmdInventory = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			_cmdInventory = new CmdInventory[_numCmdInventory + 1];
			if (_numCmdInventory == 0) {
				_cmdInventory [0] = new CmdInventory ();
				_cmdInventory [0].ReadFromBE (data, ref ptr);
			} else {
				for (i = 1; i <= _numCmdInventory; i++) {
					_cmdInventory [i] = new CmdInventory ();
					_cmdInventory [i].ReadFromBE (data, ref ptr);
				}
			}

			_numCmdGameState = data.ToUInt16BigEndian (ptr);
			ptr += 2;
			_cmdGameState = new CmdGameState[_numCmdGameState + 1];
			if (_numCmdGameState == 0) {
				_cmdGameState [0] = new CmdGameState ();
				_cmdGameState [0].ReadFromBE (data, ref ptr);
			} else {
				for (i = 1; i <= _numCmdGameState; i++) {
					_cmdGameState [i] = new CmdGameState ();
					_cmdGameState [i].ReadFromBE (data, ref ptr);
				}
			}
		}

		public void Clear (bool clearTexts)
		{
            D.Debug(6, $"Command::clear({clearTexts})");
			_cmdText.Clear ();
			if (clearTexts) {
				_vm.Display.ClearTexts (CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
			}
			_parse = false;
			_state.Init ();
		}
	}
}

