using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Dump
{
	partial class ScriptParser
	{
		IEnumerable<Statement> PanCameraTo ()
		{
			var to = GetVarOrDirectWord (OpCodeParameter.Param1);
			yield return new MethodInvocation ("PanCameraTo").AddArgument (to).ToStatement ();
		}

		IEnumerable<Statement> SetCameraAt ()
		{
			var at = GetVarOrDirectWord (OpCodeParameter.Param1);
			yield return new MethodInvocation ("SetCameraAt").AddArgument (at).ToStatement ();
		}
	}
}

