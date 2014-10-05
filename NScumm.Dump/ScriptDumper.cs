//
//  ScriptDumper.cs
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
using System;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Dump
{
    public class ScriptDumper
    {
        GameInfo Game
        {
            get;
            set;
        }

        public ScriptDumper(GameInfo game)
        {
            Game = game;
        }

        public void DumpScripts(ResourceManager index)
        {
            var dumper = new ConsoleDumper();
            foreach (var script in index.Scripts)
            {
                dumper.WriteLine("script " + script.Id);
                DumpScript(script.Data, dumper);
            }
            foreach (var room in index.Rooms)
            {
                dumper.WriteLine("room {0} {1} {{", room.Number, room.Name);
                foreach (var obj in room.Objects)
                {
                    if (obj.Script.Data != null && obj.Script.Data.Length > 0)
                    {
                        dumper.WriteLine("obj {0} {1} {{", obj.Number, System.Text.Encoding.ASCII.GetString(obj.Name));
                        foreach (var off in obj.ScriptOffsets)
                        {
                            dumper.WriteLine("idx #{0}: {1}", off.Key, off.Value);
                        }
                        dumper.WriteLine("script");
                        DumpScript(obj.Script.Data, dumper);
                    }
                }
                if (room.EntryScript.Data.Length > 0)
                {
                    dumper.WriteLine("Entry script");
                    DumpScript(room.EntryScript.Data, dumper);
                }
                if (room.EntryScript.Data.Length > 0)
                {
                    dumper.WriteLine("Exit script");
                    DumpScript(room.ExitScript.Data, dumper);

                }
                for (int i = 0; i < room.LocalScripts.Length; i++)
                {
                    var script = room.LocalScripts[i];
                    if (script != null && script.Data != null)
                    {
                        dumper.WriteLine("Local script " + i);
                        DumpScript(script.Data, dumper);
                    }
                }
                dumper.WriteLine("}");
            }
        }

        public void DumpScript(byte[] data, IDumper dumper)
        {
//            try
            {
                var scriptInterpreter = ScriptParser.Create(Game);
                var resolveVarVisitor = new ResolveVariablesAstVisitor(scriptInterpreter.KnownVariables);
                var visitor = new DumpAstVisitor();
                var compilationUnit = scriptInterpreter.Parse(data);
                var cuWithResolvedVariables = (CompilationUnit)compilationUnit.Accept(resolveVarVisitor);

                var cuWithIfs = new ChangeJumpToIf().Change(cuWithResolvedVariables);

                dumper.Write(cuWithIfs.Accept(visitor));
            }
//            catch (Exception e)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//                Console.WriteLine(e);
//                Console.ResetColor();
//            }
//            finally
//            {
//            }
        }
    }
}

