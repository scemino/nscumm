//
//  ReplaceJumpToWhile.cs
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

using System.Linq;
using System;

namespace NScumm.Dump
{
    public interface IAstReplacer
    {
        CompilationUnit Replace(CompilationUnit cu);
    }

    public class ReplaceJumpToWhile: IAstReplacer
    {
        public CompilationUnit Replace(CompilationUnit cu)
        {
            var visitor = new JumpAstVisitor();
            cu.Accept(visitor);
            var jumps = visitor.Jumps.Where(jump => jump.JumpOffset < jump.StartOffset).OrderByDescending(jmp => jmp.JumpOffset).ThenByDescending(jmp => jmp.StartOffset).ToList();
            foreach (var jump in jumps)
            {
                cu = new CompilationUnit().AddStatements(ReplaceJump(jump, cu.Statement));
            }
            return cu;
        }

        static BlockStatement ReplaceJump(JumpStatement jump, BlockStatement block)
        {
            if (jump.StartOffset < block.StartOffset)
                throw new ArgumentOutOfRangeException("jump", "jump should be inside the given block");
            if (jump.JumpOffset > block.EndOffset)
                throw new ArgumentOutOfRangeException("jump", "jump should be inside the given block");

            var newBlock = new BlockStatement();
            var doWhileStatement = new DoWhileStatement(jump.Condition, new BlockStatement()){ StartOffset = jump.StartOffset, EndOffset = jump.JumpOffset };
            var inside = false;
            foreach (var statement in block)
            {
                if (statement.StartOffset == jump.JumpOffset)
                {                    
                    ((BlockStatement)doWhileStatement.Statement).AddStatement(statement);
                    inside = true;
                }
                else if (statement.StartOffset == jump.StartOffset)
                {
                    if (doWhileStatement == null)
                        throw new InvalidOperationException("DoWhileStatement can't be null");
                    newBlock.AddStatement(doWhileStatement);
                    doWhileStatement = null;
                    inside = false;
                }
                else if (inside)
                {
                    ((BlockStatement)doWhileStatement.Statement).AddStatement(statement);
                }
                else
                {
                    var lastStatement = newBlock.LastOrDefault();
                    if (lastStatement != null && lastStatement.EndOffset > statement.StartOffset)
                    {
                        throw new NotSupportedException("invalid Statement");
                    }
                    newBlock.AddStatement(statement);
                }
            }
            return newBlock;
        }
    }
}

