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
        // Loads arbitrary resources of type 'restype' with resource numbers 'resnrs'
        // This implementation ignores all resource numbers except the first one.
        private static Register kLoad(EngineState s, int argc, StackPtr argv)
        {
            ResourceType restype = SciEngine.Instance.ResMan.ConvertResType(argv[0].ToUInt16());
            int resnr = argv[1].ToUInt16();

            // Request to dynamically allocate hunk memory for later use
            if (restype == ResourceType.Memory)
                return s._segMan.AllocateHunkEntry("kLoad()", resnr);

            return Register.Make(0, (ushort)((((int)restype) << 11) | resnr)); // Return the resource identifier as handle
        }

        // Unloads an arbitrary resource of type 'restype' with resource numbber 'resnr'
        //  behavior of this call didn't change between sci0.sci1.1 parameter wise, which means getting called with
        //  1 or 3+ parameters is not right according to sierra sci
        private static Register kUnLoad(EngineState s, int argc, Register[] argv)
        {
            if (argc >= 2)
            {
                ResourceType restype = SciEngine.Instance.ResMan.ConvertResType(argv[0].ToUInt16());
                Register resnr = argv[1];

                if (restype == ResourceType.Memory)
                    s._segMan.FreeHunkEntry(resnr);
            }

            return s.r_acc;
        }

        // Returns script dispatch address index in the supplied script
        private static Register kScriptID(EngineState s, int argc, Register[] argv)
        {
            int script = argv[0].ToUInt16();
            ushort index = (argc > 1) ? argv[1].ToUInt16() : (ushort)0;

            if (argv[0].Segment!=0)
                return argv[0];

            var scriptSeg = s._segMan.GetScriptSegment(script, ScriptLoadType.LOAD);

            if (scriptSeg==0)
                return Register.NULL_REG;

            Script scr = s._segMan.GetScript(scriptSeg);

            if (!scr.ExportsNr)
            {
                // This is normal. Some scripts don't have a dispatch (exports) table,
                // and this call is probably used to load them in memory, ignoring
                // the return value. If only one argument is passed, this call is done
                // only to load the script in memory. Thus, don't show any warning,
                // as no return value is expected. If an export is requested, then
                // it will most certainly fail with OOB access.
                if (argc == 2)
                    throw new NotImplementedException($"Script 0x{script:X} does not have a dispatch table and export {index} was requested from it");
                return Register.NULL_REG;
            }

            ushort address = scr.ValidateExportFunc(index, true);

            // Point to the heap for SCI1.1 - SCI2.1 games
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                address += scr.ScriptSize;

            // Bugfix for the intro speed in PQ2 version 1.002.011.
            // This is taken from the patch by NewRisingSun(NRS) / Belzorash. Global 3
            // is used for timing during the intro, and in the problematic version it's
            // initialized to 0, whereas it's 6 in other versions. Thus, we assign it
            // to 6 here, fixing the speed of the introduction. Refer to bug #3102071.
            if (SciEngine.Instance.GameId == SciGameId.PQ2 && script == 200 && s.variables[Vm.VAR_GLOBAL][3].IsNull)
            {
                s.variables[Vm.VAR_GLOBAL][3] = Register.Make(0, 6);
            }

            return Register.Make(scriptSeg, address);
        }

        internal void SignatureDebug(ushort[] signature, int argc, StackPtr argv)
        {
            throw new NotImplementedException();
        }
    }
}
