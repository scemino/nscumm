//
//  DoWhileStatement.cs
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
    public class DoWhileStatement: Statement
    {
        public Expression Condition { get; private set; }

        public Statement Statement { get; private set; }

        public DoWhileStatement(Expression condition, Statement statement, long? startOffset = null, long? endOffset = null)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            Condition = condition;
            Statement = statement;
            ChildrenCore.Add(Condition);
            ChildrenCore.Add(Statement);
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

