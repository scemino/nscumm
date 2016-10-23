//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
    internal class SciVersionRange
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public static readonly SciVersionRange SIG_EVERYWHERE = new SciVersionRange {forPlatform = Kernel.SIGFOR_ALL};

        public SciVersionRange()
        {
        }

        public SciVersionRange(SciVersion fromVersion, SciVersion toVersion, byte forPlatform)
        {
            this.fromVersion = fromVersion;
            this.toVersion = toVersion;
            this.forPlatform = forPlatform;
        }

        public static SciVersionRange SIG_SCI0(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.V01,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI1(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_EGA_ONLY,
                toVersion = SciVersion.V1_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI11(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_1,
                toVersion = SciVersion.V1_1,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI11(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_1,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI2(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI21EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V2_1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_THRU_SCI21EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_THRU_SCI21MID(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_MIDDLE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_UNTIL_SCI21EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_UNTIL_SCI21MID(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_MIDDLE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21MID(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_MIDDLE,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21LATE(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_LATE,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI16(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.V1_1,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI32(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI3(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V3,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCIALL(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI0(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V0_EARLY,
                toVersion = SciVersion.V0_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI1EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_EARLY,
                toVersion = SciVersion.V1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI1LATE(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_LATE,
                toVersion = SciVersion.V1_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI21()
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V3
            };
        }
    }
}