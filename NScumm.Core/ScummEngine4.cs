//
//  ScummEngine4.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using System.Collections.Generic;
using NScumm.Core.IO;
using NScumm.Core.Audio;

namespace NScumm.Core
{
    public class ScummEngine4: ScummEngine3
    {
        public ScummEngine4(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            VariableScrollScript = 27;
            VariableDebugMode = 39;
            VariableMainMenu = 50;
            VariableFixedDisk = 51;
            VariableCursorState = 52;
            VariableUserPut = 53;
            VariableTalkStringY = 54;

            Variables[VariableFixedDisk.Value] = 1;

            if (Game.Version >= 4 && Game.Version <= 5)
                Variables[VariableTalkStringY.Value] = -0x50;

            if (game.Id == "loom")
            {
                VariableNoSubtitles = 60;
            }
            else if (game.Id == "monkey")
            {
                Variables[74] = 1225;
            }
        }

        protected override void InitOpCodes()
        {
            base.InitOpCodes();

            _opCodes[0x30] = MatrixOperations;
            _opCodes[0xB0] = MatrixOperations;
            _opCodes[0x3B] = GetActorScale;
            _opCodes[0xBB] = GetActorScale;
            _opCodes[0x4C] = SoundKludge;
        }

        protected void GetActorScale()
        {
            GetResult();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var actor = Actors[act];
            SetResult((int)actor.ScaleX);
        }

        protected void SoundKludge()
        {
            var items = GetWordVarArgs();
            Sound.SoundKludge(items);
        }

        void MatrixOperations()
        {
            int a, b;

            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxFlags(a, b);
                    break;

                case 2:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, b);
                    break;

                case 3:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, (b - 1) | 0x8000);
                    break;

