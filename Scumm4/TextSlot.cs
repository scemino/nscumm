using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Slot
    {
        public short xpos = 2;
        public short ypos = 5;
        public short right = 319;
        public short height = 0;
        public byte color = 0xF;
        public byte charset = 4;
        public bool center = false;
        public bool overhead;
        public bool no_talk_anim;
        public bool wrapping;

        public void CopyFrom(Slot s)
        {
            this.xpos = s.xpos;
            this.ypos = s.ypos;
            this.right = s.right;
            this.color = s.color;
            this.charset = s.charset;
            this.center = s.center;
            this.overhead = s.overhead;
            this.no_talk_anim = s.no_talk_anim;
            this.wrapping = s.wrapping;
        }
    }

    public class TextSlot : Slot
    {
        private Slot _default = new Slot();

        public Slot Default
        {
            get { return _default; }
        }

        public void SaveDefault()
        {
            _default.CopyFrom(this);
        }

        public void LoadDefault()
        {
            CopyFrom(_default);
        }
    }
}
