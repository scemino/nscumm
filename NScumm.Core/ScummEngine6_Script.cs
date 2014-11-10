//
//  ScummEngine6_Script.cs
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        bool _skipVideo;

        [OpCode(0x5e)]
        void StartScript(int flags, byte script, int[] args)
        {
            RunScript(script, (flags & 1) != 0, (flags & 2) != 0, args);
        }

        [OpCode(0x5f)]
        void StartScriptQuick(byte script, int[] args)
        {
            RunScript(script, false, false, args);
        }

        [OpCode(0x65, 0x66)]
        void StopObjectCode6()
        {
            StopObjectCode();
        }

        [OpCode(0x7c)]
        void StopScript6(int script)
        {
            if (script == 0)
            {
                StopObjectCode();
            }
            else
            {
                StopScript(script);
            }
        }

        [OpCode(0xb3)]
        void StopSentence()
        {
            SentenceNum = 0;
            StopScript(Variables[VariableSentenceScript.Value]);
            // TODO: scumm6
//            ClearClickedStatus();
        }

        [OpCode(0xb0)]
        void Delay(int delay)
        {
            DelayCore(delay);
        }

        [OpCode(0xb1)]
        void DelaySeconds(int seconds)
        {
            DelayCore(seconds * 60);
        }

        [OpCode(0xb2)]
        void DelayMinutes(int minutes)
        {
            DelayCore(minutes * 3600);
        }

        void DelayCore(int delay)
        {
            Slots[CurrentScript].Delay = delay;
            Slots[CurrentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        [OpCode(0x6c)]
        void BreakHere()
        {
            // ?? UpdateScriptPtr();
            CurrentScript = 0xFF;
        }

        [OpCode(0x95)]
        void BeginOverride()
        {
            BeginOverrideCore();
            _skipVideo = false;
        }

        [OpCode(0x96)]
        void EndOverride()
        {
            EndOverrideCore();
        }

        [OpCode(0x8b)]
        void IsScriptRunning(int script)
        {
            Push(IsScriptRunningCore(script));
        }

        [OpCode(0xd8)]
        void IsRoomScriptRunning(int script)
        {
            Push(IsRoomScriptRunningCore(script));
        }

        bool IsRoomScriptRunningCore(int script)
        {
            for (var i = 0; i < Slots.Length; i++)
                if (Slots[i].Number == script && Slots[i].Where == WhereIsObject.Room && Slots[i].Status != ScriptStatus.Dead)
                    return true;
            return false;
        }
    }
}

