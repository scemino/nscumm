/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using Scumm4;
using System;
using System.Threading;
using System.Windows;

namespace NScumm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private ScummIndex _index;
        private ScummEngine _engine;
        private Thread _thread;
        private TimeSpan tsDelta;
        private TimeSpan delay = TimeSpan.Zero;
        private ManualResetEvent _evtQuit = new ManualResetEvent(false);
        private WpfGraphicsManager _gfx;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // get game
            var info = ((NScumm.App)App.Current).Info;
            this.Title = string.Format("{0} - {1}", info.Description, info.Culture.NativeName);


            // load game
            _index = new ScummIndex();
            _index.LoadIndex(info.Path);

            // create engine
            _gfx = new WpfGraphicsManager(_screen);
            var im = new WpfInputManager(_screen);
            _engine = new ScummEngine(_index, info, _gfx, im);
            _engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;

            // start the game
            _engine.RunBootScript();
            _thread = new Thread(new ThreadStart(Update));
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void Update()
        {
            while (!_engine.HastToQuit)
            {
                var dt = DateTime.Now;
                _engine.Loop(delay);
                var elapsed = DateTime.Now - dt;
                _engine.Update(elapsed);
                delay = _engine.GetTimeToWait();
                _gfx.UpdateScreen();
                Thread.Sleep(delay);
                delay = elapsed;
            }
        }

        private void OnShowMenuDialogRequested(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                MenuDialog dlg = new MenuDialog();
                dlg.Engine = _engine;
                dlg.Owner = this;
                dlg.ShowDialog();
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            _thread.Abort();
            base.OnClosed(e);
        }
    }
}

