using NScumm.Core.Audio;

namespace NScumm.Sword1
{
    internal class Music
    {
        private IMixer _mixer;
        private ushort _volumeL;
        private ushort _volumeR;
        private uint _sampleRate;

        public Music(IMixer mixer)
        {
            _mixer = mixer;
            _sampleRate = (uint) mixer.OutputRate;
            _volumeL = _volumeR = 192;
        }

        public void StartMusic(int tuneId, int loopFlag)
        {
            // TODO: StartMusic
        }

        public void FadeDown()
        {
            // TODO: FadeDown
        }

        public void GiveVolume(out byte volL, out byte volR)
        {
            volL = (byte)_volumeL;
            volR = (byte)_volumeR;
        }

        public void SetVolume(byte volL, byte volR)
        {
            _volumeL = volL;
            _volumeR = volR;
        }
    }
}