//
//  IMuse.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core.Audio.IMuse
{
    public class IMuse: IIMuse, IMusicEngine
    {
        IPlayer player;
        object locker = new object();

        public IMuse(IPlayer player)
        {
            this.player = player;
        }

        #region IMusicEngine implementation

        public void SetMusicVolume(int vol)
        {
            throw new NotImplementedException();
        }

        public void StartSound(int sound)
        {
            lock (locker)
            {
                StartSoundCore(sound);
            }
        }

        bool StartSoundCore(int sound, int offset = 0)
        {
            player.Clear();
            player.OffsetNote = offset;
            return player.StartSound(sound);
        }

        public void StopSound(int sound)
        {
            lock (locker)
            {
                player.Clear();
            }
        }

        public void StopAllSounds()
        {
            throw new NotImplementedException();
        }

        public int GetSoundStatus(int sound)
        {
            lock (locker)
            {
                return GetSoundStatusCore(sound, true);
            }
        }

        int GetSoundStatusCore(int sound, bool ignoreFadeouts)
        {
            if (player.IsActive && (!ignoreFadeouts || !player.IsFadingOut))
            {
                if (sound == -1)
                    return player.Id;
                else if (player.Id == sound)
                    return 1;
            }
            return (sound == -1) ? 0 : GetQueueSoundStatus(sound);
        }

        int GetQueueSoundStatus(int sound)
        {
            // TODO
            return 0;
        }

        public int GetMusicTimer()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IIMuse implementation

        public int DoCommand(int num, int[] a)
        {
            var cmd = a[0] & 0xFF;
            var param = a[0] >> 8;

            if (param == 0)
            {
                switch (cmd)
                {
                    case 13:
                        return GetSoundStatusCore(a[1], true);
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (param == 1)
            {
                switch (cmd)
                {
                    case 0:
                        return -1; //player.GetParam(a[2], a[3]);
                    case 1:
                            //player.SetPriority(a[2]);
                        return 0;
                    case 2:
//                        return player.SetVolume(a[2]);
                        return 0;
                    case 3:
//                        player.SetPan(a[2]);
                        return 0;
                    case 4:
//                        return player.SetTranspose(a[2], a[3]);
                        return 0;
                    case 5:
//                        player.SetDetune(a[2]);
                        return 0;
                    case 6:
                            // WORKAROUND for bug #1324106. When playing the
                            // "flourishes" as Rapp's body appears from his ashes,
                            // MI2 sets up triggers to pause the music, in case the
                            // animation plays too slowly, and then the music is
                            // manually unpaused for the next part of the music.
                            //
                            // In ScummVM, the animation finishes slightly too
                            // quickly, and the pause command is run *after* the
                            // unpause command. So we work around it by ignoring
                            // all attempts at pausing this particular sound.
                            //
                            // I could have sworn this wasn't needed after the
                            // recent timer change, but now it looks like it's
                            // still needed after all.
//                        if (_game.Id != "monkey2" || player.Id != 183 || a[2] != 0)
//                        {
//                            player.SetSpeed(a[2]);
//                        }
                        return 0;
                    case 7:
                            //return player.Jump(a[2], a[3], a[4]) ? 0 : -1;
                        return 0;
                    case 8:
                            //return player.Scan(a[2], a[3], a[4]);
                        return 0;
                    case 9:
                            //return player.SetLoop(a[2], a[3], a[4], a[5], a[6]) ? 0 : -1;
                        return 0;
                    case 10:
                            //player.ClearLoop();
                        return 0;
                    case 11:
                            //((Part)player).SetOnOff(a[3] != 0);
                        return 0;
                    case 12:
                        return player.SetHook(a[2], a[3], a[4]);
                    case 13:
                            //return player.AddParameterFader(ParameterFader.Volume, a[2], a[3]);
                        return -1;
                    case 14:
                        return EnqueueTrigger(a[1], a[2]);
                    case 15:
                            //return EnqueueCommand(new []{ a[1], a[2], a[3], a[4], a[5], a[6], a[7] });
                        return 0;
                    case 16:
                        return ClearQueue();
                    case 19:
                            //return player.GetParam(a[2], a[3]);
                        return -1;
                    case 20:
                        return player.SetHook(a[2], a[3], a[4]);
                    case 21:
                        return -1;
                    case 22:
                            //((Part)player).Volume(a[3]);
                        return 0;
                    case 23:
                        return QueryQueue(a[1]);
                    case 24:
                        return 0;

                    default:
                        throw new NotImplementedException();
                }
            }

            return -1;
        }

        int QueryQueue(int param)
        {
            // TODO:
            switch (param)
            {
//                case 0: // Get trigger count
//                    return _trigger_count;
//                case 1: // Get trigger type
//                    if (_queue_end == _queue_pos)
//                        return -1;
//                    return _cmd_queue[_queue_end].array[1];
//                case 2: // Get trigger sound
//                    if (_queue_end == _queue_pos)
//                        return 0xFF;
//                    return _cmd_queue[_queue_end].array[2];
                default:
                    return -1;
            }
        }

        int EnqueueCommand(int[] arr)
        {
            // TODO:
            return 0;
        }

        int EnqueueTrigger(int sound, int marker)
        {
            // TODO:
            return 0;
        }

        int ClearQueue()
        {
            //TODO:
            return 0;
        }

        #endregion

    }
}
