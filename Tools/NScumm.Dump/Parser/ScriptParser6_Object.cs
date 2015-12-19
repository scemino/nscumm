//
//  ScriptParser6_Object.cs
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
        readonly SimpleName Objects = new SimpleName("Objects");

        protected Statement PickupObject()
        {
            Expression room, obj;
            PopRoomAndObject(out room, out obj);
            return new MethodInvocation("PickupObject").AddArguments(room, obj).ToStatement();
        }

        protected Statement PickOneOf()
        {
            var args = GetStackList(100);
            var i = Pop();
            return new MethodInvocation("PickOneOf").AddArguments(i, args).ToStatement();
        }

        protected Statement PickOneOfDefault()
        {
            var def = Pop();
            var args = GetStackList(100);
            var i = Pop();
            return new MethodInvocation("PickOneOfDefault").AddArguments(i, args, def).ToStatement();
        }

        protected Expression Object(Expression index)
        {
            return new ElementAccess(Objects, index);
        }

        protected Statement GetObjectX()
        {
            return Push(new MemberAccess(Object(Pop()), "X"));
        }

        protected Statement GetObjectY()
        {
            return Push(new MemberAccess(Object(Pop()), "Y"));
        }

        protected Statement GetObjectOldDir()
        {
            return Push(new MemberAccess(Object(Pop()), "OldDir"));
        }

        protected Statement GetObjectNewDir()
        {
            return Push(new MemberAccess(Object(Pop()), "NewDir"));
        }

        protected Statement SetObjectName()
        {
            return new BinaryExpression(Object(Pop()), Operator.Assignment, ReadCharacters()).ToStatement();
        }

        protected Statement SetState()
        {
            var state = Pop();
            return new BinaryExpression(new MemberAccess(Object(Pop()), "State"), Operator.Assignment, state).ToStatement();
        }

        protected Statement GetState()
        {
            return Push(new MemberAccess(Object(Pop()), "State"));
        }

        protected Statement SetOwner()
        {
            var owner = Pop();
            return new BinaryExpression(new MemberAccess(Object(Pop()), "Owner"), Operator.Assignment, owner).ToStatement();
        }

        protected Statement GetOwner()
        {
            return Push(new MemberAccess(Object(Pop()), "Owner"));
        }

        protected Statement SetClass()
        {
            var list = GetStackList(16);
            var obj = Pop();
            return new MethodInvocation("SetClass").AddArguments(list, obj).ToStatement();
        }

        protected Statement DistObjectObject()
        {
            var b = Pop();
            var a = Pop();
            return Push(new MethodInvocation("GetDistanceBetween").AddArguments(a, b));
        }

        protected Statement DistObjectPt()
        {
            return Push(new MethodInvocation("GetDistanceBetween").AddArguments(Pop(), Pop(), Pop()));
        }

        protected Statement DistPtPt()
        {
            return Push(new MethodInvocation("GetDistanceBetween").AddArguments(Pop(), Pop(), Pop(), Pop()));
        }

        protected Statement FindObject()
        {
            var y = Pop();
            var x = Pop();
            return Push(new MethodInvocation("FindObject").AddArguments(x, y));
        }

        protected Statement StampObject()
        {
            var state = Pop();
            var y = Pop();
            var x = Pop();
            var obj = Pop();
            return new MethodInvocation("StampObject").AddArguments(obj, x, y, state).ToStatement();
        }

        protected Statement FindAllObjects()
        {
            var room = Pop();
            return Push(new MethodInvocation("FindAllObjects").AddArgument(room));
        }
    }
}

