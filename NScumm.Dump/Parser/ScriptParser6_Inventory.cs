//
//  ScriptParser6_Inventory.cs
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
        protected Statement FindInventory()
        {
            var index = Pop();
            var owner = Pop();
            return Push(new MethodInvocation("FindInventory").AddArguments(owner, index));
        }

        protected Statement GetInventoryCount()
        {
            return Push(new MethodInvocation("GetInventoryCount").AddArgument(Pop()));
        }
    }
}

