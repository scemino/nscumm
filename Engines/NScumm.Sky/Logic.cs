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

using NScumm.Core;
using NScumm.Sky.Music;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NScumm.Core.Graphics;

namespace NScumm.Sky
{
    partial class Logic
    {
        class VariableCollection : IList<uint>
        {
            public uint[] Variables { get; } = new uint[NumSkyScriptVars];

            public IEnumerator<uint> GetEnumerator()
            {
                return (IEnumerator<uint>)Variables.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Variables.GetEnumerator();
            }

            void ICollection<uint>.Add(uint item)
            {
                throw new InvalidOperationException();
            }

            public void Clear()
            {
                throw new InvalidOperationException();
            }

            public bool Contains(uint item)
            {
                return Array.IndexOf(Variables, item) != -1;
            }

            public void CopyTo(uint[] array, int arrayIndex)
            {
                Variables.CopyTo(array, arrayIndex);
            }

            public bool Remove(uint item)
            {
                throw new InvalidOperationException();
            }

            public int Count
            {
                get { return Variables.Length; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public int IndexOf(uint item)
            {
                return Array.IndexOf(Variables, item);
            }

            public void Insert(int index, uint item)
            {
                throw new InvalidOperationException();
            }

            public void RemoveAt(int index)
            {
                throw new InvalidOperationException();
            }

            public uint this[int index]
            {
                get { return Variables[index]; }
                set { Variables[index] = value; }
            }
        }

        public const int NumSkyScriptVars = 838;
        private readonly SkyCompact _skyCompact;
        private readonly Screen _skyScreen;
        private readonly Disk _skyDisk;
        private readonly Text _skyText;
        private readonly MusicBase _skyMusic;
        private readonly Sound _skySound;
        private readonly Mouse _skyMouse;
        private readonly VariableCollection _scriptVariables = new VariableCollection();
        private uint _currentSection;
        private Action[] _logicTable;
        private Compact _compact;
        private readonly Grid _skyGrid;
        private readonly byte[][] _moduleList = new byte[16][];
        private readonly uint[] _stack = new uint[20];
        private int _stackPtr;
        private Func<uint, uint, uint, bool>[] _mcodeTable;
        private readonly uint[] _objectList = new uint[30];
        private readonly Random _rnd = new Random(Environment.TickCount);
        private readonly AutoRoute _skyAutoRoute;

        public Control Control { get; internal set; }

        public IList<uint> ScriptVariables
        {
            get { return _scriptVariables; }
        }

        public Grid Grid
        {
            get { return _skyGrid; }
        }

        public Logic(SkyCompact skyCompact, Screen skyScreen, Disk skyDisk, Text skyText, MusicBase skyMusic, Mouse skyMouse, Sound skySound)
        {
            _skyCompact = skyCompact;
            _skyScreen = skyScreen;
            _skyDisk = skyDisk;
            _skyText = skyText;
            _skyMusic = skyMusic;
            _skySound = skySound;
            _skyMouse = skyMouse;

            _skyGrid = new Grid(this, _skyDisk, _skyCompact);
            _skyAutoRoute = new AutoRoute(_skyGrid, _skyCompact);

            SetupLogicTable();
            SetupMcodeTable();

            _currentSection = 0xFF; //force music & sound reload
            InitScriptVariables();
        }

        public void ParseSaveData(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                if (!SkyEngine.IsDemo)
                    FnLeaveSection(_scriptVariables[CUR_SECTION], 0, 0);
                for (ushort cnt = 0; cnt < NumSkyScriptVars; cnt++)
                    _scriptVariables[cnt] = reader.ReadUInt32();
                FnEnterSection(_scriptVariables[CUR_SECTION], 0, 0);
            }
        }

