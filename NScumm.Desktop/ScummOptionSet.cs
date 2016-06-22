//
//  ScummOptionSet.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using Mono.Options;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm
{
    public class ScummOptionSet : OptionSet
    {
        public string MusicDriver { get; set; }

        public int BootParam { get; set; }

        public string Switches { get; set; }

        public bool CopyProtection { get; set; }

        private bool listAudioDevices;
        private bool showVersion;
        private bool showHelp;

        public ScummOptionSet()
        {
            MusicDriver = "adlib";
            Add("v|version", "Display NScumm version information and exit", v => showVersion = v != null);
            Add("h|help", "Display a brief help text and exit", h => showHelp = h != null);
            Add("e|music-driver=", "Select music driver", d => MusicDriver = d);
            Add("list-audio-devices", "List all available audio devices", b => listAudioDevices = b != null);
            Add("b|boot-param=", "Pass number to the boot script (boot param)", (int b) => BootParam = b);
            Add("d|debuglevel=", "Set debug verbosity level", (int lvl) => DebugHelper.DebugLevel = lvl);
            Add("debugflags=", "Enable engine specific debug flags (separated by commas)", d => Switches = d);
            Add("alt-intro", "Use alternative intro for CD versions of Beneath a Steel Sky and Flight of the Amazon Queen", b => ConfigManager.Instance.Set("alt_intro", b != null));
            Add("copy_protection", "Enable copy protection in SCUMM games, when NScumm disables it by default.", b => CopyProtection = b != null);
        }

        public List<string> Parse(string[] arguments)
        {
            List<string> result = null;
            try
            {
                result = base.Parse(arguments);
                if (showVersion)
                {
                    ShowVersion();
                }
                else if (showHelp)
                {
                    Usage();
                }
                else if (listAudioDevices)
                {
                    ListAudioDevices();
                }
                else if (result.Count != 1)
                {
                    Usage();
                }
            }
            catch (ArgumentException)
            {
                Usage();
            }
            catch (OptionException)
            {
                Usage();
            }
            return result ?? new List<string>();
        }

        public static void ShowVersion()
        {
            var filename = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            var asm = Assembly.GetExecutingAssembly();
            var info = new FileInfo(asm.Location);
            Console.WriteLine("{0} {1} ({2:R})", filename, asm.GetName().Version, info.CreationTime);
        }

        public static void ListAudioDevices()
        {
            Console.WriteLine("{0,-10} {1}", "Id", "Description");
            Console.WriteLine("{0,-10} {1}", new string('-', 10), new string('-', 20));
            var plugins = NScumm.Core.Audio.MusicManager.GetPlugins();
            foreach (var plugin in plugins)
            {
                Console.WriteLine("{0,-10} {1}", plugin.Id, plugin.Name);
            }
        }

        public void Usage()
        {
            var filename = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Usage : {0} [OPTIONS]... [FILE]", filename);
            WriteOptionDescriptions(Console.Out);
        }
    }
}

