using System;
using System.Collections.Generic;

namespace NScumm.Dump
{
	public class CompilationUnit: AstNodeBase
	{
		private List<Statement> statementsCore = new List<Statement> ();

		public IEnumerable<Statement> Statements { get { return statementsCore; } }

		public CompilationUnit AddStatement (Statement statement)
		{
			statementsCore.Add (statement);
			return this;
		}

		public CompilationUnit AddStatements (IEnumerable<Statement> statements)
		{
			statementsCore.AddRange (statements);
			return this;
		}

		public CompilationUnit AddStatements (params Statement[] statements)
		{
			statementsCore.AddRange (statements);
			return this;
		}

		#region implemented abstract members of AstNodeBase

		public override void Accept (IAstNodeVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override T Accept<T> (IAstNodeVisitor<T> visitor)
		{
			return visitor.Visit (this);
		}

		#endregion

	}
}

