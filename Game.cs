using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace PairReader
{
    internal class Game
    {
        public static List<string> GameCodes = new List<string>();
        private static readonly string saveFile = @"e:\CardPairs\GameCodes.txt";

        public string Code { get; internal set; }
        public Player First { get; internal set; }
        public Player Second { get; internal set; }
        
        internal List<CardEntity> Cards = new List<CardEntity>();

        internal List<CardPair> CardPairs = new List<CardPair>();

        private int turn = 0;
        public int Turn 
        { 
            get => turn; 
            set { 
                if (turn != value - 1) throw new ArgumentException(); 
                turn = value; 
            } 
        }

        internal static bool ContainsGame(string gameCode)
        {
            return GameCodes.Contains(gameCode);
        }

        internal void RegisterCard(string id, string zone)
        {
            if (int.Parse(id) > 64)
            {
                return;
            }
            if (!Cards.Exists(x => x.EntityId == id)){
                if (zone == "7")
                {
                    CardEntity ent = new CardEntity(null, id, null, CardType.SECRET, null)
                    {
                        Turn = Turn
                    };
                    Cards.Add(ent);
                }
                return;
            }
            CardEntity entity = Cards.Find(x => x.EntityId == id);
            string hotZone = "1"; //play
            if (entity.CardType == CardType.SPELL)
            {
                hotZone = "4"; //graveyard to account for discount spells
            }
            if (entity.CardType == CardType.SECRET)
            {
                hotZone = "7";
            }
            if (hotZone != zone)
            {
                return;
            }
            if (entity.Turn == 0)
            {
                entity.Turn = Turn; //avoid replaying of bounced cards
            }
        }

        internal static void SaveGameCodes()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string code in GameCodes)
            {
                sb.AppendLine(code);
            }
            File.WriteAllText(saveFile, sb.ToString());
        }

        internal void RegisterWinner(XElement e)
        {
            string value = e.Attribute("value").Value;
            if (value != "4" && value != "5")
            {
                return;
            }
            string entity = e.Attribute("entity").Value;
            if ((entity == "2" && value == "4") || (entity == "3" && value == "5"))
            {
                First.IsWinner = true;
                Second.IsWinner = false;
                return;
            }
            if ((entity == "2" && value == "5") || (entity == "3" && value == "4"))
            {
                First.IsWinner = false;
                Second.IsWinner = true;
                return;
            }
            throw new InvalidOperationException();
        }

        private void AddCardPair(CardPair cp)
        {
            if (cp == null)
            {
                return;
            }
            if (CardPairs.Contains(cp))
            {
                CardPair existing = CardPairs.Find(x => x.Equals(cp));
                if (existing != null && existing.Wins == cp.Wins)
                {
                    return;
                }
            }
            CardPairs.Add(cp);
        }

        internal void Summarize()
        {
            this.Cards.Sort();
            foreach (CardEntity ce in Cards)
            {
                if (ce.Name is null || ce.Turn == 0)
                {
                    continue;
                }
                List<CardEntity> others = Cards.FindAll(x => x.Turn == ce.Turn);
                others.AddRange(Cards.FindAll(x => x.Turn == ce.Turn + 2));
                foreach (CardEntity other in others)
                {
                    if(other.Name is null)
                    {
                        continue;
                    }
                    if (IsValidPair(ce, other))
                    {
                        AddCardPair(new CardPair(ce.Name, other.Name, ce.Owner.HeroClass, ce.Owner.IsWinner));
                    }
                }
            }
        }

        private bool IsValidPair(CardEntity ce, CardEntity other)
        {
            if (ce.Name == other.Name || ce.Owner != other.Owner)
            {
                return false;
            }
            //string heroClass1 = PairReader.Cards.FindByName(ce.Name).cardClass;
            //string heroClass2 = PairReader.Cards.FindByName(other.Name).cardClass;
            //if ((heroClass1 != ce.Owner.HeroClass.ToString() && heroClass1 != "NEUTRAL") ||
            //    (heroClass2 != ce.Owner.HeroClass.ToString() && heroClass2 != "NEUTRAL"))
            //{
            //    return false;
            //}
            return true;
        }

        internal static void LoadGameCodes()
        {
            if (!File.Exists(saveFile))
            {
                return;
            }
            string[] lines = File.ReadAllLines(saveFile);
            foreach(string line in lines)
            {
                GameCodes.Add(line.Trim());
            }
        }
    }
}