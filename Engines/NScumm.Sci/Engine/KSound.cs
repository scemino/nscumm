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

using NScumm.Sci.Sound;
using System;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kDoAudio(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }

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
            SciEngine.Instance._soundCmd.kDoSoundRestore(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundDispose(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundDispose(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundMute(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundMute(argc, argv);
        }

        private static Register kDoSoundStop(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundStop(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundPause(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundPause(argc, argv, s.r_acc);
        }

        private static Register kDoSoundResumeAfterRestore(EngineState s, int argc, StackPtr? argv)
        {
            // SCI0 only command
            //  It's called right after restoring a game - it's responsible to kick off playing music again
            //  we don't need this at all, so we don't do anything here
            return s.r_acc;
        }

        private static Register kDoSoundMasterVolume(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundMasterVolume(argc, argv);
        }

        private static Register kDoSoundUpdate(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundUpdate(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundFade(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundFade(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundGetPolyphony(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGetPolyphony(argc, argv);
        }

        private static Register kDoSoundStopAll(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundStopAll(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundUpdateCues(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundUpdateCues(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSendMidi(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSendMidi(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundGlobalReverb(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGlobalReverb(argc, argv);
        }

        private static Register kDoSoundSetHold(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetHold(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundDummy(EngineState s, int argc, StackPtr? argv)
        {
            Warning("cmdDummy invoked");    // not supposed to occur
            return s.r_acc;
        }

        private static Register kDoSoundGetAudioCapability(EngineState s, int argc, StackPtr? argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGetAudioCapability(argc, argv);
        }

        private static Register kDoSoundSuspend(EngineState s, int argc, StackPtr? argv)
        {
            Warning("kDoSound(suspend): STUB");
            return s.r_acc;
        }

        private static Register kDoSoundSetVolume(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetVolume(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSetPriority(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetPriority(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSetLoop(EngineState s, int argc, StackPtr? argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetLoop(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSync(EngineState s, int argc, StackPtr? argv)
        {
            SegManager segMan = s._segMan;
            switch ((AudioSyncCommands)argv.Value[0].ToUInt16())
            {
                case AudioSyncCommands.Start:
                    {
                        ResourceId id;

                        SciEngine.Instance._audio.StopSoundSync();

                        // Load sound sync resource and lock it
                        if (argc == 3)
                        {
                            id = new ResourceId(ResourceType.Sync, argv.Value[2].ToUInt16());
                        }
                        else if (argc == 7)
                        {
                            id = new ResourceId(ResourceType.Sync36, argv.Value[2].ToUInt16(), (byte)argv.Value[3].ToUInt16(), (byte)argv.Value[4].ToUInt16(),
                                            (byte)argv.Value[5].ToUInt16(), (byte)argv.Value[6].ToUInt16());
                        }
                        else {
                            Warning($"kDoSync: Start called with an unknown number of parameters ({argc})");
                            return s.r_acc;
                        }

                        SciEngine.Instance._audio.SetSoundSync(id, argv.Value[1], segMan);
                        break;
                    }
                case AudioSyncCommands.Next:
                    SciEngine.Instance._audio.DoSoundSync(argv.Value[1], segMan);
                    break;
                case AudioSyncCommands.Stop:
                    SciEngine.Instance._audio.StopSoundSync();
                    break;
                default:
                    throw new InvalidOperationException($"DoSync: Unhandled subfunction {argv.Value[0].ToUInt16()}");
            }

            return s.r_acc;
        }

        public string GetKernelName(ushort number)
        {
            // FIXME: The following check is a temporary workaround for an issue
            // leading to crashes when using the debugger's backtrace command.
            if (number >= _kernelNames.Count)
                return _invalid;
            return _kernelNames[number];
        }
    }
}
