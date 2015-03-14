//
//  ResolveVariablesAstVisitor.cs
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
    public class ResolveVariables: IAstReplacer
    {
        readonly ResolveVariablesAstVisitor visitor;

        public ResolveVariables(IDictionary<int,string> knownVariables)
        {
            visitor = new ResolveVariablesAstVisitor(knownVariables);
        }

        public CompilationUnit Replace(CompilationUnit cu)
        {
            var newCu = cu.Accept(visitor);
            return (CompilationUnit)newCu;
        }

        class ResolveVariablesAstVisitor: AstRewriterVisitor
        {
            IDictionary<int, string> KnownVariables{ get; set; }

            public ResolveVariablesAstVisitor(IDictionary<int,string> knownVariables)
            {
                KnownVariables = knownVariables;
            }

            public override IAstNode Visit(ElementAccess node)
            {
                var name = node.Target as SimpleName;
                if (name != null && name.Name == "Variables" && node.Indices.Count == 1)
                {
                    var index = node.Indices[0] as IntegerLiteralExpression;
                    if (index != null)
                    {
                        if (KnownVariables.ContainsKey(index.Value))
                        {
                            return new SimpleName(KnownVariables[index.Value]);
                        }
                    }
                }
                return new ElementAccess((Expression)node.Target.Accept(this), node.Indices.Select(idx => (Expression)idx.Accept(this)));
            }
        }
    }
}
