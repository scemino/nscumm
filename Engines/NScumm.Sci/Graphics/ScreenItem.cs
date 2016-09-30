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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    internal class ScreenItem
    {
        /**
	 * A descriptor for the cel object represented by the
	 * screen item.
	 */
	public CelInfo32 _celInfo;

	/**
	 * The cel object used to actually render the screen
	 * item. This member is populated by calling
	 * `getCelObj`.
	 */
        public CelObj _celObj;

	/**
	 * If set, the priority for this screen item is fixed
	 * in place. Otherwise, the priority of the screen item
	 * is calculated from its y-position + z-index.
	 */
        public bool _fixedPriority;

	/**
	 * The rendering priority of the screen item, relative
	 * only to the other screen items within the same plane.
	 * Higher priorities are drawn above lower priorities.
	 */
        public short _priority;

	/**
	 * The top-left corner of the screen item, in game
	 * script coordinates, relative to the parent plane.
	 */
        public Point _position;

	/**
	 * The associated View script object that was
	 * used to create the ScreenItem, or a numeric
	 * value in the case of a ScreenItem that was
	 * generated outside of the VM.
	 */
        public Register _object;

	/**
	 * For screen items representing picture resources,
	 * the resource ID of the picture.
	 */
        public int _pictureId;

	/**
	 * Flags indicating the state of the screen item.
	 * - `created` is set when the screen item is first
	 *   created, either from a VM object or from within the
	 *   engine itself
	 * - `updated` is set when `created` is not already set
	 *   and the screen item is updated from a VM object
	 * - `deleted` is set by the parent plane, if the parent
	 *   plane is a pic type and its picture resource ID has
	 *   changed
	 */
        public int _created, _updated, _deleted;

	/**
	 * For screen items that represent picture cels, this
	 * value is set to match the `_mirrorX` property of the
	 * parent plane and indicates that the cel should be
	 * drawn horizontally mirrored. For final drawing, it is
	 * XORed with the `_mirrorX` property of the cel object.
	 * The cel object's `_mirrorX` property comes from the
	 * resource data itself.
	 */
        public bool _mirrorX;

	/**
	 * The scaling ratios to use when drawing this screen
	 * item. These values are calculated according to the
	 * scale info whenever the screen item is updated.
	 */
        public Rational _ratioX, _ratioY;

	/**
	 * The top-left corner of the screen item, in screen
	 * coordinates.
	 */
        public Point _scaledPosition;

	/**
	 * The position & dimensions of the screen item in
	 * screen coordinates. This rect includes the offset of
	 * the parent plane and is clipped to the screen.
	 */
        public Rect _screenRect;

	/**
	 * Whether or not the screen item should be drawn
	 * with black lines drawn every second line. This is
	 * used when pixel doubling videos to improve apparent
	 * sharpness at the cost of your eyesight.
	 */
        public bool _drawBlackLines;

        public ScreenItem(Register register, object info)
        {
            throw new System.NotImplementedException();
        }

        public static void Init()
        {
            throw new System.NotImplementedException();
        }
    }
}

#endif
