//
//  TrsFile.cs
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
using System.IO;
using NScumm.Core.Audio;
using System.Threading;
using NScumm.Core.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace NScumm.Core.Smush
{
    class TrsFile
    {
        static readonly Regex DefRegex = new Regex(@"#define\s+(\S+)\s+(\d+)", RegexOptions.Singleline);
        static readonly Regex TextRegex = new Regex(@"//(.+)", RegexOptions.Singleline);
        IDictionary<int, string> _texts;

        private TrsFile(IDictionary<int, string> texts)
        {
            if (texts == null)
                throw new ArgumentNullException("texts");
            _texts = texts;
        }

        public static TrsFile Load(string filename)
        {
            var texts = new Dictionary<int, string>();
            using (var f = new StreamReader(filename, Encoding.GetEncoding("iso-8859-1")))
            {
                string line;
                while ((line = f.ReadLine()) != null)
                {
                    var m = TrsFile.DefRegex.Match(line);
                    if (m.Success)
                    {
                        var id = int.Parse(m.Groups[2].Value);
                        line = f.ReadLine();
                        var m2 = TrsFile.TextRegex.Match(line);
                        texts.Add(id, m2.Groups[1].Value);
                    }
                }
            }
            return new TrsFile(texts);
        }

        public string this [int id]
        {
            get
            {
                if (_texts.ContainsKey(id))
                {
                    return _texts[id];
                }
                return string.Empty;
            }
        }
    }
}
