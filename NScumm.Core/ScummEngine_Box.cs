//
//  ScummEngine_Box.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using NScumm.Core.Graphics;

namespace NScumm.Core
{
	partial class ScummEngine
	{
		List<byte> _boxMatrix = new List<byte> ();
		Box[] _boxes;
		ScaleSlot[] _scaleSlots;

		void MatrixOperations ()
		{
			int a, b;

			_opCode = ReadByte ();
			switch (_opCode & 0x1F) {
			case 1:
				a = GetVarOrDirectByte (OpCodeParameter.Param1);
				b = GetVarOrDirectByte (OpCodeParameter.Param2);
				SetBoxFlags (a, b);
				break;

			case 2:
				a = GetVarOrDirectByte (OpCodeParameter.Param1);
				b = GetVarOrDirectByte (OpCodeParameter.Param2);
				SetBoxScale (a, b);
				break;

			case 3:
				a = GetVarOrDirectByte (OpCodeParameter.Param1);
				b = GetVarOrDirectByte (OpCodeParameter.Param2);
				SetBoxScale (a, (b - 1) | 0x8000);
				break;

			case 4:
				CreateBoxMatrix ();
				break;
			}
		}

		void CreateBoxMatrix ()
		{
			// The total number of boxes
			int num = GetNumBoxes ();

			// calculate shortest paths
			var itineraryMatrix = CalcItineraryMatrix (num);

			// "Compress" the distance matrix into the box matrix format used
			// by the engine. The format is like this:
			// For each box (from 0 to num) there is first a byte with value 0xFF,
			// followed by an arbitrary number of byte triples; the end is marked
			// again by the lead 0xFF for the next "row". The meaning of the
			// byte triples is as follows: the first two bytes define a range
			// of box numbers (e.g. 7-11), while the third byte defines an
			// itineray box. Assuming we are in the 5th "row" and encounter
			// the triplet 7,11,15: this means to get from box 5 to any of
			// the boxes 7,8,9,10,11 the shortest way is to go via box 15.
			// See also getNextBox.

			var boxMatrix = new List<byte> ();

			for (byte i = 0; i < num; i++) {
				boxMatrix.Add (0xFF);
				for (byte j = 0; j < num; j++) {
					byte itinerary = itineraryMatrix [i, j];
					if (itinerary != Actor.InvalidBox) {
						boxMatrix.Add (j);
						while (j < num - 1 && itinerary == itineraryMatrix [i, (j + 1)])
							j++;
						boxMatrix.Add (j);
						boxMatrix.Add (itinerary);
					}
				}
			}
			boxMatrix.Add (0xFF);

			_boxMatrix.Clear ();
			_boxMatrix.AddRange (boxMatrix);
		}

		internal BoxFlags GetBoxFlags (byte boxNum)
		{
			var box = GetBoxBase (boxNum);
			if (box == null)
				return 0;
			return box.Flags;
		}

		internal byte GetBoxMask (byte boxNum)
		{
			Box box = GetBoxBase (boxNum);
			if (box == null)
				return 0;
			return box.Mask;
		}

		internal int GetNumBoxes ()
		{
			return _boxes.Length;
		}

		internal BoxCoords GetBoxCoordinates (int boxnum)
		{
			var bp = GetBoxBase (boxnum);
			var box = new BoxCoords ();

			box.Ul.X = bp.Ulx;
			box.Ul.Y = bp.Uly;
			box.Ur.X = bp.Urx;
			box.Ur.Y = bp.Ury;

			box.Ll.X = bp.Llx;
			box.Ll.Y = bp.Lly;
			box.Lr.X = bp.Lrx;
			box.Lr.Y = bp.Lry;

			return box;
		}

		Box GetBoxBase (int boxnum)
		{
			if (boxnum == 255)
				return null;

			// As a workaround, we simply use the last box if the last+1 box is requested.
			// Note that this may cause different behavior than the original game
			// engine exhibited! To faithfully reproduce the behavior of the original
			// engine, we would have to know the data coming *after* the walkbox table.
			if (_boxes.Length == boxnum)
				boxnum--;
			return _boxes [boxnum];
		}

