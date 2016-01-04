//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 3 of the License; or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful;
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not; see <http://www.gnu.org/licenses/>.


using NScumm.Sci.Graphics;

namespace NScumm.Sci.Engine
{
    partial class Kernel
    {
        private static Register kAddMenu(EngineState s, int argc, StackPtr? argv)
        {
            string title = s._segMan.GetString(argv.Value[0]);
            string content = s._segMan.GetString(argv.Value[1]);

            SciEngine.Instance._gfxMenu.KernelAddEntry(title, content, Register.Make(argv.Value[1]));
            return s.r_acc;
        }

        private static Register kSetMenu(EngineState s, int argc, StackPtr? argv)
        {
            ushort menuId = (ushort)(argv.Value[0].ToUInt16() >> 8);
            ushort itemId = (ushort)(argv.Value[0].ToUInt16() & 0xFF);
            MenuAttribute attributeId;
            int argPos = 1;
            Register value;

            while (argPos < argc)
            {
                attributeId = (MenuAttribute)argv.Value[argPos].ToUInt16();
                // Happens in the fanmade game Cascade Quest when loading - bug #3038767
                value = (argPos + 1 < argc) ? argv.Value[argPos + 1] : Register.NULL_REG;
                SciEngine.Instance._gfxMenu.KernelSetAttribute(menuId, itemId, attributeId, value);
                argPos += 2;
            }
            return s.r_acc;
        }

        private static Register kDrawMenuBar(EngineState s, int argc, StackPtr? argv)
        {
            bool clear = argv.Value[0].IsNull ? true : false;

            SciEngine.Instance._gfxMenu.KernelDrawMenuBar(clear);
            return s.r_acc;
        }

        private static Register kDrawStatus(EngineState s, int argc, StackPtr? argv)
        {
            Register textReference = argv.Value[0];
            string text;
            short colorPen = (argc > 1) ? argv.Value[1].ToInt16() : (short)0;
            short colorBack = (argc > 2) ? argv.Value[2].ToInt16() : SciEngine.Instance._gfxScreen.ColorWhite;

            if (!textReference.IsNull)
            {
                // Sometimes this is called without giving text, if thats the case dont process it.
                text = s._segMan.GetString(textReference);

                if (text == "Replaying sound")
                {
                    // Happens in the fanmade game Cascade Quest when loading - ignore it
                    return s.r_acc;
                }

                SciEngine.Instance._gfxMenu.KernelDrawStatus(SciEngine.Instance.StrSplit(text, null), colorPen, colorBack);
            }
            return s.r_acc;
        }
    }
}
