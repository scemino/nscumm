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


namespace NScumm.Tmp
{
	[System.Diagnostics.DebuggerDisplay ("{DebuggerDisplay,nq}")]
	public class LiteralExpression: Expression
	{
		public object Value {
			get;
			private set;
		}

		internal override string DebuggerDisplay {
			get { return Value.ToString(); }    
		}

		public LiteralExpression (object value)
		{
			this.Value = value;
		}

		public override void Accept (IAstNodeVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override T Accept<T> (IAstNodeVisitor<T> visitor)
		{
			return visitor.Visit (this);
		}
	}

	public static class LiteralExpressionExtensions
	{
		public static LiteralExpression ToLiteral (this object obj)
		{
			if (obj is Expression)
				throw new System.ArgumentException ("obj");
			return new LiteralExpression (obj);
		}
	}
}