        private void InitScriptVariables()
        {
            _scriptVariables[LOGIC_LIST_NO] = 141;
            _scriptVariables[LAMB_GREET] = 62;
            _scriptVariables[JOEY_SECTION] = 1;
            _scriptVariables[LAMB_SECTION] = 2;
            _scriptVariables[S15_FLOOR] = 8371;
            _scriptVariables[GUARDIAN_THERE] = 1;
            _scriptVariables[DOOR_67_68_FLAG] = 1;
            _scriptVariables[SC70_IRIS_FLAG] = 3;
            _scriptVariables[DOOR_73_75_FLAG] = 1;
            _scriptVariables[SC76_CABINET1_FLAG] = 1;
            _scriptVariables[SC76_CABINET2_FLAG] = 1;
            _scriptVariables[SC76_CABINET3_FLAG] = 1;
            _scriptVariables[DOOR_77_78_FLAG] = 1;
            _scriptVariables[SC80_EXIT_FLAG] = 1;
            _scriptVariables[SC31_LIFT_FLAG] = 1;
            _scriptVariables[SC32_LIFT_FLAG] = 1;
            _scriptVariables[SC33_SHED_DOOR_FLAG] = 1;
            _scriptVariables[BAND_PLAYING] = 1;
            _scriptVariables[COLSTON_AT_TABLE] = 1;
            _scriptVariables[SC36_NEXT_DEALER] = 16731;
            _scriptVariables[SC36_DOOR_FLAG] = 1;
            _scriptVariables[SC37_DOOR_FLAG] = 2;
            _scriptVariables[SC40_LOCKER_1_FLAG] = 1;
            _scriptVariables[SC40_LOCKER_2_FLAG] = 1;
            _scriptVariables[SC40_LOCKER_3_FLAG] = 1;
            _scriptVariables[SC40_LOCKER_4_FLAG] = 1;
            _scriptVariables[SC40_LOCKER_5_FLAG] = 1;

            if (SystemVars.Instance.GameVersion.Version.Minor == 288)
            {
                forwardList1b288.CopyTo(_scriptVariables.Variables, 352);
            }
            else
            {
                forwardList1b.CopyTo(_scriptVariables.Variables, 352);
            }

            forwardList2b.CopyTo(_scriptVariables.Variables, 656);
            forwardList3b.CopyTo(_scriptVariables.Variables, 721);
            forwardList4b.CopyTo(_scriptVariables.Variables, 663);
            forwardList5b.CopyTo(_scriptVariables.Variables, 505);
        }

        public void InitScreen0()
        {
            FnEnterSection(0, 0, 0);
            _skyMusic.StartMusic(2);
            SystemVars.Instance.CurrentMusic = 2;
        }

        public void Engine()
        {
            do
            {
                var raw = _skyCompact.FetchCptRaw((ushort)_scriptVariables[LOGIC_LIST_NO]);
                var logicList = new UShortAccess(raw, 0);
                ushort id;
                while ((id = logicList[0]) != 0)
                {
                    logicList.Offset += 2;
                    // 0 means end of list
                    if (id == 0xffff)
                    {
                        // Change logic data address
                        raw = _skyCompact.FetchCptRaw(logicList[0]);
                        logicList = new UShortAccess(raw, 0);
                        continue;
                    }

                    _scriptVariables[CUR_ID] = id;
                    _compact = _skyCompact.FetchCpt(id);

                    // check the id actually wishes to be processed
                    if ((_compact.Core.status & (1 << 6)) == 0)
                        continue;

                    // ok, here we process the logic bit system

                    if ((_compact.Core.status & (1 << 7)) != 0)
                        _skyGrid.RemoveObjectFromWalk(_compact);

                    Debug.Instance.Logic(_compact.Core.logic);
                    _logicTable[_compact.Core.logic]();

                    if ((_compact.Core.status & (1 << 7)) != 0)
                        _skyGrid.ObjectToWalk(_compact);

                    // a sync sent to the compact is available for one cycle
                    // only. that cycle has just ended so remove the sync.
                    // presumably the mega has just reacted to it.
                    _compact.Core.sync = 0;
                }
                // usually this loop is run only once, it'll only be run a second time if the game
                // script just asked the user to enter a copy protection code.
                // this is done to prevent the copy protection screen from flashing up.
                // (otherwise it would be visible for 1/50 second)
            } while (CheckProtection());
        }

        public ushort MouseScript(uint scrNum, Compact scriptComp)
        {
            var tmpComp = _compact;
            _compact = scriptComp;
            var retVal = Script((ushort)(scrNum & 0xFFFF), (ushort)(scrNum >> 16));
            _compact = tmpComp;

            if (scrNum == MENU_SELECT || (scrNum >= LINC_MENU_SELECT && scrNum <= DOC_MENU_SELECT))
            {
                // HACK: See patch #1689516 for details. The short story:
                // The user has clicked on an inventory item.  We update the
                // mouse cursor instead of waiting for the script to update it.
                // In the original game the cursor is just updated when the mouse
                // moves away the item, but it's unintuitive.
                FnCrossMouse(0, 0, 0);
            }

            return retVal;
        }

