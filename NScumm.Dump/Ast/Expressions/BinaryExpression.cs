using System;

namespace NScumm.Dump
{
    public enum Operator
    {
        Lower,
        LowerOrEquals,
        Greater,
        GreaterOrEquals,
        Divide,
        Multiply,
        Add,
        AddAssignment,
        Assignment,
        And,
        BitwiseAnd,
        Or,
        BitwiseOr,
        Modulus,
        Not,
        Equals,
        Inequals,
        Subtract,
        PostIncrement,
        PostDecrement,
        SubtractionAssignment
    }

    public class BinaryExpression: Expression
    {
        public Expression Left
        {
            get;
            private set;
        }

        public Operator Operator
        {
            get;
            private set;
        }

        public Expression Right
        {
            get;
            private set;
        }

        internal override string DebuggerDisplay
        {
            get
            { 
                return string.Format("{0} {1} {2}", Left.DebuggerDisplay, Operator, Right.DebuggerDisplay);
            }
        }

        public BinaryExpression(Expression left, Operator op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
            ChildrenCore.Add(Left);
            ChildrenCore.Add(Right);
        }

        #region implemented abstract members of Expression

        public override void Accept(IAstNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAstNodeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        #endregion

        public BinaryExpression Not()
        {
            return new BinaryExpression(Left, Not(Operator), Right);
        }

        static Operator Not(Operator op)
        {
            Operator notOp;
            switch (op)
            {
                case Operator.Equals:
                    notOp = Operator.Inequals;
                    break;
                case Operator.Inequals:
                    notOp = Operator.Equals;
                    break;
                case Operator.LowerOrEquals:
                    notOp = Operator.Greater;
                    break;
                case Operator.Greater:
                    notOp = Operator.LowerOrEquals;
                    break;
                case Operator.GreaterOrEquals:
                    notOp = Operator.Lower;
                    break;
                case Operator.Lower:
                    notOp = Operator.GreaterOrEquals;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Invalid operator {0}", op));
            }
            return notOp;
        }
    }
}

