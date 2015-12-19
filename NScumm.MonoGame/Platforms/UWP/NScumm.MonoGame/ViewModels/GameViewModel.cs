using NScumm.Core.IO;

namespace NScumm.MonoGame.ViewModels
{
    public class GameViewModel
    {
        public string Description { get; private set; }
        public string Platform { get; private set; }
        public string Culture { get; private set; }
        public GameDetected Game { get; private set; }

        public GameViewModel(GameDetected info)
        {
            Game = info;
            Description = info.Game.Description;
            Platform = info.Game.Platform.ToString();
            Culture = info.Game.Culture.DisplayName;
        }
    }
}
