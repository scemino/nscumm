//
//  ScummEngine_Script.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void ChainScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var vars = GetWordVarArgs();
            var cur = CurrentScript;

            Slots[cur].Number = 0;
            Slots[cur].Status = ScriptStatus.Dead;
            CurrentScript = 0xFF;

            RunScript(script, Slots[cur].FreezeResistant, Slots[cur].Recursive, vars);
        }

        void FreezeScripts()
        {
            var scr = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (scr != 0)
                FreezeScripts(scr);
            else
                UnfreezeScripts();
        }

        protected override void BeginOverride()
        {
            if (ReadByte() != 0)
                BeginOverrideCore();
            else
                EndOverrideCore();
        }

        void CutScene()
        {
            var args = GetWordVarArgs();
            BeginCutscene(args);
        }

        void StartObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var script = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);

            var data = GetWordVarArgs();
            RunObjectScript(obj, script, false, false, data);
        }

        void StopObjectScript()
        {
            StopObjectScriptCore((ushort)GetVarOrDirectWord(OpCodeParameter.Param1));
        }

        protected override void StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var data = GetWordVarArgs();

            // Method used by original games to skip copy protection scheme
            if (!Settings.CopyProtection)
            {
                // Copy protection was disabled in LucasArts Classic Adventures (PC Disk)
                if (Game.GameId == GameId.Loom && Game.Platform == Platform.DOS && Game.Version == 3 && CurrentRoom == 69 && script == 201)
                    script = 205;
                // Copy protection was disabled in KIXX XL release (Amiga Disk) and
                // in LucasArts Classic Adventures (PC Disk)
                if (Game.GameId == Scumm.IO.GameId.Monkey1 && Game.Variant == "VGA" && script == 0x98)
                {
                    return;
                }
                // Copy protection was disabled in LucasArts Mac CD Game Pack II (Macintosh CD)
                if (Game.GameId == GameId.Monkey1 && Game.Platform == Platform.Macintosh && script == 155)
                    return;
            }

            RunScript(script, (op & 0x20) != 0, (op & 0x40) != 0, data);
        }

        protected override void StopScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);

            if (script == 0)
                StopObjectCode();
            else
                StopScript(script);
        }

        void DoSentence()
        {
            var verb = GetVarOrDirectByte(OpCodeParameter.Param1);
            if (verb == 0xFE)
            {
                SentenceNum = 0;
                StopScript(Variables[VariableSentenceScript.Value]);
                ClearClickedStatus();
                return;
            }

            var objectA = GetVarOrDirectWord(OpCodeParameter.Param2);
            var objectB = GetVarOrDirectWord(OpCodeParameter.Param3);
            DoSentence((byte)verb, (ushort)objectA, (ushort)objectB);
        }
    }
}

