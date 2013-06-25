
namespace NScumm.GL
{
    sealed class XnaInputManager : NScumm.Core.Input.IInputManager
    {
        Microsoft.Xna.Framework.GameWindow _window;

        public XnaInputManager (Microsoft.Xna.Framework.GameWindow window)
        {
            _window = window;
        }

        public NScumm.Core.Graphics.Point GetMousePosition ()
        {
            var state = Microsoft.Xna.Framework.Input.Mouse.GetState ();
            int x = state.X;
            int y = state.Y;

            double scaleX = 320.0 / _window.ClientBounds.Width;
            double scaleY = 200.0 / _window.ClientBounds.Height;

            var pOut = new NScumm.Core.Graphics.Point ((short)(x * scaleX), (short)(y * scaleY));
            return pOut;
        }

        public bool IsKeyDown (NScumm.Core.KeyCode code)
        {
            //var state = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            var keyState = Microsoft.Xna.Framework.Input.Keyboard.GetState (Microsoft.Xna.Framework.PlayerIndex.One);
            if (code >= NScumm.Core.KeyCode.A && code <= NScumm.Core.KeyCode.Z) {
                return /*state.IsButtonDown(code - Scumm4.KeyCode.A + Microsoft.Xna.Framework.Input.Buttons.A) ||*/
                    keyState.IsKeyDown (code - NScumm.Core.KeyCode.A + Microsoft.Xna.Framework.Input.Keys.A);
            }
            if (code == NScumm.Core.KeyCode.Escape) {
                return /*state.IsButtonDown(Microsoft.Xna.Framework.Input.Buttons.Back) ||*/
                    keyState.IsKeyDown (Microsoft.Xna.Framework.Input.Keys.Escape);
            }
            if (code >= NScumm.Core.KeyCode.F1 && code <= NScumm.Core.KeyCode.F9) {
                return keyState.IsKeyDown (code - NScumm.Core.KeyCode.F1 + Microsoft.Xna.Framework.Input.Keys.F1);
            }
            return false;

        }

        public bool IsMouseLeftPressed ()
        {
            return Microsoft.Xna.Framework.Input.Mouse.GetState ().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        }

        public bool IsMouseRightPressed ()
        {
            return Microsoft.Xna.Framework.Input.Mouse.GetState ().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        }
    }
}
