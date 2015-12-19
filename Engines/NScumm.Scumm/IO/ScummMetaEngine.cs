using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    public class ScummMetaEngine : IMetaEngine
    {
        private readonly GameManager _gm;

        public ScummMetaEngine()
            : this(ServiceLocator.FileStorage.OpenContent("Nscumm.xml"))
        {
        }

        public ScummMetaEngine(Stream stream)
        {
            _gm = GameManager.Create(stream);
        }

        public IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false)
        {
            return ScummEngine.Create(settings, gfxManager, inputManager, output, debugMode);
        }

        public GameDetected DetectGame(string path)
        {
            return new GameDetected(_gm.GetInfo(path), this);
        }
    }
}