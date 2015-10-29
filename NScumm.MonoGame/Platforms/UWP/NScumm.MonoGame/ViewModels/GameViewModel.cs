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
            Description = info.Game is GameInfo ? GetDescription((GameInfo)info.Game) : info.Game.Description;
            Platform = info.Game.Platform.ToString();
            Culture = info.Game.Culture.DisplayName;
        }

        private string GetDescription(GameInfo info)
        {
            if (info.Features.HasFlag(GameFeatures.Demo))
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var description = string.Format(loader.GetString("GameInfoDemo"), info.Description);
                return description;
            }
            return info.Description;
        }
    }
}
