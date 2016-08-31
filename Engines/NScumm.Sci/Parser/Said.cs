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


using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Parser
{
    enum SaidToken
    {
        COMMA = 0xF000,
        AMP = 0xF100,
        SLASH = 0xF200,
        PARENO = 0xF300,
        PARENC = 0xF400,
        BRACKETO = 0xF500,
        BRACKETC = 0xF600,
        HASH = 0xF700,
        LT = 0xF800,
        GT = 0xF900,
        TERM = 0xFF00
    }

    enum SaidWord
    {
        NONE = 0x0ffe,
        ANY = 0x0fff
    }

    enum ScanSaidType
    {
        AND = 0,
        OR = 1
    }

    internal class Said
    {
        private const int MAX_SAID_TOKENS = 128;
        private const int SAID_TREE_START = 4; // Reserve space for the 4 top nodes

        private static int said_token;
        private static int said_tokens_nr;
        private static int[] said_tokens = new int[MAX_SAID_TOKENS];

        private static int said_tree_pos;

        private static bool dontclaim;
        private static int outputDepth;

        // TODO: maybe turn this into a proper n-ary tree instead of an
        //   n-ary tree implemented in terms of a binary tree.
        //   (Together with _parserNodes in Vocabulary)

        private static ParseTreeNode[] said_tree = new ParseTreeNode[Vocabulary.VOCAB_TREE_NODES];

        public static int said(ByteAccess spec, bool verbose)
        {
            int retval;
            Vocabulary voc = SciEngine.Instance.Vocabulary;

            ParseTreeNode[] parse_tree_ptr = voc._parserNodes;

            if (voc.parserIsValid)
            {
                if (said_parse_spec(spec) != 0)
                    return Vocabulary.SAID_NO_MATCH;

                if (verbose)

                    vocab_dump_parse_tree("Said-tree", said_tree);
                retval = augment_parse_nodes(parse_tree_ptr[0], said_tree[0]);

                if (retval == 0)
                    return Vocabulary.SAID_NO_MATCH;
                else if (retval != Vocabulary.SAID_PARTIAL_MATCH)
                    return Vocabulary.SAID_FULL_MATCH;
                else
                    return Vocabulary.SAID_PARTIAL_MATCH;
            }

            return Vocabulary.SAID_NO_MATCH;
        }

        private static void vocab_dump_parse_tree(string tree_name, ParseTreeNode[] nodes)
        {
            DebugN("(setq {0} \n'(", tree_name);
            _vocab_recursive_ptree_dump(nodes[0], 1);
            DebugN("))\n");
        }

        private static void _vocab_recursive_ptree_dump(ParseTreeNode tree, int blanks)
        {
            System.Diagnostics.Debug.Assert(tree!=null);

            ParseTreeNode lbranch = tree.left;
            ParseTreeNode rbranch = tree.right;
            int i;

            if (tree.type == ParseTypes.LeafNode)
            {
                DebugN("vocab_dump_parse_tree: Error: consp is nil\n");
                return;
            }

            if (lbranch != null)
            {
                if (lbranch.type == ParseTypes.BranchNode)
                {
                    DebugN("\n");
                    for (i = 0; i < blanks; i++)
                        DebugN("    ");
                    DebugN("(");
                    _vocab_recursive_ptree_dump(lbranch, blanks + 1);
                    DebugN(")\n");
                    for (i = 0; i < blanks; i++)
                        DebugN("    ");
                }
                else
                    DebugN("{0:X}", lbranch.value);
                DebugN(" ");
            }/* else debugN ("nil");*/

            if (rbranch != null)
            {
                if (rbranch.type == ParseTypes.BranchNode)
                    _vocab_recursive_ptree_dump(rbranch, blanks);
                else
                {
                    DebugN("{0:X}", rbranch.value);
                    while (rbranch.right != null)
                    {
                        rbranch = rbranch.right;
                        DebugN("/{0}", rbranch.value);
                    }
                }
            }/* else debugN("nil");*/
        }

        private static int augment_parse_nodes(ParseTreeNode parseT, ParseTreeNode saidT)
        {
            outputDepth = 0;
            scidprintf("augment_parse_nodes on ");
            node_print_desc(parseT);
            scidprintf(" and ");
            node_print_desc(saidT);
            scidprintf("\n");

            dontclaim = false;

            int ret = matchTrees(parseT, saidT);

            scidprintf("matchTrees returned %d\n", ret);

            if (ret != 1)
                return 0;

            if (dontclaim)
                return Vocabulary.SAID_PARTIAL_MATCH;

            return 1;
        }

        private static int matchTrees(ParseTreeNode parseT, ParseTreeNode saidT)
        {
            outputDepth++;
            scidprintf("%*smatchTrees on ", outputDepth, "");
            node_print_desc(parseT);
            scidprintf(" and ");
            node_print_desc(saidT);
            scidprintf("\n");

            bool inParen = node_minor(saidT) == 0x14F || node_minor(saidT) == 0x150;
            bool inBracket = node_major(saidT) == 0x152;

            int ret;

            if (node_major(parseT) != 0x141 &&
                node_major(saidT) != 0x141 && node_major(saidT) != 0x152 &&
                node_major(saidT) != node_major(parseT))
            {
                ret = -1;
            }

            // parse major is 0x141 and/or
            // said major is 0x141/0x152 and/or
            // said major is parse major

            else if (node_is_terminal(saidT) && node_is_terminal(parseT))
            {

                // both saidT and parseT are terminals

                int said_val = node_terminal_value(saidT);

# if SCI_DEBUG_PARSE_TREE_AUGMENTATION
                scidprintf("%*smatchTrees matching terminals: %03x", outputDepth, "", node_terminal_value(parseT));
                ParseTreeNode* t = parseT.right.right;
                while (t)
                {
                    scidprintf(",%03x", t.value);
                    t = t.right;
                }
                scidprintf(" vs %03x", said_val);
#endif

                if (said_val == (int)SaidWord.NONE)
                {
                    ret = -1;
                }
                else if (said_val == (int)SaidWord.ANY)
                {
                    ret = 1;
                }
                else
                {
                    ret = -1;

                    // scan through the word group ids in the parse tree leaf to see if
                    // one matches the word group in the said tree
                    parseT = parseT.right.right;
                    do
                    {
                        //assert(parseT.type != kParseTreeBranchNode);
                        int parse_val = parseT.value;
                        if (parse_val == (int)SaidWord.ANY || parse_val == said_val)
                        {
                            ret = 1;
                            break;
                        }
                        parseT = parseT.right;
                    } while (parseT != null);
                }

                scidprintf(" (ret %d)\n", ret);

            }
            else if (node_is_terminal(saidT) && !node_is_terminal(parseT))
            {

                // saidT is a terminal, but parseT isn't

                if (node_major(parseT) == 0x141 ||
                        node_major(parseT) == node_major(saidT))
                    ret = scanParseChildren(parseT.right.right, saidT);
                else
                    ret = 0;

            }
            else if (node_is_terminal(parseT))
            {

                // parseT is a terminal, but saidT isn't

                if (node_major(saidT) == 0x141 || node_major(saidT) == 0x152 ||
                        node_major(saidT) == node_major(parseT))
                    ret = scanSaidChildren(parseT, saidT.right.right,
                                           inParen ? ScanSaidType.OR : ScanSaidType.AND);
                else
                    ret = 0;

            }
            else if (node_major(saidT) != 0x141 && node_major(saidT) != 0x152 &&
                     node_major(saidT) != node_major(parseT))
            {

                // parseT and saidT both aren't terminals
                // said major is not 0x141 or 0x152 or parse major

                ret = scanParseChildren(parseT.right.right, saidT);

            }
            else
            {

                // parseT and saidT are both not terminals,
                // said major 0x141 or 0x152 or equal to parse major

                ret = scanSaidChildren(parseT.right.right, saidT.right.right,
                                       inParen ? ScanSaidType.OR : ScanSaidType.AND);

            }

            if (inBracket && ret == 0)
            {
                scidprintf("%*smatchTrees changing ret to 1 due to brackets\n",
                           outputDepth, "");
                ret = 1;
            }

            scidprintf("%*smatchTrees returning %d\n", outputDepth, "", ret);
            outputDepth--;

            return ret;
        }

        private static int scanParseChildren(ParseTreeNode parseT, ParseTreeNode saidT)
        {

            outputDepth++;
            scidprintf("%*sscanParse on ", outputDepth, "");
            node_print_desc(parseT);
            scidprintf(" and ");
            node_print_desc(saidT);
            scidprintf("\n");

            if (node_major(saidT) == 0x14B)
            {
                dontclaim = true;
                scidprintf("%*sscanParse returning 1 (0x14B)\n", outputDepth, "");
                outputDepth--;
                return 1;
            }

            bool inParen = node_minor(saidT) == 0x14F || node_minor(saidT) == 0x150;
            bool inBracket = node_major(saidT) == 0x152;

            int ret;

            // descend further down saidT before actually scanning parseT
            if ((node_major(saidT) == 0x141 || node_major(saidT) == 0x152) &&
                !node_is_terminal(saidT))
            {

                ret = scanSaidChildren(parseT, saidT.right.right,
                                       inParen ? ScanSaidType.OR : ScanSaidType.AND);

            }
            else if (parseT != null && parseT.left.type == ParseTypes.BranchNode)
            {

                ret = 0;
                int subresult = 0;

                while (parseT != null)
                {
                    // assert(parseT.type == kParseTreeBranchNode);

                    ParseTreeNode parseChild = parseT.left;
                    //assert(parseChild);

                    scidprintf("%*sscanning next: ", outputDepth, "");
                    node_print_desc(parseChild);
                    scidprintf("\n");

                    if (node_major(parseChild) == node_major(saidT) ||
                            node_major(parseChild) == 0x141)
                        subresult = matchTrees(parseChild, saidT);

                    if (subresult != 0)
                        ret = subresult;

                    if (ret == 1)
                        break;

                    parseT = parseT.right;

                }

                // ret is now:
                // 1 if ANY matchTrees(parseSibling, saidTree) returned 1
                // ELSE: -1 if ANY returned -1
                // ELSE: 0

            }
            else
            {

                ret = matchTrees(parseT, saidT);

            }

            if (inBracket && ret == 0)
            {
                scidprintf("%*sscanParse changing ret to 1 due to brackets\n",
                           outputDepth, "");
                ret = 1;
            }

            scidprintf("%*sscanParse returning %d\n", outputDepth, "", ret);
            outputDepth--;

            return ret;
        }

        private static int scanSaidChildren(ParseTreeNode parseT, ParseTreeNode saidT,
                            ScanSaidType type)
        {
            outputDepth++;
            scidprintf("%*sscanSaid(%s) on ", outputDepth, "",
                                              type == ScanSaidType.OR ? "OR" : "AND");
            node_print_desc(parseT);
            scidprintf(" and ");
            node_print_desc(saidT);
            scidprintf("\n");

            int ret = 1;

            //assert(!(type == SCAN_SAID_OR && !saidT));

            while (saidT != null)
            {
                //assert(saidT.type == kParseTreeBranchNode);

                ParseTreeNode saidChild = saidT.left;
                //assert(saidChild);

                if (node_major(saidChild) != 0x145)
                {

                    ret = scanParseChildren(parseT, saidChild);

                    if (type == ScanSaidType.AND && ret != 1)
                        break;

                    if (type == ScanSaidType.OR && ret == 1)
                        break;

                }

                saidT = saidT.right;

            }
            scidprintf("%*sscanSaid returning %d\n", outputDepth, "", ret);

            outputDepth--;
            return ret;
        }


        private static void scidprintf(string v, params object[] arguments)
        {
        }

        private static void node_print_desc(ParseTreeNode parseT)
        {
        }

        private static int node_major(ParseTreeNode node)
        {
            //assert(node.type == kParseTreeBranchNode);
            //assert(node.left.type == kParseTreeLeafNode);
            return node.left.value;
        }

        private static int node_minor(ParseTreeNode node)
        {
            //assert(node.type == kParseTreeBranchNode);
            //assert(node.right.type == kParseTreeBranchNode);
            //assert(node.right.left.type == kParseTreeLeafNode);
            return node.right.left.value;
        }

        private static bool node_is_terminal(ParseTreeNode node)
        {
            return (node.right.right != null &&
                    node.right.right.type != ParseTypes.BranchNode);
        }

        private static int node_terminal_value(ParseTreeNode node)
        {
            //assert(node_is_terminal(node));
            return node.right.right.value;
        }

        private static int said_parse_spec(ByteAccess spec)
        {
            var s = new ByteAccess(spec);
            int nextitem;

            said_token = 0;
            said_tokens_nr = 0;

            said_tree_pos = SAID_TREE_START;

            do
            {
                nextitem = s.Increment();
                if (nextitem < Vocabulary.SAID_FIRST)

                    said_tokens[said_tokens_nr++] = (nextitem << 8 | s.Increment());
                else
                    said_tokens[said_tokens_nr++] = Vocabulary.SAID_LONG(nextitem);

            } while ((nextitem != Vocabulary.SAID_TERM) && (said_tokens_nr < MAX_SAID_TOKENS));

            if (nextitem != Vocabulary.SAID_TERM)
            {
                Warning("SAID spec is too long");
                return 1;
            }

            if (!buildSaidTree())
            {
                Warning("Error while parsing SAID spec");
                return 1;
            }

            return 0;
        }

        private static bool buildSaidTree()
        {
            said_branch_node(said_tree[0], said_tree[1], said_tree[2]);
            said_leaf_node(said_tree[1], 0x141); // Magic number #1
            said_branch_node(said_tree[2], said_tree[3], null);
            said_leaf_node(said_tree[3], 0x13f); // Magic number #2

            said_tree_pos = SAID_TREE_START;

            bool ret = parseSpec(said_tree[2]);

            if (!ret)
                return false;

            if (said_tokens[said_token] != (int)SaidToken.TERM)
            {
                // No terminator, so parse error.

                // Rollback
                said_tree[2].right = null;
                said_token = 0;
                said_tree_pos = SAID_TREE_START;
                return false;
            }

            return true;
        }

        private static bool parseSpec(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            bool ret = false;

            bool found;

            ParseTreeNode newParent = parentNode;

            found = parseExpr(newNode);

            if (found)
            {
                // Sentence part 1 found
                said_attach_subtree(newParent, 0x141, 0x149, newNode);

                newParent = newParent.right;

                ret = true;
            }

            bool nonempty;

            found = parsePart2(newParent, out nonempty);

            if (found)
            {

                ret = true;

                if (nonempty) // non-empty part found
                    newParent = newParent.right;


                found = parsePart3(newParent, out nonempty);

                if (found)
                {

                    if (nonempty)
                        newParent = newParent.right;
                }
            }

            if (said_tokens[said_token] == (int)SaidToken.GT)
            {
                said_token++;

                newNode = said_branch_node(said_next_node(), null,
                                said_leaf_node(said_next_node(), (int)SaidToken.GT));

                said_attach_subtree(newParent, 0x14B, (int)SaidToken.GT, newNode);

            }


            if (ret)
                return true;

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseExpr(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            bool ret = false;
            bool found;

            ParseTreeNode newParent = parentNode;

            found = parseList(newNode);

            if (found)
            {
                ret = true;

                said_attach_subtree(newParent, 0x141, 0x14F, newNode);

                newParent = newParent.right;
            }

            found = parseRef(newParent);

            if (found || ret)
                return true;

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseRef(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            ParseTreeNode newParent = parentNode;

            bool found;

            if (said_tokens[said_token] == (int)SaidToken.LT)
            {
                said_token++;

                found = parseList(newNode);

                if (found)
                {

                    said_attach_subtree(newParent, 0x144, 0x14f, newNode);

                    newParent = newParent.right;

                    newNode = said_branch_node(said_next_node(), null, null);

                    found = parseRef(newNode);

                    if (found)
                    {

                        said_attach_subtree(newParent, 0x141, 0x144, newNode);

                    }

                    return true;

                }

            }

            // NB: This is not an "else if'.
            // If there is a "< [ ... ]", that is parsed as "< ..."

            if (said_tokens[said_token] == (int)SaidToken.BRACKETO)
            {
                said_token++;

                found = parseRef(newNode);

                if (found)
                {

                    if (said_tokens[said_token] == (int)SaidToken.BRACKETC)
                    {
                        said_token++;

                        said_attach_subtree(parentNode, 0x152, 0x144, newNode);

                        return true;
                    }
                }

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parsePart2(ParseTreeNode parentNode, out bool nonempty)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            nonempty = true;

            bool found;

            found = parseSlash(newNode);

            if (found)
            {

                said_attach_subtree(parentNode, 0x142, 0x14a, newNode);

                return true;

            }
            else if (said_tokens[said_token] == (int)SaidToken.BRACKETO)
            {
                said_token++;

                found = parsePart2(newNode, out nonempty);

                if (found)
                {

                    if (said_tokens[said_token] == (int)SaidToken.BRACKETC)
                    {
                        said_token++;

                        said_attach_subtree(parentNode, 0x152, 0x142, newNode);

                        return true;
                    }
                }

            }

            // CHECKME: this doesn't look right if the [] section matched partially
            // Should the below 'if' be an 'else if' ?

            if (said_tokens[said_token] == (int)SaidToken.SLASH)
            {
                said_token++;

                nonempty = false;

                return true;

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parsePart3(ParseTreeNode parentNode, out bool nonempty)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            bool found;

            nonempty = true;

            found = parseSlash(newNode);

            if (found)
            {

                said_attach_subtree(parentNode, 0x143, 0x14a, newNode);

                return true;

            }
            else if (said_tokens[said_token] == (int)SaidToken.BRACKETO)
            {
                said_token++;

                found = parsePart3(newNode, out nonempty);

                if (found)
                {

                    if (said_tokens[said_token] == (int)SaidToken.BRACKETC)
                    {
                        said_token++;

                        said_attach_subtree(parentNode, 0x152, 0x143, newNode);

                        return true;
                    }
                }

            }

            // CHECKME: this doesn't look right if the [] section matched partially
            // Should the below 'if' be an 'else if' ?

            if (said_tokens[said_token] == (int)SaidToken.SLASH)
            {
                said_token++;

                nonempty = false;

                return true;

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseSlash(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            if (said_tokens[said_token] == (int)SaidToken.SLASH)
            {
                said_token++;

                bool found = parseExpr(parentNode);

                if (found)
                    return true;

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseList(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            bool found;

            ParseTreeNode newParent = parentNode;

            found = parseListEntry(newParent);

            if (found)
            {

                newParent = newParent.right;

                found = parseComma(newParent);

                return true;

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseComma(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            if (said_tokens[said_token] == (int)SaidToken.COMMA)
            {
                said_token++;

                bool found = parseList(parentNode);

                if (found)
                    return true;

            }

            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static bool parseWord(ParseTreeNode parentNode)
        {
            int token = said_tokens[said_token];
            if ((token & 0x8000) != 0)
                return false;

            said_token++;

            ParseTreeNode newNode = said_word_node(said_next_node(), token);

            parentNode.right = newNode;

            return true;
        }

        private static ParseTreeNode said_branch_attach_right(ParseTreeNode pos,
                                               ParseTreeNode right)
        {
            pos.type = ParseTypes.BranchNode;
            pos.right = right;

            return pos;
        }

        /*
        pos
        / \
       .   \
            *
           / \
          /   0
         *
        / \
       /   \
      /   subtree
    major  /   \
         /     .
      minor

    . = unchanged child node
    * = new branch node
    0 = NULL child node. (Location for future siblings of the subtree)

    */

        static bool said_attach_subtree(ParseTreeNode pos, int major, int minor,
                                        ParseTreeNode subtree)
        {
            bool retval = true;

            said_branch_attach_right(pos,
                said_branch_node(said_next_node(),
                    said_branch_node(said_next_node(),
                        said_leaf_node(said_next_node(), major),
                        said_branch_attach_left(subtree,
                            said_leaf_node(said_next_node(), minor))),
                    null));

            return retval;
        }

        private static ParseTreeNode said_branch_attach_left(ParseTreeNode pos,
                                              ParseTreeNode left)
        {
            pos.type = ParseTypes.BranchNode;
            pos.left = left;

            return pos;

        }

        private static bool parseListEntry(ParseTreeNode parentNode)
        {
            // Store current state for rolling back if we fail
            int curToken = said_token;
            int curTreePos = said_tree_pos;
            ParseTreeNode curRightChild = parentNode.right;

            ParseTreeNode newNode = said_branch_node(said_next_node(), null, null);

            bool found;

            if (said_tokens[said_token] == (int)SaidToken.BRACKETO)
            {
                said_token++;

                found = parseExpr(newNode);

                if (found)
                {

                    if (said_tokens[said_token] == (int)SaidToken.BRACKETC)
                    {
                        said_token++;

                        said_attach_subtree(parentNode, 0x152, 0x14c, newNode);

                        return true;
                    }
                }

            }
            else if (said_tokens[said_token] == (int)SaidToken.PARENO)
            {
                said_token++;

                found = parseExpr(newNode);

                if (found)
                {

                    if (said_tokens[said_token] == (int)SaidToken.PARENC)
                    {
                        said_token++;

                        said_attach_subtree(parentNode, 0x141, 0x14c, newNode);

                        return true;
                    }
                }

            }
            else if (parseWord(newNode))
            {

                said_attach_subtree(parentNode, 0x141, 0x153, newNode);

                return true;

            }


            // Rollback
            said_token = curToken;
            said_tree_pos = curTreePos;
            parentNode.right = curRightChild;
            return false;
        }

        private static ParseTreeNode said_next_node()
        {
            //assert(said_tree_pos > 0 && said_tree_pos < VOCAB_TREE_NODES);

            return said_tree[said_tree_pos++];
        }

        private static ParseTreeNode said_branch_node(ParseTreeNode pos,
                                       ParseTreeNode left,
                                       ParseTreeNode right)
        {
            pos.type = ParseTypes.BranchNode;
            pos.left = left;
            pos.right = right;

            return pos;
        }

        private static ParseTreeNode said_leaf_node(ParseTreeNode pos, int value)
        {
            pos.type = ParseTypes.LeafNode;
            pos.value = value;
            pos.right = null;

            return pos;
        }

        private static ParseTreeNode said_word_node(ParseTreeNode pos, int value)
        {
            pos.type = ParseTypes.WordNode;
            pos.value = value;
            pos.right = null;

            return pos;
        }

    }
}
