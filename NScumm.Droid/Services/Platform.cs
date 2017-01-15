﻿//
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

namespace NScumm.Mobile.Services
{
    public class Platform : IPlatform
    {
        public void LogMessage(LogMessageType type, string format, params object[] args)
        {
			switch (type)
			{
				case LogMessageType.Info:
					Android.Util.Log.Info("nSCUMM", format, args);
					break;
				case LogMessageType.Warning:
					Android.Util.Log.Warn("nSCUMM", format, args);
					break;
				case LogMessageType.Error:
					Android.Util.Log.Error("nSCUMM", format, args);
					break;
				case LogMessageType.Debug:
					Android.Util.Log.Debug("nSCUMM", format, args);
					break;
			}
        }

        public void Sleep(int timeInMs)
        {
            Thread.Sleep(timeInMs);
        }

        public int GetMilliseconds()
        {
            return Environment.TickCount;
        }
    }
}
