//
//  Program.cs
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
using NScumm.Core;
using System.IO;
using System.Linq;
using NScumm.Core.IO;
using NScumm.Services;
using NScumm.Sci;
using NScumm.Queen;

namespace NScumm
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            var options = new ScummOptionSet();
            var extras = options.Parse(args);

            if (extras.Count != 1)
            {
                return 1;
            }

            RegisterDefaults();
            Initialize(options);
            var path = ScummHelper.NormalizePath(extras[0]);
            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("The file {0} does not exist.", path);
                Console.ResetColor();
                return 1;
            }

            //var pluginsdDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var gd = new GameDetector();
            gd.Add(new SciMetaEngine());
            gd.Add(new QueenMetaEngine());
            gd.Add(new Sky.SkyMetaEngine());
            gd.Add(new Sword1.Sword1MetaEngine());
            //gd.AddPluginsFromDirectory(pluginsdDirectory);

            var info = gd.DetectGame(path);
            if (info == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("This game is not supported, sorry please contact me if you want to support this game.");
                Console.ResetColor();
                return 1;
            }

            ((AudioManager)ServiceLocator.AudioManager).Directory = Path.GetDirectoryName(info.Game.Path);
            var settings = new GameSettings(info.Game, info.Engine)
            {
                AudioDevice = options.MusicDriver,
                CopyProtection = options.CopyProtection,
                BootParam = options.BootParam,
                Switches = options.Switches ?? string.Empty
            };

            // Set default values for all of the custom engine options
            // Appareantly some engines query them in their constructor, thus we
            // need to set this up before instance creation.
            var engineOptions = info.Engine.GetExtraGuiOptions(string.Empty);
            foreach (var engineOption in engineOptions)
            {
                ConfigManager.Instance.RegisterDefault(engineOption.ConfigOption, engineOption.DefaultState);
            }

            var game = new ScummGame(settings);
            game.Services.AddService<IMenuService>(new MenuService(game));
            game.Run();
            return 0;
        }

        private static void Initialize(ScummOptionSet options)
        {
            ServiceLocator.Platform = new Platform();
            ServiceLocator.FileStorage = new FileStorage();
            ServiceLocator.SaveFileManager = new SaveFileManager();
            ServiceLocator.AudioManager = new AudioManager();
            var switches = string.IsNullOrEmpty(options.Switches) ? Enumerable.Empty<string>() : options.Switches.Split(',');
            ServiceLocator.TraceFatory = new TraceFactory(switches);
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
    }
}