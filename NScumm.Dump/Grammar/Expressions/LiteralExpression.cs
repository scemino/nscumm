//
//  LiteralExpression.cs
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
using System;


namespace NScumm.Dump
{
    public interface ILiteralExpression
    {
        object Value
        {
            get;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class LiteralExpression<T>: Expression, ILiteralExpression
    {
        public T Value
        {
            get;
            private set;
        }

        object ILiteralExpression.Value
        {
            get{ return Value; }
        }

        internal override string DebuggerDisplay
        {
            get { return Value.ToString(); }    
        }

        protected LiteralExpression(T value)
        {
            Value = value;
        }
    }

    public static class LiteralExpressionExtensions
    {
        public static Expression ToLiteral(this object obj)
        {
            if (obj is Expression)
                throw new ArgumentException("obj");
            if (obj is int)
                return new IntegerLiteralExpression((int)obj);
            if (obj is byte[])
                return new StringLiteralExpression((byte[])obj);
            if (obj is string)
            {
                var text = (string)obj;
                return new StringLiteralExpression(System.Text.Encoding.ASCII.GetBytes(text));
            }
            if (obj is bool)
                return new BooleanLiteralExpression((bool)obj);

            throw new NotSupportedException("This type of literal is not supported.");
        }
    }
}
