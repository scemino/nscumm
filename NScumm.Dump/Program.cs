using NScumm.Scumm.IO;
using Mono.Options;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core;
using System.Text;
using System;
using NScumm.Scumm;

namespace NScumm.Dump
{
    class MainClass
    {
        static void Usage(OptionSet p)
        {
            System.Console.WriteLine("SCUMM script and image dumper");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            p.WriteOptionDescriptions(System.Console.Out);
            System.Console.WriteLine();
            System.Console.WriteLine("Supported engines:");
            System.Console.WriteLine("\tv2 [only images] (Maniac Mansion/Zak)");
            System.Console.WriteLine("\tv3 old [experimental] (indy3 16 colors)");
            System.Console.WriteLine("\tv3 (indy3 256 colors)");
            System.Console.WriteLine("\tv4 (monkey island1)");
            System.Console.WriteLine("\tv5 (monkey island2)");
            System.Console.WriteLine("\tv6 [experimental] (Day of the tentacle)");
            System.Console.WriteLine("\tv7 [experimental] (Full Throttle/The Dig)");
            System.Console.WriteLine("\tv8 (The Curse of Monkey Island)");
        }

        static void Initialize()
        {
            ServiceLocator.Platform = new Platform();
            ServiceLocator.FileStorage = new FileStorage();
        }

        public static int Main(string[] args)
        {
            bool showHelp = false;
            bool dumpAllObjectImages = false;
            bool dumpAllRoomImages = false;
            string input = null;
            var scripts = new List<int>();
            var scriptObjects = new List<int>();
            var scriptRooms = new List<int>();
            var rooms = new List<int>();
            var objects = new List<int>();

            Initialize();

            var options = new OptionSet()
            {
                { "f=", "The input file",   v => input = v },
                { "s|script=", "the global script number to dump", (int s) => scripts.Add(s) },
                { "so|script_object=", "the object number whose script has to be dumped", (int s) => scriptObjects.Add(s) },
                { "sr|script_room=", "the room number whose script has to be dumped", (int s) => scriptRooms.Add(s) },
                { "r|room=", "the room image to dump", (int r) => rooms.Add(r) },
                { "ra", "dump all object images",ra => dumpAllRoomImages = ra != null },
                { "o|object=", "the object images to dump",(int o) => objects.Add(o) },
                { "oa", "dump all object images",oa => dumpAllObjectImages = oa != null },
                { "h|help",  "show this message and exit",  v => showHelp = v != null }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                System.Console.WriteLine("Try `nsdump --help' for more information.");
                return 1;
            }

            if (input == null || showHelp)
            {
                Usage(options);
                return 0;
            }

            var resStream = typeof(GameManager).Assembly.GetManifestResourceStream(typeof(GameManager), "Nscumm.xml");
            var gm = GameManager.Create(resStream);
            var game = gm.GetInfo(input);
            if (game == null)
            {
                System.Console.Error.WriteLine("This game is not supported, sorry please contact me if you want to support this game.");
                return 1;
            }

            var index = ResourceManager.Load(game);
            var scriptDumper = new ScriptDumper(game);
            var dumper = new ConsoleDumper();

            // dump scripts
            foreach (var script in scripts)
            {
                var scr = index.GetScript(script);
                dumper.WriteLine("script " + script);
                scriptDumper.DumpScript(scr, dumper);
            }

            // dump room scripts
            if (scriptRooms.Count > 0)
            {
                var roomScripts = index.Rooms.Where(r => scriptRooms.Contains(r.Number)).ToList();
                foreach (var room in roomScripts)
                {
                    dumper.WriteLine("Room {0}", room.Number);
                    if (room.EntryScript.Data.Length > 0)
                    {
                        dumper.WriteLine("Entry");
                        scriptDumper.DumpScript(room.EntryScript.Data, dumper);
                        dumper.WriteLine();
                    }
                    if (room.ExitScript.Data.Length > 0)
                    {
                        dumper.WriteLine("Exit");
                        scriptDumper.DumpScript(room.ExitScript.Data, dumper);
                        dumper.WriteLine();
                    }
                    for (int i = 0; i < room.LocalScripts.Length; i++)
                    {
                        var ls = room.LocalScripts[i];
                        if (ls != null)
                        {
                            dumper.WriteLine("LocalScript {0}", i);
                            scriptDumper.DumpScript(ls.Data, dumper);
                        }
                    }
                    for (int i = 0; i < room.Objects.Count; i++)
                    {
                        var obj = room.Objects[i];
                        if (obj != null && obj.Script.Data.Length > 0)
                        {
                            var sb = new StringBuilder();
                            var decoder = new TextDecoder(sb);
                            var text = new ScummText(obj.Name);
                            text.Decode(decoder);

                            dumper.WriteLine("obj {0} {1}", obj.Number, sb);
                            var tmp = obj.Script.Offset;
                            var offsets = new long[] { 0, obj.Script.Data.Length }.Concat(obj.ScriptOffsets.Select(off => off.Value - tmp)).OrderBy(o => o).Distinct().ToList();
                            var scr = new List<Tuple<long, byte[]>>();
                            for (int j = 0; j < offsets.Count - 1; j++)
                            {
                                var len = offsets[j + 1] - offsets[j];
                                var d = new byte[len];
                                Array.Copy(obj.Script.Data, offsets[j], d, 0, len);
                                scr.Add(Tuple.Create(offsets[j], d));
                            }
                            foreach (var s in scr)
                            {
                                var keys = obj.ScriptOffsets.Where(o => o.Value - tmp == s.Item1).Select(o => o.Key).ToList();
                                foreach (var key in keys)
                                {
                                    dumper.WriteLine("{0}", (VerbsV0)key);
                                }
                                scriptDumper.DumpScript(s.Item2, dumper);
                            }
                            dumper.WriteLine();
                        }
                    }
                }
            }

            // dump object scripts
            if (scriptObjects.Count > 0)
            {
                var objs = index.Rooms.SelectMany(r => r.Objects).Where(o => scriptObjects.Contains(o.Number)).ToList();
                foreach (var obj in objs)
                {
                    dumper.WriteLine("obj {0} {1} {{", obj.Number, System.Text.Encoding.UTF8.GetString(obj.Name));
                    dumper.WriteLine("Script offset: {0}", obj.Script.Offset);
                    foreach (var off in obj.ScriptOffsets)
                    {
                        dumper.WriteLine("idx #{0}: {1}", off.Key, off.Value - obj.Script.Offset);
                    }
                    dumper.WriteLine("script");
                    scriptDumper.DumpScript(obj.Script.Data, dumper);
                }
            }

            // dump rooms
            if (dumpAllRoomImages || rooms.Count > 0)
            {
                var imgDumper = new ImageDumper(game);
                imgDumper.DumpRoomImages(index, dumpAllRoomImages ? null : rooms);
            }

            // dump objects
            if (dumpAllObjectImages || objects.Count > 0)
            {
                var imgDumper = new ImageDumper(game);
                imgDumper.DumpObjectImages(index, dumpAllObjectImages ? null : objects);
            }

            return 0;
        }
    }
}
