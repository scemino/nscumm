using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Tmp
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
            System.Console.WriteLine("\tv5 [experimental] (monkey island2)");
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                var game = GameManager.GetInfo(args[0]);
                var index = ResourceManager.Load(game);

                var scriptDumper = new ScriptDumper(game);
                scriptDumper.DumpScripts(index);
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
