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
using NScumm.Core.Graphics;

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
        string _command;

        public bool IsEmpty { get { return string.IsNullOrEmpty(_command); } }

        public CmdText(byte y, QueenEngine vm)
        {
            _y = y;
            _vm = vm;
        }

        public virtual void AddObject(string objName)
        {
            _command = $"{_command} {objName}";
        }

        public void AddLinkWord(Verb v)
        {
            _command = $"{_command} {_vm.Logic.VerbName(v)}";
        }

        public void Display(InkColor color, string command = null, bool outlined = false)
        {
            _vm.Display.TextCurrentColor(_vm.Display.GetInkColor(color));
            if (command == null)
            {
                command = _command;
            }
            _vm.Display.SetTextCentered(_y, command, outlined);
        }

        public void DisplayTemp(InkColor color, Verb v)
        {
            var temp = _vm.Logic.VerbName(v);
            Display(color, temp, false);
        }

        public void DisplayTemp(InkColor color, string name, bool outlined)
        {
            var temp = $"{_command } {name}";
            Display(color, temp, outlined);
        }

        public void SetVerb(Verb v)
        {
            _command = _vm.Logic.VerbName(v);
        }

        public void Clear()
        {
            _command = null;
        }

        public static CmdText MakeCmdTextInstance(byte y, QueenEngine vm)
        {
            switch (vm.Resource.Language)
            {
                case Language.HE_ISR:
                    return new CmdTextHebrew(y, vm);
                case Language.GR_GRE:
                    return new CmdTextGreek(y, vm);
                default:
                    return new CmdText(y, vm);
            }
        }

    }

    class CmdTextHebrew : CmdText
    {
        public CmdTextHebrew(byte y, QueenEngine vm)
            : base(y, vm)
        {
        }
    }

    class CmdTextGreek : CmdText
    {
        public CmdTextGreek(byte y, QueenEngine vm)
            : base(y, vm)
        {
        }
    }

    class CmdState
    {
        public void Init()
        {
            commandLevel = 1;
            oldVerb = verb = action = Verb.NONE;
            oldNoun = noun = subject[0] = subject[1] = 0;

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
        private const int MAX_MATCHING_CMDS = 50;

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
        CmdState _state = new CmdState();

        int _mouseKey;
        int _selPosX, _selPosY;

        public Command(QueenEngine vm)
        {
            _vm = vm;
            _cmdText = CmdText.MakeCmdTextInstance(CmdText.COMMAND_Y_POS, vm);
        }

        public void UpdatePlayer()
        {
            if (_vm.Logic.JoeWalk != JoeWalkMode.MOVE)
            {
                Point mouse = _vm.Input.MousePos;
                LookForCurrentObject((short)mouse.X, (short)mouse.Y);
                LookForCurrentIcon((short)mouse.X, (short)mouse.Y);
            }

            if (_vm.Input.KeyVerb != Verb.NONE)
            {
                if (_vm.Input.KeyVerb == Verb.USE_JOURNAL)
                {
                    _vm.Logic.UseJournal();
                }
                else if (_vm.Input.KeyVerb != Verb.SKIP_TEXT)
                {
                    _state.verb = _vm.Input.KeyVerb;
                    if (IsVerbInv(_state.verb))
                    {
                        _state.noun = _state.selNoun = 0;
                        _state.oldNoun = 0;
                        _state.oldVerb = Verb.NONE;
                        GrabSelectedItem();
                    }
                    else
                    {
                        GrabSelectedVerb();
                    }
                }
                _vm.Input.ClearKeyVerb();
            }

            _mouseKey = _vm.Input.MouseButton;
            _vm.Input.ClearMouseButton();
            if (_mouseKey > 0)
            {
                GrabCurrentSelection();
            }
        }

        private void GrabSelectedItem()
        {
            ItemData id = FindItemData(_state.verb);
            if (id == null || id.name <= 0)
            {
                return;
            }

            short item = (short)_vm.Logic.FindInventoryItem(_state.verb - Verb.INV_FIRST);

            // If we've selected via keyboard, and there is no VERB then do
            // the ITEMs default, otherwise keep constructing!

            if (_mouseKey == Input.MOUSE_LBUTTON ||
                (_vm.Input.KeyVerb != Verb.NONE && _state.verb != Verb.NONE))
            {
                if (_state.action == Verb.NONE)
                {
                    if (_vm.Input.KeyVerb != Verb.NONE)
                    {
                        // We've selected via the keyboard, no command is being
                        // constructed, so we shall find the item's default
                        _state.verb = State.FindDefaultVerb(id.state);
                        if (_state.verb == Verb.NONE)
                        {
                            // set to Look At
                            _state.verb = Verb.LOOK_AT;
                            _cmdText.SetVerb(Verb.LOOK_AT);
                        }
                        _state.action = _state.verb;
                    }
                    else
                    {
                        // Action>0 ONLY if command has been constructed
                        // Left Mouse Button pressed just do Look At
                        _state.action = Verb.LOOK_AT;
                        _cmdText.SetVerb(Verb.LOOK_AT);
                    }
                }
                _state.verb = Verb.NONE;
            }
            else
            {
                if (_cmdText.IsEmpty)
                {
                    _state.verb = Verb.LOOK_AT;
                    _state.action = Verb.LOOK_AT;
                    _cmdText.SetVerb(Verb.LOOK_AT);
                }
                else
                {
                    if (_state.commandLevel == 2 && _parse)
                        _state.verb = _state.action;
                    else
                        _state.verb = State.FindDefaultVerb(id.state);
                    if (_state.verb == Verb.NONE)
                    {
                        // No match made, so command not yet completed. Redefine as LOOK AT
                        _state.action = Verb.LOOK_AT;
                        _cmdText.SetVerb(Verb.LOOK_AT);
                    }
                    else
                    {
                        _state.action = _state.verb;
                    }
                    _state.verb = Verb.NONE;
                }
            }

            GrabSelectedObject((short)-item, id.state, (ushort)id.name);
        }

        private void LookForCurrentIcon(short cx, short cy)
        {
            _state.verb = _vm.Grid.FindVerbUnderCursor(cx, cy);
            if (_state.oldVerb != _state.verb)
            {

                if (_state.action == Verb.NONE)
                {
                    _cmdText.Clear();
                }
                _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);

                if (IsVerbInv(_state.verb))
                {
                    ItemData id = FindItemData(_state.verb);
                    if (id != null && id.name > 0)
                    {
                        if (_state.action == Verb.NONE)
                        {
                            Verb v = State.FindDefaultVerb(id.state);
                            _cmdText.SetVerb((v == Verb.NONE) ? Verb.LOOK_AT : v);
                        }
                        string name = _vm.Logic.ObjectName((ushort)id.name);
                        _cmdText.DisplayTemp(InkColor.INK_CMD_NORMAL, name, false);
                    }
                }
                else if (IsVerbAction(_state.verb))
                {
                    _cmdText.DisplayTemp(InkColor.INK_CMD_NORMAL, _state.verb);
                }
                else if (_state.verb == Verb.NONE)
                {
                    _cmdText.Display(InkColor.INK_CMD_NORMAL);
                }
                _state.oldVerb = _state.verb;
            }
        }

        private bool IsVerbAction(Verb v) { return (v >= Verb.PANEL_COMMAND_FIRST && v <= Verb.PANEL_COMMAND_LAST) || (v == Verb.WALK_TO); }

        private bool IsVerbInvScroll(Verb v) { return v == Verb.SCROLL_UP || v == Verb.SCROLL_DOWN; }

        private ItemData FindItemData(Verb invNum)
        {
            ItemData id = null;
            ushort itNum = _vm.Logic.FindInventoryItem(invNum - Verb.INV_FIRST);
            if (itNum != 0)
            {
                id = _vm.Logic.ItemData[itNum];
            }
            return id;
        }

        private void GrabCurrentSelection()
        {
            Point mouse = _vm.Input.MousePos;
            _selPosX = mouse.X;
            _selPosY = mouse.Y;

            ushort zone = _vm.Grid.FindObjectUnderCursor((short)_selPosX, (short)_selPosY);
            _state.noun = (short)_vm.Grid.FindObjectNumber(zone);
            _state.verb = _vm.Grid.FindVerbUnderCursor((short)_selPosX, (short)_selPosY);

            _selPosX += _vm.Display.HorizontalScroll;

            if (IsVerbAction(_state.verb) || IsVerbInvScroll(_state.verb))
            {
                GrabSelectedVerb();
            }
            else if (IsVerbInv(_state.verb))
            {
                GrabSelectedItem();
            }
            else if (_state.noun != 0)
            {
                GrabSelectedNoun();
            }
            else if (_selPosY < Defines.ROOM_ZONE_HEIGHT && _state.verb == Verb.NONE)
            {
                // select without a command, do a WALK
                Clear(true);
                _vm.Logic.JoeWalk = JoeWalkMode.EXECUTE;
            }
        }

        private void GrabSelectedNoun()
        {
            ObjectData od = FindObjectData((ushort)_state.noun);
            if (od == null || od.name <= 0)
            {
                // selected a turned off object, so just walk
                Clear(true);
                _state.noun = 0;
                _vm.Logic.JoeWalk = JoeWalkMode.EXECUTE;
                return;
            }

            if (_state.verb == Verb.NONE)
            {
                if (_mouseKey == Input.MOUSE_LBUTTON)
                {
                    if ((_state.commandLevel != 2 && _state.action == Verb.NONE) ||
                        (_state.commandLevel == 2 && _parse))
                    {
                        _state.verb = Verb.WALK_TO;
                        _state.action = Verb.WALK_TO;
                        _cmdText.SetVerb(Verb.WALK_TO);
                    }
                }
                else if (_mouseKey == Input.MOUSE_RBUTTON)
                {
                    if (_cmdText.IsEmpty)
                    {
                        _state.verb = State.FindDefaultVerb(od.state);
                        _state.selAction = (_state.verb == Verb.NONE) ? Verb.WALK_TO : _state.verb;
                        _cmdText.SetVerb(_state.selAction);
                        _cmdText.AddObject(_vm.Logic.ObjectName((ushort)od.name));
                    }
                    else
                    {
                        if ((_state.commandLevel == 2 && !_parse) || _state.action != Verb.NONE)
                        {
                            _state.verb = _state.action;
                        }
                        else
                        {
                            _state.verb = State.FindDefaultVerb(od.state);
                        }
                        _state.action = (_state.verb == Verb.NONE) ? Verb.WALK_TO : _state.verb;
                        _state.verb = Verb.NONE;
                    }
                }
            }

            _state.selNoun = 0;
            short objNum = (short)(_vm.Logic.CurrentRoomData + _state.noun);
            GrabSelectedObject(objNum, od.state, (ushort)od.name);
        }

        private void GrabSelectedObject(short objNum, ushort objState, ushort objName)
        {
            if (_state.action != Verb.NONE)
            {
                _cmdText.AddObject(_vm.Logic.ObjectName(objName));
            }

            _state.subject[_state.commandLevel - 1] = objNum;

            // if first noun and it's a 2 level command then set up action word
            if (_state.action == Verb.USE && _state.commandLevel == 1)
            {
                if (State.FindUse(objState) == StateUse.STATE_USE_ON)
                {
                    // object supports 2 levels, command not fully constructed
                    _state.commandLevel = 2;
                    _cmdText.AddLinkWord(Verb.PREP_WITH);
                    _cmdText.Display(InkColor.INK_CMD_NORMAL);
                    _parse = false;
                }
                else
                {
                    _parse = true;
                }
            }
            else if (_state.action == Verb.GIVE && _state.commandLevel == 1)
            {
                // command not fully constructed
                _state.commandLevel = 2;
                _cmdText.AddLinkWord(Verb.PREP_TO);
                _cmdText.Display(InkColor.INK_CMD_NORMAL);
                _parse = false;
            }
            else
            {
                _parse = true;
            }

            if (_parse)
            {
                _state.verb = Verb.NONE;
                _vm.Logic.JoeWalk = JoeWalkMode.EXECUTE;
                _state.selAction = _state.action;
                _state.action = Verb.NONE;
            }
        }

        public void ReadCommandsFrom(byte[] data, ref int ptr)
        {
            ushort i;

            _numCmdList = data.ToUInt16BigEndian(ptr);
            ptr += 2;
            _cmdList = new CmdListData[_numCmdList + 1];
            if (_numCmdList == 0)
            {
                _cmdList[0] = new CmdListData();
                _cmdList[0].ReadFromBE(data, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numCmdList; i++)
                {
                    _cmdList[i] = new CmdListData();
                    _cmdList[i].ReadFromBE(data, ref ptr);
                }
            }

            _numCmdArea = data.ToUInt16BigEndian(ptr);
            ptr += 2;
            _cmdArea = new CmdArea[_numCmdArea + 1];
            if (_numCmdArea == 0)
            {
                _cmdArea[0] = new CmdArea();
                _cmdArea[0].ReadFromBE(data, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numCmdArea; i++)
                {
                    _cmdArea[i] = new CmdArea();
                    _cmdArea[i].ReadFromBE(data, ref ptr);
                }
            }

            _numCmdObject = data.ToUInt16BigEndian(ptr);
            ptr += 2;
            _cmdObject = new CmdObject[_numCmdObject + 1];
            if (_numCmdObject == 0)
            {
                _cmdObject[0] = new CmdObject();
                _cmdObject[0].ReadFromBE(data, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numCmdObject; i++)
                {
                    _cmdObject[i] = new CmdObject();
                    _cmdObject[i].ReadFromBE(data, ref ptr);

                    // WORKAROUND bug #1858081: Fix an off by one error in the object
                    // command 175. Object 309 should be copied to 308 (disabled).
                    //
                    // _objectData[307].name = -195
                    // _objectData[308].name = 50
                    // _objectData[309].name = -50

                    if (i == 175 && _cmdObject[i].id == 320 && _cmdObject[i].dstObj == 307 && _cmdObject[i].srcObj == 309)
                    {
                        _cmdObject[i].dstObj = 308;
                    }
                }
            }

            _numCmdInventory = data.ToUInt16BigEndian(ptr);
            ptr += 2;
            _cmdInventory = new CmdInventory[_numCmdInventory + 1];
            if (_numCmdInventory == 0)
            {
                _cmdInventory[0] = new CmdInventory();
                _cmdInventory[0].ReadFromBE(data, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numCmdInventory; i++)
                {
                    _cmdInventory[i] = new CmdInventory();
                    _cmdInventory[i].ReadFromBE(data, ref ptr);
                }
            }

            _numCmdGameState = data.ToUInt16BigEndian(ptr);
            ptr += 2;
            _cmdGameState = new CmdGameState[_numCmdGameState + 1];
            if (_numCmdGameState == 0)
            {
                _cmdGameState[0] = new CmdGameState();
                _cmdGameState[0].ReadFromBE(data, ref ptr);
            }
            else
            {
                for (i = 1; i <= _numCmdGameState; i++)
                {
                    _cmdGameState[i] = new CmdGameState();
                    _cmdGameState[i].ReadFromBE(data, ref ptr);
                }
            }
        }

        public void ExecuteCurrentAction()
        {
            _vm.Logic.EntryObj = 0;

            if (_mouseKey == Input.MOUSE_RBUTTON && _state.subject[0] > 0)
            {

                ObjectData od = _vm.Logic.ObjectData[_state.subject[0]];
                if (od == null || od.name <= 0)
                {
                    CleanupCurrentAction();
                    return;
                }

                _state.verb = State.FindDefaultVerb(od.state);
                _state.selAction = (_state.verb == Verb.NONE) ? Verb.WALK_TO : _state.verb;
                _cmdText.SetVerb(_state.selAction);
                _cmdText.AddObject(_vm.Logic.ObjectName((ushort)od.name));
            }

            // always highlight the current command when actioned
            _cmdText.Display(InkColor.INK_CMD_SELECT);

            _state.selNoun = _state.noun;
            _state.commandLevel = 1;

            if (HandleWrongAction())
            {
                CleanupCurrentAction();
                return;
            }

            // get the commands associated with this object/item
            ushort comMax = 0;
            ushort[] matchingCmds = new ushort[MAX_MATCHING_CMDS];
            for (var i = 1; i <= _numCmdList; ++i)
            {
                var cmdList = _cmdList[i];
                if (cmdList.Match(_state.selAction, _state.subject[0], _state.subject[1]))
                {
                    //assert(comMax < MAX_MATCHING_CMDS);
                    matchingCmds[comMax] = (ushort)i;
                    ++comMax;
                }
            }

            D.Debug(6, $"Command::executeCurrentAction() - comMax={comMax} subj1={_state.subject[0]:X} subj2={_state.subject[1]:X}");

            if (comMax == 0)
            {
                SayInvalidAction(_state.selAction, _state.subject[0], _state.subject[1]);
                Clear(true);
                CleanupCurrentAction();
                return;
            }

            // process each associated command for the Object, until all done
            // or one of the Gamestate tests fails...
            short cond = 0;
            CmdListData com = _cmdList[0];
            ushort comId = 0;
            for (var i = 1; i <= comMax; ++i)
            {

                comId = matchingCmds[i - 1];

                // WORKAROUND bug #1497280: This command is triggered in room 56 (the
                // room with two waterfalls in the maze part of the game) if the user
                // tries to walk through the left waterfall (object 423).
                //
                // Normally, this would move Joe to room 101 on the upper level and
                // start a cutscene. Joe would notice that Yan has been trapped (on
                // the lower level of the same room). The problem would then appear :
                // Joe is stuck behind the waterfall due to a walkbox issue. We could
                // fix the walkbox issue, but then Joe would walk through the waterfall
                // which wouldn't look that nice, graphically.
                //
                // Since this command isn't necessary to complete the game and doesn't
                // really makes sense here, we just skip it for now. The same cutscene
                // is already played in command 648, so the user don't miss anything
                // from the story/experience pov.
                //
                // Note: this happens with the original engine, too.

                if (comId == 649)
                {
                    continue;
                }

                com = _cmdList[comId];

                // check the Gamestates and set them if necessary
                cond = 0;
                if (com.setConditions)
                {
                    cond = SetConditions(comId, (i == comMax));
                }

                if (cond == -1 && i == comMax)
                {
                    // only exit on a condition fail if at last command
                    // Joe hasnt spoken, so do normal LOOK command
                    break;
                }
                else if (cond == -2 && i == comMax)
                {
                    // only exit on a condition fail if at last command
                    // Joe has spoken, so skip LOOK command
                    CleanupCurrentAction();
                    return;
                }
                else if (cond >= 0)
                {
                    // we've had a successful Gamestate check, so we must now exit
                    cond = ExecuteCommand(comId, cond);
                    break;
                }
            }

            if (_state.selAction == Verb.USE_JOURNAL)
            {
                Clear(true);
            }
            else
            {
                if (cond <= 0 && _state.selAction == Verb.LOOK_AT)
                {
                    LookAtSelectedObject();
                }
                else
                {
                    // only play song if it's a PLAY AFTER type
                    if (com.song < 0)
                    {
                        _vm.Sound.PlaySong((short)-com.song);
                    }
                    Clear(true);
                }
                CleanupCurrentAction();
            }

        }

        private short ExecuteCommand(ushort comId, short condResult)
        {
            // execute.c l.313-452
            D.Debug(6, $"Command::executeCommand() - cond = {condResult:X}, com = {comId:X}");

            CmdListData com = _cmdList[comId];

            if (com.setAreas)
            {
                SetAreas(comId);
            }

            // don't try to grab if action is TALK or WALK
            if (_state.selAction != Verb.TALK_TO && _state.selAction != Verb.WALK_TO)
            {
                int i;
                for (i = 0; i < 2; ++i)
                {
                    short obj = _state.subject[i];
                    if (obj > 0)
                    {
                        _vm.Logic.JoeGrab(State.FindGrab(_vm.Logic.ObjectData[obj].state));
                    }
                }
            }

            bool cutDone = false;
            if (condResult > 0)
            {
                // check for cutaway/dialogs before updating Objects
                string desc = _vm.Logic.ObjectTextualDescription((ushort)condResult);
                if (ExecuteIfCutaway(desc))
                {
                    condResult = 0;
                    cutDone = true;
                }
                else if (ExecuteIfDialog(desc))
                {
                    condResult = 0;
                }
            }

            short oldImage = 0;
            if (_state.subject[0] > 0)
            {
                // an object (not an item)
                oldImage = _vm.Logic.ObjectData[_state.subject[0]].image;
            }

            if (com.setObjects)
            {
                SetObjects(comId);
            }

            if (com.setItems)
            {
                SetItems(comId);
            }

            if (com.imageOrder != 0 && _state.subject[0] > 0)
            {
                ObjectData od = _vm.Logic.ObjectData[_state.subject[0]];
                // we must update the graphic image of the object
                if (com.imageOrder < 0)
                {
                    // instead of setting to -1 or -2, flag as negative
                    if (od.image > 0)
                    {
                        // make sure that object is not already updated
                        od.image = (short)-(od.image + 10);
                    }
                }
                else
                {
                    od.image = com.imageOrder;
                }
                _vm.Graphics.RefreshObject((ushort)_state.subject[0]);
            }
            else
            {
                // this object is not being updated by command list, see if
                // it has another image copied to it
                if (_state.subject[0] > 0)
                {
                    // an object (not an item)
                    if (_vm.Logic.ObjectData[_state.subject[0]].image != oldImage)
                    {
                        _vm.Graphics.RefreshObject((ushort)_state.subject[0]);
                    }
                }
            }

            // don't play music on an OPEN/CLOSE command - in case the command fails
            if (_state.selAction != Verb.NONE &&
                _state.selAction != Verb.OPEN &&
                _state.selAction != Verb.CLOSE)
            {
                // only play song if it's a PLAY BEFORE type
                if (com.song > 0)
                {
                    _vm.Sound.PlaySong(com.song);
                }
            }

            // do a special hardcoded section
            // l.419-452 execute.c
            switch (com.specialSection)
            {
                case 1:
                    _vm.Logic.UseJournal();
                    _state.selAction = Verb.USE_JOURNAL;
                    return condResult;
                case 2:
                    _vm.Logic.JoeUseDress(true);
                    break;
                case 3:
                    _vm.Logic.JoeUseClothes(true);
                    break;
                case 4:
                    _vm.Logic.JoeUseUnderwear();
                    break;
            }

            if (_state.subject[0] > 0)
                ChangeObjectState(_state.selAction, _state.subject[0], com.song, cutDone);

            if (condResult > 0)
            {
                _vm.Logic.MakeJoeSpeak((ushort)condResult, true);
            }
            return condResult;
        }

        private void ChangeObjectState(Verb action, short obj, short song, bool cutDone)
        {
            // l.456-533 execute.c
            ObjectData objData = _vm.Logic.ObjectData[obj];

            if (action == Verb.OPEN && !cutDone)
            {
                if (State.FindOn(objData.state) == StateOn.STATE_ON_ON)
                {
                    State.AlterOn(ref objData.state, StateOn.STATE_ON_OFF);
                    State.AlterDefaultVerb(ref objData.state, Verb.NONE);

                    // play music if it exists... (or SFX for open/close door)
                    if (song != 0)
                    {
                        _vm.Sound.PlaySong(Math.Abs(song));
                    }

                    if (objData.entryObj != 0)
                    {
                        // if it's a door, then update door that it links to
                        OpenOrCloseAssociatedObject(action, Math.Abs(objData.entryObj));
                        objData.entryObj = Math.Abs(objData.entryObj);
                    }
                }
                else
                {
                    // 'it's already open !'
                    _vm.Logic.MakeJoeSpeak(9);
                }
            }
            else if (action == Verb.CLOSE && !cutDone)
            {
                if (State.FindOn(objData.state) == StateOn.STATE_ON_OFF)
                {
                    State.AlterOn(ref objData.state, StateOn.STATE_ON_ON);
                    State.AlterDefaultVerb(ref objData.state, Verb.OPEN);

                    // play music if it exists... (or SFX for open/close door)
                    if (song != 0)
                    {
                        _vm.Sound.PlaySong(Math.Abs(song));
                    }

                    if (objData.entryObj != 0)
                    {
                        // if it's a door, then update door that it links to
                        OpenOrCloseAssociatedObject(action, Math.Abs(objData.entryObj));
                        objData.entryObj = (short)-Math.Abs(objData.entryObj);
                    }
                }
                else
                {
                    // 'it's already closed !'
                    _vm.Logic.MakeJoeSpeak(10);
                }
            }
            else if (action == Verb.MOVE)
            {
                State.AlterOn(ref objData.state, StateOn.STATE_ON_OFF);
            }
        }

        private void OpenOrCloseAssociatedObject(Verb action, short otherObj)
        {
            CmdListData cmdList;
            ushort com = 0;
            ushort i;
            for (i = 1; i <= _numCmdList && com == 0; ++i)
            {
                cmdList = _cmdList[i];
                if (cmdList.Match(action, otherObj, 0))
                {
                    if (cmdList.setConditions)
                    {
                        CmdGameState[] cmdGs = _cmdGameState;
                        ushort j;
                        for (j = 1; j <= _numCmdGameState; ++j)
                        {
                            if (cmdGs[j].id == i && cmdGs[j].gameStateSlot > 0)
                            {
                                if (_vm.Logic.GameState[cmdGs[j].gameStateSlot] == cmdGs[j].gameStateValue)
                                {
                                    com = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        com = i;
                        break;
                    }
                }
            }

            if (com != 0)
            {

                D.Debug(6, $"Command::openOrCloseAssociatedObject() com={com:X}");

                cmdList = _cmdList[com];
                ObjectData objData = _vm.Logic.ObjectData[otherObj];

                if (cmdList.imageOrder != 0)
                {
                    objData.image = cmdList.imageOrder;
                }

                if (action == Verb.OPEN)
                {
                    if (State.FindOn(objData.state) == StateOn.STATE_ON_ON)
                    {
                        State.AlterOn(ref objData.state, StateOn.STATE_ON_OFF);
                        State.AlterDefaultVerb(ref objData.state, Verb.NONE);
                        objData.entryObj = Math.Abs(objData.entryObj);
                    }
                }
                else if (action == Verb.CLOSE)
                {
                    if (State.FindOn(objData.state) == StateOn.STATE_ON_OFF)
                    {
                        State.AlterOn(ref objData.state, StateOn.STATE_ON_ON);
                        State.AlterDefaultVerb(ref objData.state, Verb.OPEN);
                        objData.entryObj = (short)-Math.Abs(objData.entryObj);
                    }
                }
            }
        }

        private void SetItems(ushort command)
        {
            D.Debug(9, $"Command::setItems({command})");

            ItemData[] items = _vm.Logic.ItemData;
            CmdInventory cmdInv;
            for (ushort i = 1; i <= _numCmdInventory; ++i)
            {
                cmdInv = _cmdInventory[i];
                if (cmdInv.id == command)
                {
                    ushort dstItem = (ushort)Math.Abs(cmdInv.dstItem);
                    // found an item
                    if (cmdInv.dstItem > 0)
                    {
                        // add item to inventory
                        if (cmdInv.srcItem > 0)
                        {
                            // copy data from source item to item, then enable it
                            items[dstItem] = items[cmdInv.srcItem];
                            items[dstItem].name = Math.Abs(items[dstItem].name);
                        }
                        _vm.Logic.InventoryInsertItem((NScumm.Queen.Item)cmdInv.dstItem);
                    }
                    else
                    {
                        // delete item
                        if (items[dstItem].name > 0)
                        {
                            _vm.Logic.InventoryDeleteItem((NScumm.Queen.Item)dstItem);
                        }
                        if (cmdInv.srcItem > 0)
                        {
                            // copy data from source item to item, then disable it
                            items[dstItem] = items[cmdInv.srcItem];
                            items[dstItem].name = (short)-Math.Abs(items[dstItem].name);
                        }
                    }
                }
            }
        }

        private void SetObjects(ushort command)
        {
            D.Debug(9, $"Command::setObjects({command})");

            CmdObject cmdObj;
            for (ushort i = 1; i <= _numCmdObject; ++i)
            {
                cmdObj = _cmdObject[i];
                if (cmdObj.id == command)
                {

                    // found an object
                    ushort dstObj = (ushort)Math.Abs(cmdObj.dstObj);
                    ObjectData objData = _vm.Logic.ObjectData[dstObj];

                    D.Debug(6, "Command::setObjects() - dstObj=%X srcObj=%X _state.subject[0]=%X", cmdObj.dstObj, cmdObj.srcObj, _state.subject[0]);

                    if (cmdObj.dstObj > 0)
                    {
                        // show the object
                        objData.name = Math.Abs(objData.name);
                        // test that the object has not already been deleted
                        // by checking if it is not equal to zero
                        if (cmdObj.srcObj == -1 && objData.name != 0)
                        {
                            // delete object by setting its name to 0 and
                            // turning off graphic image
                            objData.name = 0;
                            if (objData.room == _vm.Logic.CurrentRoom)
                            {
                                if (dstObj != _state.subject[0])
                                {
                                    // if the new object we have updated is on screen and is not the
                                    // current object, then we can update. This is because we turn
                                    // current object off ourselves by COM_LIST(com, 8)
                                    if (objData.image != -3 && objData.image != -4)
                                    {
                                        // it is a normal object (not a person)
                                        // turn the graphic image off for the object
                                        objData.image = (short)-(objData.image + 10);
                                    }
                                }
                                // invalidate object area
                                ushort objZone = (ushort)(dstObj - _vm.Logic.CurrentRoomData);
                                _vm.Grid.SetZone(GridScreen.ROOM, (short)objZone, 0, 0, 1, 1);
                            }
                        }

                        if (cmdObj.srcObj > 0)
                        {
                            // copy data from dummy object to object
                            short image1 = objData.image;
                            short image2 = _vm.Logic.ObjectData[cmdObj.srcObj].image;
                            _vm.Logic.ObjectCopy(cmdObj.srcObj, (short)dstObj);
                            if (image1 != 0 && image2 == 0 && objData.room == _vm.Logic.CurrentRoom)
                            {
                                ushort bobNum = _vm.Logic.FindBob((short)dstObj);
                                if (bobNum != 0)
                                {
                                    _vm.Graphics.ClearBob(bobNum);
                                }
                            }
                        }

                        if (dstObj != _state.subject[0])
                        {
                            // if the new object we have updated is on screen and
                            // is not current object then update it
                            _vm.Graphics.RefreshObject(dstObj);
                        }
                    }
                    else
                    {
                        // hide the object
                        if (objData.name > 0)
                        {
                            objData.name = (short)-objData.name;
                            // may need to turn BOBs off for objects to be hidden on current
                            // screen ! if the new object we have updated is on screen and
                            // is not current object then update it
                            _vm.Graphics.RefreshObject(dstObj);
                        }
                    }
                }
            }
        }

        private void SetAreas(ushort command)
        {
            D.Debug(9, $"Command::setAreas({command})");

            CmdArea cmdArea;
            for (ushort i = 1; i <= _numCmdArea; ++i)
            {
                cmdArea = _cmdArea[i];
                if (cmdArea.id == command)
                {
                    ushort areaNum = (ushort)Math.Abs(cmdArea.area);
                    Area area = _vm.Grid.Areas[cmdArea.room][areaNum];
                    if (cmdArea.area > 0)
                    {
                        // turn on area
                        area.mapNeighbors = Math.Abs(area.mapNeighbors);
                    }
                    else
                    {
                        // turn off area
                        area.mapNeighbors = (short)-Math.Abs(area.mapNeighbors);
                    }
                }
            }
        }

        private short SetConditions(ushort command, bool lastCmd)
        {
            D.Debug(9, $"Command::setConditions({command}, {lastCmd})");

            short ret = 0;
            ushort[] cmdState = new ushort[21];
            ushort cmdStateCount = 0;
            ushort i;
            CmdGameState cmdGs;
            for (i = 1; i <= _numCmdGameState; ++i)
            {
                cmdGs = _cmdGameState[i];
                if (cmdGs.id == command)
                {
                    if (cmdGs.gameStateSlot > 0)
                    {
                        if (_vm.Logic.GameState[cmdGs.gameStateSlot] != cmdGs.gameStateValue)
                        {
                            D.Debug(6, $"Command::setConditions() - GS[{cmdGs.gameStateSlot}] == {_vm.Logic.GameState[cmdGs.gameStateSlot]} (should be {cmdGs.gameStateValue})");
                            // failed test
                            ret = (short)i;
                            break;
                        }
                    }
                    else
                    {
                        cmdState[cmdStateCount] = i;
                        ++cmdStateCount;
                    }
                }
            }

            if (ret > 0)
            {
                // we've failed, so see if we need to make Joe speak
                cmdGs = _cmdGameState[ret];
                if (cmdGs.speakValue > 0 && lastCmd)
                {
                    // check to see if fail state is in fact a cutaway
                    string objDesc = _vm.Logic.ObjectTextualDescription(cmdGs.speakValue);
                    if (!ExecuteIfCutaway(objDesc) && !ExecuteIfDialog(objDesc))
                    {
                        _vm.Logic.MakeJoeSpeak(cmdGs.speakValue, true);
                    }
                    ret = -2;
                }
                else
                {
                    // return -1 so Joe will be able to speak a normal description
                    ret = -1;
                }
            }
            else
            {
                ret = 0;
                // all tests were okay, now set gamestates
                for (i = 0; i < cmdStateCount; ++i)
                {
                    cmdGs = _cmdGameState[cmdState[i]];
                    _vm.Logic.GameState[Math.Abs(cmdGs.gameStateSlot)] = cmdGs.gameStateValue;
                    // set return value for Joe to say something
                    ret = (short)cmdGs.speakValue;
                }
            }
            return ret;
        }

        private bool ExecuteIfCutaway(string description)
        {
            if (description.Length > 4 && string.Compare(description, description.Length - 4, ".CUT", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
            {
                _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);

                string nextCutaway;
                _vm.Logic.PlayCutaway(description, out nextCutaway);
                while (nextCutaway.Length != 0)
                {
                    _vm.Logic.PlayCutaway(nextCutaway, out nextCutaway);
                }
                return true;
            }
            return false;
        }

        private bool ExecuteIfDialog(string description)
        {
            if (description.Length > 4 &&
                string.Compare(description, description.Length - 4, ".DOG", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
            {

                _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);

                string cutaway;
                _vm.Logic.StartDialogue(description, _state.selNoun, out cutaway);

                while (cutaway.Length != 0)
                {
                    string currentCutaway = cutaway;
                    _vm.Logic.PlayCutaway(currentCutaway, out cutaway);
                }
                return true;
            }
            return false;
        }

        private void LookAtSelectedObject()
        {
            ushort desc;
            if (_state.subject[0] < 0)
            {
                desc = _vm.Logic.ItemData[-_state.subject[0]].description;
            }
            else
            {
                ObjectData objData = _vm.Logic.ObjectData[_state.subject[0]];
                if (objData.name <= 0)
                {
                    return;
                }
                desc = objData.description;
            }

            D.Debug(6, $"Command::lookAtSelectedObject() - desc = {desc:X}, _state.subject[0] = {_state.subject[0]:X}");

            // check to see if the object/item has a series of description
            ObjectDescription objDesc = _vm.Logic.ObjectDescription[1];
            ushort i;
            for (i = 1; i <= _vm.Logic.ObjectDescriptionCount; ++i)
            {
                objDesc = _vm.Logic.ObjectDescription[i];
                if (objDesc.@object == _state.subject[0])
                {
                    desc = NextObjectDescription(objDesc, desc);
                    break;
                }
            }
            if (desc != 0)
            {
                _vm.Logic.MakeJoeSpeak(desc, true);
            }
            _vm.Logic.JoeFace();
        }

        private ushort NextObjectDescription(ObjectDescription objDesc, ushort firstDesc)
        {
            // l.69-103 select.c
            ushort i;
            ushort diff = (ushort)(objDesc.lastDescription - firstDesc);
            D.Debug(6, $"Command::nextObjectDescription() - diff = {diff}, type = {objDesc.type}");
            switch (objDesc.type)
            {
                case 0:
                case 1:
                    // random type, start with first description
                    if (objDesc.type == 0 && objDesc.lastSeenNumber == 0)
                    {
                        // first time look at called
                        objDesc.lastSeenNumber = firstDesc;
                        break;
                    }
                    // already displayed first, do a random

                    i = objDesc.lastSeenNumber;
                    while (i == objDesc.lastSeenNumber)
                    {
                        i = (ushort)(firstDesc + _vm.Randomizer.Next(1 + diff));
                    }
                    objDesc.lastSeenNumber = i;
                    break;
                case 2:
                    // sequential, but loop
                    ++objDesc.lastSeenNumber;
                    if (objDesc.lastSeenNumber > objDesc.lastDescription)
                    {
                        objDesc.lastSeenNumber = firstDesc;
                    }
                    break;
                case 3:
                    // sequential without looping
                    if (objDesc.lastSeenNumber != objDesc.lastDescription)
                    {
                        ++objDesc.lastSeenNumber;
                    }
                    break;
            }
            return objDesc.lastSeenNumber;
        }

        private void SayInvalidAction(Verb action, short subj1, short subj2)
        {
            // l.158-272 execute.c
            switch (action)
            {

                case Verb.LOOK_AT:
                    LookAtSelectedObject();
                    break;

                case Verb.OPEN:
                    // 'it doesn't seem to open'
                    _vm.Logic.MakeJoeSpeak(1);
                    break;

                case Verb.USE:
                    if (subj1 < 0)
                    {
                        ushort k = (ushort)_vm.Logic.ItemData[-subj1].sfxDescription;
                        if (k > 0)
                        {
                            _vm.Logic.MakeJoeSpeak(k, true);
                        }
                        else
                        {
                            _vm.Logic.MakeJoeSpeak(2);
                        }
                    }
                    else
                    {
                        _vm.Logic.MakeJoeSpeak(2);
                    }
                    break;

                case Verb.TALK_TO:
                    _vm.Logic.MakeJoeSpeak((ushort)(24 + _vm.Randomizer.Next(1 + 2)));
                    break;

                case Verb.CLOSE:
                    _vm.Logic.MakeJoeSpeak(2);
                    break;

                case Verb.MOVE:
                    // 'I can't move it'
                    if (subj1 > 0)
                    {
                        short img = _vm.Logic.ObjectData[subj1].image;
                        if (img == -4 || img == -3)
                        {
                            _vm.Logic.MakeJoeSpeak(18);
                        }
                        else
                        {
                            _vm.Logic.MakeJoeSpeak(3);
                        }
                    }
                    else
                    {
                        _vm.Logic.MakeJoeSpeak(3);
                    }
                    break;

                case Verb.GIVE:
                    // 'I can't give the subj1 to subj2'
                    if (subj1 < 0)
                    {
                        if (subj2 > 0)
                        {
                            short img = _vm.Logic.ObjectData[subj2].image;
                            if (img == -4 || img == -3)
                            {
                                _vm.Logic.MakeJoeSpeak((ushort)(27 + _vm.Randomizer.Next(1 + 2)));
                            }
                        }
                        else
                        {
                            _vm.Logic.MakeJoeSpeak(11);
                        }
                    }
                    else
                    {
                        _vm.Logic.MakeJoeSpeak(12);
                    }
                    break;

                case Verb.PICK_UP:
                    if (subj1 < 0)
                    {
                        _vm.Logic.MakeJoeSpeak(14);
                    }
                    else
                    {
                        short img = _vm.Logic.ObjectData[subj1].image;
                        if (img == -4 || img == -3)
                        {
                            // Trying to get a person
                            _vm.Logic.MakeJoeSpeak(20);
                        }
                        else
                        {
                            // 5 : 'I can't pick that up'
                            // 6 : 'I don't think I need that'
                            // 7 : 'I'd rather leave it here'
                            // 8 : 'I don't think I'd have any use for that'
                            _vm.Logic.MakeJoeSpeak((ushort)(5 + _vm.Randomizer.Next(1 + 3)));
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        private bool HandleWrongAction()
        {
            // l.96-141 execute.c
            ushort objMax = (ushort)_vm.Grid.ObjMax[_vm.Logic.CurrentRoom];
            ushort roomData = _vm.Logic.CurrentRoomData;

            // select without a command or WALK TO ; do a WALK
            if ((_state.selAction == Verb.WALK_TO || _state.selAction == Verb.NONE) &&
                (_state.selNoun > objMax || _state.selNoun == 0))
            {
                if (_state.selAction == Verb.NONE)
                {
                    _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
                }
                _vm.Walk.MoveJoe(0, (short)_selPosX, (short)_selPosY, false);
                return true;
            }

            // check to see if one of the objects is hidden
            int i;
            for (i = 0; i < 2; ++i)
            {
                short obj = _state.subject[i];
                if (obj > 0 && _vm.Logic.ObjectData[obj].name <= 0)
                {
                    return true;
                }
            }

            // check for USE command on exists
            if (_state.selAction == Verb.USE &&
                _state.subject[0] > 0 && _vm.Logic.ObjectData[_state.subject[0]].entryObj > 0)
            {
                _state.selAction = Verb.WALK_TO;
            }

            if (_state.selNoun > 0 && _state.selNoun <= objMax)
            {
                ushort objNum = (ushort)(roomData + _state.selNoun);
                if (MakeJoeWalkTo((short)_selPosX, (short)_selPosY, objNum, _state.selAction, true) != 0)
                {
                    return true;
                }
                if (_state.selAction == Verb.WALK_TO && _vm.Logic.ObjectData[objNum].entryObj < 0)
                {
                    return true;
                }
            }
            return false;
        }

        private int MakeJoeWalkTo(short x, short y, ushort objNum, Verb v, bool mustWalk)
        {
            // Check to see if object is actually an exit to another
            // room. If so, then set up new room
            ObjectData objData = _vm.Logic.ObjectData[objNum];
            if (objData.x != 0 || objData.y != 0)
            {
                x = (short)objData.x;
                y = (short)objData.y;
            }
            if (v == Verb.WALK_TO)
            {
                _vm.Logic.EntryObj = (ushort)objData.entryObj;
                if (objData.entryObj > 0)
                {
                    _vm.Logic.NewRoom = _vm.Logic.ObjectData[objData.entryObj].room;
                    // because this is an exit object, see if there is
                    // a walk off point and set (x,y) accordingly
                    WalkOffData wod = _vm.Logic.WalkOffPointForObject(objNum);
                    if (wod != null)
                    {
                        x = (short)wod.x;
                        y = (short)wod.y;
                    }
                }
            }
            else
            {
                _vm.Logic.EntryObj = 0;
                _vm.Logic.NewRoom = 0;
            }

            D.Debug(6, $"Command::makeJoeWalkTo() - x={x} y={y} newRoom={_vm.Logic.NewRoom}");

            short p = 0;
            if (mustWalk)
            {
                // determine which way for Joe to face Object
                Direction facing = State.FindDirection(objData.state);
                BobSlot bobJoe = _vm.Graphics.Bobs[0];
                if (x == bobJoe.x && y == bobJoe.y)
                {
                    _vm.Logic.JoeFacing = facing;
                    _vm.Logic.JoeFace();
                }
                else
                {
                    p = _vm.Walk.MoveJoe(facing, x, y, false);
                    if (p != 0)
                    {
                        _vm.Logic.NewRoom = 0; // cancel makeJoeWalkTo, that should be equivalent to cr10 fix
                    }
                }
            }
            return p;
        }

        private void CleanupCurrentAction()
        {
            // l.595-597 execute.c
            _vm.Logic.JoeFace();
            _state.oldNoun = 0;
            _state.oldVerb = Verb.NONE;
        }

        public void Clear(bool clearTexts)
        {
            D.Debug(6, $"Command::clear({clearTexts})");
            _cmdText.Clear();
            if (clearTexts)
            {
                _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
            }
            _parse = false;
            _state.Init();
        }

        private bool IsVerbInv(Verb v) { return v >= Verb.INV_FIRST && v <= Verb.INV_LAST; }

        private void LookForCurrentObject(short cx, short cy)
        {
            ushort obj = _vm.Grid.FindObjectUnderCursor(cx, cy);
            _state.noun = (short)_vm.Grid.FindObjectNumber(obj);

            if (_state.oldNoun == _state.noun)
            {
                return;
            }

            ObjectData od = FindObjectData((ushort)_state.noun);
            if (od == null || od.name <= 0)
            {
                _state.oldNoun = _state.noun;
                _vm.Display.ClearTexts(CmdText.COMMAND_Y_POS, CmdText.COMMAND_Y_POS);
                if (_state.action != Verb.NONE)
                {
                    _cmdText.Display(InkColor.INK_CMD_NORMAL);
                }
                return;
            }

            // if no command yet selected, then use DEFAULT command, if any
            if (_state.action == Verb.NONE)
            {
                Verb v = State.FindDefaultVerb(od.state);
                _cmdText.SetVerb((v == Verb.NONE) ? Verb.WALK_TO : v);
                if (_state.noun == 0)
                {
                    _cmdText.Clear();
                }
            }
            var name = _vm.Logic.ObjectName((ushort)od.name);
            _cmdText.DisplayTemp(InkColor.INK_CMD_NORMAL, name, false);
            _state.oldNoun = _state.noun;
        }

        private void GrabSelectedVerb()
        {
            if (IsVerbInvScroll(_state.verb))
            {
                // move through inventory (by four if right mouse button)
                ushort scroll = (ushort)((_mouseKey == Input.MOUSE_RBUTTON) ? 4 : 1);
                _vm.Logic.InventoryScroll(scroll, _state.verb == Verb.SCROLL_UP);
            }
            else
            {
                _state.action = _state.verb;
                _state.subject[0] = 0;
                _state.subject[1] = 0;

                if (_vm.Logic.JoeWalk == JoeWalkMode.MOVE && _state.verb != Verb.NONE)
                {
                    _vm.Logic.JoeWalk = JoeWalkMode.NORMAL;
                }
                _state.commandLevel = 1;
                _state.oldVerb = Verb.NONE;
                _state.oldNoun = 0;
                _cmdText.SetVerb(_state.verb);
                _cmdText.Display(InkColor.INK_CMD_NORMAL);
            }
        }

        private ObjectData FindObjectData(ushort objRoomNum)
        {
            ObjectData od = null;
            if (objRoomNum != 0)
            {
                objRoomNum += _vm.Logic.CurrentRoomData;
                od = _vm.Logic.ObjectData[objRoomNum];
            }
            return od;
        }
    }
}

