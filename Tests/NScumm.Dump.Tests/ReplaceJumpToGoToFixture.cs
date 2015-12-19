//
//  ReplaceJumpToGoToFixture.cs
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
    public class ReplaceJumpToGoToFixture
    {
        [Test]
        public void ReplaceJumpToGoTo()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(true.ToLiteral(), 10){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new GoToStatement("label_10"),
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 },
                    new LabelStatement("label_10"),
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });

            var actualCu = new ReplaceJumpToGoTo().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplaceJumpToGoToIfNecessary()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(false.ToLiteral(), 10){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(false.ToLiteral(), 10){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });

            var actualCu = new ReplaceJumpToGoTo().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }
    }
}

