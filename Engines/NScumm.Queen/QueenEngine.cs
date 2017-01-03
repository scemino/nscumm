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

    public class QueenEngine : Engine
    {
        const int MIN_TEXT_SPEED = 4;
        public const int MAX_TEXT_SPEED = 100;

        const int SAVESTATE_CUR_VER = 1;
        const int SAVESTATE_MAX_NUM = 100;
        const int SAVESTATE_MAX_SIZE = 30000;

        const int SLOT_LISTPREFIX = -2;
        const int SLOT_AUTOSAVE = -1;
        const int SLOT_QUICKSAVE = 0;

        ISystem _system;

        int _lastUpdateTime, _lastSaveTime;

        public ISystem System { get { return _system; } }

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

        public int TalkSpeed { get; set; }

        public bool Subtitles { get; set; }

        private bool CanLoadOrSave
        {
            get
            {
                return !Input.CutawayRunning && !(Resource.IsDemo || Resource.IsInterview);
            }
        }

        public QueenEngine(GameSettings settings, ISystem system)
            : base(system,settings)
        {
            Randomizer = new Random();
            _system = system;
        }


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
            ConfigManager.Instance.Set("music_volume", Sound.Volume);
            ConfigManager.Instance.Set("music_mute", !Sound.MusicOn);
            ConfigManager.Instance.Set("sfx_mute", !Sound.SfxOn);
            ConfigManager.Instance.Set("talkspeed", ((TalkSpeed - MIN_TEXT_SPEED) * 255 + (MAX_TEXT_SPEED - MIN_TEXT_SPEED) / 2) / (MAX_TEXT_SPEED - MIN_TEXT_SPEED));
            ConfigManager.Instance.Set("speech_mute", !Sound.SpeechOn);
            ConfigManager.Instance.Set("subtitles", Subtitles);
            // TODO: ConfigManager.Instance.FlushToDisk();
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

        public override void Run()
        {
            Resource = new Resource();
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

            Sound = Sound.MakeSoundInstance(Mixer, this, Resource.Compression);

            Walk = new Walk(this);
            //_talkspeedScale = (MAX_TEXT_SPEED - MIN_TEXT_SPEED) / 255.0;

            RegisterDefaultSettings();

            // Setup mixer
            SyncSoundSettings();

            Logic.Start();

            if (ConfigManager.Instance.HasKey("save_slot") && CanLoadOrSave)
            {
                LoadGameState(ConfigManager.Instance.Get<int>("save_slot"));
            }
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

        public override void LoadGameState(int slot)
        {
            D.Debug(3, $"Loading game from slot {slot}");
            var header = new GameStateHeader();
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

        public override void SyncSoundSettings()
        {
            base.SyncSoundSettings();

            ReadOptionSettings();
        }

        private void ReadOptionSettings()
        {
            bool mute = false;
            if (ConfigManager.Instance.HasKey("mute"))
                mute = ConfigManager.Instance.Get<bool>("mute");

            Sound.Volume = ConfigManager.Instance.Get<int>("music_volume");
            Sound.MusicOn = !(mute || ConfigManager.Instance.Get<bool>("music_mute"));
            Sound.SfxOn = !(mute || ConfigManager.Instance.Get<bool>("sfx_mute"));
            Sound.SpeechOn = !(mute || ConfigManager.Instance.Get<bool>("speech_mute"));
            TalkSpeed = (ConfigManager.Instance.Get<int>("talkspeed") * (MAX_TEXT_SPEED - MIN_TEXT_SPEED) + 255 / 2) / 255 + MIN_TEXT_SPEED;
            Subtitles = ConfigManager.Instance.Get<bool>("subtitles");
            CheckOptionSettings();
        }

        public override void SaveGameState(int slot, string desc)
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

        private void RegisterDefaultSettings()
        {
            ConfigManager.Instance.RegisterDefault("talkspeed", Logic.DEFAULT_TALK_SPEED);
            ConfigManager.Instance.RegisterDefault("subtitles", true);
        }
    }
}

