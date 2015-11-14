using System;

namespace NScumm.Core
{
    public interface IEngine
    {
        event EventHandler ShowMenuDialogRequested;
        bool HasToQuit { get; set; }
        bool IsPaused { get; set; }

        void Run();

        // TODO: remove this
        void Load(string filename);
        void Save(string filename);
    }
}