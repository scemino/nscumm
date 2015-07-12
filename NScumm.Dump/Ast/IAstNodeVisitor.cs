namespace NScumm.Dump
{
    public interface IAstNodeVisitor
    {
        void Visit(CompilationUnit node);

        void Visit(BlockStatement node);

        void Visit(ExpressionStatement node);

        void Visit(JumpStatement node);

        void Visit(IfStatement node);

        void Visit(CaseStatement node);

        void Visit(SwitchStatement node);

        void Visit(GoToStatement node);

        void Visit(LabelStatement node);

        void Visit(DoWhileStatement node);

        void Visit(SimpleName node);

        void Visit(IntegerLiteralExpression node);

        void Visit(ArrayLiteralExpression node);

        void Visit(StringLiteralExpression node);

        void Visit(BooleanLiteralExpression node);

        void Visit(MethodInvocation node);

        void Visit(MemberAccess node);

        void Visit(ElementAccess node);

        void Visit(UnaryExpression node);

        void Visit(BinaryExpression node);

        void Visit(EnumExpression enumExpression);
    }

    public interface IAstNodeVisitor<out T>
    {
        T Visit(CompilationUnit node);

        T Visit(BlockStatement node);

        T Visit(JumpStatement node);

        T Visit(IfStatement node);

        T Visit(CaseStatement node);

        T Visit(SwitchStatement node);

        T Visit(GoToStatement node);

        T Visit(LabelStatement node);

        T Visit(DoWhileStatement node);

        T Visit(ExpressionStatement node);

        T Visit(SimpleName node);

        T Visit(IntegerLiteralExpression node);

        T Visit(ArrayLiteralExpression node);

        T Visit(StringLiteralExpression node);

        T Visit(BooleanLiteralExpression node);

        T Visit(EnumExpression enumExpression);

        T Visit(MethodInvocation node);

        T Visit(MemberAccess node);

        T Visit(ElementAccess node);

        T Visit(UnaryExpression node);

        T Visit(BinaryExpression node);
    }
}
