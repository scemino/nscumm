//
//  ScummEngine0_Actor.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using System.Diagnostics;
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine0
    {
        protected override bool IsActor(int id)
        {
            // object IDs < _numActors are used in v0 for objects too (e.g. hamster)
            return OBJECT_V0_TYPE(id) == ObjectV0Type.Actor;
        }

        protected override int ObjToActor(int id)
        {
            return OBJECT_V0_ID(id);
        }

        void SwitchActor(int slot)
        {
            ResetSentence();

            // actor switching only allowed during normal gamplay (not cutscene, ...)
            if (_currentMode != Engine0Mode.Normal)
                return;

            Variables[VariableEgo.Value] = Variables[97 + slot];
            ActorFollowCamera(Variables[VariableEgo.Value]);
        }

        void SetActorBitVar()
        {
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = (ActorV0MiscFlags)GetVarOrDirectByte(OpCodeParameter.Param2);
            var mod = GetVarOrDirectByte(OpCodeParameter.Param3);

            // 0x63ED
            if (act >= Actors.Length)
                return;

            var a = (Actor0)Actors[act];

            if (mod != 0)
                a.MiscFlags |= mask;
            else
                a.MiscFlags &= ~mask;

            // This flag causes the actor to stop moving (used by script #158, Green Tentacle 'Oomph!')
            if (a.MiscFlags.HasFlag(ActorV0MiscFlags.Freeze))
                a.StopActorMoving();

            Debug.WriteLine("SetActorBitVar({0}, {1}, {2})", act, mask, mod);
        }

        void WalkActorToObject()
        {
            int actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            int objId = ReadByte();
            int obj;

            if ((_opCode & 0x40) != 0)
                obj = OBJECT_V0(objId, ObjectV0Type.Background);
            else
                obj = OBJECT_V0(objId, ObjectV0Type.Foreground);

            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                WalkActorToObject(actor, obj);
            }
        }

        void PutActorAtObject()
        {
            int obj;
            Point p;
            var a = Actors[GetVarOrDirectByte(OpCodeParameter.Param1)];

            int objId = ReadByte();
            if ((_opCode & 0x40) != 0)
                obj = OBJECT_V0(objId, ObjectV0Type.Background);
            else
                obj = OBJECT_V0(objId, ObjectV0Type.Foreground);

            if (GetWhereIsObject(obj) != WhereIsObject.NotFound)
            {
                p = GetObjectXYPos(obj);
                AdjustBoxResult r = a.AdjustXYToBeInBox(p);
                p = r.Position;
            }
            else
            {
                p = new Point(30, 60);
            }

            a.PutActor(p);
        }

        void GetActorBitVar()
        {
            GetResult();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);

            var a = (Actor0)Actors[act];
            SetResult(((((int)a.MiscFlags) & mask) != 0) ? 1 : 0);

            Debug.WriteLine("getActorBitVar({0}, {1}, {2})", act, mask, (((int)a.MiscFlags) & mask));
        }

        void WalkToActorOrObject(int obj)
        {
            int dir;
            Point p;
            var a = (Actor0)Actors[Variables[VariableEgo.Value]];

            _walkToObject = obj;
            _walkToObjectState = WalkToObjectState.Walk;

            if (OBJECT_V0_TYPE(obj) == ObjectV0Type.Actor)
            {
                WalkActorToActor(Variables[VariableEgo.Value], OBJECT_V0_ID(obj), 4);
                p = a.RealPosition;
            }
            else
            {
                WalkActorToObject(Variables[VariableEgo.Value], obj);
                GetObjectXYPos(obj, out p, out dir);
            }

            Variables[6] = p.X;
            Variables[7] = p.Y;

            // actor must not move if frozen
            if (a.MiscFlags.HasFlag(ActorV0MiscFlags.Freeze))
            {
                a.StopActorMoving();
                a.NewWalkBoxEntered = false;
            }
        }

        void GetClosestActor()
        {
            int act, check_act;
            int dist;

            // This code can't detect any actors farther away than 255 units
            // (pixels in newer games, characters in older ones.) But this is
            // perfectly OK, as it is exactly how the original behaved.

            int closest_act = 0xFF, closest_dist = 0xFF;

            GetResult();

            act = GetVarOrDirectByte(OpCodeParameter.Param1);
            check_act = ((_opCode & (byte)OpCodeParameter.Param2) != 0) ? 25 : 7;

            do
            {
                dist = GetObjActToObjActDist(ActorToObj(act), ActorToObj(check_act));
                if (dist < closest_dist)
                {
                    closest_dist = dist;
                    closest_act = check_act;
                }
            } while ((--check_act) != 0);

            SetResult(closest_act);
        }

        protected override int ActorToObj(int actor)
        {
            return OBJECT_V0(actor, ObjectV0Type.Actor);
        }
    }
}

