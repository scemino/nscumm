using NScumm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Windows.Storage;
using System.Text.RegularExpressions;

namespace NScumm
{
    public class FileStorage : IFileStorage, IEnableTrace
    {
        public string ChangeExtension(string path, string newExtension)
        {
            return Path.ChangeExtension(path, newExtension);
        }

        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            var folder = StorageFolder.GetFolderFromPathAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var items = folder.GetFilesAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            return items.Select(item => item.Path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            var regex = new Regex(WildcardToRegex(searchPattern));
            return EnumerateFiles(path).Where(f => regex.IsMatch(Path.GetFileName(f)));
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, Core.SearchOption option)
        {
            if (option == Core.SearchOption.TopDirectoryOnly)
            {
                return EnumerateFiles(path, searchPattern);
            }
            else
            {
                var folder = StorageFolder.GetFolderFromPathAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                var folders = folder.GetFoldersAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult().Select(f => f.Path);
                var allFolders = folders.Concat(new string[] { folder.Path });
                return allFolders.SelectMany(f => EnumerateFiles(f, searchPattern));
            }
        }

        public bool FileExists(string path)
        {
            return EnumerateFiles(GetDirectoryName(path)).Any(f => StringComparer.OrdinalIgnoreCase.Equals(f, path));
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public string GetSignature(string path)
        {
            string signature;
            using (var md5 = MD5.Create())
            {
                using (var file = OpenFileRead(path))
                {
                    var br = new BinaryReader(file);
                    var data = br.ReadBytes(1024 * 1024);
                    var md5Key = md5.ComputeHash(data, 0, data.Length);
                    var md5Text = new StringBuilder();
                    for (int i = 0; i < 16; i++)
                    {
                        md5Text.AppendFormat("{0:x2}", md5Key[i]);
                    }
                    signature = md5Text.ToString();
                }
            }
            return signature;
        }

        public Stream OpenFileRead(string path)
        {
            this.Trace().Write("IO", "Read {0}", path);
            var file = StorageFile.GetFileFromPathAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var stream = file.OpenStreamForReadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return stream;
        }

        public Stream OpenFileWrite(string path)
        {
            this.Trace().Write("IO", "Write {0}", path);
            if (!FileExists(path))
            {
                var dir = Path.GetDirectoryName(path);
                var folder = StorageFolder.GetFolderFromPathAsync(dir).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                folder.CreateFileAsync(Path.GetFileName(path)).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            var file = StorageFile.GetFileFromPathAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                
            var stream = file.OpenStreamForWriteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return stream;
        }

        public byte[] ReadAllBytes(string filename)
        {
            var file = StorageFile.GetFileFromPathAsync(filename).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var r = file.OpenReadAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var size = r.Size;
            var buffer = new byte[size];
            r.AsStream().Read(buffer, 0, (int)size);
            return buffer;
        }

        static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }
    }
}
