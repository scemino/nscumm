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
using System.Collections.Generic;
using System.Linq;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        // Loads arbitrary resources of type 'restype' with resource numbers 'resnrs'
        // This implementation ignores all resource numbers except the first one.
        private static Register kLoad(EngineState s, int argc, StackPtr? argv)
        {
            ResourceType restype = SciEngine.Instance.ResMan.ConvertResType(argv.Value[0].ToUInt16());
            int resnr = argv.Value[1].ToUInt16();

            // Request to dynamically allocate hunk memory for later use
            if (restype == ResourceType.Memory)
                return s._segMan.AllocateHunkEntry("kLoad()", resnr);

            return Register.Make(0, (ushort)((((int)restype) << 11) | resnr)); // Return the resource identifier as handle
        }

        // Unloads an arbitrary resource of type 'restype' with resource numbber 'resnr'
        //  behavior of this call didn't change between sci0.sci1.1 parameter wise, which means getting called with
        //  1 or 3+ parameters is not right according to sierra sci
        private static Register kUnLoad(EngineState s, int argc, StackPtr? argv)
        {
            if (argc >= 2)
            {
                ResourceType restype = SciEngine.Instance.ResMan.ConvertResType(argv.Value[0].ToUInt16());
                Register resnr = argv.Value[1];

                if (restype == ResourceType.Memory)
                    s._segMan.FreeHunkEntry(resnr);
            }

            return s.r_acc;
        }

        // Returns script dispatch address index in the supplied script
        private static Register kScriptID(EngineState s, int argc, StackPtr? argv)
        {
            int script = argv.Value[0].ToUInt16();
            ushort index = (argc > 1) ? argv.Value[1].ToUInt16() : (ushort)0;

            if (argv.Value[0].Segment != 0)
                return argv.Value[0];

            var scriptSeg = s._segMan.GetScriptSegment(script, ScriptLoadType.LOAD);

            if (scriptSeg == 0)
                return Register.NULL_REG;

            Script scr = s._segMan.GetScript(scriptSeg);

            if (scr.ExportsNr == 0)
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

        private static Register kDisposeClone(EngineState s, int argc, StackPtr? argv)
        {
            Register obj = argv.Value[0];
            var @object = s._segMan.GetObject(obj);

            if (@object == null)
            {
                throw new InvalidOperationException($"Attempt to dispose non-class/object at {obj}");
            }

            // SCI uses this technique to find out, if it's a clone and if it's supposed to get freed
            //  At least kq4early relies on this behavior. The scripts clone "Sound", then set bit 1 manually
            //  and call kDisposeClone later. In that case we may not free it, otherwise we will run into issues
            //  later, because kIsObject would then return false and Sound object wouldn't get checked.
            ushort infoSelector = (ushort)@object.InfoSelector.Offset;
            if ((infoSelector & 3) == SciObject.InfoFlagClone)
                @object.MarkAsFreed();

            return s.r_acc;
        }

        private static Register kDisposeScript(EngineState s, int argc, StackPtr? argv)
        {
            int script = (int)argv.Value[0].Offset;

            ushort id = s._segMan.GetScriptSegment(script);
            Script scr = s._segMan.GetScriptIfLoaded(id);
            if (scr != null && !scr.IsMarkedAsDeleted)
            {
                if (s._executionStack.Last().pc.Segment != id)
                    scr.Lockers = 1;
            }

            s._segMan.UninstantiateScript(script);

            if (argc != 2)
            {
                return s.r_acc;
            }
            else {
                // This exists in the KQ5CD and GK1 interpreter. We know it is used
                // when GK1 starts up, before the Sierra logo.
                // TODO: warning("kDisposeScript called with 2 parameters, still untested");
                return argv.Value[1];
            }
        }

        private static Register kIsObject(EngineState s, int argc, StackPtr? argv)
        {
            if (argv.Value[0].Offset == Register.SIGNAL_OFFSET) // Treated specially
                return Register.NULL_REG;
            else
                return Register.Make(0, s._segMan.IsHeapObject(argv.Value[0]));
        }

        private static Register kClone(EngineState s, int argc, StackPtr? argv)
        {
            Register parentAddr = argv.Value[0];
            SciObject parentObj = s._segMan.GetObject(parentAddr);
            Register cloneAddr;

            if (parentObj == null)
            {
                throw new InvalidOperationException($"Attempt to clone non-object/class at {parentAddr} failed");
            }

            //debugC(kDebugLevelMemory, "Attempting to clone from %04x:%04x", PRINT_REG(parentAddr));

            ushort infoSelector = (ushort)parentObj.InfoSelector.Offset;
            var cloneObj = s._segMan.AllocateClone(out cloneAddr);

            if (cloneObj.Item == null)
            {
                throw new InvalidOperationException($"Cloning {parentAddr} failed-- internal error");
            }

            // In case the parent object is a clone itself we need to refresh our
            // pointer to it here. This is because calling allocateClone might
            // invalidate all pointers, references and iterators to data in the clones
            // segment.
            //
            // The reason why it might invalidate those is, that the segment code
            // (Table) uses Common::Array for internal storage. Common::Array now
            // might invalidate references to its contained data, when it has to
            // extend the internal storage size.
            if ((infoSelector & SciObject.InfoFlagClone) != 0)
                parentObj = s._segMan.GetObject(parentAddr);

            cloneObj.Item = parentObj.Clone();

            // Mark as clone
            unchecked
            {
                infoSelector &= (ushort)(~SciObject.InfoFlagClone); // remove class bit
            }
            cloneObj.Item.InfoSelector = Register.Make(0, (ushort)(infoSelector | SciObject.InfoFlagClone));

            cloneObj.Item.SpeciesSelector = cloneObj.Item.Pos;
            if (parentObj.IsClass)
                cloneObj.Item.SuperClassSelector = parentObj.Pos;
            s._segMan.GetScript(parentObj.Pos.Segment).IncrementLockers();
            s._segMan.GetScript(cloneObj.Item.Pos.Segment).IncrementLockers();

            return cloneAddr;
        }

        private static Register kLock(EngineState s, int argc, StackPtr? argv)
        {
            int state = argc > 2 ? argv.Value[2].ToUInt16() : 1;
            ResourceType type = SciEngine.Instance.ResMan.ConvertResType(argv.Value[0].ToUInt16());
            ResourceId id = new ResourceId(type, argv.Value[1].ToUInt16());

            ResourceManager.ResourceSource.Resource which;

            switch (state)
            {
                case 1:
                    SciEngine.Instance.ResMan.FindResource(id, true);
                    break;
                case 0:
                    if (id.Number == 0xFFFF)
                    {
                        // Unlock all resources of the requested type
                        List<ResourceId> resources = SciEngine.Instance.ResMan.ListResources(type);
                        foreach (var i in resources)
                        {
                            var res = SciEngine.Instance.ResMan.TestResource(i);
                            if (res.IsLocked)
                                SciEngine.Instance.ResMan.UnlockResource(res);
                        }
                    }
                    else {
                        which = SciEngine.Instance.ResMan.FindResource(id, false);

                        if (which != null)
                            SciEngine.Instance.ResMan.UnlockResource(which);
                        else {
                            if (id.Type == ResourceType.Invalid)
                            {
                                // TODO: warning("[resMan] Attempt to unlock resource %i of invalid type %i", id.getNumber(), argv[0].toUint16());
                            }
                            else {
                                // Happens in CD games (e.g. LSL6CD) with the message
                                // resource. It isn't fatal, and it's usually caused
                                // by leftover scripts.
                                // TODO: debugC(kDebugLevelResMan, "[resMan] Attempt to unlock non-existant resource %s", id.toString().c_str());
                            }
                        }
                    }
                    break;
            }
            return s.r_acc;
        }

        private static Register kRespondsTo(EngineState s, int argc, StackPtr? argv)
        {
            Register obj = argv.Value[0];
            int selector = argv.Value[1].ToUInt16();

            Register tmp;
            return Register.Make(0, s._segMan.IsHeapObject(obj) && SciEngine.LookupSelector(s._segMan, obj, selector, null, out tmp) != SelectorType.None);
        }

        internal void SignatureDebug(ushort[] signature, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
    }
}
