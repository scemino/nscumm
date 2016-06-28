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

namespace NScumm.Core
{
    public static class ServiceLocator
    {
        public static IFileStorage FileStorage { get; set; }

        public static IPlatform Platform { get; set; }

        public static ITraceFactory TraceFatory { get; set; }

        public static ISaveFileManager SaveFileManager { get; set; }

        public static IAudioManager AudioManager { get; set; }
    }

    public static class DebugHelper
    {
        public static int DebugLevel { get; set; }

        public static void Debug(int level, string format, params object[] args)
        {
            if (level > DebugLevel)
                return;
            
            ServiceLocator.Platform.LogMessage( LogMessageType.Debug, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            ServiceLocator.Platform.LogMessage(LogMessageType.Debug, format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            var output = $"WARNING: {string.Format(format,args)} !";
            ServiceLocator.Platform.LogMessage(LogMessageType.Warning, output);
        }
    }
}

