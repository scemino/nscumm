using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public struct Rect
    {
        public int top, left;
        public int bottom, right;

        public int height()
        {
            return bottom - top;
        }

        public int width()
        {
            return right - left;
        }

        public void Clip(int maxw, int maxh)
        {
            Clip(new Rect(0, 0, maxw, maxh));
        }

        public void Clip(Rect r)
        {
            if (top < r.top) top = r.top;
            else if (top > r.bottom) top = r.bottom;

            if (left < r.left) left = r.left;
            else if (left > r.right) left = r.right;

            if (bottom > r.bottom) bottom = r.bottom;
            else if (bottom < r.top) bottom = r.top;

            if (right > r.right) right = r.right;
            else if (right < r.left) right = r.left;
        }

        public Rect(int x1, int y1, int x2, int y2)
        {
            top = y1;
            left = x1;
            bottom = y2;
            right = x2;
        }
    }
}
