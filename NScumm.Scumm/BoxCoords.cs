/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */


using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
	/// <summary>
	/// Box coordinates.
	/// </summary>
	public class BoxCoords
	{
		public Point UpperLeft;
		public Point UpperRight;
		public Point LowerLeft;
		public Point LowerRight;

		public bool InBoxQuickReject (Point p, int threshold)
		{
			int t = p.X - threshold;
			if (t > UpperLeft.X && t > UpperRight.X && t > LowerRight.X && t > LowerLeft.X)
				return true;

			t = p.X + threshold;
			if (t < UpperLeft.X && t < UpperRight.X && t < LowerRight.X && t < LowerLeft.X)
				return true;

			t = p.Y - threshold;
			if (t > UpperLeft.Y && t > UpperRight.Y && t > LowerRight.Y && t > LowerLeft.Y)
				return true;

			t = p.Y + threshold;
			if (t < UpperLeft.Y && t < UpperRight.Y && t < LowerRight.Y && t < LowerLeft.Y)
				return true;

			return false;
		}
	}
}
