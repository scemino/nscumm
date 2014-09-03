//
//  IDumper.cs
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

namespace NScumm.Tmp
{
    public interface IDumper
    {
        void Indent();

        void Deindent();

        void Write(string format, params object[] args);

        void Write(string text);

        void WriteLine(string format, params object[] args);

        void WriteLine(string text);

        void WriteLine();
    }


    class ConsoleDumper: IDumper
    {
        int indentLevel;

        #region IDumper implementation

        public void Indent()
        {
            indentLevel++;
        }

        public void Deindent()
        {
            if (indentLevel > 0)
            {
                indentLevel--;
            }
        }

        void WriteIndentLevel()
        {
            Console.Write(new string(' ', indentLevel * 2));
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public void Write(string text)
        {
            var lines = text.Split('\n');
            if (lines.Length == 1)
                WriteIndentLevel();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == lines.Length - 1)
                {
                    Console.Write(lines[i]);
                }
                else
                {
                    WriteIndentLevel();
                    Console.WriteLine(lines[i]);
                }
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public void WriteLine(string text)
        {
            Write(text + Environment.NewLine);
        }

        public void WriteLine()
        {
            Write(Environment.NewLine);
        }

        #endregion


    }
}
