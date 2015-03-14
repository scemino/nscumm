using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NScumm.Dump
{
    public abstract class AstNodeBase: IAstNode
    {
        public IAstNode Parent
        {
            get;
            internal set;
        }

        public IList<IAstNode> Children
        {
            get{ return ChildrenCore; }
        }

        protected AstNodes ChildrenCore
        {
            get;
            private set;
        }

        protected AstNodeBase()
        {
            ChildrenCore = new AstNodes(this);
        }

        public long? StartOffset { get; set; }

        public long? EndOffset { get; set; }

        public bool Contains(long offset)
        {
            return StartOffset.HasValue && EndOffset.HasValue && StartOffset.Value <= offset && offset < EndOffset.Value;
        }

        #region IAstNode implementation

        public abstract void Accept(IAstNodeVisitor visitor);

        public abstract T Accept<T>(IAstNodeVisitor<T> visitor);

        #endregion

        protected sealed class AstNodes: Collection<IAstNode>
        {
            public IAstNode Parent
            {
                get;
                private set;
            }

            public AstNodes(IAstNode parent)
            {
                Parent = parent;
            }

            protected override void InsertItem(int index, IAstNode item)
            {
                base.InsertItem(index, item);
                ((AstNodeBase)item).Parent = Parent;
                AdjustOffsets(item);
            }

            protected override void SetItem(int index, IAstNode item)
            {
                base.SetItem(index, item);
                ((AstNodeBase)item).Parent = Parent;
                AdjustOffsets(item);
            }

            public void AddRange(IEnumerable<IAstNode> nodes)
            {
                foreach (var node in nodes)
                {
                    Add(node);
                }
            }

            void AdjustOffsets(IAstNode node)
            {
                long? start = node.StartOffset;
                long? end = node.EndOffset;

                if (start.HasValue)
                {
                    var index = IndexOf(node);
                    if (index > 0)
                    {
                        var previousNode = this[index - 1];
                        if (previousNode.EndOffset != start.Value)
                        {
                            ((AstNodeBase)node).StartOffset = previousNode.EndOffset;
                        }
                    }

                    if (!Parent.StartOffset.HasValue || Parent.StartOffset.Value > start)
                    {
                        ((AstNodeBase)Parent).StartOffset = start;
                    }
                    if (!Parent.EndOffset.HasValue || Parent.EndOffset.Value < end)
                    {
                        ((AstNodeBase)Parent).EndOffset = end;
                    }
                }
            }
        }
    }
}
