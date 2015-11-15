using System.IO;

namespace NScumm.Core
{
    public interface ISaveFileManager
    {
        Stream OpenForLoading(string fileName);
        Stream OpenForSaving(string fileName, bool compress = true);
    }
}
