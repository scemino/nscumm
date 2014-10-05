namespace NScumm.Dump
{
    public interface IAstNodeVisitor
    {
        void Visit(CompilationUnit node);

        void Visit(BlockStatement node);

        void Visit(ExpressionStatement node);

        void Visit(JumpStatement node);

        void Visit(IfStatement node);

        void Visit(SimpleName node);

        void Visit(IntegerLiteralExpression node);

        void Visit(StringLiteralExpression node);

        void Visit(BooleanLiteralExpression node);

        void Visit(MethodInvocation node);

        void Visit(MemberAccess node);

        void Visit(ElementAccess node);

        void Visit(UnaryExpression node);

        void Visit(BinaryExpression node);
    }

    public interface IAstNodeVisitor<T>
    {
        T Visit(CompilationUnit node);

        T Visit(BlockStatement node);

        T Visit(JumpStatement node);

        T Visit(IfStatement node);

        T Visit(ExpressionStatement node);

        T Visit(SimpleName node);

        T Visit(IntegerLiteralExpression node);

        T Visit(StringLiteralExpression node);

        T Visit(BooleanLiteralExpression node);

        T Visit(MethodInvocation node);

        T Visit(MemberAccess node);

        T Visit(ElementAccess node);

        T Visit(UnaryExpression node);

        T Visit(BinaryExpression node);
    }
}
