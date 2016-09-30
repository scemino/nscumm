//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

#if ENABLE_SCI32

using System;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Sci.Graphics
{
    internal class Renderer
    {
        private readonly IMapper _mapper;
        private readonly IScaler _scaler;
        private readonly byte _skipColor;
        private readonly bool _drawBlackLines;

        public Renderer(IMapper mapper, IScaler scaler, byte skipColor, bool drawBlackLines)
        {
            _mapper = mapper;
            _scaler = scaler;
            _skipColor = skipColor;
            _drawBlackLines = drawBlackLines;
        }

        public void Draw(Buffer target, Rect targetRect)
        {
            var targetPixel = new BytePtr(target.Pixels, target.ScreenWidth * targetRect.Top + targetRect.Left);

            var skipStride = (short) (target.ScreenWidth - targetRect.Width);
            var targetWidth = (short) targetRect.Width;
            var targetHeight = (short) targetRect.Height;
            for (var y = 0; y < targetHeight; ++y)
            {
                if (_drawBlackLines && (y % 2) == 0)
                {
                    Array.Clear(targetPixel.Data, targetPixel.Offset, targetWidth);
                    targetPixel.Offset += targetWidth + skipStride;
                    continue;
                }

                _scaler.SetTarget((short) targetRect.Left, (short) (targetRect.Top + y));

                for (var x = 0; x < targetWidth; ++x)
                {
                    _mapper.Draw(targetPixel, _scaler.Read(), _skipColor);
                    targetPixel.Offset++;
                }

                targetPixel.Offset += skipStride;
            }
        }
    }
}

#endif
