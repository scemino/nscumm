namespace NScumm.Dump
{
	public class MemberAccess: Expression
	{
		public Expression Target {
			get;
			private set;
		}

		public Expression Field {
			get;
			private set;
		}

		internal override string DebuggerDisplay {
			get { 
				return string.Format ("{0}.{1}", Target.DebuggerDisplay, Field.DebuggerDisplay);
			}
		}

		public MemberAccess (Expression target, Expression field)
		{
			Target = target;
			Field = field;
			ChildrenCore.Add (Target);
			ChildrenCore.Add (Field);
		}

		public MemberAccess (string target, string field)
			: this (target.ToLiteral(), field.ToLiteral())
		{
		}

		public MemberAccess (string target, Expression field)
			: this (target.ToLiteral(), field)
		{
		}

		public MemberAccess (Expression target, string field)
			: this (target, field.ToLiteral())
		{
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

