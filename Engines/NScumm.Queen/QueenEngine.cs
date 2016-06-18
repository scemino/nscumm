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
using System.Diagnostics;
using System.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    class GameStateHeader
    {
        public uint version;
        public uint flags;
        public uint dataSize;
        public string description;
    }

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
        public const int MAX_TEXT_SPEED = 100;

        const int SAVESTATE_CUR_VER = 1;
        const int SAVESTATE_MAX_NUM = 100;
        const int SAVESTATE_MAX_SIZE = 30000;

        const int SLOT_LISTPREFIX = -2;
        const int SLOT_AUTOSAVE = -1;
        const int SLOT_QUICKSAVE = 0;

        QueenSystem _system;
        Mixer _mixer;

        int _lastUpdateTime, _lastSaveTime;

        public ISystem System { get { return _system; } }

        public GameSettings Settings { get; private set; }

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

        public Sound Sound { get; private set; }

        public bool HasToQuit { get; set; }

        public bool IsPaused { get; set; }

        public int TalkSpeed { get; set; }

        public bool Subtitles { get; set; }

        public IMixer Mixer { get { return _mixer; } }

        private bool CanLoadOrSave
        {
            get
            {
                return !Input.CutawayRunning && !(Resource.IsDemo || Resource.IsInterview);
            }
        }

        public QueenEngine(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager,
                            IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode)
        {
            Randomizer = new Random();
            Settings = settings;
            _system = new QueenSystem(gfxManager, inputManager, saveFileManager);
            _mixer = new Mixer(44100);
            _mixer.Read(new short[0], 0);
            output.SetSampleProvider(_mixer);
        }

        public event EventHandler ShowMenuDialogRequested;

        public void FindGameStateDescriptions(string[] descriptions)
        {
            var prefix = MakeGameStateName(SLOT_LISTPREFIX);
            string[] filenames = _system.SaveFileManager.ListSavefiles(prefix);
            foreach (var filename in filenames)
            {
                int i = GetGameStateSlot(filename);
                if (i >= 0 && i < SAVESTATE_MAX_NUM)
                {
                    var header = new GameStateHeader();
                    using (var f = ReadGameStateHeader(i, header))
                    {
                        descriptions[i] = header.description;
                    }
                }
            }
        }

        private string MakeGameStateName(int slot)
        {
            string buf;
            if (slot == SLOT_LISTPREFIX)
            {
                buf = "queen.s??";
            }
            else if (slot == SLOT_AUTOSAVE)
            {
                buf = "queen.asd";
            }
            else
            {
                Debug.Assert(slot >= 0);
                buf = $"queen.s{slot:D2}";
            }
            return buf;
        }

        private int GetGameStateSlot(string filename)
        {
            if (filename == null) return -1;
            int i = -1;
            var dot = filename.IndexOf('.');
            if (dot != -1 && (filename[dot + 1] == 's' || filename[dot + 1] == 'S'))
            {
                i = int.Parse(filename.Substring(dot + 2, 2));
            }
            return i;
        }

        private Stream ReadGameStateHeader(int slot, GameStateHeader gsh)
        {
            var name = MakeGameStateName(slot);
            var file = _system.SaveFileManager.OpenForLoading(name);
            var br = new BinaryReader(file);
            if (file != null && br.ReadUInt32BigEndian() == ScummHelper.MakeTag('S', 'C', 'V', 'M'))
            {
                gsh.version = br.ReadUInt32BigEndian();
                gsh.flags = br.ReadUInt32BigEndian();
                gsh.dataSize = br.ReadUInt32BigEndian();
                gsh.description = ScummHelper.GetText(br.ReadBytes(32));
            }
            return file;
        }

        public void WriteOptionSettings()
        {
            // TODO: conf
            //ConfMan.setInt("music_volume", _sound.getVolume());
            //ConfMan.setBool("music_mute", !_sound.musicOn());
            //ConfMan.setBool("sfx_mute", !_sound.sfxOn());
            //ConfMan.setInt("talkspeed", ((_talkSpeed - MIN_TEXT_SPEED) * 255 + (MAX_TEXT_SPEED - MIN_TEXT_SPEED) / 2) / (MAX_TEXT_SPEED - MIN_TEXT_SPEED));
            //ConfMan.setBool("speech_mute", !_sound.speechOn());
            //ConfMan.setBool("subtitles", _subtitles);
            //ConfMan.flushToDisk();
        }

        public void Update(bool checkPlayerInput = false)
        {
            //_debugger.onFrame();

            Graphics.Update(Logic.CurrentRoom);
            Logic.Update();

            int frameDelay = (_lastUpdateTime + Input.DELAY_NORMAL - Environment.TickCount);
            if (frameDelay <= 0)
            {
                frameDelay = 1;
            }
            Input.Delay(frameDelay);
            _lastUpdateTime = Environment.TickCount;

            if (!Resource.IsInterview)
            {
                Display.PalCustomScroll(Logic.CurrentRoom);
            }
            BobSlot joe = Graphics.Bobs[0];
            Display.Update(joe.active, joe.x, joe.y);

            Input.CheckKeys();
            //			if (Input.Debugger()) {
            //				Input.DebuggerReset();
            //				Debugger.Attach();
            //			}
            if (CanLoadOrSave)
            {
                if (Input.QuickSave)
                {
                    Input.QuickSaveReset();
                    SaveGameState(SLOT_QUICKSAVE, "Quicksave");
                }
                if (Input.QuickLoad)
                {
                    Input.QuickLoadReset();
                    LoadGameState(SLOT_QUICKSAVE);
                }
                if (ShouldPerformAutoSave(_lastSaveTime))
                {
                    SaveGameState(SLOT_AUTOSAVE, "Autosave");
                    _lastSaveTime = Environment.TickCount;
                }
            }
            if (!Input.CutawayRunning)
            {
                if (checkPlayerInput)
                {
                    Command.UpdatePlayer();
                }
                if (Input.IdleTime >= Input.DELAY_SCREEN_BLANKER)
                {
                    Display.BlankScreen();
                }
            }
            Sound.UpdateMusic();

        }

        public void Run()
        {
            Resource = new Resource(Settings.Game.Path);
            Bam = new BamScene(this);
            BankMan = new BankManager(Resource);
            Command = new Command(this);
            //_debugger = new Debugger(this);
            Display = new Display(this, _system);
            Graphics = new Graphics(this);
            Grid = new Grid(this);
            Input = new Input(Resource.Language, _system);

            if (Resource.IsDemo)
            {
                Logic = new LogicDemo(this);
            }
            else if (Resource.IsInterview)
            {
                Logic = new LogicInterview(this);
            }
            else
            {
                Logic = new LogicGame(this);
            }

            Sound = Sound.MakeSoundInstance(_mixer, this, Resource.Compression);

            Walk = new Walk(this);
            //_talkspeedScale = (MAX_TEXT_SPEED - MIN_TEXT_SPEED) / 255.0;

            RegisterDefaultSettings();

            // Setup mixer
            SyncSoundSettings();

            Logic.Start();

            // TODO: save
            //			if (ConfMan.hasKey("save_slot") && canLoadOrSave()) {
            //				loadGameState(ConfMan.getInt("save_slot"));
            //			}
            _lastSaveTime = _lastUpdateTime = Environment.TickCount;

            while (!HasToQuit)
            {
                if (Logic.NewRoom > 0)
                {
                    Logic.Update();
                    Logic.OldRoom = Logic.CurrentRoom;
                    Logic.CurrentRoom = Logic.NewRoom;
                    Logic.ChangeRoom();
                    Display.Fullscreen = false;
                    if (Logic.CurrentRoom == Logic.NewRoom)
                    {
                        Logic.NewRoom = 0;
                    }
                }
                else if (Logic.JoeWalk == JoeWalkMode.EXECUTE)
                {
                    Logic.JoeWalk = JoeWalkMode.NORMAL;
                    Command.ExecuteCurrentAction();
                }
                else
                {
                    Logic.JoeWalk = JoeWalkMode.NORMAL;
                    Update(true);
                }
            }
        }

        public void LoadGameState(int slot)
        {
            D.Debug(3, $"Loading game from slot {slot}");
            GameStateHeader header = new GameStateHeader();
            using (var file = ReadGameStateHeader(slot, header))
            {
                var br = new BinaryReader(file);
                if (file != null && header.dataSize != 0)
                {
                    byte[] saveData = br.ReadBytes((int)header.dataSize);
                    int p = 0;
                    Bam.LoadState(header.version, saveData, ref p);
                    Grid.LoadState(header.version, saveData, ref p);
                    Logic.LoadState(header.version, saveData, ref p);
                    Sound.LoadState(header.version, saveData, ref p);
                    if (header.dataSize != p)
                    {
                        D.Warning("Corrupted savegame file");
                    }
                    else
                    {
                        Logic.SetupRestoredGame();
                    }
                }
            }
        }

        private void SyncSoundSettings()
        {
            // TODO: Engine::syncSoundSettings();

            ReadOptionSettings();
        }

        private void ReadOptionSettings()
        {
            bool mute = false;
            // TODO: conf
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

        internal void QuitGame()
        {
            throw new NotImplementedException();
        }

        public void SaveGameState(int slot, string desc)
        {
            D.Debug(3, $"Saving game to slot {slot}");
            var name = MakeGameStateName(slot);
            using (var file = System.SaveFileManager.OpenForSaving(name))
            {
                var bw = new BinaryWriter(file);
                // save data
                byte[] saveData = new byte[SAVESTATE_MAX_SIZE];
                int p = 0;
                Bam.SaveState(saveData, ref p);
                Grid.SaveState(saveData, ref p);
                Logic.SaveState(saveData, ref p);
                Sound.SaveState(saveData, ref p);
                uint dataSize = (uint)p;
                Debug.Assert(dataSize < SAVESTATE_MAX_SIZE);

                // write header
                bw.WriteUInt32BigEndian(ScummHelper.MakeTag('S', 'C', 'V', 'M'));
                bw.WriteUInt32BigEndian(SAVESTATE_CUR_VER);
                bw.WriteUInt32BigEndian(0);
                bw.WriteUInt32BigEndian(dataSize);
                byte[] d = new byte[32];
                var descBytes = global::System.Text.Encoding.UTF8.GetBytes(desc);
                Array.Copy(descBytes, d, Math.Min(32, descBytes.Length));
                bw.WriteBytes(d, d.Length);

                //bwwrite save data
                bw.WriteBytes(saveData, (int)dataSize);
            }
        }

        public void CheckOptionSettings()
        {
            ScummHelper.Clip(TalkSpeed, MIN_TEXT_SPEED, MAX_TEXT_SPEED);

            // demo and interview versions don't have speech at all
            if (Sound.SpeechOn && (Resource.IsDemo || Resource.IsInterview))
            {
                Sound.SpeechOn = false;
            }

            // ensure text is always on when voice is off
            if (!Sound.SpeechOn)
            {
                Subtitles = true;
            }
        }

        void IEngine.Load(string filename)
        {
            throw new NotImplementedException();
        }

        void IEngine.Save(string filename)
        {
            throw new NotImplementedException();
        }

        private void RegisterDefaultSettings()
        {
            // TODO: conf
            // ConfMan.registerDefault("talkspeed", Logic::DEFAULT_TALK_SPEED);
            // ConfMan.registerDefault("subtitles", true);
            Subtitles = true;
        }

        // TODO: move this in Engine base class
        private bool ShouldPerformAutoSave(int lastSaveTime)
        {
            int diff = Environment.TickCount - lastSaveTime;
            // TODO: conf
            //int autosavePeriod = ConfMan.getInt("autosave_period");
            int autosavePeriod = 5 * 60;
            return autosavePeriod != 0 && diff > autosavePeriod * 1000;
        }
    }
}

