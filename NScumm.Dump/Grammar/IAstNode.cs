using System.Collections.Generic;

namespace NScumm.Dump
{
	public interface IAstNode
	{
		void Accept (IAstNodeVisitor visitor);

		T Accept<T> (IAstNodeVisitor<T> visitor);

		IAstNode Parent { get; }

		IEnumerable<IAstNode> Children { get; }
	}

	public static class IAstNodeExtension
	{
		public static IEnumerable<IAstNode> GetAncestors (this IAstNode node)
		{
			var currentNode = node;
			while (currentNode.Parent != null) {
				yield return currentNode.Parent;
				currentNode = currentNode.Parent;
			}
		}
	}
}
