//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
