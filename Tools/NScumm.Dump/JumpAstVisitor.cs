//
//  JumpAstVisitor.cs
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

namespace NScumm.Dump
{
    public class JumpAstVisitor: DefaultVisitor
    {
        public List<JumpStatement> Jumps { get; private set; }

        public JumpAstVisitor()
        {
            Jumps = new List<JumpStatement>();
        }

        protected override void DefaultVisit(IAstNode node)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this);
            }
        }

        public override void Visit(JumpStatement node)
        {
            Jumps.Add(node);
        }
    }

}