		/// <summary>
		/// Compute if there is a way that connects box 'from' with box 'to'.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>
		/// The number of a box adjacent to 'from' that is the next on the
		/// way to 'to' (this can be 'to' itself or a third box).
		/// If there is no connection -1 is return.
		/// </returns>
		internal int GetNextBox (byte from, byte to)
		{
			byte i;
			int numOfBoxes = GetNumBoxes ();
			int dest = -1;

			if (from == to)
				return to;

			if (to == Actor.InvalidBox)
				return -1;

			if (from == Actor.InvalidBox)
				return to;

			if (from >= numOfBoxes)
				throw new ArgumentOutOfRangeException ("from");
			if (to >= numOfBoxes)
				throw new ArgumentOutOfRangeException ("to");

			var boxm = _boxMatrix;

			// WORKAROUND #1: It seems that in some cases, the box matrix is corrupt
			// (more precisely, is too short) in the datafiles already. In
			// particular this seems to be the case in room 46 of Indy3 EGA (see
			// also bug #770690). This didn't cause problems in the original
			// engine, because there, the memory layout is different. After the
			// walkbox would follow the rest of the room file, thus the program
			// always behaved the same (and by chance, correct). Not so for us,
			// since random data may follow after the resource in ScummVM.
			//
			// As a workaround, we add a check for the end of the box matrix
			// resource, and abort the search once we reach the end.

			int boxmIndex = _boxMatrix [0] == 0xFF ? 1 : 0;
			// Skip up to the matrix data for box 'from'
			for (i = 0; i < from && boxmIndex < boxm.Count; i++) {
				while (boxmIndex < boxm.Count && boxm [boxmIndex] != 0xFF)
					boxmIndex += 3;
				boxmIndex++;
			}

			// Now search for the entry for box 'to'
			while (boxmIndex < boxm.Count && boxm [boxmIndex] != 0xFF) {
				if (boxm [boxmIndex] <= to && to <= boxm [boxmIndex + 1])
					dest = (sbyte)boxm [boxmIndex + 2];
				boxmIndex += 3;
			}

			//if (boxm >= boxm.Count)
			//    debug(0, "The box matrix apparently is truncated (room %d)", _roomResource);

			return dest;
		}

		internal bool CheckXYInBoxBounds (int boxnum, Point p)
		{
			// Since this method is called by many other methods that take params
			// from e.g. script opcodes, but do not validate the boxnum, we
			// make a check here to filter out invalid boxes.
			// See also bug #1599113.
			if (boxnum < 0 || boxnum == Actor.InvalidBox)
				return false;

			var box = GetBoxCoordinates (boxnum);

			// Quick check: If the x (resp. y) coordinate of the point is
			// strictly smaller (bigger) than the x (y) coordinates of all
			// corners of the quadrangle, then it certainly is *not* contained
			// inside the quadrangle.
			if (p.X < box.Ul.X && p.X < box.Ur.X && p.X < box.Lr.X && p.X < box.Ll.X)
				return false;

			if (p.X > box.Ul.X && p.X > box.Ur.X && p.X > box.Lr.X && p.X > box.Ll.X)
				return false;

			if (p.Y < box.Ul.Y && p.Y < box.Ur.Y && p.Y < box.Lr.Y && p.Y < box.Ll.Y)
				return false;

			if (p.Y > box.Ul.Y && p.Y > box.Ur.Y && p.Y > box.Lr.Y && p.Y > box.Ll.Y)
				return false;

			// Corner case: If the box is a simple line segment, we consider the
			// point to be contained "in" (or rather, lying on) the line if it
			// is very close to its projection to the line segment.
			if ((box.Ul == box.Ur && box.Lr == box.Ll) ||
				(box.Ul == box.Ll && box.Ur == box.Lr)) {
				Point tmp;
				tmp = ScummMath.ClosestPtOnLine (box.Ul, box.Lr, p);
				if (p.SquareDistance (tmp) <= 4)
					return true;
			}

			// Finally, fall back to the classic algorithm to compute containment
			// in a convex polygon: For each (oriented) side of the polygon
			// (quadrangle in this case), compute whether p is "left" or "right"
			// from it.

			if (!ScummMath.CompareSlope (box.Ul, box.Ur, p))
				return false;

			if (!ScummMath.CompareSlope (box.Ur, box.Lr, p))
				return false;

			if (!ScummMath.CompareSlope (box.Lr, box.Ll, p))
				return false;

			if (!ScummMath.CompareSlope (box.Ll, box.Ul, p))
				return false;

			return true;
		}

