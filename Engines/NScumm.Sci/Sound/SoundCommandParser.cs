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
using NScumm.Sci.Engine;
using NScumm.Core.Audio;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    enum SoundStatus
    {
        Stopped = 0,
        Initialized = 1,
        Paused = 2,
        Playing = 3
    }

    class SoundCommandParser
    {
        public const int MUSIC_VOLUME_MAX = 127;
        public const int MUSIC_MASTERVOLUME_MAX = 15;

        private readonly SciVersion _soundVersion;
        private readonly SegManager _segMan;
        private readonly AudioPlayer _audio;
        private readonly ResourceManager _resMan;
        private readonly bool _useDigitalSfx;
        private readonly SciMusic _music;

        public MusicType MusicType
        {
            get
            {
                System.Diagnostics.Debug.Assert(_music != null);
                return _music.SoundMusicType;
            }
        }

        public SoundCommandParser(ResourceManager resMan, SegManager segMan, AudioPlayer audio, SciVersion soundVersion)
        {
            _resMan = resMan;
            _segMan = segMan;
            _audio = audio;
            _soundVersion = soundVersion;

            // Check if the user wants synthesized or digital sound effects in SCI1.1
            // games based on the prefer_digitalsfx config setting

            // In SCI2 and later games, this check should always be true - there was
            // always only one version of each sound effect or digital music track
            // (e.g. the menu music in GK1 - there is a sound effect with the same
            // resource number, but it's totally unrelated to the menu music).
            // The GK1 demo (very late SCI1.1) does the same thing
            // TODO: Check the QFG4 demo
            _useDigitalSfx = (ResourceManager.GetSciVersion() >= SciVersion.V2 || SciEngine.Instance.GameId == SciGameId.GK1 || ConfigManager.Instance.Get<bool>("prefer_digitalsfx"));

            _music = new SciMusic(_soundVersion, _useDigitalSfx);
            _music.Init();
        }

        public void kDoSoundStop(int argc, StackPtr argv)
        {
            DebugC(DebugLevels.Sound, "kDoSound(stop): {0}", argv[0]);
            ProcessStopSound(argv[0], false);
        }

        public Register kDoSoundPause(int argc, StackPtr argv, Register acc)
        {
            if (argc == 1)
                DebugC(DebugLevels.Sound, "kDoSound(pause): {0}", argv[0]);
            else
                DebugC(DebugLevels.Sound, "kDoSound(pause): {0}, {1}", argv[0], argv[1]);

            if (_soundVersion <= SciVersion.V0_LATE)
            {
                // SCI0 games give us 0/1 for either resuming or pausing the current music
                //  this one doesn't count, so pausing 2 times and resuming once means here that we are supposed to resume
                ushort value = argv[0].ToUInt16();
                MusicEntry musicSlot = _music.ActiveSci0MusicSlot;
                switch (value)
                {
                    case 1:
                        if ((musicSlot != null) && (musicSlot.status == SoundStatus.Playing))
                        {
                            _music.SoundPause(musicSlot);
                            SciEngine.WriteSelectorValue(_segMan, musicSlot.soundObj, o => o.state, (ushort)SoundStatus.Paused);
                        }
                        return Register.Make(0, 0);
                    case 0:
                        if ((musicSlot != null) && (musicSlot.status == SoundStatus.Paused))
                        {
                            _music.SoundResume(musicSlot);
                            SciEngine.WriteSelectorValue(_segMan, musicSlot.soundObj, o => o.state, (ushort)SoundStatus.Playing);
                            return Register.Make(0, 1);
                        }
                        return Register.Make(0, 0);
                    default:
                        throw new InvalidOperationException("kDoSound(pause): parameter 0 is invalid for sound-sci0");
                }
            }

            {
                Register obj = argv[0];
                ushort value = (ushort)(argc > 1 ? argv[1].ToUInt16() : 0);
                if (obj.Segment == 0)
                {       // pause the whole playlist
                    _music.PauseAll(value != 0);
                }
                else {  // pause a playlist slot
                    MusicEntry musicSlot = _music.GetSlot(obj);
                    if (musicSlot == null)
                    {
                        // This happens quite frequently
                        DebugC(DebugLevels.Sound, "kDoSound(pause): Slot not found ({0})", obj);
                        return acc;
                    }

                    _music.SoundToggle(musicSlot, value != 0);
                }
            }
            return acc;
        }

        public Register kDoSoundGetAudioCapability(int argc, StackPtr argv)
        {
            // Tests for digital audio support
            return Register.Make(0, 1);
        }

        public void kDoSoundSetHold(int argc, StackPtr argv)
        {
            Register obj = argv[0];

            DebugC(DebugLevels.Sound, "doSoundSetHold: {0}, {1}", argv[0], argv[1].ToUInt16());

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(setHold): Slot not found ({obj})");
                return;
            }

            // Set the special hold marker ID where the song should be looped at.
            musicSlot.hold = argv[1].ToInt16();
        }

        public void kDoSoundSetPriority(int argc, StackPtr argv)
        {
            Register obj = argv[0];
            short value = argv[1].ToInt16();

            DebugC(DebugLevels.Sound, "kDoSound(setPriority): {0}, {1}", obj, value);

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                DebugC(DebugLevels.Sound, "kDoSound(setPriority): Slot not found ({0})", obj);
                return;
            }

            if (value == -1)
            {
                musicSlot.overridePriority = false;
                musicSlot.priority = 0;

                // NB: It seems SSCI doesn't actually reset the priority here.

                SciEngine.WriteSelectorValue(_segMan, obj, o => o.flags, (ushort)(SciEngine.ReadSelectorValue(_segMan, obj, o => o.flags) & 0xFD));
            }
            else {
                // Scripted priority
                musicSlot.overridePriority = true;

                SciEngine.WriteSelectorValue(_segMan, obj, o => o.flags, (ushort)(SciEngine.ReadSelectorValue(_segMan, obj, o => o.flags) | 2));

                _music.SoundSetPriority(musicSlot, (byte)value);
            }
        }

        public void kDoSoundSetLoop(int argc, StackPtr argv)
        {
            Register obj = argv[0];
            short value = argv[1].ToInt16();

            DebugC(DebugLevels.Sound, "kDoSound(setLoop): {0}, {1}", obj, value);

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                // Apparently, it's perfectly normal for a game to call cmdSetSoundLoop
                // before actually initializing the sound and adding it to the playlist
                // with cmdInitSound. Usually, it doesn't matter if the game doesn't
                // request to loop the sound, so in this case, don't throw any warning,
                // otherwise do, because the sound won't be looped.
                if (value == -1)
                {
                    Warning($"kDoSound(setLoop): Slot not found ({obj}) and the song was requested to be looped");
                }
                else {
                    // Doesn't really matter
                }
                return;
            }
            if (value == -1)
            {
                musicSlot.loop = 0xFFFF;
            }
            else {
                musicSlot.loop = 1; // actually plays the music once
            }

            SciEngine.WriteSelectorValue(_segMan, obj, o => o.loop, musicSlot.loop);
        }

        public void SyncPlayList(Serializer s)
        {
            _music.SaveLoadWithSerializer(s);
        }

        public void kDoSoundSetVolume(int argc, StackPtr argv)
        {
            Register obj = argv[0];
            short value = argv[1].ToInt16();

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                // Do not throw a warning if the sound can't be found, as in some games
                // this is called before the actual sound is loaded (e.g. SQ4CD, with
                // the drum sounds of the energizer bunny at the beginning), so this is
                // normal behavior.
                Warning($"cmdSetSoundVolume: Slot not found ({obj})");
                return;
            }

            DebugC(DebugLevels.Sound, "kDoSound(setVolume): {0}", value);

            value = (short)ScummHelper.Clip(value, 0, MUSIC_VOLUME_MAX);

            if (musicSlot.volume != value)
            {
                musicSlot.volume = value;
                _music.SoundSetVolume(musicSlot, (byte)value);
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.vol, (ushort)value);
            }
        }

        public Register kDoSoundGlobalReverb(int argc, StackPtr argv)
        {
            byte prevReverb = _music.CurrentReverb;
            byte reverb = (byte)(argv[0].ToUInt16() & 0xF);

            if (argc == 1)
            {
                DebugC(DebugLevels.Sound, "doSoundGlobalReverb: {0}", argv[0].ToUInt16() & 0xF);
                if (reverb <= 10)
                    _music.GlobalReverb = (sbyte)reverb;
            }

            return Register.Make(0, prevReverb);
        }

        public void kDoSoundSendMidi(int argc, StackPtr argv)
        {
            // The 4 parameter variant of this call is used in at least LSL1VGA, room
            // 110 (Lefty's bar), to distort the music when Larry is drunk and stands
            // up - bug #3614447.
            Register obj = argv[0];
            byte channel = (byte)(argv[1].ToUInt16() & 0xf);
            byte midiCmd = (byte)((argc == 5) ? argv[2].ToUInt16() & 0xff : 0xB0);  // 0xB0: controller
            ushort controller = (argc == 5) ? argv[3].ToUInt16() : argv[2].ToUInt16();
            ushort param = (argc == 5) ? argv[4].ToUInt16() : argv[3].ToUInt16();

            if (argc == 4 && controller == 0xFF)
            {
                midiCmd = 0xE0; // 0xE0: pitch wheel
                ushort pitch = (ushort)ScummHelper.Clip(argv[3].ToInt16() + 0x2000, 0x0000, 0x3FFF);
                controller = (ushort)(pitch & 0x7F);
                param = (ushort)(pitch >> 7);
            }

            DebugC(DebugLevels.Sound, "kDoSound(sendMidi): {0}, {1}, {2}, {3}, {4}", obj, channel, midiCmd, controller, param);
            if (channel != 0)
                channel--; // channel is given 1-based, we are using 0-based

            uint midiCommand = (uint)(channel | midiCmd | (controller << 8) | (param << 16));

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                // TODO: maybe it's possible to call this with obj == 0:0 and send directly?!
                // if so, allow it
                //_music.sendMidiCommand(_midiCommand);
                Warning($"kDoSound(sendMidi): Slot not found ({obj})");
                return;
            }
            _music.SendMidiCommand(musicSlot, midiCommand);
        }

        public void kDoSoundUpdateCues(int argc, StackPtr argv)
        {
            ProcessUpdateCues(argv[0]);
        }

        public Register kDoSoundGetPolyphony(int argc, StackPtr argv)
        {
            return Register.Make(0, _music.SoundGetVoices());	// Get the number of voices
        }

        public void kDoSoundUpdate(int argc, StackPtr argv)
        {
            Register obj = argv[0];

            DebugC(DebugLevels.Sound, "kDoSound(update): {0}", argv[0]);

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(update): Slot not found ({obj})");
                return;
            }

            musicSlot.loop = (ushort)SciEngine.ReadSelectorValue(_segMan, obj, o => o.loop);
            short objVol = (short)ScummHelper.Clip((int)SciEngine.ReadSelectorValue(_segMan, obj, o => o.vol), 0, 255);
            if (objVol != musicSlot.volume)
                _music.SoundSetVolume(musicSlot, (byte)objVol);
            short objPrio = (short)SciEngine.ReadSelectorValue(_segMan, obj, o => o.priority);
            if (objPrio != musicSlot.priority)
                _music.SoundSetPriority(musicSlot, (byte)objPrio);
        }

        public Register kDoSoundMasterVolume(int argc, StackPtr argv, ref Register acc)
        {
            acc.Set(Register.Make(0, _music.SoundGetMasterVolume()));

            if (argc <= 0) return acc;

            DebugC(DebugLevels.Sound, "kDoSound(masterVolume): {0}", argv[0].ToInt16());
            int vol = (short)ScummHelper.Clip(argv[0].ToInt16(), 0, MUSIC_MASTERVOLUME_MAX);
            vol = vol * Mixer.MaxMixerVolume / MUSIC_MASTERVOLUME_MAX;
            ConfigManager.Instance.Set<int>("music_volume", vol);
            ConfigManager.Instance.Set<int>("sfx_volume", vol);
            SciEngine.Instance.SyncSoundSettings();
            return acc;
        }

        public Register kDoSoundMute(int argc, StackPtr argv)
        {
            ushort previousState = _music.SoundOn ? (ushort)1 : (ushort)0;
            if (argc > 0)
            {
                DebugC(DebugLevels.Sound, "kDoSound(mute): {0}", argv[0].ToUInt16());
                _music.SoundOn = argv[0].ToUInt16() != 0;
            }

            return Register.Make(0, previousState);
        }

        public void kDoSoundInit(int argc, StackPtr argv)
        {
            DebugC(DebugLevels.Sound, "kDoSound(init): {0}", argv[0]);
            ProcessInitSound(argv[0]);
        }

        public void kDoSoundFade(int argc, StackPtr argv)
        {
            Register obj = argv[0];

            // The object can be null in several SCI0 games (e.g. Camelot, KQ1, KQ4, MUMG).
            // Check bugs #3035149, #3036942 and #3578335.
            // In this case, we just ignore the call.
            if (obj.IsNull && argc == 1)
                return;

            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                DebugC(DebugLevels.Sound, "kDoSound(fade): Slot not found ({0})", obj);
                return;
            }

            int volume = musicSlot.volume;

            // If sound is not playing currently, set signal directly
            if (musicSlot.status != SoundStatus.Playing)
            {
                DebugC(DebugLevels.Sound, "kDoSound(fade): {0} fading requested, but sound is currently not playing", obj);
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.signal, Register.SIGNAL_OFFSET);
                return;
            }

            switch (argc)
            {
                case 1: // SCI0
                        // SCI0 fades out all the time and when fadeout is done it will also
                        // stop the music from playing
                    musicSlot.fadeTo = 0;
                    musicSlot.fadeStep = -5;
                    musicSlot.fadeTickerStep = 10 * 16667 / _music.SoundGetTempo;
                    musicSlot.fadeTicker = 0;
                    break;

                case 4: // SCI01+
                case 5: // SCI1+ (SCI1 late sound scheme), with fade and continue
                    musicSlot.fadeTo = (byte)ScummHelper.Clip(argv[1].ToUInt16(), 0, MUSIC_VOLUME_MAX);
                    // Check if the song is already at the requested volume. If it is, don't
                    // perform any fading. Happens for example during the intro of Longbow.
                    if (musicSlot.fadeTo == musicSlot.volume)
                        return;

                    // Sometimes we get objects in that position, so fix the value (refer to workarounds.cpp)
                    if (argv[1].Segment == 0)
                        musicSlot.fadeStep = (short)(volume > musicSlot.fadeTo ? -argv[3].ToUInt16() : argv[3].ToUInt16());
                    else
                        musicSlot.fadeStep = (short)(volume > musicSlot.fadeTo ? -5 : 5);
                    musicSlot.fadeTickerStep = (uint)(argv[2].ToUInt16() * 16667 / _music.SoundGetTempo);
                    musicSlot.fadeTicker = 0;

                    // argv[4] is a boolean. Scripts sometimes pass strange values,
                    // but SSCI only checks for zero/non-zero. (Verified in KQ6.)
                    // KQ6 room 460 even passes an object, but treating this as 'true'
                    // seems fine in that case.
                    if (argc == 5)
                        musicSlot.stopAfterFading = !argv[4].IsNull;
                    else
                        musicSlot.stopAfterFading = false;
                    break;

                default:
                    throw new InvalidOperationException($"kDoSound(fade): unsupported argc {argc}");
            }

            DebugC(DebugLevels.Sound, "kDoSound(fade): {0} to {1}, step {2}, ticker {3}", obj, musicSlot.fadeTo, musicSlot.fadeStep, musicSlot.fadeTickerStep);
            return;
        }

        public void kDoSoundStopAll(int argc, StackPtr argv)
        {
            // TODO: this can't be right, this gets called in kq1 - e.g. being in witch house, getting the note
            //  now the point jingle plays and after a messagebox they call this - and would stop the background effects with it
            //  this doesn't make sense, so i disable it for now
        }

        public void kDoSoundPlay(int argc, StackPtr argv)
        {
            DebugC(DebugLevels.Sound, "kDoSound(play): {0}", argv[0]);

            bool playBed = false;
            if (argc >= 2 && !argv[1].IsNull)
                playBed = true;
            ProcessPlaySound(argv[0], playBed);
        }

        public void kDoSoundRestore(int argc, StackPtr argv)
        {
            // Called after loading, to restore the playlist
            // We don't really use or need this
        }

        public void kDoSoundDispose(int argc, StackPtr argv)
        {
            DebugC(DebugLevels.Sound, "kDoSound(dispose): {0}", argv[0]);
            ProcessDisposeSound(argv[0]);
        }

        private void ProcessPlaySound(Register obj, bool playBed)
        {
            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(play): Slot not found ({obj}), initializing it manually");
                // The sound hasn't been initialized for some reason, so initialize it
                // here. Happens in KQ6, room 460, when giving the creature (child) to
                // the bookworm. Fixes bugs #3413301 and #3421098.
                ProcessInitSound(obj);
                musicSlot = _music.GetSlot(obj);
                if (musicSlot == null)
                    throw new InvalidOperationException("Failed to initialize uninitialized sound slot");
            }

            int resourceId = GetSoundResourceId(obj);

            if (musicSlot.resourceId != resourceId)
            { // another sound loaded into struct
                ProcessDisposeSound(obj);
                ProcessInitSound(obj);
                // Find slot again :)
                musicSlot = _music.GetSlot(obj);
            }

            SciEngine.WriteSelector(_segMan, obj, s => s.handle, obj);

            if (_soundVersion >= SciVersion.V1_EARLY)
            {
                SciEngine.WriteSelector(_segMan, obj, s => s.nodePtr, obj);
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.min, 0);
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.sec, 0);
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.frame, 0);
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.signal, 0);
            }
            else {
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.state, (ushort)SoundStatus.Playing);
            }

            musicSlot.loop = (ushort)SciEngine.ReadSelectorValue(_segMan, obj, s => s.loop);

            // Get song priority from either obj or soundRes
            byte resourcePriority = 0xFF;
            if (musicSlot.soundRes != null)
                resourcePriority = musicSlot.soundRes.SoundPriority;
            if (!musicSlot.overridePriority && resourcePriority != 0xFF)
            {
                musicSlot.priority = resourcePriority;
            }
            else {
                musicSlot.priority = (short)SciEngine.ReadSelectorValue(_segMan, obj, s => s.priority);
            }

            // Reset hold when starting a new song. kDoSoundSetHold is always called after
            // kDoSoundPlay to set it properly, if needed. Fixes bug #3413589.
            musicSlot.hold = -1;
            musicSlot.playBed = playBed;
            if (_soundVersion >= SciVersion.V1_EARLY)
                musicSlot.volume = (short)SciEngine.ReadSelectorValue(_segMan, obj, s => s.vol);

            DebugC(DebugLevels.Sound, "kDoSound(play): {0} number {1}, loop {2}, prio {3}, vol {4}, bed {5}", obj,
                resourceId, musicSlot.loop, musicSlot.priority, musicSlot.volume, playBed ? 1 : 0);

            _music.SoundPlay(musicSlot);

            // Reset any left-over signals
            musicSlot.signal = 0;
            musicSlot.fadeStep = 0;
        }

        private int GetSoundResourceId(Register obj)
        {
            int resourceId = obj.Segment != 0 ? (int)SciEngine.ReadSelectorValue(_segMan, obj, s => s.number) : -1;
            // Modify the resourceId for the Windows versions that have an alternate MIDI soundtrack, like SSCI did.
            if (SciEngine.Instance != null && SciEngine.Instance.Features.UseAltWinGMSound)
            {
                // Check if the alternate MIDI song actually exists...
                // There are cases where it just doesn't exist (e.g. SQ4, room 530 -
                // bug #3392767). In these cases, use the DOS tracks instead.
                if (resourceId != 0 && _resMan.TestResource(new ResourceId(ResourceType.Sound, (ushort)(resourceId + 1000))) != null)
                    resourceId += 1000;
            }

            return resourceId;
        }

        private void ProcessInitSound(Register obj)
        {
            int resourceId = GetSoundResourceId(obj);

            // Check if a track with the same sound object is already playing
            MusicEntry oldSound = _music.GetSlot(obj);
            if (oldSound != null)
                ProcessDisposeSound(obj);

            MusicEntry newSound = new MusicEntry();
            newSound.resourceId = (ushort)resourceId;
            newSound.soundObj = obj;
            newSound.loop = (ushort)SciEngine.ReadSelectorValue(_segMan, obj, s => s.loop);
            if (_soundVersion <= SciVersion.V0_LATE)
                newSound.priority = (short)SciEngine.ReadSelectorValue(_segMan, obj, s => s.priority);
            else
                newSound.priority = (short)(SciEngine.ReadSelectorValue(_segMan, obj, s => s.priority) & 0xFF);
            if (_soundVersion >= SciVersion.V1_EARLY)
                newSound.volume = (short)ScummHelper.Clip((int)SciEngine.ReadSelectorValue(_segMan, obj, s => s.vol), 0, MUSIC_VOLUME_MAX);
            newSound.reverb = -1;  // initialize to SCI invalid, it'll be set correctly in soundInitSnd() below

            DebugC(DebugLevels.Sound, "kDoSound(init): {0} number {1}, loop {2}, prio {3}, vol {4}", obj,
                resourceId, newSound.loop, newSound.priority, newSound.volume);

            InitSoundResource(newSound);

            _music.PushBackSlot(newSound);

            if (newSound.soundRes != null || newSound.pStreamAud != null)
            {
                // Notify the engine
                if (_soundVersion <= SciVersion.V0_LATE)
                    SciEngine.WriteSelectorValue(_segMan, obj, s => s.state, (ushort)SoundStatus.Initialized);
                else
                    SciEngine.WriteSelector(_segMan, obj, s => s.nodePtr, obj);
            }
        }

        public void PauseAll(bool pause)
        {
            _music.PauseAll(pause);
        }

        private void InitSoundResource(MusicEntry newSound)
        {
            if (newSound.resourceId != 0 && _resMan.TestResource(new ResourceId(ResourceType.Sound, newSound.resourceId)) != null)
                newSound.soundRes = new SoundResource(newSound.resourceId, _resMan, _soundVersion);
            else
                newSound.soundRes = null;

            // In SCI1.1 games, sound effects are started from here. If we can find
            // a relevant audio resource, play it, otherwise switch to synthesized
            // effects. If the resource exists, play it using map 65535 (sound
            // effects map)
            bool checkAudioResource = ResourceManager.GetSciVersion() >= SciVersion.V1_1;
            // Hoyle 4 has garbled audio resources in place of the sound resources.
            if (SciEngine.Instance.GameId == SciGameId.HOYLE4)
                checkAudioResource = false;

            if (checkAudioResource && _resMan.TestResource(new ResourceId(ResourceType.Audio, newSound.resourceId)) != null)
            {
                // Found a relevant audio resource, create an audio stream if there is
                // no associated sound resource, or if both resources exist and the
                // user wants the digital version.
                if (_useDigitalSfx || newSound.soundRes == null)
                {
                    int sampleLen;
                    newSound.pStreamAud = _audio.GetAudioStream(newSound.resourceId, 65535, out sampleLen);
                    newSound.soundType = SoundType.SFX;
                }
            }

            if (newSound.pStreamAud == null && newSound.soundRes != null)
                _music.SoundInitSnd(newSound);
        }

        private void ProcessDisposeSound(Register obj)
        {
            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(dispose): Slot not found ({obj})");
                return;
            }

            ProcessStopSound(obj, false);

            _music.SoundKill(musicSlot);
            SciEngine.WriteSelectorValue(_segMan, obj, s => s.handle, 0);
            if (_soundVersion >= SciVersion.V1_EARLY)
                SciEngine.WriteSelector(_segMan, obj, s => s.nodePtr, Register.NULL_REG);
            else
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.state, (ushort)SoundStatus.Stopped);
        }

        private void ProcessStopSound(Register obj, bool sampleFinishedPlaying)
        {
            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(stop): Slot not found ({obj})");
                return;
            }

            if (_soundVersion <= SciVersion.V0_LATE)
            {
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.state, (ushort)SoundStatus.Stopped);
            }
            else {
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.handle, 0);
            }

            // Set signal selector in sound SCI0 games only, when the sample has
            // finished playing. If we don't set it at all, we get a problem when using
            // vaporizer on the 2 guys. If we set it all the time, we get no music in
            // sq3new and kq1.
            // FIXME: This *may* be wrong, it's impossible to find out in Sierra DOS
            //        SCI, because SCI0 under DOS didn't have sfx drivers included.
            // We need to set signal in sound SCI1+ games all the time.
            if ((_soundVersion > SciVersion.V0_LATE) || sampleFinishedPlaying)
                SciEngine.WriteSelectorValue(_segMan, obj, s => s.signal, Register.SIGNAL_OFFSET);

            musicSlot.dataInc = 0;
            musicSlot.signal = Register.SIGNAL_OFFSET;
            _music.SoundStop(musicSlot);
        }

        public void UpdateSci0Cues()
        {
            bool noOnePlaying = true;
            MusicEntry pWaitingForPlay = null;

            foreach (var i in _music.PlayList)
            {
                // Is the sound stopped, and the sound object updated too? If yes, skip
                // this sound, as SCI0 only allows one active song.
                if (i.isQueued)
                {
                    pWaitingForPlay = i;
                    // FIXME(?): In iceman 2 songs are queued when playing the door
                    // sound - if we use the first song for resuming then it's the wrong
                    // one. Both songs have same priority. Maybe the new sound function
                    // in sci0 is somehow responsible.
                    continue;
                }
                if (i.signal == 0 && i.status != SoundStatus.Playing)
                    continue;

                ProcessUpdateCues(i.soundObj);
                noOnePlaying = false;
            }

            if (noOnePlaying && pWaitingForPlay != null)
            {
                // If there is a queued entry, play it now - check SciMusic::soundPlay()
                pWaitingForPlay.isQueued = false;
                _music.SoundPlay(pWaitingForPlay);
            }
        }

        private void ProcessUpdateCues(Register obj)
        {
            MusicEntry musicSlot = _music.GetSlot(obj);
            if (musicSlot == null)
            {
                Warning($"kDoSound(updateCues): Slot not found ({obj})");
                return;
            }

            if (musicSlot.pStreamAud != null)
            {
                // Update digital sound effect slots
                int currentLoopCounter = 0;

                if (musicSlot.pLoopStream != null)
                    currentLoopCounter = musicSlot.pLoopStream.CompleteIterations;

                if (currentLoopCounter != musicSlot.sampleLoopCounter)
                {
                    // during last time we looped at least one time, update loop accordingly
                    musicSlot.loop -= (ushort)(currentLoopCounter - musicSlot.sampleLoopCounter);
                    musicSlot.sampleLoopCounter = currentLoopCounter;
                }
                if (musicSlot.status == SoundStatus.Playing)
                {
                    if (!_music.SoundIsActive(musicSlot))
                    {
                        ProcessStopSound(obj, true);
                    }
                    else {
                        _music.UpdateAudioStreamTicker(musicSlot);
                    }
                }
                else if (musicSlot.status == SoundStatus.Paused)
                {
                    _music.UpdateAudioStreamTicker(musicSlot);
                }
                // We get a flag from MusicEntry::doFade() here to set volume for the stream
                if (musicSlot.fadeSetVolume)
                {
                    _music.SoundSetSampleVolume(musicSlot, musicSlot.volume);
                    musicSlot.fadeSetVolume = false;
                }
            }
            else if (musicSlot.pMidiParser != null)
            {
                // Update MIDI slots
                if (musicSlot.signal == 0)
                {
                    if (musicSlot.dataInc != SciEngine.ReadSelectorValue(_segMan, obj, o => o.dataInc))
                    {
                        if (SciEngine.Selector(o => o.dataInc) > -1)
                            SciEngine.WriteSelectorValue(_segMan, obj, o => o.dataInc, (ushort)musicSlot.dataInc);
                        SciEngine.WriteSelectorValue(_segMan, obj, o => o.signal, (ushort)(musicSlot.dataInc + 127));
                    }
                }
                else {
                    // Sync the signal of the sound object
                    SciEngine.WriteSelectorValue(_segMan, obj, o => o.signal, musicSlot.signal);
                    // We need to do this especially because state selector needs to get updated
                    if (musicSlot.signal == Register.SIGNAL_OFFSET)
                        ProcessStopSound(obj, false);
                }
            }
            else {
                // The sound slot has no data for the currently selected sound card.
                // An example can be found during the mud wrestling scene in LSL5, room
                // 730: sound 744 (a splat sound heard when Lana Luscious jumps in the
                // mud) only contains MIDI channel data. If a non-MIDI sound card is
                // selected (like Adlib), then the scene freezes. We also need to stop
                // the sound at this point, otherwise KQ6 Mac breaks because the rest
                // of the object needs to be reset to avoid a continuous stream of
                // sound cues.
                ProcessStopSound(obj, true);    // this also sets the signal selector
            }

            if (musicSlot.fadeCompleted)
            {
                musicSlot.fadeCompleted = false;
                // We need signal for sci0 at least in iceman as well (room 14,
                // fireworks).
                // It is also needed in other games, e.g. LSL6 when talking to the
                // receptionist (bug #3192166).
                // TODO: More thorougly check the different SCI version:
                // * SCI1late sets signal to 0xFE here. (With signal 0xFF
                //       duplicate music plays in LauraBow2CD - bug #6462)
                //   SCI1middle LSL1 1.000.510 does not have the 0xFE;
                //   SCI1late CastleDrBrain demo 1.000.005 does have the 0xFE.
                // * Other SCI1 games seem to rely on processStopSound to set the signal
                // * Need to check SCI0 behaviour.
                ushort sig;
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_LATE)
                    sig = 0xFFFE;
                else
                    sig = Register.SIGNAL_OFFSET;
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.signal, sig);
                if (_soundVersion <= SciVersion.V0_LATE)
                {
                    ProcessStopSound(obj, false);
                }
                else {
                    if (musicSlot.stopAfterFading)
                        ProcessStopSound(obj, false);
                }
            }

            // Sync loop selector for SCI0
            if (_soundVersion <= SciVersion.V0_LATE)
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.loop, musicSlot.loop);

            musicSlot.signal = 0;

            if (_soundVersion >= SciVersion.V1_EARLY)
            {
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.min, (ushort)(musicSlot.ticker / 3600));
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.sec, (ushort)(musicSlot.ticker % 3600 / 60));
                SciEngine.WriteSelectorValue(_segMan, obj, o => o.frame, (ushort)(musicSlot.ticker % 60 / 2));
            }
        }

        public void ClearPlayList()
        {
            _music.ClearPlayList();
        }

        public void SetMasterVolume(int vol)
        {
            _music.SoundSetMasterVolume((ushort)vol);
        }
    }
}
