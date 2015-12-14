using System;
using System.IO;
using System.Linq;
using NScumm.Core;

namespace NScumm
{
    public class SaveFileManager: ISaveFileManager
    {
        private IFileStorage _fileStorage;

        public SaveFileManager(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public Stream OpenForLoading(string fileName)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            return File.OpenRead(Path.Combine(path, fileName));
        }

        public Stream OpenForSaving(string fileName, bool compress = true)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            return File.OpenWrite(Path.Combine(path,fileName));
        }

        public string[] ListSavefiles(string pattern)
        {
            var path = GetSavePath();
            EnsureSavePathExists(path);
            return Directory.EnumerateFiles(path, pattern).Select(Path.GetFileName).ToArray();
        }

        private static void EnsureSavePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nScumm",
                "SaveGames");
        }
    }
}
