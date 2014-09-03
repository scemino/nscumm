namespace NScumm.Tmp
{
	public enum Operator
	{
		Lower,
		LowerOrEquals,
		Greater,
		GreaterOrEquals,
		Divide,
		Multiply,
		Add,
		Assignment,
		And,
		Or,
		Not,
		Equals,
		Inequals,
		Minus,
		PostIncrement,
		PostDecrement
	}

	public class BinaryExpression: Expression
	{
		public Expression Left {
			get;
			private set;
		}

		public Operator Operator {
			get;
			private set;
		}

		public Expression Right {
			get;
			private set;
		}

		internal override string DebuggerDisplay {
			get { 
				return string.Format ("{0} {1} {2}", Left.DebuggerDisplay, Operator, Right.DebuggerDisplay);
			}
		}

		public BinaryExpression (Expression left, Operator op, Expression right)
		{
			Left = left;
			Operator = op;
			Right = right;
			ChildrenCore.Add (Left);
			ChildrenCore.Add (Right);
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

