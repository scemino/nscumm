//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

using System.IO;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;

namespace NScumm.Core.Video
{
    /// <summary>
    /// A wrapper around the VMD code that implements the VideoDecoder
    /// API.
    /// </summary>
    public class AdvancedVMDDecoder: VideoDecoder
    {
        class VMDVideoTrack : FixedRateVideoTrack
        {
            readonly VMDDecoder _decoder;

            public override ushort Width => _decoder.Width;
            public override ushort Height => _decoder.Height;
            public override PixelFormat PixelFormat => _decoder.PixelFormat;
            public override int CurrentFrame => _decoder.CurFrame;
            public override int FrameCount => _decoder.FrameCount;
            protected override Rational FrameRate => _decoder.FrameRate;

            public override byte[] GetPalette()
            {
                return _decoder.Palette;
            }

            public override bool HasDirtyPalette()
            {
                return _decoder.HasDirtyPalette;
            }

            public override Surface DecodeNextFrame()
            {
                return _decoder.DecodeNextFrame();
            }

            public VMDVideoTrack(VMDDecoder decoder )
            {
                _decoder = decoder;
            }
        }

        class VMDAudioTrack : AudioTrack
        {
            readonly VMDDecoder _decoder;

            public override IAudioStream AudioStream => _decoder.AudioStream;
            public override SoundType SoundType => _decoder.SoundType;

            public VMDAudioTrack(VMDDecoder decoder)
            {
                _decoder = decoder;
            }
        }

        readonly VMDDecoder _decoder;
        VMDVideoTrack _videoTrack;
        VMDAudioTrack _audioTrack;

        public AdvancedVMDDecoder(SoundType soundType = SoundType.Plain)
        {
            _decoder = new VMDDecoder(Engine.Instance.Mixer, soundType);
            _decoder.SetAutoStartSound(false);
        }

        public override bool LoadStream(Stream stream)
        {
            Close();

            if (!_decoder.LoadStream(stream))
                return false;

            if (_decoder.HasVideo)
            {
                _videoTrack = new VMDVideoTrack(_decoder);
                AddTrack(_videoTrack);
            }

            if (_decoder.HasSound)
            {
                _audioTrack = new VMDAudioTrack(_decoder);
                AddTrack(_audioTrack);
            }

            return true;
        }

        public override void Close()
        {
            base.Close();
            _decoder.Close();
        }

        public void SetSurfaceMemory(BytePtr mem, ushort width, ushort height, byte bpp)
        {
            _decoder.SetSurfaceMemory(mem, width, height, bpp);
        }
    }
}