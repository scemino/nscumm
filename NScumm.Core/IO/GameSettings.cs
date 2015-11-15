namespace NScumm.Core.IO
{
    public class GameSettings
    {
        public IGameDescriptor Game { get; private set; }

        public IMetaEngine MetaEngine { get; private set; }

        public string AudioDevice { get; set; }

        public bool CopyProtection { get; set; }

        public int BootParam { get; set; }

        public GameSettings(IGameDescriptor game, IMetaEngine engine)
        {
            Game = game;
            AudioDevice = "adlib";
            MetaEngine = engine;
        }
    }
}