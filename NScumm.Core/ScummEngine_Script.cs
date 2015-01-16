//
//  ScummEngine_Script.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        byte[] _currentScriptData;
        byte _currentScript;
        int _currentPos;
        int _numNestedScripts;
        NestedScript[] _nest;
        ScriptSlot[] _slots;
        internal CutScene cutScene = new CutScene();
        const int NumLocalScripts = 60;

        protected byte CurrentScript { get { return _currentScript; } set { _currentScript = value; } }

        protected int CurrentPos { get { return _currentPos; } set { _currentPos = value; } }

        internal ScriptSlot[] Slots { get { return _slots; } }

        void ChainScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var vars = GetWordVarArgs();
            var cur = _currentScript;

            _slots[cur].Number = 0;
            _slots[cur].Status = ScriptStatus.Dead;
            _currentScript = 0xFF;

            RunScript((byte)script, _slots[cur].FreezeResistant, _slots[cur].Recursive, vars);
        }

        protected void UnfreezeScripts()
        {
            for (var i = 0; i < NumScriptSlot; i++)
            {
                _slots[i].Unfreeze();
            }

            for (var i = 0; i < _sentence.Length; i++)
            {
                _sentence[i].Unfreeze();
            }
        }

        void BeginOverride()
        {
            if (ReadByte() != 0)
                BeginOverrideCore();
            else
                EndOverrideCore();
        }

        protected void BeginOverrideCore()
        {
            cutScene.Override.Pointer = _currentPos;
            cutScene.Override.Script = _currentScript;

            // Skip the jump instruction following the override instruction
            // (the jump is responsible for "skipping" cutscenes, and the reason
            // why we record the current script position in vm.cutScenePtr).
            ReadByte();
            ReadWord();

            if (Game.Version >= 5)
            {
                Variables[VariableOverride.Value] = 0;
            }
        }

        protected void EndOverrideCore()
        {
            cutScene.Override.Pointer = 0;
            cutScene.Override.Script = 0;

            if (Game.Version >= 4)
            {
                _variables[VariableOverride.Value] = 0;
            }
        }

        protected void BeginCutscene(int[] args)
        {
            var scr = _currentScript;
            _slots[scr].CutSceneOverride++;

            var cutSceneData = new CutSceneData
            {
                Data = args.Length > 0 ? args[0] : 0
            };
            cutScene.Data.Push(cutSceneData);

            if (cutScene.Data.Count >= MaxCutsceneNum)
                throw new NotSupportedException("Cutscene stack overflow");

            cutScene.CutSceneScriptIndex = scr;

            if (_variables[VariableCutSceneStartScript.Value] != 0)
                RunScript((byte)_variables[VariableCutSceneStartScript.Value], false, false, args);

            cutScene.CutSceneScriptIndex = 0xFF;
        }

        protected void AbortCutscene()
        {
            byte script = 0;
            var offs = 0;
            if (cutScene.Override.Pointer != 0)
            {
                offs = cutScene.Override.Pointer;
                script = cutScene.Override.Script;
            }

            if (offs != 0)
            {
                _slots[script].Offset = (uint)offs;
                _slots[script].Status = ScriptStatus.Running;
                _slots[script].UnfreezeAll();

                if (_slots[script].CutSceneOverride > 0)
                    _slots[script].CutSceneOverride--;

                _variables[VariableOverride.Value] = 1;
                cutScene.Override.Pointer = 0;
            }
        }

        void CutScene()
        {
            var args = GetWordVarArgs();
            BeginCutscene(args);
        }

        protected void EndCutsceneCore()
        {
            if (_slots[_currentScript].CutSceneOverride > 0)    // Only terminate if active
				_slots[_currentScript].CutSceneOverride--;

            var cutSceneData = cutScene.Data.Pop();
            var args = new [] { cutSceneData.Data };

            _variables[VariableOverride.Value] = 0;

            if (cutSceneData.Pointer != 0 && (_slots[_currentScript].CutSceneOverride > 0))   // Only terminate if active
				_slots[_currentScript].CutSceneOverride--;

            if (_variables[VariableCutSceneEndScript.Value] != 0)
                RunScript((byte)_variables[VariableCutSceneEndScript.Value], false, false, args);
        }

        void StartObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var script = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var data = GetWordVarArgs();
            RunObjectScript(obj, script, false, false, data);
        }

        protected void StopObjectCode()
        {
            var ss = _slots[_currentScript];
            if (Game.Version <= 5)
            {
                if (ss.Where != WhereIsObject.Global && ss.Where != WhereIsObject.Local)
                {
                    StopObjectScriptCore(ss.Number);
                }
                else
                {
                    ss.Number = 0;
                    ss.Status = ScriptStatus.Dead;
                }
            }
            else
            {
                if (ss.CutSceneOverride != 0)
                    throw new InvalidOperationException(
                        string.Format("{0} {1} ending with active cutscene/override ({2})", 
                            (ss.Where != WhereIsObject.Global && ss.Where != WhereIsObject.Local) ? "Object" : "Script",
                            ss.Number, ss.CutSceneOverride));
                ss.Number = 0;
                ss.Status = ScriptStatus.Dead;
            }

            _currentScript = 0xFF;
        }

        void StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var data = GetWordVarArgs();

            // Copy protection was disabled in KIXX XL release (Amiga Disk) and
            // in LucasArts Classic Adventures (PC Disk)
            if (_game.Id == "monkey" && _game.Variant == "VGA" && script == 0x98)
            {
                return;
            }

            RunScript((byte)script, (op & 0x20) != 0, (op & 0x40) != 0, data);
        }

        void StopScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (script == 0)
                StopObjectCode();
            else
                StopScript(script);
        }

        internal void StartScene(byte room, Actor a = null, int objectNr = 0)
        {
            StopTalk();

            FadeOut(_switchRoomEffect2);
            _newEffect = _switchRoomEffect;

            if (_currentScript != 0xFF)
            {
                if (_slots[_currentScript].Where == WhereIsObject.Room || _slots[_currentScript].Where == WhereIsObject.FLObject)
                {
                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
                else if (_slots[_currentScript].Where == WhereIsObject.Local)
                {
                    //if (slots[_currentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Script %d stopped with active cutscene/override in exit", slots[_currentScript].number);

                    //nukeArrays(_currentScript);
                    _currentScript = 0xFF;
                }
            }

            if (VariableNewRoom.HasValue)
                _variables[VariableNewRoom.Value] = room;

            RunExitScript();

            KillScriptsAndResources();

            if (_game.Version >= 4)
            {
                StopCycle(0);
            }

            for (var i = 1; i < Actors.Length; i++)
            {
                Actors[i].Hide();
            }

            for (var i = 0; i < 256; i++)
            {
                Gdi.RoomPalette[i] = (byte)i;
                if (_shadowPalette != null)
                    _shadowPalette[i] = (byte)i;
            }

            if (Game.Version < 5)
            {
                SetDirtyColors(0, 255);
            }

            Variables[VariableRoom.Value] = room;
            _fullRedraw = true;

            _currentRoom = room;

            if (room >= 0x80)
                _roomResource = _resourceMapper[room & 0x7F];
            else
                _roomResource = room;

            Variables[VariableRoomResource.Value] = _roomResource;

            if (room != 0 && _game.Version == 5 && room == _roomResource)
                Variables[VariableRoomFlag.Value] = 1;

            ClearRoomObjects();

            if (_currentRoom == 0)
            {
                return;
            }

            roomData = _resManager.GetRoom(_roomResource);
            SetCurrentPalette(0);

            Gdi.NumZBuffer = GetNumZBuffers();

            Gdi.TransparentColor = roomData.TransparentColor;
            ResetRoomSubBlocks();
            ResetRoomObjects();
            _drawingObjects.Clear();

            if (VariableRoomWidth.HasValue && VariableRoomHeight.HasValue)
            {
                Variables[VariableRoomWidth.Value] = roomData.Header.Width;
                Variables[VariableRoomHeight.Value] = roomData.Header.Height;
            }

            _variables[VariableCameraMinX.Value] = ScreenWidth / 2;
            _variables[VariableCameraMaxX.Value] = roomData.Header.Width - (ScreenWidth / 2);

            _camera.Mode = CameraMode.Normal;
            _camera.CurrentPosition.X = _camera.DestinationPosition.X = (short)(ScreenWidth / 2);
            _camera.CurrentPosition.Y = _camera.DestinationPosition.Y = (short)(ScreenHeight / 2);

            if (_roomResource == 0)
                return;

            Gdi.ClearGfxUsageBits();

            if (_game.Version >= 5 && a != null)
            {
                var where = GetWhereIsObject(objectNr);
                if (where != WhereIsObject.Room && where != WhereIsObject.FLObject)
                    throw new NotSupportedException(string.Format("StartScene: Object {0} is not in room {1}", objectNr, _currentRoom));

                Point pos;
                int dir;
                GetObjectXYPos(objectNr, out pos, out dir);
                a.PutActor(pos, _currentRoom);
                a.SetDirection(dir + 180);
                a.StopActorMoving();

                if (Game.GameId == NScumm.Core.IO.GameId.SamNMax)
                {
                    Camera.CurrentPosition.X = Camera.DestinationPosition.X = a.Position.X;
                    SetCameraAt(a.Position);
                }
            }

            ShowActors();

            _egoPositioned = false;

            RunEntryScript();

            if (_game.Version >= 5)
            {
                if (a != null && !_egoPositioned)
                {
                    var pos = GetObjectXYPos(objectNr);
                    a.PutActor(pos, _currentRoom);
                    a.Moving = 0;
                }
            }

            _doEffect = true;
        }

        protected void RunInventoryScript(int i)
        {
            if (_variables[VariableInventoryScript.Value] != 0)
            {
                RunScript((byte)_variables[VariableInventoryScript.Value], false, false, new [] { i });
            }
        }

        void RunInputScript(ClickArea clickArea, KeyCode code, int mode)
        {
            var verbScript = _variables[VariableVerbScript.Value];

            if (verbScript != 0)
            {
                RunScript((byte)verbScript, false, false, new [] { (int)clickArea, (int)code, mode });
            }
        }

        void RunEntryScript()
        {
            if (_variables[VariableEntryScript.Value] != 0)
                RunScript((byte)_variables[VariableEntryScript.Value], false, false, new int[0]);

            if (roomData != null && roomData.EntryScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot] = new ScriptSlot
                {
                    Status = ScriptStatus.Running,
                    Number = 10002,
                    Where = WhereIsObject.Room,
                };
                _currentScriptData = roomData.EntryScript.Data;
                RunScriptNested((byte)slot);
            }

            if (_variables[VariableEntryScript2.Value] != 0)
                RunScript((byte)_variables[VariableEntryScript2.Value], false, false, new int[0]);
        }

        void RunExitScript()
        {
            if (_variables[VariableExitScript.Value] != 0)
            {
                RunScript((byte)_variables[VariableExitScript.Value], false, false, new int[0]);
            }

            if (roomData != null && roomData.ExitScript.Data.Length != 0)
            {
                var slot = GetScriptSlotIndex();
                _slots[slot] = new ScriptSlot
                {
                    Status = ScriptStatus.Running,
                    Number = 10001,
                    Where = WhereIsObject.Room
                };
                _currentScriptData = roomData.ExitScript.Data;
                RunScriptNested((byte)slot);
            }
        }

        public TimeSpan RunBootScript(int bootParam = 0)
        {
            if (_game.GameId == NScumm.Core.IO.GameId.Indy4 && bootParam == 0)
            {
                bootParam = -7873;
            }
            else if (_game.GameId == NScumm.Core.IO.GameId.SamNMax && bootParam == 0)
            {
                bootParam = -1;
            }
            RunScript(1, false, false, new [] { bootParam });
            SetDefaultCursor();

            return GetTimeToWaitBeforeLoop(TimeSpan.Zero);
        }

        public void StopScript(int script)
        {
            if (script == 0)
                return;

            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (script == _slots[i].Number && _slots[i].Status != ScriptStatus.Dead &&
                    (_slots[i].Where == WhereIsObject.Global || _slots[i].Where == WhereIsObject.Local))
                {
                    if (_slots[i].CutSceneOverride != 0 && Game.Version >= 5)
                        throw new NotSupportedException(string.Format("Script {0} stopped with active cutscene/override", script));

                    _slots[i].Number = 0;
                    _slots[i].Status = ScriptStatus.Dead;

                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (var i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].Number == script &&
                    (_nest[i].Where == WhereIsObject.Global || _nest[i].Where == WhereIsObject.Local))
                {
                    _nest[i].Number = 0;
                    _nest[i].Slot = 0xFF;
                    _nest[i].Where = WhereIsObject.NotFound;
                }
            }
        }

        public void RunScript(byte scriptNum, bool freezeResistant, bool recursive, int[] data)
        {
            if (scriptNum == 0)
                return;

            if (!recursive)
                StopScript(scriptNum);

            WhereIsObject scriptType;
            if (scriptNum < NumGlobalScripts)
            {
                scriptType = WhereIsObject.Global;
            }
            else
            {
                scriptType = WhereIsObject.Local;
            }

            var slotIndex = GetScriptSlotIndex();
            _slots[slotIndex] = new ScriptSlot
            {
                Number = scriptNum,
                Status = ScriptStatus.Running,
                FreezeResistant = freezeResistant,
                Recursive = recursive,
                Where = scriptType
            };

            UpdateScriptData(slotIndex);
            _slots[slotIndex].InitializeLocals(data);
            RunScriptNested(slotIndex);
        }

        void UpdateScriptData(ushort slotIndex)
        {
            var scriptNum = _slots[slotIndex].Number;
            if (_slots[slotIndex].Where == WhereIsObject.Inventory)
            {
                var data = (from o in _invData
                                        where o.Number == scriptNum
                                        select o.Script.Data).FirstOrDefault();
                _currentScriptData = data;
            }
            else if (scriptNum == 10002)
            {
                _currentScriptData = roomData.EntryScript.Data;
            }
            else if (scriptNum == 10001)
            {
                _currentScriptData = roomData.ExitScript.Data;
            }
            else if (_slots[slotIndex].Where == WhereIsObject.Room)
            {
                var data = (from o in roomData.Objects
                                        where o.Number == scriptNum
                                        select o.Script.Data).First();
                _currentScriptData = data;
            }
            else if (scriptNum < NumGlobalScripts)
            {
                var data = _resManager.GetScript((byte)scriptNum);
                _currentScriptData = data;
            }
            else if ((scriptNum - NumGlobalScripts) < roomData.LocalScripts.Length)
            {
                _currentScriptData = roomData.LocalScripts[scriptNum - NumGlobalScripts].Data;
            }
            else
            {
                var data = (from o in roomData.Objects
                                        where o.Number == scriptNum
                                        let entry = (byte)_slots[slotIndex].InventoryEntry
                                        where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
                                        select o.Script.Data).FirstOrDefault();
                _currentScriptData = data;
            }
        }

        void RunScriptNested(byte script)
        {
            if (_currentScript == 0xFF)
            {
                _nest[_numNestedScripts].Number = 0xFF;
                _nest[_numNestedScripts].Where = WhereIsObject.NotFound;
            }
            else
            {
                // Store information about the currently running script
                _slots[_currentScript].Offset = (uint)_currentPos;
                _nest[_numNestedScripts].Number = _slots[_currentScript].Number;
                _nest[_numNestedScripts].Where = _slots[_currentScript].Where;
                _nest[_numNestedScripts].Slot = _currentScript;
            }

            _numNestedScripts++;

            _currentScript = script;
            ResetScriptPointer();
            Run();

            if (_numNestedScripts > 0)
                _numNestedScripts--;

            var nest = _nest[_numNestedScripts];
            if (nest.Number != 0 && nest.Slot < _slots.Length)
            {
                // Try to resume the script which called us, if its status has not changed
                // since it invoked us. In particular, we only resume it if it hasn't been
                // stopped in the meantime, and if it did not already move on.
                var slot = _slots[nest.Slot];
                if (slot.Number == nest.Number && slot.Where == nest.Where &&
                    slot.Status != ScriptStatus.Dead && !slot.Frozen)
                {
                    _currentScript = nest.Slot;
                    UpdateScriptData(nest.Slot);
                    ResetScriptPointer();
                    return;
                }
            }
            _currentScript = 0xFF;
        }

        void ResetScriptPointer()
        {
            _currentPos = (int)_slots[_currentScript].Offset;
            if (_currentPos < 0)
                throw new NotSupportedException("Invalid offset in reset script pointer");
        }

        byte GetScriptSlotIndex()
        {
            for (byte i = 1; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Dead)
                    return i;
            }
            return 0xFF;
        }

        void RunAllScripts()
        {
            for (var i = 0; i < NumScriptSlot; i++)
                _slots[i].IsExecuted = false;

            _currentScript = 0xFF;

            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Running && !_slots[i].IsExecuted)
                {
                    _currentScript = (byte)i;
                    UpdateScriptData((ushort)i);
                    ResetScriptPointer();
                    Run();
                }
            }
        }

        protected bool IsScriptInUse(int script)
        {
            for (var i = 0; i < NumScriptSlot; i++)
                if (_slots[i].Number == script)
                    return true;
            return false;
        }

        void CheckAndRunSentenceScript()
        {
            var sentenceScript = _variables[VariableSentenceScript.Value];

            if (IsScriptInUse(sentenceScript))
            {
                for (var i = 0; i < NumScriptSlot; i++)
                    if (_slots[i].Number == sentenceScript && _slots[i].Status != ScriptStatus.Dead &&
                        !_slots[i].Frozen)
                        return;
            }

            if (_sentenceNum == 0 || _sentence[_sentenceNum - 1].IsFrozen)
                return;

            _sentenceNum--;
            var st = _sentence[_sentenceNum];

            if (st.Preposition && st.ObjectB == st.ObjectA)
                return;

            _currentScript = 0xFF;
            if (sentenceScript != 0)
            {
                var data = new int[] { st.Verb, st.ObjectA, st.ObjectB };
                RunScript((byte)sentenceScript, false, false, data);
            }
        }

        protected void RunObjectScript(int obj, byte entry, bool freezeResistant, bool recursive, int[] vars)
        {
            if (obj == 0)
                return;

            if (!recursive)
                StopObjectScriptCore((ushort)obj);

            var where = GetWhereIsObject(obj);

            if (where == WhereIsObject.NotFound)
            {
                Console.Error.WriteLine("warning: Code for object {0} not in room {1}", obj, _roomResource);
                return;
            }

            // Find a free object slot, unless one was specified
            var slot = GetScriptSlotIndex();

            ObjectData objFound = null;
            if (roomData != null)
            {
                objFound = (from o in roomData.Objects.Concat(_invData)
                                        where o != null
                                        where o.Number == obj
                                        where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
                                        select o).FirstOrDefault();
            }

            if (objFound == null)
                return;

            _slots[slot] = new ScriptSlot
            {
                Number = (ushort)obj,
                InventoryEntry = entry,
                Offset = (uint)((objFound.ScriptOffsets.ContainsKey(entry) ? objFound.ScriptOffsets[entry] : objFound.ScriptOffsets[0xFF]) - objFound.Script.Offset),
                Status = ScriptStatus.Running,
                Where = where,
                FreezeResistant = freezeResistant,
                Recursive = recursive
            };

            _slots[slot].InitializeLocals(vars);

            // V0 Ensure we don't try and access objects via index inside the script
            //_v0ObjectIndex = false;
            UpdateScriptData(slot);
            RunScriptNested(slot);
        }

        protected void StopObjectScriptCore(ushort script)
        {
            if (script == 0)
                return;

            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (script == _slots[i].Number && _slots[i].Status != ScriptStatus.Dead &&
                    (_slots[i].Where == WhereIsObject.Room || _slots[i].Where == WhereIsObject.Inventory || _slots[i].Where == WhereIsObject.FLObject))
                {
                    if (_slots[i].CutSceneOverride != 0 && Game.Version >= 5)
                        throw new NotSupportedException(string.Format("Script {0} stopped with active cutscene/override", script));

                    _slots[i].Number = 0;
                    _slots[i].Status = ScriptStatus.Dead;
                    if (_currentScript == i)
                        _currentScript = 0xFF;
                }
            }

            for (var i = 0; i < _numNestedScripts; ++i)
            {
                if (_nest[i].Number == script &&
                    (_nest[i].Where == WhereIsObject.Room || _nest[i].Where == WhereIsObject.Inventory || _nest[i].Where == WhereIsObject.FLObject))
                {
                    _nest[i].Number = 0;
                    _nest[i].Slot = 0xFF;
                    _nest[i].Where = WhereIsObject.NotFound;
                }
            }
        }

        protected void FreezeScripts(int flag)
        {
            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (_currentScript != i && _slots[i].Status != ScriptStatus.Dead && (!_slots[i].FreezeResistant || flag >= 0x80))
                {
                    _slots[i].Freeze();
                }
            }

            for (var i = 0; i < _sentence.Length; i++)
                _sentence[i].Freeze();

            if (cutScene.CutSceneScriptIndex != 0xFF)
            {
                _slots[cutScene.CutSceneScriptIndex].UnfreezeAll();
            }
        }

        protected bool IsScriptRunningCore(int script)
        {
            for (var i = 0; i < NumScriptSlot; i++)
            {
                var ss = _slots[i];
                if (ss.Number == script && (ss.Where == WhereIsObject.Global || ss.Where == WhereIsObject.Local) && ss.Status != ScriptStatus.Dead)
                    return true;
            }
            return false;
        }

        protected void BreakHereCore()
        {
            _slots[_currentScript].Offset = (uint)_currentPos;
            _currentScript = 0xFF;
        }

        void DecreaseScriptDelay(int amount)
        {
            _talkDelay -= amount;
            if (_talkDelay < 0)
                _talkDelay = 0;
            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Paused)
                {
                    _slots[i].Delay -= amount;
                    if (_slots[i].Delay < 0)
                    {
                        _slots[i].Status = ScriptStatus.Running;
                        _slots[i].Delay = 0;
                    }
                }
            }
        }
    }
}

