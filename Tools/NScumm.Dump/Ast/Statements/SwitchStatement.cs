//
//  SwitchStatement.cs
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
using System.Collections.Generic;
using System.Linq;

namespace NScumm.Dump
{

    public class SwitchStatement: Statement
    {
        public Expression Condition { get; private set; }

        public IEnumerable<CaseStatement> CaseStatements { get { return ChildrenCore.OfType<CaseStatement>(); } }

        public SwitchStatement(Expression condition, long? startOffset = null, long? endOffset = null)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            Condition = condition;
        }

        public SwitchStatement Add(CaseStatement statement)
        {
            ChildrenCore.Add(statement);
            return this;
        }

        public SwitchStatement Add(IEnumerable<CaseStatement> statements)
        {
            ChildrenCore.AddRange(statements);
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
