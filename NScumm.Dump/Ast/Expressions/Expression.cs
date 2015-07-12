
namespace NScumm.Dump
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class Expression : AstNodeBase
    {
        internal abstract string DebuggerDisplay
        {
            get;
        }
    }

    public static class ExpressionExtensions
    {
        public static Statement ToStatement(this Expression exp)
        {
            return new ExpressionStatement(exp);
        }

        public static Statement ToStatement(this Expression exp, long? startOffset, long? endOffset)
        {
            return new ExpressionStatement(exp, startOffset, endOffset);
        }

        public static Expression Not(this Expression condition)
        {
            var unExp = condition as UnaryExpression;
            if (unExp != null)
            {
                return unExp.Not();
            }
            var boolExp = condition as BooleanLiteralExpression;
            if (boolExp != null)
            {
                return new BooleanLiteralExpression(!boolExp.Value);
            }
            var binExp = condition as BinaryExpression;
            if (binExp != null)
            {
                return binExp.Not();
            }
            return new UnaryExpression(condition, Operator.Not);
        }
    }
}
