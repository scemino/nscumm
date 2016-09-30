//
//  Platform.cs
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
using System.Threading;
using NScumm.Core;

namespace NScumm.Droid.Services
{
    internal class DroidPlatform : IPlatform
    {
        public void LogMessage(LogMessageType type, string format, params object[] args)
        {
            switch (type)
            {
                case LogMessageType.Debug:
                    Android.Util.Log.Debug(MainActivity.LogTag, format, args);
                    break;
                    
                case LogMessageType.Error:
                    Android.Util.Log.Error(MainActivity.LogTag, format, args);
                    break;
                    
                case LogMessageType.Info:
                    Android.Util.Log.Info(MainActivity.LogTag, format, args);
                    break;
                    
                case LogMessageType.Warning:
                    Android.Util.Log.Warn(MainActivity.LogTag, format, args);
                    break;
            }

        }

        public int GetMilliseconds()
        {
            return Environment.TickCount;
        }

        public void Sleep(int timeInMs)
        {
            Thread.Sleep(timeInMs);
        }
	}
}

