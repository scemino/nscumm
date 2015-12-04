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

using System.Collections.Generic;
using System.Linq;

namespace NScumm.Core.IO
{
    public class GameDetector
    {
        private List<IMetaEngine> _engines;

        public void Add(IMetaEngine engine)
        {
            if (_engines == null) _engines = new List<IMetaEngine>();
            _engines.Add(engine);
        }

        public GameDetected DetectGame(string path)
        {
            return _engines.Select(e => e.DetectGame(path)).FirstOrDefault(o => o != null && o.Game != null);
        }
    }
}
