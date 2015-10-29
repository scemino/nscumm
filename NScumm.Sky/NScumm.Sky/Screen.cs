using NScumm.Core.Graphics;
using System;

namespace NScumm.Sky
{
    class Screen : IDisposable
    {
        const int VGA_COLORS = 256;
        const int GAME_COLORS = 240;

        const int GAME_SCREEN_WIDTH = 320;
        const int GAME_SCREEN_HEIGHT = 192;
        
        private Color[] _palette;
        private ISystem _system;
        private Disk _skyDisk;
        private byte[] _currentScreen;

        public Screen(ISystem system, Disk disk, SkyCompact skyCompact)
        {
            _system = system;
            _skyDisk = disk;
        }

        public void Dispose()
        {
            if (_skyDisk != null)
            {
                _skyDisk.Dispose();
                _skyDisk = null;
            }
        }

        public void SetPalette(ushort fileNum)
        {
            var tmpPal = _skyDisk.LoadFile(fileNum);
            if (tmpPal != null)
            {
                SetPalette(tmpPal);
            }
            else
                throw new InvalidOperationException(string.Format("Screen::setPalette: can't load file nr. {0}", fileNum));
        }

        /// <summary>
        /// Set a new palette.
        /// </summary>
        /// <param name="pal">pal is an array to dos vga rgb components 0..63</param>
        public void SetPalette(byte[] pal)
        {
            _palette = ConvertPalette(pal);
            _system.GraphicsManager.SetPalette(_palette, 0, GAME_COLORS);
            _system.GraphicsManager.UpdateScreen();
        }

        public void ShowScreen(int fileNum)
        {
            // This is only used for static images in the floppy and cd intro
            _currentScreen = _skyDisk.LoadFile(fileNum);
            // TODO: make sure the last 8 lines are forced to black.
            //memset(_currentScreen + GAME_SCREEN_HEIGHT * GAME_SCREEN_WIDTH, 0, (FULL_SCREEN_HEIGHT - GAME_SCREEN_HEIGHT) * GAME_SCREEN_WIDTH);

            ShowScreen(_currentScreen);
        }

        private void ShowScreen(byte[] screen)
        {
            _system.GraphicsManager.CopyRectToScreen(screen, 320, 0, 0, GAME_SCREEN_WIDTH, GAME_SCREEN_HEIGHT);
            _system.GraphicsManager.UpdateScreen();
        }

        private Color[] ConvertPalette(byte[] pal)
        {
            Color[] colors = new Color[VGA_COLORS];
            for (var i = 0; i < VGA_COLORS; i++)
            {
                colors[i] = Color.FromRgb(
                    (pal[3 * i + 0] << 2) + (pal[3 * i + 0] >> 4),
                    (pal[3 * i + 1] << 2) + (pal[3 * i + 1] >> 4),
                    (pal[3 * i + 2] << 2) + (pal[3 * i + 2] >> 4));
            }
            return colors;
        }
    }
}
