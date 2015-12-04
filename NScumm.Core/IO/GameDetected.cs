namespace NScumm.Core.IO
{
    public class GameDetected
    {
        public GameDetected(IGameDescriptor game, IMetaEngine engine)
        {
            Game = game;
            Engine = engine;
        }

        public IMetaEngine Engine { get; private set; }
        public IGameDescriptor Game { get; private set; }
    }
}