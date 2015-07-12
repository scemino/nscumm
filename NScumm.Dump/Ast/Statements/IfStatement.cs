//
//  IfStatement.cs
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

namespace NScumm.Dump
{
    public class IfStatement : Statement
    {
        public Expression Condition { get; private set; }

        public Statement TrueStatement { get; private set; }

        public Statement FalseStatement { get; private set; }

        public IfStatement(Expression condition, Statement trueStatement = null, Statement falseStatement = null, long? startOffset = null, long? endOffset = null)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            Condition = condition;
            TrueStatement = trueStatement ?? new BlockStatement();
            FalseStatement = falseStatement ?? new BlockStatement();
            ChildrenCore.Add(Condition);
            ChildrenCore.Add(TrueStatement);
        }

        public IfStatement SetTrueStatement(params Statement[] trueStatements)
        {
            return new IfStatement(Condition, new BlockStatement().AddStatements(trueStatements));
        }

        public IfStatement SetTrueStatement(Statement trueStatement)
        {
            return new IfStatement(Condition, trueStatement);
        }

        public IfStatement SetFalseStatement(Statement falseStatement)
        {
            return new IfStatement(Condition, falseStatement: falseStatement);
        }

        public IfStatement SetFalseStatement(params Statement[] falseStatements)
        {
            return new IfStatement(Condition, falseStatement: new BlockStatement().AddStatements(falseStatements));
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

