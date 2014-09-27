using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NScumm.Dump
{
	public class ElementAccess: Expression
	{
		private List<Expression> indices;
		private ReadOnlyCollection<Expression> roIndices;

		public Expression Target {
			get;
			private set;
		}

		public ReadOnlyCollection<Expression> Indices {
			get{ return roIndices; }
		}

		internal override string DebuggerDisplay {
			get { 
				var args = string.Join (", ", Indices.Select (arg => arg.DebuggerDisplay));
				return string.Format ("{0}[{1}]", Target.DebuggerDisplay, args);
			}
		}

		public ElementAccess (string target, Expression index)
			: this (new SimpleName (target), index)
		{
		}

		public ElementAccess (string target, object index)
			: this (new SimpleName (target), new LiteralExpression (index))
		{
		}

		public ElementAccess (Expression target, object index)
			: this (target, new LiteralExpression (index))
		{
		}

		public ElementAccess (Expression target, Expression index)
		{
			Target = target;
			indices = new List<Expression> { index };
			roIndices = new ReadOnlyCollection<Expression> (indices);
			ChildrenCore.Add (Target);
			ChildrenCore.Add (index);
		}

		public ElementAccess AddIndex (Expression index)
		{
			indices.Add (index);
			ChildrenCore.Add (index);
			return this;
		}

		#region implemented abstract members of Expression

		public override void Accept (IAstNodeVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override T Accept<T> (IAstNodeVisitor<T> visitor)
		{
			return visitor.Visit (this);
		}

		#endregion

	}
}

