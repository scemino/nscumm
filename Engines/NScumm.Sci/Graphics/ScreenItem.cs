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
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal class ScreenItem : IComparable<ScreenItem>
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

        /**
         * A serial used for screen items that are generated
         * inside the graphics engine, rather than the
         * interpreter.
         */
        private static ushort _nextObjectId;

        /**
         * The parent plane of this screen item.
         */
        public Register _plane;

        /**
         * Scaling data used to calculate the final screen
         * dimensions of the screen item as well as the scaling
         * ratios used when drawing the item to screen.
         */
        public readonly ScaleInfo _scale;

        /**
         * The position & dimensions of the screen item in
         * screen coordinates. This rect includes the offset
         * of the parent plane, but is not clipped to the
         * screen, so may include coordinates that are
         * offscreen.
         */
        Rect _screenItemRect;

        /**
         * If true, the `_insetRect` rectangle will be used
         * when calculating the dimensions of the screen item
         * instead of the cel's intrinsic width and height.
         *
         * In other words, using an inset rect means that
         * the cel is cropped to the dimensions given in
         * `_insetRect`.
         */
        bool _useInsetRect;

        /**
         * The cropping rectangle used when `_useInsetRect`
         * is true.
         *
         * `_insetRect` is also used to describe the fill
         * rectangle of a screen item with a CelObjColor
         * cel.
         */
        Rect _insetRect;

        /**
         * The z-index of the screen item in pseudo-3D space.
         * Higher values are drawn on top of lower values.
         */
        int _z;

        public ScreenItem(Register @object)
        {
            _object = @object;
            _pictureId = -1;
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            var segMan = SciEngine.Instance.EngineState._segMan;

            _scale = new ScaleInfo();
            _celInfo = new CelInfo32();
            SetFromObject(segMan, @object, true, true);
            _plane = SciEngine.ReadSelector(segMan, @object, o => o.plane);
        }

        public ScreenItem(ScreenItem other)
        {
            _plane = other._plane;
            _scale = other._scale;
            _celInfo = other._celInfo;
            _celObj = other._celObj;
            _position = other._position;
            _object = other._object;
            _pictureId = other._pictureId;
            _created = other._created;
            _deleted = other._deleted;
            _drawBlackLines = other._drawBlackLines;
            _fixedPriority = other._fixedPriority;
            _insetRect = other._insetRect;
            _mirrorX = other._mirrorX;
            _priority = other._priority;
            _ratioX = other._ratioX;
            _ratioY = other._ratioY;
            _scaledPosition = other._scaledPosition;
            _screenItemRect = other._screenItemRect;
            _screenRect = other._screenRect;
            _updated = other._updated;
            _useInsetRect = other._useInsetRect;
            _z = other._z;
        }

        public ScreenItem(Register plane, CelInfo32 celInfo, Rect rect)
        {
            _plane = plane;
            _scale = new ScaleInfo();
            _celInfo = celInfo;
            _position = new Point(rect.Left, rect.Top);
            _object = Register.Make(0, _nextObjectId++);
            _pictureId = -1;
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            if (celInfo.type == CelType.Color)
            {
                _insetRect = rect;
            }
        }

        public ScreenItem(Register plane, CelInfo32 celInfo, Point position, ScaleInfo scaleInfo)
        {
            _plane = plane;
            _scale = scaleInfo;
            _celInfo = celInfo;
            _position = position;
            _object = Register.Make(0, _nextObjectId++);
            _pictureId = -1;
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
        }

        public ScreenItem(Register plane, CelInfo32 celInfo)
        {
            _plane = plane;
            _scale = new ScaleInfo();
            _celInfo = celInfo;
            _object = Register.Make(0, _nextObjectId++);
            _pictureId = -1;
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
        }

        public static void Init()
        {
            _nextObjectId = 20000;
        }

        public void Update()
        {
            Plane plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(_plane);
            if (plane == null)
            {
                Error("ScreenItem::update: Invalid plane {0}", _plane);
            }

            if (plane._screenItemList.FindByObject(_object) == null)
            {
                Error("ScreenItem::update: {0} not in plane {1}", _object, _plane);
            }

            if (_created == 0)
            {
                _updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
            }
            _deleted = 0;

            _celObj = null;
        }

        public void Update(Register @object)
        {
            SegManager segMan = SciEngine.Instance.EngineState._segMan;

            int view = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.view);
            short loopNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.loop);
            short celNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.cel);

            bool updateCel =
                    _celInfo.resourceId != view ||
                    _celInfo.loopNo != loopNo ||
                    _celInfo.celNo != celNo
                ;

            bool updateBitmap = !SciEngine.ReadSelector(segMan, @object, o => o.bitmap).IsNull;

            SetFromObject(segMan, @object, updateCel, updateBitmap);

            if (_created == 0)
            {
                _updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
            }

            _deleted = 0;
        }

        private void SetFromObject(SegManager segMan, Register @object, bool updateCel, bool updateBitmap)
        {
            _position.X = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.x);
            _position.Y = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.y);
            _scale.x = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.scaleX);
            _scale.y = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.scaleY);
            _scale.max = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.maxScale);
            _scale.signal = (ScaleSignals32) (SciEngine.ReadSelectorValue(segMan, @object, o => o.scaleSignal) & 3);

            if (updateCel)
            {
                _celInfo.resourceId = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.view);
                _celInfo.loopNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.loop);
                _celInfo.celNo = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.cel);

                if (_celInfo.resourceId <= (int) PlanePictureCodes.kPlanePic)
                {
                    // TODO: Enhance GfxView or ResourceManager to allow
                    // metadata for resources to be retrieved once, from a
                    // single location
                    ResourceManager.ResourceSource.Resource view =
                        SciEngine.Instance.ResMan.FindResource(
                            new ResourceId(ResourceType.View, (ushort) _celInfo.resourceId), false);
                    if (view == null)
                    {
                        Error("Failed to load resource {0}", _celInfo.resourceId);
                    }

                    // NOTE: +2 because the header size field itself is excluded from
                    // the header size in the data
                    ushort headerSize = (ushort) (view.data.ReadSci11EndianUInt16() + 2);
                    byte loopCount = view.data[2];
                    byte loopSize = view.data[12];

                    if (_celInfo.loopNo >= loopCount)
                    {
                        int maxLoopNo = loopCount - 1;
                        _celInfo.loopNo = (short) maxLoopNo;
                        SciEngine.WriteSelectorValue(segMan, @object, o => o.loop, (ushort) maxLoopNo);
                    }

                    var loopData = new BytePtr(view.data, headerSize + _celInfo.loopNo * loopSize);
                    sbyte seekEntry = (sbyte) loopData[0];
                    if (seekEntry != -1)
                    {
                        loopData = new BytePtr(view.data, headerSize + seekEntry * loopSize);
                    }
                    byte celCount = loopData[2];
                    if (_celInfo.celNo >= celCount)
                    {
                        int maxCelNo = celCount - 1;
                        _celInfo.celNo = (short) maxCelNo;
                        SciEngine.WriteSelectorValue(segMan, @object, o => o.cel, (ushort) maxCelNo);
                    }
                }
            }

            if (updateBitmap)
            {
                Register bitmap = SciEngine.ReadSelector(segMan, @object, o => o.bitmap);
                if (!bitmap.IsNull)
                {
                    _celInfo.bitmap = bitmap;
                    _celInfo.type = CelType.Mem;
                }
                else
                {
                    _celInfo.bitmap = Register.NULL_REG;
                    _celInfo.type = CelType.View;
                }
            }

            if (updateCel || updateBitmap)
            {
                _celObj = null;
            }

            if (SciEngine.ReadSelectorValue(segMan, @object, o => o.fixPriority) != 0)
            {
                _fixedPriority = true;
                _priority = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.priority);
            }
            else
            {
                _fixedPriority = false;
                SciEngine.WriteSelectorValue(segMan, @object, o => o.priority, (ushort) _position.Y);
            }

            _z = (int) SciEngine.ReadSelectorValue(segMan, @object, o => o.z);
            _position.Y = (short) (_position.Y - _z);

            if (SciEngine.ReadSelectorValue(segMan, @object, o => o.useInsetRect) != 0)
            {
                _useInsetRect = true;
                _insetRect.Left = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.inLeft);
                _insetRect.Top = (short) SciEngine.ReadSelectorValue(segMan, @object, o => o.inTop);
                _insetRect.Right = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.inRight) + 1);
                _insetRect.Bottom = (short) (SciEngine.ReadSelectorValue(segMan, @object, o => o.inBottom) + 1);
            }
            else
            {
                _useInsetRect = false;
            }

            segMan.GetObject(@object).ClearInfoSelectorFlag(SciObject.InfoFlagViewVisible);
        }

        public int CompareTo(ScreenItem other)
        {
            var order = _priority.CompareTo(other._priority);
            if (order != 0)
            {
                return order;
            }

            order = (_position.Y + _z).CompareTo((short) (other._position.Y + other._z));
            if (order != 0)
            {
                return order;
            }

            return _object.CompareTo(other._object);
        }

        public CelObj GetCelObj()
        {
            if (_celObj != null) return _celObj;

            switch (_celInfo.type)
            {
                case CelType.View:
                    _celObj = CelObjView.Create(_celInfo.resourceId, _celInfo.loopNo, _celInfo.celNo);
                    break;
                case CelType.Pic:
                    Error("Internal error, pic screen item with no cel.");
                    break;
                case CelType.Mem:
                    _celObj = new CelObjMem(_celInfo.bitmap);
                    break;
                case CelType.Color:
                    _celObj = new CelObjColor(_celInfo.color, _insetRect.Width, _insetRect.Height);
                    break;
            }

            return _celObj;
        }

        public void CalcRects(Plane plane)
        {
            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            short screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            short screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;

            CelObj celObj = GetCelObj();

            Rect celRect = new Rect((short) celObj._width, (short) celObj._height);
            if (_useInsetRect)
            {
                if (_insetRect.Intersects(celRect))
                {
                    _insetRect.Clip(celRect);
                }
                else
                {
                    _insetRect = new Rect();
                }
            }
            else
            {
                _insetRect = celRect;
            }

            Rational scaleX = new Rational();
            Rational scaleY = null;

            if ((_scale.signal & ScaleSignals32.kScaleSignalManual) != ScaleSignals32.kScaleSignalNone)
            {
                if ((_scale.signal & ScaleSignals32.kScaleSignalVanishingPoint) != ScaleSignals32.kScaleSignalNone)
                {
                    int num = _scale.max * (_position.Y - plane._vanishingPoint.Y) /
                              (scriptWidth - plane._vanishingPoint.Y);
                    scaleX = new Rational(num, 128);
                    scaleY = new Rational(num, 128);
                }
                else
                {
                    scaleX = new Rational(_scale.x, 128);
                    scaleY = new Rational(_scale.y, 128);
                }
            }

            if (scaleX.Numerator != 0 && scaleY.Numerator != 0)
            {
                _screenItemRect = _insetRect;

                Rational celToScreenX = new Rational(screenWidth, celObj._xResolution);
                Rational celToScreenY = new Rational(screenHeight, celObj._yResolution);

                // Cel may use a coordinate system that is not the same size as the
                // script coordinate system (usually this means high-resolution
                // pictures with low-resolution scripts)
                if (celObj._xResolution != LowRes.X || celObj._yResolution != LowRes.Y)
                {
                    // high resolution coordinates

                    if (_useInsetRect)
                    {
                        Rational scriptToCelX = new Rational(celObj._xResolution, scriptWidth);
                        Rational scriptToCelY = new Rational(celObj._yResolution, scriptHeight);
                        Helpers.Mulru(ref _screenItemRect, ref scriptToCelX, ref scriptToCelY, 0);

                        if (_screenItemRect.Intersects(celRect))
                        {
                            _screenItemRect.Clip(celRect);
                        }
                        else
                        {
                            _screenItemRect = new Rect();
                        }
                    }

                    int displaceX = celObj._displace.X;
                    int displaceY = celObj._displace.Y;

                    if (_mirrorX != celObj._mirrorX && _celInfo.type != CelType.Pic)
                    {
                        displaceX = celObj._width - celObj._displace.X - 1;
                    }

                    if (!scaleX.IsOne || !scaleY.IsOne)
                    {
                        // Different games use a different cel scaling mode, but the
                        // difference isn't consistent across SCI versions; instead,
                        // it seems to be related to an update that happened during
                        // SCI2.1mid where games started using hi-resolution game
                        // scripts
                        if (scriptWidth == LowRes.X)
                        {
                            Helpers.Mulinc(ref _screenItemRect, scaleX, scaleY);
                        }
                        else
                        {
                            _screenItemRect.Left = (short) (_screenItemRect.Left * scaleX);
                            _screenItemRect.Top = (short) (_screenItemRect.Top * scaleY);

                            if (scaleX.Numerator > scaleX.Denominator)
                            {
                                _screenItemRect.Right = (short) (_screenItemRect.Right * scaleX);
                            }
                            else
                            {
                                _screenItemRect.Right = (short) ((_screenItemRect.Right - 1) * scaleX + 1);
                            }

                            if (scaleY.Numerator > scaleY.Denominator)
                            {
                                _screenItemRect.Bottom = (short) (_screenItemRect.Bottom * scaleY);
                            }
                            else
                            {
                                _screenItemRect.Bottom = (short) (((_screenItemRect.Bottom - 1) * scaleY) + 1);
                            }
                        }

                        displaceX = (displaceX * scaleX);
                        displaceY = (displaceY * scaleY);
                    }

                    Helpers.Mulinc(ref _screenItemRect, celToScreenX, celToScreenY);
                    displaceX = (displaceX * celToScreenX);
                    displaceY = (displaceY * celToScreenY);

                    var scriptToScreenX = new Rational(screenWidth, scriptWidth);
                    var scriptToScreenY = new Rational(screenHeight, scriptHeight);

                    if ( /* TODO: dword_C6288 */ false && _celInfo.type == CelType.Pic)
                    {
                        _scaledPosition.X = _position.X;
                        _scaledPosition.Y = _position.Y;
                    }
                    else
                    {
                        _scaledPosition.X = (short) ((_position.X * scriptToScreenX) - displaceX);
                        _scaledPosition.Y = (short) ((_position.Y * scriptToScreenY) - displaceY);
                    }

                    _screenItemRect.Translate(_scaledPosition.X, _scaledPosition.Y);

                    if (_mirrorX != celObj._mirrorX && _celInfo.type == CelType.Pic)
                    {
                        Rect temp = new Rect(_insetRect);

                        if (!scaleX.IsOne)
                        {
                            Helpers.Mulinc(ref temp, scaleX, new Rational());
                        }

                        Helpers.Mulinc(ref temp, celToScreenX, new Rational());

                        CelObjPic celObjPic = (CelObjPic) _celObj;
                        if (celObjPic == null)
                        {
                            Error("Expected a CelObjPic");
                        }
                        temp.Translate((short) ((celObjPic._relativePosition.X * scriptToScreenX) - displaceX), 0);

                        // TODO: This is weird.
                        int deltaX = plane._planeRect.Width - temp.Right - 1 - temp.Left;

                        _scaledPosition.X = (short) (_scaledPosition.X + deltaX);
                        _screenItemRect.Translate((short) deltaX, 0);
                    }

                    _scaledPosition.X += plane._planeRect.Left;
                    _scaledPosition.Y += plane._planeRect.Top;
                    _screenItemRect.Translate(plane._planeRect.Left, plane._planeRect.Top);

                    _ratioX = scaleX * celToScreenX;
                    _ratioY = scaleY * celToScreenY;
                }
                else
                {
                    // low resolution coordinates

                    int displaceX = celObj._displace.X;
                    if (_mirrorX != celObj._mirrorX && _celInfo.type != CelType.Pic)
                    {
                        displaceX = celObj._width - celObj._displace.X - 1;
                    }

                    if (!scaleX.IsOne || !scaleY.IsOne)
                    {
                        Helpers.Mulinc(ref _screenItemRect, scaleX, scaleY);
                        // TODO: This was in the original code, baked into the
                        // multiplication though it is not immediately clear
                        // why this is the only one that reduces the BR corner
                        _screenItemRect.Right -= 1;
                        _screenItemRect.Bottom -= 1;
                    }

                    _scaledPosition.X = (short) (_position.X - displaceX * scaleX);
                    _scaledPosition.Y = (short) (_position.Y - (celObj._displace.Y * scaleY));
                    _screenItemRect.Translate(_scaledPosition.X, _scaledPosition.Y);

                    if (_mirrorX != celObj._mirrorX && _celInfo.type == CelType.Pic)
                    {
                        Rect temp = new Rect(_insetRect);

                        if (!scaleX.IsOne)
                        {
                            Helpers.Mulinc(ref temp, scaleX, new Rational());
                            temp.Right -= 1;
                        }

                        CelObjPic celObjPic = (CelObjPic) _celObj;
                        if (celObjPic == null)
                        {
                            Error("Expected a CelObjPic");
                        }
                        temp.Translate((short) (celObjPic._relativePosition.X - (displaceX * scaleX)),
                            (short) (celObjPic._relativePosition.Y - (celObj._displace.Y * scaleY)));

                        // TODO: This is weird.
                        int deltaX = plane._gameRect.Width - temp.Right - 1 - temp.Left;

                        _scaledPosition.X = (short) (_scaledPosition.X + deltaX);
                        _screenItemRect.Translate((short) deltaX, 0);
                    }

                    _scaledPosition.X += plane._gameRect.Left;
                    _scaledPosition.Y += plane._gameRect.Top;
                    _screenItemRect.Translate(plane._gameRect.Left, plane._gameRect.Top);

                    if (celObj._xResolution != screenWidth || celObj._yResolution != screenHeight)
                    {
                        Helpers.Mulru(ref _scaledPosition, ref celToScreenX, ref celToScreenY);
                        Helpers.Mulru(ref _screenItemRect, ref celToScreenX, ref celToScreenY, 1);
                    }

                    _ratioX = scaleX * celToScreenX;
                    _ratioY = scaleY * celToScreenY;
                }

                _screenRect = _screenItemRect;

                if (_screenRect.Intersects(plane._screenRect))
                {
                    _screenRect.Clip(plane._screenRect);
                }
                else
                {
                    _screenRect = new Rect();
                }

                if (!_fixedPriority)
                {
                    _priority = (short) (_z + _position.Y);
                }
            }
            else
            {
                _screenRect = new Rect();
            }
        }

        public Rect GetNowSeenRect(Plane plane)
        {
            CelObj celObj = GetCelObj();

            Rect celObjRect = new Rect((short) celObj._width, (short) celObj._height);
            Rect nsRect;

            if (_useInsetRect)
            {
                if (_insetRect.Intersects(celObjRect))
                {
                    nsRect = _insetRect;
                    nsRect.Clip(celObjRect);
                }
                else
                {
                    nsRect = new Rect();
                }
            }
            else
            {
                nsRect = celObjRect;
            }

            ushort scriptWidth = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            ushort scriptHeight = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            Rational scaleX = new Rational();
            Rational scaleY = new Rational();
            if (_scale.signal == ScaleSignals32.kScaleSignalManual)
            {
                scaleX = new Rational(_scale.x, 128);
                scaleY = new Rational(_scale.y, 128);
            }
            else if (_scale.signal == ScaleSignals32.kScaleSignalVanishingPoint)
            {
                int num = _scale.max * (_position.Y - plane._vanishingPoint.Y) / (scriptWidth - plane._vanishingPoint.Y);
                scaleX = new Rational(num, 128);
                scaleY = new Rational(num, 128);
            }

            if (scaleX.Numerator == 0 || scaleY.Numerator == 0)
            {
                return new Rect();
            }

            short originX = celObj._origin.X;
            short originY = celObj._origin.Y;

            if (_mirrorX != celObj._mirrorX && _celInfo.type != CelType.Pic)
            {
                originX = (short) (celObj._width - originX - 1);
            }

            if (celObj._xResolution != LowRes.X || celObj._yResolution != LowRes.Y)
            {
                // high resolution coordinates

                if (_useInsetRect)
                {
                    Rational scriptToCelX = new Rational(celObj._xResolution, scriptWidth);
                    Rational scriptToCelY = new Rational(celObj._yResolution, scriptHeight);
                    Helpers.Mulru(ref nsRect, ref scriptToCelX, ref scriptToCelY, 0);

                    if (nsRect.Intersects(celObjRect))
                    {
                        nsRect.Clip(celObjRect);
                    }
                    else
                    {
                        nsRect = new Rect();
                    }
                }

                if (!scaleX.IsOne || !scaleY.IsOne)
                {
                    // Different games use a different cel scaling mode, but the
                    // difference isn't consistent across SCI versions; instead,
                    // it seems to be related to an update that happened during
                    // SCI2.1mid where games started using hi-resolution game
                    // scripts
                    if (scriptWidth == LowRes.X)
                    {
                        Helpers.Mulinc(ref nsRect, scaleX, scaleY);
                        // TODO: This was in the original code, baked into the
                        // multiplication though it is not immediately clear
                        // why this is the only one that reduces the BR corner
                        nsRect.Right -= 1;
                        nsRect.Bottom -= 1;
                    }
                    else
                    {
                        nsRect.Left = (short) (nsRect.Left * scaleX);
                        nsRect.Top = (short) (nsRect.Top * scaleY);

                        if (scaleX.Numerator > scaleX.Denominator)
                        {
                            nsRect.Right = (short) (nsRect.Right * scaleX);
                        }
                        else
                        {
                            nsRect.Right = (short) (((nsRect.Right - 1) * scaleX) + 1);
                        }

                        if (scaleY.Numerator > scaleY.Denominator)
                        {
                            nsRect.Bottom = (short) (nsRect.Bottom * scaleY);
                        }
                        else
                        {
                            nsRect.Bottom = (short) (((nsRect.Bottom - 1) * scaleY) + 1);
                        }
                    }
                }

                Rational celToScriptX = new Rational(scriptWidth, celObj._xResolution);
                Rational celToScriptY = new Rational(scriptHeight, celObj._yResolution);

                originX = (short) (originX * scaleX * celToScriptX);
                originY = (short) (originY * scaleY * celToScriptY);

                Helpers.Mulinc(ref nsRect, celToScriptX, celToScriptY);
                nsRect.Translate((short) (_position.X - originX), (short) (_position.Y - originY));
            }
            else
            {
                // low resolution coordinates

                if (!scaleX.IsOne || !scaleY.IsOne)
                {
                    Helpers.Mulinc(ref nsRect, scaleX, scaleY);
                    // TODO: This was in the original code, baked into the
                    // multiplication though it is not immediately clear
                    // why this is the only one that reduces the BR corner
                    nsRect.Right -= 1;
                    nsRect.Bottom -= 1;
                }

                originX = (short) (originX * scaleX);
                originY = (short) (originY * scaleY);
                nsRect.Translate((short) (_position.X - originX), (short) (_position.Y - originY));

                if (_mirrorX != celObj._mirrorX && _celInfo.type != CelType.Pic)
                {
                    nsRect.Translate((short) (plane._gameRect.Width - nsRect.Width), 0);
                }
            }

            return nsRect;
        }
    }

    internal class ScreenItemList : StablePointerArray<ScreenItem>
    {
        private int[] _unsorted = new int[250];

        public ScreenItemList() : base(250)
        {
        }

        public ScreenItem FindByObject(Register @object)
        {
            return this.FirstOrDefault(o => o?._object == @object);
        }

        public override void Sort()
        {
            if (Count < 2)
            {
                return;
            }

            for (var i = 0; i < Count; ++i)
            {
                _unsorted[i] = i;
            }

            for (var i = Count - 1; i > 0; --i)
            {
                bool swap = false;

                for (var j = 0; j < i; ++j)
                {
                    ScreenItem a = this[j];
                    ScreenItem b = this[j + 1];

                    if (a == null || a.CompareTo(b) > 0)
                    {
                        ScummHelper.Swap(ref a, ref b);
                        ScummHelper.Swap(ref _unsorted[j], ref _unsorted[j + 1]);
                        swap = true;
                    }
                }

                if (!swap)
                {
                    break;
                }
            }
        }


        public void Unsort()
        {
            if (Count < 2)
            {
                return;
            }

            for (var i = 0; i < Count; ++i)
            {
                while (_unsorted[i] != i)
                {
                    var a = this[_unsorted[i]];
                    var b = this[i];
                    ScummHelper.Swap(ref a, ref b);
                    ScummHelper.Swap(ref _unsorted[_unsorted[i]], ref _unsorted[i]);
                }
            }
        }
    }
}

#endif