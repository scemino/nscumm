using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public enum VerbType
    {
        Text = 0,
        Image = 1
    }

    public class VerbSlot
    {
        public System.Windows.Rect curRect;
        public System.Windows.Rect oldRect;
        public ushort verbid;
        public byte color, hicolor, dimcolor, bkcolor;
        public VerbType type;
        public byte charset_nr, curmode;
        public ushort saveid;
        public byte key;
        public bool center;
        public byte prep;
        public ushort imgindex;

        public string Text { get; set; }
    }

    
}
