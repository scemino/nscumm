using NScumm.Core.Audio;
using System.Threading;
using NScumm.Core.Audio.SampleProviders;
using System;

namespace NScumm.MonoGame
{
    class NullMixer : IAudioOutput
    {
        readonly Timer timer;
        byte[] samples;
        IAudioSampleProvider _audioSampleProvider;

        public NullMixer() 
        {
            timer = new Timer(OnTimer);
        }

        public void Play()
        {
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1.0));
        }

        public void Pause()
        {
        }

        public void SetSampleProvider(IAudioSampleProvider audioSampleProvider)
        {
            _audioSampleProvider = audioSampleProvider;
            samples = new byte[_audioSampleProvider.AudioFormat.AverageBytesPerSecond];
        }

        void OnTimer(object state)
        {
            _audioSampleProvider.Read(samples, samples.Length);
        }

        public void Dispose()
        {
            Stop();
            timer.Dispose();
        }

        public void Stop()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
