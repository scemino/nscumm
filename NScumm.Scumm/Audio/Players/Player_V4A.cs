//
//  Player_V4A.cs
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
using System.Diagnostics;
using NScumm.Core.IO;

namespace NScumm.Core.Audio
{
    /// <summary>
    /// Scumm V4 Amiga sound/music driver.
    /// </summary>
    class Player_V4A: IMusicEngine
    {
        public Player_V4A(ScummEngine scumm, IMixer mixer)
        {
            _vm = scumm;
            _mixer = mixer;
            _tfmxMusic = new Tfmx(_mixer.OutputRate, true);
            _tfmxSfx = new Tfmx(_mixer.OutputRate, true);

            Debug.Assert(scumm != null);
            Debug.Assert(mixer != null);
            Debug.Assert(_vm.Game.GameId == GameId.Monkey1);
            _tfmxMusic.SetSignalAction((num, value) =>
                {
                    if (num == 0)
                    {
                        _signal = value;
                    }
                });
        }

        public void Dispose()
        {
            _mixer.StopHandle(_musicHandle);
            _mixer.StopHandle(_sfxHandle);
            _tfmxMusic.Dispose();
        }

        public void SetMusicVolume(int vol)
        {
            Debug.WriteLine("player_v4a: setMusicVolume {0}", vol);
        }

        public void StopAllSounds()
        {
            Debug.WriteLine("player_v4a: stopAllSounds");
            if (_initState > 0)
            {
                _tfmxMusic.StopSong();
                _signal = 0;
                _musicId = 0;

                _tfmxSfx.StopSong();
                ClearSfxSlots();
            }
            else
                _mixer.StopHandle(_musicHandle);
        }

        public void StopSound(int nr)
        {
            Debug.WriteLine("player_v4a: stopSound {0}", nr);
            if (nr == 0)
                return;
            if (nr == _musicId)
            {
                _musicId = 0;
                if (_initState > 0)
                    _tfmxMusic.StopSong();
                else
                    _mixer.StopHandle(_musicHandle);
                _signal = 0;
            }
            else
            {
                var chan = GetSfxChan(nr);
                if (chan != -1)
                {
                    SetSfxSlot(chan, 0);
                    _tfmxSfx.StopMacroEffect(chan);
                }
            }
        }

        public void StartSound(int nr)
        {

            var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
            Debug.Assert(ptr != null);

            var val = ptr[9];
            if (val < 0 || val >= _monkeyCommands.Length)
            {
                Debug.WriteLine("player_v4a: illegal Songnumber {0}", val);
                return;
            }

            if (_initState == 0)
            {
                Init();
                _initState = 1;
            }

            if (_initState < 0)
                return;

            int index = _monkeyCommands[val];
            var type = ptr[6];
            if (index < 0)
            {    // SoundFX
                index = -index - 1;
                Debug.WriteLine("player_v4a: play {0}: custom {1} - {2:X2}", nr, index, type);

                // start an empty Song so timing is setup
                if (_tfmxSfx.SongIndex < 0)
                    _tfmxSfx.DoSong(0x18);

                var chan = _tfmxSfx.DoSfx((ushort)index);
                if (chan >= 0 && chan < _sfxSlots.Length)
                    SetSfxSlot(chan, nr, type);
                else
                    Debug.WriteLine("player_v4a: custom {0} is not of required type", index);

                // the Tfmx-player never "ends" the output by itself, so this should be threadsafe
                if (!_mixer.IsSoundHandleActive(_sfxHandle))
                    _sfxHandle = _mixer.PlayStream(SoundType.SFX, _tfmxSfx, -1, Mixer.MaxChannelVolume, 0, false);

            }
            else
            {    // Song
                Debug.WriteLine("player_v4a: play {0}: song {1} - {2:X2}", nr, index, type);
                if (ptr[6] != 0x7F)
                    Debug.WriteLine("player_v4a: Song has wrong type");

                _tfmxMusic.DoSong(index);
                _signal = 2;

                // the Tfmx-player never "ends" the output by itself, so this should be threadsafe
                if (!_mixer.IsSoundHandleActive(_musicHandle))
                    _musicHandle = _mixer.PlayStream(SoundType.Music, _tfmxMusic, -1, Mixer.MaxChannelVolume, 0, false);
                _musicId = nr;
            }
        }

        public int GetMusicTimer()
        {
            // A workaround if the modplayer couldnt load the datafiles - just return a number big enough to pass all tests
            if (_initState < 0)
                return 2000;
            if (_musicId != 0)
            {
                // The titlesong (and a few others) is running with ~70 ticks per second and the scale seems to be based on that.
                // The Game itself doesnt get the timing from the Tfmx Player however, so we just use the elapsed time
                // 357 ~ 1000 * 25 * (1 / 70)
                return _mixer.GetSoundElapsedTime(_musicHandle) / 357;
            }
            return 0;
        }

        public int GetSoundStatus(int nr)
        {
            // For music the game queues a variable the Tfmx Player sets through a special command.
            // For sfx there seems to be no way to queue them, and the game doesnt try to.
            return (nr == _musicId) ? _signal : 0;
        }

        void IMusicEngine.SaveOrLoad(Serializer serializer)
        {
        }

        void Init()
        {
            if (_vm.Game.GameId != GameId.Monkey1)
                Debug.WriteLine("player_v4a - unknown game");

            var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path);
            var fileMdat = ServiceLocator.FileStorage.OpenFileRead(ScummHelper.LocatePath(directory, "music.dat"));
            var fileSample = ServiceLocator.FileStorage.OpenFileRead(ScummHelper.LocatePath(directory, "sample.dat"));

            // explicitly request that no instance delets the resources automatically
            if (_tfmxMusic.Load(fileMdat, fileSample, false))
            {
                _tfmxSfx.SetModuleData(_tfmxMusic);
            }
        }

        int GetSfxChan(int id)
        {
            for (int i = 0; i < _sfxSlots.Length; ++i)
                if (_sfxSlots[i].id == id)
                    return i;
            return -1;
        }

        void SetSfxSlot(int channel, int id, byte type = 0)
        {
            _sfxSlots[channel].id = id;
            //      _sfxSlots[channel].type = type;
        }

        void ClearSfxSlots()
        {
            for (int i = 0; i < _sfxSlots.Length; ++i)
            {
                _sfxSlots[i].id = 0;
                //          _sfxSlots[i].type = 0;
            }
        }

        readonly ScummEngine _vm;
        readonly IMixer _mixer;

        Tfmx _tfmxMusic;
        Tfmx _tfmxSfx;
        SoundHandle _musicHandle = new SoundHandle();
        SoundHandle _sfxHandle = new SoundHandle();

        int _musicId;
        ushort _signal;

        struct SfxChan
        {
            public int id;
            //      byte type;
        }

        sbyte _initState;
        // < 0: failed, 0: uninitialized, > 0: initialized

        readonly SfxChan[] _sfxSlots = new SfxChan[4];

        readonly sbyte[] _monkeyCommands =
            {
                -1,  -2,  -3,  -4,  -5,  -6,  -7,  -8,
                -9, -10, -11, -12, -13, -14,  18,  17,
                -17, -18, -19, -20, -21, -22, -23, -24,
                -25, -26, -27, -28, -29, -30, -31, -32,
                -33,  16, -35,   0,   1,   2,   3,   7,
                8,  10,  11,   4,   5,  14,  15,  12,
                6,  13,   9,  19
            };
    }
}

