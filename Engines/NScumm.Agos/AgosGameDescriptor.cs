//
//  AgosGameDescriptor.cs
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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;

namespace NScumm.Agos
{
	class AgosGameDescriptor : IGameDescriptor
	{
		public AgosGameDescriptor(string path, AgosGameDescription desc)
		{
			ADGameDescription = desc;
			Path = path;
			Id = desc.gameid;
			Language = desc.language;
			Platform = desc.platform;

			switch (Id)
			{
				case "feeble":
					Description = "The Feeble Files";
					break;
				case "elvira1":
					Description = "Elvira - Mistress of the Dark";
					break;
				case "elvira2":
					Description = "Elvira II - The Jaws of Cerberus";
					break;
				case "waxworks":
					Description = "Waxworks";
					break;
				case "simon1":
					Description = "Simon the Sorcerer 1";
					break;
				case "simon2":
					Description = "Simon the Sorcerer 2";
					break;
			}
		}

		public AgosGameDescription ADGameDescription { get; }

		public string Description { get; private set; }

		public int Height => ADGameDescription.gameType == SIMONGameType.GType_FF ? 480 : 200;

		public string Id
		{
			get; private set;
		}

		public Language Language
		{
			get; private set;
		}

		public string Path
		{
			get; private set;
		}

		public PixelFormat PixelFormat
		{
			get; private set;
		}

		public Platform Platform
		{
			get; private set;
		}

		public int Width => ADGameDescription.gameType == SIMONGameType.GType_FF ? 640 : 320;
	}
}