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
using System.Timers;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.IMuse;
using System.IO;

namespace NScumm.Core
{
    class Sound: ISoundRepository
    {
        ScummEngine vm;
        Timer timer;
        Stack<int> soundQueue;
        Queue<int> soundQueueIMuse;
        const int BufferSize = 4096;
        readonly IOpl opl;
        long minicnt;
        bool playing;
        IAudioStream stream;
        IMuse imuse;
        IMuseSysEx sysEx;

        string _sfxFilename;
        int _talk_sound_a1;
        int _talk_sound_b1;
        int _talk_sound_a2;
        int _talk_sound_b2;
        int _talk_sound_channel;
        int _talk_sound_mode;
        ushort[] _mouthSyncTimes;
        int _sfxMode;
        int _curSoundPos;
        bool _mouthSyncMode;
        IMixer _mixer;
        SoundHandle _talkChannelHandle;
        bool _endOfMouthSync;

        public IMuse IMuse { get { return imuse; } }

        public int SfxMode { get { return _sfxMode; } }

        class AudioStream: IAudioStream
        {
            readonly Sound sound;

            #region IAudioStream implementation

            public short[] Read()
            {
                return sound.Read();
            }

            public int Frequency
            {
                get
                {
                    return 49700;
                }
            }

            #endregion

            public AudioStream(Sound sound)
            {
                this.sound = sound;
            }
        }

        public Sound(ScummEngine vm, IMixer mixer)
        {
            this.vm = vm;
            soundQueue = new Stack<int>();
            soundQueueIMuse = new Queue<int>();
            timer = new Timer(100.7);
            timer.Elapsed += OnCDTimer;

            // initialize output & player
            opl = new OPL3();
            imuse = new IMuse(this, opl);
            stream = new AudioStream(this);
//            driver.Play(stream);
            _mixer = mixer;
        }

        public int GetMusicTimer()
        {
            var refresh = imuse.GetMusicTimer();
            return (int)refresh;
        }

        public void PlayCDTrack(int track, int numLoops, int startFrame, int duration)
        {
            // Reset the music timer variable at the start of a new track
            vm.Variables[vm.VariableMusicTimer.Value] = 0;

            // Play it
            //if (!_soundsPaused)
            //    g_system->getAudioCDManager()->play(track, numLoops, startFrame, duration);

            // Start the timer after starting the track. Starting an MP3 track is
            // almost instantaneous, but a CD player may take some time. Hopefully
            // playCD() will block during that delay.
            StartCDTimer();
        }

        public void StartCDTimer()
        {
            timer.Start();
        }

        public void StopCD()
        {
            timer.Stop();
        }

        void OnCDTimer(object sender, EventArgs e)
        {
            // FIXME: Turn off the timer when it's no longer needed. In theory, it
            // should be possible to check with pollCD(), but since CD sound isn't
            // properly restarted when reloading a saved game, I don't dare to.

            vm.Variables[vm.VariableMusicTimer.Value] += 6;
        }

        public void AddSoundToQueue(int sound)
        {
            if (vm.VariableLastSound.HasValue)
                vm.Variables[vm.VariableLastSound.Value] = sound;

            soundQueue.Push(sound);
        }

        public void ProcessSound()
        {
            if (vm.Game.Version >= 7)
            {
                ProcessSfxQueues();
            }
            else
            {
                ProcessSfxQueues();
                ProcessSoundQueue();
            }
        }

        void ProcessSoundQueue()
        {
            while (soundQueue.Count > 0)
            {
                var sound = soundQueue.Pop();
                if (sound != 0)
                    PlaySound(sound);
            }

            if (soundQueueIMuse.Count > 0)
            {
                var num = soundQueueIMuse.Dequeue();
                var args = new int[16];
                for (int i = 0; i < num; i++)
                {
                    args[i] = soundQueueIMuse.Dequeue();
                }
                vm.Variables[vm.VariableSoundResult.Value] = imuse.DoCommand(num, args);
            }
        }

        void PlaySound(int sound)
        {
            imuse.StartSound(sound);
        }

        public void StopAllSounds()
        {
            soundQueue.Clear();
        }

        public bool IsSoundRunning(int snd)
        {
            return soundQueue.Contains(snd);
        }

        //        public void Update()
        //        {
        //            driver.Update(stream);
        //    }

        short[] Read()
        {
            long i, towrite = BufferSize;
            var buffer = new short[towrite];
            long pos = 0;

            // Prepare audiobuf with emulator output
            while (towrite > 0)
            {
                while (minicnt < 0)
                {
                    minicnt += stream.Frequency * 4;
                    playing = imuse.Update();
                }
                i = Math.Min(towrite, (long)(minicnt / imuse.GetMusicTimer() + 4) & ~3);
                var n = Update(buffer, pos, i);
                pos += n;
                towrite -= i;
                minicnt -= (long)(imuse.GetMusicTimer() * i);
            }

            return buffer;
        }

        long Update(short[] buf, long pos, long samples)
        {
            for (int i = 0; i < samples; i += 4)
            {
                var data = opl.Read();
                Array.Copy(data, 0, buf, pos, 2);
                pos += 2;
            }
            return samples / 2;
        }

        public void SoundKludge(int[] items)
        {
            if (items[0] == -1)
            {
                ProcessSoundQueue();
            }
            else
            {
                soundQueueIMuse.Enqueue(items.Length);
                foreach (var item in items)
                {
                    soundQueueIMuse.Enqueue(item);
                }
            }
        }