		byte[,] CalcItineraryMatrix (int num)
		{
			const byte boxSize = 64;

			// Allocate the adjacent & itinerary matrices
			var itineraryMatrix = new byte[boxSize, boxSize];
			var adjacentMatrix = new byte[boxSize, boxSize];

			// Initialize the adjacent matrix: each box has distance 0 to itself,
			// and distance 1 to its direct neighbors. Initially, it has distance
			// 255 (= infinity) to all other boxes.
			for (byte i = 0; i < num; i++) {
				for (byte j = 0; j < num; j++) {
					if (i == j) {
						adjacentMatrix [i, j] = 0;
						itineraryMatrix [i, j] = j;
					} else if (AreBoxesNeighbors (i, j)) {
						adjacentMatrix [i, j] = 1;
						itineraryMatrix [i, j] = j;
					} else {
						adjacentMatrix [i, j] = 255;
						itineraryMatrix [i, j] = Actor.InvalidBox;
					}
				}
			}

			// Compute the shortest routes between boxes via Kleene's algorithm.
			// The original code used some kind of mangled Dijkstra's algorithm;
			// while that might in theory be slightly faster, it was
			// a) extremly obfuscated
			// b) incorrect: it didn't always find the shortest paths
			// c) not any faster in reality for our sparse & small adjacent matrices
			for (byte k = 0; k < num; k++) {
				for (byte i = 0; i < num; i++) {
					for (byte j = 0; j < num; j++) {
						if (i == j)
							continue;
						byte distIK = adjacentMatrix [i, k];
						byte distKJ = adjacentMatrix [k, j];
						if (adjacentMatrix [i, j] > distIK + distKJ) {
							adjacentMatrix [i, j] = (byte)(distIK + distKJ);
							itineraryMatrix [i, j] = itineraryMatrix [i, k];
						}
					}
				}
			}

			return itineraryMatrix;
		}

		/// <summary>
		/// Check if two boxes are neighbors.
		/// </summary>
		/// <param name="box1nr"></param>
		/// <param name="box2nr"></param>
		/// <returns></returns>
		bool AreBoxesNeighbors (byte box1nr, byte box2nr)
		{
			Point tmp;

			if (GetBoxFlags (box1nr).HasFlag (BoxFlags.Invisible) || GetBoxFlags (box2nr).HasFlag (BoxFlags.Invisible))
				return false;

			//System.Diagnostics.Debug.Assert(_game.version >= 3);
			var box2 = GetBoxCoordinates (box1nr);
			var box = GetBoxCoordinates (box2nr);

			// Roughly, the idea of this algorithm is to search for sies of the given
			// boxes that touch each other.
			// In order to keep te code simple, we only match the upper sides;
			// then, we "rotate" the box coordinates four times each, for a total
			// of 16 comparisions.
			for (int j = 0; j < 4; j++) {
				for (int k = 0; k < 4; k++) {
					// Are the "upper" sides of the boxes on a single vertical line
					// (i.e. all share one x value) ?
					if (box2.Ur.X == box2.Ul.X && box.Ul.X == box2.Ul.X && box.Ur.X == box2.Ul.X) {
						bool swappedBox2 = false, swappedBox1 = false;
						if (box2.Ur.Y < box2.Ul.Y) {
							swappedBox2 = true;
							ScummHelper.Swap (ref box2.Ur.Y, ref box2.Ul.Y);
						}
						if (box.Ur.Y < box.Ul.Y) {
							swappedBox1 = true;
							ScummHelper.Swap (ref box.Ur.Y, ref box.Ul.Y);
						}
						if (box.Ur.Y < box2.Ul.Y ||
							box.Ul.Y > box2.Ur.Y ||
							((box.Ul.Y == box2.Ur.Y ||
								box.Ur.Y == box2.Ul.Y) && box2.Ur.Y != box2.Ul.Y && box.Ul.Y != box.Ur.Y)) {
						} else {
							return true;
						}

						// Swap back if necessary
						if (swappedBox2) {
							ScummHelper.Swap (ref box2.Ur.Y, ref box2.Ul.Y);
						}
						if (swappedBox1) {
							ScummHelper.Swap (ref box.Ur.Y, ref box.Ul.Y);
						}
					}

					// Are the "upper" sides of the boxes on a single horizontal line
					// (i.e. all share one y value) ?
					if (box2.Ur.Y == box2.Ul.Y && box.Ul.Y == box2.Ul.Y && box.Ur.Y == box2.Ul.Y) {
						var swappedBox2 = false;
						var swappedBox1 = false;
						if (box2.Ur.X < box2.Ul.X) {
							swappedBox2 = true;
							ScummHelper.Swap (ref box2.Ur.X, ref box2.Ul.X);
						}
						if (box.Ur.X < box.Ul.X) {
							swappedBox1 = true;
							ScummHelper.Swap (ref box.Ur.X, ref box.Ul.X);
						}
						if (box.Ur.X < box2.Ul.X ||
							box.Ul.X > box2.Ur.X ||
							((box.Ul.X == box2.Ur.X ||
								box.Ur.X == box2.Ul.X) && box2.Ur.X != box2.Ul.X && box.Ul.X != box.Ur.X)) {

						} else {
							return true;
						}

						// Swap back if necessary
						if (swappedBox2) {
							ScummHelper.Swap (ref box2.Ur.X, ref box2.Ul.X);
						}
						if (swappedBox1) {
							ScummHelper.Swap (ref box.Ur.X, ref box.Ul.X);
						}
					}

					// "Rotate" the box coordinates
					tmp = box2.Ul;
					box2.Ul = box2.Ur;
					box2.Ur = box2.Lr;
					box2.Lr = box2.Ll;
					box2.Ll = tmp;
				}

				// "Rotate" the box coordinates
				tmp = box.Ul;
				box.Ul = box.Ur;
				box.Ur = box.Lr;
				box.Lr = box.Ll;
				box.Ll = tmp;
			}

			return false;
		}

