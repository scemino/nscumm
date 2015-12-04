using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;

namespace NScumm.Core.IO
{
    public interface IMetaEngine
    {
        GameDetected DetectGame(string path);

        IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false);
    }
}