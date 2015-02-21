using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Dump
{
    class MainClass
    {
        static void Usage()
        {
            System.Console.WriteLine("SCUMM script and image dumper");
            System.Console.WriteLine("Syntax:");
            System.Console.WriteLine("\tnsdump index");
            System.Console.WriteLine();
            System.Console.WriteLine("Supported engines:");
            System.Console.WriteLine("\tv3 old [experimental] (indy3 16 colors)");
            System.Console.WriteLine("\tv3 (indy3 256 colors)");
            System.Console.WriteLine("\tv4 (monkey island1)");
            System.Console.WriteLine("\tv5 (monkey island2)");
            System.Console.WriteLine("\tv6 [experimental] (Day of the tentacle)");
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                var game = GameManager.GetInfo(args[0]);
                var index = ResourceManager.Load(game);

//                var scriptDumper = new ScriptDumper(game);
//                scriptDumper.DumpScripts(index);
                var imgDumper = new ImageDumper(game);
                imgDumper.DumpImages(index);
                return 0;
            }
            else
            {
                Usage();
            }
            return 1;
        }
    }
}
