using NScumm.Core.IO;

namespace NScumm.MonoGame.ViewModels
{
    public class GameViewModel
    {
        public string Description { get; private set; }
        public string Platform { get; private set; }
        public string Culture { get; private set; }
        public GameInfo Game { get; private set; }

        public GameViewModel(GameInfo info)
        {
            Game = info;
            Description = GetDescription(info);
            Platform = info.Platform.ToString();
            Culture = info.Culture.DisplayName;
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
