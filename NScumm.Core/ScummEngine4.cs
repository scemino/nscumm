//
//  ScummEngine4.cs
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

//
//  ScummEngine4.cs
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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;

namespace NScumm.Core
{   
    public class ScummEngine4: ScummEngine
    {
        public ScummEngine4(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IAudioDriver audioDriver)
            : base(game,graphicsManager,inputManager,audioDriver)
        {
        }

        protected override void InitOpCodes()
        {
            base.InitOpCodes();

            _opCodes[0x30] = MatrixOperations;
            _opCodes[0xB0] = MatrixOperations;
            _opCodes[0x3B] = GetActorScale;
            _opCodes[0xBB] = GetActorScale;
            _opCodes[0x4C] = SoundKludge;
        }

        void GetActorScale()
        {
            GetResult();
            var act = GetVarOrDirectByte(OpCodeParameter.Param1);
            var actor = _actors[act];
            SetResult((int)actor.ScaleX);
        }

        void SoundKludge()
        {
            var items = GetWordVarArgs();
            _sound.SoundKludge(items);
        }

        void MatrixOperations()
        {
            int a, b;

            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxFlags(a, b);
                    break;

                case 2:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, b);
                    break;

                case 3:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    SetBoxScale(a, (b - 1) | 0x8000);
                    break;

                case 4:
                    CreateBoxMatrix();
                    break;
            }
        }
    }
}

