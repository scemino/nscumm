//
//  ReplaceJumpToGoTo.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Linq;
using System.Collections.Generic;

namespace NScumm.Dump
{
    public class ReplaceJumpToGoTo: IAstReplacer
    {
        public CompilationUnit Replace(CompilationUnit cu)
        {
            var visitor = new JumpAstVisitor();
            cu.Accept(visitor);
            var jumps = visitor.Jumps.Where(AcceptJump).ToList();
            var newCu = cu.Accept(new JumpReplacer());
            newCu.Accept(new LabelInserter(jumps));
            return (CompilationUnit)newCu;
        }

        static bool AcceptJump(JumpStatement node)
        {
            var condition = node.Condition as BooleanLiteralExpression;
            return (condition != null && condition.Value);
        }

        static string CreateLabel(JumpStatement node)
        {
            return string.Format("label_{0}", node.JumpOffset);
        }

        class LabelInserter: DefaultVisitor
        {
            public IList<JumpStatement> Statements{ get; private set; }

            public LabelInserter(IList<JumpStatement> statements)
            {
                Statements = statements;
            }

            protected override void DefaultVisit(IAstNode node)
            {
                var statement = node as Statement;
                if (statement != null)
                {
                    var jmp = Statements.FirstOrDefault(jump => statement.StartOffset == jump.JumpOffset);
                    if (jmp != null)
                    {
                        var index = node.Parent.Children.IndexOf(node);
                        node.Parent.Children.Insert(index, new LabelStatement(CreateLabel(jmp)));
                    }
                }

                foreach (var child in node.Children.ToList())
                {
                    child.Accept(this);
                }
            }
        }

        class JumpReplacer: AstRewriterVisitor
        {
            public override IAstNode Visit(JumpStatement node)
            {
                IAstNode n;
                if (AcceptJump(node))
                {
                    n = new GoToStatement(CreateLabel(node)){ StartOffset = node.StartOffset, EndOffset = node.EndOffset };
                }
                else
                {
                    n = node;
                }
                return n;
            }
        }
    }
}

