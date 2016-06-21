using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    public class GameManager
    {
        XDocument _doc;
        static readonly XNamespace Namespace = "http://schemas.scemino.com/nscumm/2012/";

        public static GameManager Create(Stream stream)
        {
            var gm = new GameManager { _doc = ServiceLocator.FileStorage.LoadDocument(stream) };
            return gm;
        }

        public GameInfo GetInfo(string path)
        {
            GameInfo info = null;
            var signature = ServiceLocator.FileStorage.GetSignature(path);
            var gameMd5 = (from md5 in _doc.Element(Namespace + "NScumm").Elements(Namespace + "MD5")
                           where (string)md5.Attribute("signature") == signature
                           select md5).FirstOrDefault();
            if (gameMd5 != null)
            {
                var game = (from g in _doc.Element(Namespace + "NScumm").Elements(Namespace + "Game")
                            where (string)g.Attribute("id") == (string)gameMd5.Attribute("gameId")
                            where (string)g.Attribute("variant") == (string)gameMd5.Attribute("variant")
                            select g).FirstOrDefault();
                var desc = (from d in _doc.Element(Namespace + "NScumm").Elements(Namespace + "Description")
                            where (string)d.Attribute("gameId") == (string)gameMd5.Attribute("gameId")
                            select (string)d.Attribute("text")).FirstOrDefault();
                var attFeatures = gameMd5.Attribute("features");
                var platformText = (string)gameMd5.Attribute("platform");
                var platform = platformText != null ? (Platform?)Enum.Parse(typeof(Platform), platformText, true) : null;
                var features = ParseFeatures((string)attFeatures);
                var attMusic = game.Attribute("music");
                var music = ParseMusic((string)attMusic);
                info = new GameInfo
                {
                    MD5 = signature,
                    Platform = platform.HasValue ? platform.Value : Platform.DOS,
                    Path = path,
                    Id = (string)game.Attribute("id"),
                    Pattern = (string)game.Attribute("pattern"),
                    GameId = (GameId)Enum.Parse(typeof(GameId), (string)game.Attribute("gameId"), true),
                    Variant = (string)game.Attribute("variant"),
                    Description = desc,
                    Version = (int)game.Attribute("version"),
                    Language = (Language)Enum.Parse(typeof(Language), (string)gameMd5.Attribute("language"), true),
                    Features = features,
                    Music = music
                };
            }
            return info;
        }

        GameFeatures ParseFeatures(string feature)
        {
            var feat = feature == null ? new string[0] : feature.Split(' ');
            var features = GameFeatures.None;
            foreach (var f in feat)
            {
                features |= (GameFeatures)Enum.Parse(typeof(GameFeatures), f, true);
            }
            return features;
        }

        MusicDriverTypes ParseMusic(string music)
        {
            var mus = music == null ? new string[0] : music.Split(' ');
            var musics = MusicDriverTypes.None;
            foreach (var m in mus)
            {
                musics |= (MusicDriverTypes)Enum.Parse(typeof(MusicDriverTypes), m, true);
            }
            return musics;
        }
    }
}
