using NScumm.Core.Audio;
using NScumm.Core.Audio.SampleProviders;
using System;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX;
using System.Threading;

namespace NScumm.MonoGame
{
    class XAudio2Mixer : IAudioOutput
    {
        private readonly Timer _timer;
        private IAudioSampleProvider _audioSampleProvider;
        private XAudio2 _xAudio;
        private SourceVoice _voice;
        private byte[] _samples;
        private DataStream _dataStream;
        private AudioBuffer _buffer;
        private object _gate = new object();
        private MasteringVoice _masteringVoice;

        public XAudio2Mixer()
        {
            _xAudio = new XAudio2();
            _xAudio.StartEngine();
            _masteringVoice = new MasteringVoice(_xAudio);
            _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Play()
        {
            if (_voice != null)
            {
                _voice.Start();
            }
            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1.0));
        }

        public void Pause()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void SetSampleProvider(IAudioSampleProvider audioSampleProvider)
        {
            var format = ToWaveFormat(audioSampleProvider.AudioFormat);
            _voice = new SourceVoice(_xAudio, format, true);
            _samples = new byte[audioSampleProvider.AudioFormat.AverageBytesPerSecond];
            _dataStream = DataStream.Create(_samples, true, true);
            _buffer = new AudioBuffer
            {
                Stream = _dataStream,
                AudioBytes = (int)_dataStream.Length,
            };
            _audioSampleProvider = audioSampleProvider;
            FillBuffer();
            _voice.Start();
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
            _dataStream.Dispose();
            _voice.Dispose();
            _masteringVoice.Dispose();
            _xAudio.Dispose();
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _voice.Stop();
        }

        private static WaveFormat ToWaveFormat(AudioFormat format)
        {
            return new WaveFormat(format.SampleRate, format.Channels);
        }

        private void OnTimer(object state)
        {
            if (_audioSampleProvider != null)
            {
                FillBuffer();
            }
        }

        private void FillBuffer()
        {
            lock (_gate)
            {
                Array.Clear(_samples, 0, _samples.Length);
                var count = _audioSampleProvider.Read(_samples, _samples.Length);
                if (count > 0)
                {
                    _dataStream.Position = 0;
                    _dataStream.Write(_samples, 0, count);
                }
            }
            _voice.SubmitSourceBuffer(_buffer, null);
        }
    }
}

