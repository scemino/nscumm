//
//  ScriptParser6_Camera.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
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

namespace NScumm.Dump
{
    partial class ScriptParser6
    {
        protected Statement PanCameraTo()
        {
            if (Game.Version >= 7)
            {
                var y = Pop();
                var x = Pop();
                return new MethodInvocation("PanCameraTo").AddArguments(x, y).ToStatement();
            }
            else
            {
                return new MethodInvocation("PanCameraTo").AddArguments(Pop(), 0.ToLiteral()).ToStatement();
            }

        }

        protected Statement ActorFollowCamera()
        {
            return new MethodInvocation("ActorFollowCamera").AddArgument(Pop()).ToStatement();
        }

        protected Statement SetCameraAt()
        {
            if (Game.Version >= 7)
            {
                var y = Pop();
                var x = Pop();
                return new MethodInvocation("SetCameraAt").AddArguments(x, y).ToStatement();
            }
            else
            {
                return new MethodInvocation("SetCameraAt").AddArguments(Pop()).ToStatement();
            }
        }
    }
}

