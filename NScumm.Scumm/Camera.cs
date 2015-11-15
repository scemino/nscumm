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
	/// Camera mode.
	/// </summary>
	enum CameraMode
	{
		Normal = 1,
		FollowActor = 2,
		Panning = 3
	}

	class Camera
	{
		public Point CurrentPosition;
		public Point DestinationPosition;
		public Point Accel;
		public Point LastPosition;
        public int LeftTrigger;
        public int RightTrigger;
		public byte ActorToFollow;

		public CameraMode Mode { get; set; }

		public bool MovingToActor { get; set; }
	}
}
