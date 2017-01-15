//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using System.IO;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal class AnotherEngine : Engine
    {
        private const int MaxSaveSlots = 100;

        private readonly IAnotherSystem _sys;
        private readonly VirtualMachine _vm;
        private readonly Resource _res;
        private readonly Video _video;
        private readonly AnotherMixer _mixer;
        private readonly SfxPlayer _player;
        private sbyte _stateSlot;

        public AnotherEngine(ISystem system, GameSettings settings)
            : base(system, settings, false)
        {
            _sys = new SdlStub();
            _res = new Resource();
            _mixer = new AnotherMixer(_sys);
            _video = new Video(_res, _sys);
            _res.Video = _video;
            _player = new SfxPlayer(_mixer, _res, _sys);
            _vm = new VirtualMachine(_mixer, _res, _player, _video, _sys);

            _video.Init();
            _res.AllocMemBlock();
            _res.ReadEntries();
            _vm.Init();
            _mixer.Init();

            //Init virtual machine, legacy way
            _vm.InitForPart(Resource.GamePartFirst); // This game part is the protection screen
            //_vm.InitForPart(Resource.GamePart2);
        }

        public override void Run()
        {
            while (!HasToQuit)
            {
                _vm.CheckThreadRequests();

                _vm.Inp_updatePlayer();

                ProcessInput();

                _vm.HostFrame();
            }
        }

        private void ProcessInput()
        {
            if (_sys.Input.Load)
            {
                LoadGameState(_stateSlot);
                _sys.Input.Load = false;
            }
            if (_sys.Input.Save)
            {
                SaveGameState(_stateSlot, "quicksave");
                _sys.Input.Save = false;
            }
            if (_sys.Input.FastMode)
            {
                _vm.FastMode = !_vm.FastMode;
                _sys.Input.FastMode = false;
            }
            if (_sys.Input.StateSlot != 0)
            {
                var slot = (sbyte) (_stateSlot + _sys.Input.StateSlot);
                if (slot >= 0 && slot < MaxSaveSlots)
                {
                    _stateSlot = slot;
                    Debug(DebugLevels.DbgInfo, "Current game state slot is {0}", _stateSlot);
                }
                _sys.Input.StateSlot = 0;
            }
        }

        private string MakeGameStateName(int slot)
        {
            return $"another.s{slot:D2}";
        }

        public override void LoadGameState(int slot)
        {
            var stateFile = MakeGameStateName(slot);
            var stream = OpenFileRead(stateFile);
            if (stream == null)
            {
                Warning("Unable to open state file '{0}'", stateFile);
            }
            else
            {
                try
                {
                    var f = new BinaryReader(stream);
                    var id = f.ReadUInt32BigEndian();
                    if (id != ScummHelper.MakeTag('A', 'W', 'S', 'V'))
                    {
                        Warning("Bad savegame format");
                    }
                    else
                    {
                        // mute
                        _player.Stop();
                        _mixer.StopAll();
                        // header
                        var ver = f.ReadUInt16BigEndian();
                        f.ReadUInt16BigEndian();
                        var hdrdesc = new byte[32];
                        f.Read(hdrdesc, 0, hdrdesc.Length);
                        // contents
                        var s = new Serializer(stream, Mode.SmLoad, _res.MemPtrStart, ver);
                        _vm.SaveOrLoad(s);
                        _res.SaveOrLoad(s);
                        _video.SaveOrLoad(s);
                        _player.SaveOrLoad(s);
                        _mixer.SaveOrLoad(s);
                    }
                    Debug(DebugLevels.DbgInfo, "Loaded state from slot {0}", _stateSlot);
                }
                catch (Exception)
                {
                    Warning("I/O error when loading game state");
                }
            }
        }

        public override void SaveGameState(int slot, string desc)
        {
            var stateFile = MakeGameStateName(slot);
            var stream = OpenFileWrite(stateFile);
            if (stream == null)
            {
                Warning("Unable to save state file '{0}'", stateFile);
            }
            else
            {
                using (var f = new BinaryWriter(stream))
                {
                    try
                    {
                        // header
                        f.WriteUInt32BigEndian(ScummHelper.MakeTag('A', 'W', 'S', 'V'));
                        f.WriteUInt16BigEndian(Serializer.CurVer);
                        f.WriteUInt16BigEndian(0);
                        var hdrdesc = new byte[32];
                        Array.Copy(desc.GetBytes(), hdrdesc, Math.Min(31, desc.Length));
                        f.WriteBytes(hdrdesc, hdrdesc.Length);

                        // contents
                        var s = new Serializer(stream, Mode.SmSave, _res.MemPtrStart);
                        _vm.SaveOrLoad(s);
                        _res.SaveOrLoad(s);
                        _video.SaveOrLoad(s);
                        _player.SaveOrLoad(s);
                        _mixer.SaveOrLoad(s);
                        Debug(DebugLevels.DbgInfo, "Saved state to slot {0}", _stateSlot);
                    }
                    catch (Exception)
                    {
                        Warning("I/O error when saving game state");
                    }
                }
            }
        }
    }
}