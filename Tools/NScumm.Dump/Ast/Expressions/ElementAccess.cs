using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NScumm.Dump
{
    public class ElementAccess: Expression
    {
        private List<Expression> indices;
        private ReadOnlyCollection<Expression> roIndices;

        public Expression Target
        {
            get;
            private set;
        }

        public ReadOnlyCollection<Expression> Indices
        {
            get{ return roIndices; }
        }

        internal override string DebuggerDisplay
        {
            get
            { 
                var args = string.Join(", ", Indices.Select(arg => arg.DebuggerDisplay));
                return string.Format("{0}[{1}]", Target.DebuggerDisplay, args);
            }
        }

        public ElementAccess(string target, Expression index)
            : this(new SimpleName(target), index)
        {
        }

        public ElementAccess(string target, object index)
            : this(new SimpleName(target), index.ToLiteral())
        {
        }

        public ElementAccess(Expression target, object index)
            : this(target, index.ToLiteral())
        {
        }

        public ElementAccess(Expression target, Expression index)
            : this(target, new[]{ index })
        {
        }

        public ElementAccess(Expression target, IEnumerable<Expression> indices)
        {
            Target = target;
            this.indices = new List<Expression>(indices);
            roIndices = new ReadOnlyCollection<Expression>(this.indices);
            ChildrenCore.Add(Target);
            ChildrenCore.AddRange(this.indices);
        }

        public ElementAccess AddIndex(Expression index)
        {
            indices.Add(index);
            ChildrenCore.Add(index);
            return this;
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

    }
}

