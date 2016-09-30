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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        const int NumLocalScripts = 60;

        protected byte[] _currentScriptData;
        internal CutScene cutScene = new CutScene();
        int _numNestedScripts;
        NestedScript[] _nest;
        ScriptSlot[] _slots;
        bool _ignoreEntryExitScript;

        internal ScriptSlot[] Slots { get { return _slots; } }

        protected byte CurrentScript { get; set; }

        protected int CurrentPos { get; set; }


        public TimeSpan RunBootScript(int bootParam = 0)
        {
            if (!Settings.CopyProtection && _game.GameId == Scumm.IO.GameId.Indy4 && bootParam == 0)
            {
                bootParam = -7873;
            }
            else if (!Settings.CopyProtection && _game.GameId == Scumm.IO.GameId.SamNMax && bootParam == 0)
            {
                bootParam = -1;
            }
            RunScript(1, false, false, new[] { bootParam });
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

                    if (CurrentScript == i)
                        CurrentScript = 0xFF;
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

        public void RunScript(int scriptNum, bool freezeResistant, bool recursive, int[] data)
        {
            if (scriptNum == 0)
                return;

            if (!recursive)
                StopScript(scriptNum);

            WhereIsObject scriptType;
            if (scriptNum < _resManager.NumGlobalScripts)
            {
                ResourceManager.LoadScript(scriptNum);
                scriptType = WhereIsObject.Global;
            }
            else
            {
                scriptType = WhereIsObject.Local;
            }

            var slotIndex = GetScriptSlotIndex();
            _slots[slotIndex] = new ScriptSlot
            {
                Number = (ushort)scriptNum,
                Status = ScriptStatus.Running,
                FreezeResistant = freezeResistant,
                Recursive = recursive,
                Where = scriptType
            };

            UpdateScriptData(slotIndex);
            _slots[slotIndex].InitializeLocals(data);
            RunScriptNested(slotIndex);
        }

        internal void StartScene(byte room, Actor a = null, int objectNr = 0)
        {
            StopTalk();

            FadeOut(_switchRoomEffect2);
            _newEffect = _switchRoomEffect;

            if (CurrentScript != 0xFF)
            {
                if (_slots[CurrentScript].Where == WhereIsObject.Room || _slots[CurrentScript].Where == WhereIsObject.FLObject)
                {
                    //nukeArrays(CurrentScript);
                    CurrentScript = 0xFF;
                }
                else if (_slots[CurrentScript].Where == WhereIsObject.Local)
                {
                    //if (slots[CurrentScript].cutsceneOverride && _game.version >= 5)
                    //    error("Script %d stopped with active cutscene/override in exit", slots[CurrentScript].number);

                    //nukeArrays(CurrentScript);
                    CurrentScript = 0xFF;
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

            if (Game.Version >= 7)
            {
                // Set the shadow palette(s) to all black. This fixes
                // bug #795940, and actually makes some sense (after all,
                // shadows tend to be rather black, don't they? ;-)
                Array.Clear(_shadowPalette, 0, _shadowPalette.Length);
            }
            else
            {
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
            }

            Variables[VariableRoom.Value] = room;
            _fullRedraw = true;

            _currentRoom = room;

            if (room >= 0x80 && Game.Version < 7)
                _roomResource = _resourceMapper[room & 0x7F];
            else
                _roomResource = room;

            if (VariableRoomResource.HasValue)
                Variables[VariableRoomResource.Value] = _roomResource;

            if (room != 0)
                ResourceManager.LoadRoom(room);

            if (room != 0 && _game.Version == 5 && room == _roomResource)
                Variables[VariableRoomFlag.Value] = 1;

            ClearRoomObjects();

            if (_currentRoom == 0)
            {
                if (roomData != null)
                {
                    _ignoreEntryExitScript = true;
                    roomData.ExitScript.Data = new byte[0];
                    //roomData.Objects.Clear();
                }
                return;
            }

            roomData = _resManager.GetRoom(_roomResource);
            _ignoreEntryExitScript = false;
            if (roomData.HasPalette)
            {
                SetCurrentPalette(0);
            }

            Gdi.NumZBuffer = GetNumZBuffers();

            Gdi.TransparentColor = roomData.TransparentColor;
            ResetRoomSubBlocks();
            ResetRoomObjects();
            _drawingObjects.Clear();

            if (Game.Version >= 7)
            {
                // Resize main virtual screen in V7 games. This is necessary
                // because in V7, rooms may be higher than one screen, so we have
                // to accomodate for that.
                _mainVirtScreen = new VirtScreen(MainVirtScreen.TopLine, ScreenWidth, roomData.Header.Height - MainVirtScreen.TopLine, MainVirtScreen.PixelFormat, 2, true);
            }
            Gdi.SetMaskHeight(roomData.Header.Height);

            if (VariableRoomWidth.HasValue && VariableRoomHeight.HasValue)
            {
                Variables[VariableRoomWidth.Value] = roomData.Header.Width;
                Variables[VariableRoomHeight.Value] = roomData.Header.Height;
            }

            if (VariableCameraMinX.HasValue)
            {
                _variables[VariableCameraMinX.Value] = ScreenWidth / 2;
            }
            if (VariableCameraMaxX.HasValue)
            {
                _variables[VariableCameraMaxX.Value] = roomData.Header.Width - (ScreenWidth / 2);
            }

            if (Game.Version >= 7)
            {
                Variables[VariableCameraMinY.Value] = ScreenHeight / 2;
                Variables[VariableCameraMaxY.Value] = roomData.Header.Height - (ScreenHeight / 2);
                SetCameraAt(new Point((short) (ScreenWidth / 2), (short) (ScreenHeight / 2)));
            }
            else
            {
                _camera.Mode = CameraMode.Normal;
                _camera.CurrentPosition.X = _camera.DestinationPosition.X = (short) (ScreenWidth / 2);
                _camera.CurrentPosition.Y = _camera.DestinationPosition.Y = (short) (ScreenHeight / 2);
            }

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

                if (Game.GameId == Scumm.IO.GameId.SamNMax)
                {
                    Camera.CurrentPosition.X = Camera.DestinationPosition.X = a.Position.X;
                    SetCameraAt(a.Position);
                }
            }

            ShowActors();

            EgoPositioned = false;

            TownsResetPalCycleFields();

            RunEntryScript();

            if (Game.Version >= 1 && Game.Version <= 2)
            {
                RunScript(5, false, false, new int[0]);
            }
            else if ((Game.Version >= 5) && (Game.Version <= 6))
            {
                if (a != null && !EgoPositioned)
                {
                    var pos = GetObjectXYPos(objectNr);
                    a.PutActor(pos, _currentRoom);
                    a.Moving = 0;
                }
            }
            else if (_game.Version >= 7)
            {
                if (Camera.ActorToFollow != 0)
                {
                    a = Actors[Camera.ActorToFollow];
                    SetCameraAt(a.Position);
                }
            }

            _doEffect = true;

            // Hint the backend about the virtual keyboard during copy protection screens
            if (_game.GameId == GameId.Monkey2)
            {
                if (room == 108)
                    _inputManager.ShowVirtualKeyboard();
                else
                    _inputManager.HideVirtualKeyboard();
            }
            else if (_game.GameId == GameId.Monkey1 && _game.Variant == "ega")
            {   // this is my estimation that the room code is 90 (untested)
                if (room == 90)
                    _inputManager.ShowVirtualKeyboard();
                else
                    _inputManager.HideVirtualKeyboard();
            }
        }

        protected void UnfreezeScripts()
        {
            if (Game.Version <= 2)
            {
                for (var i = 0; i < NumScriptSlot; i++)
                {
                    _slots[i].Unfreeze();
                }
                return;
            }

            for (var i = 0; i < NumScriptSlot; i++)
            {
                _slots[i].Unfreeze();
            }

            for (var i = 0; i < _sentence.Length; i++)
            {
                _sentence[i].Unfreeze();
            }
        }

        protected void BeginOverrideCore()
        {
            var idx = cutScene.StackPointer;
            cutScene.Data[idx].Pointer = CurrentPos;
            cutScene.Data[idx].Script = CurrentScript;

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
            var idx = cutScene.StackPointer;
            cutScene.Data[idx].Pointer = 0;
            cutScene.Data[idx].Script = 0;

            if (Game.Version >= 4)
            {
                _variables[VariableOverride.Value] = 0;
            }
        }

        protected void BeginCutscene(int[] args)
        {
            var scr = CurrentScript;
            _slots[scr].CutSceneOverride++;

            ++cutScene.StackPointer;

            cutScene.Data[cutScene.StackPointer].Data = args.Length > 0 ? args[0] : 0;
            cutScene.Data[cutScene.StackPointer].Script = 0;
            cutScene.Data[cutScene.StackPointer].Pointer = 0;

            cutScene.ScriptIndex = scr;

            if (_variables[VariableCutSceneStartScript.Value] != 0)
                RunScript(_variables[VariableCutSceneStartScript.Value], false, false, args);

            cutScene.ScriptIndex = 0xFF;
        }

        protected void AbortCutscene()
        {
            var idx = cutScene.StackPointer;
            var offs = cutScene.Data[idx].Pointer;

            if (offs != 0)
            {
                var ss = Slots[cutScene.Data[idx].Script];
                ss.Offset = (uint)offs;
                ss.Status = ScriptStatus.Running;
                ss.UnfreezeAll();

                if (ss.CutSceneOverride > 0)
                    ss.CutSceneOverride--;

                _variables[VariableOverride.Value] = 1;
                cutScene.Data[idx].Pointer = 0;
            }
        }

        protected void EndCutsceneCore()
        {
            var ss = _slots[CurrentScript];

            if (ss.CutSceneOverride > 0)    // Only terminate if active
                ss.CutSceneOverride--;

            var args = new[] { cutScene.Data[cutScene.StackPointer].Data };

            Variables[VariableOverride.Value] = 0;

            if (cutScene.Data[cutScene.StackPointer].Pointer != 0 && (ss.CutSceneOverride > 0))   // Only terminate if active
                ss.CutSceneOverride--;

            cutScene.Data[cutScene.StackPointer].Script = 0;
            cutScene.Data[cutScene.StackPointer].Pointer = 0;

            cutScene.StackPointer--;

            if (Variables[VariableCutSceneEndScript.Value] != 0)
                RunScript(Variables[VariableCutSceneEndScript.Value], false, false, args);
        }

        protected void StopObjectCode()
        {
            var ss = _slots[CurrentScript];
            if (Game.Version <= 2)
            {
                if (ss.Where == WhereIsObject.Global || ss.Where == WhereIsObject.Local)
                {
                    StopScript(ss.Number);
                }
                else
                {
                    ss.Number = 0;
                    ss.Status = ScriptStatus.Dead;
                }
            }
            else if (Game.Version <= 5)
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

            CurrentScript = 0xFF;
        }

        protected bool IsScriptInUse(int script)
        {
            for (var i = 0; i < NumScriptSlot; i++)
                if (_slots[i].Number == script)
                    return true;
            return false;
        }

        protected virtual void CheckAndRunSentenceScript()
        {
            var sentenceScript = Game.Version <= 2 ? 2 : _variables[VariableSentenceScript.Value];

            if (IsScriptInUse(sentenceScript))
            {
                for (var i = 0; i < NumScriptSlot; i++)
                    if (_slots[i].Number == sentenceScript && _slots[i].Status != ScriptStatus.Dead &&
                        !_slots[i].Frozen)
                        return;
            }

            if (SentenceNum == 0 || _sentence[SentenceNum - 1].IsFrozen)
                return;

            SentenceNum--;
            var st = _sentence[SentenceNum];

            if (Game.Version < 7 && st.Preposition && st.ObjectB == st.ObjectA)
                return;

            int[] data;
            if (Game.Version <= 2)
            {
                data = new int[0];
                Variables[VariableActiveVerb.Value] = st.Verb;
                Variables[VariableActiveObject1.Value] = st.ObjectA;
                Variables[VariableActiveObject2.Value] = st.ObjectB;
                Variables[VariableVerbAllowed.Value] = GetVerbEntrypointCore(st.ObjectA, st.Verb);
            }
            else
            {
                data = new int[] { st.Verb, st.ObjectA, st.ObjectB };
            }

            CurrentScript = 0xFF;
            if (sentenceScript != 0)
            {
                RunScript(sentenceScript, false, false, data);
            }
        }

        protected void RunObjectScript(int obj, byte entry, bool freezeResistant, bool recursive, int[] vars, int slot = -1)
        {
            if (obj == 0)
                return;

            if (!recursive && (Game.Version >= 3))
                StopObjectScriptCore((ushort)obj);

            var where = GetWhereIsObject(obj);

            if (where == WhereIsObject.NotFound)
            {
                //                Console.Error.WriteLine("warning: Code for object {0} not in room {1}", obj, _roomResource);
                return;
            }

            // Find a free object slot, unless one was specified
            if (slot == -1)
                slot = GetScriptSlotIndex();

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
            {
                objFound = (from o in _objs
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
            UpdateScriptData((ushort)slot);
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
                    if (CurrentScript == i)
                        CurrentScript = 0xFF;
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
            if (Game.Version <= 2)
            {
                for (var i = 0; i < NumScriptSlot; i++)
                {
                    if (CurrentScript != i && _slots[i].Status != ScriptStatus.Dead && !_slots[i].FreezeResistant)
                    {
                        _slots[i].Freeze();
                    }
                }
                return;
            }

            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (CurrentScript != i && _slots[i].Status != ScriptStatus.Dead && (!_slots[i].FreezeResistant || flag >= 0x80))
                {
                    _slots[i].Freeze();
                }
            }

            for (var i = 0; i < _sentence.Length; i++)
                _sentence[i].Freeze();

            if (cutScene.ScriptIndex != 0xFF)
            {
                Slots[cutScene.ScriptIndex].UnfreezeAll();
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
            _slots[CurrentScript].Offset = (uint)CurrentPos;
            CurrentScript = 0xFF;
        }

        protected abstract void RunInventoryScript(int i);

        protected abstract void RunInputScript(ClickArea clickArea, KeyCode code, int mode);

        void RunEntryScript()
        {
            if (VariableEntryScript.HasValue && _variables[VariableEntryScript.Value] != 0)
                RunScript(_variables[VariableEntryScript.Value], false, false, new int[0]);

            if (!_ignoreEntryExitScript && roomData != null && roomData.EntryScript.Data != null)
            {
                int slot = GetScriptSlotIndex();
                _slots[slot] = new ScriptSlot
                {
                    Status = ScriptStatus.Running,
                    Number = 10002,
                    Where = WhereIsObject.Room,
                };
                _currentScriptData = roomData.EntryScript.Data;
                RunScriptNested(slot);
            }

            if (VariableEntryScript2.HasValue && _variables[VariableEntryScript2.Value] != 0)
                RunScript(_variables[VariableEntryScript2.Value], false, false, new int[0]);
        }

        void RunExitScript()
        {
            if (VariableExitScript.HasValue && _variables[VariableExitScript.Value] != 0)
            {
                RunScript(_variables[VariableExitScript.Value], false, false, new int[0]);
            }

            if (!_ignoreEntryExitScript && roomData != null && roomData.ExitScript.Data.Length != 0)
            {
                var slot = GetScriptSlotIndex();
                _slots[slot] = new ScriptSlot
                {
                    Status = ScriptStatus.Running,
                    Number = 10001,
                    Where = WhereIsObject.Room
                };
                _currentScriptData = roomData.ExitScript.Data;
                RunScriptNested(slot);
            }
            if (VariableExitScript2.HasValue && _variables[VariableExitScript2.Value] != 0)
                RunScript(_variables[VariableExitScript2.Value], false, false, new int[0]);
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
            else if (_slots[slotIndex].Where == WhereIsObject.FLObject)
            {
                var data = (from o in _objs
                            where o.Number == scriptNum
                            let entry = (byte)_slots[slotIndex].InventoryEntry
                            where o.ScriptOffsets.ContainsKey(entry) || o.ScriptOffsets.ContainsKey(0xFF)
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
            else if (scriptNum < _resManager.NumGlobalScripts)
            {
                var data = _resManager.GetScript(scriptNum);
                _currentScriptData = data;
            }
            else if ((scriptNum - _resManager.NumGlobalScripts) < roomData.LocalScripts.Length)
            {
                _currentScriptData = roomData.LocalScripts[scriptNum - _resManager.NumGlobalScripts].Data;
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

        void RunScriptNested(int script)
        {
            var nest = _nest[_numNestedScripts];

            if (CurrentScript == 0xFF)
            {
                nest.Number = 0;
                nest.Where = WhereIsObject.NotFound;
            }
            else
            {
                // Store information about the currently running script
                _slots[CurrentScript].Offset = (uint)CurrentPos;
                nest.Number = _slots[CurrentScript].Number;
                nest.Where = _slots[CurrentScript].Where;
                nest.Slot = CurrentScript;
            }

            _numNestedScripts++;

            CurrentScript = (byte)script;
            ResetScriptPointer();
            RunCurrentScript();

            if (_numNestedScripts > 0)
                _numNestedScripts--;

            if (nest.Number != 0 && nest.Slot < _slots.Length)
            {
                // Try to resume the script which called us, if its status has not changed
                // since it invoked us. In particular, we only resume it if it hasn't been
                // stopped in the meantime, and if it did not already move on.
                var slot = _slots[nest.Slot];
                if (slot.Number == nest.Number && slot.Where == nest.Where &&
                    slot.Status != ScriptStatus.Dead && !slot.Frozen)
                {
                    CurrentScript = nest.Slot;
                    UpdateScriptData(nest.Slot);
                    ResetScriptPointer();
                    return;
                }
            }
            CurrentScript = 0xFF;
        }

        void ResetScriptPointer()
        {
            CurrentPos = (int)_slots[CurrentScript].Offset;
            if (CurrentPos < 0)
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

            CurrentScript = 0xFF;

            for (var i = 0; i < NumScriptSlot; i++)
            {
                if (_slots[i].Status == ScriptStatus.Running && !_slots[i].IsExecuted)
                {
                    CurrentScript = (byte)i;
                    UpdateScriptData((ushort)i);
                    ResetScriptPointer();
                    RunCurrentScript();
                }
            }
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

