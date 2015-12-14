using NScumm.Core.Graphics;
using NScumm.Core.Input;

namespace NScumm.Core
{
    public interface ISystem
    {
        IGraphicsManager GraphicsManager { get; }
        IInputManager InputManager { get; }
        ISaveFileManager SaveFileManager { get; }
    }
}