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
using System;

namespace NScumm.Core
{
    public class ScummEngine5: ScummEngine4
    {
        const int VariableCameraPosX = 0x02;
        const int VariableSoundResult = 56;
        const int VariableTalkStopKey = 57;
        const int VariableFadeDelay = 59;
        const int VariableSoundParam = 64;
        const int VariableSoundParam2 = 65;
        const int VariableSoundParam3 = 66;
        const int VariableInputMode = 67;
        // 1 is keyboard, 2 is joystick, 3 is mouse
        const int VariableMemoryPerformance = 68;
        const int VariableVideoPerformance = 69;
        const int VariableRoomFlag = 70;
        const int VariableGameLoaded = 71;
        const int VariableNewRoom = 72;

        public ScummEngine5(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IAudioDriver audioDriver)
            : base(game, graphicsManager, inputManager, audioDriver)
        {
        }

        protected override void InitOpCodes()
        {
            base.InitOpCodes();

            _opCodes[0x25] = PickupObject;
            _opCodes.Remove(0x45);
            _opCodes[0x65] = PickupObject;
            _opCodes[0xA5] = PickupObject;
            _opCodes.Remove(0xC5);
            _opCodes[0xE5] = PickupObject;

            _opCodes.Remove(0x50);
            _opCodes.Remove(0xD0);

            _opCodes.Remove(0x5C);
            _opCodes.Remove(0xDC);

            _opCodes[0x0F] = GetObjectState;
            _opCodes.Remove(0x4F);
            _opCodes[0x8F] = GetObjectState;
            _opCodes.Remove(0xCF);

            _opCodes.Remove(0x2F);
            _opCodes.Remove(0x6F);
            _opCodes.Remove(0xAF);
            _opCodes.Remove(0xEF);

            //_opCodes[0xA7] = Dummy;
            _opCodes.Remove(0xA7);

            _opCodes[0x22] = GetAnimCounter;
            _opCodes[0xA2] = GetAnimCounter;

            _opCodes[0x3B] = GetActorScale;
            _opCodes[0x4C] = SoundKludge;
            _opCodes[0xBB] = GetActorScale;
        }

        protected override void DrawObjectCore(out int xpos, out int ypos, out int state)
        {
            xpos = 255;
            ypos = 255;
            state = 1;
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:                                                                         /* draw at */
                    xpos = GetVarOrDirectWord(OpCodeParameter.Param1);
                    ypos = GetVarOrDirectWord(OpCodeParameter.Param2);
                    break;
                case 2:                                                                         /* set state */
                    state = GetVarOrDirectWord(OpCodeParameter.Param1);
                    break;
                case 0x1F:                                                                      /* neither */
                    break;
                default:
                    throw new NotSupportedException(string.Format("DrawObject: unknown subopcode {0:X2}", _opCode & 0x1F));
            }

        }

        protected override void PickupObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);
            if (room == 0)
                room = _roomResource;
            AddObjectToInventory(obj, (byte)room);
            PutOwner(obj, (byte)_variables[VariableEgo]);
            PutClass(obj, (int)ObjectClass.Untouchable, true);
            PutState(obj, 1);
            MarkObjectRectAsDirty(obj);
            ClearDrawObjectQueue();
            RunInventoryScript(1);

        }

        void GetObjectState()
        {
            GetResult();
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(GetState(obj));
        }

        void GetAnimCounter()
        {
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);
            SetResult(_actors[index].Cost.AnimCounter);
        }

        void GetActorScale()
        {
            var index = GetVarOrDirectByte(OpCodeParameter.Param1);
            SetResult(_actors[index].ScaleX);
        }
    }
}

