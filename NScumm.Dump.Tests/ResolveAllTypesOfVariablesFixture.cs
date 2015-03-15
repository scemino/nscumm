//
//  ResolveAllTypesOfVariablesFxiture.cs
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

using NUnit.Framework;

namespace NScumm.Dump.Tests
{
    [TestFixture]
    public class ResolveAllTypesOfVariablesFixture
    {
        [Test]
        public void ReplaceVariables()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Variables", 0), Operator.Assignment, new ElementAccess("Variables", 1)).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Variables", 0), Operator.Assignment, new ElementAccess("Variables", 1)).ToStatement()
                });
            var resolver = new ResolveAllTypesOfVariables(8);
            var actualCu = resolver.Replace(cu);
            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplaceBitVariables()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Variables", 0x80000000), Operator.Assignment, new ElementAccess("Variables", 0x80000001)).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Bits", 0), Operator.Assignment, new ElementAccess("Bits", 1)).ToStatement()
                });
            var resolver = new ResolveAllTypesOfVariables(8);
            var actualCu = resolver.Replace(cu);
            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplaceLocalVariables()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Variables", 0x40000000), Operator.Assignment, new ElementAccess("Variables", 0x40000001)).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new BinaryExpression(
                        new ElementAccess("Locals", 0), Operator.Assignment, new ElementAccess("Locals", 1)).ToStatement()
                });
            var resolver = new ResolveAllTypesOfVariables(8);
            var actualCu = resolver.Replace(cu);
            AstHelper.AstEquals(expectedCu, actualCu);
        }
    }
}

