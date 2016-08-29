//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

namespace NScumm.Sci.Engine
{
    internal class SelectorRemap
    {
        public SciVersion minVersion;
        public SciVersion maxVersion;
        public string name;
        public uint slot;
    }

    internal static class Selectors
    {
        public static readonly SelectorRemap[] sciSelectorRemap = {
            new SelectorRemap { minVersion = SciVersion.V0_EARLY, maxVersion = SciVersion.V0_LATE, name =  "moveDone",  slot= 170 },
            new SelectorRemap { minVersion = SciVersion.V0_EARLY, maxVersion = SciVersion.V0_LATE, name =    "points",  slot= 316 },
            new SelectorRemap { minVersion = SciVersion.V0_EARLY, maxVersion = SciVersion.V0_LATE, name =     "flags",  slot= 368 },
            new SelectorRemap { minVersion = SciVersion.V1_EARLY, maxVersion = SciVersion.V1_LATE, name =   "nodePtr",  slot=  44 },
            new SelectorRemap { minVersion = SciVersion.V1_LATE,  maxVersion = SciVersion.V1_LATE, name ="cantBeHere",  slot=  57 },
            new SelectorRemap { minVersion = SciVersion.V1_EARLY, maxVersion = SciVersion.V1_LATE, name = "topString",  slot= 101 },
            new SelectorRemap { minVersion = SciVersion.V1_EARLY, maxVersion = SciVersion.V1_LATE, name =     "flags",  slot= 102 },
	        // SCI1.1
	        new SelectorRemap { minVersion =          SciVersion.V1_1,maxVersion =         SciVersion.V1_1, name =    "nodePtr",slot =   41 },
            new SelectorRemap { minVersion =          SciVersion.V1_1,maxVersion =         SciVersion.V1_1, name = "cantBeHere",slot =   54 },
	        // The following are not really needed. They've only been defined to
	        // ease game debugging.
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name =    "-objID-",slot = 4096 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name =     "-size-",slot = 4097 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name = "-propDict-",slot = 4098 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name = "-methDict-",slot = 4099 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name = "-classScript-", slot = 4100 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name =   "-script-", slot = 4101 },
            new SelectorRemap { minVersion =          SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE,name =    "-super-", slot = 4102 },
	        //
	        new SelectorRemap {minVersion =           SciVersion.V1_1, maxVersion =        SciVersion.V2_1_LATE, name =     "-info-", slot = 4103 },
            new SelectorRemap {minVersion =    SciVersion.NONE,       maxVersion =       SciVersion.NONE, name =           null, slot = 0 }
        };
    }
}
