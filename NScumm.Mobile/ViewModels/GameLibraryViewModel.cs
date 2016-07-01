//
//  GameLibraryViewModel.cs
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

using ReactiveUI;
using Splat;
using System;
using System.Linq;
using NScumm.Core.IO;
using NScumm.Sky;
using System.Reactive.Linq;
using System.Reactive;
using NScumm.Sword1;
using System.IO;
using System.Collections.Generic;
using NScumm.Mobile.Resx;
using NScumm.Mobile.Services;
using NScumm.Scumm.IO;
using NScumm.Queen;
using NScumm.Core;
using System.Collections.ObjectModel;

namespace NScumm.Mobile.ViewModels
{
    public class GameLibraryViewModel : ReactiveObject, IRoutableViewModel
    {
        ObservableCollection<GameViewModel> _games;
        IGameService _gameService;

        public string UrlPathSegment
        {
            get { return AppResources.GameLibrary_Title; }
        }

        public IScreen HostScreen { get; protected set; }

        public IReactiveCommand<IList<GameViewModel>> Scan { get; private set; }

        public IReactiveCommand<object> Delete { get; private set; }

        public IReactiveCommand LaunchGame { get; private set; }

        public ObservableCollection<GameViewModel> Games
        {
            get { return _games; }
        }

        public GameLibraryViewModel(IScreen hostScreen = null)
        {
            _gameService = new GameService();
            HostScreen = hostScreen ?? Locator.Current.GetService<IScreen>();
            _games = new ObservableCollection<GameViewModel>();

            RegisterDefaults();

            // create commands
            Delete = ReactiveCommand.Create();
            Delete.Subscribe(DeleteImpl);

            Scan = ReactiveCommand.CreateAsyncObservable(ScanImpl);
            Scan.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler).Subscribe(e =>
            {
                this.Log().ErrorException("Scan error", e);
            });

            LaunchGame = ReactiveCommand.CreateAsyncObservable(LaunchGameImpl);
            LaunchGame.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler).Subscribe(e =>
            {
                this.Log().ErrorException("LaunchGame error", e);
            });

