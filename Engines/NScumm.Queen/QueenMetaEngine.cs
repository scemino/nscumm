//
//  MyClass.cs
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
using NScumm.Core.IO;
using NScumm.Core;
using System.Globalization;

namespace NScumm.Queen
{
	class QueenGameDescriptor: IGameDescriptor
	{
		public string Id {
			get {
				return "queen";
			}
		}

		public string Description {
			get {
				return "Flight of the Amazon Queen";
			}
		}

		public System.Globalization.CultureInfo Culture { get; set; }

		public Platform Platform { get; set; }

		public int Width {
			get {
				return 320;
			}
		}

		public int Height {
			get {
				return 200;
			}
		}

		public NScumm.Core.Graphics.PixelFormat PixelFormat {
			get {
				return NScumm.Core.Graphics.PixelFormat.Indexed8;
			}
		}

		public string Path { get; set; }
	}

	public class QueenMetaEngine : IMetaEngine
	{
		public string OriginalCopyright { get { return "Flight of the Amazon Queen (C) John Passfield and Steve Stamatiadis"; } }

		public GameDetected DetectGame (string path)
		{
			var fileName = ServiceLocator.FileStorage.GetFileName (path);
			if (string.Equals (fileName, "queen.1", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals (fileName, "queen.1c", StringComparison.OrdinalIgnoreCase)) {
				using (var dataFile = ServiceLocator.FileStorage.OpenFileRead (path)) {
					DetectedGameVersion version;
					if ((version = Resource.DetectVersion (dataFile)) != null) {
						var game = new QueenGameDescriptor {
							Culture = CultureInfo.CurrentCulture,
							Platform = version.platform,
							Path = path
						};
						// TODO: vs
						return new GameDetected (game, this);
					}
				}
				return null;
			}
			return null;
		}

		public IEngine Create (GameSettings settings, NScumm.Core.Graphics.IGraphicsManager gfxManager, NScumm.Core.Input.IInputManager inputManager, NScumm.Core.Audio.IAudioOutput output, NScumm.Core.ISaveFileManager saveFileManager, bool debugMode = false)
		{
			return new QueenEngine(settings, gfxManager, inputManager, output, saveFileManager, debugMode);
		}
	}
}

