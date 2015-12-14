using System;
using System.Globalization;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    enum SwordGameId
    {
        Sword1,
        Sword1Demo,
        Sword1Mac,
        Sword1MacDemo,
        Sword1Psx,
        Sword1PsxDemo
    }

    class SwordGameDescriptor : IGameDescriptor
    {
        private readonly CultureInfo _culture;
        private readonly string _path;

        public string Description
        {
            get;
        }

        public SwordGameId GameId { get; private set; }

        public string Id
        {
            get;
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
            get; private set;
        }

        public int Width
        {
            get
            {
                return 640;
            }
        }

        public int Height
        {
            get
            {
                return 480;
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

        public SwordGameDescriptor(string path, SwordGameId gameId)
        {
            _path = path;
            GameId = gameId;
            Id = gameId.ToString().ToLowerInvariant();
            // The game detector uses US English by default. We want British
            // English to match the recorded voices better.
            _culture = new CultureInfo("en-GB");
            switch (gameId)
            {
                case SwordGameId.Sword1:
                    Description = "Broken Sword: The Shadow of the Templars";
                    Platform = Platform.Windows;
                    break;
                case SwordGameId.Sword1Demo:
                    Description = "Broken Sword: The Shadow of the Templars(Demo)";
                    Platform = Platform.Windows;
                    break;
                case SwordGameId.Sword1Mac:
                    Description = "Broken Sword: The Shadow of the Templars (Mac)";
                    Platform = Platform.Macintosh;
                    break;
                case SwordGameId.Sword1MacDemo:
                    Description = "Broken Sword: The Shadow of the Templars (Mac demo)";
                    Platform = Platform.Macintosh;
                    break;
                case SwordGameId.Sword1Psx:
                    Description = "Broken Sword: The Shadow of the Templars (PlayStation)";
                    Platform = Platform.PSX;
                    break;
                case SwordGameId.Sword1PsxDemo:
                    Description = "Broken Sword: The Shadow of the Templars (PlayStation demo)";
                    Platform = Platform.PSX;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gameId));
            }
        }
    }

    class Sword1MetaEngine : IMetaEngine
    {
        public IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false)
        {
            return new SwordEngine(settings, gfxManager, inputManager, output, saveFileManager, debugMode);
        }

        public GameDetected DetectGame(string path)
        {
            var fileName = ServiceLocator.FileStorage.GetFileName(path);
            if (string.Equals(fileName, "swordres.rif", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: check in subdirectories too
                var directory = ServiceLocator.FileStorage.GetDirectoryName(path);
                var filesFound = new bool[_filesToCheck.Length];
                int i;
                for (i = 0; i < _filesToCheck.Length; i++)
                {
                    filesFound[i] = ServiceLocator.FileStorage.FileExists(ServiceLocator.FileStorage.Combine(directory, _filesToCheck[i]));
                }

                bool mainFilesFound = true;
                bool pcFilesFound = true;
                bool macFilesFound = true;
                bool demoFilesFound = true;
                bool macDemoFilesFound = true;
                bool psxFilesFound = true;
                bool psxDemoFilesFound = true;
                for (i = 0; i < NUM_COMMON_FILES_TO_CHECK; i++)
                    if (!filesFound[i])
                        mainFilesFound = false;
                for (var j = 0; j < NUM_PC_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i])
                        pcFilesFound = false;
                for (var j = 0; j < NUM_MAC_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i])
                        macFilesFound = false;
                for (var j = 0; j < NUM_DEMO_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i])
                        demoFilesFound = false;
                for (var j = 0; j < NUM_DEMO_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i])
                        macDemoFilesFound = false;
                for (var j = 0; j < NUM_PSX_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i])
                        psxFilesFound = false;
                for (var j = 0; j < NUM_PSX_DEMO_FILES_TO_CHECK; i++, j++)
                    if (!filesFound[i] || psxFilesFound)
                        psxDemoFilesFound = false;

                SwordGameId? gameId = null;
                if (mainFilesFound && pcFilesFound && demoFilesFound)
                    gameId = SwordGameId.Sword1Demo;
                else if (mainFilesFound && pcFilesFound && psxFilesFound)
                    gameId = SwordGameId.Sword1Psx;
                else if (mainFilesFound && pcFilesFound && psxDemoFilesFound)
                    gameId = SwordGameId.Sword1PsxDemo;
                else if (mainFilesFound && pcFilesFound)
                    gameId = SwordGameId.Sword1;
                else if (mainFilesFound && macFilesFound)
                    gameId = SwordGameId.Sword1Mac;
                else if (mainFilesFound && macDemoFilesFound)
                    gameId = SwordGameId.Sword1MacDemo;

                return gameId.HasValue ? new GameDetected(new SwordGameDescriptor(path, gameId.Value), this) : null;
            }
            return null;
        }

        const int NUM_COMMON_FILES_TO_CHECK = 1;
        const int NUM_PC_FILES_TO_CHECK = 3;
        const int NUM_MAC_FILES_TO_CHECK = 4;
        const int NUM_PSX_FILES_TO_CHECK = 1;
        const int NUM_PSX_DEMO_FILES_TO_CHECK = 2;
        const int NUM_DEMO_FILES_TO_CHECK = 1;
        const int NUM_MAC_DEMO_FILES_TO_CHECK = 1;
        const int NUM_FILES_TO_CHECK = NUM_COMMON_FILES_TO_CHECK + NUM_PC_FILES_TO_CHECK + NUM_MAC_FILES_TO_CHECK + NUM_PSX_FILES_TO_CHECK + NUM_DEMO_FILES_TO_CHECK + NUM_MAC_DEMO_FILES_TO_CHECK + NUM_PSX_DEMO_FILES_TO_CHECK;

        static readonly string[] _filesToCheck =
        {
            // these files have to be found
            "swordres.rif", // Mac, PC and PSX version
            "general.clu", // PC and PSX version
            "compacts.clu", // PC and PSX version
            "scripts.clu", // PC and PSX version
            "general.clm", // Mac version only
            "compacts.clm", // Mac version only
            "scripts.clm", // Mac version only
            "paris2.clm", // Mac version (full game only)
            "cows.mad", // this one should only exist in the demo version
            "scripts.clm", // Mac version both demo and full game
            "train.plx", // PSX version only
            "speech.dat", // PSX version only
            "tunes.dat", // PSX version only
            // the engine needs several more files to work, but checking these should be sufficient
        };
    }
}
