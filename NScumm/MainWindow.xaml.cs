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
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            var info = ((NScumm.App)App.Current).Info;
            this.Title = string.Format("{0} - {1}", info.Description, info.Culture.NativeName);

            _index = new ScummIndex();
            _index.LoadIndex(info.Path);
            
            var gfx = new WpfGraphicsManager(_screen);
            _engine = new ScummEngine(_index, gfx);
            _engine.ShowMenuDialogRequested += OnShowMenuDialogRequested;

            _thread = new Thread(new ThreadStart(() =>
            {
                _engine.Go();
            }));
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void OnShowMenuDialogRequested(object sender, EventArgs e)
        {
            MenuDialog dlg = new MenuDialog();
            dlg.Engine = _engine;
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _engine.HastToQuit = true;
            base.OnClosed(e);
        }
    }
}

