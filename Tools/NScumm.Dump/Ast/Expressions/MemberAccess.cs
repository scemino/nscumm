namespace NScumm.Dump
{
    public class MemberAccess: Expression
    {
        public Expression Target
        {
            get;
            private set;
        }

        public string Field
        {
            get;
            private set;
        }

        internal override string DebuggerDisplay
        {
            get
            { 
                return string.Format("{0}.{1}", Target.DebuggerDisplay, Field);
            }
        }

        public MemberAccess(Expression target, string field)
        {
            Target = target;
            Field = field;
            ChildrenCore.Add(Target);
        }

        public MemberAccess(string target, string field)
            : this(new SimpleName(target), field)
        {
        }

        #region implemented abstract members of Expression

        public override void Accept(IAstNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAstNodeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        #endregion

    }
}

