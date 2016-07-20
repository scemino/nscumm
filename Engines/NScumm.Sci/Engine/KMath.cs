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

using System;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kAbs(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, (ushort)Math.Abs(argv.Value[0].ToInt16()));
        }

        private static Register kCosDiv(EngineState s, int argc, StackPtr? argv)
        {
            int angle = argv.Value[0].ToInt16();
            int value = argv.Value[1].ToInt16();
            double cosval = Math.Cos(angle * Math.PI / 180.0);

            if ((cosval < 0.0001) && (cosval > -0.0001))
            {
                throw new InvalidOperationException("kCosDiv: Attempted division by zero");
            }
            else
                return Register.Make(0, (ushort)(value / cosval));
        }

        private static Register kGetAngle(EngineState s, int argc, StackPtr? argv)
        {
            // Based on behavior observed with a test program created with
            // SCI Studio.
            short x1 = argv.Value[0].ToInt16();
            short y1 = argv.Value[1].ToInt16();
            short x2 = argv.Value[2].ToInt16();
            short y2 = argv.Value[3].ToInt16();

            return Register.Make(0, kGetAngleWorker(x1, y1, x2, y2));
        }

        private static Register kGetDistance(EngineState s, int argc, StackPtr? argv)
        {
            int xdiff = (argc > 3) ? argv.Value[3].ToInt16() : 0;
            int ydiff = (argc > 2) ? argv.Value[2].ToInt16() : 0;
            int angle = (argc > 5) ? argv.Value[5].ToInt16() : 0;
            int xrel = (int)(((float)argv.Value[1].ToInt16() - xdiff) / Math.Cos(angle * Math.PI / 180.0)); // This works because cos(0)==1
            int yrel = argv.Value[0].ToInt16() - ydiff;
            return Register.Make(0, (ushort)(short)Math.Sqrt((float)xrel * xrel + yrel * yrel));
        }

        private static Register kRandom(EngineState s, int argc, StackPtr? argv)
        {
            switch (argc)
            {
                case 1: // set seed to argv[0]
                        // SCI0/SCI01 just reset the seed to 0 instead of using argv[0] at all
                    return Register.NULL_REG;

                case 2:
                    { // get random number
                      // numbers are definitely unsigned, for example lsl5 door code in k rap radio is random
                      //  and 5-digit - we get called kRandom(10000, 65000)
                      //  some codes in sq4 are also random and 5 digit (if i remember correctly)
                        ushort fromNumber = argv.Value[0].ToUInt16();
                        ushort toNumber = argv.Value[1].ToUInt16();
                        // Some scripts may request a range in the reverse order (from largest
                        // to smallest). An example can be found in Longbow, room 710, where a
                        // random number is requested from 119 to 83. In this case, we're
                        // supposed to return toNumber (determined by the KQ5CD disasm).
                        // Fixes bug #3413020.
                        if (fromNumber > toNumber)
                            return Register.Make(0, toNumber);

                        ushort range = (ushort)(toNumber - fromNumber + 1);
                        // calculating range is exactly how sierra sci did it and is required for hoyle 4
                        //  where we get called with kRandom(0, -1) and we are supposed to give back values from 0 to 0
                        //  the returned value will be used as displace-offset for a background cel
                        //  note: i assume that the hoyle4 code is actually buggy and it was never fixed because of
                        //         the way sierra sci handled it - "it just worked". It should have called kRandom(0, 0)
                        if (range != 0)
                            range--; // the range value was never returned, our random generator gets 0->range, so fix it

                        int randomNumber = fromNumber + (int)SciEngine.Instance.Rng.GetRandomNumber(range);
                        return Register.Make(0, (ushort)randomNumber);
                    }

                case 3: // get seed
                        // SCI0/01 did not support this at all
                        // Actually we would have to return the previous seed
                    throw new InvalidOperationException("kRandom: scripts asked for previous seed");

                default:
                    throw new InvalidOperationException("kRandom: unsupported argc");
            }
        }

        private static Register kSinDiv(EngineState s, int argc, StackPtr? argv)
        {
            int angle = argv.Value[0].ToInt16();
            int value = argv.Value[1].ToInt16();
            double sinval = Math.Sin(angle * Math.PI / 180.0);

            if ((sinval < 0.0001) && (sinval > -0.0001))
            {
                throw new InvalidOperationException("kSinDiv: Attempted division by zero");
            }
            else
                return Register.Make(0, (ushort)(value / sinval));
        }

        private static Register kSqrt(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, (ushort)Math.Sqrt(Math.Abs(argv.Value[0].ToInt16())));
        }

        private static Register kTimesCos(EngineState s, int argc, StackPtr? argv)
        {
            int angle = argv.Value[0].ToInt16();
            int factor = argv.Value[1].ToInt16();

            return Register.Make(0, (ushort)(factor * Math.Cos(angle * Math.PI / 180.0)));
        }

        private static Register kTimesCot(EngineState s, int argc, StackPtr? argv)
        {
            int param = argv.Value[0].ToInt16();
            int scale = (argc > 1) ? argv.Value[1].ToInt16() : 1;

            if ((param % 90) == 0)
            {
                throw new InvalidOperationException("kTimesCot: Attempted tan(pi/2)");
            }
            else
                return Register.Make(0, (ushort)(Math.Tan(param * Math.PI / 180.0) * scale));
        }

        private static Register kTimesSin(EngineState s, int argc, StackPtr? argv)
        {
            int angle = argv.Value[0].ToInt16();
            int factor = argv.Value[1].ToInt16();

            return Register.Make(0, (ushort)(factor * Math.Sin(angle * Math.PI / 180.0)));
        }

        private static Register kTimesTan(EngineState s, int argc, StackPtr? argv)
        {
            int param = argv.Value[0].ToInt16();
            int scale = (argc > 1) ? argv.Value[1].ToInt16() : 1;

            param -= 90;
            if ((param % 90) == 0)
            {
                throw new InvalidOperationException("kTimesTan: Attempted tan(pi/2)");
            }
            else
                return Register.Make(0, (ushort)-(Math.Tan(param * Math.PI / 180.0) * scale));
        }


        private static ushort kGetAngle_SCI0(short x1, short y1, short x2, short y2)
        {
            short xRel = (short)(x2 - x1);
            short yRel = (short)(y1 - y2); // y-axis is mirrored.
            short angle;

            // Move (xrel, yrel) to first quadrant.
            if (y1 < y2)
                yRel = (short)-yRel;
            if (x2 < x1)
                xRel = (short)-xRel;

            // Compute angle in grads.
            if (yRel == 0 && xRel == 0)
                return 0;
            else
                angle = (short)(100 * xRel / (xRel + yRel));

            // Fix up angle for actual quadrant of (xRel, yRel).
            if (y1 < y2)
                angle = (short)(200 - angle);
            if (x2 < x1)
                angle = (short)(400 - angle);

            // Convert from grads to degrees by merging grad 0 with grad 1,
            // grad 10 with grad 11, grad 20 with grad 21, etc. This leads to
            // "degrees" that equal either one or two grads.
            angle -= (short)((angle + 9) / 10);
            return (ushort)angle;
        }

        /// <summary>
        /// Returns the angle (in degrees) between the two points determined by (x1, y1)
        /// and (x2, y2). The angle ranges from 0 to 359 degrees.
        /// What this function does is pretty simple but apparently the original is not
        /// accurate.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        private static ushort kGetAngleWorker(short x1, short y1, short x2, short y2)
        {
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
                return kGetAngle_SCI1(x1, y1, x2, y2);
            else
                return kGetAngle_SCI0(x1, y1, x2, y2);
        }

        private static ushort kGetAngle_SCI1(short x1, short y1, short x2, short y2)
        {
            // We flip things around to get into the standard atan2 coordinate system
            return (ushort)kGetAngle_SCI1_atan2(x2 - x1, y1 - y2);
        }

        private static int kGetAngle_SCI1_atan2(int y, int x)
        {
            if (y < 0)
            {
                int a = kGetAngle_SCI1_atan2(-y, -x);
                if (a == 180)
                    return 0;
                else
                    return 180 + a;
            }
            if (x < 0)
                return 90 + kGetAngle_SCI1_atan2(-x, y);
            if (y > x)
                return 90 - kGetAngle_SCI1_atan2_base(x, y);
            else
                return kGetAngle_SCI1_atan2_base(y, x);
        }

        // atan2 for first octant, x >= y >= 0. Returns [0,45] (inclusive)
        private static int kGetAngle_SCI1_atan2_base(int y, int x)
        {
            if (x == 0)
                return 0;

            // fixed point tan(a)
            int tan_fp = 10000 * y / x;

            if (tan_fp >= 1000)
            {
                // For tan(a) >= 0.1, interpolate between multiples of 5 degrees

                // 10000 * tan([5, 10, 15, 20, 25, 30, 35, 40, 45])
                int[] tan_table = new int[]{ 875, 1763, 2679, 3640, 4663, 5774,
                                  7002, 8391, 10000 };

                // Look up tan(a) in our table
                int i = 1;
                while (tan_fp > tan_table[i]) ++i;

                // The angle a is between 5*i and 5*(i+1). We linearly interpolate.
                int dist = tan_table[i] - tan_table[i - 1];
                int interp = (5 * (tan_fp - tan_table[i - 1]) + dist / 2) / dist;
                return 5 * i + interp;
            }
            else {
                // for tan(a) < 0.1, tan(a) is approximately linear in a.
                // tan'(0) = 1, so in degrees the slope of atan is 180/pi = 57.29...
                return (57 * y + x / 2) / x;
            }
        }

        public bool SignatureMatch(ushort[] signature, int argc, StackPtr? argv)
        {
            var sig = 0;
            var nextSig = 0;
            var curSig = nextSig;
            while (nextSig < signature.Length && argc != 0)
            {
                curSig = nextSig;
                int type = FindRegType(argv.Value[0]);

                if ((type & SIG_IS_INVALID) != 0 && (0 == (signature[curSig] & SIG_IS_INVALID)))
                    return false; // pointer is invalid and signature doesn't allow that?

                if (0 == ((type & ~SIG_IS_INVALID) & signature[curSig]))
                {
                    if ((type & ~SIG_IS_INVALID) == SIG_TYPE_ERROR && (signature[curSig] & SIG_IS_INVALID) != 0)
                    {
                        // Type is unknown (error - usually because of a deallocated object or
                        // stale pointer) and the signature allows invalid pointers. In this case,
                        // ignore the invalid pointer.
                    }
                    else {
                        return false; // type mismatch
                    }
                }

                if (0 == (signature[curSig] & SIG_MORE_MAY_FOLLOW))
                {
                    sig++;
                    nextSig = sig;
                }
                else {
                    signature[nextSig] |= SIG_IS_OPTIONAL; // more may follow . assumes followers are optional
                }
                argv++;
                argc--;
            }

            // Too many arguments?
            if (argc != 0)
                return false;
            // Signature end reached?
            if (signature[nextSig] == 0)
                return true;
            // current parameter is optional?
            if ((signature[curSig] & SIG_IS_OPTIONAL) != 0)
            {
                // yes, check if nothing more is required
                if (0 == (signature[curSig] & SIG_NEEDS_MORE))
                    return true;
            }
            else {
                // no, check if next parameter is optional
                if ((signature[nextSig] & SIG_IS_OPTIONAL) != 0)
                    return true;
            }
            // Too few arguments or more optional arguments required
            return false;
        }

        private int FindRegType(Register reg)
        {
            // No segment? Must be integer
            if (reg.Segment == 0)
                return SIG_TYPE_INTEGER | (reg.Offset != 0 ? 0 : SIG_TYPE_NULL);

            if (reg.Segment == 0xFFFF)
                return SIG_TYPE_UNINITIALIZED;

            // Otherwise it's an object
            SegmentObj mobj = _segMan.GetSegmentObj(reg.Segment);
            if (mobj == null)
                return SIG_TYPE_ERROR;

            var result = 0;
            if (!mobj.IsValidOffset((ushort)reg.Offset))
                result |= SIG_IS_INVALID;

            switch (mobj.Type)
            {
                case SegmentType.SCRIPT:
                    if (reg.Offset <= ((Script)mobj).BufSize &&
                        reg.Offset >= (uint)-Script.SCRIPT_OBJECT_MAGIC_OFFSET &&
                        ((Script)mobj).OffsetIsObject((int)reg.Offset))
                    {
                        result |= ((Script)mobj).GetObject((ushort)reg.Offset) != null ? SIG_TYPE_OBJECT : SIG_TYPE_REFERENCE;
                    }
                    else
                        result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.CLONES:
                    result |= SIG_TYPE_OBJECT;
                    break;
                case SegmentType.LOCALS:
                case SegmentType.STACK:
                case SegmentType.DYNMEM:
                case SegmentType.HUNK:
# if ENABLE_SCI32
                case SegmentType.ARRAY:
                case SegmentType.STRING:
#endif
                    result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.LISTS:
                    result |= SIG_TYPE_LIST;
                    break;
                case SegmentType.NODES:
                    result |= SIG_TYPE_NODE;
                    break;
                default:
                    return SIG_TYPE_ERROR;
            }
            return result;
        }
    }
}
