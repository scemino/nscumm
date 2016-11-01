//
//  Engine.cs
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
using NScumm.Core.Audio;
using System.Diagnostics;
using D = NScumm.Core.DebugHelper;
using System.IO;
using NScumm.Core.IO;
using System.Collections.Generic;

namespace NScumm.Core
{
    public abstract class Engine : IEngine
    {
        public event EventHandler ShowMenuDialogRequested;

        public static Engine Instance;

        public static Stream OpenFileRead(string filename)
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(Instance.Settings.Game.Path);
            var path = ScummHelper.LocatePath(dir, filename);
            if (path == null) return null;
            return ServiceLocator.FileStorage.OpenFileRead(path);
        }

        public static bool FileExists(string filename)
        {
            var dir = ServiceLocator.FileStorage.GetDirectoryName(Instance.Settings.Game.Path);
            var path = ScummHelper.LocatePath(dir, filename);
            if (path == null) return false;
            return true;
        }

        public static IEnumerable<string> EnumerateFiles(string pattern)
        {
            var files = ServiceLocator.FileStorage.EnumerateFiles(
                ServiceLocator.FileStorage.GetDirectoryName(Instance.Settings.Game.Path), pattern);
            return files;
        }

        public bool IsPaused
        {
            get { return _pauseLevel != 0; }
            set
            {
                Debug.Assert((value && _pauseLevel >= 0) || (!value && _pauseLevel != 0));

                if (value)
                    _pauseLevel++;
                else
                    _pauseLevel--;

                if (_pauseLevel == 1 && value)
                {
                    _pauseStartTime = Environment.TickCount;
                    PauseEngineIntern(true);
                }
                else if (_pauseLevel == 0)
                {
                    PauseEngineIntern(false);
                    _engineStartTime += Environment.TickCount - _pauseStartTime;
                    _pauseStartTime = 0;
                }
            }
        }

        public ISystem OSystem { get; }
        public IMixer Mixer { get; }

        public int TotalPlayTime
        {
            get
            {
                if (_pauseLevel == 0)
                    return Environment.TickCount - _engineStartTime;
                return _pauseStartTime - _engineStartTime;
            }
            set
            {
                int currentTime = Environment.TickCount;

                // We need to reset the pause start time here in case the engine is already
                // paused to avoid any incorrect play time counting.
                if (_pauseLevel > 0)
                    _pauseStartTime = currentTime;

                _engineStartTime = currentTime - value;
            }
        }

        public bool HasToQuit { get; set; }
        public GameSettings Settings { get; }

        /// <summary>
        /// target name for saves
        /// </summary>
        protected string _targetName;

        /// <summary>
        /// The pause level, 0 means 'running', a positive value indicates
        /// how often the engine has been paused(and hence how often it has
        /// to be un-paused before it resumes running). This makes it possible
        /// to nest code which pauses the engine.
        /// </summary>
        private int _pauseLevel;
        /// <summary>
        /// The time when the pause was started.
        /// </summary>
        private int _pauseStartTime;
        /// <summary>
        /// The time when the engine was started. This value is used to calculate.
        /// </summary>
        private int _engineStartTime;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:NScumm.Queen.Engine"/> class.
        /// All Engine subclasses should consider overloading some or all of the following methods.
        /// </summary>
        /// <param name="system">System.</param>
        /// <param name="settings"></param>
        protected Engine(ISystem system, GameSettings settings)
        {
            Instance = this;
            OSystem = system;
            _targetName = ConfigManager.Instance.ActiveDomainName;
            _engineStartTime = Environment.TickCount;
            Settings = settings;
            Mixer = new Mixer(44100);
            // HACK:
            ((Mixer)Mixer).Read(new byte[0], 0);
            system.AudioOutput.SetSampleProvider(((Mixer)Mixer));

            // FIXME: Get rid of the following again. It is only here
            // temporarily. We really should never run with a non-working Mixer,
            // so ought to handle this at a much earlier stage. If we *really*
            // want to support systems without a working mixer, then we need
            // more work. E.g. we could modify the Mixer to immediately drop any
            // streams passed to it. This way, at least we don't crash because
            // heaps of (sound) memory get allocated but never freed. Of course,
            // there still would be problems with many games...
            if (!Mixer.IsReady)
                D.Warning("Sound initialization failed. This may cause severe problems in some games");
        }

        ~Engine()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public void QuitGame()
        {
            HasToQuit = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            Mixer.StopAll();
        }

        public virtual void LoadGameState(int slot)
        {
            // Do nothing by default
        }

        public virtual void SaveGameState(int slot, string desc)
        {
            // Do nothing by default
        }

        public abstract void Run();

        public virtual void SyncSoundSettings()
        {
            // Sync the engine with the config manager
            int soundVolumeMusic = ConfigManager.Instance.Get<int>("music_volume");
            int soundVolumeSFX = ConfigManager.Instance.Get<int>("sfx_volume");
            int soundVolumeSpeech = ConfigManager.Instance.Get<int>("speech_volume");

            bool mute = false;
            if (ConfigManager.Instance.HasKey("mute"))
                mute = ConfigManager.Instance.Get<bool>("mute");

            // We need to handle the speech mute separately here. This is because the
            // engine code should be able to rely on all speech sounds muted when the
            // user specified subtitles only mode, which results in "speech_mute" to
            // be set to "true". The global mute setting has precedence over the
            // speech mute setting though.
            bool speechMute = mute;
            if (!speechMute)
                speechMute = ConfigManager.Instance.Get<bool>("speech_mute");

            Mixer.MuteSoundType(SoundType.Plain, mute);
            Mixer.MuteSoundType(SoundType.Music, mute);
            Mixer.MuteSoundType(SoundType.SFX, mute);
            Mixer.MuteSoundType(SoundType.Speech, speechMute);

            Mixer.SetVolumeForSoundType(SoundType.Plain, Audio.Mixer.MaxMixerVolume);
            Mixer.SetVolumeForSoundType(SoundType.Music, soundVolumeMusic);
            Mixer.SetVolumeForSoundType(SoundType.SFX, soundVolumeSFX);
            Mixer.SetVolumeForSoundType(SoundType.Speech, soundVolumeSpeech);
        }

        protected void ShowMenu()
        {
            var eh = ShowMenuDialogRequested;
            eh?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void PauseEngineIntern(bool pause)
        {
            // By default, just (un)pause all digital sounds
            Mixer.PauseAll(pause);
        }

        /// <summary>
        /// Indicate whether an autosave should be performed.
        /// </summary>
        /// <returns>The perform auto save.</returns>
        /// <param name="lastSaveTime">Last save time.</param>
        protected bool ShouldPerformAutoSave(int lastSaveTime)
        {
            int diff = Environment.TickCount - lastSaveTime;
            int autosavePeriod = ConfigManager.Instance.Get<int>("autosave_period");
            return autosavePeriod != 0 && diff > autosavePeriod * 1000;
        }
    }
}
