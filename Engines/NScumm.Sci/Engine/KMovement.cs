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


using NScumm.Sci.Graphics;
using System;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kSetJump(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            // Input data
            Register @object = argv[0];
            int dx = argv[1].ToInt16();
            int dy = argv[2].ToInt16();
            int gy = argv[3].ToInt16();

            // Derived data
            int c;
            int tmp;
            int vx = 0;  // x velocity
            int vy = 0;  // y velocity

            bool dxWasNegative = (dx < 0);
            dx = Math.Abs(dx);

            System.Diagnostics.Debug.Assert(gy >= 0);

            if (dx == 0)
            {
                // Upward jump. Value of c doesn't really matter
                c = 1;
            }
            else
            {
                // Compute a suitable value for c respectively tmp.
                // The important thing to consider here is that we want the resulting
                // *discrete* x/y velocities to be not-too-big integers, for a smooth
                // curve (i.e. we could just set vx=dx, vy=dy, and be done, but that
                // is hardly what you would call a parabolic jump, would ya? ;-).
                //
                // So, we make sure that 2.0*tmp will be bigger than dx (that way,
                // we ensure vx will be less than sqrt(gy * dx)).
                if (dx + dy < 0)
                {
                    // dy is negative and |dy| > |dx|
                    c = (2 * Math.Abs(dy)) / dx;
                    //tmp = ABS(dy);  // ALMOST the resulting value, except for obvious rounding issues
                }
                else
                {
                    // dy is either positive, or |dy| <= |dx|
                    c = (dx * 3 / 2 - dy) / dx;

                    // We force c to be strictly positive
                    if (c < 1)
                        c = 1;

                    //tmp = dx * 3 / 2;  // ALMOST the resulting value, except for obvious rounding issues

                    // FIXME: Where is the 3 coming from? Maybe they hard/coded, by "accident", that usually gy=3 ?
                    // Then this choice of scalar will make t equal to roughly sqrt(dx)
                }
            }
            // POST: c >= 1
            tmp = c * dx + dy;
            // POST: (dx != 0)  ==>  ABS(tmp) > ABS(dx)
            // POST: (dx != 0)  ==>  ABS(tmp) ~>=~ ABS(dy)

            DebugC(DebugLevels.Bresen, "c: {0}, tmp: {1}", c, tmp);

            // Compute x step
            if (tmp != 0 && dx != 0)
                vx = (short)((float)(dx * Math.Sqrt(gy / (2.0 * tmp))));
            else
                vx = 0;

            // Restore the left/right direction: dx and vx should have the same sign.
            if (dxWasNegative)
                vx = -vx;

            if ((dy < 0) && (vx == 0))
            {
                // Special case: If this was a jump (almost) straight upward, i.e. dy < 0 (upward),
                // and vx == 0 (i.e. no horizontal movement, at least not after rounding), then we
                // compute vy directly.
                // For this, we drop the assumption on the linear correlation of vx and vy (obviously).

                // FIXME: This choice of vy makes t roughly (2+sqrt(2))/gy * sqrt(dy);
                // so if gy==3, then t is roughly sqrt(dy)...
                vy = (int)Math.Sqrt((float)gy * Math.Abs(2 * dy)) + 1;
            }
            else
            {
                // As stated above, the vertical direction is correlated to the horizontal by the
                // (non-zero) factor c.
                // Strictly speaking, we should probably be using the value of vx *before* rounding
                // it to an integer... Ah well
                vy = c * vx;
            }

            // Always force vy to be upwards
            vy = -Math.Abs(vy);

            DebugC(DebugLevels.Bresen, "SetJump for object at {0}", @object);
            DebugC(DebugLevels.Bresen, "xStep: {0}, yStep: {1}", vx, vy);

            SciEngine.WriteSelectorValue(segMan, @object, o => o.xStep, (ushort)vx);
            SciEngine.WriteSelectorValue(segMan, @object, o => o.yStep, (ushort)vy);

            return s.r_acc;
        }

        private static Register kInitBresen(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            Register mover = argv[0];
            Register client = SciEngine.ReadSelector(segMan, mover, o => o.client);
            short stepFactor = (argc >= 2) ? argv[1].ToInt16() : (short)1;
            short mover_x = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.x);
            short mover_y = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.y);
            short client_xStep = (short)(SciEngine.ReadSelectorValue(segMan, client, o => o.xStep) * stepFactor);
            short client_yStep = (short)(SciEngine.ReadSelectorValue(segMan, client, o => o.yStep) * stepFactor);

            short client_step;
            if (client_xStep < client_yStep)
                client_step = (short)(client_yStep * 2);
            else
                client_step = (short)(client_xStep * 2);

            short deltaX = (short)(mover_x - SciEngine.ReadSelectorValue(segMan, client, o => o.x));
            short deltaY = (short)(mover_y - SciEngine.ReadSelectorValue(segMan, client, o => o.y));
            short mover_dx = 0;
            short mover_dy = 0;
            short mover_i1 = 0;
            short mover_i2 = 0;
            short mover_di = 0;
            short mover_incr = 0;
            short mover_xAxis = 0;

            while (true)
            {
                mover_dx = client_xStep;
                mover_dy = client_yStep;
                mover_incr = 1;

                if (Math.Abs(deltaX) >= Math.Abs(deltaY))
                {
                    mover_xAxis = 1;
                    if (deltaX < 0)
                        mover_dx = (short)-mover_dx;
                    mover_dy = deltaX != 0 ? (short)(mover_dx * deltaY / deltaX) : (short)0;
                    mover_i1 = (short)(((mover_dx * deltaY) - (mover_dy * deltaX)) * 2);
                    if (deltaY < 0)
                    {
                        mover_incr = -1;
                        mover_i1 = (short)-mover_i1;
                    }
                    mover_i2 = (short)(mover_i1 - (deltaX * 2));
                    mover_di = (short)(mover_i1 - deltaX);
                    if (deltaX < 0)
                    {
                        mover_i1 = (short)-mover_i1;
                        mover_i2 = (short)-mover_i2;
                        mover_di = (short)-mover_di;
                    }
                }
                else {
                    mover_xAxis = 0;
                    if (deltaY < 0)
                        mover_dy = (short)-mover_dy;
                    mover_dx = deltaY != 0 ? (short)(mover_dy * deltaX / deltaY) : (short)0;
                    mover_i1 = (short)(((mover_dy * deltaX) - (mover_dx * deltaY)) * 2);
                    if (deltaX < 0)
                    {
                        mover_incr = -1;
                        mover_i1 = (short)-mover_i1;
                    }
                    mover_i2 = (short)(mover_i1 - (deltaY * 2));
                    mover_di = (short)(mover_i1 - deltaY);
                    if (deltaY < 0)
                    {
                        mover_i1 = (short)-mover_i1;
                        mover_i2 = (short)-mover_i2;
                        mover_di = (short)-mover_di;
                    }
                    break;
                }
                if (client_xStep <= client_yStep)
                    break;
                if (client_xStep == 0)
                    break;
                if (client_yStep >= Math.Abs(mover_dy + mover_incr))
                    break;

                client_step--;
                if (client_step == 0)
                    throw new InvalidOperationException("kInitBresen failed");
                client_xStep--;
            }

            // set mover
            SciEngine.WriteSelectorValue(segMan, mover, o => o.dx, (ushort)mover_dx);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.dy, (ushort)mover_dy);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.b_i1, (ushort)mover_i1);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.b_i2, (ushort)mover_i2);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.b_di, (ushort)mover_di);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.b_incr, (ushort)mover_incr);
            SciEngine.WriteSelectorValue(segMan, mover, o => o.b_xAxis, (ushort)mover_xAxis);
            return s.r_acc;
        }

        private static Register kDoBresen(EngineState s, int argc, StackPtr argv)
        {
            SegManager segMan = s._segMan;
            Register mover = argv[0];
            Register client = SciEngine.ReadSelector(segMan, mover, o => o.client);
            bool completed = false;
            bool handleMoveCount = SciEngine.Instance.Features.HandleMoveCount;

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
            {
                var client_signal = (ViewSignals)SciEngine.ReadSelectorValue(segMan, client, o => o.signal);
                SciEngine.WriteSelectorValue(segMan, client, o => o.signal, (ushort)(client_signal & ~ViewSignals.HitObstacle));
            }

            short mover_moveCnt = 1;
            short client_moveSpeed = 0;
            if (handleMoveCount)
            {
                mover_moveCnt = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_movCnt);
                client_moveSpeed = (short)SciEngine.ReadSelectorValue(segMan, client, o => o.moveSpeed);
                mover_moveCnt++;
            }

            if (client_moveSpeed < mover_moveCnt)
            {
                mover_moveCnt = 0;
                short client_x = (short)SciEngine.ReadSelectorValue(segMan, client, o => o.x);
                short client_y = (short)SciEngine.ReadSelectorValue(segMan, client, o => o.y);
                short mover_x = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.x);
                short mover_y = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.y);
                short mover_xAxis = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_xAxis);
                short mover_dx = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.dx);
                short mover_dy = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.dy);
                short mover_incr = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_incr);
                short mover_i1 = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_i1);
                short mover_i2 = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_i2);
                short mover_di = (short)SciEngine.ReadSelectorValue(segMan, mover, o => o.b_di);
                short mover_org_i1 = mover_i1;
                short mover_org_i2 = mover_i2;
                short mover_org_di = mover_di;

                if ((ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY))
                {
                    // save current position into mover
                    SciEngine.WriteSelectorValue(segMan, mover, o => o.xLast, (ushort)client_x);
                    SciEngine.WriteSelectorValue(segMan, mover, o => o.yLast, (ushort)client_y);
                }

                // Store backups of all client selector variables. We will restore them
                // in case of a collision.
                SciObject clientObject = segMan.GetObject(client);
                int clientVarNum = clientObject.VarCount;
                Register[] clientBackup = new Register[clientVarNum];
                for (var i = 0; i < clientVarNum; ++i)
                    clientBackup[i] = clientObject.GetVariable(i);

                if (mover_xAxis != 0)
                {
                    if (Math.Abs(mover_x - client_x) < Math.Abs(mover_dx))
                        completed = true;
                }
                else {
                    if (Math.Abs(mover_y - client_y) < Math.Abs(mover_dy))
                        completed = true;
                }
                if (completed)
                {
                    client_x = mover_x;
                    client_y = mover_y;
                }
                else {
                    client_x += mover_dx;
                    client_y += mover_dy;
                    if (mover_di < 0)
                    {
                        mover_di += mover_i1;
                    }
                    else {
                        mover_di += mover_i2;
                        if (mover_xAxis == 0)
                        {
                            client_x += mover_incr;
                        }
                        else {
                            client_y += mover_incr;
                        }
                    }
                }
                SciEngine.WriteSelectorValue(segMan, client, o => o.x, (ushort)client_x);
                SciEngine.WriteSelectorValue(segMan, client, o => o.y, (ushort)client_y);

                // Now call client::canBeHere/client::cantBehere to check for collisions
                bool collision = false;
                Register cantBeHere = Register.NULL_REG;

                if (SciEngine.Selector(o => o.cantBeHere) != -1)
                {
                    // adding this here for hoyle 3 to get happy. CantBeHere is a dummy in hoyle 3 and acc is != 0 so we would
                    //  get a collision otherwise
                    s.r_acc = Register.NULL_REG;
                    SciEngine.InvokeSelector(s, client, o => o.cantBeHere, argc, argv);
                    if (!s.r_acc.IsNull)
                        collision = true;
                    cantBeHere = s.r_acc;
                }
                else {
                    SciEngine.InvokeSelector(s, client, o => o.canBeHere, argc, argv);
                    if (s.r_acc.IsNull)
                        collision = true;
                }

                if (collision)
                {
                    // We restore the backup of the client variables
                    for (var i = 0; i < clientVarNum; ++i)
                    {
                        var var = clientObject.GetVariableRef(i);
                        var[0] = clientBackup[i];
                    }

                    mover_i1 = mover_org_i1;
                    mover_i2 = mover_org_i2;
                    mover_di = mover_org_di;

                    ViewSignals client_signal = (ViewSignals)SciEngine.ReadSelectorValue(segMan, client, o => o.signal);
                    SciEngine.WriteSelectorValue(segMan, client, o => o.signal, (ushort)(client_signal | ViewSignals.HitObstacle));
                }

                SciEngine.WriteSelectorValue(segMan, mover, o => o.b_i1, (ushort)mover_i1);
                SciEngine.WriteSelectorValue(segMan, mover, o => o.b_i2, (ushort)mover_i2);
                SciEngine.WriteSelectorValue(segMan, mover, o => o.b_di, (ushort)mover_di);

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
                {
                    // In sci1egaonly this block of code was outside of the main if,
                    // but client_x/client_y aren't set there, so it was an
                    // uninitialized read in SSCI. (This issue was fixed in sci1early.)
                    if (handleMoveCount)
                        SciEngine.WriteSelectorValue(segMan, mover, o => o.b_movCnt, (ushort)mover_moveCnt);
                    // We need to compare directly in here, complete may have happened during
                    //  the current move
                    if ((client_x == mover_x) && (client_y == mover_y))
                        SciEngine.InvokeSelector(s, mover, o => o.moveDone, argc, argv);
                    return s.r_acc;
                }
            }

            if (handleMoveCount)
                SciEngine.WriteSelectorValue(segMan, mover, o => o.b_movCnt, (ushort)mover_moveCnt);

            return s.r_acc;
        }
    }
}
