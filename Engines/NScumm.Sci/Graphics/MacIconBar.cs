//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    internal class GfxMacIconBar
    {
        internal Register HandleEvents()
        {
            throw new NotImplementedException();
        }

        public void AddIcon(Register obj)
        {
            throw new NotImplementedException();
            //IconBarItem item;
            //uint32 iconIndex = readSelectorValue(g_sci->getEngineState()->_segMan, obj, SELECTOR(iconIndex));

            //item.object = obj;
            //item.nonSelectedImage = createImage(iconIndex, false);

            //if (iconIndex != _inventoryIndex)
            //    item.selectedImage = createImage(iconIndex, true);
            //else
            //    item.selectedImage = 0;

            //item.enabled = true;

            //// Start after the main viewing window and add a two pixel buffer
            //uint16 y = g_sci->_gfxScreen->getHeight() + 2;

            //if (item.nonSelectedImage)
            //    item.rect = Common::Rect(_lastX, y, MIN<uint32>(_lastX + item.nonSelectedImage->w, 320), y + item.nonSelectedImage->h);
            //else
            //    error("Could not find a non-selected image for icon %d", iconIndex);

            //_lastX += item.rect.width();

            //_iconBarItems.push_back(item);
        }

        internal void SetIconEnabled(short v1, bool v2)
        {
            throw new NotImplementedException();
        }

        internal void SetInventoryIcon(short v)
        {
            throw new NotImplementedException();
        }

        internal void DrawIcons()
        {
            throw new NotImplementedException();
        }
    }
}
