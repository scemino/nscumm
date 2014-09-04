using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Tmp
{
    class MainClass
    {
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
            return 1;
        }
    }
}
