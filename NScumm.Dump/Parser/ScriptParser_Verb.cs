using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        IEnumerable<Statement> SaveRestoreVerbs()
        {
            _opCode = ReadByte();

            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
            var c = GetVarOrDirectByte(OpCodeParameter.Param3);

            switch (_opCode)
            {
                case 1:
                    yield return new MethodInvocation("SaveVerbs").AddArguments(a, b, c).ToStatement();
                    break;
                case 2:
                    yield return new MethodInvocation("LoadVerbs").AddArguments(a, b, c).ToStatement();
                    break;
                case 3:
                    yield return new MethodInvocation("DeleteVerbs").AddArguments(a, b, c).ToStatement();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        IEnumerable<Statement> GetVerbEntrypoint()
        {
            var index = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectWord(OpCodeParameter.Param2);

            yield return SetResultExpression(index, new MethodInvocation("GetVerbEntrypoint").AddArguments(a, b)).ToStatement();
        }

        IEnumerable<Statement> VerbOps()
        {
            Expression verb = new ElementAccess("Verbs", GetVarOrDirectByte(OpCodeParameter.Param1));

            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:     // SO_VERB_IMAGE
                        {
                            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            verb = new MethodInvocation(new MemberAccess(verb, "SetImage")).AddArgument(a);
                        }
                        break;

                    case 2:     // SO_VERB_NAME
                        var text = ReadCharacters();
                        verb = new MethodInvocation(new MemberAccess(verb, "SetName")).AddArgument(text);
                        break;

                    case 3:     // SO_VERB_COLOR
                        {
                            var color = GetVarOrDirectByte(OpCodeParameter.Param1);
                            verb = new MethodInvocation(new MemberAccess(verb, "SetColor")).AddArgument(color);
                        }
                        break;

                    case 4:     // SO_VERB_HICOLOR
                        var hiColor = GetVarOrDirectByte(OpCodeParameter.Param1);
                        verb = new MethodInvocation(new MemberAccess(verb, "SetHiColor")).AddArgument(hiColor);
                        break;

                    case 5:     // SO_VERB_AT
                        var left = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var top = GetVarOrDirectWord(OpCodeParameter.Param2);
                        verb = new MethodInvocation(new MemberAccess(verb, "At")).AddArguments(left, top);
                        break;

                    case 6:
					// SO_VERB_ON
                        verb = new MethodInvocation(new MemberAccess(verb, "On"));
                        break;

                    case 7:
					// SO_VERB_OFF
                        verb = new MethodInvocation(new MemberAccess(verb, "Off"));
                        break;

                    case 8:     // SO_VERB_DELETE
                        verb = new MethodInvocation(new MemberAccess(verb, "Delete"));
                        break;

                    case 9:
                        {
                            // SO_VERB_NEW
                            verb = new MethodInvocation(new MemberAccess(verb, "New"));
                            break;
                        }
                    case 16:    // SO_VERB_DIMCOLOR
                        {
                            var color = GetVarOrDirectByte(OpCodeParameter.Param1);
                            verb = new MethodInvocation(new MemberAccess(verb, "SetDimColor")).AddArgument(color);
                        }
                        break;

                    case 17:    // SO_VERB_DIM
                        verb = new MethodInvocation(new MemberAccess(verb, "Dim"));
                        break;

                    case 18:    // SO_VERB_KEY
                        var key = GetVarOrDirectByte(OpCodeParameter.Param1);
                        verb = new MethodInvocation(new MemberAccess(verb, "SetKey")).AddArgument(key);
                        break;

                    case 19:    // SO_VERB_CENTER
                        verb = new MethodInvocation(new MemberAccess(verb, "Center"));
                        break;

                    case 20:    // SO_VERB_NAME_STR
                        var index = GetVarOrDirectWord(OpCodeParameter.Param1);
                        verb = new MethodInvocation(new MemberAccess(verb, "SetText")).AddArgument(new ElementAccess("Strings", index));
                        break;
                    case 22:    // assign object
                        {
                            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            verb = new MethodInvocation(new MemberAccess(verb, "SetVerbObject")).AddArguments(a, b);
                        }
                        break;
                    case 23:    // Set Back Color
                        {
                            var color = GetVarOrDirectByte(OpCodeParameter.Param1);
                            verb = new MethodInvocation(new MemberAccess(verb, "SetBackColor")).AddArgument(color);
                        }
                        break;
                    default:
                        throw new NotImplementedException(string.Format("VerbOps #{0} is not yet implemented.", _opCode & 0x1F));
                }
            }
            yield return verb.ToStatement();
        }
    }
}

