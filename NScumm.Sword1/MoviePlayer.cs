using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Video;

namespace NScumm.Sword1
{
    internal class MoviePlayer
    {
        private readonly SwordEngine _vm;
        private SmackerDecoder _decoder;

        public MoviePlayer(SwordEngine vm)
        {
            _vm = vm;
        }

        public void Load(int id)
        {
            // TODO: psx
            var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Settings.Game.Path);
            var filename = $"{SequenceList[id]}.smk";
            var path = ScummHelper.LocatePath(directory, filename);
            var stream = ServiceLocator.FileStorage.OpenFileRead(path);

            _decoder = new SmackerDecoder(_vm.Mixer);
            _decoder.LoadStream(stream);
            _decoder.Start();
        }

        public void Play()
        {
            bool skipped = false;
            ushort x = (ushort)((_vm.Settings.Game.Width - _decoder.GetWidth()) / 2);
            ushort y = (ushort)((_vm.Settings.Game.Height - _decoder.GetHeight()) / 2);

            while (!_vm.HasToQuit && !_decoder.EndOfVideo && !skipped)
            {
                if (_decoder.NeedsUpdate)
                {
                    var frame = _decoder.DecodeNextFrame();
                    if (frame != null)
                    {
                        _vm.GraphicsManager.CopyRectToScreen(frame.Pixels, frame.Pitch, x, y, frame.Width, frame.Height);
                    }

                    if (_decoder.HasDirtyPalette)
                    {
                        var palette = ToPalette(_decoder.Palette);
                        _vm.GraphicsManager.SetPalette(palette, 0, 256);
                    }

                    _vm.GraphicsManager.UpdateScreen();

                }

                int count;
                var lastState = new ScummInputState();
                do
                {
                    var state = _vm.System.InputManager.GetState();
                    count = state.GetKeys().Count;
                    if (state.IsKeyDown(KeyCode.Escape) || (lastState.IsLeftButtonDown && !state.IsLeftButtonDown))
                        skipped = true;
                    _vm.System.InputManager.ResetKeys();
                    lastState = state;
                } while (!skipped && count != 0);

                ServiceLocator.Platform.Sleep(10);
            }
        }

        private static Color[] ToPalette(byte[] palette)
        {
            var colors = new Color[256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.FromRgb(palette[i * 3], palette[i * 3 + 1], palette[i * 3 + 2]);
            }
            return colors;
        }

        private static readonly string[] SequenceList = {
            "ferrari",  // 0  CD2   ferrari running down fitz in sc19
            "ladder",   // 1  CD2   george walking down ladder to dig sc24->sc$
            "steps",    // 2  CD2   george walking down steps sc23->sc24
            "sewer",    // 3  CD1   george entering sewer sc2->sc6
            "intro",    // 4  CD1   intro sequence ->sc1
            "river",    // 5  CD1   george being thrown into river by flap & g$
            "truck",    // 6  CD2   truck arriving at bull's head sc45->sc53/4
            "grave",    // 7  BOTH  george's grave in scotland, from sc73 + from sc38 $
            "montfcon", // 8  CD2   monfaucon clue in ireland dig, sc25
            "tapestry", // 9  CD2   tapestry room beyond spain well, sc61
            "ireland",  // 10 CD2   ireland establishing shot europe_map->sc19
            "finale",   // 11 CD2   grand finale at very end, from sc73
            "history",  // 12 CD1   George's history lesson from Nico, in sc10
            "spanish",  // 13 CD2   establishing shot for 1st visit to Spain, europe_m$
            "well",     // 14 CD2   first time being lowered down well in Spai$
            "candle",   // 15 CD2   Candle burning down in Spain mausoleum sc59
            "geodrop",  // 16 CD2   from sc54, George jumping down onto truck
            "vulture",  // 17 CD2   from sc54, vultures circling George's dead body
            "enddemo",  // 18 ---   for end of single CD demo
            "credits",  // 19 CD2   credits, to follow "finale" sequence
        };
    }
}