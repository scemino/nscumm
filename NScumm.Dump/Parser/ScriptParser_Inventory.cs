using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        IEnumerable<Statement> FindInventory()
        {
            var index = GetResultIndexExpression();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1);
            var y = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return SetResultExpression(index, new MethodInvocation("FindInventory").AddArguments(x, y)).ToStatement();
        }

        IEnumerable<Statement> GetInventoryCount()
        {
            var index = GetResultIndexExpression();
            yield return SetResultExpression(index, new MethodInvocation("GetInventoryCount").AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1))).ToStatement();
        }
    }
}

