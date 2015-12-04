using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace NScumm.Core
{
    public enum SearchOption
    {
        TopDirectoryOnly,
        AllDirectories
    }

    public interface IFileStorage
    {
        IEnumerable<string> EnumerateFiles(string path);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option);

        string Combine(string path1, string path2);

        string GetDirectoryName(string path);

        string GetFileName(string path);

        string GetFileNameWithoutExtension(string path);

        string GetExtension(string path);

        string ChangeExtension(string path, string newExtension);

        bool FileExists(string path);

        bool DirectoryExists(string path);

        Stream OpenFileRead(string path);

        Stream OpenFileWrite(string path);

        byte[] ReadAllBytes(string filename);

        string GetSignature(string path);

        Stream OpenContent(string path);

        // warning: don't remove this or you will have a TypeInitializationException
        XDocument LoadDocument(Stream stream);
    }

    public static class FileStorageExtension
    {
        public static string Combine(this IFileStorage fileStorage, params string[] paths)
        {
            var path = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                path = fileStorage.Combine(path, paths[i]);
            }
            return path;
        }
    }
}