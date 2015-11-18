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
            throw new System.NotImplementedException();
        }

        public void QuitScreen()
        {
            throw new System.NotImplementedException();
        }

        public void CloseCowSystem()
        {
            throw new System.NotImplementedException();
        }

        public void CheckSpeechFileEndianness()
        {
            throw new System.NotImplementedException();
        }
    }
}