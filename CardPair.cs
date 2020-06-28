using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PairReader
{
    public class CardPair : IComparable<CardPair>
    {
        public static List<CardPair> CardPairs = new List<CardPair>();
        public static readonly string saveFile = @"e:\CardPairs\CardPairs.csv";
        private static readonly char saveDelimiter = ',';

        public CardPair(string card1, string card2, HeroClass hero, bool won)
        {
            if (card1.CompareTo(card2) < 0)
            {
                Card1 = card1;
                Card2 = card2;
            }
            else
            {
                Card1 = card2;
                Card2 = card1;
            }
            Hero = hero;
            if (won)
            {
                Wins = 1;
                Losses = 0;
            }
            else
            {
                Wins = 0;
                Losses = 1;
            }
        }

        public CardPair(string card1, string card2, HeroClass hero)
        {
            Card1 = card1 ?? throw new ArgumentNullException(nameof(card1));
            Card2 = card2 ?? throw new ArgumentNullException(nameof(card2));
            Hero = hero;
        }

        public string Card1 { get; private set; }
        public string Card2 { get; private set; }
        public HeroClass Hero { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }

        public double DeckWinPercentage()
        {
            if (Wins + Losses == 0)
            {
                return 0;
            }
            return 1.0 * Wins / (Wins + Losses);
        }

        private void AddCardPair(CardPair other)
        {
            if (!Equals(other))
            {
                return;
            }
            Wins += other.Wins;
            Losses += other.Losses;
        }

        internal static void SavePairs()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Card1,Card2,Hero,Wins,Loses,Percentage,Count");
            foreach(CardPair cp in CardPairs)
            {
                sb.Append("\"" + cp.Card1 + "\"")
                    .Append(saveDelimiter)
                    .Append("\"" + cp.Card2 + "\"")
                    .Append(saveDelimiter)
                    .Append(cp.Hero.ToString())
                    .Append(saveDelimiter)
                    .Append(cp.Wins)
                    .Append(saveDelimiter)
                    .Append(cp.Losses)
                    .Append(saveDelimiter)    
                    .Append(cp.DeckWinPercentage().ToString())
                    .Append(saveDelimiter)
                    .AppendLine(cp.Count().ToString());
            }
            File.WriteAllText(saveFile, sb.ToString().Trim());
        }

        public int Count()
        {
            return Wins + Losses;
        }

        public override bool Equals(object other)
        {
            if (other is CardPair)
            {
                CardPair otherCP = other as CardPair;
                return Card1 == otherCP.Card1 && Card2 == otherCP.Card2 && Hero == otherCP.Hero;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Card1 + Card2 + Hero.ToString()).GetHashCode();
        }

        internal static void AddGamePairs(Game game)
        {
            foreach (CardPair cp in game.CardPairs)
            {
                if (CardPairs.Contains(cp))
                {
                    CardPairs.Find(x => x.Equals(cp)).AddCardPair(cp);
                }
                else
                {
                    CardPairs.Add(cp);
                }
            }
        }

        public static void LoadPairs()
        {
            if (!File.Exists(saveFile))
            {
                return;
            }
            string[] lines = File.ReadAllLines(saveFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] names = lines[i].Split(new string[] { "\"," }, StringSplitOptions.RemoveEmptyEntries);
                string card1 = names[0].Substring(1);
                string card2 = names[1].Substring(1);
                string[] rest = names[2].Split(saveDelimiter);
                CardPair pair = new CardPair(card1, card2, (HeroClass)Enum.Parse(typeof(HeroClass), rest[0]))
                {
                    Wins = int.Parse(rest[1]),
                    Losses = int.Parse(rest[2])
                };
                CardPairs.Add(pair);
            }
        }

        public int CompareTo(CardPair other)
        {
            if (DeckWinPercentage() == other.DeckWinPercentage())
            {
                return -1 * Count().CompareTo(other.Count());
            }
            return -1 * DeckWinPercentage().CompareTo(other.DeckWinPercentage());
        }
    }
}