		void SetBoxScale (int box, int scale)
		{
			var b = GetBoxBase (box);
			b.Scale = (ushort)scale;
		}

		void SetBoxFlags (int box, int val)
		{
			var b = GetBoxBase (box);
			if (b == null)
				return;
			b.Flags = (BoxFlags)val;
		}

		public int GetBoxScale (byte boxNum)
		{
			var box = GetBoxBase (boxNum);
			if (box == null)
				return 255;
			return box.Scale;
		}

		public int GetScale (int boxNum, short x, short y)
		{
			var box = GetBoxBase (boxNum);
			if (box == null)
				return 255;

			int scale = (int)box.Scale;
			int slot = 0;
			if ((scale & 0x8000) != 0)
				slot = (scale & 0x7FFF) + 1;

			// Was a scale slot specified? If so, we compute the effective scale
			// from it, ignoring the box scale.
			if (slot != 0)
				scale = GetScaleFromSlot (slot, x, y);

			return scale;
		}

		public int GetScaleFromSlot (int slot, int x, int y)
		{
			int scale;
			int scaleX;
			int scaleY = 0;
			var s = _scaleSlots [slot - 1];

			//if (s.y1 == s.y2 && s.x1 == s.x2)
			//    throw new NotSupportedException(string.Format("Invalid scale slot {0}", slot));

			if (s.Y1 != s.Y2) {
				if (y < 0)
					y = 0;

				scaleY = (s.Scale2 - s.Scale1) * (y - s.Y1) / (s.Y2 - s.Y1) + s.Scale1;
			}
			if (s.X1 == s.X2) {
				scale = scaleY;
			} else {
				scaleX = (s.Scale2 - s.Scale1) * (x - s.X1) / (s.X2 - s.X1) + s.Scale1;

				if (s.Y1 == s.Y2) {
					scale = scaleX;
				} else {
					scale = (scaleX + scaleY) / 2;
				}
			}

			// Clip the scale to range 1-255
			if (scale < 1)
				scale = 1;
			else if (scale > 255)
				scale = 255;

			return scale;
		}

		void SetScaleSlot (int slot, int x1, int y1, int scale1, int x2, int y2, int scale2)
		{
			if (slot < 1)
				throw new ArgumentOutOfRangeException ("slot", slot, "Invalid scale slot");
			if (slot > _scaleSlots.Length)
				throw new ArgumentOutOfRangeException ("slot", slot, "Invalid scale slot");
			_scaleSlots [slot - 1] = new ScaleSlot { X1 = x1, X2 = x2, Y1 = y1, Y2 = y2, Scale1 = scale1, Scale2 = scale2 };
		}
	}
}

