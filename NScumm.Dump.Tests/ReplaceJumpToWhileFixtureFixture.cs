//
//  ReplaceJumpToWhileFixture.cs
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

namespace NScumm.Dump.Tests
{
    [TestFixture]
    public class ReplaceJumpToWhileFixture
    {
        [Test]
        public void ReplaceJumpToWhile()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new ExpressionStatement(new MethodInvocation("Delay").AddArgument(60)){ StartOffset = 0, EndOffset = 4 },
                    new ExpressionStatement(new BinaryExpression(new ElementAccess("LocalVariables", 0), Operator.Assignment, new MethodInvocation("GetRandomNumber").AddArgument(10))){ StartOffset = 4, EndOffset = 8 },
                    new ExpressionStatement(new BinaryExpression(new ElementAccess("Variables", 103), Operator.Assignment, 0.ToLiteral())){ StartOffset = 8, EndOffset = 13 },
                    new JumpStatement(true.ToLiteral(), 0){ StartOffset = 13, EndOffset = 16 },
                    new ExpressionStatement(new MethodInvocation("StopObjectCode")){ StartOffset = 16, EndOffset = 17 },
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new DoWhileStatement(true.ToLiteral(), new BlockStatement().AddStatements(
                            new Statement[]
                            {
                                new ExpressionStatement(new MethodInvocation("Delay").AddArgument(60)){ StartOffset = 0, EndOffset = 4 },
                                new ExpressionStatement(new BinaryExpression(new ElementAccess("LocalVariables", 0), Operator.Assignment, new MethodInvocation("GetRandomNumber").AddArgument(10))){ StartOffset = 4, EndOffset = 8 },
                                new ExpressionStatement(new BinaryExpression(new ElementAccess("Variables", 103), Operator.Assignment, 0.ToLiteral())){ StartOffset = 8, EndOffset = 13 },
                            })),
                    new ExpressionStatement(new MethodInvocation("StopObjectCode")){ StartOffset = 16, EndOffset = 17 },
                });

            var actualCu = new ReplaceJumpToWhile().Replace(cu);
            AstHelper.AstEquals(expectedCu, actualCu);
        }
    }
}

