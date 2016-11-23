//
//  BaseSound.cs
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

using System;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Common;

namespace NScumm.Agos
{
    abstract class BaseSound
    {
        protected string _filename;
        protected IMixer _mixer;
        protected bool _freeOffsets;
        protected uint[] _offsets;

        protected BaseSound(IMixer mixer, string filename, uint @base, bool bigEndian)
        {
            _mixer = mixer;
            _filename = filename;

            var file = Engine.OpenFileRead(_filename);
            if (file == null)
                DebugHelper.Error("BaseSound: Could not open file \"{0}\"", filename);

            file.Seek(@base + 4, SeekOrigin.Begin);
            var br = new BinaryReader(file);

            int size;
            if (bigEndian)
                size = br.ReadInt32BigEndian();
            else
                size = br.ReadInt32();

            // The Feeble Files uses set amount of voice offsets
            if (size == 0)
                size = 40000;

            var res = size / 4;

            _offsets = new uint[size + 1];
            _freeOffsets = true;

            file.Seek(@base, SeekOrigin.Begin);

            for (var i = 0; i < res; i++)
            {
                if (bigEndian)
                    _offsets[i] = @base + br.ReadUInt32BigEndian();
                else
                    _offsets[i] = @base + br.ReadUInt32();
            }

            _offsets[res] = (uint) file.Length;
        }

        protected BaseSound(IMixer mixer, string filename, uint[] offsets)
        {
            _mixer = mixer;
            _filename = filename;
            _offsets = offsets;
        }

        protected Stream GetSoundStream(uint sound)
        {
            if (_offsets == null)
                return null;

            var file = Engine.OpenFileRead(_filename);
            if (file == null)
            {
                DebugHelper.Warning("BaseSound::getSoundStream: Could not open file \"{0}\"", _filename);
                return null;
            }

            int i = 1;
            while (_offsets[sound + i] == _offsets[sound])
                i++;
            uint end;
            if (_offsets[sound + i] > _offsets[sound])
            {
                end = _offsets[sound + i];
            }
            else
            {
                end = (uint) file.Length;
            }

            return new SeekableSubReadStream(file, _offsets[sound], end, true);
        }

        public SoundHandle PlaySound(uint sound, SoundType type, bool loop, int vol = 0)
        {
            return PlaySound(sound, sound, type, loop, vol);
        }

        public virtual SoundHandle PlaySound(uint sound, uint loopSound, SoundType type, bool loop, int vol = 0)
        {
            ConvertVolume(ref vol);
            return _mixer.PlayStream(type, new LoopingAudioStream(this, sound, loopSound, loop), -1, vol);
        }

        public abstract IAudioStream MakeAudioStream(uint sound);

        private static void ConvertVolume(ref int vol)
        {
            // DirectSound was originally used, which specifies volume
            // and panning differently than ScummVM does, using a logarithmic scale
            // rather than a linear one.
            //
            // Volume is a value between -10,000 and 0.
            //
            // In both cases, the -10,000 represents -100 dB. When panning, only
            // one speaker's volume is affected - just like in ScummVM - with
            // negative values affecting the left speaker, and positive values
            // affecting the right speaker. Thus -10,000 means the left speaker is
            // silent.

            int v = ScummHelper.Clip(vol, -10000, 0);
            if (v != 0)
            {
                vol = (int) (Mixer.MaxChannelVolume * Math.Pow(10.0, v / 2000.0) + 0.5);
            }
            else
            {
                vol = Mixer.MaxChannelVolume;
            }
        }
    }
}