                case 4:
                    CreateBoxMatrixCore();
                    break;
            }
        }

        void SetBoxScale(int box, int scale)
        {
            var b = GetBoxBase(box);
            b.Scale = (ushort)scale;
        }

        protected void CreateBoxMatrixCore()
        {
            // The total number of boxes
            var num = GetNumBoxes();

            // calculate shortest paths
            var itineraryMatrix = CalcItineraryMatrix(num);

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

            var boxMatrix = new List<byte>();

            for (byte i = 0; i < num; i++)
            {
                boxMatrix.Add(0xFF);
                for (byte j = 0; j < num; j++)
                {
                    var itinerary = itineraryMatrix[i, j];
                    if (itinerary != InvalidBox)
                    {
                        boxMatrix.Add(j);
                        while (j < num - 1 && itinerary == itineraryMatrix[i, (j + 1)])
                            j++;
                        boxMatrix.Add(j);
                        boxMatrix.Add(itinerary);
                    }
                }
            }
            boxMatrix.Add(0xFF);

            _boxMatrix.Clear();
            _boxMatrix.AddRange(boxMatrix);
        }

        /// <summary>
        /// Check if two boxes are neighbors.
        /// </summary>
        /// <param name="box1nr"></param>
        /// <param name="box2nr"></param>
        /// <returns></returns>
        bool AreBoxesNeighbors(byte box1nr, byte box2nr)
        {
            Point tmp;

            if (GetBoxFlags(box1nr).HasFlag(BoxFlags.Invisible) || GetBoxFlags(box2nr).HasFlag(BoxFlags.Invisible))
                return false;

            //System.Diagnostics.Debug.Assert(_game.version >= 3);
            var box2 = GetBoxCoordinates(box1nr);
            var box = GetBoxCoordinates(box2nr);

            // Roughly, the idea of this algorithm is to search for sies of the given
            // boxes that touch each other.
            // In order to keep te code simple, we only match the upper sides;
            // then, we "rotate" the box coordinates four times each, for a total
            // of 16 comparisions.
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    // Are the "upper" sides of the boxes on a single vertical line
                    // (i.e. all share one x value) ?
                    if (box2.UpperRight.X == box2.UpperLeft.X && box.UpperLeft.X == box2.UpperLeft.X && box.UpperRight.X == box2.UpperLeft.X)
                    {
                        bool swappedBox2 = false, swappedBox1 = false;
                        if (box2.UpperRight.Y < box2.UpperLeft.Y)
                        {
                            swappedBox2 = true;
                            ScummHelper.Swap(ref box2.UpperRight.Y, ref box2.UpperLeft.Y);
                        }
                        if (box.UpperRight.Y < box.UpperLeft.Y)
                        {
                            swappedBox1 = true;
                            ScummHelper.Swap(ref box.UpperRight.Y, ref box.UpperLeft.Y);
                        }
                        if (box.UpperRight.Y < box2.UpperLeft.Y ||
                            box.UpperLeft.Y > box2.UpperRight.Y ||
                            ((box.UpperLeft.Y == box2.UpperRight.Y ||
                            box.UpperRight.Y == box2.UpperLeft.Y) && box2.UpperRight.Y != box2.UpperLeft.Y && box.UpperLeft.Y != box.UpperRight.Y))
                        {
                        }
                        else
                        {
                            return true;
                        }

                        // Swap back if necessary
                        if (swappedBox2)
                        {
                            ScummHelper.Swap(ref box2.UpperRight.Y, ref box2.UpperLeft.Y);
                        }
                        if (swappedBox1)
                        {
                            ScummHelper.Swap(ref box.UpperRight.Y, ref box.UpperLeft.Y);
                        }
                    }

                    // Are the "upper" sides of the boxes on a single horizontal line
                    // (i.e. all share one y value) ?
                    if (box2.UpperRight.Y == box2.UpperLeft.Y && box.UpperLeft.Y == box2.UpperLeft.Y && box.UpperRight.Y == box2.UpperLeft.Y)
                    {
                        var swappedBox2 = false;
                        var swappedBox1 = false;
                        if (box2.UpperRight.X < box2.UpperLeft.X)
                        {
                            swappedBox2 = true;
                            ScummHelper.Swap(ref box2.UpperRight.X, ref box2.UpperLeft.X);
                        }
                        if (box.UpperRight.X < box.UpperLeft.X)
                        {
                            swappedBox1 = true;
                            ScummHelper.Swap(ref box.UpperRight.X, ref box.UpperLeft.X);
                        }
                        if (box.UpperRight.X < box2.UpperLeft.X ||
                            box.UpperLeft.X > box2.UpperRight.X ||
                            ((box.UpperLeft.X == box2.UpperRight.X ||
                            box.UpperRight.X == box2.UpperLeft.X) && box2.UpperRight.X != box2.UpperLeft.X && box.UpperLeft.X != box.UpperRight.X))
                        {

                        }
                        else
                        {
                            return true;
                        }

                        // Swap back if necessary
                        if (swappedBox2)
                        {
                            ScummHelper.Swap(ref box2.UpperRight.X, ref box2.UpperLeft.X);
                        }
                        if (swappedBox1)
                        {
                            ScummHelper.Swap(ref box.UpperRight.X, ref box.UpperLeft.X);
                        }
                    }

                    // "Rotate" the box coordinates
                    tmp = box2.UpperLeft;
                    box2.UpperLeft = box2.UpperRight;
                    box2.UpperRight = box2.LowerRight;
                    box2.LowerRight = box2.LowerLeft;
                    box2.LowerLeft = tmp;
                }

                // "Rotate" the box coordinates
                tmp = box.UpperLeft;
                box.UpperLeft = box.UpperRight;
                box.UpperRight = box.LowerRight;
                box.LowerRight = box.LowerLeft;
                box.LowerLeft = tmp;
            }

            return false;
        }

        byte[,] CalcItineraryMatrix(int num)
        {
            const byte boxSize = 64;

            // Allocate the adjacent & itinerary matrices
            var itineraryMatrix = new byte[boxSize, boxSize];
            var adjacentMatrix = new byte[boxSize, boxSize];

            // Initialize the adjacent matrix: each box has distance 0 to itself,
            // and distance 1 to its direct neighbors. Initially, it has distance
            // 255 (= infinity) to all other boxes.
            for (byte i = 0; i < num; i++)
            {
                for (byte j = 0; j < num; j++)
                {
                    if (i == j)
                    {
                        adjacentMatrix[i, j] = 0;
                        itineraryMatrix[i, j] = j;
                    }
                    else if (AreBoxesNeighbors(i, j))
                    {
                        adjacentMatrix[i, j] = 1;
                        itineraryMatrix[i, j] = j;
                    }
                    else
                    {
                        adjacentMatrix[i, j] = 255;
                        itineraryMatrix[i, j] = InvalidBox;
                    }
                }
            }

            // Compute the shortest routes between boxes via Kleene's algorithm.
            // The original code used some kind of mangled Dijkstra's algorithm;
            // while that might in theory be slightly faster, it was
            // a) extremly obfuscated
            // b) incorrect: it didn't always find the shortest paths
            // c) not any faster in reality for our sparse & small adjacent matrices
            for (byte k = 0; k < num; k++)
            {
                for (byte i = 0; i < num; i++)
                {
                    for (byte j = 0; j < num; j++)
                    {
                        if (i == j)
                            continue;
                        byte distIK = adjacentMatrix[i, k];
                        byte distKJ = adjacentMatrix[k, j];
                        if (adjacentMatrix[i, j] > distIK + distKJ)
                        {
                            adjacentMatrix[i, j] = (byte)(distIK + distKJ);
                            itineraryMatrix[i, j] = itineraryMatrix[i, k];
                        }
                    }
                }
            }

            return itineraryMatrix;
        }
    }
}

