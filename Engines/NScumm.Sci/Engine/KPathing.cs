//
//  KPathing.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    // Structure describing an "extension" to the work polygon following edges
    // of the polygon being merged.

    // The patch begins on the point intersection1, being the intersection
    // of the edges starting at indexw1/vertexw1 on the work polygon, and at
    // indexp1/vertexp1 on the polygon being merged.
    // It ends with the point intersection2, being the analogous intersection.
    internal struct Patch
    {
        public int indexw1;
        public int indexp1;
        public Vertex vertexw1;
        public Vertex vertexp1;
        public Point intersection1;

        public int indexw2;
        public int indexp2;
        public Vertex vertexw2;
        public Vertex vertexp2;
        public Point intersection2;

        public bool disabled; // If true, this Patch was made superfluous by another Patch
    }

    internal class Vertex
    {
        public const uint HUGE_DISTANCE = uint.MaxValue;

        // Location
        public Point v;

        // Vertex circular list entry
        public Vertex _next; // next element
        public Vertex _prev; // previous element

        // A* cost variables
        public uint costF;
        public uint costG;

        // Previous vertex in shortest path
        public Vertex path_prev;

        public Vertex()
        {
        }

        public Vertex(Point p)
        {
            v = p;
            costG = HUGE_DISTANCE;
        }
    }

    // Error codes
    internal enum PathFindingError
    {
        OK = 0,
        ERROR = -1,
        FATAL = -2
    }

    internal class CircularVertexList : IEnumerable<Vertex>
    {
        public Vertex _head;

        public Vertex First
        {
            get { return _head; }
        }

        public void InsertAtEnd(Vertex elm)
        {
            if (_head == null)
            {
                elm._next = elm._prev = elm;
                _head = elm;
            }
            else
            {
                elm._next = _head;
                elm._prev = _head._prev;
                _head._prev = elm;
                elm._prev._next = elm;
            }
        }

        public void InsertHead(Vertex elm)
        {
            InsertAtEnd(elm);
            _head = elm;
        }

        public static void InsertAfter(Vertex listelm, Vertex elm)
        {
            elm._prev = listelm;
            elm._next = listelm._next;
            listelm._next._prev = elm;
            listelm._next = elm;
        }

        public void Remove(Vertex elm)
        {
            if (elm._next == elm)
            {
                _head = null;
            }
            else
            {
                if (_head == elm)
                    _head = elm._next;
                elm._prev._next = elm._next;
                elm._next._prev = elm._prev;
            }
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            var v = First;
            while (v != null)
            {
                yield return v;
                v = v._next == First ? null : v._next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsEmpty
        {
            get { return _head == null; }
        }
    }

    // SCI-defined polygon types
    internal enum PolygonType
    {
        TOTAL_ACCESS = 0,
        NEAREST_ACCESS = 1,
        BARRED_ACCESS = 2,
        CONTAINED_ACCESS = 3
    }

    internal enum PolygonContainmentType
    {
        OUTSIDE = 0,
        ON_EDGE = 1,
        INSIDE = 2
    }

    // Floating point struct
    internal struct FloatPoint
    {
        public FloatPoint(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        public FloatPoint(Point p)
        {
            X = p.X;
            Y = p.Y;
        }

        public Point ToPoint()
        {
            return new Point((short) (X + 0.5), (short) (Y + 0.5));
        }

        public static float operator *(FloatPoint p1, FloatPoint p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y;
        }

        public static FloatPoint operator *(FloatPoint p, float l)
        {
            return new FloatPoint(l * p.X, l * p.Y);
        }

        public static FloatPoint operator -(FloatPoint p1, FloatPoint p2)
        {
            return new FloatPoint(p1.X - p2.X, p1.Y - p2.Y);
        }

        public float Norm()
        {
            return X * X + Y * Y;
        }

        public float X, Y;
    }

    internal class Polygon
    {
        // SCI polygon type
        public PolygonType Type;

        // Circular list of vertices
        public CircularVertexList Vertices = new CircularVertexList();

        public Polygon(PolygonType t)
        {
            Type = t;
        }
    }

    // Pathfinding state
    internal class PathfindingState
    {
        // List of all polygons
        public List<Polygon> polygons = new List<Polygon>();

        // Start and end points for pathfinding
        public Vertex vertex_start, vertex_end;

        // Array of all vertices, used for sorting
        public Vertex[] vertex_index;

        // Total number of vertices
        public int vertices;

        // Point to prepend and append to final path
        public Point? _prependPoint;
        public Point? _appendPoint;

        // Screen size
        public int _width, _height;

        public PathfindingState(int width, int height)
        {
            _width = width;
            _height = height;
            vertices = 0;
        }

        /// <summary>
        /// Determines if a point lies on the screen border
        /// </summary>
        /// <returns>true if p lies on the screen border, false otherwise.</returns>
        /// <param name="p">The point.</param>
        public bool PointOnScreenBorder(Point p)
        {
            return (p.X == 0) || (p.X == _width - 1) || (p.Y == 0) || (p.Y == _height - 1);
        }

        /// <summary>
        /// Determines if an edge lies on the screen border
        /// </summary>
        /// <returns>true if (p, q) lies on the screen border, false otherwise.</returns>
        /// <param name="p">p, q: The edge (p, q)</param>
        /// <param name="q">p, q: The edge (p, q)</param>
        public bool EdgeOnScreenBorder(Point p, Point q)
        {
            return ((p.X == 0 && q.X == 0) || (p.Y == 0 && q.Y == 0)
                    || ((p.X == _width - 1) && (q.X == _width - 1))
                    || ((p.Y == _height - 1) && (q.Y == _height - 1)));
        }

        public PathFindingError FindNearPoint(Point p, Polygon polygon, out Point? ret)
        {
            FloatPoint near_p = new FloatPoint();
            uint dist = Vertex.HUGE_DISTANCE;

            foreach (var vertex in polygon.Vertices)
            {
                Point p1 = vertex.v;
                Point p2 = vertex._next.v;
                float u;
                FloatPoint new_point = new FloatPoint();
                uint new_dist;

                // Ignore edges on the screen border, except for contained access polygons
                if ((polygon.Type != PolygonType.CONTAINED_ACCESS) && (EdgeOnScreenBorder(p1, p2)))
                    continue;

                // Compute near point
                u = ((p.X - p1.X) * (p2.X - p1.X) + (p.Y - p1.Y) * (p2.Y - p1.Y)) / (float) p1.SquareDistance(p2);

                // Clip to edge
                if (u < 0.0f)
                    u = 0.0f;
                if (u > 1.0f)
                    u = 1.0f;

                new_point.X = p1.X + u * (p2.X - p1.X);
                new_point.Y = p1.Y + u * (p2.Y - p1.Y);

                new_dist = p.SquareDistance(new_point.ToPoint());

                if (new_dist < dist)
                {
                    near_p = new_point;
                    dist = new_dist;
                }
            }

            // Find point not contained in polygon
            return FindFreePoint(near_p, polygon, out ret);
        }

        public static PathFindingError FindFreePoint(FloatPoint f, Polygon polygon, out Point? ret)
        {
            ret = null;
            Point p;

            // Try nearest point first
            p = new Point((short) Math.Floor(f.X + 0.5), (short) Math.Floor(f.Y + 0.5));

            if (Contained(p, polygon) != PolygonContainmentType.INSIDE)
            {
                ret = p;
                return PathFindingError.OK;
            }

            p = new Point((short) Math.Floor(f.X), (short) Math.Floor(f.Y));

            // Try (x, y), (x + 1, y), (x , y + 1) and (x + 1, y + 1)
            if (Contained(p, polygon) == PolygonContainmentType.INSIDE)
            {
                p.X++;
                if (Contained(p, polygon) == PolygonContainmentType.INSIDE)
                {
                    p.Y++;
                    if (Contained(p, polygon) == PolygonContainmentType.INSIDE)
                    {
                        p.X--;
                        if (Contained(p, polygon) == PolygonContainmentType.INSIDE)
                            return PathFindingError.FATAL;
                    }
                }
            }

            ret = p;
            return PathFindingError.OK;
        }

        private static PolygonContainmentType Contained(Point p, Polygon polygon)
        {
            int lcross = 0, rcross = 0;

            // Iterate over edges
            foreach (var vertex in polygon.Vertices)
            {
                Point v1 = vertex.v;
                Point v2 = vertex._next.v;

                // Flags for ray straddling left and right
                bool rstrad, lstrad;

                // Check if p is a vertex
                if (p == v1)
                    return PolygonContainmentType.ON_EDGE;

                // Check if edge straddles the ray
                rstrad = (v1.Y < p.Y) != (v2.Y < p.Y);
                lstrad = (v1.Y > p.Y) != (v2.Y > p.Y);

                if (lstrad || rstrad)
                {
                    // Compute intersection point x / xq
                    int x = v2.X * v1.Y - v1.X * v2.Y + (v1.X - v2.X) * p.Y;
                    int xq = v1.Y - v2.Y;

                    // Multiply by -1 if xq is negative (for comparison that follows)
                    if (xq < 0)
                    {
                        x = -x;
                        xq = -xq;
                    }

                    // Avoid floats by multiplying instead of dividing
                    if (rstrad && (x > xq * p.X))
                        rcross++;
                    else if (lstrad && (x < xq * p.X))
                        lcross++;
                }
            }

            // If we counted an odd number of total crossings the point is on an edge
            if ((lcross + rcross) % 2 == 1)
                return PolygonContainmentType.ON_EDGE;

            // If there are an odd number of crossings to one side the point is contained in the polygon
            if (rcross % 2 == 1)
            {
                // Invert result for contained access polygons.
                if (polygon.Type == PolygonType.CONTAINED_ACCESS)
                    return PolygonContainmentType.OUTSIDE;
                return PolygonContainmentType.INSIDE;
            }

            // Point is outside polygon. Invert result for contained access polygons
            if (polygon.Type == PolygonType.CONTAINED_ACCESS)
                return PolygonContainmentType.INSIDE;

            return PolygonContainmentType.OUTSIDE;
        }
    }

    internal partial class Kernel
    {
        private const int POLY_LAST_POINT = 0x7777;
        private const int POLY_POINT_SIZE = 4;

        private static Register kMergePoly(EngineState s, int argc, StackPtr argv)
        {
            // 3 parameters: raw polygon data, polygon list, list size
            Register polygonData = argv[0];
            List list = s._segMan.LookupList(argv[1]);

            // The size of the "work" point list SSCI uses. We use a dynamic one instead
            //reg_t listSize = args[2];

            SegmentRef pointList = s._segMan.Dereference(polygonData);
            if (!pointList.IsValid || pointList.skipByte)
            {
                Warning("kMergePoly: Polygon data pointer is invalid");
                return Register.Make(0, 0);
            }

            Node node;

# if DEBUG_MERGEPOLY
            node = s._segMan.lookupNode(list.first);
            while (node)
            {
                draw_polygon(s, node.value, 320, 190);
                node = s._segMan.lookupNode(node.succ);
            }
            Common::Point prev, first;
            prev = first = readPoint(pointList, 0);
            for (int i = 1; readPoint(pointList, i).x != 0x7777; i++)
            {
                Common::Point point = readPoint(pointList, i);
                draw_line(s, prev, point, 1, 320, 190);
                prev = point;
            }
            draw_line(s, prev, first, 1, 320, 190);
            // Update the whole screen
            g_sci._gfxScreen.copyToScreen();
            g_system.updateScreen();
            g_system.delayMillis(1000);
#endif

            // The work polygon which we're going to merge with the polygons in list
            Polygon work = new Polygon(0);

            for (int i = 0;; ++i)
            {
                Point p = ReadPoint(pointList, i);
                if (p.X == POLY_LAST_POINT)
                    break;

                Vertex vertex = new Vertex(p);
                work.Vertices.InsertAtEnd(vertex);
            }

            // TODO: Check behaviour for single-vertex polygons
            node = s._segMan.LookupNode(list.first);
            while (node != null)
            {
                Polygon polygon = ConvertPolygon(s, node.value);

                if (polygon != null)
                {
                    // CHECKME: Confirm vertex order that convert_polygon and
                    // fix_vertex_order output. For now, we re-reverse the order since
                    // convert_polygon reads the vertices reversed, and fix up head.
                    polygon.Vertices.Reverse();
                    polygon.Vertices._head = polygon.Vertices._head._next;

                    // Merge this polygon into the work polygon if there is an
                    // intersection.
                    bool intersected = MergeSinglePolygon(work, polygon);

                    // If so, flag it
                    if (intersected)
                    {
                        SciEngine.WriteSelectorValue(s._segMan, node.value,
                            o => o.type, (ushort) (polygon.Type + 0x10));
# if DEBUG_MERGEPOLY
                        debugN("Merged polygon: ");
                        // Iterate over edges
                        Vertex* vertex;
                        CLIST_FOREACH(vertex, &(work.vertices)) {
                            debugN(" (%d,%d) ", vertex.v.x, vertex.v.y);
                        }
                        debugN("\n");
#endif
                    }
                }

                node = s._segMan.LookupNode(node.succ);
            }


            // Allocate output array
            Register output = AllocateOutputArray(s._segMan, work.Vertices.Count() + 1);
            SegmentRef arrayRef = s._segMan.Dereference(output);

            // Copy work.vertices into arrayRef
            int n = 0;
            foreach (var vertex in work.Vertices)
            {
                if (vertex == work.Vertices._head || vertex.v != vertex._prev.v)
                    WritePoint(arrayRef, n++, vertex.v);
            }

            WritePoint(arrayRef, n, new Point(POLY_LAST_POINT, POLY_LAST_POINT));

# if DEBUG_MERGEPOLY
            prev = first = readPoint(arrayRef, 0);
            for (int i = 1; readPoint(arrayRef, i).x != 0x7777; i++)
            {
                Common::Point point = readPoint(arrayRef, i);
                draw_line(s, prev, point, 3, 320, 190);
                prev = point;
            }

            draw_line(s, prev, first, 3, 320, 190);

            // Update the whole screen
            g_sci._gfxScreen.copyToScreen();
            g_system.updateScreen();
            if (!g_sci._gfxPaint16)
                g_system.delayMillis(1000);

            debug("kMergePoly done");
#endif

            return output;
        }

        // Merge a single polygon into the work polygon.
        // If there is an intersection between work and polygon, this function
        // returns true, and replaces the vertex list of work by an extended version,
        // that covers polygon.
        //
        // NOTE: The strategy used matches qfg1new closely, and is a bit error-prone.
        // A more robust strategy would be inserting all intersection points directly
        // into both vertex lists as a first pass. This would make finding the merged
        // polygon a much more straightforward edge-walk, and avoid cases where SSCI's
        // algorithm mixes up the order of multiple intersections on a single edge.
        private static bool MergeSinglePolygon(Polygon work, Polygon polygon)
        {
#if DEBUG_MERGEPOLY
    const Vertex* vertex;
    debugN("work:");
    CLIST_FOREACH(vertex, &(work.vertices)) {
        debugN(" (%d,%d) ", vertex.v.x, vertex.v.y);
    }
    debugN("\n");
    debugN("poly:");
    CLIST_FOREACH(vertex, &(polygon.vertices)) {
        debugN(" (%d,%d) ", vertex.v.x, vertex.v.y);
}
    debugN("\n");
#endif
            int workSize = work.Vertices.Count();
            int polygonSize = polygon.Vertices.Count();

            int patchCount = 0;
            var patchList = new Patch[8];

            Vertex workv = work.Vertices._head;
            Vertex polyv = polygon.Vertices._head;
            for (int wi = 0; wi < workSize; ++wi, workv = workv._next)
            {
                for (int pi = 0; pi < polygonSize; ++pi, polyv = polyv._next)
                {
                    Point? intersection1;
                    Point? intersection2 = new Point();

                    bool intersects = SegSegIntersect(workv, polyv, out intersection1);
                    if (!intersects)
                        continue;

#if DEBUG_MERGEPOLY
            debug("mergePoly: intersection at work %d, poly %d", wi, pi);
#endif

                    if (IntersectDir(workv, polyv) >= 0)
                        continue;

#if DEBUG_MERGEPOLY
            debug("mergePoly: intersection in right direction");
#endif

                    int angle = 0;
                    int baseAngle = EdgeDir(workv);

                    // We now found the point where an edge of 'polygon' left 'work'.
                    // Now find the re-entry point.

                    // NOTE: The order in which this searches does not always work
                    // properly if the correct patch would only use a single partial
                    // edge of poly. Because it starts at polyv._next, it will skip
                    // the correct re-entry and proceed to the next.

                    Vertex workv2 = new Vertex();
                    Vertex polyv2 = polyv._next;

                    intersects = false;

                    int pi2, wi2 = 0;
                    for (pi2 = 0; pi2 < polygonSize; ++pi2, polyv2 = polyv2._next)
                    {
                        int newAngle = EdgeDir(polyv2);

                        int relAngle = newAngle - baseAngle;
                        if (relAngle > 180) relAngle -= 360;
                        if (relAngle < -180) relAngle += 360;

                        angle += relAngle;
                        baseAngle = newAngle;

                        workv2 = workv;
                        for (wi2 = 0; wi2 < workSize; ++wi2, workv2 = workv2._next)
                        {
                            intersects = SegSegIntersect(workv2, polyv2, out intersection2);
                            if (!intersects)
                                continue;
#if DEBUG_MERGEPOLY
                    debug("mergePoly: re-entry intersection at work %d, poly %d", (wi + wi2) % workSize, (pi + 1 + pi2) % polygonSize);
#endif

                            if (IntersectDir(workv2, polyv2) > 0)
                            {
#if DEBUG_MERGEPOLY
                        Debug("mergePoly: re-entry intersection in right direction, angle = %d", angle);
#endif
                                break; // found re-entry point
                            }
                        }

                        if (intersects)
                            break;
                    }

                    if (!intersects || angle < 0)
                        continue;


                    if (patchCount >= 8)
                        Error("kMergePoly: Too many patches");

                    // convert relative to absolute vertex indices
                    pi2 = (pi + 1 + pi2) % polygonSize;
                    wi2 = (wi + wi2) % workSize;

                    var newPatch = patchList[patchCount];
                    newPatch.indexw1 = wi;
                    newPatch.vertexw1 = workv;
                    newPatch.indexp1 = pi;
                    newPatch.vertexp1 = polyv;
                    newPatch.intersection1 = intersection1.Value;

                    newPatch.indexw2 = wi2;
                    newPatch.vertexw2 = workv2;
                    newPatch.indexp2 = pi2;
                    newPatch.vertexp2 = polyv2;
                    newPatch.intersection2 = intersection2.Value;
                    newPatch.disabled = false;

#if DEBUG_MERGEPOLY
            debug("mergePoly: adding patch at work %d, poly %d", wi, pi);
#endif

                    if (patchCount == 0)
                    {
                        patchCount++;
                        continue;
                    }

                    bool necessary = true;
                    for (int i = 0; i < patchCount; ++i)
                    {
                        if (IsPatchCovered(patchList[i], newPatch))
                        {
                            necessary = false;
                            break;
                        }
                    }

                    if (!necessary)
                        continue;

                    patchCount++;

                    if (patchCount > 1)
                    {
                        // check if this patch makes other patches superfluous
                        for (int i = 0; i < patchCount - 1; ++i)
                            if (IsPatchCovered(newPatch, patchList[i]))
                                patchList[i].disabled = true;
                    }
                }
            }


            if (patchCount == 0)
                return false; // nothing changed


            // Determine merged work by doing a walk over the edges
            // of work, crossing over to polygon when encountering a patch.

            Polygon output = new Polygon(0);

            workv = work.Vertices._head;
            for (int wi = 0; wi < workSize; ++wi, workv = workv._next)
            {
                bool covered = false;
                for (int p = 0; p < patchCount; ++p)
                {
                    if (patchList[p].disabled) continue;
                    if (IsVertexCovered(patchList[p], wi))
                    {
                        covered = true;
                        break;
                    }
                }

                if (!covered)
                {
                    // Add vertex to output
                    output.Vertices.InsertAtEnd(new Vertex(workv.v));
                }


                // CHECKME: Why is this the correct order in which to process
                // the patches? (What if two of them start on this line segment
                // in the opposite order?)

                for (int p = 0; p < patchCount; ++p)
                {
                    var patch = patchList[p];
                    if (patch.disabled) continue;
                    if (patch.indexw1 != wi) continue;
                    if (patch.intersection1 != workv.v)
                    {
                        // Add intersection point to output
                        output.Vertices.InsertAtEnd(new Vertex(patch.intersection1));
                    }

                    // Add vertices from polygon between vertexp1 (excl) and vertexp2 (incl)
                    for (polyv = patch.vertexp1._next; polyv != patch.vertexp2; polyv = polyv._next)
                        output.Vertices.InsertAtEnd(new Vertex(polyv.v));

                    output.Vertices.InsertAtEnd(new Vertex(patch.vertexp2.v));

                    if (patch.intersection2 != patch.vertexp2.v)
                    {
                        // Add intersection point to output
                        output.Vertices.InsertAtEnd(new Vertex(patch.intersection2));
                    }

                    // TODO: We could continue after the re-entry point here?
                }
            }
            // Remove last vertex if it's the same as the first vertex
            if (output.Vertices._head.v == output.Vertices._head._prev.v)
                output.Vertices.Remove(output.Vertices._head._prev);


            // Slight hack: swap vertex lists of output and work polygons.
            Core.ScummHelper.Swap(ref output.Vertices._head, ref work.Vertices._head);

            return true;
        }

        // find intersection between edges of two polygons.
        // endpoints count, except v2._next
        private static bool SegSegIntersect(Vertex v1, Vertex v2, out Point? intp)
        {
            Point a = v1.v;
            Point b = v1._next.v;
            Point c = v2.v;
            Point d = v2._next.v;

            // First handle the endpoint cases manually

            if (Collinear(a, b, c) && Collinear(a, b, d))
            {
                intp = null;
                return false;
            }

            if (Collinear(a, b, c))
            {
                // a, b, c collinear
                // return true/c if c is between a and b
                intp = c;
                if (a.X != b.X)
                {
                    if ((a.X <= c.X && c.X <= b.X) || (b.X <= c.X && c.X <= a.X))
                        return true;
                }
                else
                {
                    if ((a.Y <= c.Y && c.Y <= b.Y) || (b.Y <= c.Y && c.Y <= a.Y))
                        return true;
                }
            }

            if (Collinear(a, b, d))
            {
                intp = d;
                // a, b, d collinear
                // return false/d if d is between a and b
                if (a.X != b.X)
                {
                    if ((a.X <= d.X && d.X <= b.X) || (b.X <= d.X && d.X <= a.X))
                        return false;
                }
                else
                {
                    if ((a.Y <= d.Y && d.Y <= b.Y) || (b.Y <= d.Y && d.Y <= a.Y))
                        return false;
                }
            }

            int len_dc = (int) c.SquareDistance(d);

            if (len_dc == 0) Error("zero length edge in polygon");

            if (PointSegDistance(c, d, a) <= 2.0f)
            {
                intp = a;
                return true;
            }

            if (PointSegDistance(c, d, b) <= 2.0f)
            {
                intp = b;
                return true;
            }

            // If not an endpoint, call the generic intersection function

            FloatPoint? p;
            if (Intersection(a, b, v2, out p) == PathFindingError.OK)
            {
                intp = p.Value.ToPoint();
                return true;
            }

            intp = null;
            return false;
        }

        // ==========================================================================
        // kMergePoly utility functions

        // Compute square of the distance of p to the segment a-b.
        private static float PointSegDistance(Point a, Point b, Point p)
        {
            FloatPoint ba = new FloatPoint(b - a);
            FloatPoint pa = new FloatPoint(p - a);
            FloatPoint bp = new FloatPoint(b - p);

            // Check if the projection of p on the line a-b lies between a and b
            if (ba * pa >= 0.0f && ba * bp >= 0.0f)
            {
                // If yes, return the (squared) distance of p to the line a-b:
                // translate a to origin, project p and subtract
                float linedist = (ba * ((ba * pa) / (ba * ba)) - pa).Norm();

                return linedist;
            }
            else
            {
                // If no, return the (squared) distance to either a or b, whichever
                // is closest.

                // distance to a:
                float adist = pa.Norm();
                // distance to b:
                float bdist = new FloatPoint(p - b).Norm();

                return Math.Min(adist, bdist);
            }
        }

        // Check if the given vertex on the work polygon is bypassed by this patch.
        private static bool IsVertexCovered(Patch p, int wi)
        {
            //         /             v       (outside)
            //  ---w1--1----p----w2--2----
            //         ^             \       (inside)
            if (wi > p.indexw1 && wi <= p.indexw2)
                return true;

            //         v             /       (outside)
            //  ---w2--2----p----w1--1----
            //         \             ^       (inside)
            if (p.indexw1 > p.indexw2 && (wi <= p.indexw2 || wi > p.indexw1))
                return true;

            //         v  /                  (outside)
            //  ---w1--2--1-------p-----
            //     w2  \  ^                  (inside)
            if (p.indexw1 == p.indexw2 && LiesBefore(p.vertexw1, p.intersection1, p.intersection2) > 0)
                return true; // This patch actually covers _all_ vertices on work

            return false;
        }


        // Check if patch p1 makes patch p2 superfluous.
        private static bool IsPatchCovered(Patch p1, Patch p2)
        {
            // Same exit and entry points
            if (p1.intersection1 == p2.intersection1 && p1.intersection2 == p2.intersection2)
                return true;

            //           /         *         v       (outside)
            //  ---p1w1--1----p2w1-1---p1w2--2----
            //           ^         *         \       (inside)
            if (p1.indexw1 < p2.indexw1 && p2.indexw1 < p1.indexw2)
                return true;
            if (p1.indexw1 > p1.indexw2 && (p2.indexw1 > p1.indexw1 || p2.indexw1 < p1.indexw2))
                return true;


            //            /         *          v       (outside)
            //  ---p1w1--11----p2w2-2---p1w2--12----
            //            ^         *          \       (inside)
            if (p1.indexw1 < p2.indexw2 && p2.indexw2 < p1.indexw2)
                return true;
            if (p1.indexw1 > p1.indexw2 && (p2.indexw2 > p1.indexw1 || p2.indexw2 < p1.indexw2))
                return true;

            // Opposite of two above situations
            if (p2.indexw1 < p1.indexw1 && p1.indexw1 < p2.indexw2)
                return false;
            if (p2.indexw1 > p2.indexw2 && (p1.indexw1 > p2.indexw1 || p1.indexw1 < p2.indexw2))
                return false;

            if (p2.indexw1 < p1.indexw2 && p1.indexw2 < p2.indexw2)
                return false;
            if (p2.indexw1 > p2.indexw2 && (p1.indexw2 > p2.indexw1 || p1.indexw2 < p2.indexw2))
                return false;


            // The above checks covered the cases where one patch covers the other and
            // the intersections of the patches are on different edges.

            // So, if we passed the above checks, we have to check the order of
            // intersections on edges.


            if (p1.indexw1 != p1.indexw2)
            {
                //            /    *              v       (outside)
                //  ---p1w1--11---21--------p1w2--2----
                //     p2w1   ^    *              \       (inside)
                if (p1.indexw1 == p2.indexw1)
                    return (LiesBefore(p1.vertexw1, p1.intersection1, p2.intersection1) < 0);

                //            /                *    v       (outside)
                //  ---p1w1--11---------p1w2--21---12----
                //            ^         p2w1   *    \       (inside)
                if (p1.indexw2 == p2.indexw1)
                    return (LiesBefore(p1.vertexw2, p1.intersection2, p2.intersection1) > 0);

                // If neither of the above, then the intervals of the polygon
                // covered by patch1 and patch2 are disjoint
                return false;
            }

            // p1w1 == p1w2
            // Also, p1w1/p1w2 isn't strictly between p2


            //            v   /             *      (outside)
            //  ---p1w1--12--11-------p2w1-21----
            //     p1w2   \   ^             *      (inside)

            //            v   /   /               (outside)
            //  ---p1w1--12--21--11---------
            //     p1w2   \   ^   ^               (inside)
            //     p2w1
            if (LiesBefore(p1.vertexw1, p1.intersection1, p1.intersection2) > 0)
                return (p1.indexw1 != p2.indexw1);

            // CHECKME: This is meaningless if p2w1 != p2w2 ??
            if (LiesBefore(p2.vertexw1, p2.intersection1, p2.intersection2) > 0)
                return false;

            // CHECKME: This is meaningless if p1w1 != p2w1 ??
            if (LiesBefore(p2.vertexw1, p2.intersection1, p1.intersection1) <= 0)
                return false;

            // CHECKME: This is meaningless if p1w2 != p2w1 ??
            if (LiesBefore(p2.vertexw1, p2.intersection1, p1.intersection2) >= 0)
                return false;

            return true;
        }

        // For points p1, p2 on the polygon segment v, determine if
        // * p1 lies before p2: negative return value
        // * p1 and p2 are the same: zero return value
        // * p1 lies after p2: positive return value
        private static int LiesBefore(Vertex v, Point p1, Point p2)
        {
            return (int) (v.v.SquareDistance(p1) - v.v.SquareDistance(p2));
        }

        // For intersecting polygon segments, determine if
        // * the v2 edge enters polygon 1 at this intersection: positive return value
        // * the v2 edge and the v1 edges are parallel: zero return value
        // * the v2 edge exits polygon 1 at this intersection: negative return value
        private static int IntersectDir(Vertex v1, Vertex v2)
        {
            Point p1 = v1._next.v - v1.v;
            Point p2 = v2._next.v - v2.v;
            return (p1.X * p2.Y - p2.X * p1.Y);
        }

        // Direction of edge in degrees from pos. x-axis, between -180 and 180
        private static int EdgeDir(Vertex v)
        {
            Point p = v._next.v - v.v;
            int deg = (int) MathHelper.Rad2Deg((float) Math.Atan2((double) p.Y, (double) p.X));
            if (deg < -180) deg += 360;
            if (deg > 180) deg -= 360;
            return deg;
        }

        public static Polygon ConvertPolygon(EngineState s, Register polygon)
        {
            SegManager segMan = s._segMan;
            int i;
            var points = SciEngine.ReadSelector(segMan, polygon, o => o.points);
            uint size = SciEngine.ReadSelectorValue(segMan, polygon, o => o.size);

#if ENABLE_SCI32
            // SCI32 stores the actual points in the data property of points (in a new array)
            if (segMan.IsHeapObject(points))
                points = SciEngine.ReadSelector(segMan, points, o => o.data);
#endif

            if (size == 0)
            {
                // If the polygon has no vertices, we skip it
                return null;
            }

            SegmentRef pointList = segMan.Dereference(points);
            // Check if the target polygon is still valid. It may have been released
            // in the meantime (e.g. in LSL6, room 700, when using the elevator).
            // Refer to bug #3034501.
            if (!pointList.IsValid || pointList.skipByte)
            {
                Warning("convert_polygon: Polygon data pointer is invalid, skipping polygon");
                return null;
            }

            // Make sure that we have enough points
            if (pointList.maxSize < size * POLY_POINT_SIZE)
            {
                Warning($"convert_polygon: Not enough memory allocated for polygon points. " +
                        "Expected {size * POLY_POINT_SIZE}, got {pointList.maxSize}. Skipping polygon");
                return null;
            }

            int skip = 0;

            // WORKAROUND: broken polygon in lsl1sci, room 350, after opening elevator
            // Polygon has 17 points but size is set to 19
            if ((size == 19) && SciEngine.Instance.GameId == SciGameId.LSL1)
            {
                if ((s.CurrentRoomNumber == 350)
                    && (ReadPoint(pointList, 18) == new Point(108, 137)))
                {
                    Debug(1, "Applying fix for broken polygon in lsl1sci, room 350");
                    size = 17;
                }
            }

            Polygon poly = new Polygon((PolygonType) SciEngine.ReadSelectorValue(segMan, polygon, o => o.type));

            for (i = skip; i < size; i++)
            {
                Vertex vertex = new Vertex(ReadPoint(pointList, i));
                poly.Vertices.InsertHead(vertex);
            }

            FixVertexOrder(poly);

            return poly;
        }

        /// <summary>
        /// Fixes the vertex order of a polygon if incorrect. Contained access
        /// polygons should have their vertices ordered clockwise, all other types
        /// anti-clockwise
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        private static void FixVertexOrder(Polygon polygon)
        {
            int area = PolygonArea(polygon);

            // When the polygon area is positive the vertices are ordered
            // anti-clockwise. When the area is negative the vertices are ordered
            // clockwise
            if (((area > 0) && (polygon.Type == PolygonType.CONTAINED_ACCESS))
                || ((area < 0) && (polygon.Type != PolygonType.CONTAINED_ACCESS)))
            {
                polygon.Vertices.Reverse();
            }
        }

        /// <summary>
        /// Computes polygon area
        /// </summary>
        /// <returns>The area multiplied by two.</returns>
        /// <param name="polygon">The polygon.</param>
        private static int PolygonArea(Polygon polygon)
        {
            Vertex first = polygon.Vertices.First;
            Vertex v;
            int size = 0;

            v = first._next;

            while (v._next != first)
            {
                size += Area(first.v, v.v, v._next.v);
                v = v._next;
            }

            return size;
        }

        private static int Area(Point a, Point b, Point c)
        {
            return (b.X - a.X) * (a.Y - c.Y) - (c.X - a.X) * (a.Y - b.Y);
        }

        private static PolygonContainmentType Contained(Point p, Polygon polygon)
        {
            int lcross = 0, rcross = 0;

            // Iterate over edges
            foreach (var vertex in polygon.Vertices)
            {
                Point v1 = vertex.v;
                Point v2 = vertex._next.v;

                // Flags for ray straddling left and right
                bool rstrad, lstrad;

                // Check if p is a vertex
                if (p == v1)
                    return PolygonContainmentType.ON_EDGE;

                // Check if edge straddles the ray
                rstrad = (v1.Y < p.Y) != (v2.Y < p.Y);
                lstrad = (v1.Y > p.Y) != (v2.Y > p.Y);

                if (lstrad || rstrad)
                {
                    // Compute intersection point x / xq
                    int x = v2.X * v1.Y - v1.X * v2.Y + (v1.X - v2.X) * p.Y;
                    int xq = v1.Y - v2.Y;

                    // Multiply by -1 if xq is negative (for comparison that follows)
                    if (xq < 0)
                    {
                        x = -x;
                        xq = -xq;
                    }

                    // Avoid floats by multiplying instead of dividing
                    if (rstrad && (x > xq * p.X))
                        rcross++;
                    else if (lstrad && (x < xq * p.X))
                        lcross++;
                }
            }

            // If we counted an odd number of total crossings the point is on an edge
            if ((lcross + rcross) % 2 == 1)
                return PolygonContainmentType.ON_EDGE;

            // If there are an odd number of crossings to one side the point is contained in the polygon
            if (rcross % 2 == 1)
            {
                // Invert result for contained access polygons.
                if (polygon.Type == PolygonType.CONTAINED_ACCESS)
                    return PolygonContainmentType.OUTSIDE;
                return PolygonContainmentType.INSIDE;
            }

            // Point is outside polygon. Invert result for contained access polygons
            if (polygon.Type == PolygonType.CONTAINED_ACCESS)
                return PolygonContainmentType.INSIDE;

            return PolygonContainmentType.OUTSIDE;
        }

        /// <summary>
        /// Changes the polygon list for optimization level 0 (used for keyboard
        /// support). Totally accessible polygons are removed and near-point
        /// accessible polygons are changed into totally accessible polygons.
        /// </summary>
        /// <param name="s">The pathfinding state.</param>
        private static void ChangePolygonsOpt0(PathfindingState s)
        {
            foreach (var polygon in s.polygons.ToList())
            {
                System.Diagnostics.Debug.Assert(polygon != null);

                if (polygon.Type == PolygonType.TOTAL_ACCESS)
                {
                    s.polygons.Remove(polygon);
                }
                else
                {
                    if (polygon.Type == PolygonType.NEAREST_ACCESS)
                        polygon.Type = PolygonType.TOTAL_ACCESS;
                }
            }
        }


        private static Point ReadPoint(SegmentRef list_r, int offset)
        {
            Point point = new Point();

            if (list_r.isRaw)
            {
                // dynmem blocks are raw
                point.X = (short) list_r.raw.Data.ReadSciEndianUInt16(list_r.raw.Offset + offset * POLY_POINT_SIZE);
                point.Y = (short) list_r.raw.Data.ReadSciEndianUInt16(list_r.raw.Offset + offset * POLY_POINT_SIZE + 2);
            }
            else
            {
                point.X = list_r.reg[offset * 2].ToInt16();
                point.Y = list_r.reg[offset * 2 + 1].ToInt16();
            }
            return point;
        }

        /// <summary>
        /// Checks whether a point is nearby a contained-access polygon (distance 1 pixel)
        /// </summary>
        /// <returns>true when point is nearby polygon, false otherwise.</returns>
        /// <param name="point">the point.</param>
        /// <param name="polygon">the contained-access polygon.</param>
        private static bool NearbyPolygon(Point point, Polygon polygon)
        {
            System.Diagnostics.Debug.Assert(polygon.Type == PolygonType.CONTAINED_ACCESS);

            return (Contained(new Point(point.X, (short) (point.Y + 1)), polygon) != PolygonContainmentType.INSIDE)
                   || (Contained(new Point(point.X, (short) (point.Y - 1)), polygon) != PolygonContainmentType.INSIDE)
                   || (Contained(new Point((short) (point.X + 1), point.Y), polygon) != PolygonContainmentType.INSIDE)
                   || (Contained(new Point((short) (point.X - 1), point.Y), polygon) != PolygonContainmentType.INSIDE);
        }

        private static Point? FixupStartPoint(PathfindingState s, Point start)
        {
            Point? new_start = new Point(start);

            foreach (var it in s.polygons.ToList())
            {
                var cont = Contained(start, it);
                var type = it.Type;

                switch (type)
                {
                    case PolygonType.TOTAL_ACCESS:
                        // Remove totally accessible polygons that contain the start point
                        if (cont != PolygonContainmentType.OUTSIDE)
                        {
                            s.polygons.Remove(it);
                            continue;
                        }
                        break;
                    // Fall through
                    case PolygonType.BARRED_ACCESS:
                    case PolygonType.NEAREST_ACCESS:
                        if (type == PolygonType.CONTAINED_ACCESS)
                        {
                            // Remove contained access polygons that do not contain
                            // the start point (containment test is inverted here).
                            // SSCI appears to be using a small margin of error here,
                            // so we do the same.
                            if ((cont == PolygonContainmentType.INSIDE) && !NearbyPolygon(start, it))
                            {
                                s.polygons.Remove(it);
                                continue;
                            }
                        }
                        if (cont != PolygonContainmentType.OUTSIDE)
                        {
                            if (s._prependPoint != null)
                            {
                                // We shouldn't get here twice.
                                // We need to break in this case, otherwise we'll end in an infinite
                                // loop.
                                Warning("AvoidPath: start point is contained in multiple polygons");
                                break;
                            }

                            if (s.FindNearPoint(start, it, out new_start) != PathFindingError.OK)
                            {
                                return null;
                            }

                            if ((type == PolygonType.BARRED_ACCESS) || (type == PolygonType.CONTAINED_ACCESS))
                                DebugC(DebugLevels.AvoidPath, "AvoidPath: start position at unreachable location");

                            // The original start position is in an invalid location, so we
                            // use the moved point and add the original one to the final path
                            // later on.
                            if (start != new_start)
                                s._prependPoint = new Point(start);
                        }
                        break;
                }
            }

            return new_start;
        }

        private static Point? FixupEndPoint(PathfindingState s, Point end)
        {
            Point? new_end = new Point(end);

            foreach (var it in s.polygons.ToList())
            {
                var cont = Contained(end, it);
                var type = it.Type;

                switch (type)
                {
                    case PolygonType.TOTAL_ACCESS:
                        // Remove totally accessible polygons that contain the end point
                        if (cont != PolygonContainmentType.OUTSIDE)
                        {
                            s.polygons.Remove(it);
                            continue;
                        }
                        break;
                    case PolygonType.CONTAINED_ACCESS:
                    case PolygonType.BARRED_ACCESS:
                    case PolygonType.NEAREST_ACCESS:
                        if (cont != PolygonContainmentType.OUTSIDE)
                        {
                            if (s._appendPoint != null)
                            {
                                // We shouldn't get here twice.
                                // Happens in LB2CD, inside the speakeasy when walking from the
                                // speakeasy (room 310) into the bathroom (room 320), after having
                                // consulted the notebook (bug #3036299).
                                // We need to break in this case, otherwise we'll end in an infinite
                                // loop.
                                Warning("AvoidPath: end point is contained in multiple polygons");
                                break;
                            }

                            // The original end position is in an invalid location, so we move the point
                            if (s.FindNearPoint(end, it, out new_end) != PathFindingError.OK)
                            {
                                return null;
                            }

                            // For near-point access polygons we need to add the original end point
                            // to the path after pathfinding.
                            if ((type == PolygonType.NEAREST_ACCESS) && (end != new_end))
                                s._appendPoint = new Point(end);
                        }
                        break;
                }
            }

            return new_end;
        }


        private static PathfindingState ConvertPolygonSet(EngineState s, Register poly_list, Point start, Point end,
            int width, int height, int opt)
        {
            SegManager segMan = s._segMan;
            Polygon polygon;
            int count = 0;
            PathfindingState pf_s = new PathfindingState(width, height);

            // Convert all polygons
            if (poly_list.Segment != 0)
            {
                List list = s._segMan.LookupList(poly_list);
                Node node = s._segMan.LookupNode(list.first);

                while (node != null)
                {
                    // The node value might be null, in which case there's no polygon to parse.
                    // Happens in LB2 floppy - refer to bug #3041232
                    polygon = !node.value.IsNull ? ConvertPolygon(s, node.value) : null;

                    if (polygon != null)
                    {
                        pf_s.polygons.Add(polygon);
                        count = (int) (count + SciEngine.ReadSelectorValue(segMan, node.value, o => o.size));
                    }

                    node = s._segMan.LookupNode(node.succ);
                }
            }

            if (opt == 0)
                ChangePolygonsOpt0(pf_s);

            Point? new_start = FixupStartPoint(pf_s, start);

            if (!new_start.HasValue)
            {
                Warning("AvoidPath: Couldn't fixup start position for pathfinding");
                return null;
            }

            Point? new_end = FixupEndPoint(pf_s, end);

            if (!new_end.HasValue)
            {
                Warning("AvoidPath: Couldn't fixup end position for pathfinding");
                return null;
            }

            if (opt == 0)
            {
                // Keyboard support. Only the first edge of the path we compute
                // here matches the path returned by SSCI. This is assumed to be
                // sufficient as all known use cases only use the first two
                // vertices of the returned path.
                // Pharkas uses this mode for a secondary polygon set containing
                // rectangular polygons used to block an actor's path.

                // If we have a prepended point, we do nothing here as the
                // actor is in barred territory and should be moved outside of
                // it ASAP. This matches the behavior of SSCI.
                if (!pf_s._prependPoint.HasValue)
                {
                    // Actor position is OK, find nearest obstacle.
                    var err = NearestIntersection(pf_s, start, new_end.Value, ref new_start);

                    if (err == PathFindingError.FATAL)
                    {
                        Warning("AvoidPath: error finding nearest intersection");
                        return null;
                    }

                    if (err == PathFindingError.OK)
                        pf_s._prependPoint = new Point(start);
                }
            }
            else
            {
                // WORKAROUND LSL5 room 660. Priority glitch due to us choosing a different path
                // than SSCI. Happens when Patti walks to the control room.
                if (SciEngine.Instance.GameId == SciGameId.LSL5 && (s.CurrentRoomNumber == 660) &&
                    (new Point(67, 131) == new_start.Value) && (new Point(229, 101) == new_end.Value))
                {
                    Debug(1, "[avoidpath] Applying fix for priority problem in LSL5, room 660");
                    pf_s._prependPoint = new_start.Value;
                    new_start = new Point(77, 107);
                }
            }

            // Merge start and end points into polygon set
            pf_s.vertex_start = MergePoint(pf_s, new_start.Value);
            pf_s.vertex_end = MergePoint(pf_s, new_end.Value);

            // Allocate and build vertex index
            pf_s.vertex_index = new Vertex[count + 2];

            count = 0;

            foreach (var p in pf_s.polygons)
            {
                foreach (var vertex in p.Vertices)
                {
                    pf_s.vertex_index[count++] = vertex;
                }
            }

            pf_s.vertices = count;

            return pf_s;
        }

        /// <summary>
        /// Computes the nearest intersection point of a line segment and the polygon
        /// set. Intersection points that are reached from the inside of a polygon
        /// are ignored as are improper intersections which do not obstruct
        /// visibility
        /// </summary>
        /// <returns>PF_OK on success, PF_ERROR when no intersections were
        /// found, PF_FATAL otherwise.</returns>
        /// <param name="s">The pathfinding state.</param>
        /// <param name="p">p, q: The line segment (p, q).</param>
        /// <param name="q">p, q: The line segment (p, q).</param>
        /// <param name="ret">On success, the closest intersection point.</param>
        private static PathFindingError NearestIntersection(PathfindingState s, Point p, Point q, ref Point? ret)
        {
            FloatPoint isec = new FloatPoint();
            Polygon ipolygon = null;
            uint dist = Vertex.HUGE_DISTANCE;

            foreach (var polygon in s.polygons)
            {
                foreach (var vertex in polygon.Vertices)
                {
                    uint new_dist;
                    FloatPoint? new_isec;

                    // Check for intersection with vertex
                    if (Between(p, q, vertex.v))
                    {
                        // Skip this vertex if we hit it from the
                        // inside of the polygon
                        if (Inside(q, vertex))
                        {
                            new_isec = new FloatPoint(vertex.v.X, vertex.v.Y);
                        }
                        else
                            continue;
                    }
                    else
                    {
                        // Check for intersection with edges

                        // Skip this edge if we hit it from the
                        // inside of the polygon
                        if (!Left(vertex.v, vertex._next.v, q))
                            continue;

                        if (Intersection(p, q, vertex, out new_isec) != PathFindingError.OK)
                            continue;
                    }

                    new_dist = p.SquareDistance(new_isec.Value.ToPoint());
                    if (new_dist < dist)
                    {
                        ipolygon = polygon;
                        isec = new_isec.Value;
                        dist = new_dist;
                    }
                }
            }

            if (dist == Vertex.HUGE_DISTANCE)
            {
                return PathFindingError.ERROR;
            }

            // Find point not contained in polygon
            return PathfindingState.FindFreePoint(isec, ipolygon, out ret);
        }

        private static PathFindingError Intersection(Point a, Point b, Vertex vertex, out FloatPoint? ret)
        {
            ret = null;
            // Parameters of parametric equations
            float s, t;
            // Numerator and denominator of equations
            float num, denom;
            Point c = vertex.v;
            Point d = vertex._next.v;

            denom = a.X * (float) (d.Y - c.Y) + b.X * (float) (c.Y - d.Y) +
                    d.X * (float) (b.Y - a.Y) + c.X * (float) (a.Y - b.Y);

            if (denom == 0.0)
                // Segments are parallel, no intersection
                return PathFindingError.ERROR;

            num = a.X * (float) (d.Y - c.Y) + c.X * (float) (a.Y - d.Y) + d.X * (float) (c.Y - a.Y);

            s = num / denom;

            num = -(a.X * (float) (c.Y - b.Y) + b.X * (float) (a.Y - c.Y) + c.X * (float) (b.Y - a.Y));

            t = num / denom;

            if ((0.0 <= s) && (s <= 1.0) && (0.0 < t) && (t < 1.0))
            {
                // Intersection found
                ret = new FloatPoint(a.X + s * (b.X - a.X), a.Y + s * (b.Y - a.Y));
                return PathFindingError.OK;
            }

            return PathFindingError.ERROR;
        }

        /// <summary>
        /// Determines whether or not a line from a point to a vertex intersects the
        /// interior of the polygon, locally at that vertex
        /// </summary>
        /// <param name="p">p: The point.</param>
        /// <param name="vertex">vertex: The vertex.</param>
        private static bool Inside(Point p, Vertex vertex)
        {
            // Check that it's not a single-vertex polygon
            if (VertexHasEdges(vertex))
            {
                Point prev = vertex._prev.v;
                Point next = vertex._next.v;
                Point cur = vertex.v;

                if (Left(prev, cur, next))
                {
                    // Convex vertex, line (p, cur) intersects the inside
                    // if p is located left of both edges
                    if (Left(cur, next, p) && Left(prev, cur, p))
                        return true;
                }
                else
                {
                    // Non-convex vertex, line (p, cur) intersects the
                    // inside if p is located left of either edge
                    if (Left(cur, next, p) || Left(prev, cur, p))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether or not a point is to the left of a directed line
        /// </summary>
        /// <param name="a">a, b: The directed line (a, b).</param>
        /// <param name="b">a, b: The directed line (a, b).</param>
        /// <param name="c">true if c is to the left of (a, b), false otherwise.</param>
        private static bool Left(Point a, Point b, Point c)
        {
            return Area(a, b, c) > 0;
        }

        /// <summary>
        /// Determines whether or not a point lies on a line segment
        /// </summary>
        /// <param name="a">a, b: The line segment (a, b).</param>
        /// <param name="b">a, b: The line segment (a, b).</param>
        /// <param name="c">c: The query point.</param>
        private static bool Between(Point a, Point b, Point c)
        {
            if (!Collinear(a, b, c))
                return false;

            // Assumes a != b.
            if (a.X != b.X)
                return ((a.X <= c.X) && (c.X <= b.X)) || ((a.X >= c.X) && (c.X >= b.X));
            return ((a.Y <= c.Y) && (c.Y <= b.Y)) || ((a.Y >= c.Y) && (c.Y >= b.Y));
        }

        /// <summary>
        /// Determines whether or not three points are collinear
        /// </summary>
        /// <param name="a">a, b, c: The three points.</param>
        /// <param name="b">a, b, c: The three points.</param>
        /// <param name="c">a, b, c: The three points.</param>
        private static bool Collinear(Point a, Point b, Point c)
        {
            return Area(a, b, c) == 0;
        }

        private static Vertex MergePoint(PathfindingState s, Point v)
        {
            Vertex v_new;

            // Check for already existing vertex
            foreach (var polygon in s.polygons)
            {
                foreach (var vertex in polygon.Vertices)
                {
                    if (vertex.v == v)
                        return vertex;
                }
            }

            v_new = new Vertex(v);

            // Check for point being on an edge
            foreach (var polygon2 in s.polygons)
            {
                // Skip single-vertex polygons
                if (VertexHasEdges(polygon2.Vertices.First))
                {
                    foreach (var vertex in polygon2.Vertices)
                    {
                        Vertex next = vertex._next;

                        if (Between(vertex.v, next.v, v))
                        {
                            // Split edge by adding vertex
                            CircularVertexList.InsertAfter(vertex, v_new);
                            return v_new;
                        }
                    }
                }
            }

            // Add point as single-vertex polygon
            var polygon3 = new Polygon(PolygonType.BARRED_ACCESS);
            polygon3.Vertices.InsertHead(v_new);
            s.polygons.Insert(0, polygon3);

            return v_new;
        }

        private static bool VertexHasEdges(Vertex v)
        {
            return v != v._next;
        }

#if ENABLE_SCI32

        private static Register kInPolygon(EngineState s, int argc, StackPtr argv)
        {
            // kAvoidPath already implements this
            return kAvoidPath(s, argc, argv);
        }

#endif
    }
}