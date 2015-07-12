//
//  ScriptParser.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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

using System.IO;
using System.Collections.Generic;
using System;
using NScumm.Core.IO;
using System.Linq;

namespace NScumm.Dump
{
    public abstract class ScriptParser
    {
        protected BinaryReader _br;
        protected Dictionary<int, Func<Statement>> opCodes;
        protected int _opCode;

        public Dictionary<int, string> KnownVariables { get; protected set; }

        public GameInfo Game
        {
            get;
            private set;
        }

        protected ScriptParser(GameInfo info)
        {
            Game = info;
        }

        protected abstract void InitOpCodes();

        protected Statement ExecuteOpCode()
        {
            Func<Statement> func;
            if (!opCodes.ContainsKey(_opCode))
                func = () => new MethodInvocation(string.Format("UnknownOpcode{0:X2}", _opCode)).ToStatement();
            else
                func = opCodes[_opCode];
            var startOffset = _br.BaseStream.Position - 1;
            var statement = func();
            var endOffset = _br.BaseStream.Position;

            statement.StartOffset = startOffset;
            statement.EndOffset = endOffset;
            return statement;
        }

        protected void AddKnownVariables(IDictionary<int, string> knownVariables)
        {
            foreach (var item in knownVariables)
            {
                KnownVariables.Add(item.Key, item.Value);
            }
        }

        public static ScriptParser Create(GameInfo info)
        {
            ScriptParser parser;
            switch (info.Version)
            {
                case 0:
                    parser = new ScriptParser0(info);
                    break;
                case 3:
                    parser = new ScriptParser3(info);
                    break;
                case 4:
                    parser = new ScriptParser4(info);
                    break;
                case 5:
                    parser = new ScriptParser5(info);
                    break;
                case 6:
                    parser = new ScriptParser6(info);
                    break;
                case 7:
                    parser = new ScriptParser7(info);
                    break;
                case 8:
                    parser = new ScriptParser8(info);
                    break;
                default:
                    throw new NotSupportedException(string.Format("SCUMM version {0} not supported.", info.Version));
            }
            parser.InitOpCodes();
            return parser;
        }

        public CompilationUnit Parse(byte[] data)
        {
            var compilationUnit = new CompilationUnit();
            _br = new BinaryReader(new MemoryStream(data));
            while (_br.BaseStream.Position < _br.BaseStream.Length)
            {
                _opCode = _br.ReadByte();
                try
                {
                    var statement = ExecuteOpCode();
                    compilationUnit.AddStatement(statement);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                    return compilationUnit;
                }
            }
			
            return compilationUnit;
        }

        protected int ReadByte()
        {
            return _br.ReadByte();
        }

        protected virtual int ReadWord()
        {
            var word = _br.ReadUInt16();
            return word;
        }

        protected virtual int ReadWordSigned()
        {
            return (short)ReadWord();
        }

        protected virtual Expression ReadCharacters()
        {
            var sb = new List<byte>();
            var character = (byte)ReadByte();
            while (character != 0)
            {
                sb.Add(character);
                if (character == 0xFF)
                {
                    character = (byte)ReadByte();
                    sb.Add(character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        var count = Game.Version == 8 ? 4 : 2;
                        sb.AddRange(from i in Enumerable.Range(0, count)
                                                         select (byte)ReadByte());
                    }
                }
                character = (byte)ReadByte();
            }
            return new StringLiteralExpression(sb.ToArray());
        }

        protected virtual Expression ReadVariable(int var)
        {
            if ((var & 0x2000) == 0x2000)
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    var exp = ReadVariable(a & ~0x2000);
                    var literalExp = exp as IntegerLiteralExpression;
                    if (literalExp != null)
                    {
                        var += Convert.ToInt32(literalExp.Value);
                    }
                    else
                    {
                        return ReadVariable2(exp);
                    }
                }
                else
                {
                    var += a & 0xFFF;
                }
                var &= ~0x2000;
            }

            return ReadVariable2(var);
        }

        protected Expression ReadVariable2(Expression var)
        {
            return new MethodInvocation("ReadVariable").AddArgument(var);
        }

        protected Expression ReadVariable2(int var)
        {
            if ((var & 0xF000) == 0)
            {
                return new ElementAccess(
                    new SimpleName("Variables"),
                    var.ToLiteral());
            }

            if ((var & 0x8000) == 0x8000)
            {
                var &= 0x7FFF;

                return new ElementAccess(
                    new SimpleName("BitVariables"),
                    var.ToLiteral());
            }

            if ((var & 0x4000) == 0x4000)
            {
                var &= 0xFFF;

                return new ElementAccess("LocalVariables", var);
            }
            throw new NotSupportedException("Illegal varbits (r)");
        }
    }
}

