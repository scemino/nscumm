using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Scumm4;

namespace CostumeViewer
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
                if (Info == null || Info.Version != 4)
                {
                    MessageBox.Show("Sorry, this game is not supported.");
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
