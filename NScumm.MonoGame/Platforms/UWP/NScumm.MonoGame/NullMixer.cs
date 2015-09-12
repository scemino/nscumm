using System;
using NScumm.Core.Audio;
using System.Threading;

namespace NScumm.MonoGame
{
    class NullMixer : Mixer, IDisposable
    {
        private readonly Timer timer;
        private short[] samples;

        public NullMixer() 
            : base(44100)
        {
            timer = new Timer(OnTimer, this, 0, 1000);
            samples = new short[2048 * 4];
        }

        private void OnTimer(object state)
        {
            MixCallback(samples);
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