            Scan.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(g =>
                {
                    _games.Clear();
                    foreach (var game in g)
                    {
                        _games.Add(game);
                    }
                });
        }

        private IObservable<Unit> LaunchGameImpl(object parameter)
        {
            var game = (GameViewModel)parameter;
            _gameService.StartGame(game.Path);
            return Observable.Return(Unit.Default);
        }

        private void DeleteImpl(object parameter)
        {
            var game = (GameViewModel)parameter;
            var gameToRemove = _games.FirstOrDefault(g => g.Path == game.Path);
            if (gameToRemove == null)
                return;
            _games.Remove(gameToRemove);
        }

        private IObservable<IList<GameViewModel>> ScanImpl(object parameter)
        {
            var directory = _gameService.GetDirectory();
            var gd = CreateGameDetector();
            return Observable.Return(GetFiles(directory)
                .Select(file => gd.DetectGame(file))
                .Where(g => g != null)
                .Select(g => CreateGameViewModel(g.Game))
                                     .ToList());
        }

        private IList<string> GetFiles(string directory)
        {
            var files = new List<string>();
            ScanDirectory(directory, files);
            return files;
        }

        private void ScanDirectory(string directory, List<string> files)
        {
            try
            {
                //this.Log ().Info ($"Scan Directory {directory}");
#if __ANDROID__
                Android.Util.Log.Info("nSCUMM", $"Scan Directory {directory}");
#endif
                var entries = Directory.EnumerateFileSystemEntries(directory, "*", System.IO.SearchOption.TopDirectoryOnly);
                foreach (var entry in entries)
                {
                    if (Directory.Exists(entry))
                    {
                        ScanDirectory(entry, files);
                    }
                    else
                    {
                        this.Log().Info($"Scan {entry}");
#if __ANDROID__
                        Android.Util.Log.Info("nSCUMM", $"Scan {entry}");
#endif
                        files.Add(entry);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore exception
            }
        }

        private static void RegisterDefaults()
        {
            // Graphics
            ConfigManager.Instance.RegisterDefault("fullscreen", false);
            ConfigManager.Instance.RegisterDefault("aspect_ratio", false);
            ConfigManager.Instance.RegisterDefault("gfx_mode", "normal");
            ConfigManager.Instance.RegisterDefault("render_mode", "default");
            ConfigManager.Instance.RegisterDefault("desired_screen_aspect_ratio", "auto");

            // Sound & Music
            ConfigManager.Instance.RegisterDefault("music_volume", 192);
            ConfigManager.Instance.RegisterDefault("sfx_volume", 192);
            ConfigManager.Instance.RegisterDefault("speech_volume", 192);

            ConfigManager.Instance.RegisterDefault("music_mute", false);
            ConfigManager.Instance.RegisterDefault("sfx_mute", false);
            ConfigManager.Instance.RegisterDefault("speech_mute", false);
            ConfigManager.Instance.RegisterDefault("mute", false);

            ConfigManager.Instance.RegisterDefault("multi_midi", false);
            ConfigManager.Instance.RegisterDefault("native_mt32", false);
            ConfigManager.Instance.RegisterDefault("enable_gs", false);
            ConfigManager.Instance.RegisterDefault("midi_gain", 100);

            ConfigManager.Instance.RegisterDefault("music_driver", "auto");
            ConfigManager.Instance.RegisterDefault("mt32_device", "null");
            ConfigManager.Instance.RegisterDefault("gm_device", "null");

            ConfigManager.Instance.RegisterDefault("cdrom", 0);

            ConfigManager.Instance.RegisterDefault("enable_unsupported_game_warning", true);

            // Game specific
            ConfigManager.Instance.RegisterDefault("path", "");
            ConfigManager.Instance.RegisterDefault("platform", Core.IO.Platform.DOS);
            ConfigManager.Instance.RegisterDefault("language", "en");
            ConfigManager.Instance.RegisterDefault("subtitles", false);
            ConfigManager.Instance.RegisterDefault("boot_param", 0);
            ConfigManager.Instance.RegisterDefault("dump_scripts", false);
            ConfigManager.Instance.RegisterDefault("save_slot", -1);
            ConfigManager.Instance.RegisterDefault("autosave_period", 5 * 60); // By default, trigger autosave every 5 minutes

            ConfigManager.Instance.RegisterDefault("object_labels", true);

            ConfigManager.Instance.RegisterDefault("copy_protection", false);
            ConfigManager.Instance.RegisterDefault("talkspeed", 60);

            ConfigManager.Instance.RegisterDefault("demo_mode", false);
            ConfigManager.Instance.RegisterDefault("tempo", 0);
            ConfigManager.Instance.RegisterDefault("dimuse_tempo", 10);

            ConfigManager.Instance.RegisterDefault("alt_intro", false);

            // Miscellaneous
            ConfigManager.Instance.RegisterDefault("joystick_num", -1);
            ConfigManager.Instance.RegisterDefault("confirm_exit", false);
            ConfigManager.Instance.RegisterDefault("disable_sdl_parachute", false);

            ConfigManager.Instance.RegisterDefault("disable_display", false);
            ConfigManager.Instance.RegisterDefault("record_mode", "none");
            ConfigManager.Instance.RegisterDefault("record_file_name", "record.bin");

            ConfigManager.Instance.RegisterDefault("gui_saveload_chooser", "grid");
            ConfigManager.Instance.RegisterDefault("gui_saveload_last_pos", "0");

            ConfigManager.Instance.RegisterDefault("gui_browser_show_hidden", false);

# if USE_FLUIDSYNTH
            // The settings are deliberately stored the same way as in Qsynth. The
            // FluidSynth music driver is responsible for transforming them into
            // their appropriate values.
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_activate", true);
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_nr", 3);
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_level", 100);
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_speed", 30);
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_depth", 80);
            ConfigManager.Instance.RegisterDefault("fluidsynth_chorus_waveform", "sine");

            ConfigManager.Instance.RegisterDefault("fluidsynth_reverb_activate", true);
            ConfigManager.Instance.RegisterDefault("fluidsynth_reverb_roomsize", 20);
            ConfigManager.Instance.RegisterDefault("fluidsynth_reverb_damping", 0);
            ConfigManager.Instance.RegisterDefault("fluidsynth_reverb_width", 1);
            ConfigManager.Instance.RegisterDefault("fluidsynth_reverb_level", 90);

            ConfigManager.Instance.RegisterDefault("fluidsynth_misc_interpolation", "4th");
#endif
        }

        private static GameDetector CreateGameDetector()
        {
            var gd = new GameDetector();
            gd.Add(new SkyMetaEngine());
            gd.Add(new ScummMetaEngine());
            gd.Add(new Sword1MetaEngine());
            gd.Add(new QueenMetaEngine());
            return gd;
        }

        private static GameViewModel CreateGameViewModel(IGameDescriptor game)
        {
            return new GameViewModel(game.Description, game.Path);
        }
    }
}

