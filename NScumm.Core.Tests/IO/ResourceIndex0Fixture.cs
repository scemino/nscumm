using System;
using System.Collections.Generic;
using System.IO;
using NScumm.Core.IO;
using NUnit.Framework;
using NScumm.Core.Tests.Properties;
using NFluent;
using System.Linq;

namespace NScumm.Core.Tests.IO
{
    [TestFixture]
    public class ResourceIndex0Fixture
    {
        class TestFileStorage : IFileStorage
        {
            public string ChangeExtension(string path, string newExtension)
            {
                throw new NotImplementedException();
            }

            public string Combine(string path1, string path2)
            {
                throw new NotImplementedException();
            }

            public bool DirectoryExists(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFiles(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
            {
                return new string[] { "maniacdemo.d64" };
            }

            public bool FileExists(string path)
            {
                throw new NotImplementedException();
            }

            public string GetDirectoryName(string path)
            {
                return string.Empty;
            }

            public string GetExtension(string path)
            {
                throw new NotImplementedException();
            }

            public string GetFileName(string path)
            {
                return "maniacdemo.d64";
            }

            public string GetFileNameWithoutExtension(string path)
            {
                throw new NotImplementedException();
            }

            public string GetSignature(string path)
            {
                return "942398bfac774bd3f0830ff614b46db9";
            }

            public Stream OpenFileRead(string path)
            {
                return new MemoryStream(Resources.maniacdemo);
            }

            public Stream OpenFileWrite(string path)
            {
                throw new NotImplementedException();
            }

            public byte[] ReadAllBytes(string filename)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ReadResource0()
        {
            var resManager = CreateResourceManager();

            Check.That(resManager.NumArray).IsEqualTo(50);
            Check.That(resManager.NumBitVariables).IsEqualTo(4096);
            Check.That(resManager.NumGlobalScripts).IsEqualTo(200);
            Check.That(resManager.NumInventory).IsEqualTo(80);
            Check.That(resManager.NumLocalObjects).IsEqualTo(200);
            Check.That(resManager.NumVariables).IsEqualTo(800);
            Check.That(resManager.NumVerbs).IsEqualTo(100);
            Check.That(resManager.ObjectRoomTable).IsNull();
            Check.That(resManager.AudioNames).IsEmpty();
            Check.That(resManager.ArrayDefinitions).IsEmpty();
        }

        [Test]
        public void ReadRoomResource()
        {
            var resManager = CreateResourceManager();
            var room = resManager.GetRoom(1);
            
            Check.That(room.Name).IsNull();
            Check.That(room.Number).IsEqualTo(1);
            Check.That(room.NumZBuffer).IsEqualTo(0);

            Check.That(room.Header.Width).IsEqualTo(1280);
            Check.That(room.Header.Height).IsEqualTo(136);
            Check.That(room.Header.NumObjects).IsEqualTo(0);
            Check.That(room.Header.NumZBuffer).IsEqualTo(0);
            Check.That(room.Header.Transparency).IsEqualTo(0);

            Check.That(room.BoxMatrix).ContainsExactly(new byte[] { 0, 0, 0, 0, 1, 255, 2, 0, 255, 5, 1, 255, 4, 255, 5, 3, 255, 6, 2, 4, 255, 7, 5, 255, 6, 8, 9, 255, 7, 255, 7, 255 });
            Check.That(room.Boxes.Count).IsEqualTo(10);

            Check.That(room.EntryScript.Data).ContainsExactly(new byte[] { 112, 2, 66, 20, 0 });
            Check.That(room.EntryScript.Offset).IsEqualTo(0);

            Check.That(room.ExitScript.Data).IsEmpty();
            Check.That(room.ExitScript.Offset).IsEqualTo(0);

            var names = room.Objects.Select(o => System.Text.Encoding.UTF8.GetString(o.Name)).ToArray();
            Check.That(names).ContainsExactly(new string[] { "doorbell", "front door", "key", "door mat", "stamps", "package", "envelope", "contract", "flag", "mailbox", "mailbox", "grating", "bushes", "undeveloped film", "bushes", "tombstone", "tombstone", "tombstone", "" });
        }

        private static ResourceManager CreateResourceManager()
        {
            ServiceLocator.FileStorage = new TestFileStorage();

            var gmStream = typeof(GameManager).Assembly.GetManifestResourceStream(typeof(GameManager), "Nscumm.xml");
            var gm = GameManager.Create(gmStream);
            var game = gm.GetInfo("maniacdemo.d64");

            var resManager = ResourceManager.Load(game);
            return resManager;
        }
    }
}
