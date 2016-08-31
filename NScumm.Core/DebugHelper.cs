//
//  DebugManager.cs
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
using System.Collections.Generic;

namespace NScumm.Core
{
    internal class DebugChannel
    {
        public DebugChannel()
        {
        }

        public DebugChannel(int c, string n, string d)
        {
            name = n;
            description = d;
            channel = c;
        }

        public string name;
        public string description;

        public int channel;
        public bool enabled;
    }

    public class DebugManager
    {
        private readonly Dictionary<string, DebugChannel> gDebugChannels =
            new Dictionary<string, DebugChannel>(StringComparer.OrdinalIgnoreCase);

        private int gDebugChannelsEnabled;

        public static readonly DebugManager Instance = new DebugManager();

        public bool AddDebugChannel(int channel, string name, string description)
        {
            if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
            {
                DebugHelper.Warning("Debug channel 'all' is reserved for internal use");
                return false;
            }

            if (gDebugChannels.ContainsKey(name))
                DebugHelper.Warning("Duplicate declaration of engine debug channel '{0}'", name);

            gDebugChannels[name] = new DebugChannel(channel, name, description);

            return true;
        }

        public void ClearAllDebugChannels()
        {
            gDebugChannelsEnabled = 0;
            gDebugChannels.Clear();
        }

        public bool EnableDebugChannel(string name)
        {
            if (!gDebugChannels.ContainsKey(name)) return false;

            gDebugChannelsEnabled |= gDebugChannels[name].channel;
            gDebugChannels[name].enabled = true;

            return true;
        }

        public bool DisableDebugChannel(string name)
        {
            if (!gDebugChannels.ContainsKey(name)) return false;

            gDebugChannelsEnabled &= ~gDebugChannels[name].channel;
            gDebugChannels[name].enabled = false;

            return true;
        }

        public void EnableAllDebugChannels()
        {
            foreach (var channel in gDebugChannels)
            {
                EnableDebugChannel(channel.Value.name);
            }
        }

        public void DisableAllDebugChannels()
        {
            foreach (var channel in gDebugChannels)
            {
                DisableDebugChannel(channel.Value.name);
            }
        }

        public bool IsDebugChannelEnabled(int channel)
        {
            // Debug level 11 turns on all special debug level messages
            if (DebugHelper.DebugLevel == 11)
                return true;
            return (gDebugChannelsEnabled & channel) != 0;
        }
    }

    public static class DebugHelper
    {
        public static int DebugLevel { get; set; }

        public static void Debug(int level, string format, params object[] args)
        {
            // Debug level 11 turns on all special debug level messages
            if (DebugLevel != 11)
                if (level > DebugLevel)
                    return;

            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format, args);
        }

        public static void DebugC(int level, int debugChannels, string format, params object[] args)
        {
            // Debug level 11 turns on all special debug level messages
            if (DebugLevel != 11)
                if (level > DebugLevel || !DebugManager.Instance.IsDebugChannelEnabled(debugChannels))
                    return;

            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format, args);
        }

        public static void DebugC(int debugChannels, string format, params object[] args)
        {
            // Debug level 11 turns on all special debug level messages
            if (DebugLevel != 11)
                if (!DebugManager.Instance.IsDebugChannelEnabled(debugChannels))
                    return;

            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format, args);
        }

        public static void DebugN(string format, params object[] args)
        {
            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format + Environment.NewLine, args);
        }

        public static void DebugN(int level, string format, params object[] args)
        {
            if (level > DebugLevel)
                return;
            
            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format + Environment.NewLine, args);
        }

        public static void Warning(string format, params object[] args)
        {
            var output = $"WARNING: {string.Format(format, args)} !";
            ServiceLocator.Platform.LogMessage(LogMessageType.Warning, output);
        }

        public static void Error(string format, params object[] args)
        {
            var output = $"Error: {string.Format(format, args)} !";
            ServiceLocator.Platform.LogMessage(LogMessageType.Error, output);
        }
    }
}