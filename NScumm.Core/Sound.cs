/*
 * This file is part of NScumm.
 *
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using NScumm.Core.Audio;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NScumm.Core.Audio.Decoders;
using NScumm.Core.IO;
using NScumm.Core.Audio.IMuse;
using System.Threading;

namespace NScumm.Core
{
    class Sound : ISoundRepository
    {
        public const int TalkSoundID = 10000;


        public int SfxMode { get { return sfxMode; } }

        public int LastSound { get; private set; }

        public MusicDriverTypes MusicType
        {
            get;
            set;
        }


        public Sound(ScummEngine vm, IMixer mixer)
        {
            this._vm = vm;
            _soundQueue = new Stack<int>();
            _soundQueueIMuse = new Queue<int>();
            _timer = new Timer(OnCDTimer, this, -1, -1);

            // initialize output & player
            _mixer = mixer;
        }

        public int PollCD()
        {
            if (!_isLoomSteam)
                return _vm.AudioCDManager.IsPlaying ? 1 : 0;
            else
                return _mixer.IsSoundHandleActive(_loomSteamCDAudioHandle) ? 1 : 0;
        }

        public void PlayCDTrack(int track, int numLoops, int startFrame, int duration)
        {
            // Reset the music timer variable at the start of a new track
            _vm.Variables[_vm.VariableMusicTimer.Value] = 0;

            // Play it
            if (!_soundsPaused)
                _vm.AudioCDManager.Play(track, numLoops, startFrame, duration);

            // Start the timer after starting the track. Starting an MP3 track is
            // almost instantaneous, but a CD player may take some time. Hopefully
            // playCD() will block during that delay.
            StartCDTimer();
        }

        public void StartCDTimer()
        {
            _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(100.7));
        }

        public void StopCDTimer()
        {
            _timer.Change(-1, -1);
        }

        public void StopCD()
        {
            _timer.Change(-1, -1);
        }

        public void AddSoundToQueue(int sound)
        {
            if (_vm.VariableLastSound.HasValue)
                _vm.Variables[_vm.VariableLastSound.Value] = sound;
            LastSound = sound;

            // HE music resources are in separate file
            _vm.ResourceManager.LoadSound(MusicType, sound);

            _soundQueue.Push(sound);
        }

        public void ProcessSound()
        {
            if (_vm.Game.Version >= 7)
            {
                ProcessSfxQueues();
            }
            else
            {
                ProcessSfxQueues();
                ProcessSoundQueue();
            }
        }

        public void StopAllSounds()
        {
            if (_currentCDSound != 0)
            {
                _currentCDSound = 0;
                StopCD();
                StopCDTimer();
            }

            // Clear the (secondary) sound queue
            //            _lastSound = 0;
            //            _soundQue2Pos = 0;
            //            memset(_soundQue2, 0, sizeof(_soundQue2));
            _soundQueue.Clear();

            if (_vm.MusicEngine != null)
            {
                _vm.MusicEngine.StopAllSounds();
            }

            // Stop all SFX
            if (!(_vm.MusicEngine is IMuseDigital))
            {
                _mixer.StopAll();
            }
        }

        public bool IsSoundRunning(int snd)
        {
            if (snd == _currentCDSound)
                return PollCD() != 0;

            if (_mixer.IsSoundIdActive(snd))
                return true;

            if (_soundQueue.Contains(snd))
                return true;

            if (_vm.MusicEngine != null)
                return _vm.MusicEngine.GetSoundStatus(snd) != 0;

            return false;
        }

        public void SoundKludge(int[] items)
        {
            var imuseDigital = _vm.MusicEngine as IMuseDigital;
            if (imuseDigital != null)
            {
                var param = new int[8];
                Array.Copy(items, param, Math.Min(8, items.Length));
                imuseDigital.ParseScriptCmds(param[0], param[1], param[2], param[3], param[4], param[5], param[6], param[7]);
                return;
            }

            if (items[0] == -1)
            {
                ProcessSoundQueue();
            }
            else
            {
                _soundQueueIMuse.Enqueue(items.Length);
                foreach (var item in items)
                {
                    _soundQueueIMuse.Enqueue(item);
                }
            }
        }

        public void TalkSound(int a, int b, int mode, int channel = 0)
        {
            if (mode == 1)
            {
                _talkSoundA1 = a;
                _talkSoundB1 = b;
                _talkSoundChannel = channel;
            }
            else
            {
                _talkSoundA2 = a;
                _talkSoundB2 = b;
            }

            _talkSoundMode |= mode;
        }

        public void SetupSound()
        {
            SetupSfxFile();

            if (_vm.Game.GameId == GameId.FullThrottle)
            {
                _vm.Variables[_vm.VariableVoiceBundleLoaded.Value] = string.IsNullOrEmpty(_sfxFilename) ? 0 : 1;
            }
        }

        public void ProcessSfxQueues()
        {
            if (_talkSoundMode != 0)
            {
                if ((_talkSoundMode & 1) != 0)
                    StartTalkSound(_talkSoundA1, _talkSoundB1, 1);
                if ((_talkSoundMode & 2) != 0)
                    _talkChannelHandle = StartTalkSound(_talkSoundA2, _talkSoundB2, 2);
                _talkSoundMode = 0;
            }

            int act = _vm.TalkingActor;
            if ((SfxMode & 2) != 0 && act != 0)
            {
                bool finished;

                if (_vm.MusicEngine is IMuseDigital)
                {
                    finished = !IsSoundRunning(TalkSoundID);
                }
                //                else if (_vm->_game.heversion >= 60)
                //                {
                //                    finished = !isSoundRunning(1);
                //                }
                else
                {
                    finished = !_mixer.IsSoundHandleActive(_talkChannelHandle);
                }

                if ((uint)act < 0x80 && ((_vm.Game.Version == 8) || (_vm.Game.Version <= 7 && !_vm.String[0].NoTalkAnim)))
                {
                    var a = _vm.Actors[act];
                    if (a.IsInCurrentRoom)
                    {
                        if (IsMouthSyncOff(_curSoundPos) && !_mouthSyncMode)
                        {
                            if (!_endOfMouthSync)
                                a.RunTalkScript(a.TalkStopFrame);
                            _mouthSyncMode = false;
                        }
                        else if (!IsMouthSyncOff(_curSoundPos) && !_mouthSyncMode)
                        {
                            a.RunTalkScript(a.TalkStartFrame);
                            _mouthSyncMode = true;
                        }

                        if (_vm.Game.Version <= 6 && finished)
                            a.RunTalkScript(a.TalkStopFrame);
                    }
                }

                if (finished && _vm.TalkDelay == 0)
                {
                    if (!(_vm.Game.Version == 8 && _vm.Variables[_vm.VariableHaveMessage.Value] == 0))
                        _vm.StopTalk();
                }
            }

            if ((sfxMode & 1) != 0)
            {
                if (IsSfxFinished())
                {
                    sfxMode &= ~1;
                }
            }
        }

        public void StopTalkSound()
        {
            if ((sfxMode & 2) != 0)
            {
                if (_vm.MusicEngine is IMuseDigital)
                {
                    ((IMuseDigital)_vm.MusicEngine).StopSound(TalkSoundID);
                }
                //                else if (_vm->_game.heversion >= 60)
                //                {
                //                    stopSound(1);
                //                }
                else
                {
                    _mixer.StopHandle(_talkChannelHandle);
                }

                sfxMode &= ~2;
            }
        }

        public void StopSound(int sound)
        {
            if (sound != 0 && sound == _currentCDSound)
            {
                _currentCDSound = 0;
                StopCD();
                StopCDTimer();
            }

            if (_vm.Game.Version < 7)
                _mixer.StopID(sound);

            if (_vm.MusicEngine != null)
                _vm.MusicEngine.StopSound(sound);

            if (_soundQueue.Count > 0)
            {
                var sounds = _soundQueue.ToList();
                sounds.RemoveAll(obj => obj == sound);
                _soundQueue = new Stack<int>(sounds);
            }
        }

        public void SaveOrLoad(Serializer serializer)
        {
            short _currentMusic = 0;
            var soundEntries = new[]
            {
                LoadAndSaveEntry.Create(r => _currentCDSound = r.ReadInt16(), writer => writer.WriteInt16(_currentCDSound), 35),
                LoadAndSaveEntry.Create(r => _currentMusic = r.ReadInt16(), writer => writer.WriteInt16(_currentMusic), 35),
            };

            soundEntries.ForEach(e => e.Execute(serializer));
        }

        public void PauseSounds(bool pause)
        {
            if (_vm.IMuse != null)
                _vm.IMuse.Pause(pause);

            // Don't pause sounds if the game isn't active
            // FIXME - this is quite a nasty hack, replace with something cleaner, and w/o
            // having to access member vars directly!
            if (_vm.CurrentRoomData == null)
                return;

            _soundsPaused = pause;

            if (_vm.MusicEngine is IMuseDigital)
            {
                ((IMuseDigital)_vm.MusicEngine).Pause(pause);
            }

            _mixer.PauseAll(pause);

            if (_vm.Game.Features.HasFlag(GameFeatures.AudioTracks) && _vm.Variables[_vm.VariableMusicTimer.Value] > 0)
            {
                if (pause)
                    StopCDTimer();
                else
                    StartCDTimer();
            }
        }

        /// <summary>
        /// Check whether the sound resource with the specified ID is still
        /// used.This is invoked by ScummEngine::isResourceInUse, to determine
        /// which resources can be expired from memory.
        /// Technically, this works very similar to isSoundRunning, however it
        /// calls IMuse::get_sound_active() instead of IMuse::getSoundStatus().
        /// The difference between those two is in how they treat sounds which
        /// are being faded out: get_sound_active() returns true even when the
        /// sound is being faded out, while getSoundStatus() returns false in
        /// that case.
        /// </summary>
        /// <param name="sound"></param>
        /// <returns></returns>
        public bool IsSoundInUse(int sound)
        {
            var iMuseDigital = _vm.MusicEngine as IMuseDigital;
            if (iMuseDigital != null)
                return (iMuseDigital.GetSoundStatus(sound) != 0);

            if (sound == _currentCDSound)
                return PollCD() != 0;

            if (IsSoundInQueue(sound))
                return true;

            if (!_vm.ResourceManager.IsSoundLoaded(sound))
                return false;

            if (_vm.IMuse != null)
                return _vm.IMuse.GetSoundActive(sound);

            if (_vm.Mixer.IsSoundIdActive(sound))
                return true;

            return false;
        }

        byte[] ISoundRepository.GetSound(int id)
        {
            return _vm.ResourceManager.GetSound(MusicType, id);
        }

        bool IsSoundInQueue(int sound)
        {
            if (_soundQueue.Any(snd => snd == sound))
                return true;

            var i = 0;
            var soundQueue2 = _soundQueueIMuse.ToArray();
            while (i < soundQueue2.Length)
            {
                var num = soundQueue2[i++];

                if (num > 0)
                {
                    if (soundQueue2[i + 0] == 0x10F && soundQueue2[i + 1] == 8 && soundQueue2[i + 2] == sound)
                        return true;
                    i += num;
                }
            }
            return false;
        }

        void ProcessSoundQueue()
        {
            while (_soundQueue.Count > 0)
            {
                var sound = _soundQueue.Pop();
                if (sound != 0)
                    PlaySound(sound);
            }

            if (_soundQueueIMuse.Count > 0)
            {
                var num = _soundQueueIMuse.Dequeue();
                var args = new int[16];
                for (int i = 0; i < num; i++)
                {
                    args[i] = _soundQueueIMuse.Dequeue();
                }
                if (_vm.TownsPlayer != null)
                    _vm.Variables[_vm.VariableSoundResult.Value] = (short)_vm.TownsPlayer.DoCommand(num, args);
                else if (_vm.IMuse != null)
                    _vm.Variables[_vm.VariableSoundResult.Value] = _vm.IMuse.DoCommand(num, args);
            }
        }

        void PlaySound(int soundID)
        {
            var res = _vm.ResourceManager.GetSound(MusicType, soundID);
            if (res == null)
                return;

            if (_vm.Game.GameId == GameId.Monkey1)
            {
                // Works around the fact that in some places in MonkeyEGA/VGA,
                // the music is never explicitly stopped.
                // Rather it seems that starting a new music is supposed to
                // automatically stop the old song.
                if (_vm.IMuse != null)
                {
                    if (System.Text.Encoding.UTF8.GetString(res, 0, 4) != "ASFX")
                        _vm.IMuse.StopAllSounds();
                }
            }

            if (_vm.MusicEngine != null)
            {
                _vm.MusicEngine.StartSound(soundID);
            }

            if (_vm.TownsPlayer != null)
                _currentCDSound = _vm.TownsPlayer.GetCurrentCdaSound();
        }

        void SetupSfxFile()
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path);
            _sfxFilename = (from filename in new[] { _vm.Game.Id + ".sou", "monster.sou" }
                            let path = ScummHelper.NormalizePath(ServiceLocator.FileStorage.Combine(dir, filename))
                            where path != null
                            select path).FirstOrDefault();
        }

        bool IsSfxFinished()
        {
            return !_mixer.HasActiveChannelOfType(SoundType.SFX);
        }

        bool IsMouthSyncOff(int pos)
        {
            int j;
            bool val = true;
            var ms = 0;

            _endOfMouthSync = false;
            do
            {
                val = !val;
                j = _mouthSyncTimes[ms++];
                if (j == 0xFFFF)
                {
                    _endOfMouthSync = true;
                    break;
                }
            } while (pos > j);
            return val;
        }

        SoundHandle StartTalkSound(int offset, int b, int mode)
        {
            Stream file;
            SoundHandle handle = null;
            var id = -1;

            if (_vm.Game.GameId == GameId.CurseOfMonkeyIsland)
            {
                sfxMode |= mode;
                return null;
            }
            else if (_vm.Game.GameId == GameId.Dig)
            {
                sfxMode |= mode;
                if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo)))
                    return null;
                throw new NotImplementedException();
            }
            else
            {
                if (_sfxFilename == null)
                {
                    //                    Console.Error.WriteLine("StartTalkSound: SFX file not found");
                    return handle;
                }

                // Some games frequently assume that starting one sound effect will
                // automatically stop any other that may be playing at that time. So
                // that is what we do here, but we make an exception for speech.
                if (mode == 1 && (_vm.Game.GameId == GameId.Tentacle || _vm.Game.GameId == GameId.SamNMax))
                {
                    id = 777777 + _talkSoundChannel;
                    _mixer.StopID(id);
                }

                int num = 0;
                if (b > 8)
                {
                    num = (b - 8) >> 1;
                }

                offset += 8;

                _mouthSyncTimes = new ushort[num + 1];
                file = ServiceLocator.FileStorage.OpenFileRead(_sfxFilename);
                var br = new BinaryReader(file);
                file.Seek(offset, SeekOrigin.Begin);
                for (int i = 0; i < num; i++)
                {
                    _mouthSyncTimes[i] = br.ReadUInt16BigEndian();
                }

                // Adjust offset to account for the mouth sync times. It is noteworthy
                // that we do not adjust the size here for compressed streams, since
                // they only set size to the size of the compressed sound data.
                offset += num * 2;
                // TODO: In case we ever set up the size for VOC streams, we should
                // really check whether the size contains the _mouthSyncTimes.
                // if (SoundMode == SoundMode.VOCMode)
                //      size -= num * 2;

                _mouthSyncTimes[num] = 0xFFFF;
                sfxMode |= mode;
                _curSoundPos = 0;
                _mouthSyncMode = true;
            }

            var input = new VocStream(file, true);

            var iMuseDigital = _vm.MusicEngine as IMuseDigital;
            if (iMuseDigital != null)
            {
                iMuseDigital.StartVoice(TalkSoundID, input);
            }
            else
            {
                if (mode == 1)
                {
                    handle = _mixer.PlayStream(SoundType.SFX, input, id);
                }
                else
                {
                    handle = _mixer.PlayStream(SoundType.Speech, input, id);
                }
            }
            return handle;
        }

        void OnCDTimer(object sender)
        {
            // FIXME: Turn off the timer when it's no longer needed. In theory, it
            // should be possible to check with pollCD(), but since CD sound isn't
            // properly restarted when reloading a saved game, I don't dare to.

            _vm.Variables[_vm.VariableMusicTimer.Value] += 6;
        }

        readonly ScummEngine _vm;
        readonly IMixer _mixer;

        Timer _timer;
        Stack<int> _soundQueue;
        Queue<int> _soundQueueIMuse;

        string _sfxFilename;
        int _talkSoundA1;
        int _talkSoundB1;
        int _talkSoundA2;
        int _talkSoundB2;
        int _talkSoundChannel;
        int _talkSoundMode;
        ushort[] _mouthSyncTimes = new ushort[64];
        int sfxMode;
        int _curSoundPos;
        bool _mouthSyncMode;
        SoundHandle _talkChannelHandle;
        bool _endOfMouthSync;
        int _currentCDSound;

        bool _soundsPaused;

        SoundHandle _loomSteamCDAudioHandle;
        bool _isLoomSteam;
    }
}
