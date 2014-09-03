using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
{
	partial class ScriptParser
	{
		IEnumerable<Statement> StartSound ()
		{
			var sound = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return new MethodInvocation ("StartSound").AddArgument (sound).ToStatement ();
		}

		IEnumerable<Statement> StopSound ()
		{
			var sound = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return new MethodInvocation ("StopSound").AddArgument (sound).ToStatement ();
		}

		IEnumerable<Statement> StartMusic ()
		{
			var arg = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return new MethodInvocation ("StartMusic").AddArgument (arg).ToStatement ();
		}

		IEnumerable<Statement> StopMusic ()
		{
			yield return new MethodInvocation ("StopMusic").ToStatement ();
		}

		IEnumerable<Statement> IsSoundRunning ()
		{
			var index = GetResultIndexExpression ();
			var snd = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return SetResultExpression (index, new MethodInvocation ("IsSoundRunning").AddArgument (snd));
		}
	}
}

