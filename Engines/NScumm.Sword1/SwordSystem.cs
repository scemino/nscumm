using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;

namespace NScumm.Sword1
{
    class SwordSystem : ISystem
    {
        public IGraphicsManager GraphicsManager { get; }
        public IInputManager InputManager { get; }
        public ISaveFileManager SaveFileManager { get; }

        public SwordSystem(IGraphicsManager graphicsManager, IInputManager inputManager, ISaveFileManager saveFileManager)
        {
            GraphicsManager = graphicsManager;
            InputManager = inputManager;
            SaveFileManager = saveFileManager;
        }
    }
}