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
    internal class SciKernelMapSubEntry
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;

        public ushort id;

        public string name;
        public KernelFunctionCall function;

        public string signature;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapSubEntry Make(SciVersionRange range, int id, KernelFunctionCall call, string signature,
            SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapSubEntry
            {
                name = call.Method.Name.Remove(0, 1),
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                id = (ushort) id,
                function = call,
                signature = signature,
                workarounds = workarounds
            };
        }

        public static SciKernelMapSubEntry Make(SciVersionRange range, int id, string name, KernelFunctionCall call,
            string signature,
            SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapSubEntry
            {
                name = name,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                id = (ushort) id,
                function = call,
                signature = signature,
                workarounds = workarounds
            };
        }

        public static SciKernelMapSubEntry MakeDummy(string name, SciVersionRange range, int id, string signature,
            SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapSubEntry
            {
                name = name,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                id = (ushort) id,
                function = Kernel.kDummy,
                signature = signature,
                workarounds = workarounds
            };
        }

        public static SciKernelMapSubEntry MakeEmpty(string name, SciVersionRange range, int id, string signature,
            SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapSubEntry
            {
                name = name,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                id = (ushort) id,
                function = Kernel.kEmpty,
                signature = signature,
                workarounds = workarounds
            };
        }
    }
}