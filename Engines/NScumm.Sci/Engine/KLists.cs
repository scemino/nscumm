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
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal class sort_temp_t
    {
        public Register key, value;
        public Register order;
    }

    internal partial class Kernel
    {
        private static Register kEmptyList(EngineState s, int argc, StackPtr argv)
        {
            if (argv[0].IsNull)
                return Register.NULL_REG;

            List list = s._segMan.LookupList(argv[0]);
# if CHECK_LISTS
            checkListPointer(s._segMan, argv[0]);
#endif
            return Register.Make(0, ((list != null) ? list.first.IsNull : false));
        }

        private static Register kNewList(EngineState s, int argc, StackPtr argv)
        {
            Register listRef;
            var list = s._segMan.AllocateList(out listRef);
            list.first = list.last = Register.NULL_REG;
            DebugC(DebugLevels.Nodes, "New listRef at {0}", listRef);

            return listRef; // Return list base address
        }

        private static Register kFindKey(EngineState s, int argc, StackPtr argv)
        {
            Register node_pos;
            Register key = argv[1];
            Register list_pos = argv[0];

            //Debug($"Looking for key {key} in list {list_pos}");

# if CHECK_LISTS
            checkListPointer(s._segMan, argv[0]);
#endif

            node_pos = s._segMan.LookupList(list_pos).first;

            //Debug($"First node at {node_pos}");

            while (!node_pos.IsNull)
            {
                Node n = s._segMan.LookupNode(node_pos);
                if (n.key == key)
                {
                    // Debug($" Found key at {node_pos}");
                    return node_pos;
                }

                node_pos = n.succ;
                //Debug($"NextNode at {node_pos}");
            }

            //Debug($"Looking for key without success");
            return Register.NULL_REG;
        }

        private static Register kNewNode(EngineState s, int argc, StackPtr argv)
        {
            Register nodeValue = argv[0];
            // Some SCI32 games call this with 1 parameter (e.g. the demo of Phantasmagoria).
            // Set the key to be the same as the value in this case
            Register nodeKey = (argc == 2) ? argv[1] : argv[0];
            s.r_acc = s._segMan.NewNode(nodeValue, nodeKey);

            DebugC(DebugLevels.Nodes, $"New nodeRef at {s.r_acc}");

            return s.r_acc;
        }

        private static Register kAddAfter(EngineState s, int argc, StackPtr argv)
        {
            List list = s._segMan.LookupList(argv[0]);
            Node firstnode = argv[1].IsNull ? null : s._segMan.LookupNode(argv[1]);
            Node newnode = s._segMan.LookupNode(argv[2]);

# if CHECK_LISTS
            checkListPointer(s._segMan, argv[0]);
#endif

            if (newnode == null)
            {
                throw new InvalidOperationException($"New 'node' {argv[2]} is not a node");
            }

            if (argc != 3 && argc != 4)
            {
                throw new InvalidOperationException("kAddAfter: Haven't got 3 or 4 arguments, aborting");
            }

            if (argc == 4)
                newnode.key = argv[3];

            if (firstnode != null)
            {
                // We're really appending after
                Register oldnext = firstnode.succ;

                newnode.pred = argv[1];
                firstnode.succ = argv[2];
                newnode.succ = oldnext;

                if (oldnext.IsNull) // Appended after last node?
                    // Set new node as last list node
                    list.last = argv[2];
                else
                    s._segMan.LookupNode(oldnext).pred = argv[2];
            }
            else
            {
                // !firstnode
                AddToFront(s, argv[0], argv[2]); // Set as initial list node
            }

            return s.r_acc;
        }

        private static Register kAddToEnd(EngineState s, int argc, StackPtr argv)
        {
            AddToEnd(s, argv[0], argv[1]);

            if (argc == 3)
                s._segMan.LookupNode(argv[1]).key = argv[2];

            return s.r_acc;
        }

        private static Register kAddToFront(EngineState s, int argc, StackPtr argv)
        {
            AddToFront(s, argv[0], argv[1]);

            if (argc == 3)
                s._segMan.LookupNode(argv[1]).key = argv[2];

            return s.r_acc;
        }

        private static Register kFirstNode(EngineState s, int argc, StackPtr argv)
        {
            if (argv[0].IsNull)
                return Register.NULL_REG;

            List list = s._segMan.LookupList(argv[0]);

            if (list != null)
            {
# if CHECK_LISTS
                checkListPointer(s._segMan, argv[0]);
#endif
                return list.first;
            }
            else
            {
                return Register.NULL_REG;
            }
        }

        private static Register kLastNode(EngineState s, int argc, StackPtr argv)
        {
            if (argv[0].IsNull)
                return Register.NULL_REG;

            List list = s._segMan.LookupList(argv[0]);

            if (list != null)
            {
# if CHECK_LISTS
                checkListPointer(s._segMan, argv[0]);
#endif
                return list.last;
            }
            else
            {
                return Register.NULL_REG;
            }
        }

        private static Register kDisposeList(EngineState s, int argc, StackPtr argv)
        {
            // This function is not needed in ScummVM. The garbage collector
            // cleans up unused objects automatically

            return s.r_acc;
        }

        private static Register kNextNode(EngineState s, int argc, StackPtr argv)
        {
            Node n = s._segMan.LookupNode(argv[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            return n.succ;
        }

        private static Register kPrevNode(EngineState s, int argc, StackPtr argv)
        {
            Node n = s._segMan.LookupNode(argv[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            return n.pred;
        }

        private static Register kNodeValue(EngineState s, int argc, StackPtr argv)
        {
            Node n = s._segMan.LookupNode(argv[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            // ICEMAN: when plotting a course in room 40, unDrawLast is called by
            // startPlot::changeState, but there is no previous entry, so we get 0 here
            return n != null ? n.value : Register.NULL_REG;
        }

        private static Register kDeleteKey(EngineState s, int argc, StackPtr argv)
        {
            Register node_pos = kFindKey(s, 2, argv);
            List list = s._segMan.LookupList(argv[0]);

            if (node_pos.IsNull)
                return Register.NULL_REG; // Signal failure

            var n = s._segMan.LookupNode(node_pos);
            if (list.first == node_pos)
                list.first = n.succ;
            if (list.last == node_pos)
                list.last = n.pred;

            if (!n.pred.IsNull)
                s._segMan.LookupNode(n.pred).succ = n.succ;
            if (!n.succ.IsNull)
                s._segMan.LookupNode(n.succ).pred = n.pred;

            // Erase references to the predecessor and successor nodes, as the game
            // scripts could reference the node itself again.
            // Happens in the intro of QFG1 and in Longbow, when exiting the cave.
            n.pred = Register.NULL_REG;
            n.succ = Register.NULL_REG;

            return Register.Make(0, 1); // Signal success
        }

        private static Register kSort(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            Register source = argv[0];
            Register dest = argv[1];
            Register order_func = argv[2];

            int input_size = (short)SciEngine.ReadSelectorValue(segMan, source, o => o.size);
            Register input_data = SciEngine.ReadSelector(segMan, source, o => o.elements);
            Register output_data = SciEngine.ReadSelector(segMan, dest, o => o.elements);

            List list;
            Node node;

            if (input_size == 0)
                return s.r_acc;

            if (output_data.IsNull)
            {
                list = s._segMan.AllocateList(out output_data);
                list.first = list.last = Register.NULL_REG;
                SciEngine.WriteSelector(segMan, dest, o => o.elements, output_data);
            }

            SciEngine.WriteSelectorValue(segMan, dest, o => o.size, (ushort)input_size);

            list = s._segMan.LookupList(input_data);
            node = s._segMan.LookupNode(list.first);

            var temp_array = new List<sort_temp_t>();

            int i = 0;
            while (node != null)
            {
                Register[] @params = { node.value };

                SciEngine.InvokeSelector(s, order_func, o => o.doit, argc, argv, 1, new StackPtr(@params, 0));
                temp_array[i].key = node.key;
                temp_array[i].value = node.value;
                temp_array[i].order = s.r_acc;
                i++;
                node = s._segMan.LookupNode(node.succ);
            }

            temp_array.Sort(sort_temp_cmp);

            for (i = 0; i < input_size; i++)
            {
                Register lNode = s._segMan.NewNode(temp_array[i].value, temp_array[i].key);

                AddToEnd(s, output_data, lNode);
            }

            return s.r_acc;
        }

        private static int sort_temp_cmp(sort_temp_t st1, sort_temp_t st2)
        {
            if (st1.order.Segment < st2.order.Segment ||
                (st1.order.Segment == st2.order.Segment &&
                 st1.order.Offset < st2.order.Offset))
                return -1;

            if (st1.order.Segment > st2.order.Segment ||
                (st1.order.Segment == st2.order.Segment &&
                 st1.order.Offset > st2.order.Offset))
                return 1;

            return 0;
        }

        private static void AddToFront(EngineState s, Register listRef, Register nodeRef)
        {
            List list = s._segMan.LookupList(listRef);
            Node newNode = s._segMan.LookupNode(nodeRef);

            DebugC(DebugLevels.Nodes, "Adding node {0} to end of list {1}", nodeRef, listRef);

            if (newNode == null)
                throw new InvalidOperationException($"Attempt to add non-node ({nodeRef}) to list at {listRef}");

# if CHECK_LISTS
            checkListPointer(s._segMan, listRef);
#endif

            newNode.pred = Register.NULL_REG;
            newNode.succ = list.first;

            // Set node to be the first and last node if it's the only node of the list
            if (list.first.IsNull)
                list.last = nodeRef;
            else
            {
                Node oldNode = s._segMan.LookupNode(list.first);
                oldNode.pred = nodeRef;
            }
            list.first = nodeRef;
        }

        private static void AddToEnd(EngineState s, Register listRef, Register nodeRef)
        {
            List list = s._segMan.LookupList(listRef);
            Node newNode = s._segMan.LookupNode(nodeRef);

            DebugC(DebugLevels.Nodes, "Adding node {0} to end of list {1}", nodeRef, listRef);

            if (newNode == null)
                throw new InvalidOperationException($"Attempt to add non-node ({nodeRef}) to list at {listRef}");

# if CHECK_LISTS
            checkListPointer(s._segMan, listRef);
#endif

            newNode.pred = list.last;
            newNode.succ = Register.NULL_REG;

            // Set node to be the first and last node if it's the only node of the list
            if (list.last.IsNull)
                list.first = nodeRef;
            else
            {
                Node old_n = s._segMan.LookupNode(list.last);
                old_n.succ = nodeRef;
            }
            list.last = nodeRef;
        }

#if ENABLE_SCI32
        private static Register kArray(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kArrayNew(EngineState s, int argc, StackPtr argv)
        {
            ushort size = argv[0].ToUInt16();
            SciArrayType type = (SciArrayType)argv[1].ToUInt16();

            if (type == SciArrayType.String)
            {
                ++size;
            }

            Register arrayHandle;
            s._segMan.AllocateArray(type, size, out arrayHandle);
            return arrayHandle;
        }

        private static Register kArrayGetSize(EngineState s, int argc, StackPtr argv)
        {
            SciArray array = s._segMan.LookupArray(argv[0]);
            return Register.Make(0, (ushort)array.Size);
        }

        private static Register kArrayGetElement(EngineState s, int argc, StackPtr argv)
        {
            SciArray array = s._segMan.LookupArray(argv[0]);
            return array.GetAsID(argv[1].ToUInt16());
        }

        private static Register kStringGetChar(EngineState s, int argc, StackPtr argv)
        {
            ushort index = argv[1].ToUInt16();

            // Game scripts may contain static raw string data
            if (!s._segMan.IsArray(argv[0]))
            {
                string @string = s._segMan.GetString(argv[0]);
                if (index >= @string.Length)
                {
                    return Register.Make(0, 0);
                }

                return Register.Make(0, (byte)@string[index]);
            }

            SciArray array = s._segMan.LookupArray(argv[0]);

            if (index >= array.Size)
            {
                return Register.Make(0, 0);
            }

            return array.GetAsID(index);
        }

        private static Register kArraySetElements(EngineState s, int argc, StackPtr argv)
        {
            SciArray array = s._segMan.LookupArray(argv[0]);
            array.SetElements(argv[1].ToUInt16(), (ushort)(argc - 2), argv + 2);
            return argv[0];
        }

        private static Register kArrayFree(EngineState s, int argc, StackPtr argv)
        {
            s._segMan.FreeArray(argv[0]);
            return s.r_acc;
        }

        private static Register kArrayFill(EngineState s, int argc, StackPtr argv)
        {
            SciArray array = s._segMan.LookupArray(argv[0]);
            array.Fill(argv[1].ToUInt16(), argv[2].ToUInt16(), argv[3]);
            return argv[0];
        }

        private static Register kArrayCopy(EngineState s, int argc, StackPtr argv)
        {
            SciArray target = s._segMan.LookupArray(argv[0]);
            ushort targetIndex = argv[1].ToUInt16();

            SciArray source = new SciArray();
            // String copies may be made from static script data
            if (!s._segMan.IsArray(argv[2]))
            {
                source.SetType(SciArrayType.String);
                source.FromString(s._segMan.GetString(argv[2]));
            }
            else
            {
                source = s._segMan.LookupArray(argv[2]);
            }
            ushort sourceIndex = argv[3].ToUInt16();
            ushort count = argv[4].ToUInt16();

            target.Copy(source, sourceIndex, targetIndex, count);
            return argv[0];
        }

        private static Register kArrayDuplicate(EngineState s, int argc, StackPtr argv)
        {
            Register targetHandle;

            // String duplicates may be made from static script data
            if (!s._segMan.IsArray(argv[0]))
            {
                string source = s._segMan.GetString(argv[0]);
                SciArray target = s._segMan.AllocateArray(SciArrayType.String, (ushort)source.Length, out targetHandle);
                target.FromString(source);
            }
            else
            {
                SciArray source = s._segMan.LookupArray(argv[0]);
                SciArray target = s._segMan.AllocateArray(source.Type, (ushort)source.Size, out targetHandle);
                target.Assign(source);
            }

            return targetHandle;
        }

        private static Register kArrayGetData(EngineState s, int argc, StackPtr argv)
        {
            if (s._segMan.IsObject(argv[0]))
            {
                return SciEngine.ReadSelector(s._segMan, argv[0], o => o.data);
            }

            return argv[0];
        }

        private static Register kArrayByteCopy(EngineState s, int argc, StackPtr argv)
        {
            SciArray target = s._segMan.LookupArray(argv[0]);
            ushort targetOffset = argv[1].ToUInt16();
            SciArray source = s._segMan.LookupArray(argv[2]);
            ushort sourceOffset = argv[3].ToUInt16();
            ushort count = argv[4].ToUInt16();

            target.ByteCopy(source, sourceOffset, targetOffset, count);
            return argv[0];
        }

        private static Register kListAt(EngineState s, int argc, StackPtr argv)
        {
            if (argc != 2)
            {
                Error("kListAt called with {0} parameters", argc);
                return Register.NULL_REG;
            }

            List list = s._segMan.LookupList(argv[0]);
            Register curAddress = list.first;
            if (list.first.IsNull)
            {
                Error("kListAt tried to reference empty list ({0})", argv[0]);
                return Register.NULL_REG;
            }
            Node curNode = s._segMan.LookupNode(curAddress);
            Register curObject = curNode.value;
            short listIndex = (short)argv[1].ToUInt16();
            int curIndex = 0;

            while (curIndex != listIndex)
            {
                if (curNode.succ.IsNull)
                {
                    // end of the list?
                    return Register.NULL_REG;
                }

                curAddress = curNode.succ;
                curNode = s._segMan.LookupNode(curAddress);
                curObject = curNode.value;

                curIndex++;
            }

            // Update the virtual file selected in the character import screen of QFG4.
            // For the SCI0-SCI1.1 version of this, check kDrawControl().
            if (SciEngine.Instance.InQfGImportRoom != 0 &&
                string.Equals(s._segMan.GetObjectName(curObject), "SelectorDText"))
                s._chosenQfGImportItem = listIndex;

            return curObject;
        }

        private static Register kListEachElementDo(EngineState s, int argc, StackPtr argv)
        {
            List list = s._segMan.LookupList(argv[0]);

            Node curNode = s._segMan.LookupNode(list.first);
            Register curObject;
            int slc = argv[1].ToUInt16();

            ObjVarRef address = new ObjVarRef();

            while (curNode != null)
            {
                // We get the next node here as the current node might be gone after the invoke
                Register nextNode = curNode.succ;
                curObject = curNode.value;

                // First, check if the target selector is a variable
                if (SciEngine.LookupSelector(s._segMan, curObject, slc, address) == SelectorType.Variable)
                {
                    // This can only happen with 3 params (list, target selector, variable)
                    if (argc != 3)
                    {
                        Error("kListEachElementDo: Attempted to modify a variable selector with {0} params", argc);
                    }
                    else
                    {
                        SciEngine.WriteSelector(s._segMan, curObject, slc, argv[2]);
                    }
                }
                else
                {
                    SciEngine.InvokeSelector(s, curObject, slc, argc, argv, argc - 2, argv + 2);
                }

                curNode = s._segMan.LookupNode(nextNode);
            }

            return s.r_acc;
        }

        private static Register kListFirstTrue(EngineState s, int argc, StackPtr argv)
        {
            List list = s._segMan.LookupList(argv[0]);

            Node curNode = s._segMan.LookupNode(list.first);
            Register curObject;
            int slc = argv[1].ToUInt16();

            ObjVarRef address = new ObjVarRef();

            s.r_acc = Register.NULL_REG; // reset the accumulator

            while (curNode != null)
            {
                Register nextNode = curNode.succ;
                curObject = curNode.value;

                // First, check if the target selector is a variable
                if (SciEngine.LookupSelector(s._segMan, curObject, slc, address) == SelectorType.Variable)
                {
                    // If it's a variable selector, check its value.
                    // Example: script 64893 in Torin, MenuHandler::isHilited checks
                    // all children for variable selector 0x03ba (bHilited).
                    if (!SciEngine.ReadSelector(s._segMan, curObject, slc).IsNull)
                        return curObject;
                }
                else
                {
                    SciEngine.InvokeSelector(s, curObject, slc, argc, argv, argc - 2, argv + 2);

                    // Check if the result is true
                    if (!s.r_acc.IsNull)
                        return curObject;
                }

                curNode = s._segMan.LookupNode(nextNode);
            }

            // No selector returned true
            return Register.NULL_REG;
        }

        private static Register kListIndexOf(EngineState s, int argc, StackPtr argv)
        {
            List list = s._segMan.LookupList(argv[0]);

            Register curAddress = list.first;
            Node curNode = s._segMan.LookupNode(curAddress);
            ushort curIndex = 0;

            while (curNode != null)
            {
                var curObject = curNode.value;
                if (curObject == argv[1])
                    return Register.Make(0, curIndex);

                curAddress = curNode.succ;
                curNode = s._segMan.LookupNode(curAddress);
                curIndex++;
            }

            return Register.SIGNAL_REG;
        }


        private static Register kListAllTrue(EngineState s, int argc, StackPtr argv)
        {
            List list = s._segMan.LookupList(argv[0]);

            Node curNode = s._segMan.LookupNode(list.first);
            int slc = argv[1].ToUInt16();

            ObjVarRef address = new ObjVarRef();

            s.r_acc = Register.Make(0, 1); // reset the accumulator

            while (curNode != null)
            {
                Register nextNode = curNode.succ;
                var curObject = curNode.value;

                // First, check if the target selector is a variable
                if (SciEngine.LookupSelector(s._segMan, curObject, slc, address) == SelectorType.Variable)
                {
                    // If it's a variable selector, check its value
                    s.r_acc = SciEngine.ReadSelector(s._segMan, curObject, slc);
                }
                else
                {
                    SciEngine.InvokeSelector(s, curObject, slc, argc, argv, argc - 2, argv + 2);
                }

                // Check if the result isn't true
                if (s.r_acc.IsNull)
                    break;

                curNode = s._segMan.LookupNode(nextNode);
            }

            return s.r_acc;
        }

        private static Register kList(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        private static Register kAddBefore(EngineState s, int argc, StackPtr argv)
        {
            Error("Unimplemented function kAddBefore called");
            return s.r_acc;
        }

        private static Register kMoveToFront(EngineState s, int argc, StackPtr argv)
        {
            Error("Unimplemented function kMoveToFront called");
            return s.r_acc;
        }

        private static Register kMoveToEnd(EngineState s, int argc, StackPtr argv)
        {
            Error("Unimplemented function kMoveToEnd called");
            return s.r_acc;
        }
#endif
    }
}