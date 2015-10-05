using NScumm.Core.IO;
using System.Text;

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
            var sbDescription = new StringBuilder(info.Description);
            if (info.Features.HasFlag(GameFeatures.Demo))
            {
                sbDescription.Append(" (Demo)");
            }
            return sbDescription.ToString();
        }
    }
}
