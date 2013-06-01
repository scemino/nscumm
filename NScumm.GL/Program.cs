#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core;

#endregion

namespace NScumm.GL
{ 
    static class Program
    {
        private static ScummGame game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main (string[] args)
        {
            GameInfo info = null;
            if (args.Length > 0) {
                var path = args [0];
                if (System.IO.File.Exists (path)) {
                    info = GameManager.GetInfo (path);
                }
            } 

            if (info != null) {
                game = new ScummGame (info);
                game.Run ();
            } else {
                Usage ();
            }
        }

        static void Usage ()
        {
            var filename = System.IO.Path.GetFileNameWithoutExtension (AppDomain.CurrentDomain.FriendlyName);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine ("Usage : {0} [FILE]", filename);
        }

    }
}
