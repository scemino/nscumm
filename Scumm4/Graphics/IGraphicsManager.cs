using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4.Graphics
{
    public interface IGraphicsManager
    {
        double Width { get; }
        double Height { get; }

        Point GetMousePosition();

        void UpdateScreen();
        void CopyRectToScreen(Array buf, int sourceStride, int x, int y, int width, int height);

        void SetPalette(System.Windows.Media.Color[] color);
    }
}
