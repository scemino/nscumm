using NScumm.Core.Audio;
using System.Threading;
using NScumm.Core.Audio.SampleProviders;
using System;

namespace NScumm.MonoGame
{
    class NullMixer : IAudioOutput
    {
        readonly Timer _timer;
        byte[] _samples;
        IAudioSampleProvider _audioSampleProvider;

        public NullMixer() 
        {
            _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Play()
        {
            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1.0));
        }

        public void Pause()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void SetSampleProvider(IAudioSampleProvider audioSampleProvider)
        {
            _audioSampleProvider = audioSampleProvider;
            _samples = new byte[_audioSampleProvider.AudioFormat.AverageBytesPerSecond];
            ReadSamples();
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnTimer(object state)
        {
            if (_audioSampleProvider != null)
            {
                ReadSamples();
            }
        }

        private void ReadSamples()
        {
            _audioSampleProvider.Read(_samples, _samples.Length);
        }
    }
}
