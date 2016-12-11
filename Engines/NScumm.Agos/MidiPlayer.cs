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
using System.Text;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Common;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal enum kMusicMode
    {
        kMusicModeDisabled = 0,
        kMusicModeAccolade = 1,
        kMusicModeMilesAudio = 2,
        kMusicModeSimon1 = 3
    }

    internal class MidiPlayer : MidiDriverBase
    {
        private const int MIDI_SETUP_BUNDLE_HEADER_SIZE = 56;
        private const int MIDI_SETUP_BUNDLE_FILEHEADER_SIZE = 48;
        private const int MIDI_SETUP_BUNDLE_FILENAME_MAX_SIZE = 12;

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

        public int MusicVolume => _musicVolume;
        public int SfxVolume => _sfxVolume;

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

            string accoladeDriverFilename = null;
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
                            _driver = MidiDriver_Accolade_AdLib_create(accoladeDriverFilename);
                            break;
                        case MusicType.MT32:
                            _driver = MidiDriverAccoladeMt32Create(accoladeDriverFilename);
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

                    //setTimerRate(_driver.getBaseTempo());
                    return 0;
                }

                case kMusicMode.kMusicModeMilesAudio:
                {
                    switch (musicType)
                    {
                        case MusicType.AdLib:
                        {
                            if (Engine.FileExists("MIDPAK.AD"))
                            {
                                // if there is a file called MIDPAK.AD, use it directly
                                Warning("SIMON 2: using MIDPAK.AD");
                                _driver = MidiDriverMilesAdLib.Create("MIDPAK.AD", "MIDPAK.AD");
                            }
                            else
                            {
                                // if there is no file called MIDPAK.AD, try to extract it from the file SETUP.SHR
                                // if we didn't do this, the user would be forced to "install" the game instead of simply
                                // copying all files from CD-ROM.
                                using (var midpakAdLibStream = Simon2SetupExtractFile("MIDPAK.AD"))
                                {
                                    if (midpakAdLibStream == null)
                                        Error("MidiPlayer: could not extract MIDPAK.AD from SETUP.SHR");

                                    // Pass this extracted data to the driver
                                    Warning("SIMON 2: using MIDPAK.AD extracted from SETUP.SHR");
                                    _driver = MidiDriverMilesAdLib.Create("", "", midpakAdLibStream);
                                }
                            }
                            // TODO: not sure what's going wrong with AdLib
                            // it doesn't seem to matter if we use the regular XMIDI tracks or the 2nd set meant for MT32
                            break;
                        }
                        case MusicType.MT32:
                            _driver = MidiDriverMilesMt32.Create(string.Empty);
                            _nativeMT32 = true; // use 2nd set of XMIDI tracks
                            break;
                        case MusicType.GeneralMidi:
                            if (ConfigManager.Instance.Get<bool>("native_mt32"))
                            {
                                _driver = MidiDriverMilesMt32.Create(string.Empty);
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

                        _driver = CreateMidiDriverSimon1AdLib("MT_FM.IBK");
                        if (_driver != null && _driver.Open() == 0)
                        {
                            _driver.SetTimerCallback(this, OnTimer);
                            // Like the original, we enable the rhythm support by default.
                            _driver.Send(0xB0, 0x67, 0x01);
                            return 0;
                        }
                        _driver = null;
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

            _map_mt32_to_gm = gameType != SIMONGameType.GType_SIMON2 && !_nativeMT32;

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

        private MidiDriver CreateMidiDriverSimon1AdLib(string instrumentFilename)
        {
            // Load instrument data.
            var ibk = Engine.OpenFileRead(instrumentFilename);

            if (ibk == null)
            {
                return null;
            }

            var br = new BinaryReader(ibk);
            if (br.ReadUInt32BigEndian() != 0x49424b1a)
            {
                return null;
            }

            byte[] instrumentData = new byte[128 * 16];
            if (ibk.Read(instrumentData, 0, 128 * 16) != 128 * 16)
            {
                return null;
            }

            return new MidiDriverSimon1AdLib(instrumentData);
        }

        private static MidiDriver MidiDriver_Accolade_AdLib_create(string driverFilename)
        {
            byte[] driverData;
            ushort driverDataSize;
            bool isMusicDrvFile;

            MidiDriverAccoladeReadDriver(driverFilename, MusicType.AdLib, out driverData, out driverDataSize,
                out isMusicDrvFile);
            if (driverData == null)
                Error("ACCOLADE-ADLIB: error during readDriver()");

            var driver = new MidiDriverAccoladeAdLib();
            if (driver != null)
            {
                if (!driver.SetupInstruments(driverData, driverDataSize, isMusicDrvFile))
                {
                    driver = null;
                }
            }

            return driver;
        }

        // this reads and gets Accolade driver data
        // we need it for channel mapping, instrument mapping and other things
        // this driver data chunk gets passed to the actual music driver (MT32 / AdLib)
        private static void MidiDriverAccoladeReadDriver(string filename, MusicType requestedDriverType,
            out byte[] driverData, out ushort driverDataSize, out bool isMusicDrvFile)
        {
            driverData = null;
            driverDataSize = 0;
            isMusicDrvFile = false;

            var driverStream = Engine.OpenFileRead(filename);
            if (driverStream == null)
            {
                Error("{0}: unable to open file", filename);
            }

            var br = new BinaryReader(driverStream);
            if (filename == "INSTR.DAT")
            {
                // INSTR.DAT: used by Elvira 1
                int streamSize = (int) driverStream.Length;
                int streamLeft = streamSize;
                ushort skipChunks = 0; // 1 for MT32, 0 for AdLib
                ushort chunkSize = 0;

                switch (requestedDriverType)
                {
                    case MusicType.AdLib:
                        skipChunks = 0;
                        break;
                    case MusicType.MT32:
                        skipChunks = 1; // Skip one entry for MT32
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }

                do
                {
                    if (streamLeft < 2)
                        Error("{0}: unexpected EOF", filename);

                    chunkSize = br.ReadUInt16();
                    streamLeft -= 2;

                    if (streamLeft < chunkSize)
                        Error("{0}: unexpected EOF", filename);

                    if (skipChunks != 0)
                    {
                        // Skip the chunk
                        driverStream.Seek(chunkSize, SeekOrigin.Current);
                        streamLeft -= chunkSize;

                        skipChunks--;
                    }
                } while (skipChunks != 0);

                // Seek over the ASCII string until there is a NUL terminator
                byte curByte = 0;

                do
                {
                    if (chunkSize == 0)
                        Error("{0}: no actual instrument data found", filename);

                    curByte = br.ReadByte();
                    chunkSize--;
                } while (curByte != 0);

                driverDataSize = chunkSize;

                // Read the requested instrument data entry
                driverData = new byte[driverDataSize];
                driverStream.Read(driverData, 0, driverDataSize);
            }
            else if (filename == "MUSIC.DRV")
            {
                // MUSIC.DRV / used by Elvira 2 / Waxworks / Simon 1 demo
                int streamSize = (int) driverStream.Length;
                int streamLeft = streamSize;
                ushort getChunk = 0; // 4 for MT32, 2 for AdLib

                switch (requestedDriverType)
                {
                    case MusicType.AdLib:
                        getChunk = 2;
                        break;
                    case MusicType.MT32:
                        getChunk = 4;
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }

                if (streamLeft < 2)
                    Error("{0}: unexpected EOF", filename);

                ushort chunkCount = br.ReadUInt16();
                streamLeft -= 2;

                if (getChunk >= chunkCount)
                    Error("{0}: required chunk not available", filename);

                ushort headerOffset = (ushort) (2 + (28 * getChunk));
                streamLeft -= (28 * getChunk);

                if (streamLeft < 28)
                    Error("{0}: unexpected EOF", filename);

                // Seek to required chunk
                driverStream.Seek(headerOffset, SeekOrigin.Begin);
                driverStream.Seek(20, SeekOrigin.Current); // skip over name
                streamLeft -= 20;

                ushort musicDrvSignature = br.ReadUInt16();
                ushort musicDrvType = br.ReadUInt16();
                ushort chunkOffset = br.ReadUInt16();
                ushort chunkSize = br.ReadUInt16();

                // Security checks
                if (musicDrvSignature != 0xFEDC)
                    Error("{0}: chunk signature mismatch", filename);
                if (musicDrvType != 1)
                    Error("{0}: not a music driver", filename);
                if (chunkOffset >= streamSize)
                    Error("{0}: driver chunk points outside of file", filename);

                streamLeft = streamSize - chunkOffset;
                if (streamLeft < chunkSize)
                    Error("{0}: driver chunk is larger than file", filename);

                driverDataSize = chunkSize;

                // Read the requested instrument data entry
                driverData = new byte[driverDataSize];

                driverStream.Seek(chunkOffset, SeekOrigin.Begin);
                driverStream.Read(driverData, 0, driverDataSize);
                isMusicDrvFile = true;
            }

            driverStream.Dispose();
        }

        private static MidiDriver MidiDriverAccoladeMt32Create(string driverFilename)
        {
            byte[] driverData;
            ushort driverDataSize;
            bool isMusicDrvFile;

            MidiDriverAccoladeReadDriver(driverFilename, MusicType.MT32, out driverData, out driverDataSize,
                out isMusicDrvFile);
            if (driverData == null)
                Error("ACCOLADE-ADLIB: error during readDriver()");

            var driver = new MidiDriverAccoladeMt32();
            if (driver.SetupInstruments(driverData, driverDataSize, isMusicDrvFile)) return driver;

            driver.Dispose();
            driver = null;

            return driver;
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
            return new MidiParserS1D();
        }

        public void LoadMultipleSMF(Stream @in, bool sfx = false)
        {
            // This is a special case for Simon 2 Windows.
            // Instead of having multiple sequences as
            // separate tracks in a Type 2 file, simon2win
            // has multiple songs, each of which is a Type 1
            // file. Thus, preceding the songs is a single
            // byte specifying how many songs are coming.
            // We need to load ALL the songs and then
            // treat them as separate tracks -- for the
            // purpose of jumps, anyway.
            lock (_mutex)
            {
                var br = new BinaryReader(@in);
                MusicInfo p = sfx ? _sfx : _music;
                ClearConstructs(p);

                p.num_songs = br.ReadByte();
                if (p.num_songs > 16)
                {
                    Warning("PlayMultipleSMF: {0} is too many songs to keep track of", (int) p.num_songs);
                    return;
                }

                byte i;
                for (i = 0; i < p.num_songs; ++i)
                {
                    byte[] buf = new byte[5];
                    int pos = (int) @in.Position;

                    // Make sure there's a MThd
                    @in.Read(buf, 0, 4);
                    if (buf.GetRawText(0,4) != "MThd")
                    {
                        Warning("Expected MThd but found '{0}{1}{2}{3}' instead", (char)buf[0], (char)buf[1],
                            (char)buf[2], (char)buf[3]);
                        return;
                    }
                    @in.Seek(br.ReadUInt32BigEndian(), SeekOrigin.Current);

                    // Now skip all the MTrk blocks
                    while (true)
                    {
                        @in.Read(buf, 0, 4);
                        if (buf.GetRawText(0, 4) != "MTrk")
                            break;
                        @in.Seek(br.ReadUInt32BigEndian(), SeekOrigin.Current);
                    }

                    int pos2 = (int) (@in.Position - 4);
                    int size = pos2 - pos;
                    p.songs[i] = new byte[size];
                    @in.Seek(pos, SeekOrigin.Begin);
                    var tmp = p.songs[i];
                    @in.Read(tmp.Data, tmp.Offset, size);
                    p.song_sizes[i] = size;
                }

                if (!sfx)
                {
                    _currentTrack = 255;
                    ResetVolumeTable();
                }
            }
        }

        public void LoadXMIDI(Stream @in, bool sfx = false)
        {
            lock (_mutex)
            {
                var br = new BinaryReader(@in);
                MusicInfo p = sfx ? _sfx : _music;
                ClearConstructs(p);

                byte[] buf = new byte[4];
                int pos = (int) @in.Position;
                int size = 4;
                @in.Read(buf, 0, 4);
                if (buf.GetRawText(4) == "FORM")
                {
                    for (var i = 0; i < 16; ++i)
                    {
                        if (buf.GetRawText(4) == "CAT ")
                            break;
                        size += 2;
                        Array.Copy(buf, 2, buf, 0, 2);
                        @in.Read(buf, 2, 2);
                    }
                    if (buf.GetRawText(4) != "CAT ")
                    {
                        Error("Could not find 'CAT ' tag to determine resource size");
                    }
                    size += 4 + br.ReadInt32BigEndian();
                    @in.Seek(pos, 0);
                    p.data = new byte[size];
                    @in.Read(p.data.Data, 0, size);
                }
                else
                {
                    Error("Expected 'FORM' tag but found '{0}{1}{2}{3}' instead", buf[0], buf[1], buf[2], buf[3]);
                }

                // In the DOS version of Simon the Sorcerer 2, the music contains lots
                // of XMIDI callback controller events. As far as we know, they aren't
                // actually used, so we disable the callback handler explicitly.

                var parser = MidiParser.CreateXMidiParser();
                parser.MidiDriver = this;
                parser.TimerRate = _driver.BaseTempo;
                parser.LoadMusic(p.data, size);

                if (!sfx)
                {
                    _currentTrack = 255;
                    ResetVolumeTable();
                }
                p.parser = parser; // That plugs the power cord into the wall
            }
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

        // PKWARE data compression library (called "DCL" in ScummVM) was used for storing files within SETUP.SHR
        // we need it to be able to get the file MIDPAK.AD, otherwise we would have to require the user
        // to "install" the game before being able to actually play it, when using AdLib.
        //
        // SETUP.SHR file format:
        //  [bundle file header]
        //    [compressed file header] [compressed file data]
        //     * compressed file count
        private Stream Simon2SetupExtractFile(string requestedFileName)
        {
            int bundleSize = 0;
            int bundleBytesLeft = 0;
            byte[] bundleHeader = new byte[MIDI_SETUP_BUNDLE_HEADER_SIZE];
            byte[] bundleFileHeader = new byte[MIDI_SETUP_BUNDLE_FILEHEADER_SIZE];
            ushort bundleFileCount = 0;
            ushort bundleFileNr = 0;

            StringBuilder fileName = new StringBuilder();

            Stream extractedStream = null;

            var setupBundleStream = Engine.OpenFileRead("setup.shr");
            if (setupBundleStream == null)
                Error("MidiPlayer: could not open setup.shr");

            bundleSize = (int) setupBundleStream.Length;
            bundleBytesLeft = bundleSize;

            if (bundleSize < MIDI_SETUP_BUNDLE_HEADER_SIZE)
                Error("MidiPlayer: unexpected EOF in setup.shr");

            if (setupBundleStream.Read(bundleHeader, 0, MIDI_SETUP_BUNDLE_HEADER_SIZE) != MIDI_SETUP_BUNDLE_HEADER_SIZE)
                Error("MidiPlayer: setup.shr read error");
            bundleBytesLeft -= MIDI_SETUP_BUNDLE_HEADER_SIZE;

            // Verify header byte
            if (bundleHeader[13] != 't')
                Error("MidiPlayer: setup.shr bundle header data mismatch");

            bundleFileCount = bundleHeader.ToUInt16(14);

            // Search for requested file
            while (bundleFileNr < bundleFileCount)
            {
                if (bundleBytesLeft < bundleFileHeader.Length)
                    Error("MidiPlayer: unexpected EOF in setup.shr");

                if (setupBundleStream.Read(bundleFileHeader, 0, bundleFileHeader.Length) != bundleFileHeader.Length)
                    Error("MidiPlayer: setup.shr read error");
                bundleBytesLeft -= MIDI_SETUP_BUNDLE_FILEHEADER_SIZE;

                // Extract filename from file-header
                fileName.Clear();
                for (var curPos = 0; curPos < MIDI_SETUP_BUNDLE_FILENAME_MAX_SIZE; curPos++)
                {
                    if (bundleFileHeader[curPos] == 0) // terminating NUL
                        break;
                    fileName.Insert(curPos, new[] {(char) bundleFileHeader[curPos]});
                }

                // Get compressed
                int fileCompressedSize = bundleFileHeader.ToInt32(20);
                if (fileCompressedSize == 0)
                    Error("MidiPlayer: compressed file is 0 bytes, data corruption?");
                if (bundleBytesLeft < fileCompressedSize)
                    Error("MidiPlayer: unexpected EOF in setup.shr");

                if (fileName.ToString() == requestedFileName)
                {
                    // requested file found
                    var fileCompressedDataPtr = new byte[fileCompressedSize];

                    if (setupBundleStream.Read(fileCompressedDataPtr, 0, fileCompressedSize) != fileCompressedSize)
                        Error("MidiPlayer: setup.shr read error");

                    using (var compressedStream = new MemoryStream(fileCompressedDataPtr, 0, fileCompressedSize))
                    {
                        // we don't know the unpacked size, let decompressor figure it out
                        extractedStream = DecompressorDCL.Decompress(compressedStream);
                    }
                    break;
                }

                // skip compressed size
                setupBundleStream.Seek(fileCompressedSize, SeekOrigin.Current);
                bundleBytesLeft -= fileCompressedSize;

                bundleFileNr++;
            }
            setupBundleStream.Dispose();

            return extractedStream;
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