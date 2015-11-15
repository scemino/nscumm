//
//  ScriptParser_Audio.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser3
    {
        Statement StartSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StartSound").AddArgument(sound).ToStatement();
        }

        Statement StopSound()
        {
            var sound = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StopSound").AddArgument(sound).ToStatement();
        }

        Statement StartMusic()
        {
            var arg = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StartMusic").AddArgument(arg).ToStatement();
        }

        Statement StopMusic()
        {
            return new MethodInvocation("StopMusic").ToStatement();
        }

        Statement IsSoundRunning()
        {
            var index = GetResultIndexExpression();
            var snd = GetVarOrDirectByte(OpCodeParameter.Param1);
            return SetResultExpression(index, new MethodInvocation("IsSoundRunning").AddArgument(snd)).ToStatement();
        }
    }
}

