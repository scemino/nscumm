using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.MonoGame
{
    static class GameDetectorExtension
    {
        public static void AddPluginsFromDirectory(this GameDetector gameDetector, string engineDirectory)
        {
            if (ServiceLocator.FileStorage.DirectoryExists(engineDirectory))
            {
                var dlls = ServiceLocator.FileStorage.EnumerateFiles(engineDirectory, "*.dll");
                foreach (var dll in dlls)
                {
                    try
                    {
                        var asm = Assembly.LoadFile(dll);
                        if (asm != null)
                        {
                            var engines = GetEngines(asm);
                            foreach (var engine in engines)
                            {
                                gameDetector.Add(engine);
                            }
                        }
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static IEnumerable<IMetaEngine> GetEngines(Assembly asm)
        {
            return asm.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IMetaEngine))).Select(t => Activator.CreateInstance(t) as IMetaEngine);
        }
    }
}
