//
//  Test.cs
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
using System;

namespace NScumm.Dump.Tests
{
    [TestFixture]
    public class ChangeJumpToIfFixture
    {
        [Test]
        public void ReplaceJumpToIf()
        {
            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new JumpStatement(
                        new BinaryExpression(
                            new SimpleName("Var1"), Operator.Greater, 0.ToLiteral()),
                        10){ StartOffset = 0, EndOffset = 5 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });
            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new IfStatement(
                        new BinaryExpression(
                            new SimpleName("Var1"), Operator.LowerOrEquals, 0.ToLiteral()),
                        new BlockStatement().AddStatement(
                            new ExpressionStatement(
                                new MethodInvocation("Print").AddArgument("Var1 is lower or equals to 0".ToLiteral())) { StartOffset = 5, EndOffset = 10 })
                    ){ StartOffset = 0, EndOffset = 10 },
                    new ExpressionStatement(
                        new MethodInvocation("Print").AddArgument("End".ToLiteral())) { StartOffset = 10, EndOffset = 15 }
                });

            var actualCu = new ReplaceJumpToIf().Replace(cu);

            AstHelper.AstEquals(expectedCu, actualCu);
        }

        [Test]
        public void ReplaceJumpToIfWithRealExample()
        {
            // cu =
            //            [   0,   5]  Push(156)
            //            [   5,  10]  Push(108)
            //            [  10,  15]  Variables[301] = Pop()
            //            [  15,  16]  StopObjectCode()
            //            [  16,  21]  Push(Bits[563])
            //            [  21,  22]  Push(!Pop())
            //            [  22,  27]  jump 48 if !Pop()
            //            [  27,  32]  Push(972)
            //            [  32,  37]  Push(972)
            //            [  37,  42]  Push(354)
            //            [  42,  43]  LoadRoomWithEgo(Pop(), Pop(), Pop(), Pop())
            //            [  43,  48]  jump 96 if True
            //            [  48,  53]  Push(Bits[9])
            //            [  53,  54]  Push(!Pop())
            //            [  54,  59]  Push(Bits[8])
            //            [  59,  60]  Push(!Pop())
            //            [  60,  61]  Push(Pop() & Pop())
            //            [  61,  66]  jump 80 if !Pop()
            //            [  66,  71]  Push(5)
            //            [  71,  76]  Push(1)
            //            [  76,  77]  CutScene(Pop(25))
            //            [  77,  79]  WaitForCamera()
            //            [  79,  80]  EndCutScene()
            //            [  80,  85]  Push(0)
            //            [  85,  90]  Push(351)
            //            [  90,  95]  Push(0)
            //            [  95,  96]  StartScript(Pop(), Pop(), Pop(25))
            //            [  96,  97]  StopObjectCode()

            var cu = new CompilationUnit().AddStatements(new Statement[]
                {
                    new MethodInvocation("Push").AddArgument(156).ToStatement(0, 5),
                    new MethodInvocation("Push").AddArgument(108).ToStatement(5, 10),
                    new BinaryExpression(new ElementAccess("Variables", 301), Operator.Assignment, new MethodInvocation("Pop")).ToStatement(10, 15),
                    new MethodInvocation("StopObjectCode").ToStatement(15, 16),
                    new MethodInvocation("Push").AddArgument(new ElementAccess("Bits", 563)).ToStatement(16, 21),
                    new MethodInvocation("Push").AddArgument(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not)).ToStatement(21, 22),
                    new JumpStatement(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not), 48, 22, 27),
                    new MethodInvocation("Push").AddArgument(972).ToStatement(27, 32),
                    new MethodInvocation("Push").AddArgument(972).ToStatement(32, 37),
                    new MethodInvocation("Push").AddArgument(354).ToStatement(37, 42),
                    new MethodInvocation("LoadRoomWithEgo").AddArguments(new MethodInvocation("Pop"), new MethodInvocation("Pop"), new MethodInvocation("Pop"), new MethodInvocation("Pop")).ToStatement(42, 43),
                    new JumpStatement(true.ToLiteral(), 96, 43, 48),
                    new MethodInvocation("Push").AddArgument(new ElementAccess("Bits", 9)).ToStatement(48, 53),
                    new MethodInvocation("Push").AddArgument(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not)).ToStatement(53, 54),
                    new MethodInvocation("Push").AddArgument(new ElementAccess("Bits", 8)).ToStatement(54, 59),
                    new MethodInvocation("Push").AddArgument(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not)).ToStatement(59, 60),
                    new MethodInvocation("Push").AddArgument(new BinaryExpression(new MethodInvocation("Pop"), Operator.And, new MethodInvocation("Pop"))).ToStatement(60, 61),
                    new JumpStatement(new UnaryExpression(new MethodInvocation("Pop"), Operator.Not), 80, 61, 66),
                    new MethodInvocation("Push").AddArgument(5).ToStatement(66, 71),
                    new MethodInvocation("Push").AddArgument(1).ToStatement(71, 76),
                    new MethodInvocation("CutScene").AddArgument(new MethodInvocation("Pop").AddArgument(25)).ToStatement(76, 77),
                    new MethodInvocation("WaitForCamera").ToStatement(77, 79),
                    new MethodInvocation("EndCutScene").ToStatement(79, 80),
                    new MethodInvocation("Push").AddArgument(0).ToStatement(80, 85),
                    new MethodInvocation("Push").AddArgument(351).ToStatement(85, 90),
                    new MethodInvocation("Push").AddArgument(0).ToStatement(90, 95),
                    new MethodInvocation("StartScript").AddArguments(new MethodInvocation("Pop"), new MethodInvocation("Pop"), new MethodInvocation("Pop").AddArgument(25)).ToStatement(95, 96),
                    new MethodInvocation("StopObjectCode").ToStatement(96, 97),
                });


            // expectedCu =
            //            [   0,   5]  Push(156)
            //            [   5,  10]  Push(108)
            //            [  10,  15]  Variables[301] = Pop()
            //            [  15,  16]  StopObjectCode()
            //            [  16,  21]  Push(Bits[563])
            //            [  21,  22]  Push(!Pop())
            //            [  22,  27]  if (!Pop())
            //                         {
            //            [  27,  32]    Push(972)
            //            [  32,  37]    Push(972)
            //            [  37,  42]    Push(354)
            //            [  42,  43]    LoadRoomWithEgo(Pop(), Pop(), Pop(), Pop())
            //            [  43,  48]    goto label_96
            //                         }
            //            [  48,  53]  Push(Bits[9])
            //            [  53,  54]  Push(!Pop())
            //            [  54,  59]  Push(Bits[8])
            //            [  59,  60]  Push(!Pop())
            //            [  60,  61]  Push(Pop() & Pop())
            //            [  61,  66]  if !Pop()
            //                         {
            //            [  66,  71]    Push(5)
            //            [  71,  76]    Push(1)
            //            [  76,  77]    CutScene(Pop(25))
            //            [  77,  79]    WaitForCamera()
            //            [  79,  80]    EndCutScene()
            //                         }
            //            [  80,  85]  Push(0)
            //            [  85,  90]  Push(351)
            //            [  90,  95]  Push(0)
            //            [  95,  96]  StartScript(Pop(), Pop(), Pop(25))
            // label_96:
            //            [  96,  97]  StopObjectCode()

            var replacers = new IAstReplacer[]
            {
                new ReplacePushAndPop(),
                new ResolveAllTypesOfVariables(8),
                new ReplaceJumpToIf(),
                new ReplaceJumpToWhile(),
                new ReplaceJumpToGoTo()
            };
            Array.ForEach(replacers, r => cu = r.Replace(cu));

            Console.WriteLine(AstHelper.ToString(cu, true));
        }

        [Test]
        public void ReplaceJumpToIfWithRealExample2()
        {
            // cu =
            //[0,65]{
            //[0, 3]  jump 59 if !ActiveObject.Locked
            //[3, 7]  Variables[1] = Actors[VariableEgo].GetBitVar(1)
            //[7,12]  jump 25 if Variables[1] != 1
            //[12,19] PrintEgo("Easy!")
            //[19,22] jump 59 if True
            //[22,25] jump 56 if True
            //[25,56] PrintEgo("I can't budge it. It's rusted shut.")
            //[56,59] jump 64 if True
            //[59,60] ActiveObject.State8 = True
            //[60,62] Objects[213].State8 = True
            //[62,64] StartSound(7)
            //[64,65] StopObjectCode()
            //[ 0,65]
            //    }

            var cu = new CompilationUnit().AddStatements(new Statement[]
            {
                new JumpStatement(new UnaryExpression(new MemberAccess("ActiveObject","Locked"),Operator.Not),59,0,3),
                new BinaryExpression(new ElementAccess("Variables", 1), Operator.Assignment,
                    new MethodInvocation(new MemberAccess(new ElementAccess("Actors","VariableEgo"),"GetBitVar")).AddArgument(1)).ToStatement(3, 7),
                new JumpStatement(new BinaryExpression(new ElementAccess("Variables",1),Operator.Inequals,1.ToLiteral()),25,7,12),
                new MethodInvocation("PrintEgo").AddArgument("Easy!").ToStatement(12, 19),
                new JumpStatement(true.ToLiteral(),59,19,22),
                new JumpStatement(true.ToLiteral(),56,19,22),
                new MethodInvocation("PrintEgo").AddArgument("I can't budge it. It's rusted shut.!").ToStatement(256, 56),
                new JumpStatement(true.ToLiteral(),64,56,59),
                new BinaryExpression(new MemberAccess("ActiveObject","State8"),Operator.Assignment,true.ToLiteral()).ToStatement(59,60),
                new BinaryExpression(new ElementAccess("Objects",213),Operator.Assignment,true.ToLiteral()).ToStatement(60,62),
                new MethodInvocation("StartSound").AddArgument(7).ToStatement(62,64),
                new MethodInvocation("StopObjectCode").ToStatement(64,65)
            });

            // expectedCu =
            //  if ActiveObject.Locked
            //  {
            //      Variables[1] = Actors[VariableEgo].GetBitVar(1)
            //      if Variables[1] == 1
            //      {
            //          PrintEgo("Easy!")
            //      }
            //      else
            //      {
            //          PrintEgo("I can't budge it. It's rusted shut.")
            //          StopObjectCode();
            //      }
            //  }
            //  ActiveObject.State8 = True
            //  213.State8 = True
            //  StartSound(7)
            //  StopObjectCode()

            var expectedCu = new CompilationUnit().AddStatements(new Statement[]
            {
                new IfStatement(new MemberAccess("ActiveObject","Locked")).SetTrueStatement(
                    new BinaryExpression(new ElementAccess("Variables", 1), Operator.Assignment,
                        new MethodInvocation(new MemberAccess(new ElementAccess("Actors","VariableEgo"),"GetBitVar")).AddArgument(1)).ToStatement(),
                    new IfStatement(new BinaryExpression(new ElementAccess("Variables",1),Operator.Equals,1.ToLiteral()))
                        .SetTrueStatement(new MethodInvocation("PrintEgo").AddArgument("Easy!").ToStatement())
                        .SetFalseStatement(
                            new MethodInvocation("PrintEgo").AddArgument("I can't budge it. It's rusted shut.!").ToStatement(),
                            new MethodInvocation("StopObjectCode").ToStatement())),
                new BinaryExpression(new MemberAccess("ActiveObject","State8"),Operator.Assignment,true.ToLiteral()).ToStatement(),
                new BinaryExpression(new ElementAccess("Objects",213),Operator.Assignment,true.ToLiteral()).ToStatement(),
                new MethodInvocation("StartSound").AddArgument(7).ToStatement(),
                new MethodInvocation("StopObjectCode").ToStatement()
            });

            var actualCu = new ReplaceJumpToIf().Replace(cu);
            Console.WriteLine(AstHelper.ToString(actualCu, false));

            AstHelper.AstEquals(expectedCu, actualCu);

        }
    }
}

