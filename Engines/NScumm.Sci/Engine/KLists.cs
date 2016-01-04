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
        private static Register kNewList(EngineState s, int argc, StackPtr? argv)
        {
            Register listRef;
            var list = s._segMan.AllocateList(out listRef);
            list.first = list.last = Register.NULL_REG;
            // TODO: debugC(kDebugLevelNodes, "New listRef at %04x:%04x", PRINT_REG(listRef));

            return listRef; // Return list base address
        }

        private static Register kFindKey(EngineState s, int argc, StackPtr? argv)
        {
            Register node_pos;
            Register key = argv.Value[1];
            Register list_pos = argv.Value[0];

            // TODO: debugC(kDebugLevelNodes, "Looking for key %04x:%04x in list %04x:%04x", PRINT_REG(key), PRINT_REG(list_pos));

# if CHECK_LISTS
            checkListPointer(s._segMan, argv[0]);
#endif

            node_pos = s._segMan.LookupList(list_pos).first;

            // TODO: debugC(kDebugLevelNodes, "First node at %04x:%04x", PRINT_REG(node_pos));

            while (!node_pos.IsNull)
            {
                Node n = s._segMan.LookupNode(node_pos);
                if (n.key == key)
                {
                    // TODO: debugC(kDebugLevelNodes, " Found key at %04x:%04x", PRINT_REG(node_pos));
                    return node_pos;
                }

                node_pos = n.succ;
                // TODO: debugC(kDebugLevelNodes, "NextNode at %04x:%04x", PRINT_REG(node_pos));
            }

            // TODO: debugC(kDebugLevelNodes, "Looking for key without success");
            return Register.NULL_REG;
        }

        private static Register kNewNode(EngineState s, int argc, StackPtr? argv)
        {
            Register nodeValue = argv.Value[0];
            // Some SCI32 games call this with 1 parameter (e.g. the demo of Phantasmagoria).
            // Set the key to be the same as the value in this case
            Register nodeKey = (argc == 2) ? argv.Value[1] : argv.Value[0];
            s.r_acc = s._segMan.NewNode(nodeValue, nodeKey);

            // TODO: debugC(kDebugLevelNodes, "New nodeRef at {s.r_acc}");

            return s.r_acc;
        }

        private static Register kAddToEnd(EngineState s, int argc, StackPtr? argv)
        {
            AddToEnd(s, argv.Value[0], argv.Value[1]);

            if (argc == 3)
                s._segMan.LookupNode(argv.Value[1]).key = argv.Value[2];

            return s.r_acc;
        }

        private static Register kAddToFront(EngineState s, int argc, StackPtr? argv)
        {
            AddToFront(s, argv.Value[0], argv.Value[1]);

            if (argc == 3)
                s._segMan.LookupNode(argv.Value[1]).key = argv.Value[2];

            return s.r_acc;
        }

        private static Register kFirstNode(EngineState s, int argc, StackPtr? argv)
        {
            if (argv.Value[0].IsNull)
                return Register.NULL_REG;

            List list = s._segMan.LookupList(argv.Value[0]);

            if (list != null)
            {
# if CHECK_LISTS
                checkListPointer(s._segMan, argv[0]);
#endif
                return list.first;
            }
            else {
                return Register.NULL_REG;
            }
        }

        private static Register kLastNode(EngineState s, int argc, StackPtr? argv)
        {
            if (argv.Value[0].IsNull)
                return Register.NULL_REG;

            List list = s._segMan.LookupList(argv.Value[0]);

            if (list != null)
            {
# if CHECK_LISTS
                checkListPointer(s._segMan, argv[0]);
#endif
                return list.last;
            }
            else {
                return Register.NULL_REG;
            }
        }

        private static Register kDisposeList(EngineState s, int argc, StackPtr? argv)
        {
            // This function is not needed in ScummVM. The garbage collector
            // cleans up unused objects automatically

            return s.r_acc;
        }

        private static Register kNextNode(EngineState s, int argc, StackPtr? argv)
        {
            Node n = s._segMan.LookupNode(argv.Value[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            return n.succ;
        }

        private static Register kPrevNode(EngineState s, int argc, StackPtr? argv)
        {
            Node n = s._segMan.LookupNode(argv.Value[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            return n.pred;
        }

        private static Register kNodeValue(EngineState s, int argc, StackPtr? argv)
        {
            Node n = s._segMan.LookupNode(argv.Value[0]);

# if CHECK_LISTS
            if (!isSaneNodePointer(s._segMan, argv[0]))
                return NULL_REG;
#endif

            // ICEMAN: when plotting a course in room 40, unDrawLast is called by
            // startPlot::changeState, but there is no previous entry, so we get 0 here
            return n != null ? n.value : Register.NULL_REG;
        }


        static void AddToFront(EngineState s, Register listRef, Register nodeRef)
        {
            List list = s._segMan.LookupList(listRef);
            Node newNode = s._segMan.LookupNode(nodeRef);

            // TODO: debugC(kDebugLevelNodes, "Adding node %04x:%04x to end of list %04x:%04x", PRINT_REG(nodeRef), PRINT_REG(listRef));

            if (newNode == null)
                throw new InvalidOperationException("Attempt to add non-node ({nodeRef}) to list at {listRef}");

# if CHECK_LISTS
            checkListPointer(s._segMan, listRef);
#endif

            newNode.pred = Register.NULL_REG;
            newNode.succ = list.first;

            // Set node to be the first and last node if it's the only node of the list
            if (list.first.IsNull)
                list.last = nodeRef;
            else {
                Node oldNode = s._segMan.LookupNode(list.first);
                oldNode.pred = nodeRef;
            }
            list.first = nodeRef;
        }

        private static void AddToEnd(EngineState s, Register listRef, Register nodeRef)
        {
            List list = s._segMan.LookupList(listRef);
            Node newNode = s._segMan.LookupNode(nodeRef);

            // TODO: debugC(kDebugLevelNodes, "Adding node %04x:%04x to end of list %04x:%04x", PRINT_REG(nodeRef), PRINT_REG(listRef));

            if (newNode == null)
                throw new InvalidOperationException("Attempt to add non-node ({nodeRef}) to list at {listRef}");

# if CHECK_LISTS
            checkListPointer(s._segMan, listRef);
#endif

            newNode.pred = list.last;
            newNode.succ = Register.NULL_REG;

            // Set node to be the first and last node if it's the only node of the list
            if (list.last.IsNull)
                list.first = nodeRef;
            else {
                Node old_n = s._segMan.LookupNode(list.last);
                old_n.succ = nodeRef;
            }
            list.last = nodeRef;
        }
    }
}
