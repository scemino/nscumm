//
//  DumpAstVisitor.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Text;

namespace NScumm.Dump
{
    public class DumpAstVisitor: DefaultVisitor<string>
    {
        #region Indentation

        int indentAmount;

        string Indentation(Statement statement, string text)
        {
            var newText = new StringBuilder();
            if (ShowOffsets)
            {
                newText.AppendFormat("[{0,4},{1,4}]", statement.StartOffset, statement.EndOffset);
            }
            newText.Append(new string(' ', indentAmount * 2)).Append(text);
            return newText.ToString();
        }

        void Indent()
        {
            indentAmount++;
        }

        void Deindent()
        {
            if (indentAmount == 0)
                throw new InvalidOperationException("IndentAmount should be positive");
            indentAmount--;
        }

        IDisposable IndentBlock()
        {
            return new Indenter(this);
        }

        class Indenter: IDisposable
        {
            DumpAstVisitor Visitor{ get; set; }

            public Indenter(DumpAstVisitor visitor)
            {
                Visitor = visitor;
                Visitor.Indent();
            }

            public void Dispose()
            {
                Visitor.Deindent();
            }
        }

        #endregion

        public bool ShowOffsets{ get; set; }

        public DumpAstVisitor(bool showOffsets = true)
        {
            ShowOffsets = showOffsets;
        }

        public override string Visit(CompilationUnit node)
        {
            return node.Statement.Accept(this);
        }

        public override string Visit(ExpressionStatement node)
        {
            return Indentation(node, string.Format("{0}{1}", node.Expression.Accept(this), Environment.NewLine));
        }

        public override string Visit(LabelStatement node)
        {
            return string.Format("{0}:{1}", node.Label, Environment.NewLine);
        }

        public override string Visit(GoToStatement node)
        {
            return Indentation(node, string.Format("goto {0}{1}", node.Label, Environment.NewLine));
        }

        public override string Visit(JumpStatement node)
        {
            return Indentation(node, string.Format("jump {0} if {1}{2}", node.JumpOffset, node.Condition.Accept(this), 
                    Environment.NewLine));
        }

        public override string Visit(BlockStatement node)
        {
            var text = new StringBuilder();
            text.AppendLine(Indentation(node, "{"));
            using (IndentBlock())
            {
                foreach (var statement in node)
                {
                    text.Append(statement.Accept(this));
                }
            }
            text.AppendLine(Indentation(node, "}"));
            return text.ToString();
        }

        public override string Visit(IfStatement node)
        {
            var text = new StringBuilder();
            text.AppendLine(Indentation(node, string.Format("if ({0})", node.Condition.Accept(this))));
            if (node.TrueStatement != null)
            {
                text.Append(node.TrueStatement.Accept(this));
            }
            if (node.FalseStatement != null)
            {
                text.Append(Indentation(node.FalseStatement, "else"));
                text.Append(node.FalseStatement.Accept(this));
            }
            return text.ToString();
        }

        public override string Visit(CaseStatement node)
        {
            var text = new StringBuilder();
            text.AppendLine(Indentation(node, string.Format("case {0}", node.Condition.Accept(this))));
            if (node.TrueStatement != null)
            {
                text.Append(node.TrueStatement.Accept(this));
            }
            return text.ToString();
        }

        public override string Visit(SwitchStatement node)
        {
            var text = new StringBuilder();
            text.AppendLine(Indentation(node, string.Format("switch ({0})", node.Condition.Accept(this))));
            foreach (var caseStatement in node.CaseStatements)
            {
                text.Append(caseStatement.Accept(this));
            }
            return text.ToString();
        }

        public override string Visit(DoWhileStatement node)
        {
            var text = new StringBuilder();
            text.AppendLine(Indentation(node, "do"));
            if (node.Statement != null)
            {
                text.Append(node.Statement.Accept(this));
            }
            text.AppendLine(Indentation(node, string.Format("while ({0})", node.Condition.Accept(this))));
            return text.ToString();
        }

        public override string Visit(MethodInvocation node)
        {
            var text = new StringBuilder();
            text.Append(node.Target.Accept(this)).Append('(');
            text.Append(string.Join(", ", node.Arguments.Select(arg => arg.Accept(this))));
            text.Append(')');
            return text.ToString();
        }

        public override string Visit(StringLiteralExpression node)
        {
            //var text = new StringBuilder();
            //var decoder = new TextDecoder(text);
            //new NScumm.Core.ScummText(node.Value).Decode(decoder);
            var text = System.Text.Encoding.ASCII.GetString(node.Value);
            return string.Format("\"{0}\"", text);
        }

        public override string Visit(BooleanLiteralExpression node)
        {
            return node.Value.ToString();
        }

        public override string Visit(IntegerLiteralExpression node)
        {
            return node.Value.ToString();
        }

        public override string Visit(EnumExpression node)
        {
            return node.Value.ToString();
        }

        public override string Visit(ArrayLiteralExpression node)
        {
            return string.Format("{{{0}}}", string.Join(", ", node.Value.Cast<Expression>().Select(v => v.Accept(this))));
        }

        public override string Visit(SimpleName node)
        {
            return node.Name;
        }

        public override string Visit(MemberAccess node)
        {
            return string.Format("{0}.{1}", node.Target.Accept(this), node.Field);
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

        string OperatorToText(Operator op)
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
                case Operator.AddAssignment:
                    return "+=";
                case Operator.Subtract:
                    return "-";
                case Operator.SubtractionAssignment:
                    return "-=";
                case Operator.Multiply:
                    return "*";
                case Operator.Divide:
                    return "/";
                case Operator.Assignment:
                    return "=";
                case Operator.BitwiseAnd:
                    return "&";
                case Operator.And:
                    return "&&";
                case Operator.Or:
                    return "||";
                case Operator.BitwiseOr:
                    return "|";
                case Operator.Not:
                    return "!";
                case Operator.PostDecrement:
                    return "--";
                case Operator.PostIncrement:
                    return "++";
                case Operator.Modulus:
                    return "%";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

