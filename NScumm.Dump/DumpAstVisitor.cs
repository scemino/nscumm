using System;
using System.Linq;
using System.Text;

namespace NScumm.Dump
{
    public class DumpAstVisitor: DefaultVisitor<string>
    {
        public override string Visit(CompilationUnit node)
        {
            var text = new StringBuilder();
            foreach (var statement in node.Statements)
            {
                text.Append(statement.Accept(this));
            }
            return text.ToString();
        }

        public override string Visit(ExpressionStatement node)
        {
            return string.Format("[{0},{1}] {2}{3}", node.StartOffset.Value, node.EndOffset.Value, node.Expression.Accept(this), Environment.NewLine);
        }

        public override string Visit(IfStatement node)
        {
            return string.Format("[{0},{1}] jump {2} if {3}{4}", 
                node.StartOffset.Value, node.EndOffset.Value, 
                node.JumpOffset, node.Condition.Accept(this), 
                Environment.NewLine);
        }

        public override string Visit(MethodInvocation node)
        {
            var text = new StringBuilder();
            text.Append(node.Name).Append('(');
            text.Append(string.Join(", ", node.Arguments.Select(arg => arg.Accept(this))));
            text.Append(')');
            return text.ToString();
        }

        public override string Visit(LiteralExpression node)
        {
            var valueText = node.Value as byte[];
            if (valueText != null)
            {
                var text = new StringBuilder();
                var decoder = new TextDecoder(text);
                new NScumm.Core.ScummText(valueText).Decode(decoder);
                return string.Format("\"{0}\"", text);
            }
            return node.Value.ToString();
        }

        public override string Visit(SimpleName node)
        {
            return node.Name;
        }

        public override string Visit(MemberAccess node)
        {
            var exp = node.Field as LiteralExpression;
            var member = exp != null ? Convert.ToString(exp.Value) : node.Field.Accept(this);
            return string.Format("{0}.{1}", node.Target.Accept(this), member);
        }

        public override string Visit(ElementAccess node)
        {
            var indices = string.Join(", ", node.Indices.Select(index => index.Accept(this)));
            return string.Format("{0}[{1}]", node.Target.Accept(this), indices);
        }

        public override string Visit(UnaryExpression node)
        {
            string text;
            switch (node.Operator)
            {
                case Operator.PostDecrement:
                case Operator.PostIncrement:
                    text = node.Expression.Accept(this) + OperatorToText(node.Operator);
                    break;
                default:
                    text = OperatorToText(node.Operator) + node.Expression.Accept(this);
                    break;
            }
            return text;
        }

        public override string Visit(BinaryExpression node)
        {
            return string.Format("{0} {1} {2}", node.Left.Accept(this), OperatorToText(node.Operator), node.Right.Accept(this));
        }

        protected override string DefaultVisit(IAstNode node)
        {
            throw new NotImplementedException();
        }

        private string OperatorToText(Operator op)
        {
            switch (op)
            {
                case Operator.Lower:
                    return "<";
                case Operator.LowerOrEquals:
                    return "<=";
                case Operator.Greater:
                    return ">";
                case Operator.GreaterOrEquals:
                    return ">=";
                case Operator.Equals:
                    return "==";
                case Operator.Inequals:
                    return "!=";
                case Operator.Add:
                    return "+";
                case Operator.Minus:
                    return "-";
                case Operator.Multiply:
                    return "*";
                case Operator.Divide:
                    return "/";
                case Operator.Assignment:
                    return "=";
                case Operator.And:
                    return "&";
                case Operator.Or:
                    return "|";
                case Operator.Not:
                    return "~";
                case Operator.PostDecrement:
                    return "--";
                case Operator.PostIncrement:
                    return "++";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

