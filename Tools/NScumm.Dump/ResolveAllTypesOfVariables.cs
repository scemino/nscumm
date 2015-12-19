//
//  ResolveAllTypesOfVariables.cs
//
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

namespace NScumm.Dump
{
    public class ResolveAllTypesOfVariables: IAstReplacer
    {
        AstRewriterVisitor visitor;

        public ResolveAllTypesOfVariables(int version)
        {
            visitor = version == 8 ? (AstRewriterVisitor)new ResolveVariablesAstVisitor8() : new ResolveVariablesAstVisitor();
        }

        public CompilationUnit Replace(CompilationUnit cu)
        {
            var newCu = cu.Accept(visitor);
            return (CompilationUnit)newCu;
        }

        class ResolveVariablesAstVisitor: AstRewriterVisitor
        {
            public override IAstNode Visit(ElementAccess node)
            {
                var name = node.Target as SimpleName;
                if (name != null && name.Name == "Variables" && node.Indices.Count == 1)
                {
                    var indexExp = node.Indices[0] as IntegerLiteralExpression;
                    if (indexExp != null)
                    {
                        var index = indexExp.Value;
                        if ((index & 0xF000) == 0)
                        {
                            return node;
                        }

                        if ((index & 0x8000) != 0)
                        {
                            index &= 0x7FFF;
                            return new ElementAccess("Bits", index);
                        }

                        if ((index & 0x4000) != 0)
                        {
                            index &= 0xFFF;
                            return new ElementAccess("Locals", index);
                        }
                    }
                }
                return base.Visit(node);
            }
        }

        class ResolveVariablesAstVisitor8: AstRewriterVisitor
        {
            public override IAstNode Visit(ElementAccess node)
            {
                var name = node.Target as SimpleName;
                if (name != null && name.Name == "Variables" && node.Indices.Count == 1)
                {
                    var indexExp = node.Indices[0] as IntegerLiteralExpression;
                    if (indexExp != null)
                    {
                        var index = indexExp.Value;
                        if ((index & 0xF0000000) == 0)
                        {
                            return node;
                        }

                        if ((index & 0x80000000) != 0)
                        {
                            index &= 0x7FFFFFFF;
                            return new ElementAccess("Bits", index);
                        }

                        if ((index & 0x40000000) != 0)
                        {
                            index &= 0xFFFFFFF;
                            return new ElementAccess("Locals", index);
                        }
                    }
                }
                return base.Visit(node);
            }
        }
    }
}
