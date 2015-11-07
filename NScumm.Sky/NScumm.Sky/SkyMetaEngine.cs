using NScumm.Core;
using NScumm.Core.IO;
using System;
using System.Globalization;
using NScumm.Core.Graphics;
using NScumm.Core.Audio;
using NScumm.Core.Input;

namespace NScumm.Sky
{
    class SkyGameDescriptor : IGameDescriptor
    {
        private readonly CultureInfo _culture;
        private string _path;

        public string Description
        {
            get
            {
                return "Beneath a Steel Sky";
            }
        }

        public string Id
        {
            get
            {
                return "sky";
            }
        }

        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }

        public Platform Platform
        {
            get
            {
                return Platform.Unknown;
            }
        }

        public int Width
        {
            get
            {
                return 320;
            }
        }

        public int Height
        {
            get
            {
                return 200;
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {
                return PixelFormat.Indexed8;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public SkyGameDescriptor(string path)
        {
            _path = path;
            // The game detector uses US English by default. We want British
            // English to match the recorded voices better.
            _culture = new CultureInfo("en-GB");
        }
    }

    public class SkyMetaEngine : IMetaEngine
    {
        public IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false)
        {
            return new SkyEngine(settings, gfxManager, inputManager, output, saveFileManager, debugMode);
        }

        public GameDetected DetectGame(string path)
        {
            var fileName = ServiceLocator.FileStorage.GetFileName(path);
            if (string.Equals(fileName, "sky.dnr", StringComparison.OrdinalIgnoreCase))
            {
                var directory = ServiceLocator.FileStorage.GetDirectoryName(path);
                using (var disk = new Disk(directory))
                {
                    var version = disk.DetermineGameVersion();
                    return new GameDetected(new SkyGameDescriptor(path), this);
                }
            }
            return null;
        }
    }
}
