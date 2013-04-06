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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Scumm4;

namespace NScumm
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public GameInfo Info { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string filename = GetGameFileName(e.Args);
            if (filename != null)
            {
                // check game
                Info = GameManager.GetInfo(filename);
                if (Info == null)
                {
                    MessageBox.Show("Sorry, this game is not supported.", string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
                    Shutdown();
                }
                else if (Info.Version != 4)
                {
                    MessageBox.Show(string.Format("Sorry, the game '{0}' is not supported.", Info.Description), string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
                    Shutdown();
                }
                else
                {
                    // start game
                    var win = new MainWindow();
                    win.Show();
                }
            }
            else
            {
                Shutdown();
            }
        }

        private static string GetGameFileName(string[] args)
        {
            string filename = null;
            if (args.Length < 1)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "Scumm index file|*.lfl|All Files|*.*";
                if (dlg.ShowDialog() == true)
                {
                    filename = dlg.FileName;
                }
            }
            else
            {
                filename = args[0];
            }
            return filename;
        }
    }
}
