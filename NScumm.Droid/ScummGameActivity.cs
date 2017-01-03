
using Android.App;
using Android.Views;
using Android.OS;
using NScumm.Core.IO;
using Android.Widget;
using NScumm.Agos;
using NScumm.Core;
using NScumm.Mobile.Services;

#if OUYA
using Ouya.Console.Api;
#endif

using Microsoft.Xna.Framework;
using NScumm.Queen;
using NScumm.Sci;
using NScumm.Sky;
using NScumm.Sword1;

namespace NScumm.Droid
{
	[Activity(Label = "nScumm")]
#if OUYA
	[IntentFilter(new[] { Intent.ActionMain }
		, Categories = new[] { Intent.CategoryLauncher, OuyaIntent.CategoryGame })]
#endif
	public class ScummGameActivity : AndroidGameActivity
	{
		ScummGame game;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			Initialize();

			var path = Intent.GetStringExtra("Game");
			if (path == null)
			{
				Toast.MakeText(this, "NoGameSelected", ToastLength.Short).Show();
				return;
			}

			var gd = new GameDetectorService();
			var info = gd.DetectGame(path);
			if (info == null)
			{
				Toast.MakeText(this, "Game not supported", ToastLength.Short).Show();
				return;
			}

		    var directory = info.Game.Path;
		    if (!System.IO.Directory.Exists(directory))
		    {
		        directory = System.IO.Path.GetDirectoryName(info.Game.Path);
		    }
		    Core.Common.SearchManager.Instance.AddDirectory(directory);
		    RegisterDefaults(directory);

		    ((AudioManager)ServiceLocator.AudioManager).Directory = directory;
			var settings = new GameSettings(info.Game, info.Engine)
			{
				AudioDevice = "adlib",
				CopyProtection = false
			};

			// Create our OpenGL view, and display it
			game = new ScummGame(settings);
			game.Services.AddService<IMenuService>(new MenuService(game));
			SetContentView(game.Services.GetService<View>());
			game.Run();

		}

		private void Initialize()
		{
			ServiceLocator.Platform = new Mobile.Services.Platform();
			ServiceLocator.FileStorage = new FileStorage();
			ServiceLocator.SaveFileManager = new SaveFileManager();
			ServiceLocator.AudioManager = new Mobile.Services.AudioManager();
			ServiceLocator.TraceFatory = new TraceFactory();
		}

		private static void RegisterDefaults(string path)
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
			ConfigManager.Instance.RegisterDefault("path", path);
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

#if USE_FLUIDSYNTH
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

