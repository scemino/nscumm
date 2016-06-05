//
//  QueenEngine.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    struct WalkData
    {
        public short dx, dy;
        public Area area;
        public ushort areaNum;
        public MovePersonAnim anim;
    }

    struct MovePersonAnim
    {
        public short firstFrame;
        public short lastFrame;
        public Direction facing;

        public void Set(short ff, short lf, Direction dir)
        {
            firstFrame = ff;
            lastFrame = lf;
            facing = dir;
        }
    }

    public class Walk
    {
        const int MAX_WALK_DATA = 16;

        QueenEngine _vm;
        /// <summary>
        /// Set if stopJoe() is called.
        /// </summary>
        bool _joeInterrupted;
        /// <summary>
        /// Set if handleSpecialArea() is called.
        /// </summary>
        bool _joeMoveBlock;
        Area[] _roomArea;
        /// <summary>
        /// Number of areas for current room.
        /// </summary>
        ushort _roomAreaCount;
        ushort _walkDataCount;
        ushort[] _areaStrike = new ushort[MAX_WALK_DATA];
        ushort _areaStrikeCount;

        ushort[] _areaList = new ushort[MAX_WALK_DATA];
        ushort _areaListCount;

        WalkData[] _walkData = new WalkData[MAX_WALK_DATA];

        public Walk(QueenEngine vm)
        {
            _vm = vm;
        }

        public void MovePerson(Person p, short moveToX, short moveToY, int i, short image)
        {
            throw new NotImplementedException();
        }

        public short MoveJoe(Direction direction, short endx, short endy, bool inCutaway)
        {
            _joeInterrupted = false;
            _joeMoveBlock = false;
            short can = 0;
            InitWalkData();

            ushort oldx = (ushort)_vm.Graphics.Bobs[0].x;
            ushort oldy = (ushort)_vm.Graphics.Bobs[0].y;

            _vm.Logic.JoeWalk = JoeWalkMode.MOVE;

            ushort oldPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, oldx, oldy);
            ushort newPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, (ushort)endx, (ushort)endy);

            D.Debug(9, $"Walk::moveJoe({direction}, {oldx}, {oldy}, {endx}, {endy}) - old = {oldPos}, new = {newPos}");

            // if in cutaway, allow Joe to walk anywhere
            if (newPos == 0 && inCutaway)
            {
                IncWalkData((short)(short)oldx, (short)oldy, endx, endy, oldPos);
            }
            else
            {
                if (Calc(oldPos, newPos, oldx, oldy, endx, endy))
                {
                    if (_walkDataCount > 0)
                    {
                        AnimateJoePrepare();
                        AnimateJoe();
                        if (_joeInterrupted)
                        {
                            can = -1;
                        }
                    }
                }
                else
                {
                    // path has been blocked, make Joe say so
                    _vm.Logic.MakeJoeSpeak(4);
                    can = -1;
                }
            }

            _vm.Graphics.Bobs[0].animating = false;
            if (_joeMoveBlock)
            {
                can = -2;
                _joeMoveBlock = false;
            }
            else if (direction > 0)
            {
                _vm.Logic.JoeFacing = direction;
            }
            _vm.Logic.JoePrevFacing = _vm.Logic.JoeFacing;
            _vm.Logic.JoeFace();
            return can;
        }

        bool Calc(ushort oldPos, ushort newPos, ushort oldx, ushort oldy, short endx, short endy)
        {
            throw new NotImplementedException();
        }

        void AnimateJoe()
        {
            throw new NotImplementedException();
        }

        void AnimateJoePrepare()
        {
            throw new NotImplementedException();
        }

        void IncWalkData(short px, short py, short x, short y, ushort areaNum)
        {
            D.Debug(9, $"Walk::incWalkData({x - px}, {y - py}, {areaNum})");
            if (px != x || py != y)
            {
                ++_walkDataCount;
                WalkData pwd = _walkData[_walkDataCount];
                pwd.dx = (short)(x - px);
                pwd.dy = (short)(y - py);
                pwd.area = _roomArea[areaNum];
                pwd.areaNum = areaNum;
            }
        }

        void InitWalkData()
        {
            ushort curRoom = _vm.Logic.CurrentRoom;
            _roomArea = _vm.Grid.Areas[curRoom];
            _roomAreaCount = (ushort)_vm.Grid.AreaMax[curRoom];

            _walkDataCount = 0;
            Array.Clear(_walkData, 0, _walkData.Length);
            _areaStrikeCount = 0;
            Array.Clear(_areaStrike, 0, _areaStrike.Length);
            _areaListCount = 0;
            Array.Clear(_areaList, 0, _areaList.Length);
        }
    }
}