        private bool CheckProtection()
        {
            if (_scriptVariables[ENTER_DIGITS] != 0)
            {
                if (_scriptVariables[CONSOLE_TYPE] == 5) // reactor code
                    _scriptVariables[FS_COMMAND] = 240;
                else                                     // copy protection
                    _scriptVariables[FS_COMMAND] = 337;
                _scriptVariables[ENTER_DIGITS] = 0;
                return true;
            }
            return false;
        }

        private bool FnEnterSection(uint sectionNo, uint b, uint c)
        {
            if (SkyEngine.IsDemo && (sectionNo > 2))
                Control.ShowGameQuitMsg();

            _scriptVariables[CUR_SECTION] = sectionNo;
            SystemVars.Instance.CurrentMusic = 0;

            if (sectionNo == 5) //linc section - has different mouse icons
                _skyMouse.ReplaceMouseCursors(60302);

            if ((sectionNo != _currentSection) || SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.GameRestored))
            {
                _currentSection = sectionNo;

                sectionNo++;
                _skyMusic.LoadSection((byte)sectionNo);
                _skySound.LoadSection((byte)sectionNo);
                _skyGrid.LoadGrids();
                SystemVars.Instance.SystemFlags &= ~SystemFlags.GameRestored;
            }

            return true;
        }

        private bool FnLeaveSection(uint sectionNo, uint b, uint c)
        {
            if (SkyEngine.IsDemo)
            {
                SkyEngine.QuitGame();
            }

            if (sectionNo == 5) //linc section - has different mouse icons
                _skyMouse.ReplaceMouseCursors(60301);

            return true;
        }

        private bool Collide(Compact cpt)
        {
            MegaSet m1 = SkyCompact.GetMegaSet(_compact);
            MegaSet m2 = SkyCompact.GetMegaSet(cpt);

            // target's base coordinates
            ushort x = (ushort)(cpt.Core.xcood & 0xfff8);
            ushort y = (ushort)(cpt.Core.ycood & 0xfff8);

            // The collision is direction dependent
            switch (_compact.Core.dir)
            {
                case 0: // looking up
                    x -= m1.colOffset; // compensate for inner x offsets
                    x += m2.colOffset;

                    if (x + m2.colWidth < _compact.Core.xcood) // their rightmost
                        return false;

                    x -= m1.colWidth; // our left, their right
                    if (x >= _compact.Core.xcood)
                        return false;

                    y += 8; // bring them down a line
                    if (y == _compact.Core.ycood)
                        return true;

                    y += 8; // bring them down a line
                    if (y == _compact.Core.ycood)
                        return true;

                    return false;
                case 1: // looking down
                    x -= m1.colOffset; // compensate for inner x offsets
                    x += m2.colOffset;

                    if (x + m2.colWidth < _compact.Core.xcood) // their rightmoast
                        return false;

                    x -= m1.colWidth; // our left, their right
                    if (x >= _compact.Core.xcood)
                        return false;

                    y -= 8; // bring them up a line
                    if (y == _compact.Core.ycood)
                        return true;

                    y -= 8; // bring them up a line
                    if (y == _compact.Core.ycood)
                        return true;

                    return false;
                case 2: // looking left

                    if (y != _compact.Core.ycood)
                        return false;

                    x += m2.lastChr;
                    if (x == _compact.Core.xcood)
                        return true;

                    x -= 8; // out another one
                    if (x == _compact.Core.xcood)
                        return true;

                    return false;
                case 3: // looking right
                case 4: // talking (not sure if this makes sense...)

                    if (y != _compact.Core.ycood)
                        return false;

                    x -= m1.lastChr; // last block
                    if (x == _compact.Core.xcood)
                        return true;

                    x -= 8; // out another block
                    if (x != _compact.Core.xcood)
                        return false;

                    return true;
                default:
                    throw new InvalidOperationException(string.Format("Unknown Direction: {0}", _compact.Core.dir));
            }
        }

