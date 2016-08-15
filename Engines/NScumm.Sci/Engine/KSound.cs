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
using NScumm.Core.Audio;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sci.Engine
{
    enum AudioCommands
    {
        WPlay = 1, /* Loads an audio stream */
        Play = 2, /* Plays an audio stream */
        Stop = 3, /* Stops an audio stream */
        Pause = 4, /* Pauses an audio stream */
        Resume = 5, /* Resumes an audio stream */
        Position = 6, /* Return current position in audio stream */
        Rate = 7, /* Return audio rate */
        Volume = 8, /* Return audio volume */
        Language = 9, /* Return audio language */
        CD = 10 /* Plays SCI1.1 CD audio */
    }

    partial class Kernel
    {
        private static Register kDoAudio(EngineState s, int argc, StackPtr argv)
        {
            // JonesCD uses different functions based on the cdaudio.map file
            // to use red book tracks.
            if (SciEngine.Instance._features.UsesCdTrack)
                return kDoCdAudio(s, argc, argv);

            var mixer = SciEngine.Instance.Mixer;

            switch ((AudioCommands)argv[0].ToUInt16())
            {
                case AudioCommands.WPlay:
                case AudioCommands.Play:
                    {
                        ushort module;
                        uint number;

                        SciEngine.Instance._audio.StopAudio();

                        if (argc == 2)
                        {
                            module = 65535;
                            number = argv[1].ToUInt16();
                        }
                        else if (argc == 6 || argc == 8)
                        {
                            module = argv[1].ToUInt16();
                            number = (uint)(((argv[2].ToUInt16() & 0xff) << 24) |
                                     ((argv[3].ToUInt16() & 0xff) << 16) |
                                     ((argv[4].ToUInt16() & 0xff) << 8) |
                                      (argv[5].ToUInt16() & 0xff));
                            // Removed warning because of the high amount of console spam
                            /*if (argc == 8) {
                                // TODO: Handle the extra 2 SCI21 params
                                // argv[6] is always 1
                                // argv[7] is the contents of global 229 (0xE5)
                                warning("kDoAudio: Play called with SCI2.1 extra parameters: %04x:%04x and %04x:%04x",
                                        PRINT_REG(argv[6]), PRINT_REG(argv[7]));
                            }*/
                        }
                        else
                        {
                            Warning("kDoAudio: Play called with an unknown number of parameters (%d)", argc);
                            return Register.NULL_REG;
                        }

                        // TODO: debugC(kDebugLevelSound, "kDoAudio: play sample %d, module %d", number, module);

                        // return sample length in ticks
                        if (argv[0].ToUInt16() == (ushort)AudioCommands.WPlay)
                            return Register.Make(0, (ushort)SciEngine.Instance._audio.WPlayAudio(module, number));
                        else
                            return Register.Make(0, (ushort)SciEngine.Instance._audio.StartAudio(module, number));
                    }
                case AudioCommands.Stop:
                    // TODO: debugC(kDebugLevelSound, "kDoAudio: stop");
                    SciEngine.Instance._audio.StopAudio();
                    break;
                case AudioCommands.Pause:
                    // TODO: debugC(kDebugLevelSound, "kDoAudio: pause");
                    SciEngine.Instance._audio.PauseAudio();
                    break;
                case AudioCommands.Resume:
                    // TODO: debugC(kDebugLevelSound, "kDoAudio: resume");
                    SciEngine.Instance._audio.ResumeAudio();
                    break;
                case AudioCommands.Position:
                    //debugC(kDebugLevelSound, "kDoAudio: get position");   // too verbose
                    return Register.Make(0, (ushort)SciEngine.Instance._audio.GetAudioPosition());
                case AudioCommands.Rate:
                    // TODO: debugC(kDebugLevelSound, "kDoAudio: set audio rate to %d", argv[1].toUint16());
                    SciEngine.Instance._audio.SetAudioRate(argv[1].ToUInt16());
                    break;
                case AudioCommands.Volume:
                    {
                        short volume = argv[1].ToInt16();
                        volume = (short)ScummHelper.Clip(volume, 0, AudioPlayer.AUDIO_VOLUME_MAX);
                        // TODO: debugC(kDebugLevelSound, "kDoAudio: set volume to %d", volume);
                        mixer.SetVolumeForSoundType(SoundType.Speech, volume * 2);
                        break;
                    }
                case AudioCommands.Language:
                    // In SCI1.1: tests for digital audio support
                    if (ResourceManager.GetSciVersion() == SciVersion.V1_1)
                    {
                        // TODO: debugC(kDebugLevelSound, "kDoAudio: audio capability test");
                        return Register.Make(0, 1);
                    }
                    short language = argv[1].ToInt16();

                    // athrxx: It seems from disasm that the original KQ5 FM-Towns loads a default language (Japanese) audio map at the beginning
                    // right after loading the video and audio drivers. The -1 language argument in here simply means that the original will stick
                    // with Japanese. Instead of doing that we switch to the language selected in the launcher.
                    if (SciEngine.Instance.Platform == Platform.FMTowns && language == -1)
                    {
                        // FM-Towns calls us to get the current language / also set the default language
                        // This doesn't just happen right at the start, but also when the user clicks on the Sierra logo in the game menu
                        // It uses the result of this call to either show "English Voices" or "Japanese Voices".

                        // Language should have been set by setLauncherLanguage() already (or could have been modified by the scripts).
                        // Get this language setting, so that the chosen language will get set for resource manager.
                        language = (short)SciEngine.Instance.Language;
                    }

                    //TODO: debugC(kDebugLevelSound, "kDoAudio: set language to %d", language);

                    if (language != -1)
                        SciEngine.Instance.ResMan.SetAudioLanguage(language);

                    var kLang = SciEngine.Instance.GetSciLanguage();
                    SciEngine.Instance.SetSciLanguage(kLang);

                    return Register.Make(0, (ushort)kLang);
                // 3 new subops in Pharkas CD (including CD demo). kDoAudio in Pharkas sits at seg026:038C
                case (AudioCommands)11:
                    if (argv[0].ToUInt16() == (ushort)AudioCommands.CD)
                    {
                        if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
                        {
                            // TODO: debugC(kDebugLevelSound, "kDoAudio: CD audio subop");
                            return kDoCdAudio(s, argc - 1, argv + 1);
                        }
                    }
                    // Not sure where this is used yet
                    Warning("kDoAudio: Unhandled case 11, %d extra arguments passed", argc - 1);
                    break;
                case (AudioCommands)12:
                    // SSCI calls this function with no parameters from
                    // the TalkRandCycle class and branches on the return
                    // value like a boolean. The conjectured purpose of
                    // this function is to ensure that the talker's mouth
                    // does not move if there is read jitter (slow CD
                    // drive, scratched CD). The old behavior here of not
                    // doing anything caused a nonzero value to be left in
                    // the accumulator by chance. This is equivalent, but
                    // more explicit.

                    return Register.Make(0, 1);
                case (AudioCommands)13:
                    // SSCI returns a serial number for the played audio
                    // here, used in the PointsSound class. The reason is severalfold:

                    // 1. SSCI does not support multiple wave effects at once
                    // 2. FPFP may disable its icon bar during the points sound.
                    // 3. Each new sound preempts any sound already playing.
                    // 4. If the points sound is interrupted before completion,
                    // the icon bar could remain disabled.

                    // Since points (1) and (3) do not apply to us, we can simply
                    // return a constant here. This is equivalent to the
                    // old behavior, as above.
                    return Register.Make(0, 1);
                case (AudioCommands)17:
                    // Seems to be some sort of audio sync, used in SQ6. Silenced the
                    // warning due to the high level of spam it produces. (takes no params)
                    //warning("kDoAudio: Unhandled case 17, %d extra arguments passed", argc - 1);
                    break;
                default:
                    Warning("kDoAudio: Unhandled case %d, %d extra arguments passed", argv[0].ToUInt16(), argc - 1);
                    break;
            }

            return s.r_acc;
        }

        private static Register kDoCdAudio(EngineState s, int argc, StackPtr argv)
        {
            throw new NotImplementedException("kDoAudio");
        }

        /// <summary>
        /// Used for synthesized music playback
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kDoSound(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)SciEngine.Instance.Features.DetectDoSoundType());
            throw new InvalidOperationException("not supposed to call this");
        }

        private static Register kDoSoundInit(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundInit(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundPlay(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundPlay(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundRestore(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundRestore(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundDispose(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundDispose(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundMute(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundMute(argc, argv);
        }

        private static Register kDoSoundStop(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundStop(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundPause(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundPause(argc, argv, s.r_acc);
        }

        private static Register kDoSoundResumeAfterRestore(EngineState s, int argc, StackPtr argv)
        {
            // SCI0 only command
            //  It's called right after restoring a game - it's responsible to kick off playing music again
            //  we don't need this at all, so we don't do anything here
            return s.r_acc;
        }

        private static Register kDoSoundMasterVolume(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundMasterVolume(argc, argv);
        }

        private static Register kDoSoundUpdate(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundUpdate(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundFade(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundFade(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundGetPolyphony(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGetPolyphony(argc, argv);
        }

        private static Register kDoSoundStopAll(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundStopAll(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundUpdateCues(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundUpdateCues(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSendMidi(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSendMidi(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundGlobalReverb(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGlobalReverb(argc, argv);
        }

        private static Register kDoSoundSetHold(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetHold(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundDummy(EngineState s, int argc, StackPtr argv)
        {
            Warning("cmdDummy invoked");    // not supposed to occur
            return s.r_acc;
        }

        private static Register kDoSoundGetAudioCapability(EngineState s, int argc, StackPtr argv)
        {
            return SciEngine.Instance._soundCmd.kDoSoundGetAudioCapability(argc, argv);
        }

        private static Register kDoSoundSuspend(EngineState s, int argc, StackPtr argv)
        {
            Warning("kDoSound(suspend): STUB");
            return s.r_acc;
        }

        private static Register kDoSoundSetVolume(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetVolume(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSetPriority(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetPriority(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSoundSetLoop(EngineState s, int argc, StackPtr argv)
        {
            SciEngine.Instance._soundCmd.kDoSoundSetLoop(argc, argv);
            return s.r_acc;
        }

        private static Register kDoSync(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            switch ((AudioSyncCommands)argv[0].ToUInt16())
            {
                case AudioSyncCommands.Start:
                    {
                        ResourceId id;

                        SciEngine.Instance._audio.StopSoundSync();

                        // Load sound sync resource and lock it
                        if (argc == 3)
                        {
                            id = new ResourceId(ResourceType.Sync, argv[2].ToUInt16());
                        }
                        else if (argc == 7)
                        {
                            id = new ResourceId(ResourceType.Sync36, argv[2].ToUInt16(), (byte)argv[3].ToUInt16(), (byte)argv[4].ToUInt16(),
                                            (byte)argv[5].ToUInt16(), (byte)argv[6].ToUInt16());
                        }
                        else
                        {
                            Warning($"kDoSync: Start called with an unknown number of parameters ({argc})");
                            return s.r_acc;
                        }

                        SciEngine.Instance._audio.SetSoundSync(id, argv[1], segMan);
                        break;
                    }
                case AudioSyncCommands.Next:
                    SciEngine.Instance._audio.DoSoundSync(argv[1], segMan);
                    break;
                case AudioSyncCommands.Stop:
                    SciEngine.Instance._audio.StopSoundSync();
                    break;
                default:
                    throw new InvalidOperationException($"DoSync: Unhandled subfunction {argv[0].ToUInt16()}");
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
