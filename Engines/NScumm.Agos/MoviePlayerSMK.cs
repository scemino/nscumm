//
//  AgosEngineFeeble.cs
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
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Video;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    abstract class MoviePlayer : IMoviePlayer
    {
        protected readonly AgosEngineFeeble _vm;

        readonly IMixer _mixer;

        SoundHandle _bgSound;
        IAudioStream _bgSoundStream;

        bool _leftButtonDown;
        bool _rightButtonDown;
        protected bool _skipMovie;
        uint _ticks;

        protected MoviePlayer(AgosEngineFeeble vm)
        {
            _vm = vm;
            _mixer = _vm.Mixer;
        }

        public void Play()
        {
            if (_vm.GetBitFlag(40))
            {
                _vm.SetBitFlag(42, false);
                StartSound();
                return;
            }

            _leftButtonDown = false;
            _rightButtonDown = false;
            _skipMovie = false;

            _vm.Mixer.StopAll();

            _ticks = (uint) ServiceLocator.Platform.GetMilliseconds();

            StartSound();

            PlayVideo();
            StopVideo();

            _vm.o_killAnimate();

            if (_vm.GetBitFlag(41))
            {
                _vm.FillBackFromFront();
            }
            else
            {
                Color[] palette = new Color[256];
                _vm.ClearSurfaces();
                _vm.OSystem.GraphicsManager.SetPalette(palette, 0, 256);
            }

            _vm.FillBackGroundFromBack();
            _vm._fastFadeOutFlag = true;
        }

        public abstract bool Load();

        public abstract void PlayVideo();

        public abstract void NextFrame();

        public abstract void StopVideo();

        protected abstract void ProcessFrame();

        protected void HandleNextFrame()
        {
            var state = _vm.OSystem.InputManager.GetState();
            if (state.IsKeyDown(KeyCode.Escape))
            {
                _leftButtonDown = true;
                _rightButtonDown = true;
            }
            else if (state.IsKeyDown(KeyCode.Pause))
            {
                _vm.IsPaused = true;
            }
            if (state.IsLeftButtonDown)
            {
                _leftButtonDown = true;
            }
            if (state.IsRightButtonDown)
            {
                _rightButtonDown = true;
            }
            if (_leftButtonDown && !state.IsLeftButtonDown)
            {
                _leftButtonDown = false;
            }
            if (_rightButtonDown && !state.IsRightButtonDown)
            {
                _rightButtonDown = false;
            }

            if (_leftButtonDown && _rightButtonDown && !_vm.GetBitFlag(41))
            {
                _skipMovie = true;
                _mixer.StopHandle(_bgSound);
            }
        }

        protected virtual void StartSound()
        {
        }

        public void Dispose()
        {
        }
    }

    internal class MoviePlayerSmk : MoviePlayer
    {
        private readonly string baseName;
        private readonly SmackerDecoder _decoder;

        public MoviePlayerSmk(AgosEngineFeeble vm, string name)
            : base(vm)
        {
            _decoder = new SmackerDecoder();
            baseName = name;
            Debug(0, "Creating SMK cutscene player");
        }

        public override bool Load()
        {
            string videoName = $"{baseName}.smk";

            var videoStream = Engine.OpenFileRead(videoName);
            if (videoStream == null)
                Error("Failed to load video file {0}", videoName);
            if (!_decoder.LoadStream(videoStream))
                Error("Failed to load video stream from file {0}", videoName);

            Debug(0, "Playing video {0}", videoName);

            Engine.Instance.OSystem.GraphicsManager.IsCursorVisible = false;

            return true;
        }

        protected override void ProcessFrame()
        {
            _vm.LockScreen(screen =>
            {
                CopyFrameToBuffer(screen.Pixels,
                    (uint) ((_vm._screenWidth - _decoder.GetWidth()) / 2),
                    (uint) ((_vm._screenHeight - _decoder.GetHeight()) / 2), screen.Pitch);
            });

            uint waitTime = _decoder.GetTimeToNextFrame();

            if (waitTime == 0 && !_decoder.EndOfVideoTracks)
            {
                Warning("dropped frame {0}", _decoder.CurrentFrame);
                return;
            }

            _vm.OSystem.GraphicsManager.UpdateScreen();

            // Wait before showing the next frame
            ServiceLocator.Platform.Sleep((int) waitTime);
        }

        private void CopyFrameToBuffer(BytePtr dst, uint x, uint y, int pitch)
        {
            int h = _decoder.GetHeight();
            int w = _decoder.GetWidth();

            var surface = _decoder.DecodeNextFrame();

            if (surface == null)
                return;

            BytePtr src = surface.Pixels;
            dst.Offset += (int) (y * pitch + x);

            do
            {
                src.Copy(dst, w);
                dst.Offset += pitch;
                src.Offset += w;
            } while (--h != 0);

            if (_decoder.HasDirtyPalette)
            {
                var p = _decoder.Palette;
                var colors = new Color[256];
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.FromRgb(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
                }
                Engine.Instance.OSystem.GraphicsManager.SetPalette(colors, 0, 256);
            }
        }

        public override void PlayVideo()
        {
            while (!_decoder.EndOfVideo && !_skipMovie && !_vm.HasToQuit)
                HandleNextFrame();
        }

        public override void StopVideo()
        {
            _decoder.Close();
        }

        protected override void StartSound()
        {
            _decoder.Start();
        }

        private void HandleNextFrame()
        {
            ProcessFrame();

            base.HandleNextFrame();
        }

        public override void NextFrame()
        {
            if (_vm._interactiveVideo == VideoFlags.TYPE_LOOPING && _decoder.EndOfVideo)
                _decoder.Rewind();

            if (!_decoder.EndOfVideo)
            {
                _decoder.DecodeNextFrame();
                if (_vm._interactiveVideo == VideoFlags.TYPE_OMNITV)
                {
                    CopyFrameToBuffer(_vm.BackBuf, 465, 222, _vm._screenWidth);
                }
                else if (_vm._interactiveVideo == VideoFlags.TYPE_LOOPING)
                {
                    CopyFrameToBuffer(_vm.BackBuf, (uint) ((_vm._screenWidth - _decoder.GetWidth()) / 2),
                        (uint) ((_vm._screenHeight - _decoder.GetHeight()) / 2), _vm._screenWidth);
                }
            }
            else if (_vm._interactiveVideo == VideoFlags.TYPE_OMNITV)
            {
                _decoder.Close();
                _vm._interactiveVideo = 0;
                _vm._variableArray[254] = 6747;
            }
        }
    }
}