        #region ISoundRepository implementation

        byte[] ISoundRepository.GetSound(int id)
        {
            return vm.ResourceManager.GetSound(id);
        }

        #endregion

        public void TalkSound(int a, int b, int mode, int channel = 0)
        {
            if (mode == 1)
            {
                _talk_sound_a1 = a;
                _talk_sound_b1 = b;
                _talk_sound_channel = channel;
            }
            else
            {
                _talk_sound_a2 = a;
                _talk_sound_b2 = b;
            }

            _talk_sound_mode |= mode;
        }

        public void SetupSound()
        {
            SetupSfxFile();

            // TODO: fullthrottle
//            if (_vm->_game.id == GID_FT) {
//                    _vm->VAR(_vm->VAR_VOICE_BUNDLE_LOADED) = _sfxFilename.empty() ? 0 : 1;
//                }
        }

        void SetupSfxFile()
        {
            var dir = Path.GetDirectoryName(vm.Game.Path);
            _sfxFilename = (from filename in new []{ vm.Game.Id + ".sou", "monster.sou" }
                                     let path=ScummHelper.NormalizePath(Path.Combine(dir, filename))
                                     where path != null
                                     select path).FirstOrDefault();
        }

        public void ProcessSfxQueues()
        {
            if (_talk_sound_mode != 0)
            {
                if ((_talk_sound_mode & 1) != 0)
                    StartTalkSound(_talk_sound_a1, _talk_sound_b1, 1);
                if ((_talk_sound_mode & 2) != 0)
                    _talkChannelHandle = StartTalkSound(_talk_sound_a2, _talk_sound_b2, 2);
                _talk_sound_mode = 0;
            }

            int act = vm.TalkingActor;
            if ((_sfxMode & 2) != 0 && act != 0)
            {
                bool finished;

//                if (vm._imuseDigital)
//                {
//                    finished = !isSoundRunning(kTalkSoundID);
//                }
//                else if (_vm->_game.heversion >= 60)
//                {
//                    finished = !isSoundRunning(1);
//                }
//                else
//                {
                finished = !_mixer.IsSoundHandleActive(_talkChannelHandle);
//                }

                if ((uint)act < 0x80 && ((vm.Game.Version == 8) || (vm.Game.Version <= 7 && !vm.String[0].NoTalkAnim)))
                {
                    var a = vm.Actors[act];
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

                        if (vm.Game.Version <= 6 && finished)
                            a.RunTalkScript(a.TalkStopFrame);
                    }
                }

                if (finished && vm.TalkDelay == 0)
                {
                    if (!(vm.Game.Version == 8 && vm.Variables[vm.VariableHaveMessage.Value] == 0))
                        vm.StopTalk();
                }
            }

            if ((_sfxMode & 1) != 0)
            {
                if (IsSfxFinished())
                {
                    _sfxMode &= ~1;
                }
            }
        }

        public void StopTalkSound()
        {
            if ((_sfxMode & 2) != 0)
            {
                //                if (_vm->_imuseDigital) {
                //                        _vm->_imuseDigital->stopSound(kTalkSoundID);
                //                } else if (_vm->_game.heversion >= 60) {
                //                        stopSound(1);
                //                } else {
                _mixer.StopHandle(_talkChannelHandle);
                //                }
                _sfxMode &= ~2;
            }
        }

        public void StopSound(int sound)
        {
//            if (sound != 0 && sound == _currentCDSound)
//            {
//                _currentCDSound = 0;
//                stopCD();
//                stopCDTimer();
//            }

            if (vm.Game.Version < 7)
                _mixer.StopID(sound);

//            if (vm._musicEngine)
//                vm->_musicEngine->stopSound(sound);

            // TODO:
//            for (var i = 0; i < soundQueue.Count; i++)
//            {
//                if (soundQueue[i] == sound)
//                {
//                    soundQueue[i] = 0;
//                }
//            }
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
            SoundHandle handle = null;
            var id = -1;
            if (_sfxFilename == null)
            {
                Console.Error.WriteLine("StartTalkSound: SFX file not found");
                return handle;
            }

            // Some games frequently assume that starting one sound effect will
            // automatically stop any other that may be playing at that time. So
            // that is what we do here, but we make an exception for speech.

            if (mode == 1 && (vm.Game.Id == "tentacle" || vm.Game.Id == "samnmax"))
            {
                id = 777777 + _talk_sound_channel;
                _mixer.StopID(id);
            }

            int num = 0;
            if (b > 8)
            {
                num = (b - 8) >> 1;
            }

            offset += 8;

            _mouthSyncTimes = new ushort[num + 1];
            var file = File.OpenRead(_sfxFilename);
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
            // if (_soundMode == SoundMode.VOCMode)
            //      size -= num * 2;

            _mouthSyncTimes[num] = 0xFFFF;
            _sfxMode |= mode;
            _curSoundPos = 0;
            _mouthSyncMode = true;

            var input = new VocStream(file, true);
            if (mode == 1)
            {
                handle = _mixer.PlayStream(SoundType.SFX, input, id);
            }
            else
            {
                handle = _mixer.PlayStream(SoundType.Speech, input, id);
            }
            return handle;
        }
    }
}
