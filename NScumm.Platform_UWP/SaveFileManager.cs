using NScumm.Core;
using System;
using System.IO;
using Windows.Storage;

namespace NScumm.Platform_UWP
{
    public class SaveFileManager : ISaveFileManager
    {
        private IFileStorage _fileStorage;

        public SaveFileManager(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public Stream OpenForLoading(string fileName)
        {
            var path = GetSavePath();
            return File.OpenRead(Path.Combine(path, fileName));
        }

        public Stream OpenForSaving(string fileName, bool compress = true)
        {
            var path = GetSavePath();
            return File.OpenWrite(Path.Combine(path, fileName));
        }

        private string GetSavePath()
        {
            return ApplicationData.Current.RoamingFolder.Path;
        }
    }
}
