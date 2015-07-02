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
using Microsoft.Xna.Framework.Audio;
using System.Runtime.InteropServices;

namespace NScumm.MonoGame
{
    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public class Buffer
    {
        [FieldOffset(0)]
        public byte[] Bytes;
        [FieldOffset(0)]
        public short[] Shorts;
    }

    class XnaAudioDriver : Mixer
    {
        readonly DynamicSoundEffectInstance _dsei;
        Buffer _buffer;
        short[] _buf;

        public XnaAudioDriver()
            : base(44100)
        {
            _buffer = new Buffer();
            _buf = new short[13230];
            _buffer.Bytes = new byte[_buf.Length * 2];
            _dsei = new DynamicSoundEffectInstance(44100, AudioChannels.Stereo);
            _dsei.BufferNeeded += OnBufferNeeded;
            _dsei.Play();
        }

        void OnBufferNeeded(object sender, EventArgs e)
        {
            Array.Clear(_buf, 0, _buf.Length);
            var available = MixCallback(_buf);
            if (available > 0)
            {
                for (int i = 0; i < available * 2; i++)
                {
                    _buffer.Shorts[i] = _buf[i];
                }
            }
            _dsei.SubmitBuffer(_buffer.Bytes);
        }

        public void Dispose()
        {
            _dsei.Dispose();
        }

        public void Stop()
        {
            _dsei.Stop();
        }
    }
}