        private void MainAnim()
        {
            // Extension of arAnim()
            _compact.Core.waitingFor = 0; // clear possible zero-zero skip

            var sequence = _skyCompact.GetGrafixPtr(_compact);
            if (sequence[0] == 0)
            {
                // ok, move to new anim segment
                sequence.Offset += 4;
                _compact.Core.grafixProgPos += 2;
                if (sequence[0] == 0)
                { // end of route?
                  // ok, sequence has finished

                    // will start afresh if new sequence continues in last direction
                    _compact.Core.arAnimIndex = 0;

                    _compact.Core.downFlag = 0; // pass back ok to script
                    _compact.Core.logic = L_SCRIPT;
                    LogicScript();
                    return;
                }

                _compact.Core.arAnimIndex = 0; // reset position
            }

            ushort dir;
            while ((dir = _compact.Core.dir) != sequence[1])
            {
                // ok, setup turning
                _compact.Core.dir = sequence[1];

                var tt = _skyCompact.GetTurnTable(_compact, dir);
                if (tt[_compact.Core.dir] != 0)
                {
                    _compact.Core.turnProgId = tt[_compact.Core.dir];
                    _compact.Core.turnProgPos = 0;
                    _compact.Core.logic = L_AR_TURNING;
                    ArTurn();
                    return;
                }
            }

            var animId = _skyCompact.GetCompactElem(_compact, (ushort)(C_ANIM_UP + _compact.Core.megaSet + dir * 4)).Field;
            var animList = new UShortAccess(_skyCompact.FetchCptRaw(animId), 0);

            ushort arAnimIndex = _compact.Core.arAnimIndex;
            if (animList[arAnimIndex / 2] == 0)
            {
                arAnimIndex = 0;
                _compact.Core.arAnimIndex = 0; // reset
            }

            _compact.Core.arAnimIndex += S_LENGTH;

            sequence[0] -= animList[(S_COUNT + arAnimIndex) / 2]; // reduce the distance to travel
            _compact.Core.frame = animList[(S_FRAME + arAnimIndex) / 2]; // new graphic frame
            _compact.Core.xcood += animList[(S_AR_X + arAnimIndex) / 2]; // update x coordinate
            _compact.Core.ycood += animList[(S_AR_Y + arAnimIndex) / 2]; // update y coordinate
        }

