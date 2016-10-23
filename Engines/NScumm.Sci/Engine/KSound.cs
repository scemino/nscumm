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
    internal enum AudioCommands
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

    internal partial class Kernel
    {
#if ENABLE_SCI32
        private static Register kDoSoundPhantasmagoriaMac(EngineState s, int argc, StackPtr argv) {
            // Phantasmagoria Mac (and seemingly no other game (!)) uses this
            // cutdown version of kDoSound.

            switch (argv[0].ToUInt16()) {
                case 0:
                    return SciEngine.Instance._soundCmd.kDoSoundMasterVolume(argc - 1, argv + 1, ref s.r_acc);
                case 2:
                    SciEngine.Instance._soundCmd.kDoSoundInit(argc - 1, argv + 1);
                    return s.r_acc;
                case 3:
                    SciEngine.Instance._soundCmd.kDoSoundDispose(argc - 1, argv + 1);
                    return s.r_acc;
                case 4:
                    SciEngine.Instance._soundCmd.kDoSoundPlay(argc - 1, argv + 1);
                    return s.r_acc;
                case 5:
                    SciEngine.Instance._soundCmd.kDoSoundStop(argc - 1, argv + 1);
                    return s.r_acc;
                case 8:
                    SciEngine.Instance._soundCmd.kDoSoundSetVolume(argc - 1, argv + 1);
                    return s.r_acc;
                case 9:
                    SciEngine.Instance._soundCmd.kDoSoundSetLoop(argc - 1, argv + 1);
                    return s.r_acc;
                case 10:
                    SciEngine.Instance._soundCmd.kDoSoundUpdateCues(argc - 1, argv + 1);
                    return s.r_acc;
            }

            Error("Unknown kDoSound Phantasmagoria Mac subop {0}", argv[0].ToUInt16());
            return s.r_acc;
        }
#endif

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

                        DebugC(DebugLevels.Sound, "kDoAudio: play sample {0}, module {1}", number, module);

                        // return sample length in ticks
                        if (argv[0].ToUInt16() == (ushort)AudioCommands.WPlay)
                            return Register.Make(0, (ushort)SciEngine.Instance._audio.WPlayAudio(module, number));
                        else
                            return Register.Make(0, (ushort)SciEngine.Instance._audio.StartAudio(module, number));
                    }
                case AudioCommands.Stop:
                    DebugC(DebugLevels.Sound, "kDoAudio: stop");
                    SciEngine.Instance._audio.StopAudio();
                    break;
                case AudioCommands.Pause:
                    DebugC(DebugLevels.Sound, "kDoAudio: pause");
                    SciEngine.Instance._audio.PauseAudio();
                    break;
                case AudioCommands.Resume:
                    DebugC(DebugLevels.Sound, "kDoAudio: resume");
                    SciEngine.Instance._audio.ResumeAudio();
                    break;
                case AudioCommands.Position:
                    DebugC(DebugLevels.Sound, "kDoAudio: get position");   // too verbose
                    return Register.Make(0, (ushort)SciEngine.Instance._audio.GetAudioPosition());
                case AudioCommands.Rate:
                    DebugC(DebugLevels.Sound, "kDoAudio: set audio rate to {0}", argv[1].ToUInt16());
                    SciEngine.Instance._audio.SetAudioRate(argv[1].ToUInt16());
                    break;
                case AudioCommands.Volume:
                    {
                        short volume = argv[1].ToInt16();
                        volume = (short)ScummHelper.Clip(volume, 0, AudioPlayer.AudioVolumeMax);
                        DebugC(DebugLevels.Sound, "kDoAudio: set volume to {0}", volume);
                        mixer.SetVolumeForSoundType(SoundType.Speech, volume * 2);
                        break;
                    }
                case AudioCommands.Language:
                    // In SCI1.1: tests for digital audio support
                    if (ResourceManager.GetSciVersion() == SciVersion.V1_1)
                    {
                        DebugC(DebugLevels.Sound, "kDoAudio: audio capability test");
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

                    DebugC(DebugLevels.Sound, "kDoAudio: set language to {0}", language);

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
                            DebugC(DebugLevels.Sound, "kDoAudio: CD audio subop");
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

#if ENABLE_SCI32
        private static Register kDoAudio32(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort) ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kDoAudioInit(EngineState s, int argc, StackPtr argv) {
            return Register.Make(0, 0);
        }

        private static Register kDoAudioWaitForPlay(EngineState s, int argc, StackPtr argv) {
            return SciEngine.Instance._audio32.KernelPlay(false, argc, argv);
        }

        private static Register kDoAudioPlay(EngineState s, int argc, StackPtr argv) {
            return SciEngine.Instance._audio32.KernelPlay(true, argc, argv);
        }

        private static Register kDoAudioStop(EngineState s, int argc, StackPtr argv) {
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 0, argc > 1 ? argv[1] : Register.NULL_REG);
            return Register.Make(0, (ushort) SciEngine.Instance._audio32.Stop((AudioChannelIndex) channelIndex));
        }

        private static Register kDoAudioPause(EngineState s, int argc, StackPtr argv) {
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 0, argc > 1 ? argv[1] : Register.NULL_REG);
            return Register.Make(0, SciEngine.Instance._audio32.Pause((AudioChannelIndex) channelIndex));
        }

        private static Register kDoAudioResume(EngineState s, int argc, StackPtr argv) {
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 0, argc > 1 ? argv[1] : Register.NULL_REG);
            return Register.Make(0, (ushort) (SciEngine.Instance._audio32.Resume(channelIndex)?1:0));
        }

        private static Register kDoAudioPosition(EngineState s, int argc, StackPtr argv) {
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 0, argc > 1 ? argv[1] : Register.NULL_REG);
            return Register.Make(0, (ushort)SciEngine.Instance._audio32.GetPosition(channelIndex));
        }

        private static Register kDoAudioRate(EngineState s, int argc, StackPtr argv) {
            // NOTE: In the original engine this would set the hardware
            // DSP sampling rate; ScummVM mixer does not need this, so
            // we only store the value to satisfy engine compatibility.

            if (argc > 0) {
                ushort sampleRate = argv[0].ToUInt16();
                if (sampleRate != 0) {
                    SciEngine.Instance._audio32.SetSampleRate(sampleRate);
                }
            }

            return Register.Make(0, SciEngine.Instance._audio32.SampleRate);
        }

        private static Register kDoAudioVolume(EngineState s, int argc, StackPtr argv) {
            short volume = (short) (argc > 0 ? argv[0].ToInt16() : -1);
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 1, argc > 2 ? argv[2] : Register.NULL_REG);

            if (volume != -1) {
                SciEngine.Instance._audio32.SetVolume((AudioChannelIndex) channelIndex, volume);
            }

            return Register.Make(0, (ushort) SciEngine.Instance._audio32.GetVolume(channelIndex));
        }

        private static Register kDoAudioGetCapability(EngineState s, int argc, StackPtr argv) {
            return Register.Make(0, 1);
        }

        private static Register kDoAudioBitDepth(EngineState s, int argc, StackPtr argv) {
            // NOTE: In the original engine this would set the hardware
            // DSP bit depth; ScummVM mixer does not need this, so
            // we only store the value to satisfy engine compatibility.

            if (argc > 0) {
                ushort bitDepth = argv[0].ToUInt16();
                if (bitDepth != 0) {
                    SciEngine.Instance._audio32.SetBitDepth((byte) bitDepth);
                }
            }

            return Register.Make(0, SciEngine.Instance._audio32.GetBitDepth());
        }

        private static Register kDoAudioDistort(EngineState s, int argc, StackPtr argv)
        {
            return kDummy(s, argc, argv);
        }

        private static Register kDoAudioFade36(EngineState s, int argc, StackPtr argv)
        {
            return kDummy(s, argc, argv);
        }

        private static Register kDoAudioPan(EngineState s, int argc, StackPtr argv)
        {
            return kDummy(s, argc, argv);
        }

        private static Register kDoAudioPanOff(EngineState s, int argc, StackPtr argv)
        {
            return kDummy(s, argc, argv);
        }

        private static Register kDoAudioCritical(EngineState s, int argc, StackPtr argv)
        {
            return kEmpty(s, argc, argv);
        }

        private static Register kDoAudioMixing(EngineState s, int argc, StackPtr argv) {
            if (argc > 0) {
                SciEngine.Instance._audio32.SetAttenuatedMixing(argv[0].ToUInt16()!=0);
            }

            return Register.Make(0, SciEngine.Instance._audio32.GetAttenuatedMixing());
        }

        private static Register kDoAudioChannels(EngineState s, int argc, StackPtr argv) {
            // NOTE: In the original engine this would set the hardware
            // DSP stereo output; ScummVM mixer does not need this, so
            // we only store the value to satisfy engine compatibility.

            if (argc > 0) {
                short numChannels = argv[0].ToInt16();
                if (numChannels != 0) {
                    SciEngine.Instance._audio32.SetNumOutputChannels(numChannels);
                }
            }

            return Register.Make(0, SciEngine.Instance._audio32.GetNumOutputChannels());
        }

        private static Register kDoAudioPreload(EngineState s, int argc, StackPtr argv) {
            // NOTE: In the original engine this would cause audio
            // data for new channels to be preloaded to memory when
            // the channel was initialized; we do not need this, so
            // we only store the value to satisfy engine compatibility.

            if (argc > 0) {
                SciEngine.Instance._audio32.SetPreload((byte) argv[0].ToUInt16());
            }

            return Register.Make(0, SciEngine.Instance._audio32.GetPreload());
        }

        private static Register kDoAudioFade(EngineState s, int argc, StackPtr argv) {
            if (argc < 4) {
                return Register.Make(0, 0);
            }

            // NOTE: Sierra did a nightmarish hack here, temporarily replacing
            // the argc of the kernel arguments with 2 and then restoring it
            // after findChannelByArgs was called.
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(2, argv, 0, argc > 5 ? argv[5] : Register.NULL_REG);

            short volume = argv[1].ToInt16();
            short speed = argv[2].ToInt16();
            short steps = argv[3].ToInt16();
            bool stopAfterFade = argc > 4 && argv[4].ToUInt16()!=0;

            return Register.Make(0, SciEngine.Instance._audio32.FadeChannel(channelIndex, volume, speed, steps, stopAfterFade));
        }

        private static Register kDoAudioHasSignal(EngineState s, int argc, StackPtr argv) {
            return Register.Make(0, SciEngine.Instance._audio32.HasSignal());
        }

        private static Register kDoAudioSetLoop(EngineState s, int argc, StackPtr argv) {
            short channelIndex = SciEngine.Instance._audio32.FindChannelByArgs(argc, argv, 0, argc == 3 ? argv[2] : Register.NULL_REG);

            bool loop = argv[0].ToInt16() != 0 && argv[0].ToInt16() != 1;

            SciEngine.Instance._audio32.SetLoop(channelIndex, loop);
            return s.r_acc;
        }

        private static Register kSetLanguage(EngineState s, int argc, StackPtr argv) {
            // This is used by script 90 of MUMG Deluxe from the main menu to toggle
            // the audio language between English and Spanish.
            // Basically, it instructs the interpreter to switch the audio resources
            // (resource.aud and associated map files) and load them from the "Spanish"
            // subdirectory instead.
            String audioDirectory = s._segMan.GetString(argv[0]);
            //warning("SetLanguage: set audio resource directory to '%s'", audioDirectory.c_str());
            SciEngine.Instance.ResMan.ChangeAudioDirectory(audioDirectory);

            return s.r_acc;
        }

#endif

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
            return SciEngine.Instance._soundCmd.kDoSoundMasterVolume(argc, argv, ref s.r_acc);
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

                        SciEngine.Instance._sync.Stop();

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

                        SciEngine.Instance._sync.Start(id, argv[1]);
                        break;
                    }
                case AudioSyncCommands.Next:
                    SciEngine.Instance._sync.Next(argv[1]);
                    break;
                case AudioSyncCommands.Stop:
                    SciEngine.Instance._sync.Stop();
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
