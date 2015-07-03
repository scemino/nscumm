using NFluent;
using NScumm.Core.IO;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace NScumm.Core.Tests
{
    [TestFixture]
    public class XorStreamFixture
    {
        [Test]
        public void ReadBytes()
        {
            var data = Enumerable.Range(0, 256).Select(d => (byte)d).ToArray();

            byte encByte = 0x73;
            var dataExpected = data.Select(d => d ^ encByte).Select(d => (byte)d).ToArray();
            using (var ms = new MemoryStream(data))
            {
                var stream = new XorStream(ms, encByte);
                var output = new byte[256];
                stream.Read(output, 0, 256);
                Check.That(output).ContainsExactly(dataExpected);
            }
        }
    }
}
