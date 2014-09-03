namespace NScumm.Tmp
{
    public interface IAstNodeVisitor
    {
        void Visit(CompilationUnit node);

        void Visit(ExpressionStatement node);

        void Visit(IfStatement node);

        void Visit(SimpleName node);

        void Visit(LiteralExpression node);

        void Visit(MethodInvocation node);

        void Visit(MemberAccess node);

        void Visit(ElementAccess node);

        void Visit(UnaryExpression node);

        void Visit(BinaryExpression node);
    }

    public interface IAstNodeVisitor<T>
    {
        T Visit(CompilationUnit node);

        T Visit(IfStatement node);

        T Visit(ExpressionStatement node);

        T Visit(SimpleName node);

        T Visit(LiteralExpression node);

        T Visit(MethodInvocation node);

        T Visit(MemberAccess node);

        T Visit(ElementAccess node);

        T Visit(UnaryExpression node);

        T Visit(BinaryExpression node);
    }
}
