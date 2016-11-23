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
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using NScumm.Core.Common;

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

    internal class DrawItem : IComparable<DrawItem>
    {
        /**
         * The screen item to draw.
         */
        public ScreenItem screenItem;

        /**
         * The target rectangle of the draw operation.
         */
        public Rect rect;

        public DrawItem()
        {
        }

        public DrawItem(DrawItem item)
        {
            screenItem = item.screenItem;
            rect = item.rect;
        }

        public int CompareTo(DrawItem other)
        {
            return screenItem.CompareTo(other.screenItem);
        }
    }

    internal class DrawList : StablePointerArray<DrawItem>
    {
        public DrawList() : base(250, o => new DrawItem(o))
        {
        }

        public void Add(ScreenItem screenItem, Rect rect)
        {
            var drawItem = new DrawItem
            {
                screenItem = screenItem,
                rect = rect
            };
            base.Add(drawItem);
        }
    }

    internal class RectList : StablePointerArray<Rect>
    {
        public RectList() : base(200, o => new Rect(o))
        {
        }
    }

    internal class PlaneList : Array<Plane>
    {
        public PlaneList() : base(() => null)
        {
        }

        public Plane FindByObject(Register @object)
        {
            var plane = this.FirstOrDefault(o => Equals(o._object, @object));
            return plane;
        }

        public short GetTopPlanePriority()
        {
            if (Size > 0)
            {
                return this[Size - 1]._priority;
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

        public int FindIndexByObject(Register @object)
        {
            for (var i = 0; i < Size; ++i)
            {
                if (this[i] != null && this[i]._object == @object)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Erase(Plane plane)
        {
            for (int i = 0; i < Size; i++)
            {
                if (this[i] == plane)
                {
                    RemoveAt(i);
                    break;
                }
            }
        }

        public void Add(Plane plane)
        {
            for (var i = 0; i < Size; i++)
            {
                if (this[i]._priority > plane._priority)
                {
                    Insert(i, plane);
                    return;
                }
            }

            PushBack(plane);
        }

        public void Sort()
        {
            Array.Sort(_storage, 0, Size);
        }
    }

    /// <summary>
    // A plane is a grouped layer of screen items.
    /// </summary>
    internal class Plane : IComparable<Plane>
    {
        /// <summary>
        /// A serial used for planes that are generated inside
        /// the graphics engine, rather than the interpreter.
        /// </summary>
        private static ushort _nextObjectId;

        /// <summary>
        /// For planes that are used to render picture data, the
        /// resource ID of the picture to be displayed. This
        /// value may also be one of the special
        /// PlanePictureCodes, in which case the plane becomes a
        /// non-picture plane.
        /// </summary>
        private PlanePictureCodes _pictureId;

        /// <summary>
        /// Whether or not the contents of picture planes should
        /// be drawn horizontally mirrored. Only applies to
        /// planes of type Picture.
        /// </summary>
        private bool _mirrored;

        /// <summary>
        /// Whether the picture ID for this plane has changed.
        /// This flag is set when the plane is created or updated
        /// from a VM object, and is cleared when the plane is
        /// synchronised to another plane (which calls
        /// changePic).
        /// </summary>
        private bool _pictureChanged;

        /// <summary>
        /// The type of the plane.
        /// </summary>
        public PlaneType _type;

        /// <summary>
        /// The color to use when erasing the plane. Only
        /// applies to planes of type Colored.
        /// </summary>
        public byte _back;

        /// <summary>
        /// Whether the priority of this plane has changed.
        /// This flag is set when the plane is updated from
        /// another plane and cleared when draw list calculation
        /// occurs.
        /// </summary>
        public int _priorityChanged;

        /// <summary>
        /// A handle to the VM object corresponding to this
        /// plane. Some planes are generated purely within the
        /// graphics engine and have a numeric object value.
        /// </summary>
        public Register _object;

        /// <summary>
        /// The rendering priority of the plane. Higher
        /// priorities are drawn above lower priorities.
        /// </summary>
        public short _priority;

        /// <summary>
        /// Whether or not all screen items in this plane should
        /// be redrawn on the next frameout, instead of just
        /// the screen items marked as updated. This is set when
        /// visual changes to the plane itself are made that
        /// affect the rendering of the entire plane, and cleared
        /// once those changes are rendered by `redrawAll`.
        /// </summary>
        public int _redrawAllCount;

        /// <summary>
        /// Flags indicating the state of the plane.
        /// - `created` is set when the plane is first created,
        ///   either from a VM object or from within the engine
        ///   itself
        /// - `updated` is set when the plane is updated from
        ///   another plane and the two planes' `planeRect`s do
        ///   not match
        /// - `deleted` is set when the plane is deleted by a
        ///   kernel call
        /// - `moved` is set when the plane has been moved or
        ///   resized
        /// </summary>
        public int _created, _updated, _deleted, _moved;

        /// <summary>
        /// The vanishing point for the plane. Used when
        /// automatically calculating the correct scaling of the
        /// plane's screen items according to their position.
        /// </summary>
        public Point _vanishingPoint;

        /// <summary>
        /// The position & dimensions of the plane in screen
        /// coordinates. This rect is not clipped to the screen,
        /// so may include coordinates that are offscreen.
        /// </summary>
        public Rect _planeRect;

        /// <summary>
        /// The position & dimensions of the plane in game script
        /// coordinates.
        /// </summary>
        public Rect _gameRect;

        /// <summary>
        /// The position & dimensions of the plane in screen
        /// coordinates. This rect is clipped to the screen.
        /// </summary>
        public Rect _screenRect;

        /// <summary>
        /// The list of screen items grouped within this plane.
        /// </summary>
        public readonly ScreenItemList _screenItemList = new ScreenItemList();

        /// <summary>
        /// NOTE: This constructor signature originally did not accept a
        /// picture ID, but some calls to construct planes with this signature
        /// immediately set the picture ID and then called setType again, so
        /// it made more sense to just make the picture ID a parameter instead.
        /// </summary>
        /// <param name="gameRect"></param>
        /// <param name="pictureId"></param>
        public Plane(Rect gameRect, PlanePictureCodes pictureId = PlanePictureCodes.kPlanePicColored)
        {
            _pictureId = pictureId;
            _type = PlaneType.Colored;
            _object = Register.Make(0, _nextObjectId++);
            _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _gameRect = gameRect;
            ConvertGameRectToPlaneRect();
            _priority = (short)Math.Max(10000, SciEngine.Instance._gfxFrameout.GetPlanes().GetTopPlanePriority() + 1);
            SetType();
            _screenRect = _planeRect;
        }

        public Plane(Plane other)
        {
            _pictureId = other._pictureId;
            _mirrored = other._mirrored;
            _type = other._type;
            _back = other._back;
            _object = other._object;
            _priority = other._priority;
            _planeRect = other._planeRect;
            _gameRect = other._gameRect;
            _screenRect = other._screenRect;
            _screenItemList.CopyFrom(other._screenItemList);
        }

        public Plane(Register @object)
        {
            _type = PlaneType.Colored;
            _object = @object;
            _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
            _created = SciEngine.Instance._gfxFrameout.GetScreenCount();
            var segMan = SciEngine.Instance.EngineState._segMan;
            _vanishingPoint.X = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingX);
            _vanishingPoint.Y = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingY);

            _gameRect.Left = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.inLeft);
            _gameRect.Top = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.inTop);
            _gameRect.Right = (short)(SciEngine.ReadSelectorValue(segMan, @object, o => o.inRight) + 1);
            _gameRect.Bottom = (short)(SciEngine.ReadSelectorValue(segMan, @object, o => o.inBottom) + 1);
            ConvertGameRectToPlaneRect();

            _back = (byte)SciEngine.ReadSelectorValue(segMan, @object, o => o.back);
            _priority = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.priority);
            _pictureId = (PlanePictureCodes)SciEngine.ReadSelectorValue(segMan, @object, o => o.picture);
            SetType();

            _mirrored = SciEngine.ReadSelectorValue(segMan, @object, o => o.mirrored) != 0;
            _screenRect = _planeRect;
            ChangePic();
        }

        public void FilterUpEraseRects(DrawList drawList, RectList lowerEraseList)
        {
            var lowerEraseCount = lowerEraseList.Count;
            for (var i = 0; i < lowerEraseCount; ++i)
            {
                var screenItemCount = _screenItemList.Count;
                for (var j = 0; j < screenItemCount; ++j)
                {
                    ScreenItem item = _screenItemList[j];
                    if (item != null && lowerEraseList[i].Intersects(item._screenRect))
                    {
                        MergeToDrawList(j, lowerEraseList[i], drawList);
                    }
                }
            }
        }

        public void FilterDownEraseRects(DrawList drawList, RectList eraseList, RectList higherEraseList)
        {
            var higherEraseCount = higherEraseList.Count;

            if (_type == PlaneType.Transparent || _type == PlaneType.TransparentPicture)
            {
                for (var i = 0; i < higherEraseCount; ++i)
                {
                    Rect r = higherEraseList[i];
                    var screenItemCount = _screenItemList.Count;
                    for (var j = 0; j < screenItemCount; ++j)
                    {
                        var item = _screenItemList[j];
                        if (item != null && r.Intersects(item._screenRect))
                        {
                            MergeToDrawList(j, r, drawList);
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < higherEraseCount; ++i)
                {
                    Rect r = higherEraseList[i];
                    if (!r.Intersects(_screenRect)) continue;

                    r.Clip(_screenRect);
                    MergeToRectList(r, eraseList);

                    var screenItemCount = _screenItemList.Count;
                    for (var j = 0; j < screenItemCount; ++j)
                    {
                        ScreenItem item = _screenItemList[j];
                        if (item != null && r.Intersects(item._screenRect))
                        {
                            MergeToDrawList(j, r, drawList);
                        }
                    }

                    Rect[] outRects = new Rect[4];
                    Rect r2 = higherEraseList[i];
                    int splitCount = GfxFrameout.SplitRects(r2, r, outRects);
                    if (splitCount > 0)
                    {
                        while (splitCount-- != 0)
                        {
                            higherEraseList.Add(outRects[splitCount]);
                        }
                    }
                    higherEraseList.RemoveAt(i);
                }

                higherEraseList.Pack();
            }
        }

        public void FilterUpDrawRects(DrawList drawList, DrawList lowerDrawList)
        {
            var lowerDrawCount = lowerDrawList.Count;
            for (var i = 0; i < lowerDrawCount; ++i)
            {
                Rect r = lowerDrawList[i].rect;
                var screenItemCount = _screenItemList.Count;
                for (var j = 0; j < screenItemCount; ++j)
                {
                    ScreenItem item = _screenItemList[j];
                    if (item != null && r.Intersects(item._screenRect))
                    {
                        MergeToDrawList(j, r, drawList);
                    }
                }
            }
        }

        public void RedrawAll(Plane visiblePlane, PlaneList planeList, DrawList drawList, RectList eraseList)
        {
            var screenItemCount = _screenItemList.Count;
            for (var i = 0; i < screenItemCount; ++i)
            {
                ScreenItem screenItem = _screenItemList[i];
                if (screenItem != null && screenItem._deleted == 0)
                {
                    screenItem.CalcRects(this);
                    if (!screenItem._screenRect.IsEmpty)
                    {
                        MergeToDrawList(i, screenItem._screenRect, drawList);
                    }
                }
            }

            eraseList.Clear();

            if (!_screenRect.IsEmpty && _type != PlaneType.Picture && _type != PlaneType.Opaque)
            {
                eraseList.Add(_screenRect);
            }
            BreakEraseListByPlanes(eraseList, planeList);
            BreakDrawListByPlanes(drawList, planeList);
            --_redrawAllCount;
            DecrementScreenItemArrayCounts(visiblePlane, true);
        }

        public void DecrementScreenItemArrayCounts(Plane visiblePlane, bool forceUpdate)
        {
            var screenItemCount = _screenItemList.Count;
            for (var i = 0; i < screenItemCount; ++i)
            {
                ScreenItem item = _screenItemList[i];

                if (item == null) continue;

                // update item in visiblePlane if item is updated
                if (item._updated != 0 ||
                    (forceUpdate && visiblePlane?._screenItemList.FindByObject(item._object) != null))
                {
                    visiblePlane._screenItemList[i] = item;
                }

                if (item._updated != 0)
                {
                    item._updated--;
                }

                // create new item in visiblePlane if item was added
                if (item._created != 0)
                {
                    item._created--;
                    visiblePlane?._screenItemList.Add(new ScreenItem(item));
                }

                // delete item from both planes if it was deleted
                if (item._deleted == 0) continue;

                item._deleted--;
                if (item._deleted != 0) continue;

                if (visiblePlane?._screenItemList.FindByObject(item._object) != null)
                {
                    visiblePlane._screenItemList.RemoveAt(i);
                }
                _screenItemList.RemoveAt(i);
            }

            _screenItemList.Pack();
            visiblePlane?._screenItemList.Pack();
        }

        public void CalcLists(Plane visiblePlane, PlaneList planeList, DrawList drawList, RectList eraseList)
        {
            var screenItemCount = _screenItemList.Count;
            var visiblePlaneItemCount = visiblePlane._screenItemList.Count;

            for (var i = 0; i < screenItemCount; ++i)
            {
                // Items can be added to ScreenItemList and we don't want to process
                // those new items, but the list also can grow smaller, so we need
                // to check that we are still within the upper bound of the list and
                // quit if we aren't any more
                if (i >= _screenItemList.Count)
                {
                    break;
                }

                ScreenItem item = _screenItemList[i];
                if (item == null)
                {
                    continue;
                }

                // NOTE: The original engine used an array without bounds checking
                // so could just get the visible screen item directly; we need to
                // verify that the index is actually within the valid range for
                // the visible plane before accessing the item to avoid a range
                // error.
                ScreenItem visibleItem = null;
                if (i < visiblePlaneItemCount)
                {
                    visibleItem = visiblePlane._screenItemList[i];
                }

                // Keep erase rects for this screen item from drawing outside
                // of its owner plane
                Rect visibleItemScreenRect = new Rect();
                if (visibleItem != null)
                {
                    visibleItemScreenRect = visibleItem._screenRect;
                    visibleItemScreenRect.Clip(_screenRect);
                }

                if (item._deleted != 0)
                {
                    // Add item's rect to erase list
                    if (
                        visibleItem != null &&
                        !visibleItemScreenRect.IsEmpty
                    )
                    {
                        if (SciEngine.Instance._gfxRemap32.RemapCount != 0)
                        {
                            MergeToRectList(visibleItemScreenRect, eraseList);
                        }
                        else
                        {
                            eraseList.Add(visibleItemScreenRect);
                        }
                    }
                }

                if (item._created == 0 && item._updated == 0)
                {
                    continue;
                }

                item.CalcRects(this);
                Rect itemScreenRect = new Rect(item._screenRect);

                if (item._created != 0)
                {
                    // Add item to draw list
                    if (!itemScreenRect.IsEmpty)
                    {
                        if (SciEngine.Instance._gfxRemap32.RemapCount != 0)
                        {
                            drawList.Add(item, itemScreenRect);
                            MergeToRectList(itemScreenRect, eraseList);
                        }
                        else
                        {
                            drawList.Add(item, itemScreenRect);
                        }
                    }
                }
                else
                {
                    // Add old rect to erase list, new item to draw list

                    if (SciEngine.Instance._gfxRemap32.RemapCount != 0)
                    {
                        // If item and visibleItem don't overlap...
                        if (itemScreenRect.IsEmpty ||
                            visibleItem == null ||
                            visibleItemScreenRect.IsEmpty ||
                            !visibleItemScreenRect.Intersects(itemScreenRect)
                        )
                        {
                            // ...add item to draw list, and old rect to erase list...
                            if (!itemScreenRect.IsEmpty)
                            {
                                drawList.Add(item, itemScreenRect);
                                MergeToRectList(itemScreenRect, eraseList);
                            }
                            if (visibleItem != null && !visibleItemScreenRect.IsEmpty)
                            {
                                MergeToRectList(visibleItemScreenRect, eraseList);
                            }
                        }
                        else
                        {
                            // ...otherwise, add bounding box of old+new to erase list,
                            // and item to draw list
                            Rect extendedScreenRect = visibleItemScreenRect;
                            extendedScreenRect.Extend(itemScreenRect);

                            drawList.Add(item, itemScreenRect);
                            MergeToRectList(extendedScreenRect, eraseList);
                        }
                    }
                    else
                    {
                        // If no active remaps, just add item to draw list and old rect
                        // to erase list

                        // TODO: SCI3 update rects for VMD?
                        if (!itemScreenRect.IsEmpty)
                        {
                            drawList.Add(item, itemScreenRect);
                        }
                        if (visibleItem != null && !visibleItemScreenRect.IsEmpty)
                        {
                            eraseList.Add(visibleItemScreenRect);
                        }
                    }
                }
            }

            // Remove parts of eraselist/drawlist that are covered by other planes
            BreakEraseListByPlanes(eraseList, planeList);
            BreakDrawListByPlanes(drawList, planeList);

            // We store the current size of the drawlist, as we want to loop
            // over the currently inserted entries later.
            var drawListSizePrimary = drawList.Count;
            var eraseListCount = eraseList.Count;

            // TODO: Figure out which games need which rendering method
            if ( /* TODO: dword_C6288 */ false)
            {
                // "high resolution pictures"
                _screenItemList.Sort();
                bool pictureDrawn = false;
                bool screenItemDrawn = false;

                for (var i = 0; i < eraseListCount; ++i)
                {
                    Rect rect = eraseList[i];

                    for (var j = 0; j < screenItemCount; ++j)
                    {
                        ScreenItem item = _screenItemList[j];

                        if (item == null)
                        {
                            continue;
                        }

                        if (rect.Intersects(item._screenRect))
                        {
                            Rect intersection = rect.FindIntersectingRect(item._screenRect);
                            if (item._deleted == 0)
                            {
                                if (pictureDrawn)
                                {
                                    if (item._celInfo.type == CelType.Pic)
                                    {
                                        if (screenItemDrawn || item._celInfo.celNo == 0)
                                        {
                                            MergeToDrawList(j, intersection, drawList);
                                        }
                                    }
                                    else
                                    {
                                        if (item._updated == 0 && item._created == 0)
                                        {
                                            MergeToDrawList(j, intersection, drawList);
                                        }
                                        screenItemDrawn = true;
                                    }
                                }
                                else
                                {
                                    if (item._updated == 0 && item._created == 0)
                                    {
                                        MergeToDrawList(j, intersection, drawList);
                                    }
                                    if (item._celInfo.type == CelType.Pic)
                                    {
                                        pictureDrawn = true;
                                    }
                                }
                            }
                        }
                    }
                }

                _screenItemList.Unsort();
            }
            else
            {
                // Add all items overlapping the erase list to the draw list
                for (var i = 0; i < eraseListCount; ++i)
                {
                    Rect rect = eraseList[i];
                    for (var j = 0; j < screenItemCount; ++j)
                    {
                        ScreenItem item = _screenItemList[j];
                        if (
                            item != null &&
                            item._created == 0 && item._updated == 0 && item._deleted == 0 &&
                            rect.Intersects(item._screenRect)
                        )
                        {
                            drawList.Add(item, rect.FindIntersectingRect(item._screenRect));
                        }
                    }
                }
            }

            if (SciEngine.Instance._gfxRemap32.RemapCount == 0)
            {
                // Add all items that overlap with items in the drawlist and have higher
                // priority.

                // We only loop over "primary" items in the draw list, skipping
                // those that were added because of the erase list in the previous loop,
                // or those to be added in this loop.
                for (var i = 0; i < drawListSizePrimary; ++i)
                {
                    DrawItem drawListEntry = null;
                    if (i < drawList.Count)
                    {
                        drawListEntry = drawList[i];
                    }

                    for (var j = 0; j < screenItemCount; ++j)
                    {
                        ScreenItem newItem = null;
                        if (j < _screenItemList.Count)
                        {
                            newItem = _screenItemList[j];
                        }

                        if (
                            drawListEntry != null && newItem != null &&
                            newItem._created == 0 && newItem._updated == 0 && newItem._deleted == 0
                        )
                        {
                            ScreenItem drawnItem = drawListEntry.screenItem;

                            if (
                                (newItem._priority > drawnItem._priority ||
                                 (newItem._priority == drawnItem._priority && newItem._object > drawnItem._object)) &&
                                drawListEntry.rect.Intersects(newItem._screenRect)
                            )
                            {
                                MergeToDrawList(j, drawListEntry.rect.FindIntersectingRect(newItem._screenRect),
                                    drawList);
                            }
                        }
                    }
                }
            }

            DecrementScreenItemArrayCounts(visiblePlane, false);
        }

        public void ChangePic()
        {
            _pictureChanged = false;

            if (_type != PlaneType.Picture && _type != PlaneType.TransparentPicture)
            {
                return;
            }

            AddPicInternal(_pictureId, new Point(), _mirrored);
        }

        public static void Init()
        {
            _nextObjectId = 20000;
        }

        public void RemapMarkRedraw()
        {
            var screenItemCount = _screenItemList.Count;
            for (var i = 0; i < screenItemCount; ++i)
            {
                ScreenItem screenItem = _screenItemList[i];
                if (
                    screenItem != null &&
                    screenItem._deleted == 0 && screenItem._created == 0 &&
                    screenItem._celObj._remap
                )
                {
                    screenItem._updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
            }
        }

        /// <summary>
        /// Updates the plane to match the state of the plane
        /// object from the virtual machine.
        /// 
        /// @note This method was originally called UpdatePlane
        /// in SCI engine.
        /// </summary>
        /// <param name="object"></param>
        public void Update(Register @object)
        {
            SegManager segMan = SciEngine.Instance.EngineState._segMan;
            _vanishingPoint.X = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingX);
            _vanishingPoint.Y = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.vanishingY);
            _gameRect.Left = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.inLeft);
            _gameRect.Top = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.inTop);
            _gameRect.Right = (short)(SciEngine.ReadSelectorValue(segMan, @object, o => o.inRight) + 1);
            _gameRect.Bottom = (short)(SciEngine.ReadSelectorValue(segMan, @object, o => o.inBottom) + 1);
            ConvertGameRectToPlaneRect();

            _priority = (short)SciEngine.ReadSelectorValue(segMan, @object, o => o.priority);
            PlanePictureCodes pictureId =
                (PlanePictureCodes)SciEngine.ReadSelectorValue(segMan, @object, o => o.picture);
            if (_pictureId != pictureId)
            {
                _pictureId = pictureId;
                _pictureChanged = true;
            }

            _mirrored = SciEngine.ReadSelectorValue(segMan, @object, o => o.mirrored) != 0;
            _back = (byte)SciEngine.ReadSelectorValue(segMan, @object, o => o.back);
        }

        /// <summary>
        /// Clips the screen rect of this plane to fit within the
        /// given screen rect.
        /// </summary>
        /// <param name="screenRect"></param>
        public void ClipScreenRect(Rect screenRect)
        {
            // LSL6 hires creates planes with invalid rects; SSCI does not
            // care about this, but `Common::Rect::clip` does, so we need to
            // check whether or not the rect is actually valid before clipping
            // and only clip valid rects
            if (_screenRect.IsValidRect && _screenRect.Intersects(screenRect))
            {
                _screenRect.Clip(screenRect);
            }
            else
            {
                _screenRect.Left = 0;
                _screenRect.Top = 0;
                _screenRect.Right = 0;
                _screenRect.Bottom = 0;
            }
        }

        /// <summary>
        /// Modifies the position of all non-pic screen items
        /// by the given delta. If `scrollPics` is true, pic
        /// items are also repositioned.
        /// </summary>
        /// <param name="deltaX"></param>
        /// <param name="deltaY"></param>
        /// <param name="scrollPics"></param>
        public void ScrollScreenItems(short deltaX, short deltaY, bool scrollPics)
        {
            _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();

            foreach (var screenItem in _screenItemList)
            {
                if (screenItem?._deleted == 0 && (screenItem._celInfo.type != CelType.Pic || scrollPics))
                {
                    screenItem._position.X += deltaX;
                    screenItem._position.Y += deltaY;
                }
            }
        }

        /// <summary>
        /// Compares the properties of the current plane against
        /// the properties of the `other` plane (which is the
        /// corresponding plane from the visible plane list) to
        /// discover which properties have been changed on this
        /// plane by a call to `update(reg_t)`.
        /// 
        /// @note This method was originally called UpdatePlane
        /// in SCI engine.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="screenRect"></param>
        public void Sync(Plane other, Rect screenRect)
        {
            if (other == null)
            {
                if (_pictureChanged)
                {
                    DeleteAllPics();
                    SetType();
                    ChangePic();
                    _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                else
                {
                    SetType();
                }
            }
            else
            {
                if (
                    _planeRect.Top != other._planeRect.Top ||
                    _planeRect.Left != other._planeRect.Left ||
                    _planeRect.Right > other._planeRect.Right ||
                    _planeRect.Bottom > other._planeRect.Bottom
                )
                {
                    // the plane moved or got larger
                    _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
                    _moved = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                else if (_planeRect != other._planeRect)
                {
                    // the plane got smaller
                    _moved = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }

                if (_priority != other._priority)
                {
                    _priorityChanged = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }

                if (_pictureId != other._pictureId || _mirrored != other._mirrored || _pictureChanged)
                {
                    DeleteAllPics();
                    SetType();
                    ChangePic();
                    _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }

                if (_back != other._back)
                {
                    _redrawAllCount = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
            }

            _deleted = 0;
            if (_created == 0)
            {
                _updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
            }

            ConvertGameRectToPlaneRect();
            _screenRect = _planeRect;
            // NOTE: screenRect originally was retrieved through globals
            // instead of being passed into the function
            ClipScreenRect(screenRect);
        }

        public int CompareTo(Plane other)
        {
            var result = _priority.CompareTo(other._priority);
            if (result != 0) return result;
            return _object.CompareTo(other._object);
        }

        public int AddPic(int pictureId, Point position, bool mirrorX, bool deleteDuplicate = true)
        {
            if (deleteDuplicate)
            {
                DeletePic(pictureId);
            }
            AddPicInternal((PlanePictureCodes)pictureId, position, mirrorX);
            return (int)_pictureId;
        }

        public void Assign(Plane other)
        {
            _gameRect = other._gameRect;
            _planeRect = other._planeRect;
            _vanishingPoint = other._vanishingPoint;
            _pictureId = other._pictureId;
            _type = other._type;
            _mirrored = other._mirrored;
            _priority = other._priority;
            _back = other._back;
            _screenRect = other._screenRect;
            _priorityChanged = other._priorityChanged;
        }


        private static void MergeToRectList(Rect rect, RectList eraseList)
        {
            RectList mergeList = new RectList();
            mergeList.Add(rect);

            for (var i = 0; i < mergeList.Count; ++i)
            {
                var r = mergeList[i];

                var eraseCount = eraseList.Count;
                for (var j = 0; j < eraseCount; ++j)
                {
                    Rect eraseRect = eraseList[j];
                    if (eraseRect.Contains(r))
                    {
                        mergeList.RemoveAt(i);
                        break;
                    }

                    var outRects = new Rect[4];
                    int splitCount = GfxFrameout.SplitRects(r, eraseRect, outRects);
                    if (splitCount == -1) continue;
                    while (splitCount-- != 0)
                    {
                        mergeList.Add(outRects[splitCount]);
                    }

                    mergeList.RemoveAt(i);

                    // proceed to the next rect
                    r = mergeList[++i];
                }
            }

            mergeList.Pack();

            for (var i = 0; i < mergeList.Count; ++i)
            {
                eraseList.Add(mergeList[i]);
            }
        }

        private void BreakDrawListByPlanes(DrawList drawList, PlaneList planeList)
        {
            int nextPlaneIndex = planeList.FindIndexByObject(_object) + 1;
            var planeCount = planeList.Size;

            for (var i = 0; i < drawList.Count; ++i)
            {
                for (var j = nextPlaneIndex; j < planeCount; ++j)
                {
                    if (
                        planeList[j]._type != PlaneType.Transparent &&
                        planeList[j]._type != PlaneType.TransparentPicture
                    )
                    {
                        Rect[] outRects = new Rect[4];
                        int splitCount = GfxFrameout.SplitRects(drawList[i].rect, planeList[j]._screenRect, outRects);
                        if (splitCount != -1)
                        {
                            while (splitCount-- != 0)
                            {
                                drawList.Add(drawList[i].screenItem, outRects[splitCount]);
                            }

                            drawList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            drawList.Pack();
        }

        private void BreakEraseListByPlanes(RectList eraseList, PlaneList planeList)
        {
            int nextPlaneIndex = planeList.FindIndexByObject(_object) + 1;
            var planeCount = planeList.Size;

            for (var i = 0; i < eraseList.Count; ++i)
            {
                for (var j = nextPlaneIndex; j < planeCount; ++j)
                {
                    if (planeList[j]._type == PlaneType.Transparent ||
                        planeList[j]._type == PlaneType.TransparentPicture) continue;

                    Rect[] outRects = new Rect[4];
                    int splitCount = GfxFrameout.SplitRects(eraseList[i], planeList[j]._screenRect, outRects);
                    if (splitCount == -1) continue;
                    while (splitCount-- != 0)
                    {
                        eraseList.Add(outRects[splitCount]);
                    }

                    eraseList.RemoveAt(i);
                    break;
                }
            }
            eraseList.Pack();
        }

        private void MergeToDrawList(int index, Rect rect, DrawList drawList)
        {
            RectList mergeList = new RectList();
            ScreenItem item = _screenItemList[index];
            Rect r = item._screenRect;
            r.Clip(rect);
            mergeList.Add(r);

            for (var i = 0; i < mergeList.Count; ++i)
            {
                r = mergeList[i];

                var drawCount = drawList.Count;
                for (var j = 0; j < drawCount; ++j)
                {
                    DrawItem drawItem = drawList[j];
                    if (item._object != drawItem.screenItem._object) continue;

                    if (drawItem.rect.Contains(r))
                    {
                        mergeList.RemoveAt(i);
                        break;
                    }

                    Rect[] outRects = new Rect[4];
                    int splitCount = GfxFrameout.SplitRects(r, drawItem.rect, outRects);
                    if (splitCount == -1) continue;

                    while (splitCount-- != 0)
                    {
                        mergeList.Add(outRects[splitCount]);
                    }

                    mergeList.RemoveAt(i);

                    // proceed to the next rect
                    r = mergeList[++i];
                }
            }

            mergeList.Pack();

            for (var i = 0; i < mergeList.Count; ++i)
            {
                drawList.Add(item, mergeList[i]);
            }
        }

        private void AddPicInternal(PlanePictureCodes pictureId, Point position, bool mirrorX)
        {
            ushort celCount = 1000;
            var transparent = true;
            for (var celNo = 0; celNo < celCount; ++celNo)
            {
                var celObj = CelObjPic.Create((int)pictureId, (short)celNo);
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
                    _pictureId = (int)pictureId,
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
            var screenWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            var screenHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;
            var scriptWidth = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            var scriptHeight = (short)SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            var ratioX = new Rational(screenWidth, scriptWidth);
            var ratioY = new Rational(screenHeight, scriptHeight);

            _planeRect = _gameRect;
            Helpers.Mulru(ref _planeRect, ref ratioX, ref ratioY, 1);
        }

        private void DeleteAllPics()
        {
            for (var i = 0; i < _screenItemList.Count; i++)
            {
                var screenItem = _screenItemList[i];
                if (screenItem == null || screenItem._celInfo.type != CelType.Pic) continue;

                if (screenItem._created == 0)
                {
                    screenItem._created = 0;
                    screenItem._updated = 0;
                    screenItem._deleted = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                else
                {
                    _screenItemList.RemoveAt(i);
                }
            }

            _screenItemList.Pack();
        }

        public void DeletePic(int pictureId)
        {
            foreach (var screenItem in _screenItemList)
            {
                if (screenItem._pictureId != pictureId) continue;

                screenItem._created = 0;
                screenItem._updated = 0;
                screenItem._deleted = SciEngine.Instance._gfxFrameout.GetScreenCount();
            }
        }

        public void DeletePic(int oldPictureId, int newPictureId)
        {
            DeletePic(oldPictureId);
            _pictureId = (PlanePictureCodes)newPictureId;
        }

    }
}

#endif