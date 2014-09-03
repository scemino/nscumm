namespace NScumm.Tmp
{
	public class UnaryExpression: Expression
	{
		public Operator Operator {
			get;
			private set;
		}

		public Expression Expression {
			get;
			private set;
		}

		internal override string DebuggerDisplay {
			get { 
				return string.Format ("{0} {1}", Operator, Expression.DebuggerDisplay);
			}
		}

		public UnaryExpression (Expression expression, Operator op)
		{
			Operator = op;
			Expression = expression;
			ChildrenCore.Add (Expression);
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

