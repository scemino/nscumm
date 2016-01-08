//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        /// <summary>
        /// Used for synthesized music playback
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kDoSound(EngineState s, int argc, StackPtr? argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)SciEngine.Instance.Features.DetectDoSoundType());
            throw new System.InvalidOperationException("not supposed to call this");
        }

        private static Register kDoSoundInit(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundInit(argc, argv);
            return s.r_acc;
        }
        private static Register kDoSoundPlay(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundPlay(argc, argv);
            return s.r_acc;
        }
        private static Register kDoSoundRestore(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundDispose(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundMute(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundMute(argc, argv);
        }
        private static Register kDoSoundStop(EngineState s, int argc, StackPtr? argv)
        {
            // TODO: sound
            return s.r_acc;
        }
        private static Register kDoSoundPause(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundResumeAfterRestore(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundMasterVolume(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundUpdate(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundFade(EngineState s, int argc, StackPtr? argv)
        {
            // TODO: sound
            return s.r_acc;
        }
        private static Register kDoSoundGetPolyphony(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundStopAll(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundUpdateCues(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundSendMidi(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundGlobalReverb(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundSetHold(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundDummy(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundGetAudioCapability(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }

        public string GetKernelName(ushort number)
        {
            // FIXME: The following check is a temporary workaround for an issue
            // leading to crashes when using the debugger's backtrace command.
            if (number >= _kernelNames.Count)
                return _invalid;
            return _kernelNames[number];
        }

        private static Register kDoSoundSuspend(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundSetVolume(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundSetPriority(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
        private static Register kDoSoundSetLoop(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }

    }
}
