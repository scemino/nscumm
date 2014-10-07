//
//  MethodInvocation.cs
//
//  Author:
//       Valéry Sablonnière <scemino74@gmail.com>
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

using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NScumm.Dump
{
    public class MethodInvocation: Expression
    {
        public List<Expression> Arguments
        {
            get;
            private set;
        }

        public Expression Target
        {
            get;
            private set;
        }

        internal override string DebuggerDisplay
        {
            get
            { 
                var args = string.Join(", ", Arguments.Select(arg => arg.DebuggerDisplay));
                return string.Format("{0}({1})", Target.DebuggerDisplay, args);
            }    
        }

        public MethodInvocation(string name)
            : this(new SimpleName(name))
        {
        }

        public MethodInvocation(Expression target)
        {
            Target = target;
            ChildrenCore.Add(target);
            Arguments = new List<Expression>();
        }

        public MethodInvocation AddArgument(object literal)
        {
            return AddArgument(literal.ToLiteral());
        }

        public MethodInvocation AddArgument(Expression exp)
        {
            Arguments.Add(exp);
            ChildrenCore.Add(exp);
            return this;
        }

        public MethodInvocation AddArguments(IEnumerable<Expression> expressions)
        {
            Arguments.AddRange(expressions);
            ChildrenCore.AddRange(expressions);
            return this;
        }

        public MethodInvocation AddArguments(params Expression[] expressions)
        {
            Arguments.AddRange(expressions);
            ChildrenCore.AddRange(expressions);
            return this;
        }

        public override void Accept(IAstNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAstNodeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
