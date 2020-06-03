using System;
using System.Xml.Linq;

namespace PairReader
{
    public class Player
    {
        public string Name { get; private set; }
        public bool IsWinner { get; internal set; }
        public HeroClass HeroClass { get; private set; }

        public string PlayerId { get; private set; }

        public Player(string name, string id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PlayerId = id ?? throw new ArgumentNullException(nameof(id));
            HeroClass = HeroClass.NONE;
        }

        public void SetHero(string name)
        {
            if (this.HeroClass == HeroClass.NONE)
            {
                this.HeroClass = (HeroClass)Enum.Parse(typeof(HeroClass), name);
            }
        }
        
        internal static void AddPlayers(XElement firstPlayer, Game game)
        {
            game.First = new Player(firstPlayer.Attribute("name").Value,
                                    firstPlayer.Attribute("id").Value);
            XElement second = firstPlayer.NextNode as XElement;
            game.Second = new Player(second.Attribute("name").Value,
                                    second.Attribute("id").Value);
        }
    }
}