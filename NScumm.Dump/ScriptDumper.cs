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

namespace NScumm.Tmp
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
                DumpScript("script " + script.Id, script.Data, dumper);
            }
            foreach (var room in index.Rooms)
            {
                dumper.WriteLine("room {0} {1} {{", room.Number, room.Name);
                dumper.Indent();
                foreach (var obj in room.Objects)
                {
                    if (obj.Script.Data.Length > 0)
                    {
                        dumper.WriteLine("obj {0} {1} {{", obj.Number, System.Text.Encoding.ASCII.GetString(obj.Name));
                        dumper.Indent();
                        foreach (var off in obj.ScriptOffsets)
                        {
                            dumper.WriteLine("idx #{0}: {1}", off.Key, off.Value);
                        }
                        DumpScript("script", obj.Script.Data, dumper);
                        dumper.Deindent();
                        dumper.WriteLine("}");
                    }
                }
                if (room.EntryScript.Data.Length > 0)
                {
                    DumpScript("Entry script", room.EntryScript.Data, dumper);
                }
                if (room.EntryScript.Data.Length > 0)
                {
                    DumpScript("Exit script", room.ExitScript.Data, dumper);

                }
                for (int i = 0; i < room.LocalScripts.Length; i++)
                {
                    var script = room.LocalScripts[i];
                    if (script != null && script.Data != null)
                    {
                        DumpScript("Local script " + i, script.Data, dumper);
                    }
                }
                dumper.Deindent();
                dumper.WriteLine("}");
            }
        }

        void DumpScript(string id, byte[] data, IDumper dumper)
        {
            dumper.Write(id);
            dumper.WriteLine(" {");
            dumper.Indent();

            try
            {
                var scriptInterpreter = ScriptParser.Create(Game);
                var visitor = new DumpAstVisitor();
                var compilationUnit = scriptInterpreter.Parse(data);
                dumper.Write(compilationUnit.Accept(visitor));
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ResetColor();
            }
            finally
            {
                dumper.Deindent();
                dumper.WriteLine("}");
            }
        }
    }
}

