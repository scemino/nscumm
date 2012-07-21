using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Scumm4;
using System.Threading;

namespace CostumeViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private ScummIndex _index;
        private ScummInterpreter _interpreter;
        private ImageDecoder _imgDecoder;
        private Thread _thread;
        public byte[] _pixels;
        private WriteableBitmap bmp;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //this.Width = 200;
            //this.Height = 100;
            //this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            this.Top += 800;

            _pixels = new byte[320 * 200 * 3];
            bmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Rgb24, null);
            _screen.Source = bmp;

            _index = new ScummIndex();
            //_index.LoadIndex(@"C:\Users\vsab\Documents\Visual Studio 2010\Projects\NScumm\monkey1vga\000.lfl");
            _index.LoadIndex(@"E:\Program Files (x86)\ScummVM\Games\monkey1vga\000.lfl");
            _index.GetCharset(4);

            _interpreter = new ScummInterpreter(_index, _pixels);

            _imgDecoder = new ImageDecoder(_pixels);
            _thread = new Thread(new ThreadStart(() =>
            {
                Run();
            }));
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void Run()
        {
            long diff = 0;
            _interpreter.RunBootScript();
            do
            {
                int delta = _interpreter.Variables[ScummInterpreter.VariableTimerNext];
                if (delta < 1)	// Ensure we don't get into an endless loop
                    delta = 1;  // by not decreasing sleepers.
                var delay = (int)(delta * 1000 / 60 - diff);
                if (delay > 0) System.Threading.Thread.Sleep(delay);
                ProcessInputs();

                var watch = System.Diagnostics.Stopwatch.StartNew();

                _interpreter.Variables[ScummInterpreter.VariableTimer1] += delta;
                _interpreter.Variables[ScummInterpreter.VariableTimer2] += delta;
                _interpreter.Variables[ScummInterpreter.VariableTimer3] += delta;

                if (delta > 15)
                    delta = 15;
                _interpreter.DecreaseScriptDelay(delta);
                _interpreter.UpdateVariables();
                _interpreter.RunAllScripts();
                if (_interpreter.CurrentRoom != 0)
                {
                    _interpreter.CheckExecVerbs();
                    _interpreter.CheckAndRunSentenceScript();
                    _interpreter.WalkActors();
                    _interpreter.MoveCamera();
                    _interpreter.UpdateObjectStates();
                    DrawRoom();
                    DrawObjects();
                    _interpreter.DrawCharset();
                    DrawActors();
                    _interpreter.RedrawVerbs();
                    _interpreter.DrawCursor();
                    _interpreter.AnimateCursor();
                }
                RefreshScreen();
                diff = watch.ElapsedMilliseconds;
                _interpreter.Camera._last = _interpreter.Camera._cur;
            } while (true);
        }

        private void ProcessInputs()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                _interpreter.MouseAndKeyboardStat = 0;
                if (Keyboard.IsKeyDown(Key.Escape))
                {
                    _interpreter.MouseAndKeyboardStat = (KeyCode)_interpreter.Variables[ScummInterpreter.VariableCutSceneExitKey];
                    _interpreter.AbortCutscene();
                }
                for (Key i = Key.A; i <= Key.Z; i++)
                {
                    if (Keyboard.IsKeyDown(i))
                    {
                        _interpreter.MouseAndKeyboardStat = (KeyCode)(i - Key.A + KeyCode.A);
                    }
                }


                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    _interpreter.MouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.MBS_LEFT_CLICK;
                }

                if (Mouse.RightButton == MouseButtonState.Pressed)
                {
                    _interpreter.MouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.MBS_RIGHT_CLICK;
                }

                var w = this._screen.ActualWidth;
                var h = this._screen.ActualHeight;
                var pos = Mouse.GetPosition(this._screen);
                if (pos.X > 0 && pos.Y > 0)
                {
                    var mouseX1 = (pos.X * 320.0) / w;
                    var mouseX = (_interpreter.ScreenStartStrip * 8) + mouseX1;
                    var mouseY = pos.Y * 200.0 / h;
                    _interpreter.Variables[ScummInterpreter.VariableMouseX] = (int)mouseX1;
                    _interpreter.Variables[ScummInterpreter.VariableMouseY] = (int)mouseY;
                    _interpreter.Variables[ScummInterpreter.VariableVirtualMouseX] = (int)mouseX;
                    _interpreter.Variables[ScummInterpreter.VariableVirtualMouseY] = (int)mouseY;
                }
            }));
        }

        private static int NewDirToOldDir(int dir)
        {
            if (dir >= 71 && dir <= 109)
                return 1;
            if (dir >= 109 && dir <= 251)
                return 2;
            if (dir >= 251 && dir <= 289)
                return 0;
            return 3;
        }

        private void DrawActors()
        {
            var numStrips = _interpreter.CurrentRoomData.Header.Width / 8;
            var maskActivated = _interpreter.CurrentRoomData != null && _interpreter.CurrentRoomData.ZPlanes.Count > 0;
            var maskData = maskActivated ? _interpreter.CurrentRoomData.ZPlanes[0] : null;
            // keep only actors of the current room
            var actors = from actor in _interpreter.Actors
                         where actor._room == _interpreter.CurrentRoom
                         where actor._visible
                         orderby actor.GetPos().y
                         select actor;
            foreach (var actor in actors)
            {
                var aPos = new Scumm4.Point((short)(actor.GetPos().x - _interpreter.ScreenStartStrip * 8), actor.GetPos().y);
                var maskX = actor.GetPos().x;
                var anim = NewDirToOldDir(actor.GetFacing()) + actor._frame * 4;
                var costume = _index.GetCostume((byte)actor._costume);
                var limbs = from limb in costume.Animations[anim].Limbs
                            where limb != null
                            select limb;
                int limbIndex = 0;
                foreach (var limb in limbs)
                {
                    int pictIndex = (actor._cost.curpos[limbIndex] < limb.Pictures.Count) ? actor._cost.curpos[limbIndex] : limb.Pictures.Count - 1;
                    if (pictIndex >= 0)
                    {
                        var pict = limb.Pictures[pictIndex];
                        var l_curX = pict.MoveX + pict.RelX;
                        var l_curY = pict.MoveY + pict.RelY;
                        for (int h = 0; h < pict.Height; h++)
                        {
                            int w2 = pict.Mirror && (anim % 4 == 0) ? pict.Width - 1 : 0;
                            int w2inc = pict.Mirror && (anim % 4 == 0) ? -1 : 1;

                            for (int w = 0; w < pict.Width; w++, w2 += w2inc)
                            {
                                var colorIndex = pict.Data[h * pict.Width + w];
                                if (colorIndex != 0)
                                {
                                    var color = _interpreter.CurrentRoomData.Palette.Colors[costume.Palette[colorIndex]];
                                    var tmpX = aPos.x + l_curX + w2;
                                    var tmpY = aPos.y + pict.RelY + h;
                                    var mX = maskX + l_curX + w2;
                                    if (tmpX < 0 || tmpY < 0 || tmpX >= 320 || tmpY >= _interpreter.CurrentRoomData.Header.Height) continue;
                                    var pos = tmpY * 320 * 3 + tmpX * 3;
                                    bool mask = false;
                                    if (actor._forceClip != 0)
                                    {
                                        mask = maskData != null && (maskData[tmpY * numStrips + (mX / 8)] & (0x80 >> (mX % 8))) != 0;
                                    }
                                    else if (actor.IsInClass(ObjectClass.NeverClip))
                                    {

                                    }
                                    else if (actor._walkbox == 0xFF || _interpreter.CurrentRoomData.Boxes[actor._walkbox].mask != 0)
                                    {
                                        mask = maskData != null && (maskData[tmpY * numStrips + (mX / 8)] & (0x80 >> (mX % 8))) != 0;
                                    }

                                    if (mask == false)
                                    {
                                        _pixels[pos++] = color.R;
                                        _pixels[pos++] = color.G;
                                        _pixels[pos++] = color.B;
                                    }
                                    //else
                                    //{
                                    //    _pixels[pos++] = 0xFF;
                                    //    _pixels[pos++] = 0;
                                    //    _pixels[pos++] = 0;
                                    //}
                                }
                            }
                        }
                        actor._cost.curpos[limbIndex]++;
                        // loop ?
                        if (limb.NoLoop == false && actor._cost.curpos[limbIndex] >= limb.Pictures.Count)
                        {
                            actor._cost.curpos[limbIndex] = 0;
                        }
                    }
                    limbIndex++;
                }
            }
        }

        private void RefreshScreen()
        {
            this._screen.Dispatcher.Invoke(new Action(() =>
            {
                bmp.WritePixels(new Int32Rect(0, 0, 320, 200), _pixels, 320 * 3, 0);
            }));
        }

        private void DrawRoom()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
            if (_interpreter.CurrentRoomData != null)
            {
                _imgDecoder.Decode(_interpreter.CurrentRoomData.Strips, 
                    _interpreter.CurrentRoomData.Palette, new Scumm4.Point(), _interpreter.ScreenStartStrip,
                    320, _interpreter.CurrentRoomData.Header.Height, _interpreter.CurrentRoomData.Header.Height);
            }
        }

        private void DrawObjects()
        {
            foreach (var obj in _interpreter.DrawingObjects)
            {
                DrawObject(obj);
            }

            for (int i = (_interpreter.Objects.Count - 1); i >= 0; i--)
            {
                if (_interpreter.Objects[i].obj_nr > 0 && (_interpreter.Objects[i].state & 0xF) != 0)
                {
                    DrawObject(_interpreter.Objects[i]);
                }
            }
        }

        private void DrawObject(ObjectData obj)
        {
            if (obj != null)
            {
                short xPos = (short)(obj.x_pos - _interpreter.ScreenStartStrip * 8);
                _imgDecoder.Decode(obj.Strips, _interpreter.CurrentRoomData.Palette, new Scumm4.Point(xPos, obj.y_pos), 0, 320, _interpreter.CurrentRoomData.Header.Height, obj.height);
            }
        }
    }
}

