using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class CostumeAnimationLimb
    {
        private List<AnimPict> pictures;

        public ushort Start { get; set; }

        public ushort End { get; set; }

        public bool NoLoop { get; set; }

        public int Command { get; set; }

        public List<AnimPict> Pictures
        {
            get { return pictures; }
        }

        public CostumeAnimationLimb()
        {
            pictures = new List<AnimPict>();
        }
    }
    
    public class CostumeAnimation
    {
        private IList<CostumeAnimationLimb> frames;
        private ushort stopped;

        public ushort Stopped
        {
            get { return stopped; }
        }

        public IList<CostumeAnimationLimb> Limbs
        {
            get { return frames; }
        }

        public CostumeAnimation(IList<CostumeAnimationLimb> frames, ushort stopped)
        {
            this.frames = frames;
            this.stopped = stopped;
        }        
    }
}
