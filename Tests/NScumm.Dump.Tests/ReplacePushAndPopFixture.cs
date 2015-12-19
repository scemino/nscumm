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
    public class ReplacePushAndPopFixture
    {
        [Test]
        public void ReplacePushAndPop()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new ExpressionStatement(
                        new MethodInvocation("Push").AddArgument(2.ToLiteral())){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Push").AddArgument(1.ToLiteral())){ StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArguments(new MethodInvocation("Pop"), new MethodInvocation("Pop"))) { StartOffset = 10, EndOffset = 15 }
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArguments(1.ToLiteral(), 2.ToLiteral())) { StartOffset = 0, EndOffset = 15 },
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithOffsets()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new ExpressionStatement(
                        new MethodInvocation("Push").AddArgument(2.ToLiteral())){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Push").AddArgument(1.ToLiteral())){ StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArguments(new MethodInvocation("Pop"), new MethodInvocation("Pop"))) { StartOffset = 10, EndOffset = 15 }
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArguments(1.ToLiteral(), 2.ToLiteral())) { StartOffset = 0, EndOffset = 15 },
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu, true);
        }

        [Test]
        public void ReplacePushAndPopWithUnary()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Push").AddArgument(0.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(false.ToLiteral()).ToStatement(),
                    new JumpStatement(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not), 0)
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(new UnaryExpression(false.ToLiteral(), Operator.Not), 0)
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithDup()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Dup").AddArgument(false.ToLiteral()).ToStatement(),
                    new JumpStatement(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not), 0),
                    new MethodInvocation("Print").AddArguments(new MethodInvocation("Pop")).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(new UnaryExpression(false.ToLiteral(), Operator.Not), 0),
                    new MethodInvocation("Print").AddArgument(false).ToStatement(),
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithPushRecurse()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Push").AddArgument(1.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(2.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(new BinaryExpression(
                            new MethodInvocation("Pop"), Operator.Equals, new MethodInvocation("Pop"))).ToStatement(),
                    new MethodInvocation("Print").AddArguments(new MethodInvocation("Pop")).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Print").AddArgument(
                        new BinaryExpression(2.ToLiteral(), Operator.Equals, 1.ToLiteral())).ToStatement()
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithPushDup()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Push").AddArgument(new ElementAccess("Variables", 1073741827)).ToStatement(),
                    new MethodInvocation("Dup").AddArgument(new MethodInvocation("Pop")).ToStatement(),
                    new MethodInvocation("Push").AddArgument(0.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(
                        new BinaryExpression(
                            new MethodInvocation("Pop"), Operator.Equals, new MethodInvocation("Pop"))).ToStatement(),
                    new JumpStatement(new MethodInvocation("Pop"), 33)
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(new BinaryExpression(
                            0.ToLiteral(), Operator.Equals, new ElementAccess("Variables", 1073741827)), 33)
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithMultiPops()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new MethodInvocation("Push").AddArgument(0.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(1.ToLiteral()).ToStatement(),
                    new MethodInvocation("Push").AddArgument(2.ToLiteral()).ToStatement(),
                    new MethodInvocation("Print").AddArgument(
                        new MethodInvocation("Pop").AddArgument(5.ToLiteral())).ToStatement()
                });
            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new MethodInvocation("Print").AddArguments(
                        new ArrayLiteralExpression(new[]{ 2.ToLiteral(), 1.ToLiteral(), 0.ToLiteral() })).ToStatement()
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplacePushAndPopWithJump()
        {
            var cu = new CompilationUnit().AddStatements(new []
                {
                    new MethodInvocation("Push").AddArgument(true.ToLiteral()).ToStatement(0, 5),
                    new JumpStatement(new ElementAccess("Bits", 0), 15, 5, 10),
                    new MethodInvocation("Print").AddArgument(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not)).ToStatement(10, 15),
                    new MethodInvocation("Print").AddArgument(new MethodInvocation("Pop")).ToStatement(15, 20)
                });
            var expectedCu = new CompilationUnit().AddStatements(new []
                {
                    new JumpStatement(new ElementAccess("Bits", 0), 15, 5, 10),
                    new MethodInvocation("Print").AddArgument(new UnaryExpression(true.ToLiteral(), Operator.Not)).ToStatement(10, 15),
                    new MethodInvocation("Print").AddArgument(true.ToLiteral()).ToStatement(15, 20)
                });

            var actualCu = new ReplacePushAndPop().Replace(cu);
            AstHelper.AstEquals(expectedCu, actualCu);
        }
    }
}

