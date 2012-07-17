using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public enum ScummMouseButtonState
    {
        MBS_LEFT_CLICK = 0x8000,
        MBS_RIGHT_CLICK = 0x4000,
        MBS_MOUSE_MASK = (MBS_LEFT_CLICK | MBS_RIGHT_CLICK),
        MBS_MAX_KEY = 0x0200
    }
}
