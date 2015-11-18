using System;
using NScumm.Core.Audio;

namespace NScumm.Sword1
{
    internal class Music
    {
        private IMixer mixer;

        public Music(IMixer mixer)
        {
            this.mixer = mixer;
        }

        public void StartMusic(int i, int i1)
        {
            throw new System.NotImplementedException();
        }

        internal void StartMusic(uint v1, int v2)
        {
            throw new NotImplementedException();
        }
    }
}