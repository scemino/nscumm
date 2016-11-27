//
//  MidiPlayer.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    enum kMusicMode
    {
        kMusicModeDisabled = 0,
        kMusicModeAccolade = 1,
        kMusicModeMilesAudio = 2,
        kMusicModeSimon1 = 3
    }

    internal class MidiPlayer : MidiDriverBase
    {
        public bool _adLibMusic;
        public bool _enable_sfx;

        protected readonly object _mutex = new object();
        protected MidiDriver _driver;
        protected bool _map_mt32_to_gm;
        protected bool _nativeMT32;

        protected readonly MusicInfo _music = new MusicInfo();
        protected readonly MusicInfo _sfx = new MusicInfo();
        protected MusicInfo _current = new MusicInfo(); // Allows us to establish current context for operations.

        // These are maintained for both music and SFX
        protected byte _masterVolume; // 0-255
        protected byte _musicVolume;
        protected byte _sfxVolume;
        protected bool _paused;

        // These are only used for music.
        protected byte _currentTrack;
        protected bool _loopTrack;
        protected byte _queuedTrack;
        protected bool _loopQueuedTrack;

        private kMusicMode _musicMode;

        public MidiPlayer()
        {
            // Since initialize() is called every time the music changes,
            // this is where we'll initialize stuff that must persist
            // between songs.
            _enable_sfx = true;

            _musicVolume = 255;
            _sfxVolume = 255;

            ResetVolumeTable();

            _currentTrack = 255;
            _queuedTrack = 255;

            _musicMode = kMusicMode.kMusicModeDisabled;
        }

        public override void Send(int b)
        {
            if (_current == null)
                return;

            if (_musicMode != kMusicMode.kMusicModeDisabled)
            {
                // Handle volume control for Simon1 output.
                if (_musicMode == kMusicMode.kMusicModeSimon1)
                {
                    // The driver does not support any volume control, thus we simply
                    // scale the velocities on note on for now.
                    // TODO: We should probably handle this at output level at some
                    // point. Then we can allow volume changes to affect already
                    // playing notes too. For now this simple change allows us to
                    // have some simple volume control though.
                    if ((b & 0xF0) == 0x90)
                    {
                        byte volume = (byte) ((b >> 16) & 0x7F);

                        if (_current == _sfx)
                        {
                            volume = (byte) (volume * _sfxVolume / 255);
                        }
                        else if (_current == _music)
                        {
                            volume = (byte) (volume * _musicVolume / 255);
                        }

                        b = (int) ((b & 0xFF00FFFF) | (volume << 16));
                    }
                }

                // Send directly to Accolade/Miles/Simon1 Audio driver
                _driver.Send(b);
                return;
            }

            byte channel = (byte) (b & 0x0F);
            if ((b & 0xFFF0) == 0x07B0)
            {
                // Adjust volume changes by master music and master sfx volume.
                byte volume = (byte) ((b >> 16) & 0x7F);
                _current.volume[channel] = volume;
                if (_current == _sfx)
                    volume = (byte) (volume * _sfxVolume / 255);
                else if (_current == _music)
                    volume = (byte) (volume * _musicVolume / 255);
                b = (int) ((b & 0xFF00FFFF) | (volume << 16));
            }
            else if ((b & 0xF0) == 0xC0 && _map_mt32_to_gm)
            {
                b = (int) ((b & 0xFFFF00FF) | (MidiDriver.Mt32ToGm[(b >> 8) & 0xFF] << 8));
            }
            else if ((b & 0xFFF0) == 0x007BB0)
            {
                // Only respond to an All Notes Off if this channel
                // has already been allocated.
                if (_current.channel[b & 0x0F] == null)
                    return;
            }
            else if ((b & 0xFFF0) == 0x79B0)
            {
                // "Reset All Controllers". There seems to be some confusion
                // about what this message should do to the volume controller.
                // See http://www.midi.org/about-midi/rp15.shtml for more
                // information.
                //
                // If I understand it correctly, the current standard indicates
                // that the volume should be reset, but the next revision will
                // exclude it. On my system, both ALSA and FluidSynth seem to
                // reset it, while AdLib does not. Let's follow the majority.

                _current.volume[channel] = 127;
            }

            // Allocate channels if needed
            if (_current.channel[channel] == null)
                _current.channel[channel] = (channel == 9) ? _driver.GetPercussionChannel() : _driver.AllocateChannel();

            if (_current.channel[channel] != null)
            {
                if (channel == 9)
                {
                    if (_current == _sfx)
                        _current.channel[9].Volume((byte) (_current.volume[9] * _sfxVolume / 255));
                    else if (_current == _music)
                        _current.channel[9].Volume((byte) (_current.volume[9] * _musicVolume / 255));
                }
                _current.channel[channel].Send((uint) b);
                if ((b & 0xFFF0) == 0x79B0)
                {
                    // We have received a "Reset All Controllers" message
                    // and passed it on to the MIDI driver. This may or may
                    // not have affected the volume controller. To ensure
                    // consistent behavior, explicitly set the volume to
                    // what we think it should be.

                    if (_current == _sfx)
                        _current.channel[channel].Volume((byte) (_current.volume[channel] * _sfxVolume / 255));
                    else if (_current == _music)
                        _current.channel[channel].Volume((byte) (_current.volume[channel] * _musicVolume / 255));
                }
            }
        }

        public MidiDriverError Open(SIMONGameType gameType, bool isDemo)
        {
            // Don't call open() twice!
            System.Diagnostics.Debug.Assert(_driver == null);

            string accoladeDriverFilename;
            MusicType musicType = MusicType.Invalid;

            switch (gameType)
            {
                case SIMONGameType.GType_ELVIRA1:
                    _musicMode = kMusicMode.kMusicModeAccolade;
                    accoladeDriverFilename = "INSTR.DAT";
                    break;
                case SIMONGameType.GType_ELVIRA2:
                case SIMONGameType.GType_WW:
                    // Attention: Elvira 2 shipped with INSTR.DAT and MUSIC.DRV
                    // MUSIC.DRV is the correct one. INSTR.DAT seems to be a left-over
                    _musicMode = kMusicMode.kMusicModeAccolade;
                    accoladeDriverFilename = "MUSIC.DRV";
                    break;
                case SIMONGameType.GType_SIMON1:
                    if (isDemo)
                    {
                        _musicMode = kMusicMode.kMusicModeAccolade;
                        accoladeDriverFilename = "MUSIC.DRV";
                    }
                    else if (Engine.FileExists("MT_FM.IBK"))
                    {
                        _musicMode = kMusicMode.kMusicModeSimon1;
                    }
                    break;
                case SIMONGameType.GType_SIMON2:
                    //_musicMode = kMusicModeMilesAudio;
                    // currently disabled, because there are a few issues
                    // MT32 seems to work fine now, AdLib seems to use bad instruments and is also outputting music on
                    // the right speaker only. The original driver did initialize the panning to 0 and the Simon2 XMIDI
                    // tracks don't set panning at all. We can reset panning to be centered, which would solve this
                    // issue, but we still don't know who's setting it in the original interpreter.
                    break;
            }

            DeviceHandle dev;
            MidiDriverError ret = 0;

            if (_musicMode != kMusicMode.kMusicModeDisabled)
            {
                dev = MidiDriver.DetectDevice(MusicDriverTypes.Midi | MusicDriverTypes.AdLib,
                    Engine.Instance.Settings.AudioDevice);
                musicType = MidiDriver.GetMusicType(dev);

                switch (musicType)
                {
                    case MusicType.AdLib:
                    case MusicType.MT32:
                        break;
                    case MusicType.GeneralMidi:
                        // TODO: vs
//                        if (!ConfMan.getBool("native_mt32")) {
//                            // Not a real MT32 / no MUNT
//                            ::GUI::MessageDialog dialog(("You appear to be using a General MIDI device,\n"
//                            "but your game only supports Roland MT32 MIDI.\n"
//                            "We try to map the Roland MT32 instruments to\n"
//                            "General MIDI ones. It is still possible that\n"
//                            "some tracks sound incorrect."));
//                            dialog.runModal();
//                        }
                        // Switch to MT32 driver in any case
                        musicType = MusicType.MT32;
                        break;
                    default:
                        _musicMode = kMusicMode.kMusicModeDisabled;
                        break;
                }
            }

            switch (_musicMode)
            {
                case kMusicMode.kMusicModeAccolade:
                {
                    // Setup midi driver
                    switch (musicType)
                    {
                        case MusicType.AdLib:
                            throw new NotImplementedException();
                            //_driver = MidiDriver_Accolade_AdLib_create(accoladeDriverFilename);
                            break;
                        case MusicType.MT32:
                            throw new NotImplementedException();
                            //_driver = MidiDriver_Accolade_MT32_create(accoladeDriverFilename);
                            break;
                    }
                    if (_driver == null)
                        return (MidiDriverError) 255;

                    ret = _driver.Open();
                    if (ret == 0)
                    {
                        throw new NotImplementedException();
                        // Reset is done inside our MIDI driver
                        //_driver.SetTimerCallback(this, OnTimer);
                    }

                    //setTimerRate(_driver.getBaseTempo());
                    return 0;
                }

                case kMusicMode.kMusicModeMilesAudio:
                {
                    switch (musicType)
                    {
                        case MusicType.AdLib:
                        {
                            throw new NotImplementedException();
                            /*Common::File instrumentDataFile;
                            if (instrumentDataFile.exists("MIDPAK.AD"))
                            {
                                // if there is a file called MIDPAK.AD, use it directly
                                Warning("SIMON 2: using MIDPAK.AD");
                                _driver = MidiDriver_Miles_AdLib_create("MIDPAK.AD", "MIDPAK.AD");
                            }
                            else
                            {
                                // if there is no file called MIDPAK.AD, try to extract it from the file SETUP.SHR
                                // if we didn't do this, the user would be forced to "install" the game instead of simply
                                // copying all files from CD-ROM.
                                var midpakAdLibStream = simon2SetupExtractFile("MIDPAK.AD");
                                if (!midpakAdLibStream)
                                    Error("MidiPlayer: could not extract MIDPAK.AD from SETUP.SHR");

                                // Pass this extracted data to the driver
                                Warning("SIMON 2: using MIDPAK.AD extracted from SETUP.SHR");
                                _driver = Audio::MidiDriver_Miles_AdLib_create("", "", midpakAdLibStream);
                                delete midpakAdLibStream;
                            }
                            // TODO: not sure what's going wrong with AdLib
                            // it doesn't seem to matter if we use the regular XMIDI tracks or the 2nd set meant for MT32
                            break;*/
                        }
                        case MusicType.MT32:
                            throw new NotImplementedException();
                            //_driver = Audio::MidiDriver_Miles_MT32_create("");
                            _nativeMT32 = true; // use 2nd set of XMIDI tracks
                            break;
                        case MusicType.GeneralMidi:
                            if (ConfigManager.Instance.Get<bool>("native_mt32"))
                            {
                                throw new NotImplementedException();
                                //_driver = Audio::MidiDriver_Miles_MT32_create("");
                                _nativeMT32 = true; // use 2nd set of XMIDI tracks
                            }
                            break;
                    }
                    if (_driver == null)
                        return (MidiDriverError) 255;

                    ret = _driver.Open();
                    if (ret == 0)
                    {
                        // Reset is done inside our MIDI driver
                        _driver.SetTimerCallback(this, OnTimer);
                    }
                    return 0;
                }

                case kMusicMode.kMusicModeSimon1:
                {
                    // This only handles the original AdLib driver of Simon1.
                    if (musicType == MusicType.AdLib)
                    {
                        _adLibMusic = true;
                        _map_mt32_to_gm = false;
                        _nativeMT32 = false;

                        throw new NotImplementedException();
//                        _driver = CreateMidiDriverSimon1AdLib("MT_FM.IBK");
//                        if (_driver != null && _driver.Open() == 0)
//                        {
//                            _driver.SetTimerCallback(this, OnTimer);
//                            // Like the original, we enable the rhythm support by default.
//                            _driver.Send(0xB0, 0x67, 0x01);
//                            return 0;
//                        }
//                        _driver = null;
                    }

                    _musicMode = kMusicMode.kMusicModeDisabled;
                    break;
                }
            }

            dev =
                MidiDriver.DetectDevice(MusicDriverTypes.AdLib | MusicDriverTypes.Midi |
                                        (gameType == SIMONGameType.GType_SIMON1
                                            ? MusicDriverTypes.PreferMt32
                                            : MusicDriverTypes.PreferGeneralMidi),
                    Engine.Instance.Settings.AudioDevice);
            _adLibMusic = (MidiDriver.GetMusicType(dev) == MusicType.AdLib);
            _nativeMT32 = ((MidiDriver.GetMusicType(dev) == MusicType.MT32) ||
                           ConfigManager.Instance.Get<bool>("native_mt32"));

            _driver = (MidiDriver) MidiDriver.CreateMidi(Engine.Instance.Mixer, dev);
            if (_driver == null)
                return (MidiDriverError) 255;

            if (_nativeMT32)
                _driver.Property(MidiDriver.PROP_CHANNEL_MASK, 0x03FE);

            _map_mt32_to_gm = (gameType != SIMONGameType.GType_SIMON2 && !_nativeMT32);

            ret = _driver.Open();
            if (ret != MidiDriverError.None)
                return ret;
            _driver.SetTimerCallback(this, OnTimer);

            if (_nativeMT32)
                _driver.SendMt32Reset();
            else
                _driver.SendGmReset();

            return 0;
        }

        public void SetVolume(int musicVol, int sfxVol)
        {
            if (musicVol < 0)
                musicVol = 0;
            else if (musicVol > 255)
                musicVol = 255;
            if (sfxVol < 0)
                sfxVol = 0;
            else if (sfxVol > 255)
                sfxVol = 255;

            if (_musicVolume == musicVol && _sfxVolume == sfxVol)
                return;

            _musicVolume = (byte) musicVol;
            _sfxVolume = (byte) sfxVol;

            // Now tell all the channels this.
            lock (_mutex)
            {
                if (_driver != null && !_paused)
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        if (_music.channel[i] != null)
                            _music.channel[i].Volume((byte) (_music.volume[i] * _musicVolume / 255));
                        if (_sfx.channel[i] != null)
                            _sfx.channel[i].Volume((byte) (_sfx.volume[i] * _sfxVolume / 255));
                    }
                }
            }
        }

        private void ResetVolumeTable()
        {
            for (var i = 0; i < 16; ++i)
            {
                _music.volume[i] = _sfx.volume[i] = 127;
                _driver?.Send(((_musicVolume >> 1) << 16) | 0x7B0 | i);
            }
        }

        public void LoadSMF(Stream stream, int song, bool sfx = false)
        {
            lock (_mutex)
            {
                var @in = new BinaryReader(stream);
                MusicInfo p = sfx ? _sfx : _music;
                ClearConstructs(p);

                int startpos = (int) stream.Position;
                char[] header = new char[4];
                @in.Read(header, 0, 4);
                bool isGMF = new string(header) == "GMF\x1";
                stream.Seek(startpos, SeekOrigin.Begin);

                int size = (int) (stream.Length - stream.Position);
                if (isGMF)
                {
                    if (sfx)
                    {
                        // Multiple GMF resources are stored in the SFX files,
                        // but each one is referenced by a pointer at the
                        // beginning of the file. Those pointers can be used
                        // to determine file size.
                        stream.Seek(0, SeekOrigin.Begin);
                        ushort value = (ushort) (@in.ReadUInt16() >> 2); // Number of resources
                        if (song != value - 1)
                        {
                            stream.Seek(song * 2 + 2, SeekOrigin.Begin);
                            value = @in.ReadUInt16();
                            size = value - startpos;
                        }
                        stream.Seek(startpos, SeekOrigin.Begin);
                    }
                    else if (size >= 64000)
                    {
                        // For GMF resources not in separate
                        // files, we're going to have to use
                        // hardcoded size tables.
                        size = simon1_gmf_size[song];
                    }
                }

                // When allocating space, add 4 bytes in case
                // this is a GMF and we have to tack on our own
                // End of Track event.
                p.data = new byte[size + 4];
                stream.Read(p.data.Data, p.data.Offset, size);

                uint timerRate = _driver.BaseTempo;

                if (isGMF)
                {
                    // The GMF header
                    // 3 BYTES: 'GMF'
                    // 1 BYTE : Major version
                    // 1 BYTE : Minor version
                    // 1 BYTE : Ticks (Ranges from 2 - 8, always 2 for SFX)
                    // 1 BYTE : Loop control. 0 = no loop, 1 = loop (Music only)
                    if (!sfx)
                    {
                        // In the original, the ticks value indicated how many
                        // times the music timer was called before it actually
                        // did something. The larger the value the slower the
                        // music.
                        //
                        // We, on the other hand, have a timer rate which is
                        // used to control by how much the music advances on
                        // each onTimer() call. The larger the value, the
                        // faster the music.
                        //
                        // It seems that 4 corresponds to our base tempo, so
                        // this should be the right way to calculate it.
                        timerRate = (4 * _driver.BaseTempo) / p.data[5];

                        // According to bug #1004919 calling setLoop() from
                        // within a lock causes a lockup, though I have no
                        // idea when this actually happens.
                        _loopTrack = (p.data[6] != 0);
                    }
                }

                MidiParser parser = MidiParser.CreateSmfParser();
                parser.Property(MidiParserProperty.MalformedPitchBends, 1);
                parser.MidiDriver = this;
                parser.TimerRate = timerRate;
                parser.LoadMusic(p.data.Data, p.data.Offset, size);

                if (!sfx)
                {
                    _currentTrack = 255;
                    ResetVolumeTable();
                }
                p.parser = parser; // That plugs the power cord into the wall
            }
        }

        public void StartTrack(int track)
        {
            lock (_mutex)
            {
                if (track == _currentTrack)
                    return;

                if (_music.num_songs > 0)
                {
                    if (track >= _music.num_songs)
                        return;

                    if (_music.parser != null)
                    {
                        _current = _music;
                        _music.parser = null;
                        _current = null;
                        _music.parser = null;
                    }

                    MidiParser parser = MidiParser.CreateSmfParser();
                    parser.Property(MidiParserProperty.MalformedPitchBends, 1);
                    parser.MidiDriver = this;
                    parser.TimerRate = _driver.BaseTempo;
                    parser.LoadMusic(_music.songs[track], _music.song_sizes[track]);

                    _currentTrack = (byte) track;
                    _music.parser = parser; // That plugs the power cord into the wall
                }
                else if (_music.parser != null)
                {
                    _music.parser.ActiveTrack = track;
                    _currentTrack = (byte) track;
                    _current = _music;
                    _music.parser.JumpToTick(0);
                    _current = null;
                }
            }
        }

        public void Stop()
        {
            lock (_mutex)
            {
                if (_music.parser != null)
                {
                    _current = _music;
                    _music.parser.JumpToTick(0);
                }
                _current = null;
                _currentTrack = 255;
            }
        }

        private void ClearConstructs()
        {
            ClearConstructs(_music);
            ClearConstructs(_sfx);
        }

        private void ClearConstructs(MusicInfo info)
        {
            info.num_songs = 0;

            info.data = BytePtr.Null;

            info.parser = null;

            if (_driver != null)
            {
                for (var i = 0; i < 16; ++i)
                {
                    if (info.channel[i] != null)
                    {
                        info.channel[i].AllNotesOff();
                        info.channel[i].Release();
                    }
                }
            }
            info.Clear();
        }

        private void OnTimer(object data)
        {
            var p = (MidiPlayer) data;
            lock (_mutex)
            {
                if (!p._paused)
                {
                    if (p._music.parser != null && p._currentTrack != 255)
                    {
                        p._current = p._music;
                        p._music.parser.OnTimer();
                    }
                }
                if (p._sfx.parser != null)
                {
                    p._current = p._sfx;
                    p._sfx.parser.OnTimer();
                }
                p._current = null;
            }
        }

        public void SetLoop(bool loop)
        {
            lock (_mutex)
            {
                _loopTrack = loop;
            }
        }

        public void LoadS1D(Stream stream, bool sfx = false)
        {
            lock (_mutex)
            {
                var @in = new BinaryReader(stream);
                MusicInfo p = sfx ? _sfx : _music;
                ClearConstructs(p);

                ushort size = @in.ReadUInt16();
                if (size != stream.Length - 2)
                {
                    Error("Size mismatch in MUS file ({0} versus reported {1})",
                        stream.Length - 2, (int) size);
                }

                p.data = new byte[size];
                @in.Read(p.data.Data, p.data.Offset, size);

                MidiParser parser = MidiParser_createS1D();
                parser.MidiDriver = this;
                parser.TimerRate = _driver.BaseTempo;
                parser.LoadMusic(p.data.Data, p.data.Offset, size);

                if (!sfx)
                {
                    _currentTrack = 255;
                    ResetVolumeTable();
                }
                p.parser = parser; // That plugs the power cord into the wall
            }
        }

        private MidiParser MidiParser_createS1D()
        {
            throw new NotImplementedException();
        }

        public void LoadMultipleSMF(Stream gameFile)
        {
            throw new NotImplementedException();
        }

        public void LoadXMIDI(Stream gameFile)
        {
            throw new NotImplementedException();
        }

        public void Pause(bool b)
        {
            if (_paused == b || _driver == null)
                return;
            _paused = b;

            lock (_mutex)
            {
                for (int i = 0; i < 16; ++i)
                {
                    if (_music.channel[i] != null)
                        _music.channel[i].Volume((byte) (_paused ? 0 : (_music.volume[i] * _musicVolume / 255)));
                    if (_sfx.channel[i] != null)
                        _sfx.channel[i].Volume((byte) (_paused ? 0 : (_sfx.volume[i] * _sfxVolume / 255)));
                }
            }
        }

        private static readonly int[] simon1_gmf_size =
        {
            8900, 12166, 2848, 3442, 4034, 4508, 7064, 9730, 6014, 4742, 3138,
            6570, 5384, 8909, 6457, 16321, 2742, 8968, 4804, 8442, 7717,
            9444, 5800, 1381, 5660, 6684, 2456, 4744, 2455, 1177, 1232,
            17256, 5103, 8794, 4884, 16
        };


    }
}