        public ushort Script(ushort scriptNo, ushort offset)
        {
            do
            {
                bool restartScript = false;

                // process a script
                // low level interface to interpreter

                ushort moduleNo = (ushort)(scriptNo >> 12);
                var data = _moduleList[moduleNo]; // get module address

                if (data == null)
                { // We need to load the script module
                    _moduleList[moduleNo] = _skyDisk.LoadScriptFile((ushort)(moduleNo + F_MODULE_0));
                    data = _moduleList[moduleNo]; // module has been loaded
                }

                var scriptData = new UShortAccess(data, 0);

                Debug.Instance.Write("Doing Script: {0}:{1}:{2:X}", moduleNo, scriptNo & 0xFFF, offset != 0 ? offset - scriptData[scriptNo & 0xFFF] : 0);

                // WORKAROUND for bug #3149412: "Invalid Mode when giving shades to travel agent"
                // Using the dark glasses on Trevor (travel agent) multiple times in succession would
                // wreck the trevor compact's mode, as the script in question doesn't account for using
                // this item at this point in the game (you will only have it here if you play the game
                // in an unusual way) and thus would loop indefinitely / never drop out.
                // To prevent this, we trigger the generic response by pretending we're using an item
                // which the script /does/ handle.
                if (scriptNo == TREVOR_SPEECH && _scriptVariables[OBJECT_HELD] == IDO_SHADES)
                    _scriptVariables[OBJECT_HELD] = IDO_GLASS;


                // Check whether we have an offset or what
                if (offset != 0)
                    scriptData = new UShortAccess(data, offset * 2);
                else
                    scriptData.Offset += scriptData[scriptNo & 0x0FFF] * 2;

                uint b = 0, c = 0;

                while (!restartScript)
                {
                    var command = scriptData.Value; scriptData.Offset += 2; // get a command
                    Debug.Instance.Script(command, scriptData);

                    uint a;
                    ushort s;
                    switch (command)
                    {
                        case 0: // push_variable
                            Push(_scriptVariables[scriptData.Value / 4]); scriptData.Offset += 2;
                            break;
                        case 1: // less_than
                            a = Pop();
                            b = Pop();
                            if (b < a)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 2: // push_number
                            Push(scriptData.Value); scriptData.Offset += 2;
                            break;
                        case 3: // not_equal
                            a = Pop();
                            b = Pop();
                            if (a != b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 4: // if_and
                            a = Pop();
                            b = Pop();
                            if (a != 0 && b != 0)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 5: // skip_zero
                            s = scriptData.Value; scriptData.Offset += 2;

                            a = Pop();
                            if (a == 0)
                                scriptData.Offset += s;
                            break;
                        case 6: // pop_var
                            b = _scriptVariables[scriptData.Value / 4] = Pop(); scriptData.Offset += 2;
                            break;
                        case 7: // minus
                            a = Pop();
                            b = Pop();
                            Push(b - a);
                            break;
                        case 8: // plus
                            a = Pop();
                            b = Pop();
                            Push(b + a);
                            break;
                        case 9: // skip_always
                            s = scriptData.Value; scriptData.Offset += 2;
                            scriptData.Offset += s;
                            break;
                        case 10: // if_or
                            a = Pop();
                            b = Pop();
                            if (a != 0 || b != 0)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 11: // call_mcode
                            {
                                a = scriptData.Value; scriptData.Offset += 2;
                                System.Diagnostics.Debug.Assert(a <= 3);
                                switch (a)
                                {
                                    case 3:
                                        c = Pop();
                                        b = Pop();
                                        a = Pop();
                                        break;
                                    case 2:
                                        b = Pop();
                                        a = Pop();
                                        break;
                                    case 1:
                                        a = Pop();
                                        break;
                                }

                                ushort mcode = (ushort)(scriptData.Value / 4); scriptData.Offset += 2; // get mcode number 

                                Debug.Instance.Mcode(mcode, a, b, c);

                                var saveCpt = _compact;
                                bool ret = _mcodeTable[mcode](a, b, c);
                                _compact = saveCpt;

                                if (!ret)
                                    return (ushort)(scriptData.Offset / 2);
                            }
                            break;
                        case 12: // more_than
                            a = Pop();
                            b = Pop();
                            if (b > a)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 14: // switch
                            c = s = scriptData.Value; scriptData.Offset += 2; // get number of cases

                            a = Pop(); // and value to switch on

                            do
                            {
                                if (a == scriptData.Value)
                                {
                                    scriptData.Offset += scriptData[1];
                                    scriptData.Offset += 2;
                                    break;
                                }
                                scriptData.Offset += 4;
                            } while (--s != 0);

                            if (s == 0)
                                scriptData.Offset += scriptData.Value; // use the default
                            break;
                        case 15: // push_offset
                            {
                                var elem = _skyCompact.GetCompactElem(_compact, scriptData.Value);
                                Push(elem.Field); scriptData.Offset += 2;
                            }
                            break;
                        case 16: // pop_offset
                            {
                                // pop a value into a compact
                                var elem = _skyCompact.GetCompactElem(_compact, scriptData.Value); scriptData.Offset += 2;
                                elem.Field = (ushort)Pop();
                            }
                            break;
                        case 17: // is_equal
                            a = Pop();
                            b = Pop();
                            if (a == b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 18:
                            { // skip_nz
                                short t = (short)scriptData.Value; scriptData.Offset += 2;
                                a = Pop();
                                if (a != 0)
                                    scriptData.Offset += t;
                                break;
                            }
                        case 13:
                        case 19: // script_exit
                            return 0;
                        case 20: // restart_script
                            offset = 0;
                            restartScript = true;
                            break;
                        default:
                            throw new InvalidOperationException(string.Format("Unknown script command: {0}", command));
                    }
                }
            } while (true);
        }

        private uint Pop()
        {
            if (_stackPtr < 1 || _stackPtr > _stack.Length - 1)
                throw new InvalidOperationException("No items on Stack to pop");
            return _stack[--_stackPtr];
        }

        private void Push(uint a)
        {
            if (_stackPtr > _stack.Length - 2)
                throw new InvalidOperationException("Stack overflow");
            _stack[_stackPtr++] = a;
        }

        private void StdSpeak(Compact target, uint textNum, uint animNum)
        {
            animNum += (uint)(target.Core.megaSet / NEXT_MEGA_SET);
            animNum &= 0xFF;

            var talkTable = new UShortAccess(_skyCompact.FetchCptRaw((ushort)CptIds.TalkTableList), 0);
            target.Core.grafixProgId = talkTable[(int)animNum];
            target.Core.grafixProgPos = 0;
            var animPtr = _skyCompact.GetGrafixPtr(target);

            if (animPtr != null)
            {
                target.Core.offset = animPtr[0]; animPtr.Offset += 2;
                target.Core.getToFlag = animPtr[0]; animPtr.Offset += 2;
                target.Core.grafixProgPos += 2;
            }
            else
                target.Core.grafixProgId = 0;

            bool speechFileFound = false;
            if (SkyEngine.IsCDVersion)
                speechFileFound = _skySound.StartSpeech((ushort)textNum);


            // Set the focus region to that area
            // Calculate the point where the character is
            int x = target.Core.xcood - TOP_LEFT_X;
            int y = target.Core.ycood - TOP_LEFT_Y;
            // TODO: Make the box size change based on the object that has the focus
            _skyScreen.SetFocusRectangle(new Rect(x, y, 192, 128));


            if (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.AllowText) || !speechFileFound)
            {
                // form the text sprite, if player wants subtitles or
                // if we couldn't find the speech file
                var textInfo = _skyText.LowTextManager(textNum, FIXED_TEXT_WIDTH, 0, (byte)target.Core.spColor, true);
                var textCompact = _skyCompact.FetchCpt(textInfo.CompactNum);
                target.Core.spTextId = textInfo.CompactNum; //So we know what text to kill
                var textGfxHeader = ServiceLocator.Platform.ToStructure<DataFileHeader>(textInfo.TextData, 0);

                textCompact.Core.screen = target.Core.screen;   //put it on our screen

                if (_scriptVariables[SCREEN] == target.Core.screen)
                { // Only use coordinates if we are on the current screen
                  //talking on-screen
                  //create the x coordinate for the speech text
                  //we need the talkers sprite information
                    var targetGfx = SkyEngine.ItemList[target.Core.frame >> 6];
                    var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(targetGfx, 0);
                    ushort xPos = (ushort)(target.Core.xcood + header.s_offset_x);
                    ushort width = (ushort)(header.s_width >> 1);

                    xPos += (ushort)(width - FIXED_TEXT_WIDTH / 2); //middle of talker

                    if (xPos < TOP_LEFT_X)
                        xPos = TOP_LEFT_X;

                    width = (ushort)(xPos + FIXED_TEXT_WIDTH);
                    if (TOP_LEFT_X + FULL_SCREEN_WIDTH <= width)
                    {
                        xPos = TOP_LEFT_X + FULL_SCREEN_WIDTH;
                        xPos -= FIXED_TEXT_WIDTH;
                    }

                    textCompact.Core.xcood = xPos;
                    ushort yPos = (ushort)(target.Core.ycood + (header.s_offset_y - 6 - textGfxHeader.s_height));

                    if (yPos < TOP_LEFT_Y)
                        yPos = TOP_LEFT_Y;

                    textCompact.Core.ycood = yPos;

                }
                else
                {
                    //talking off-screen
                    target.Core.spTextId = 0;   //don't kill any text 'cos none was made
                    textCompact.Core.status = 0;    //don't display text
                }
                // In CD version, we're doing the timing by checking when the VOC has stopped playing.
                // Setting spTime to 10 thus means that we're doing a pause of 10 gamecycles between
                // each sentence.
                if (speechFileFound)
                    target.Core.spTime = 10;
                else
                    target.Core.spTime = (ushort)(_skyText.NumLetters + 5);
            }
            else
            {
                target.Core.spTime = 10;
                target.Core.spTextId = 0;
            }
            target.Core.logic = L_TALK;
        }

        private void StopAndWait()
        {
            _compact.Core.mode += 4;

            var scriptNo = SkyCompact.GetSub(_compact, _compact.Core.mode);
            var offset = SkyCompact.GetSub(_compact, _compact.Core.mode + 2);

            scriptNo.Field = _compact.Core.stopScript;
            offset.Field = 0;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        static readonly uint[] forwardList1b = {
            JOBS_SPEECH,
            JOBS_S4,
            JOBS_ALARMED,
            JOEY_RECYCLE,
            SHOUT_SSS,
            JOEY_MISSION,
            TRANS_MISSION,
            SLOT_MISSION,
            CORNER_MISSION,
            JOEY_LOGIC,
            GORDON_SPEECH,
            JOEY_BUTTON_MISSION,
            LOB_DAD_SPEECH,
            LOB_SON_SPEECH,
            GUARD_SPEECH,
            MANTRACH_SPEECH,
            WRECK_SPEECH,
            ANITA_SPEECH,
            LAMB_FACTORY,
            FORE_SPEECH,
            JOEY_42_MISS,
            JOEY_JUNCTION_MISS,
            WELDER_MISSION,
            JOEY_WELD_MISSION,
            RADMAN_SPEECH,
            LINK_7_29,
            LINK_29_7,
            LAMB_TO_3,
            LAMB_TO_2,
            BURKE_SPEECH,
            BURKE_1,
            BURKE_2,
            DR_BURKE_1,
            JASON_SPEECH,
            JOEY_BELLEVUE,
            ANCHOR_SPEECH,
            ANCHOR_MISSION,
            JOEY_PC_MISSION,
            HOOK_MISSION,
            TREVOR_SPEECH,
            JOEY_FACTORY,
            HELGA_SPEECH,
            JOEY_HELGA_MISSION,
            GALL_BELLEVUE,
            GLASS_MISSION,
            LAMB_FACT_RETURN,
            LAMB_LEAVE_GARDEN,
            LAMB_START_29,
            LAMB_BELLEVUE,
            CABLE_MISSION,
            FOSTER_TOUR,
            LAMB_TOUR,
            FOREMAN_LOGIC,
            LAMB_LEAVE_FACTORY,
            LAMB_BELL_LOGIC,
            LAMB_FACT_2,
            START90,
            0,
            0,
            LINK_28_31,
            LINK_31_28,
            EXIT_LINC,
            DEATH_SCRIPT
        };

        public const int RESULT = 0;
        public const int SCREEN = 1;
        public const int LOGIC_LIST_NO = 2;
        public const int MOUSE_LIST_NO = 6;
        public const int DRAW_LIST_NO = 8;
        public const int CUR_ID = 12;
        public const int MOUSE_STATUS = 13;
        public const int MOUSE_STOP = 14;
        public const int BUTTON = 15;
        public const int SPECIAL_ITEM = 17;
        public const int GET_OFF = 18;
        public const int CURSOR_ID = 22;
        public const int SAFEX = 25;
        public const int SAFEY = 26;
        public const int PLAYER_X = 27;
        public const int PLAYER_Y = 28;
        public const int PLAYER_MOOD = 29;
        public const int PLAYER_SCREEN = 30;
        public const int HIT_ID = 37;
        public const int LAYER_0_ID = 41;
        public const int LAYER_1_ID = 42;
        public const int LAYER_2_ID = 43;
        public const int LAYER_3_ID = 44;
        public const int GRID_1_ID = 45;
        public const int GRID_2_ID = 46;
        public const int GRID_3_ID = 47;
        public const int THE_CHOSEN_ONE = 51;
        public const int TEXT1 = 53;
        public const int MENU_LENGTH = 100;
        public const int SCROLL_OFFSET = 101;
        public const int MENU = 102;
        public const int OBJECT_HELD = 103;
        public const int LAMB_GREET = 109;
        public const int RND = 115;
        public const int CUR_SECTION = 143;
        public const int JOEY_SECTION = 145;
        public const int LAMB_SECTION = 146;
        public const int KNOWS_PORT = 190;
        public const int GOT_SPONSOR = 240;
        public const int GOT_JAMMER = 258;
        public const int CONSOLE_TYPE = 345;
        public const int S15_FLOOR = 450;
        public const int FOREMAN_FRIEND = 451;
        public const int REICH_DOOR_FLAG = 470;
        public const int CARD_STATUS = 479;
        public const int CARD_FIX = 480;
        public const int GUARDIAN_THERE = 640;
        public const int FS_COMMAND = 643;
        public const int ENTER_DIGITS = 644;
        public const int LINC_DIGIT_0 = 646;
        public const int LINC_DIGIT_1 = 647;
        public const int LINC_DIGIT_2 = 648;
        public const int LINC_DIGIT_3 = 649;
        public const int LINC_DIGIT_4 = 650;
        public const int LINC_DIGIT_5 = 651;
        public const int LINC_DIGIT_6 = 651;
        public const int LINC_DIGIT_7 = 653;
        public const int LINC_DIGIT_8 = 654;
        public const int LINC_DIGIT_9 = 655;
        public const int DOOR_67_68_FLAG = 678;
        public const int SC70_IRIS_FLAG = 693;
        public const int DOOR_73_75_FLAG = 704;
        public const int SC76_CABINET1_FLAG = 709;
        public const int SC76_CABINET2_FLAG = 710;
        public const int SC76_CABINET3_FLAG = 711;
        public const int DOOR_77_78_FLAG = 719;
        public const int SC80_EXIT_FLAG = 720;
        public const int SC31_LIFT_FLAG = 793;
        public const int SC32_LIFT_FLAG = 797;
        public const int SC33_SHED_DOOR_FLAG = 798;
        public const int BAND_PLAYING = 804;
        public const int COLSTON_AT_TABLE = 805;
        public const int SC36_NEXT_DEALER = 806;
        public const int SC36_DOOR_FLAG = 807;
        public const int SC37_DOOR_FLAG = 808;
        public const int SC40_LOCKER_1_FLAG = 817;
        public const int SC40_LOCKER_2_FLAG = 818;
        public const int SC40_LOCKER_3_FLAG = 819;
        public const int SC40_LOCKER_4_FLAG = 820;
        public const int SC40_LOCKER_5_FLAG = 821;

        static readonly uint[] forwardList1b288 = {
            JOBS_SPEECH,
            JOBS_S4,
            JOBS_ALARMED,
            JOEY_RECYCLE,
            SHOUT_SSS,
            JOEY_MISSION,
            TRANS_MISSION,
            SLOT_MISSION,
            CORNER_MISSION,
            JOEY_LOGIC,
            GORDON_SPEECH,
            JOEY_BUTTON_MISSION,
            LOB_DAD_SPEECH,
            LOB_SON_SPEECH,
            GUARD_SPEECH,
            0x68,
            WRECK_SPEECH,
            ANITA_SPEECH,
            LAMB_FACTORY,
            FORE_SPEECH,
            JOEY_42_MISS,
            JOEY_JUNCTION_MISS,
            WELDER_MISSION,
            JOEY_WELD_MISSION,
            RADMAN_SPEECH,
            LINK_7_29,
            LINK_29_7,
            LAMB_TO_3,
            LAMB_TO_2,
            0x3147,
            0x3100,
            0x3101,
            0x3102,
            0x3148,
            0x3149,
            0x314A,
            0x30C5,
            0x30C6,
            0x30CB,
            0x314B,
            JOEY_FACTORY,
            0x314C,
            0x30E2,
            0x314D,
            0x310C,
            LAMB_FACT_RETURN,
            0x3139,
            0x313A,
            0x004F,
            CABLE_MISSION,
            FOSTER_TOUR,
            LAMB_TOUR,
            FOREMAN_LOGIC,
            LAMB_LEAVE_FACTORY,
            0x3138,
            LAMB_FACT_2,
            0x004D,
            0,
            0,
            LINK_28_31,
            LINK_31_28,
            0x004E,
            DEATH_SCRIPT
        };

        static readonly uint[] forwardList2b = {
            STD_ON,
            STD_EXIT_LEFT_ON,
            STD_EXIT_RIGHT_ON,
            ADVISOR_188,
            SHOUT_ACTION,
            MEGA_CLICK,
            MEGA_ACTION
        };

        static readonly uint[] forwardList3b = {
            DANI_SPEECH,
            DANIELLE_GO_HOME,
            SPUNKY_GO_HOME,
            HENRI_SPEECH,
            BUZZER_SPEECH,
            FOSTER_VISIT_DANI,
            DANIELLE_LOGIC,
            JUKEBOX_SPEECH,
            VINCENT_SPEECH,
            EDDIE_SPEECH,
            BLUNT_SPEECH,
            DANI_ANSWER_PHONE,
            SPUNKY_SEE_VIDEO,
            SPUNKY_BARK_AT_FOSTER,
            SPUNKY_SMELLS_FOOD,
            BARRY_SPEECH,
            COLSTON_SPEECH,
            GALL_SPEECH,
            BABS_SPEECH,
            CHUTNEY_SPEECH,
            FOSTER_ENTER_COURT
        };

        static readonly uint[] forwardList4b = {
            WALTER_SPEECH,
            JOEY_MEDIC,
            JOEY_MED_LOGIC,
            JOEY_MED_MISSION72,
            KEN_LOGIC,
            KEN_SPEECH,
            KEN_MISSION_HAND,
            SC70_IRIS_OPENED,
            SC70_IRIS_CLOSED,
            FOSTER_ENTER_BOARDROOM,
            BORED_ROOM,
            FOSTER_ENTER_NEW_BOARDROOM,
            HOBS_END,
            SC82_JOBS_SSS
        };

        static readonly uint[] forwardList5b = {
            SET_UP_INFO_WINDOW,
            SLAB_ON,
            UP_MOUSE,
            DOWN_MOUSE,
            LEFT_MOUSE,
            RIGHT_MOUSE,
            DISCONNECT_FOSTER
        };
    }
}