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

using NScumm.Core.Audio;
using System;
using System.Linq;
using NScumm.Sci.Engine;
using System.Collections.Generic;
using NScumm.Sci.Sound.Drivers;
using NScumm.Core.Audio.Decoders;
using System.IO;
using System.Threading;
using NScumm.Core.Common;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Sound
{
    class ChannelRemapping
    {
        public DeviceChannelUsage[] _map = new DeviceChannelUsage[16];
        public int[] _prio = new int[16];
        public int[] _voices = new int[16];
        public bool[] _dontRemap = new bool[16];
        public int _freeVoices;

        public ChannelRemapping()
        {
            for (int i = 0; i < 16; ++i)
            {
                _map[i] = new DeviceChannelUsage();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < 16; ++i)
            {
                _map[i]._song = null;
                _map[i]._channel = -1;
                _prio[i] = 0;
                _voices[i] = 0;
                _dontRemap[i] = false;
            }
        }

        public void Swap(int i, int j)
        {
            DeviceChannelUsage t1;
            int t2;
            bool t3;

            t1 = _map[i]; _map[i] = _map[j]; _map[j] = t1;
            t2 = _prio[i]; _prio[i] = _prio[j]; _prio[j] = t2;
            t2 = _voices[i]; _voices[i] = _voices[j]; _voices[j] = t2;
            t3 = _dontRemap[i]; _dontRemap[i] = _dontRemap[j]; _dontRemap[j] = t3;
        }

        public void Evict(int i)
        {
            _freeVoices += _voices[i];

            _map[i]._song = null;
            _map[i]._channel = -1;
            _prio[i] = 0;
            _voices[i] = 0;
            _dontRemap[i] = false;
        }

        public int LowestPrio()
        {
            int max = 0;
            int channel = -1;
            for (int i = 0; i < 16; ++i)
            {
                if (_prio[i] > max)
                {
                    max = _prio[i];
                    channel = i;
                }
            }
            return channel;
        }
    }

    class MusicEntryChannel
    {
        // Channel info
        public sbyte _prio; // 0 = essential; lower is higher priority
        public sbyte _voices;
        public bool _dontRemap;
        public bool _dontMap;
        public bool _mute;
    }

    internal class MusicEntry
    {
        private const int MUSIC_VOLUME_DEFAULT = 127;

        // Do not get these directly for the sound objects!
        // It's a bad idea, as the sound code (i.e. the SciMusic
        // class) should be as separate as possible from the rest
        // of the engine

        public Register soundObj;
        public ushort loop;
        public short priority; // must be int16, at least in Laura Bow 1, main music (object conMusic) uses priority -1
        public ushort resourceId;
        public sbyte reverb;
        public short volume;
        public SoundResource soundRes;
        public IRewindableAudioStream pStreamAud;
        public LoopingAudioStream pLoopStream;
        public SoundType soundType;
        public int time; // "tim"estamp to indicate in which order songs have been added
        public SoundHandle hCurrentAud;
        public bool playBed;
        public bool overridePriority;
        public int pauseCounter;
        public MidiParser_SCI pMidiParser;
        public int[] _usedChannels = new int[16];
        public MusicEntryChannel[] _chan;
        public short hold;
        public ushort signal;
        public List<ushort> signalQueue;
        public SoundStatus status;
        public short fadeStep;
        public ushort ticker;

        public byte fadeTo;
        public uint fadeTicker;
        public uint fadeTickerStep;
        public bool fadeCompleted;
        public bool fadeSetVolume;
        public bool isQueued; // for SCI0 only!
        public int dataInc;
        public int sampleLoopCounter;
        public bool stopAfterFading;

        public MusicEntry()
        {
            soundObj = Register.NULL_REG;
            volume = MUSIC_VOLUME_DEFAULT;
            hold = -1;
            reverb = -1;
            status = SoundStatus.Stopped;
            soundType = SoundType.Music;

            signalQueue = new List<ushort>();
            _chan = new MusicEntryChannel[16];
            for (int i = 0; i < _chan.Length; i++)
            {
                _chan[i] = new MusicEntryChannel();
            }

            for (int i = 0; i < 16; ++i)
            {
                _usedChannels[i] = 0xFF;
                _chan[i]._prio = 127;
                _chan[i]._voices = 0;
                _chan[i]._dontRemap = false;
                _chan[i]._mute = false;
            }
        }

        public void OnTimer()
        {
            if (signal == 0)
            {
                if (signalQueue.Count != 0)
                {
                    // no signal set, but signal in queue, set that one
                    signal = signalQueue[0];
                    signalQueue.RemoveAt(0);
                }
            }

            if (status != SoundStatus.Playing)
                return;

            // Fade MIDI and digital sound effects
            if (fadeStep != 0)
                DoFade();

            // Only process MIDI streams in this thread, not digital sound effects
            if (pMidiParser != null)
            {
                pMidiParser.OnTimer();
                ticker = (ushort)pMidiParser.Tick;
            }
        }

        private void DoFade()
        {
            if (fadeTicker != 0)
                fadeTicker--;
            else
            {
                fadeTicker = fadeTickerStep;
                volume += fadeStep;
                if (((fadeStep > 0) && (volume >= fadeTo)) || ((fadeStep < 0) && (volume <= fadeTo)))
                {
                    volume = fadeTo;
                    fadeStep = 0;
                    fadeCompleted = true;
                }

                // Only process MIDI streams in this thread, not digital sound effects
                if (pMidiParser != null)
                {
                    pMidiParser.SetVolume((byte)volume);
                }

                fadeSetVolume = true; // set flag so that SoundCommandParser::cmdUpdateCues will set the volume of the stream
            }
        }

        public void SetSignal(int newSignal)
        {
            // For SCI0, we cache the signals to set, as some songs might
            // update their signal faster than kGetEvent is called (which is where
            // we manually invoke kDoSoundUpdateCues for SCI0 games). SCI01 and
            // newer handle signalling inside kDoSoundUpdateCues. Refer to bug #3042981
            if (SciEngine.Instance.Features.DetectDoSoundType() <= SciVersion.V0_LATE)
            {
                if (signal == 0)
                {
                    signal = (ushort)newSignal;
                }
                else
                {
                    // signal already set and waiting for getting to scripts, queue new one
                    signalQueue.Add((ushort)newSignal);
                }
            }
            else
            {
                // Set the signal directly for newer games, otherwise the sound
                // object might be deleted already later on (refer to bug #3045913)
                signal = (ushort)newSignal;
            }
        }
    }

    struct DeviceChannelUsage
    {
        public MusicEntry _song;
        public int _channel;
    }

    internal class SciMusic
    {
        private uint _dwTempo;
        private IMixer _mixer;
        private MusicType _musicType;
        private SciVersion _soundVersion;
        private bool _useDigitalSFX;
        private object _mutex = new object();
        private List<MusicEntry> _playList;
        private DeviceChannelUsage[] _channelMap;
        private MidiPlayer _pMidiDrv;
        private int _timeCounter; // Used to keep track of the order in which MusicEntries
        private bool _needsRemap;
        private int _driverFirstChannel;
        private int _driverLastChannel;
        private sbyte _globalReverb;
        private MusicEntry _currentlyPlayingSample;
        private List<int> _queuedCommands;
        private byte _masterVolume;
        private bool _soundOn;

        public bool SoundOn
        {
            get
            {
                return _soundOn;
            }
            set
            {
                lock (_mutex)
                {

                    _soundOn = value;
                    _pMidiDrv.PlaySwitch(value);
                }
            }
        }

        internal void SaveLoadWithSerializer(Serializer s)
        {
            throw new NotImplementedException();
        }

        public void SendMidiCommand(MusicEntry pSnd, uint cmd)
        {
            lock (_mutex)
            {
                if (pSnd.pMidiParser == null)
                    Error($"tried to cmdSendMidi on non midi slot ({pSnd.soundObj})");

                pSnd.pMidiParser.MainThreadBegin();
                pSnd.pMidiParser.SendFromScriptToDriver(cmd);
                pSnd.pMidiParser.MainThreadEnd();
            }
        }

        public IList<MusicEntry> PlayList { get { return _playList; } }

        public sbyte GlobalReverb
        {
            get { return _globalReverb; }
            set
            {
                lock (_mutex)
                {
                    if (value != 127)
                    {
                        // Set global reverb normally
                        _globalReverb = value;

                        // Check the reverb of the active song...
                        foreach (var item in _playList)
                        {
                            if (item.status == SoundStatus.Playing)
                            {
                                if (item.reverb == 127)            // Active song has no reverb
                                    _pMidiDrv.Reverb = value;   // Set the global reverb
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Set reverb of the active song
                        foreach (var item in _playList)
                        {
                            if (item.status == SoundStatus.Playing)
                            {
                                _pMidiDrv.Reverb = item.reverb; // Set the song's reverb
                                break;
                            }
                        }
                    }
                }
            }
        }

        public uint SoundGetTempo { get { return _dwTempo; } }

        public MusicEntry ActiveSci0MusicSlot
        {
            get
            {
                MusicEntry highestPrioritySlot = null;
                foreach (var playSlot in _playList)
                {
                    if (playSlot.pMidiParser != null)
                    {
                        if (playSlot.status == SoundStatus.Playing)
                            return playSlot;
                        if (playSlot.status == SoundStatus.Paused)
                        {
                            if ((highestPrioritySlot == null) || (highestPrioritySlot.priority < playSlot.priority))
                                highestPrioritySlot = playSlot;
                        }
                    }
                }
                return highestPrioritySlot;
            }
        }

        public byte CurrentReverb
        {
            get
            {
                lock (_mutex)
                {
                    return (byte)_pMidiDrv.Reverb;
                }
            }
        }

        public SciMusic(SciVersion soundVersion, bool useDigitalSFX)
        {
            _masterVolume = 15;
            _soundVersion = soundVersion;
            _useDigitalSFX = useDigitalSFX;
            _soundOn = true;
            _playList = new List<MusicEntry>();
            _channelMap = new DeviceChannelUsage[16];
            for (int i = 0; i < _channelMap.Length; i++)
            {
                _channelMap[i] = new DeviceChannelUsage();
            }
            _queuedCommands = new List<int>();
        }

        public void Init()
        {
            // system init
            _mixer = SciEngine.Instance.Mixer;
            // SCI sound init
            _dwTempo = 0;

            Core.IO.Platform platform = SciEngine.Instance.Platform;
            MusicDriverTypes deviceFlags = MusicDriverTypes.PCSpeaker | MusicDriverTypes.PCjr | MusicDriverTypes.AdLib | MusicDriverTypes.Midi;

            // Default to MIDI in SCI2.1+ games, as many don't have AdLib support.
            // Also, default to MIDI for Windows versions of SCI1.1 games, as their
            // soundtrack is written for GM.
            // TODO: if (ResourceManager.GetSciVersion() >= SciVersion.V2_1 || SciEngine.Instance.Features.UseAltWinGMSound)
            //deviceFlags |= MDT_PREFER_GM;

            // Currently our CMS implementation only supports SCI1(.1)
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY && ResourceManager.GetSciVersion() <= SciVersion.V1_1)
                deviceFlags |= MusicDriverTypes.CMS;

            if (SciEngine.Instance.Platform == Core.IO.Platform.FMTowns)
            {
                if (ResourceManager.GetSciVersion() > SciVersion.V1_EARLY)
                    deviceFlags = MusicDriverTypes.FMTowns;
                else
                    deviceFlags |= MusicDriverTypes.FMTowns;
            }

            var dev = MidiDriver.DetectDevice(deviceFlags, "auto");
            _musicType = MidiDriver.GetMusicType(dev);

            if (SciEngine.Instance.Features.UseAltWinGMSound && _musicType != MusicType.GeneralMidi)
            {
                Warning("A Windows CD version with an alternate MIDI soundtrack has been chosen, but no MIDI music device has been selected. Reverting to the DOS soundtrack");
                SciEngine.Instance.Features.ForceDOSTracks();
            }

            // TODO: 
            switch (_musicType)
            {
                case MusicType.AdLib:
                    // FIXME: There's no Amiga sound option, so we hook it up to AdLib
                    // TODO:
                    //if (SciEngine.Instance.Platform == Core.IO.Platform.Amiga || platform == Core.IO.Platform.Macintosh)
                    //    _pMidiDrv = MidiPlayer_AmigaMac_create(_soundVersion);
                    //else
                    _pMidiDrv = MidiPlayer_AdLib_create(_soundVersion);
                    break;
                    //    case MusicType.PCjr:
                    //        _pMidiDrv = MidiPlayer_PCJr_create(_soundVersion);
                    //        break;
                    //    case MusicType.PCSpeaker:
                    //        _pMidiDrv = MidiPlayer_PCSpeaker_create(_soundVersion);
                    //        break;
                    //    case MusicType.CMS:
                    //        _pMidiDrv = MidiPlayer_CMS_create(_soundVersion);
                    //        break;
                    //    case MusicType.FMTowns:
                    //        _pMidiDrv = MidiPlayer_FMTowns_create(_soundVersion);
                    //        break;
                    //    default:
                    //        if (ConfMan.getBool("native_fb01"))
                    //            _pMidiDrv = MidiPlayer_Fb01_create(_soundVersion);
                    //        else
                    //            _pMidiDrv = MidiPlayer_Midi_create(_soundVersion);
                    //        break;
            }

            if (_pMidiDrv != null && _pMidiDrv.Open() == 0)
            {
                _pMidiDrv.SetTimerCallback(this, MiditimerCallback);
                _dwTempo = _pMidiDrv.BaseTempo;
            }
            else
            {
                if (SciEngine.Instance.GameId == SciGameId.FUNSEEKER)
                {
                    // HACK: The Fun Seeker's Guide demo doesn't have patch 3 and the version
                    // of the Adlib driver (adl.drv) that it includes is unsupported. That demo
                    // doesn't have any sound anyway, so this shouldn't be fatal.
                }
                else
                {
                    throw new InvalidOperationException("Failed to initialize sound driver");
                }
            }

            // Find out what the first possible channel is (used, when doing channel
            // remapping).
            _driverFirstChannel = _pMidiDrv.FirstChannel;
            _driverLastChannel = _pMidiDrv.LastChannel;
            if (ResourceManager.GetSciVersion() <= SciVersion.V0_LATE)
                _globalReverb = _pMidiDrv.Reverb; // Init global reverb for SCI0

            _currentlyPlayingSample = null;
            _timeCounter = 0;
            _needsRemap = false;
        }

        public void SoundSetPriority(MusicEntry pSnd, byte prio)
        {
            lock (_mutex)
            {
                pSnd.priority = prio;
                pSnd.time = ++_timeCounter;
                SortPlayList();
            }
        }

        public void PauseAll(bool pause)
        {
            foreach (var i in _playList)
            {
                SoundToggle(i, pause);
            }
        }

        public void SoundStop(MusicEntry pSnd)
        {
            SoundStatus previousStatus = pSnd.status;
            pSnd.status = SoundStatus.Stopped;
            if (_soundVersion <= SciVersion.V0_LATE)
                pSnd.isQueued = false;
            if (pSnd.pStreamAud != null)
            {
                if (_currentlyPlayingSample == pSnd)
                    _currentlyPlayingSample = null;
                _mixer.StopHandle(pSnd.hCurrentAud);
            }

            if (pSnd.pMidiParser != null)
            {
                lock (_mutex)
                {
                    pSnd.pMidiParser.MainThreadBegin();
                    // We shouldn't call stop in case it's paused, otherwise we would send
                    // allNotesOff() again
                    if (previousStatus == SoundStatus.Playing)
                        pSnd.pMidiParser.Stop();
                    pSnd.pMidiParser.MainThreadEnd();
                    RemapChannels();
                }
            }

            pSnd.fadeStep = 0; // end fading, if fading was in progress
        }

        public void SoundKill(MusicEntry pSnd)
        {
            pSnd.status = SoundStatus.Stopped;

            lock (_mutex)
            {
                RemapChannels();

                if (pSnd.pMidiParser != null)
                {
                    pSnd.pMidiParser.MainThreadBegin();
                    pSnd.pMidiParser.UnloadMusic();
                    pSnd.pMidiParser.MainThreadEnd();
                    pSnd.pMidiParser = null;
                }

            }

            if (pSnd.pStreamAud != null)
            {
                if (_currentlyPlayingSample == pSnd)
                {
                    // Forget about this sound, in case it was currently playing
                    _currentlyPlayingSample = null;
                }
                _mixer.StopHandle(pSnd.hCurrentAud);
                pSnd.pStreamAud.DisposeIfNotNull();
                pSnd.pStreamAud = null;
                pSnd.pLoopStream.DisposeIfNotNull();
                pSnd.pLoopStream = null;
            }

            lock (_mutex)
            {
                int sz = _playList.Count, i;
                // Remove sound from playlist
                for (i = 0; i < sz; i++)
                {
                    if (_playList[i] == pSnd)
                    {
                        _playList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void SoundPlay(MusicEntry pSnd)
        {
            Monitor.Enter(_mutex);

            int playListCount;
            if (_soundVersion <= SciVersion.V1_EARLY && pSnd.playBed)
            {
                // If pSnd.playBed, and version <= SCI1_EARLY, then kill
                // existing sounds with playBed enabled.

                playListCount = _playList.Count;
                for (var i = 0; i < playListCount; i++)
                {
                    if (_playList[i] != pSnd && _playList[i].playBed)
                    {
                        // TODO: debugC(2, kDebugLevelSound, "Automatically stopping old playBed song from soundPlay");
                        MusicEntry old = _playList[i];
                        Monitor.Exit(_mutex);
                        SoundStop(old);
                        Monitor.Enter(_mutex);
                        break;
                    }
                }
            }

            playListCount = _playList.Count;
            int playListNo = playListCount;
            MusicEntry alreadyPlaying = null;

            // searching if sound is already in _playList
            for (var i = 0; i < playListCount; i++)
            {
                if (_playList[i] == pSnd)
                    playListNo = i;
                if ((_playList[i].status == SoundStatus.Playing) && (_playList[i].pMidiParser != null))
                    alreadyPlaying = _playList[i];
            }
            if (playListNo == playListCount)
            { // not found
                _playList.Add(pSnd);
            }

            pSnd.time = ++_timeCounter;
            SortPlayList();

            Monitor.Exit(_mutex);    // unlock to perform mixer-related calls

            if (pSnd.pMidiParser != null)
            {
                if ((_soundVersion <= SciVersion.V0_LATE) && (alreadyPlaying != null))
                {
                    // Music already playing in SCI0?
                    if (pSnd.priority > alreadyPlaying.priority)
                    {
                        // And new priority higher? pause previous music and play new one immediately.
                        // Example of such case: lsl3, when getting points (jingle is played then)
                        SoundPause(alreadyPlaying);
                        alreadyPlaying.isQueued = true;
                    }
                    else
                    {
                        // And new priority equal or lower? queue up music and play it afterwards done by
                        //  SoundCommandParser::updateSci0Cues()
                        // Example of such case: iceman room 14
                        pSnd.isQueued = true;
                        pSnd.status = SoundStatus.Paused;
                        return;
                    }
                }
            }

            if (pSnd.pStreamAud != null)
            {
                if (!_mixer.IsSoundHandleActive(pSnd.hCurrentAud))
                {
                    if ((_currentlyPlayingSample != null) && (_mixer.IsSoundHandleActive(_currentlyPlayingSample.hCurrentAud)))
                    {
                        // Another sample is already playing, we have to stop that one
                        // SSCI is only able to play 1 sample at a time
                        // In Space Quest 5 room 250 the player is able to open the air-hatch and kill himself.
                        //  In that situation the scripts are playing 2 samples at the same time and the first sample
                        //  is not supposed to play.
                        // TODO: SSCI actually calls kDoAudio(play) internally, which stops other samples from being played
                        //        but such a change isn't trivial, because we also handle Sound resources in here, that contain samples
                        _mixer.StopHandle(_currentlyPlayingSample.hCurrentAud);
                        Warning("kDoSound: sample already playing, old resource {0}, new resource {1}", _currentlyPlayingSample.resourceId, pSnd.resourceId);
                    }
                    // Sierra SCI ignores volume set when playing samples via kDoSound
                    //  At least freddy pharkas/CD has a script bug that sets volume to 0
                    //  when playing the "score" sample
                    if (pSnd.loop > 1)
                    {
                        pSnd.pLoopStream = new LoopingAudioStream(pSnd.pStreamAud, pSnd.loop, false);
                        pSnd.hCurrentAud = _mixer.PlayStream(pSnd.soundType,
                                                pSnd.pLoopStream, -1, Mixer.MaxChannelVolume, 0,
                                                false);
                    }
                    else
                    {
                        // Rewind in case we play the same sample multiple times
                        // (non-looped) like in pharkas right at the start
                        pSnd.pStreamAud.Rewind();
                        pSnd.hCurrentAud = _mixer.PlayStream(pSnd.soundType,
                                                pSnd.pStreamAud, -1, Mixer.MaxChannelVolume, 0,
                                                false);
                    }
                    // Remember the sample, that is now playing
                    _currentlyPlayingSample = pSnd;
                }
            }
            else
            {
                if (pSnd.pMidiParser != null)
                {
                    lock (_mutex)
                    {
                        pSnd.pMidiParser.MainThreadBegin();

                        if (pSnd.status != SoundStatus.Paused)
                            pSnd.pMidiParser.SendInitCommands();
                        pSnd.pMidiParser.SetVolume((byte)pSnd.volume);

                        // Disable sound looping and hold before jumpToTick is called,
                        // otherwise the song may keep looping forever when it ends in jumpToTick.
                        // This is needed when loading saved games, or when a game
                        // stops the same sound twice (e.g. LSL3 Amiga, going left from
                        // room 210 to talk with Kalalau). Fixes bugs #3083151 and #3106107.
                        ushort prevLoop = pSnd.loop;
                        short prevHold = pSnd.hold;
                        pSnd.loop = 0;
                        pSnd.hold = -1;

                        if (pSnd.status == SoundStatus.Stopped)
                            pSnd.pMidiParser.JumpToTick(0);
                        else
                        {
                            // Fast forward to the last position and perform associated events when loading
                            pSnd.pMidiParser.JumpToTick(pSnd.ticker, true, true, true);
                        }

                        // Restore looping and hold
                        pSnd.loop = prevLoop;
                        pSnd.hold = prevHold;
                        pSnd.pMidiParser.MainThreadEnd();
                    }
                }
            }

            pSnd.status = SoundStatus.Playing;

            lock (_mutex)
            {
                RemapChannels();
            }
        }

        // // this is used to set volume of the sample, used for fading only!
        public void SoundSetSampleVolume(MusicEntry pSnd, short volume)
        {
            //assert(volume <= MUSIC_VOLUME_MAX);
            //assert(pSnd.pStreamAud);
            _mixer.SetChannelVolume(pSnd.hCurrentAud, volume * 2); // Mixer is 0-255, SCI is 0-127
        }

        public void UpdateAudioStreamTicker(MusicEntry pSnd)
        {
            // assert(pSnd.pStreamAud != 0);
            pSnd.ticker = (ushort)(_mixer.GetSoundElapsedTime(pSnd.hCurrentAud) * 0.06);
        }

        public bool SoundIsActive(MusicEntry pSnd)
        {
            //assert(pSnd.pStreamAud != 0);
            return _mixer.IsSoundHandleActive(pSnd.hCurrentAud);
        }

        public void SoundPause(MusicEntry pSnd)
        {
            // SCI seems not to be pausing samples played back by kDoSound at all
            //  It only stops looping samples (actually doesn't loop them again before they are unpaused)
            //  Examples: Space Quest 1 death by acid drops (pause is called even specifically for the sample, see bug #3038048)
            //             Eco Quest 1 during the intro when going to the abort-menu
            //             In both cases sierra sci keeps playing
            //            Leisure Suit Larry 1 doll scene - it seems that pausing here actually just stops
            //             further looping from happening
            //  This is a somewhat bigger change, I'm leaving in the old code in here just in case
            //  I'm currently pausing looped sounds directly, non-looped sounds won't get paused
            if ((pSnd.pStreamAud != null) && (pSnd.pLoopStream == null))
                return;
            pSnd.pauseCounter++;
            if (pSnd.status != SoundStatus.Playing)
                return;
            pSnd.status = SoundStatus.Paused;
            if (pSnd.pStreamAud != null)
            {
                _mixer.PauseHandle(pSnd.hCurrentAud, true);
            }
            else
            {
                if (pSnd.pMidiParser != null)
                {
                    lock (_mutex)
                    {
                        pSnd.pMidiParser.MainThreadBegin();
                        pSnd.pMidiParser.Pause();
                        pSnd.pMidiParser.MainThreadEnd();
                        RemapChannels();
                    }
                }
            }
        }

        public void SoundSetMasterVolume(ushort vol)
        {
            _masterVolume = (byte)vol;

            lock (_mutex)
            {

                foreach (var item in PlayList)
                {
                    if (item.pMidiParser != null)
                        item.pMidiParser.SetMasterVolume((byte)vol);
                }
            }
        }

        private void SortPlayList()
        {
            // Sort the play list in descending priority order
            _playList.Sort(MusicEntryCompare);
        }

        // A larger priority value has higher priority. For equal priority values,
        // songs that have been added later have higher priority.
        private static int MusicEntryCompare(MusicEntry l, MusicEntry r)
        {
            var ret = r.priority.CompareTo(l.priority);
            if (ret == 0)
            {
                ret = r.time.CompareTo(l.time);
            }
            return ret;
        }

        private void MiditimerCallback(object p)
        {
            SciMusic sciMusic = (SciMusic)p;

            lock (sciMusic._mutex)
            {
                sciMusic.OnTimer();
            }
        }

        private void OnTimer()
        {
            // sending out queued commands that were "sent" via main thread
            SendMidiCommandsFromQueue();

            // remap channels, if requested
            if (_needsRemap)
                RemapChannels(false);
            _needsRemap = false;

            foreach (var item in _playList)
            {
                item.OnTimer();
            }
        }

        private void RemapChannels(bool mainThread = true)
        {
            if (_soundVersion <= SciVersion.V0_LATE)
                return;

            // NB: This function should only be called with _mutex locked
            // Make sure to set the mainThread argument correctly.


            ChannelRemapping map = DetermineChannelMap();

            DeviceChannelUsage[] currentMap = new DeviceChannelUsage[16];

#if DEBUG_REMAP
            debug("Remap results:");
#endif

            // Save current map, and then start from an empty map
            for (int i = 0; i < 16; ++i)
            {
                currentMap[i] = _channelMap[i];
                _channelMap[i]._song = null;
                _channelMap[i]._channel = -1;
            }

            // Inform MidiParsers of any unmapped channels
            int songIndex = -1;
            foreach (var song in _playList)
            {
                songIndex++;

                if (song == null || song.pMidiParser == null)
                    continue;

                bool[] channelMapped = new bool[16];
#if DEBUG_REMAP
                bool channelUsed[16];
#endif

                for (int j = 0; j < 16; ++j)
                {
                    if (map._map[j]._song == song)
                    {
                        int channel = map._map[j]._channel;
                        System.Diagnostics.Debug.Assert(channel >= 0 && channel <= 0x0F);
                        channelMapped[channel] = true;
                    }
#if DEBUG_REMAP
                    if (song._usedChannels[j] <= 0x0F)
                        channelUsed[song._usedChannels[j]] = true;
#endif
                }

                for (int j = 0; j < 16; ++j)
                {
                    if (!channelMapped[j])
                    {
                        if (mainThread) song.pMidiParser.MainThreadBegin();
                        song.pMidiParser.RemapChannel(j, -1);
                        if (mainThread) song.pMidiParser.MainThreadEnd();
#if DEBUG_REMAP
                        if (channelUsed[j])
                            debug(" Unmapping song %d, channel %d", songIndex, j);
#endif
                    }
                }
            }

            // Now reshuffle the channels on the device.

            // First, set up any dontRemap channels
            for (int i = 0; i < 16; ++i)
            {

                if (map._map[i]._song == null || map._map[i]._song.pMidiParser == null || !map._dontRemap[i])
                    continue;

                songIndex = -1;
                foreach (var song in _playList)
                {
                    songIndex++;
                    if (map._map[i]._song == song)
                        break;
                }

                _channelMap[i] = map._map[i];
                map._map[i]._song = null; // mark as done

                // If this channel was not yet mapped to the device, reset it
                if (!Equals(currentMap[i], _channelMap[i]))
                {
#if DEBUG_REMAP
                    debug(" Mapping (dontRemap) song %d, channel %d to device channel %d", songIndex, _channelMap[i]._channel, i);
#endif
                    if (mainThread) _channelMap[i]._song.pMidiParser.MainThreadBegin();
                    _channelMap[i]._song.pMidiParser.RemapChannel(_channelMap[i]._channel, i);
                    if (mainThread) _channelMap[i]._song.pMidiParser.MainThreadEnd();
                }

            }

            // Next, we look for channels which were already playing.
            // We keep those on the same device channel as before.
            for (int i = 0; i < 16; ++i)
            {

                if (map._map[i]._song == null)
                    continue;

                songIndex = -1;
                foreach (var song in _playList)
                {
                    songIndex++;
                    if (map._map[i]._song == song)
                        break;
                }


                for (int j = 0; j < 16; ++j)
                {
                    if (Equals(map._map[i], currentMap[j]))
                    {
                        // found it
                        _channelMap[j] = map._map[i];
                        map._map[i]._song = null; // mark as done
#if DEBUG_REMAP
                        debug(" Keeping song %d, channel %d on device channel %d", songIndex, _channelMap[j]._channel, j);
#endif
                        break;
                    }
                }
            }

            // Then, remap the rest.
            for (int i = 0; i < 16; ++i)
            {
                if (map._map[i]._song == null || map._map[i]._song.pMidiParser == null)
                    continue;

                songIndex = -1;
                foreach (var song in _playList)
                {
                    songIndex++;
                    if (map._map[i]._song == song)
                        break;
                }

                for (int j = _driverLastChannel; j >= _driverFirstChannel; --j)
                {
                    if (_channelMap[j]._song == null)
                    {
                        _channelMap[j] = map._map[i];
                        map._map[i]._song = null;
#if DEBUG_REMAP
                        debug(" Mapping song %d, channel %d to device channel %d", songIndex, _channelMap[j]._channel, j);
#endif
                        if (mainThread) _channelMap[j]._song.pMidiParser.MainThreadBegin();
                        _channelMap[j]._song.pMidiParser.RemapChannel(_channelMap[j]._channel, j);
                        if (mainThread) _channelMap[j]._song.pMidiParser.MainThreadEnd();
                        break;
                    }
                }

            }

            // And finally, stop any empty channels
            for (int i = _driverLastChannel; i >= _driverFirstChannel; --i)
            {
                if (_channelMap[i]._song == null && currentMap[i]._song != null)
                    ResetDeviceChannel(i, mainThread);
            }
        }

        private void ResetDeviceChannel(int devChannel, bool mainThread)
        {
            System.Diagnostics.Debug.Assert(devChannel >= 0 && devChannel <= 0x0F);

            if (mainThread)
            {
                PutMidiCommandInQueue(0x0040B0 | devChannel); // sustain off
                PutMidiCommandInQueue(0x007BB0 | devChannel); // notes off
                PutMidiCommandInQueue(0x004BB0 | devChannel); // release voices
            }
            else
            {
                _pMidiDrv.Send(0x0040B0 | devChannel); // sustain off
                _pMidiDrv.Send(0x007BB0 | devChannel); // notes off
                _pMidiDrv.Send(0x004BB0 | devChannel); // release voices
            }
        }

        private ChannelRemapping DetermineChannelMap()
        {
# if DEBUG_REMAP
            debug("Remap: avail chans: %d-%d", _driverFirstChannel, _driverLastChannel);
#endif

            ChannelRemapping map = new ChannelRemapping();
            ChannelRemapping backupMap;
            map.Clear();
            map._freeVoices = _pMidiDrv.Polyphony;

            if (_playList.Count == 0)
                return map;

            // TODO: set reverb, either from first song, or from global???

            int songIndex = -1;
            foreach (var song in _playList)
            {
                songIndex++;
                if (song.status != SoundStatus.Playing)
                    continue;

                // If song is digital, skip.
                // CHECKME: Is this condition correct?
                if (song.pMidiParser == null)
                {
# if DEBUG_REMAP
                    debug(" Song %d (%p), digital?", songIndex, (void*)song);
#endif
                    continue;
                }


# if DEBUG_REMAP
                const char* name = g_sci.getEngineState()._segMan.getObjectName(song.soundObj);
                debug(" Song %d (%p) [%s], prio %d%s", songIndex, (void*)song, name, song.priority, song.playBed ? ", bed" : "");
#endif

                // Store backup. If we fail to map this song, we will revert to this.
                backupMap = map;

                bool songMapped = true;

                for (int i = 0; i < 16; ++i)
                {
                    int c = song._usedChannels[i];
                    if (c == 0xFF || c == 0xFE || c == 0x0F)
                        continue;
                    MusicEntryChannel channel = song._chan[c];
                    if (channel._dontMap)
                    {
# if DEBUG_REMAP
                        debug("  Channel %d dontMap, skipping", c);
#endif
                        continue;
                    }
                    if (channel._mute)
                    {
# if DEBUG_REMAP
                        debug("  Channel %d muted, skipping", c);
#endif
                        continue;
                    }

                    bool dontRemap = channel._dontRemap || song.playBed;

# if DEBUG_REMAP
                    debug("  Channel %d: prio %d, %d voice%s%s", c, channel._prio, channel._voices, channel._voices == 1 ? "" : "s", dontRemap ? ", dontRemap" : "");
#endif

                    DeviceChannelUsage dc = new DeviceChannelUsage { _song = song, _channel = c };

                    // our target
                    int devChannel = -1;

                    if (dontRemap && map._map[c]._song == null)
                    {
                        // unremappable channel, with channel still free
                        devChannel = c;
                    }

                    // try to find a free channel
                    if (devChannel == -1)
                    {
                        for (int j = 0; j < 16; ++j)
                        {
                            if (Equals(map._map[j], dc))
                            {
                                // already mapped?! (Can this happen?)
                                devChannel = j;
                                break;
                            }
                            if (map._map[j]._song != null)
                                continue;

                            if (j >= _driverFirstChannel && j <= _driverLastChannel)
                                devChannel = j;
                        }
                    }

                    int prio = channel._prio;
                    if (prio > 0)
                    {
                        // prio > 0 means non-essential
                        prio = (16 - prio) + 16 * songIndex;
                    }

                    if (devChannel == -1 && prio > 0)
                    {
                        // no empty channel, but this isn't an essential channel,
                        // so we just skip it.
# if DEBUG_REMAP
                        debug("   skipping non-essential");
#endif
                        continue;
                    }

                    // try to empty a previous channel if this is an essential channel
                    if (devChannel == -1)
                    {
                        devChannel = map.LowestPrio();
                        if (devChannel != -1)
                            map.Evict(devChannel);
                    }

                    if (devChannel == -1)
                    {
                        // failed to map this song.
# if DEBUG_REMAP
                        debug("   no free (or lower priority) channel found");
#endif
                        songMapped = false;
                        break;
                    }

                    if (Equals(map._map[devChannel], dc))
                    {
                        // already mapped?! (Can this happen?)
                        continue;
                    }

                    int neededVoices = channel._voices;
                    // do we have enough free voices?
                    if (map._freeVoices < neededVoices)
                    {
                        // We only care for essential channels.
                        // Note: In early SCI1 interpreters, a song started by 'playBed'
                        // would not be skipped even if some channels couldn't be
                        // mapped due to voice limits. So, we treat all channels as
                        // non-essential here for playBed songs.
                        if (prio > 0 || (song.playBed && _soundVersion <= SciVersion.V1_EARLY))
                        {
# if DEBUG_REMAP
                            debug("   not enough voices; need %d, have %d. Skipping this channel.", neededVoices, map._freeVoices);
#endif
                            continue;
                        }
                        do
                        {
                            int j = map.LowestPrio();
                            if (j == -1)
                            {
# if DEBUG_REMAP
                                debug("   not enough voices; need %d, have %d", neededVoices, map._freeVoices);
#endif
                                // failed to free enough voices.
                                songMapped = false;
                                break;
                            }
# if DEBUG_REMAP
                            debug("   creating room for voices; evict %d", j);
#endif
                            map.Evict(j);
                        } while (map._freeVoices < neededVoices);

                        if (!songMapped)
                        {
                            // failed to map this song.
                            break;
                        }
                    }

                    // We have a channel and enough free voices now.
# if DEBUG_REMAP
                    debug("   trying to map to %d", devChannel);
#endif

                    map._map[devChannel] = dc;
                    map._voices[devChannel] = neededVoices;
                    map._prio[devChannel] = prio;
                    map._dontRemap[devChannel] = dontRemap;
                    map._freeVoices -= neededVoices;

                    if (!dontRemap || devChannel == c)
                    {
                        // If this channel fits here, we're done.
# if DEBUG_REMAP
                        debug("    OK");
#endif
                        continue;
                    }

                    // If this channel can't be remapped, we need to move it or fail.

                    if (!map._dontRemap[c])
                    {
                        // Target channel can be remapped, so just swap
                        map.Swap(devChannel, c);
                        continue;
                    }
# if DEBUG_REMAP
                    debug("    but %d is already dontRemap", c);
#endif

                    if (prio > 0)
                    {
                        // Channel collision, but this channel is non-essential,
                        // so drop it.
                        // TODO: Maybe we should have checked this before making room?
                        map.Evict(devChannel);
                        continue;
                    }

                    if (map._prio[c] > 0)
                    {
                        // Channel collision, but the other channel is non-essential,
                        // so we take its place.
                        map.Evict(c);
                        map.Swap(devChannel, c);
                        continue;
                    }

                    // Otherwise, we have two essential channels claiming the same
                    // device channel.
                    songMapped = false;
                    break;
                }

                if (!songMapped)
                {
                    // We failed to map this song, so unmap all its channels.
# if DEBUG_REMAP
                    debug(" Failed song");
#endif
                    map = backupMap;
                }
            }

            return map;
        }


        // This sends the stored commands from queue to driver (is supposed to get
        // called only during onTimer()). At least mt32 emulation doesn't like getting
        // note-on commands from main thread (if we directly send, we would get a crash
        // during piano scene in lsl5).
        private void SendMidiCommandsFromQueue()
        {
            int curCommand = 0;
            int commandCount = _queuedCommands.Count;

            while (curCommand < commandCount)
            {
                _pMidiDrv.Send(_queuedCommands[curCommand]);
                curCommand++;
            }
            _queuedCommands.Clear();
        }

        private MidiPlayer MidiPlayer_AdLib_create(SciVersion soundVersion)
        {
            return new MidiPlayer_AdLib(soundVersion);
        }

        public void PushBackSlot(MusicEntry slotEntry)
        {
            lock (_mutex)
            {
                _playList.Add(slotEntry);
            }
        }

        public MusicEntry GetSlot(Register obj)
        {
            lock (_mutex)
            {
                return _playList.FirstOrDefault(e => e.soundObj == obj);
            }
        }

        public void SoundInitSnd(MusicEntry pSnd)
        {
            // Remove all currently mapped channels of this MusicEntry first,
            // since they will no longer be valid.
            for (int i = 0; i < 16; ++i)
            {
                if (_channelMap[i]._song == pSnd)
                {
                    _channelMap[i]._song = null;
                    _channelMap[i]._channel = -1;
                }
            }

            int channelFilterMask = 0;
            SoundResource.Track track = pSnd.soundRes.GetTrackByType(_pMidiDrv.PlayId);

            // If MIDI device is selected but there is no digital track in sound
            // resource try to use Adlib's digital sample if possible. Also, if the
            // track couldn't be found, load the digital track, as some games depend on
            // this (e.g. the Longbow demo).
            if (track == null || (_useDigitalSFX && track.digitalChannelNr == -1))
            {
                SoundResource.Track digital = pSnd.soundRes.DigitalTrack;
                if (digital != null)
                    track = digital;
            }

            pSnd.time = ++_timeCounter;

            if (track != null)
            {
                // Play digital sample
                if (track.digitalChannelNr != -1)
                {
                    var channelData = track.channels[track.digitalChannelNr].data;
                    if (pSnd.pStreamAud != null)
                    {
                        pSnd.pStreamAud.DisposeIfNotNull();
                    }
                    var flags = AudioFlags.Unsigned;
                    // Amiga SCI1 games had signed sound data
                    if (_soundVersion >= SciVersion.V1_EARLY && SciEngine.Instance.Platform == Core.IO.Platform.Amiga)
                        flags = 0;
                    int endPart = track.digitalSampleEnd > 0 ? (track.digitalSampleSize - track.digitalSampleEnd) : 0;
                    pSnd.pStreamAud = new RawStream(flags, track.digitalSampleRate, false,
                        new MemoryStream(channelData.Data, channelData.Offset + track.digitalSampleStart, track.digitalSampleSize - track.digitalSampleStart - endPart));
                    pSnd.pLoopStream.DisposeIfNotNull();
                    pSnd.pLoopStream = null;
                    pSnd.soundType = SoundType.SFX;
                    pSnd.hCurrentAud = new SoundHandle();
                    pSnd.playBed = false;
                    pSnd.overridePriority = false;
                }
                else
                {
                    // play MIDI track
                    lock (_mutex)
                    {
                        pSnd.soundType = SoundType.Music;
                        if (pSnd.pMidiParser == null)
                        {
                            pSnd.pMidiParser = new MidiParser_SCI(_soundVersion, this);
                            pSnd.pMidiParser.MidiDriver = _pMidiDrv;
                            pSnd.pMidiParser.TimerRate = _dwTempo;
                            pSnd.pMidiParser.SetMasterVolume(_masterVolume);
                        }

                        pSnd.pauseCounter = 0;

                        // Find out what channels to filter for SCI0
                        channelFilterMask = pSnd.soundRes.GetChannelFilterMask(_pMidiDrv.PlayId, _pMidiDrv.HasRhythmChannel);

                        for (int i = 0; i < 16; ++i)
                            pSnd._usedChannels[i] = 0xFF;
                        for (int i = 0; i < track.channelCount; ++i)
                        {
                            SoundResource.Channel chan = track.channels[i];

                            pSnd._usedChannels[i] = chan.number;
                            pSnd._chan[chan.number]._dontRemap = (chan.flags & 2) != 0;
                            pSnd._chan[chan.number]._prio = (sbyte)chan.prio;
                            pSnd._chan[chan.number]._voices = (sbyte)chan.poly;

                            // CHECKME: Some SCI versions use chan.flags & 1 for this:
                            pSnd._chan[chan.number]._dontMap = false;

                            // FIXME: Most MIDI tracks use the first 10 bytes for
                            // fixed MIDI commands. SSCI skips those the first iteration,
                            // but _does_ update channel state (including volume) with
                            // them. Specifically, prio/voices, patch, volume, pan.
                            // This should probably be implemented in
                            // MidiParser_SCI::loadMusic.
                        }

                        pSnd.pMidiParser.MainThreadBegin();
                        // loadMusic() below calls jumpToTick.
                        // Disable sound looping and hold before jumpToTick is called,
                        // otherwise the song may keep looping forever when it ends in
                        // jumpToTick (e.g. LSL3, when going left from room 210).
                        ushort prevLoop = pSnd.loop;
                        short prevHold = pSnd.hold;
                        pSnd.loop = 0;
                        pSnd.hold = -1;
                        pSnd.playBed = false;
                        pSnd.overridePriority = false;

                        pSnd.pMidiParser.LoadMusic(track, pSnd, channelFilterMask, _soundVersion);
                        pSnd.reverb = (sbyte)pSnd.pMidiParser.SongReverb;

                        // Restore looping and hold
                        pSnd.loop = prevLoop;
                        pSnd.hold = prevHold;
                        pSnd.pMidiParser.MainThreadEnd();
                    }
                }
            }
        }

        public void NeedsRemap()
        {
            _needsRemap = true;
        }

        public void PutMidiCommandInQueue(int midi)
        {
            _queuedCommands.Add(midi);
        }

        private void PutMidiCommandInQueue(byte status, byte firstOp, byte secondOp)
        {
            PutMidiCommandInQueue(status | (firstOp << 8) | (secondOp << 16));
        }

        public void SoundResume(MusicEntry pSnd)
        {
            if (pSnd.pauseCounter > 0)
                pSnd.pauseCounter--;
            if (pSnd.pauseCounter != 0)
                return;
            if (pSnd.status != SoundStatus.Paused)
                return;
            if (pSnd.pStreamAud != null)
            {
                _mixer.PauseHandle(pSnd.hCurrentAud, false);
                pSnd.status = SoundStatus.Playing;
            }
            else
            {
                SoundPlay(pSnd);
            }
        }

        public void ClearPlayList()
        {
            // we must NOT lock our mutex here. Playlist is modified inside soundKill() which will lock the mutex
            //  during deletion. If we lock it here, a deadlock may occur within soundStop() because that one
            //  calls the mixer, which will also lock the mixer mutex and if the mixer thread is active during
            //  that time, we will get a deadlock.
            while (_playList.Count > 0)
            {
                SoundStop(_playList[0]);
                SoundKill(_playList[0]);
            }
        }

        public void SoundToggle(MusicEntry pSnd, bool pause)
        {
            if (pause)
                SoundPause(pSnd);
            else
                SoundResume(pSnd);
        }

        public ushort SoundGetMasterVolume()
        {
            return _masterVolume;
        }

        public void SoundSetVolume(MusicEntry pSnd, byte volume)
        {
            //assert(volume <= MUSIC_VOLUME_MAX);
            if (pSnd.pStreamAud != null)
            {
                // we simply ignore volume changes for samples, because sierra sci also
                //  doesn't support volume for samples via kDoSound
            }
            else if (pSnd.pMidiParser != null)
            {
                lock (_mutex)
                {
                    pSnd.pMidiParser.MainThreadBegin();
                    pSnd.pMidiParser.SetVolume(volume);
                    pSnd.pMidiParser.MainThreadEnd();
                }
            }
        }

        public ushort SoundGetVoices()
        {
            lock (_mutex)
            {
                return (ushort)_pMidiDrv.Polyphony;
            }
        }
    }
}
