namespace NScumm.Dump
{
    public class DefaultVisitor: IAstNodeVisitor
    {

        #region IAstNodeVisitor implementation

        public virtual void Visit(CompilationUnit node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(LiteralExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(MethodInvocation node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(ExpressionStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(IfStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(SimpleName node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(MemberAccess node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(ElementAccess node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(UnaryExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(BinaryExpression node)
        {
            DefaultVisit(node);
        }

        #endregion

        protected virtual void DefaultVisit(IAstNode node)
        {
        }
    }

    public class DefaultVisitor<T>: IAstNodeVisitor<T>
    {

        #region IAstNodeVisitor implementation

        public virtual T Visit(CompilationUnit node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(IfStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(LiteralExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(MethodInvocation node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(ExpressionStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(SimpleName node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(MemberAccess node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(ElementAccess node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(UnaryExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(BinaryExpression node)
        {
            return DefaultVisit(node);
        }

        #endregion

        protected virtual T DefaultVisit(IAstNode node)
        {
            return default(T);
        }
    }
}

