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

using System.IO;
using NScumm.Core.Audio;

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

        protected object _mutex = new object();
        protected MidiDriver _driver;
        protected bool _map_mt32_to_gm;
        protected bool _nativeMT32;

        protected MusicInfo _music = new MusicInfo();
        protected MusicInfo _sfx = new MusicInfo();
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
            throw new System.NotImplementedException();
        }

        public MidiDriverError Open(SIMONGameType gameType, bool isDemo)
        {
            // Don't call open() twice!
//            System.Diagnostics.Debug.Assert(_driver == null);
//
//            string accoladeDriverFilename;
//            MusicType musicType = MusicType.Invalid;
//
//            switch (gameType)
//            {
//                case SIMONGameType.GType_ELVIRA1:
//                    _musicMode = kMusicMode.kMusicModeAccolade;
//                    accoladeDriverFilename = "INSTR.DAT";
//                    break;
//                case SIMONGameType.GType_ELVIRA2:
//                case SIMONGameType.GType_WW:
//                    // Attention: Elvira 2 shipped with INSTR.DAT and MUSIC.DRV
//                    // MUSIC.DRV is the correct one. INSTR.DAT seems to be a left-over
//                    _musicMode = kMusicMode.kMusicModeAccolade;
//                    accoladeDriverFilename = "MUSIC.DRV";
//                    break;
//                case SIMONGameType.GType_SIMON1:
//                    if (isDemo)
//                    {
//                        _musicMode = kMusicMode.kMusicModeAccolade;
//                        accoladeDriverFilename = "MUSIC.DRV";
//                    }
//                    else if (Engine.FileExists("MT_FM.IBK"))
//                    {
//                        _musicMode = kMusicMode.kMusicModeSimon1;
//                    }
//                    break;
//                case SIMONGameType.GType_SIMON2:
//                    //_musicMode = kMusicModeMilesAudio;
//                    // currently disabled, because there are a few issues
//                    // MT32 seems to work fine now, AdLib seems to use bad instruments and is also outputting music on
//                    // the right speaker only. The original driver did initialize the panning to 0 and the Simon2 XMIDI
//                    // tracks don't set panning at all. We can reset panning to be centered, which would solve this
//                    // issue, but we still don't know who's setting it in the original interpreter.
//                    break;
//            }
//
//            DeviceHandle dev;
//            MidiDriverError ret = 0;
//
//            if (_musicMode != kMusicMode.kMusicModeDisabled)
//            {
//                dev = MidiDriver.DetectDevice(MusicType.Midi | MusicType.AdLib);
//                musicType = MidiDriver.GetMusicType(dev);
//
//                switch (musicType)
//                {
//                    case MusicType.AdLib:
//                    case MusicType.MT32:
//                        break;
//                    case MusicType.GeneralMidi:
//                        // TODO: vs
////                        if (!ConfMan.getBool("native_mt32")) {
////                            // Not a real MT32 / no MUNT
////                            ::GUI::MessageDialog dialog(("You appear to be using a General MIDI device,\n"
////                            "but your game only supports Roland MT32 MIDI.\n"
////                            "We try to map the Roland MT32 instruments to\n"
////                            "General MIDI ones. It is still possible that\n"
////                            "some tracks sound incorrect."));
////                            dialog.runModal();
////                        }
//                        // Switch to MT32 driver in any case
//                        musicType = MusicType.MT32;
//                        break;
//                    default:
//                        _musicMode = kMusicMode.kMusicModeDisabled;
//                        break;
//                }
//            }
//
//            switch (_musicMode)
//            {
//                case kMusicMode.kMusicModeAccolade:
//                {
//                    // Setup midi driver
//                    switch (musicType)
//                    {
//                        case MusicType.AdLib:
//                            _driver = MidiDriver_Accolade_AdLib_create(accoladeDriverFilename);
//                            break;
//                        case MusicType.MT32:
//                            _driver = MidiDriver_Accolade_MT32_create(accoladeDriverFilename);
//                            break;
//                    }
//                    if (_driver == null)
//                        return 255;
//
//                    ret = _driver.Open();
//                    if (ret == 0)
//                    {
//                        // Reset is done inside our MIDI driver
//                        _driver.SetTimerCallback(this, OnTimer);
//                    }
//
//                    //setTimerRate(_driver->getBaseTempo());
//                    return 0;
//                }
//
//                case kMusicMode.kMusicModeMilesAudio:
//                {
//                    switch (musicType)
//                    {
//                        case MusicType.AdLib:
//                        {
//                            Common::File instrumentDataFile;
//                            if (instrumentDataFile.exists("MIDPAK.AD"))
//                            {
//                                // if there is a file called MIDPAK.AD, use it directly
//                                Warning("SIMON 2: using MIDPAK.AD");
//                                _driver = MidiDriver_Miles_AdLib_create("MIDPAK.AD", "MIDPAK.AD");
//                            }
//                            else
//                            {
//                                // if there is no file called MIDPAK.AD, try to extract it from the file SETUP.SHR
//                                // if we didn't do this, the user would be forced to "install" the game instead of simply
//                                // copying all files from CD-ROM.
//                                var midpakAdLibStream = simon2SetupExtractFile("MIDPAK.AD");
//                                if (!midpakAdLibStream)
//                                    Error("MidiPlayer: could not extract MIDPAK.AD from SETUP.SHR");
//
//                                // Pass this extracted data to the driver
//                                Warning("SIMON 2: using MIDPAK.AD extracted from SETUP.SHR");
//                                _driver = Audio::MidiDriver_Miles_AdLib_create("", "", midpakAdLibStream);
//                                delete midpakAdLibStream;
//                            }
//                            // TODO: not sure what's going wrong with AdLib
//                            // it doesn't seem to matter if we use the regular XMIDI tracks or the 2nd set meant for MT32
//                            break;
//                        }
//                        case MusicType.MT32:
//                            _driver = Audio::MidiDriver_Miles_MT32_create("");
//                            _nativeMT32 = true; // use 2nd set of XMIDI tracks
//                            break;
//                        case MusicType.GM:
//                            if (ConfigManager.Instance.Get<bool>("native_mt32"))
//                            {
//                                _driver = Audio::MidiDriver_Miles_MT32_create("");
//                                _nativeMT32 = true; // use 2nd set of XMIDI tracks
//                            }
//                            break;
//                    }
//                    if (_driver == null)
//                        return 255;
//
//                    ret = _driver.Open();
//                    if (ret == 0)
//                    {
//                        // Reset is done inside our MIDI driver
//                        _driver.SetTimerCallback(this, OnTimer);
//                    }
//                    return 0;
//                }
//
//                case kMusicMode.kMusicModeSimon1:
//                {
//                    // This only handles the original AdLib driver of Simon1.
//                    if (musicType == MT_ADLIB)
//                    {
//                        _adLibMusic = true;
//                        _map_mt32_to_gm = false;
//                        _nativeMT32 = false;
//
//                        _driver = CreateMidiDriverSimon1AdLib("MT_FM.IBK");
//                        if (_driver != null && _driver.Open() == 0)
//                        {
//                            _driver.SetTimerCallback(this, OnTimer);
//                            // Like the original, we enable the rhythm support by default.
//                            _driver.Send(0xB0, 0x67, 0x01);
//                            return 0;
//                        }
//                        _driver = null;
//                    }
//
//                    _musicMode = kMusicMode.kMusicModeDisabled;
//                }
//            }
//
//            dev =
//                MidiDriver.DetectDevice(MusicType.AdLib | MusicType.Midi |
//                                         (gameType == SIMONGameType.GType_SIMON1 ? MDT_PREFER_MT32 : MDT_PREFER_GM));
//            _adLibMusic = (MidiDriver.GetMusicType(dev) == MusicType.AdLib);
//            _nativeMT32 = ((MidiDriver.GetMusicType(dev) == MusicType.MT32) || ConfigManager.Instance.Get<bool>("native_mt32"));
//
//            _driver = MidiDriver.CreateMidi(dev);
//            if (_driver == null)
//                return 255;
//
//            if (_nativeMT32)
//                _driver.Property(MidiDriver.PROP_CHANNEL_MASK, 0x03FE);
//
//            _map_mt32_to_gm = (gameType != SIMONGameType.GType_SIMON2 && !_nativeMT32);
//
//            ret = _driver.Open();
//            if (ret!=MidiDriverError.None)
//                return ret;
//            _driver.SetTimerCallback(this, OnTimer);
//
//            if (_nativeMT32)
//                _driver.SendMT32Reset();
//            else
//                _driver.SendGMReset();

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

        public void LoadSMF(Stream musFile, ushort soundId, bool b)
        {
            throw new System.NotImplementedException();
        }

        public void StartTrack(int i)
        {
            throw new System.NotImplementedException();
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