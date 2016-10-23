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
    internal class SciKernelMapEntry
    {
        public string name;
        public KernelFunctionCall function;

        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public string signature;
        public SciKernelMapSubEntry[] subFunctions;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapEntry Make(string name, KernelFunctionCall function, SciVersionRange range,
            string signature, SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = name,
                function = function,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }

        public static SciKernelMapEntry Make(KernelFunctionCall function, SciVersionRange range, string signature,
            SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = function.Method.Name.Remove(0, 1),
                function = function,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }

        public static SciKernelMapEntry MakeEmpty(string name, SciVersionRange range, string signature,
            SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = name,
                function = Kernel.kEmpty,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }

        public static SciKernelMapEntry MakeDummy(string name, SciVersionRange range, string signature,
            SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = name,
                function = Kernel.kDummy,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }
    }
}