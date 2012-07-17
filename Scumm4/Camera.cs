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
        public int _leftTrigger, _rightTrigger;
        public byte _follows;
        public CameraMode _mode;
        public bool _movingToActor;
    }
}
