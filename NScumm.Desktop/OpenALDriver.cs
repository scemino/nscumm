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
using NScumm.Core.Audio.SampleProviders;

namespace NScumm
{
    internal class XnaAudioDriver : IAudioOutput
    {
        private readonly DynamicSoundEffectInstance _dsei;
        private readonly byte[] _buffer;
        private IAudioSampleProvider _audioSampleProvider;
        private AudioFormat _audioFormat;

        public XnaAudioDriver()
        {
            _audioFormat = new AudioFormat(44100);

            _buffer = new byte[13230 * 2];
            _dsei = new DynamicSoundEffectInstance(_audioFormat.SampleRate,
                _audioFormat.Channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
            _dsei.BufferNeeded += OnBufferNeeded;
        }

        public void SetSampleProvider(IAudioSampleProvider audioSampleProvider)
        {
            _audioSampleProvider = audioSampleProvider;
            if (_audioSampleProvider.AudioFormat.SampleRate != _audioFormat.SampleRate)
            {
                _audioSampleProvider = new ResampleAudioSampleProvider(audioSampleProvider, _audioFormat.SampleRate);
            }
            if (_audioSampleProvider.AudioFormat.Channels == 1 && _audioFormat.Channels == 2)
            {
                _audioSampleProvider = new MonoToStereoAudioSampleProvider16(_audioSampleProvider);
            }
        }

        public void Dispose()
        {
            _dsei.BufferNeeded -= OnBufferNeeded;
            _dsei.Dispose();
        }

        public void Play()
        {
            _dsei.Play();
        }

        public void Pause()
        {
            _dsei.Pause();
        }

        public void Stop()
        {
            _dsei.Stop();
        }

        private void OnBufferNeeded(object sender, EventArgs e)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            try
            {
                var count = _audioSampleProvider?.Read(_buffer, _buffer.Length);
                _dsei.SubmitBuffer(_buffer, count ?? _buffer.Length);
            }
            catch (Exception ex)
            {
                Core.DebugHelper.Warning($"Audio error: {ex}");
            }
        }
    }
}