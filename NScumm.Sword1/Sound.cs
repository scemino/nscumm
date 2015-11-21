using NScumm.Core.Audio;

namespace NScumm.Sword1
{
    internal class Sound
    {
        public Sound(Mixer mixer, ResMan resMan)
        {
            
        }

        public void NewScreen(uint scriptVar)
        {
            // TODO:
            //if (_currentCowFile != SwordEngine::_systemVars.currentCD)
            //{
            //    if (_cowFile.isOpen())
            //        closeCowSystem();
            //    initCowSystem();
            //}

            //// Start the room's looping sounds.
            //for (uint16 cnt = 0; cnt < TOTAL_FX_PER_ROOM; cnt++)
            //{
            //    uint16 fxNo = _roomsFixedFx[screen][cnt];
            //    if (fxNo)
            //    {
            //        if (_fxList[fxNo].type == FX_LOOP)
            //            addToQueue(fxNo);
            //    }
            //    else
            //        break;
            //}
        }

        public void Engine()
        {
            // TODO:
        }

        public void QuitScreen()
        {
            // TODO:
        }

        public void CloseCowSystem()
        {
            // TODO:
        }

        public void CheckSpeechFileEndianness()
        {
            // TODO:
        }

        public bool StartSpeech(int i, int i1)
        {
            // TODO:
            return true;
        }

        public uint AddToQueue(int fxNo)
        {
            // TODO:
            return 1;
        }

        public void FnStopFx(int fxNo)
        {
            // TODO:
        }

        public bool SpeechFinished()
        {
            // TODO:
            return true;
        }

        public void StopSpeech()
        {
            // TODO:
        }

        public int AmISpeaking()
        {
            return 0;
        }
    }
}