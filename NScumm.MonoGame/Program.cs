/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using Mono.Options;
using NScumm.Core;
using NScumm.Core.IO;
using System.Reflection;
using System.IO;
using System.Linq;

namespace NScumm.MonoGame
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            var musicDriver = "adlib";
            var showVersion = false;
            var showHelp = false;
            var listAudioDevices = false;
            var bootParam = 0;
            var copyProtection = false;
            string switches = null;
            var options = new OptionSet
            {
                { "v|version", "Display NScumm version information and exit", v => showVersion = v != null },
                { "h|help", "Display a brief help text and exit", h => showHelp = h != null },
                { "e|music-driver=", "Select music driver", d => musicDriver = d },
                { "list-audio-devices", "List all available audio devices", b => listAudioDevices = b != null },
                { "b|boot-param=", "Pass number to the boot script (boot param)", (int b) => bootParam = b },
                { "debugflags=", "Enable engine specific debug flags (separated by commas)", d => switches = d },
                { "copy_protection", "Enable copy protection in SCUMM games, when NScumm disables it by default.", b => copyProtection = b != null }
            };

            try
            {
                var extras = options.Parse(args);
                Initialize(switches);
                if (showVersion)
                {
                    ShowVersion();
                }
                else if (showHelp)
                {
                    Usage(options);
                }
                else if (listAudioDevices)
                {
                    ListAudioDevices();
                }
                else if (extras.Count == 1)
                {
                    var path = ScummHelper.NormalizePath(extras[0]);
                    if (File.Exists(path))
                    {
                        var gd = new GameDetector(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
                        var info = gd.DetectGame(path);
                        //var resStream = typeof(GameManager).Assembly.GetManifestResourceStream(typeof(GameManager), "Nscumm.xml");
                        //var gm = GameManager.Create(resStream);
                        //var info = gm.GetInfo(path);
                        if (info == null)
                        {
                            Console.Error.WriteLine("This game is not supported, sorry please contact me if you want to support this game.");
                        }
                        else
                        {
                            var settings = new GameSettings(info.Game, info.Engine) { AudioDevice = musicDriver, CopyProtection = copyProtection, BootParam = bootParam };
                            var game = new ScummGame(settings);
                            game.Run();
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("The file {0} does not exist.", path);
                    }
                }
                else
                {
                    Usage(options);
                    return 1;
                }
            }
            catch (ArgumentException)
            {
                Usage(options);
                return 1;
            }
            catch (OptionException)
            {
                Usage(options);
                return 1;
            }
            return 0;
        }

        static void Initialize(string sw)
        {
            ServiceLocator.Platform = new Platform();
            ServiceLocator.FileStorage = new FileStorage();
            ServiceLocator.SaveFileManager = new SaveFileManager(ServiceLocator.FileStorage);
            var switches = string.IsNullOrEmpty(sw) ? Enumerable.Empty<string>() : sw.Split(',');
            ServiceLocator.TraceFatory = new TraceFactory(switches);
        }

        static void ShowVersion()
        {
            var filename = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            var asm = Assembly.GetExecutingAssembly();
            var info = new FileInfo(asm.Location);
            Console.WriteLine("{0} {1} ({2:R})", filename, asm.GetName().Version, info.CreationTime);
        }

        static void ListAudioDevices()
        {
            Console.WriteLine("{0,-10} {1}", "Id", "Description");
            Console.WriteLine("{0,-10} {1}", new string('-', 10), new string('-', 20));
            var plugins = NScumm.Core.Audio.MusicManager.GetPlugins();
            foreach (var plugin in plugins)
            {
                Console.WriteLine("{0,-10} {1}", plugin.Id, plugin.Name);
            }
        }

        static void Usage(OptionSet options)
        {
            var filename = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Usage : {0} [OPTIONS]... [FILE]", filename);
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}