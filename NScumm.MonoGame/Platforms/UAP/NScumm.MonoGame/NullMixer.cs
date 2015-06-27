using System;
using NScumm.Core.Audio;

namespace NScumm.MonoGame
{
    class NullMixer : Mixer, IDisposable
    {
        public NullMixer() 
            : base(44100)
        {
        }

        public void Dispose()
        {
        }
    }
}
