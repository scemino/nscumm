//
//  ResolveVariablesAstVisitorFixture.cs
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
using NUnit.Framework;
using System.Collections.Generic;

namespace NScumm.Dump.Tests
{
    [TestFixture]
    public class ResolveVariablesAstVisitorFixture
    {
        [Test]
        public void ResolveVariable()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(CreateVariable(1), Operator.Assignment, CreateVariable(2)).ToStatement(),
                    new BinaryExpression(CreateVariable(1), Operator.Assignment, CreateVariable(3)).ToStatement()
                });

            var knownVariables = new Dictionary<int,string>{ { 1, "Foo1" }, { 2,"Foo2" } };
            var resolver = new ResolveVariables(knownVariables);
            var actualCu = resolver.Replace(cu);

            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(new SimpleName("Foo1"), Operator.Assignment, new SimpleName("Foo2")).ToStatement(),
                    new BinaryExpression(new SimpleName("Foo1"), Operator.Assignment, CreateVariable(3)).ToStatement()
                });

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        static Expression CreateVariable(int index)
        {
            return new ElementAccess("Variables", index.ToLiteral());
        }
    }
}

