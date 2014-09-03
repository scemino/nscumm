using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
{
	partial class ScriptParser
	{
		IEnumerable<Statement> StartScript ()
		{
			var op = _opCode;
			var script = GetVarOrDirectByte (OpCodeParameter.Param1);
			var args = GetWordVarArgs ();
			yield return new MethodInvocation ("RunScript").
				AddArguments (
					script,
					new LiteralExpression ((op & 0x20) != 0),
					new LiteralExpression ((op & 0x40) != 0)).
				AddArguments (args).ToStatement ();
		}

		IEnumerable<Statement> StopScript ()
		{
			var script = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return new MemberAccess (
				new ElementAccess ("Scripts", script),
				new MethodInvocation ("Stop")).ToStatement ();
		}

		IEnumerable<Statement> StopObjectScript ()
		{
			var obj = GetVarOrDirectWord (OpCodeParameter.Param1);
			yield return new MethodInvocation ("StopObjectScript").AddArgument (obj).ToStatement ();
		}

		IEnumerable<Statement> FreezeScripts ()
		{
			var scr = GetVarOrDirectByte (OpCodeParameter.Param1);
			yield return new MethodInvocation ("FreezeScripts").AddArgument (scr).ToStatement ();
		}

		IEnumerable<Statement> ChainScript ()
		{
			var script = GetVarOrDirectByte (OpCodeParameter.Param1);
			var args = GetWordVarArgs ();
			yield return new MethodInvocation ("ChainScript").AddArgument (script).AddArguments (args).ToStatement ();
		}

		IEnumerable<Statement> CutScene ()
		{
			var args = GetWordVarArgs ();
			yield return new MethodInvocation ("CutScene").AddArguments (args).ToStatement ();
		}

		IEnumerable<Statement> BeginOverride ()
		{
			if (ReadByte () != 0)
				yield return new MethodInvocation ("BeginOverride").ToStatement ();
			else
				yield return new MethodInvocation ("EndOverride").ToStatement ();
		}

		IEnumerable<Statement> EndCutscene ()
		{
			yield return new MethodInvocation ("EndCutScene").ToStatement ();
		}

		IEnumerable<Statement> IsScriptRunning ()
		{
			var indexExp = GetResultIndexExpression ();
			yield return SetResultExpression (indexExp, new MethodInvocation ("IsScriptRunning").
				AddArgument (GetVarOrDirectByte (OpCodeParameter.Param1)));
		}

		IEnumerable<Statement> BreakHere ()
		{
			yield return new MethodInvocation ("BreakHere").ToStatement ();
		}
	}
}

