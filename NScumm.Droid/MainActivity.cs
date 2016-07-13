
using Android.App;
using Android.Views;
using Android.OS;
using Android.Content.PM;
using Android.Util;
using NScumm.Core;
using NScumm.Queen;
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using System.Threading;
using System;
using Android.Widget;
using NScumm.Sky;
using NScumm.Droid.Services;

namespace NScumm.Droid
{
    // the ConfigurationChanges flags set here keep the EGL context
    // from being destroyed whenever the device is rotated or the
    // keyboard is shown (highly recommended for all GL apps)
    [Activity(Label = "NScumm.Droid",
              ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
              WindowSoftInputMode = SoftInput.AdjustResize,
              ScreenOrientation = ScreenOrientation.Landscape,
              Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
              Icon = "@mipmap/icon")]
    public class MainActivity : Activity, GestureDetector.IOnGestureListener
    {
        public const string LogTag = "nScumm";

        private EditableSurfaceView view;
        private DroidInputManager _im;
        private GestureDetector _gd;
        private IEngine _engine;
        private Thread _thread;

        public static Activity Instance { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;
            base.OnCreate(savedInstanceState);

            var path = Intent.GetStringExtra("Game");
            if (path == null)
            {
                Toast.MakeText(this, "No Game Selected", ToastLength.Short).Show();
                return;
            }

            _im = new DroidInputManager(this);
            _gd = new GestureDetector(this, this);
            //_gd.SetOnDoubleTapListener(this);
            _gd.IsLongpressEnabled = false;

            var manager = GetSystemService(ActivityService) as ActivityManager;
            if (manager.DeviceConfigurationInfo.ReqGlEsVersion >= 0x20000)
            {
                // Create our OpenGL view, and display it
                view = new EditableSurfaceView(this);
                view.Resize += delegate
                {
                    _im.width = view.Width;
                    _im.height = view.Height;
                };
                _im.View = view;

                Init(path);
                SetContentView(view);
                Run();
            }
            else
                SetContentView(Resource.Layout.Main);

            TakeKeyEvents(true);
            if (view != null)
            {
                view?.RequestFocus();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _engine.HasToQuit = true;
            try
            {
                // 1s timeout
                _thread.Join(1000);
            }
            catch (Exception e)
            {
                Log.Info(LogTag, "Error while joining nScumm thread", e);
            }
            _engine = null;
        }

        protected override void OnStart()
        {
            Log.Debug(LogTag, "OnStart");

            base.OnStart();
        }

        protected override void OnResume()
        {
            Log.Debug(LogTag, "OnResume");

            base.OnResume();

            if (_engine != null)
                _engine.IsPaused = false;
            //ShowMouseCursor(false);
        }

        protected override void OnStop()
        {
            Log.Debug(LogTag, "OnStop");

            base.OnStop();
        }

        protected override void OnPause()
        {
            Log.Debug(LogTag, "OnPause");

            base.OnPause();

            if (_engine != null)
                _engine.IsPaused = true;
        }

        private void Init(string path)
        {
            RegisterDefaults();

            ServiceLocator.FileStorage = new DroidFileStorage();
            ServiceLocator.Platform = new DroidPlatform();
            ServiceLocator.SaveFileManager = new SaveFileManager();
            ServiceLocator.AudioManager = new AudioManager();

            var gd = new GameDetector();
            gd.Add(new SkyMetaEngine());
            //gd.Add(new ScummMetaEngine());
            //gd.Add(new Sword1MetaEngine());
            gd.Add(new QueenMetaEngine());
            var game = gd.DetectGame(path);
            var settings = new GameSettings(game.Game, game.Engine)
            {
                AudioDevice = "adlib",
                CopyProtection = false
            };
            var audioDriver = new XnaAudioDriver();
            _im.Game = settings.Game;
            var size = new Rect(settings.Game.Width, settings.Game.Height);
            // TODO: audioDriver.Play();
            var system = new OSystem(new GfxManager(view, size), _im,
                                     ServiceLocator.SaveFileManager,
                                     audioDriver);
            _engine = game.Engine.Create(settings, system);
        }

        private void Run()
        {
            _thread = new Thread(new ThreadStart(UpdateGame));
            _thread.Name = "nScumm";
            _thread.Start();
        }

        private void UpdateGame()
        {
            try
            {
                _engine.Run();
            }
            catch (Exception e)
            {
                Log.Error(LogTag, $"An exception occured: {e}");
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Sorry an error occurred... :(", ToastLength.Long);
                });
            }
        }

        public override void OnBackPressed()
        {
            _im.OnKeyDown(Keycode.Back);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return _im.OnKeyDown(keyCode);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!_im.OnTouchEvent(e))
                return _gd.OnTouchEvent(e);

            return true;
        }

        public bool OnDown(MotionEvent e)
        {
            return _im.OnDown(e);
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            return true;
        }

        public void OnLongPress(MotionEvent e)
        {
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return true;
        }

        public void OnShowPress(MotionEvent e)
        {
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            return _im.OnSingleTapUp(e);
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
            ConfigManager.Instance.RegisterDefault("platform", Platform.DOS);
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


