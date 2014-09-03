
namespace NScumm.Tmp
{
    public abstract class Statement: AstNodeBase
    {
        public long? StartOffset { get; set; }

        public long? EndOffset { get; set; }
    }

    public class ExpressionStatement: Statement
    {
        public Expression Expression
        {
            get;
            private set;
        }

        public ExpressionStatement(Expression exp)
        {
            this.Expression = exp;
            ChildrenCore.Add(Expression);
        }

        public override void Accept(IAstNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAstNodeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

}

