//
//  GameDetector.cs
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

using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NScumm.Core.IO
{
    public interface IGameDescriptor
    {
        string Id { get; }
        string Description { get; }
        CultureInfo Culture { get; }
        Platform Platform { get; }
        int Width { get; }
        int Height { get; }
        PixelFormat PixelFormat { get; }
        string Path { get; }
    }

    public class GameDetected
    {
        public GameDetected(IGameDescriptor game, IMetaEngine engine)
        {
            Game = game;
            Engine = engine;
        }

        public IMetaEngine Engine { get; private set; }
        public IGameDescriptor Game { get; private set; }
    }

    public interface IMetaEngine
    {
        GameDetected DetectGame(string path);

        IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false);
    }

    public class GameDetector
    {
        private List<IMetaEngine> _engines;

        public GameDetector(string engineDirectory)
        {
            _engines = new List<IMetaEngine>(GetEngines(typeof(GameDetector).Assembly));

            if (ServiceLocator.FileStorage.DirectoryExists(engineDirectory))
            {
                var dlls = ServiceLocator.FileStorage.EnumerateFiles(engineDirectory, "*.dll");
                foreach (var dll in dlls)
                {
                    try
                    {
                        var asm = ServiceLocator.Platform.LoadAssembly(dll);
                        if (asm != null)
                        {
                            _engines.AddRange(GetEngines(asm));
                        }
                    }
                    catch (BadImageFormatException) { }
                }
            }
        }

        public GameDetected DetectGame(string path)
        {
            return _engines.Select(e => e.DetectGame(path)).FirstOrDefault(o => o != null && o.Game != null);
        }

        private IEnumerable<IMetaEngine> GetEngines(Assembly asm)
        {
            return asm.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IMetaEngine))).Select(t => Activator.CreateInstance(t) as IMetaEngine);
        }
    }
}
