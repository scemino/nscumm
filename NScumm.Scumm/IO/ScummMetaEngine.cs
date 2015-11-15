using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    public class ScummMetaEngine : IMetaEngine
    {
        private GameManager _gm;

        public ScummMetaEngine()
        {
            var resStream = typeof(ScummMetaEngine).Assembly.GetManifestResourceStream(typeof(ScummMetaEngine), "Nscumm.xml");
            _gm = GameManager.Create(resStream);
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