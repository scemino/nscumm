using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
{
	partial class ScriptParser
	{
		IEnumerable<Statement> SaveRestoreVerbs ()
		{
			_opCode = ReadByte ();

			var a = GetVarOrDirectByte (OpCodeParameter.Param1);
			var b = GetVarOrDirectByte (OpCodeParameter.Param2);
			var c = GetVarOrDirectByte (OpCodeParameter.Param3);

			switch (_opCode) {
			case 1:
				yield return new MethodInvocation ("SaveVerbs").AddArguments (a, b, c).ToStatement ();
				break;
			case 2:
				yield return new MethodInvocation ("LoadVerbs").AddArguments (a, b, c).ToStatement ();
				break;
			case 3:
				yield return new MethodInvocation ("DeleteVerbs").AddArguments (a, b, c).ToStatement ();
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		IEnumerable<Statement> GetVerbEntrypoint ()
		{
			var index = GetResultIndexExpression ();
			var a = GetVarOrDirectWord (OpCodeParameter.Param1);
			var b = GetVarOrDirectWord (OpCodeParameter.Param2);

			yield return SetResultExpression (index, new MethodInvocation ("GetVerbEntrypoint").AddArguments (a, b));
		}

		IEnumerable<Statement> VerbOps ()
		{
			var verb = new ElementAccess ("Verbs", GetVarOrDirectByte (OpCodeParameter.Param1));

			while ((_opCode = ReadByte ()) != 0xFF) {
				switch (_opCode & 0x1F) {
				case 1:     // SO_VERB_IMAGE
					var a = GetVarOrDirectWord (OpCodeParameter.Param1);
					yield return new BinaryExpression (
						new MemberAccess (verb, "Image"),
						Operator.Assignment,
						a).ToStatement ();
					break;

				case 2:     // SO_VERB_NAME
					var text = ReadCharacters ();
					yield return new BinaryExpression (
						new MemberAccess (verb, "Name"),
						Operator.Assignment,
						text).ToStatement ();
					break;

				case 3:     // SO_VERB_COLOR
					var color = GetVarOrDirectByte (OpCodeParameter.Param1);
					yield return new BinaryExpression (
						new MemberAccess (verb, "Color"),
						Operator.Assignment,
						color).ToStatement ();
					break;

				case 4:     // SO_VERB_HICOLOR
					var hiColor = GetVarOrDirectByte (OpCodeParameter.Param1);
					yield return new BinaryExpression (
						new MemberAccess (verb, "HiColor"),
						Operator.Assignment,
						hiColor).ToStatement ();
					break;

				case 5:     // SO_VERB_AT
					var left = GetVarOrDirectWord (OpCodeParameter.Param1);
					var top = GetVarOrDirectWord (OpCodeParameter.Param2);
					yield return new MemberAccess (
						verb,
						new MethodInvocation ("At").
						AddArguments (left, top)).ToStatement ();
					break;

				case 6:
					// SO_VERB_ON
					yield return new BinaryExpression (
						new MemberAccess (verb, "CurMode"),
						Operator.Assignment,
						1.ToLiteral ()).ToStatement ();
					break;

				case 7:
					// SO_VERB_OFF
					yield return new BinaryExpression (
						new MemberAccess (verb, "CurMode"),
						Operator.Assignment,
						0.ToLiteral ()).ToStatement ();
					break;

				case 8:     // SO_VERB_DELETE
					yield return new MemberAccess (
						verb,
						new MethodInvocation ("Delete")).ToStatement ();
					break;

				case 9:
					{
						// SO_VERB_NEW
						yield return new BinaryExpression (
							verb,
							Operator.Assignment,
							new MethodInvocation ("CreateVerb")).ToStatement ();
						break;
					}
				case 16:    // SO_VERB_DIMCOLOR
					yield return new BinaryExpression (
						new MemberAccess (verb, "DimColor"),
						Operator.Assignment,
						GetVarOrDirectByte (OpCodeParameter.Param1)).ToStatement ();
					break;

				case 17:    // SO_VERB_DIM
					yield return new BinaryExpression (
						new MemberAccess (verb, "CurMode"),
						Operator.Assignment,
						2.ToLiteral ()).ToStatement ();
					break;

				case 18:    // SO_VERB_KEY
					var key = GetVarOrDirectByte (OpCodeParameter.Param1);
					yield return new BinaryExpression (
						new MemberAccess (verb, "Key"),
						Operator.Assignment,
						key).ToStatement ();
					break;

				case 19:    // SO_VERB_CENTER
					yield return new BinaryExpression (
						new MemberAccess (verb, "Center"),
						Operator.Assignment,
						true.ToLiteral ()).ToStatement ();
					break;

				case 20:    // SO_VERB_NAME_STR
					var index = GetVarOrDirectWord (OpCodeParameter.Param1);
					yield return new BinaryExpression (
						new MemberAccess (verb, "Text"),
						Operator.Assignment,
						new ElementAccess ("Strings", index)).ToStatement ();
					break;

				default:
					throw new NotImplementedException (string.Format ("VerbOps #{0} is not yet implemented.", _opCode));
				}
			}
		}


	}
}

