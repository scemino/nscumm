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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    /** Camera modes */
    public enum CameraMode
    {
        Normal = 1,
        FollowActor = 2,
        Panning = 3
    }

    public class Camera
    {
        public Point _cur;
        public Point _dest;
        public Point _accel;
        public Point _last;
        public int _leftTrigger=10, _rightTrigger=30;
        public byte _follows;
        public CameraMode _mode;
        public bool _movingToActor;
    }
}
