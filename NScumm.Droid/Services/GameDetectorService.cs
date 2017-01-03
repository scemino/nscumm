using System;
using NScumm.Agos;
using NScumm.Core.IO;
using NScumm.Queen;
using NScumm.Sci;
using NScumm.Sky;
using NScumm.Sword1;

namespace NScumm.Droid
{
	public class GameDetectorService
	{
		GameDetector _gd;

		public GameDetectorService()
		{
			_gd = new GameDetector();
			_gd.Add(new AgosMetaEngine());
			_gd.Add(new QueenMetaEngine());
			_gd.Add(new SciMetaEngine());
			//gd.Add(new ScummMetaEngine());
			_gd.Add(new SkyMetaEngine());
			_gd.Add(new Sword1MetaEngine());
		}

		public GameDetected DetectGame(string path)
		{
			return _gd.DetectGame(path);
		}
	}
}
