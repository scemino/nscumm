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

using Microsoft.Win32;
using Scumm4;
using System;
using System.Windows;
using System.Windows.Shapes;

namespace NScumm
{
    /// <summary>
    /// Interaction logic for MenuDialog.xaml
    /// </summary>
    public partial class MenuDialog : Window
    {
        public ScummEngine Engine { get; set; }

        public MenuDialog()
        {
            InitializeComponent();
        }

        private void OnLoadClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Scumm savegames|*.s*|All Files|*.*";
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = System.IO.Path.Combine(path, @"ScummVM\Saved games");
            dlg.InitialDirectory = path;
            if (dlg.ShowDialog(this) == true)
            {
                this.Engine.Load(dlg.FileName);
                this.Close();
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Scumm savegames|*.s*|All Files|*.*";
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = System.IO.Path.Combine(path, @"ScummVM\Saved games");
            dlg.InitialDirectory = path;
            if (dlg.ShowDialog(this) == true)
            {
                this.Engine.Save(dlg.FileName);
                this.Close();
            }
        }

        private void OnQuitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        
    }
}
