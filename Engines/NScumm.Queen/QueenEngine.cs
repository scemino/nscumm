//
//  QueenEngine.cs
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
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;

namespace NScumm.Queen
{
    class QueenSystem : ISystem
    {
        public IGraphicsManager GraphicsManager { get; }

        public IInputManager InputManager { get; }

        public ISaveFileManager SaveFileManager { get; }

        public QueenSystem(IGraphicsManager graphicsManager, IInputManager inputManager, ISaveFileManager saveFileManager)
        {
            GraphicsManager = graphicsManager;
            InputManager = inputManager;
            SaveFileManager = saveFileManager;
        }
    }

    public class QueenEngine : IEngine
    {
        const int MIN_TEXT_SPEED = 4;
        const int MAX_TEXT_SPEED = 100;
            
        GameSettings _settings;
        QueenSystem _system;

        int _lastUpdateTime, _lastSaveTime;

        public Logic Logic { get; private set; }

        public Input Input { get; private set; }

        public Walk Walk { get; private set; }

        public BamScene Bam { get; private set; }

        public BankManager BankMan { get; private set; }

        public Command Command { get; private set; }

        public Display Display { get; private set; }

        public Graphics Graphics { get; private set; }

        public Grid Grid { get; private set; }

        public Resource Resource { get; private set; }

        public Random Randomizer { get; private set; }

        public bool HasToQuit { get; set; }

        public bool IsPaused { get; set; }

        public int TalkSpeed { get; set; }

        public bool Subtitles { get; private set; }


		public QueenEngine (GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager,
		                    IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode)
		{
            Randomizer = new Random();
			_settings = settings;
			_system = new QueenSystem (gfxManager, inputManager, saveFileManager);
		}

		public event EventHandler ShowMenuDialogRequested;

		public void Update (bool checkPlayerInput= false)
		{
			//_debugger.onFrame();

			Graphics.Update(Logic.CurrentRoom);
			Logic.Update();

			int frameDelay = (_lastUpdateTime + Input.DELAY_NORMAL - Environment.TickCount);
			if (frameDelay <= 0) {
				frameDelay = 1;
			}
			Input.Delay(frameDelay);
			_lastUpdateTime = Environment.TickCount;

			if (!Resource.IsInterview) {
				Display.PalCustomScroll(Logic.CurrentRoom);
			}
			BobSlot joe = Graphics.Bobs[0];
			Display.Update(joe.active, joe.x, joe.y);

			Input.CheckKeys();
//			if (Input.Debugger()) {
//				Input.DebuggerReset();
//				Debugger.Attach();
//			}
			// TODO:
//			if (CanLoadOrSave()) {
//				if (Input.QuickSave()) {
//					Input.QuickSaveReset();
//					SaveGameState(SLOT_QUICKSAVE, "Quicksave");
//				}
//				if (Input.QuickLoad()) {
//					Input.QuickLoadReset();
//					LoadGameState(SLOT_QUICKSAVE);
//				}
//				if (ShouldPerformAutoSave(_lastSaveTime)) {
//					SaveGameState(SLOT_AUTOSAVE, "Autosave");
//					_lastSaveTime = _system.getMillis();
//				}
//			}
			if (!Input.CutawayRunning) {
				if (checkPlayerInput) {
					Command.UpdatePlayer();
				}
				if (Input.IdleTime >= Input.DELAY_SCREEN_BLANKER) {
					Display.BlankScreen();
				}
			}
			// TODO:
			//Sound.updateMusic();

		}

		public void Run ()
		{
			Resource = new Resource (_settings.Game.Path);
			Bam = new BamScene (this);
			BankMan = new BankManager (Resource);
			Command = new Command (this);
			//_debugger = new Debugger(this);
			Display = new Display (this, _system);
			Graphics = new Graphics (this);
			Grid = new Grid (this);
			Input = new Input (Resource.Language, _system);

			// TODO:
//			if (Resource.IsDemo) {
//				_logic = new LogicDemo(this);
//			} else if (Resource.IsInterview) {
//				_logic = new LogicInterview(this);
//			} else {
			Logic = new LogicGame (this);
//			}

			// TODO: _sound = Sound::makeSoundInstance(_mixer, this, _resource.getCompression());

			Walk = new Walk(this);
			//_talkspeedScale = (MAX_TEXT_SPEED - MIN_TEXT_SPEED) / 255.0;

			RegisterDefaultSettings();

			// Setup mixer
            SyncSoundSettings();

			Logic.Start();

			// TODO: 
//			if (ConfMan.hasKey("save_slot") && canLoadOrSave()) {
//				loadGameState(ConfMan.getInt("save_slot"));
//			}
			_lastSaveTime = _lastUpdateTime = Environment.TickCount;

			while (!HasToQuit) {
				if (Logic.NewRoom > 0) {
					Logic.Update();
					Logic.OldRoom = Logic.CurrentRoom;
					Logic.CurrentRoom = Logic.NewRoom;
					Logic.ChangeRoom();
					Display.Fullscreen=false;
				}
			}
		}

        private void SyncSoundSettings()
        {
            // TODO: Engine::syncSoundSettings();

            ReadOptionSettings();
        }

        void ReadOptionSettings()
        {
            bool mute = false;
            // TODO:
            //if (ConfMan.hasKey("mute"))
            //    mute = ConfMan.getBool("mute");

            //Sound.setVolume(ConfMan.getInt("music_volume"));
            //Sound.musicToggle(!(mute || ConfMan.getBool("music_mute")));
            //Sound.sfxToggle(!(mute || ConfMan.getBool("sfx_mute")));
            //Sound.speechToggle(!(mute || ConfMan.getBool("speech_mute")));
            //TalkSpeed = (ConfMan.getInt("talkspeed") * (MAX_TEXT_SPEED - MIN_TEXT_SPEED) + 255 / 2) / 255 + MIN_TEXT_SPEED;
            TalkSpeed = 50;
            //Subtitles = ConfMan.getBool("subtitles");
            Subtitles = true;
            CheckOptionSettings();
        }

        void CheckOptionSettings()
        {
            ScummHelper.Clip(TalkSpeed, MIN_TEXT_SPEED, MAX_TEXT_SPEED);

            // TODO: demo and interview versions don't have speech at all
            //if (_sound.SpeechOn && (Resource.IsDemo() || Resource.IsInterview))
            //{
            //    _sound.SpeechToggle(false);
            //}

            // TODO: ensure text is always on when voice is off
            //if (!Sound.SpeechOn)
            {
                Subtitles = true;
            }
        }

        void IEngine.Load (string filename)
		{
            throw new NotImplementedException ();
		}

		void IEngine.Save (string filename)
		{
			throw new NotImplementedException ();
		}

		private void RegisterDefaultSettings() {
            // TODO:
			// ConfMan.registerDefault("talkspeed", Logic::DEFAULT_TALK_SPEED);
			// ConfMan.registerDefault("subtitles", true);
			Subtitles = true;
		}
	}
}

