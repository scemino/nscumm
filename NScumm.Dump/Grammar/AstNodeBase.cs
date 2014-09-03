using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NScumm.Tmp
{
	public abstract class AstNodeBase: IAstNode
	{
		public IAstNode Parent {
			get;
			internal set;
		}

		public IEnumerable<IAstNode> Children {
			get{ return ChildrenCore; }
		}

		protected AstNodes ChildrenCore {
			get;
			private set;
		}

		protected AstNodeBase ()
		{
			this.ChildrenCore = new AstNodes (this);
		}

		#region IAstNode implementation

		public abstract void Accept (IAstNodeVisitor visitor);

		public abstract T Accept<T> (IAstNodeVisitor<T> visitor);

		#endregion

		protected sealed class AstNodes: Collection<IAstNode>
		{
			public IAstNode Parent {
				get;
				private set;
			}

			public AstNodes (IAstNode parent)
			{
				Parent = parent;
			}

			protected override void InsertItem (int index, IAstNode item)
			{
				base.InsertItem (index, item);
				((AstNodeBase)item).Parent = this.Parent;
			}

			protected override void SetItem (int index, IAstNode item)
			{
				base.SetItem (index, item);
				((AstNodeBase)item).Parent = this.Parent;
			}
		}
	}
}
