using NScumm.Core.IO;
using Mono.Options;
using System.Collections.Generic;
using System.Linq;

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
            System.Console.WriteLine("\tv3 old [experimental] (indy3 16 colors)");
            System.Console.WriteLine("\tv3 (indy3 256 colors)");
            System.Console.WriteLine("\tv4 (monkey island1)");
            System.Console.WriteLine("\tv5 (monkey island2)");
            System.Console.WriteLine("\tv6 [experimental] (Day of the tentacle)");
            System.Console.WriteLine("\tv8 [experimental] (The Curse of Monkey Island)");
        }

        public static int Main(string[] args)
        {
            bool showHelp = false;
            string input = null;
            var scripts = new List<int>();
            var scriptObjects = new List<int>();
            var rooms = new List<int>();
            var objects = new List<int>();

            var options = new OptionSet()
            {
                { "f=", "The input file",   v => input = v },
                { "s|script=", "the global script number to dump", (int s) => scripts.Add(s) },
                { "so|script_object=", "the object number whose script has to be dumped", (int s) => scriptObjects.Add(s) },
                { "r|room=", "the room image to dump", (int r) => rooms.Add(r) },
                { "o|object=", "the object images to dump",(int o) => objects.Add(o) },
                { "h|help",  "show this message and exit",  v => showHelp = v != null }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                System.Console.WriteLine("Try `nsdump --help' for more information.");
                return 1;
            }

            if (input == null || showHelp)
            {
                Usage(options);
                return 0;
            }

            var game = GameManager.GetInfo(input);
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

            // dump scripts
            var objs = index.Rooms.SelectMany(r => r.Objects).Where(o => scriptObjects.Contains(o.Number)).ToList();
            foreach (var obj in objs)
            {
                dumper.WriteLine("obj {0} {1} {{", obj.Number, System.Text.Encoding.ASCII.GetString(obj.Name));
                dumper.WriteLine("Script offset: {0}", obj.Script.Offset);
                foreach (var off in obj.ScriptOffsets)
                {
                    dumper.WriteLine("idx #{0}: {1}", off.Key, off.Value - obj.Script.Offset);
                }
                dumper.WriteLine("script");
                scriptDumper.DumpScript(obj.Script.Data, dumper);
            }

            // dump rooms
            if (rooms.Count > 0)
            {
                var imgDumper = new ImageDumper(game);
                imgDumper.DumpRoomImages(index, rooms);
            }

            // dump objects
            if (objects.Count > 0)
            {
                var imgDumper = new ImageDumper(game);
                imgDumper.DumpObjectImages(index, objects);
            }

            return 0;
        }
    }
}
