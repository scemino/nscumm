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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    internal enum PlanePictureCodes
    {
        // NOTE: Any value at or below 65531 means the plane
        // is a kPlaneTypePicture.
        kPlanePic = 65531,
        kPlanePicTransparentPicture = 65532,
        kPlanePicOpaque = 65533,
        kPlanePicTransparent = 65534,
        kPlanePicColored = 65535
    }

    internal enum PlaneType
    {
        Colored = 0,
        Picture = 1,
        Transparent = 2,
        Opaque = 3,
        TransparentPicture = 4
    }

    internal class PlaneList : Collection<Plane>
    {
        public Plane FindByObject(Register @object)
        {
            var plane = this.FirstOrDefault(o => Equals(o._object, @object));
            return plane;
        }

        public short GetTopPlanePriority()
        {
            if (Count > 0)
            {
                return this[Count - 1]._priority;
            }

            return 0;
        }

        public short GetTopSciPlanePriority()
        {
            short priority = 0;

            foreach (var it in this)
            {
                if (it._priority >= 10000)
                {
                    break;
                }

                priority = it._priority;
            }

            return priority;
        }
    }

    /// <summary>
    // A plane is a grouped layer of screen items.
    /// </summary>
    internal class Plane
    {

        /**
         * A serial used for planes that are generated inside
         * the graphics engine, rather than the interpreter.
         */
        private static ushort _nextObjectId;

        /**
         * For planes that are used to render picture data, the
         * resource ID of the picture to be displayed. This
         * value may also be one of the special
         * PlanePictureCodes, in which case the plane becomes a
         * non-picture plane.
         */
        private readonly PlanePictureCodes _pictureId;

        /**
         * Whether or not the contents of picture planes should
         * be drawn horizontally mirrored. Only applies to
         * planes of type Picture.
         */
        private readonly bool _mirrored;

        /**
         * Whether the picture ID for this plane has changed.
         * This flag is set when the plane is created or updated
         * from a VM object, and is cleared when the plane is
         * synchronised to another plane (which calls
         * changePic).
         */
        private bool _pictureChanged;

        /**
         * The type of the plane.
         */
        public PlaneType _type;

        /**
         * The color to use when erasing the plane. Only
         * applies to planes of type Colored.
         */
        public byte _back;

        /**
         * Whether the priority of this plane has changed.
         * This flag is set when the plane is updated from
         * another plane and cleared when draw list calculation
         * occurs.
         */
        public int _priorityChanged;

        /**
         * A handle to the VM object corresponding to this
         * plane. Some planes are generated purely within the
         * graphics engine and have a numeric object value.
         */
        public Register _object;

        /**
         * The rendering priority of the plane. Higher
         * priorities are drawn above lower priorities.
         */
        public short _priority;

        /**
         * Whether or not all screen items in this plane should
         * be redrawn on the next frameout, instead of just
         * the screen items marked as updated. This is set when
         * visual changes to the plane itself are made that
         * affect the rendering of the entire plane, and cleared
         * once those changes are rendered by `redrawAll`.
         */
        public int _redrawAllCount;

        /**
         * Flags indicating the state of the plane.
         * - `created` is set when the plane is first created,
         *   either from a VM object or from within the engine
         *   itself
         * - `updated` is set when the plane is updated from
         *   another plane and the two planes' `planeRect`s do
         *   not match
         * - `deleted` is set when the plane is deleted by a
         *   kernel call
         * - `moved` is set when the plane has been moved or
         *   resized
         */
        public int _created, _updated, _deleted, _moved;

        /**
         * The vanishing point for the plane. Used when
         * automatically calculating the correct scaling of the
         * plane's screen items according to their position.
         */
        public Point _vanishingPoint;

        /**
         * The position & dimensions of the plane in screen
         * coordinates. This rect is not clipped to the screen,
         * so may include coordinates that are offscreen.
         */
        public Rect _planeRect;

        /**
         * The position & dimensions of the plane in game script
         * coordinates.
         */
        public Rect _gameRect;

        /**
         * The position & dimensions of the plane in screen
         * coordinates. This rect is clipped to the screen.
         */
        public Rect _screenRect;

        /**
         * The list of screen items grouped within this plane.
         */
        public List<ScreenItem> _screenItemList;

        public Plane(Rect gameRect, PlanePictureCodes pictureId = PlanePictureCodes.kPlanePicColored)
        {
            _pictureId=pictureId;
            _type = PlaneType.Colored;
            _object = Register.Make(0, _nextObjectId++);
            _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _gameRect = gameRect;
            ConvertGameRectToPlaneRect();
            _priority = (short) Math.Max(10000, SciEngine.Instance._gfxFrameout.GetPlanes().GetTopPlanePriority() + 1);
            SetType();
            _screenRect = _planeRect;
        }

        public Plane(Register @object)
        {
            _type = PlaneType.Colored;
            _object = @object;
            _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            var segMan = SciEngine.Instance.EngineState._segMan;
            _vanishingPoint.X = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingX);
            _vanishingPoint.Y = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingY);

            _gameRect.Left = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.inLeft);
            _gameRect.Top = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.inTop);
            _gameRect.Right = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.inRight) + 1);
            _gameRect.Bottom = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.inBottom) + 1);
            ConvertGameRectToPlaneRect();

            _back = (byte) SciEngine.ReadSelectorValue(segMan, @object, o => o.back);
            _priority = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.priority);
            _pictureId = (PlanePictureCodes) SciEngine.ReadSelectorValue(segMan, @object, o => o.picture);
            SetType();

            _mirrored = SciEngine.ReadSelectorValue(segMan, @object, o => o.mirrored)!=0;
            _screenRect = _planeRect;
            ChangePic();
        }

        private void ChangePic()
        {
            _pictureChanged = false;

            if (_type != PlaneType.Picture && _type != PlaneType.TransparentPicture)
            {
                return;
            }

            AddPicInternal(_pictureId, new Point(), _mirrored);
        }

        private void AddPicInternal(PlanePictureCodes pictureId, Point position, bool mirrorX)
        {
            ushort celCount = 1000;
            var transparent = true;
            for (var celNo = 0; celNo < celCount; ++celNo)
            {
                var celObj = CelObjPic.Create((int) pictureId, (short) celNo);
                if (celCount == 1000)
                {
                    celCount = celObj._celCount;
                }
                if (!celObj._transparent)
                {
                    transparent = false;
                }

                var screenItem = new ScreenItem(_object, celObj._info)
                {
                    _pictureId = (int) pictureId,
                    _mirrorX = mirrorX,
                    _priority = celObj._priority,
                    _fixedPriority = true,
                    _position = position + celObj._relativePosition
                };
                _screenItemList.Add(screenItem);
                screenItem._celObj = celObj;
            }
            _type = transparent ? PlaneType.TransparentPicture : PlaneType.Picture;
        }

        private void SetType()
        {
            switch (_pictureId)
            {
                case PlanePictureCodes.kPlanePicColored:
                    _type = PlaneType.Colored;
                    break;
                case PlanePictureCodes.kPlanePicTransparent:
                    _type = PlaneType.Transparent;
                    break;
                case PlanePictureCodes.kPlanePicOpaque:
                    _type = PlaneType.Opaque;
                    break;
                case PlanePictureCodes.kPlanePicTransparentPicture:
                    _type = PlaneType.TransparentPicture;
                    break;
                default:
                    if (_type != PlaneType.TransparentPicture)
                    {
                        _type = PlaneType.Picture;
                    }
                    break;
            }
        }

        private void ConvertGameRectToPlaneRect()
        {
            var screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            var screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;
            var scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            var scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            var ratioX = new Rational(screenWidth, scriptWidth);
            var ratioY = new Rational(screenHeight, scriptHeight);

            _planeRect = _gameRect;
            Helpers.Mulru(ref _planeRect, ref ratioX, ref ratioY, 1);
        }

        public static void Init()
        {
            _nextObjectId = 20000;
        }
    }
}

#endif
