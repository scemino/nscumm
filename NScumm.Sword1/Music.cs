using NScumm.Core.Audio;

namespace NScumm.Sword1
{
    internal class Music
    {
        private IMixer _mixer;

        public Music(IMixer mixer)
        {
            _mixer = mixer;
        }

        public void StartMusic(int tuneId, int loopFlag)
        {
            // TODO: StartMusic
        }

        public void FadeDown()
        {
            // TODO: FadeDown
        }
    }
}