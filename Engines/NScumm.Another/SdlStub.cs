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
using System.Threading;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.SampleProviders;
using NScumm.Core.Graphics;

namespace NScumm.Another
{
    internal class SdlStub : IAnotherSystem
    {
        private class SampleProvider : IAudioSampleProvider
        {
            private readonly AudioCallback _callback;
            private readonly object _param;
            public AudioFormat AudioFormat { get; }

            public SampleProvider(AudioCallback callback, object param)
            {
                _callback = callback;
                _param = param;
                AudioFormat = new AudioFormat(SoundSampleRate, 1, 8);
            }

            public int Read(byte[] samples, int count)
            {
                _callback(_param, samples, count);
                return count;
            }
        }

        private const int SoundSampleRate = 22050;

        private readonly byte[] _offscreen;
        private readonly Color[] _palette;

        public PlayerInput Input { get; }
        public int OutputSampleRate => SoundSampleRate;
        public BytePtr OffScreenFramebuffer => _offscreen;

        public SdlStub()
        {
            Input = new PlayerInput();
            _offscreen = new byte[Video.ScreenWidth * Video.ScreenHeight];
            _palette = new Color[Video.NumColors];
        }

        public void SetPalette(byte start, byte numEntries, Ptr<Color> buf)
        {
            //assert(start + numEnties <= 16);
            Array.Copy(buf.Data, buf.Offset, _palette, start, numEntries);
            Engine.Instance.OSystem.GraphicsManager.SetPalette(_palette, 0, 16);
        }

        public void CopyRect(ushort x, ushort y, ushort w, ushort h, BytePtr buf)
        {
            const int pitch = Video.ScreenWidth / 2;
            buf += y * pitch + x;
            BytePtr p = _offscreen;

            //For each line
            while (h-- != 0)
            {
                //One byte gives us two pixels, we only need to iterate w/2 times.
                for (int i = 0; i < w / 2; ++i)
                {
                    //Extract two palette indices from upper byte and lower byte.
                    byte p1 = (byte) (buf[i] >> 4);
                    byte p2 = (byte) (buf[i] & 0xF);

                    //Get the pixel value from the palette and write in in offScreen.
                    p.Value = p1;
                    p.Offset++;
                    p.Value = p2;
                    p.Offset++;
                }

                p += Video.ScreenWidth - w;
                buf += pitch;
            }

            Engine.Instance.OSystem.GraphicsManager.CopyRectToScreen(_offscreen, Video.ScreenWidth, 0, 0,
                Video.ScreenWidth, Video.ScreenHeight);
            Engine.Instance.OSystem.GraphicsManager.UpdateScreen();
        }

        public void ProcessEvents()
        {
            var ev = Engine.Instance.OSystem.InputManager.GetState();
            //printf("type %d, key=%d\n",ev.type,ev.key.keysym.sym);
            if (ev.IsKeyUp(KeyCode.Left))
            {
                Input.DirMask &= ~Direction.Left;
            }
            if (ev.IsKeyUp(KeyCode.Right))
            {
                Input.DirMask &= ~Direction.Right;
            }
            if (ev.IsKeyUp(KeyCode.Up))
            {
                Input.DirMask &= ~Direction.Up;
            }
            if (ev.IsKeyUp(KeyCode.Down))
            {
                Input.DirMask &= ~Direction.Down;
            }
            if (ev.IsKeyUp(KeyCode.Space) ||
                ev.IsKeyUp(KeyCode.Return))
            {
                Input.Button = false;
            }
            if (ev.IsKeyDown(KeyCode.LeftAlt))
            {
                if (ev.IsKeyDown(KeyCode.X))
                {
                    Input.Quit = true;
                }
            }
            else if (ev.IsKeyDown(KeyCode.LeftControl))
            {
                if (ev.IsKeyDown(KeyCode.S))
                {
                    Input.Save = true;
                }
                else if (ev.IsKeyDown(KeyCode.L))
                {
                    Input.Load = true;
                }
                else if (ev.IsKeyDown(KeyCode.F))
                {
                    Input.FastMode = true;
                }
                else if (ev.IsKeyDown(KeyCode.Plus))
                {
                    Input.StateSlot = 1;
                }
                else if (ev.IsKeyDown(KeyCode.Minus))
                {
                    Input.StateSlot = -1;
                }
            }
            //input.lastChar = ev.key.keysym.sym;
            if (ev.IsKeyDown(KeyCode.Left))
            {
                Input.DirMask |= Direction.Left;
            }
            if (ev.IsKeyDown(KeyCode.Right))
            {
                Input.DirMask |= Direction.Right;
            }
            if (ev.IsKeyDown(KeyCode.Up))
            {
                Input.DirMask |= Direction.Up;
            }
            if (ev.IsKeyDown(KeyCode.Down))
            {
                Input.DirMask |= Direction.Down;
            }
            if (ev.IsKeyDown(KeyCode.Space) ||
                ev.IsKeyDown(KeyCode.Return))
            {
                Input.Button = true;
            }
            if (ev.IsKeyDown(KeyCode.C))
            {
                Input.Code = true;
            }
            if (ev.IsKeyDown(KeyCode.P))
            {
                Input.Pause = true;
            }
            Engine.Instance.OSystem.InputManager.ResetKeys();
        }

        public void Sleep(int duration)
        {
            ServiceLocator.Platform.Sleep(duration);
        }

        public uint GetTimeStamp()
        {
            return (uint) ServiceLocator.Platform.GetMilliseconds();
        }

        public void StartAudio(AudioCallback callback, object param)
        {
            Engine.Instance.OSystem.AudioOutput.SetSampleProvider(new SampleProvider(callback, param));
            Engine.Instance.OSystem.AudioOutput.Play();
        }

        public void StopAudio()
        {
            Engine.Instance.OSystem.AudioOutput.Stop();
            Engine.Instance.OSystem.AudioOutput.SetSampleProvider(null);
        }

        public object AddTimer(int delay, TimerCallback callback, object param)
        {
            return new Timer(state => callback(delay, state), param, 0, delay);
        }

        public void RemoveTimer(object timerId)
        {
            ((Timer) timerId).Dispose();
        }
    }
}