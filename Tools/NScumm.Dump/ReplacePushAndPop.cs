//
//  ReplacePushAndPop.cs
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

using System.Collections.Generic;
using System;
using System.Linq;

namespace NScumm.Dump
{
    public class ReplacePushAndPop: IAstReplacer
    {
        public CompilationUnit Replace(CompilationUnit cu)
        {
            var visitor = new PushAstVisitor();
            return cu.Accept(visitor) as CompilationUnit;
        }

        class PushAstVisitor: AstRewriterVisitor
        {
            class Context: Stack<Expression>
            {
                public Context()
                {
                    EndOffset = long.MaxValue;
                }

                public Context(IEnumerable<Expression> expressions, long endOffset)
                    : base(expressions)
                {
                    EndOffset = endOffset;
                }

                public long EndOffset
                {
                    get;
                    private set;
                }
            }

            Stack<Context> contexts;

            Context CurrentContext { get { return contexts.Peek(); } }

            public PushAstVisitor()
            {
                contexts = new Stack<Context>();
                contexts.Push(new Context());
            }

            public override IAstNode Visit(JumpStatement node)
            {
                var newNode =  base.Visit(node);
                if (CurrentContext.EndOffset == node.EndOffset)
                {
                    contexts.Pop();
                }
                else if (node.JumpOffset > node.EndOffset)
                {
                    contexts.Push(new Context(CurrentContext.Reverse(), node.JumpOffset));
                }
                return newNode;
            }

            public override IAstNode Visit(ExpressionStatement node)
            {
                if (node.StartOffset >= CurrentContext.EndOffset)
                {
                    contexts.Pop();
                }
                if (node.Children.Count == 1)
                {
                    var expMethod = node.Children[0] as MethodInvocation;
                    if (expMethod != null)
                    {
                        var exp = expMethod.Target as SimpleName;
                        if (exp != null)
                        {
                            if (exp.Name == "Push")
                            {
                                expMethod.Arguments.ForEach(e =>
                                    {
                                        var arg = (Expression)e.Accept(this);
                                        CurrentContext.Push(arg);
                                    });
                                return null;
                            }
                            if (exp.Name == "Dup")
                            {
                                expMethod.Arguments.ForEach(e =>
                                    {
                                        var arg = (Expression)e.Accept(this);
                                        CurrentContext.Push(arg);
                                        CurrentContext.Push(arg);
                                    });
                                return null;
                            }
                        }
                    }
                }
                return base.Visit(node);
            }

            public override IAstNode Visit(MethodInvocation node)
            {
                // is it a pop ?
                var exp = node.Target as SimpleName;
                if (exp != null && exp.Name == "Pop")
                {
                    if (node.Arguments.Count == 1 && node.Arguments[0] is IntegerLiteralExpression)
                    {
                        var max = ((IntegerLiteralExpression)CurrentContext.Pop()).Value;
                        var num = Math.Min(CurrentContext.Count, max);
                        var args = Enumerable.Range(0, num).Select(_ => CurrentContext.Pop()).ToArray();
                        return new ArrayLiteralExpression(args);
                    }
                    else
                    {
                        // yes, so replace it by the last push
                        var pushExp = CurrentContext.Count > 0 ? CurrentContext.Pop() : new MethodInvocation("MissingPop?");
                        return pushExp;
                    }
                }
                else
                {
                    return new MethodInvocation((Expression)node.Target.Accept(this)).AddArguments(node.Arguments.Cast<Expression>().Reverse().Select(arg => (Expression)arg.Accept(this)).ToList());
                }
            }
        }
    }
    
}
