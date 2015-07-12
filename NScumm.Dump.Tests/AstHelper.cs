//
//  AstHelper.cs
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
using System;
using NFluent;

namespace NScumm.Dump.Tests
{
    public static class AstHelper
    {
        public static string ToString(IAstNode ast, bool showOffsets = false)
        {
            var dumper = new DumpAstVisitor(showOffsets);
            var code = ast.Accept(dumper);
            return code;
        }

        public static void AstEquals(IAstNode astExpected, IAstNode astActual, bool showOffsets = false)
        {
            var dumper = new DumpAstVisitor(showOffsets);

            var expectedCode = astExpected.Accept(dumper);
            var actualCode = astActual.Accept(dumper);

            Check.That(actualCode).IsEqualTo(expectedCode);
            Console.WriteLine(expectedCode);
        }
    }
}

