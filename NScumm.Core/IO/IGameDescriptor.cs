using System.Globalization;
using NScumm.Core.Graphics;

namespace NScumm.Core.IO
{
    public interface IGameDescriptor
    {
        string Id { get; }
        string Description { get; }
        CultureInfo Culture { get; }
        Platform Platform { get; }
        int Width { get; }
        int Height { get; }
        PixelFormat PixelFormat { get; }
        string Path { get; }
    }
}