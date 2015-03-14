//
//  ScriptParser6_Audio.cs
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
        protected Statement SoundKludge()
        {
            var args = GetStackList(16);
            return new MethodInvocation("SoundKludge").AddArgument(args).ToStatement();
        }

        protected Statement StartMusic()
        {
            return new MethodInvocation("StartMusic").AddArgument(Pop()).ToStatement();
        }

        protected Statement StopMusic()
        {
            return new MethodInvocation("StopMusic").ToStatement();
        }

        protected Statement StartSound()
        {
            return new MethodInvocation("StartSound").AddArgument(Pop()).ToStatement();
        }

        protected Statement StopSound()
        {
            return new MethodInvocation("StopSound").AddArgument(Pop()).ToStatement();
        }

        protected Statement IsSoundRunning()
        {
            return Push(new MethodInvocation("IsSoundRunning").AddArgument(Pop()));
        }
    }
}

