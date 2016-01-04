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

using NScumm.Core;
using NScumm.Core.Audio;
using System;
using System.Linq;
using NScumm.Sci.Engine;
using System.Collections.Generic;
using NScumm.Sci.Sound.Drivers;
using NScumm.Core.Audio.Decoders;
using System.IO;

namespace NScumm.Sci.Sound
{
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

        public MusicEntry()
        {
            signalQueue = new List<ushort>();
            _chan = new MusicEntryChannel[16];
            for (int i = 0; i < _chan.Length; i++)
            {
                _chan[i] = new MusicEntryChannel();
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
            else {
                fadeTicker = fadeTickerStep;
                volume += fadeStep;
                if (((fadeStep > 0) && (volume >= fadeTo)) || ((fadeStep < 0) && (volume <= fadeTo)))
                {
                    volume = fadeTo;
                    fadeStep = 0;
                    fadeCompleted = true;
                }

                // Only process MIDI streams in this thread, not digital sound effects
                if (pMidiParser!=null)
                {
                    pMidiParser.SetVolume((byte)volume);
                }

                fadeSetVolume = true; // set flag so that SoundCommandParser::cmdUpdateCues will set the volume of the stream
            }
        }
    }

    class DeviceChannelUsage
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
        private ISystem _system;
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
        private object _currentlyPlayingSample;
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

        public SciMusic(IMixer mixer, SciVersion soundVersion, bool useDigitalSFX)
        {
            _mixer = mixer;
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
                // TODO: warning("A Windows CD version with an alternate MIDI soundtrack has been chosen, but no MIDI music device has been selected. Reverting to the DOS soundtrack");
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
            else {
                if (SciEngine.Instance.GameId == SciGameId.FUNSEEKER)
                {
                    // HACK: The Fun Seeker's Guide demo doesn't have patch 3 and the version
                    // of the Adlib driver (adl.drv) that it includes is unsupported. That demo
                    // doesn't have any sound anyway, so this shouldn't be fatal.
                }
                else {
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

        private void RemapChannels(bool v)
        {
            throw new NotImplementedException();
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
                    pSnd.pStreamAud.Dispose();
                    var flags = AudioFlags.Unsigned;
                    // Amiga SCI1 games had signed sound data
                    if (_soundVersion >= SciVersion.V1_EARLY && SciEngine.Instance.Platform == Core.IO.Platform.Amiga)
                        flags = 0;
                    int endPart = track.digitalSampleEnd > 0 ? (track.digitalSampleSize - track.digitalSampleEnd) : 0;
                    pSnd.pStreamAud = new RawStream(flags, track.digitalSampleRate, false,
                        new MemoryStream(channelData.Data, channelData.Offset + track.digitalSampleStart, track.digitalSampleSize - track.digitalSampleStart - endPart));
                    pSnd.pLoopStream.Dispose();
                    pSnd.pLoopStream = null;
                    pSnd.soundType = SoundType.SFX;
                    pSnd.hCurrentAud = new SoundHandle();
                    pSnd.playBed = false;
                    pSnd.overridePriority = false;
                }
                else {
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

        
    }
}
