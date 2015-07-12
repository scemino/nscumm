namespace NScumm.Dump
{
    public class DefaultVisitor: IAstNodeVisitor
    {
        #region IAstNodeVisitor implementation

        public virtual void Visit(CompilationUnit node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(BlockStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(IntegerLiteralExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(EnumExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(ArrayLiteralExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(StringLiteralExpression node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(BooleanLiteralExpression node)
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

        public virtual void Visit(JumpStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(IfStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(CaseStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(SwitchStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(GoToStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(LabelStatement node)
        {
            DefaultVisit(node);
        }

        public virtual void Visit(DoWhileStatement node)
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

        public virtual T Visit(BlockStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(JumpStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(IfStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(CaseStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(SwitchStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(GoToStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(LabelStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(DoWhileStatement node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(IntegerLiteralExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(EnumExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(ArrayLiteralExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(StringLiteralExpression node)
        {
            return DefaultVisit(node);
        }

        public virtual T Visit(BooleanLiteralExpression node)
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

