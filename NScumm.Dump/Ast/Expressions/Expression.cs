
namespace NScumm.Dump
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class Expression: AstNodeBase
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
    }
}
