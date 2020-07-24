namespace PairReader
{
    internal class Stats
    {
        internal int pairs;
        internal int loses;
        internal int wins;
        internal int fullPairs;
        internal int fullLoses;
        internal int fullWins;
        internal HeroClass hero;

        public Stats(HeroClass hero)
        {
            this.hero = hero;
            var heroPairs = CardPair.CardPairs.FindAll(x => x.Hero == hero);
            pairs = heroPairs.Count;
            foreach (var pair in heroPairs)
            {
                wins += pair.Wins;
                loses += pair.Losses;
            }
            heroPairs = CardPair.FullCardPairs.FindAll(x => x.Hero == hero);
            fullPairs = heroPairs.Count;
            foreach (var pair in heroPairs)
            {
                fullWins += pair.Wins;
                fullLoses += pair.Losses;
            }
        }
    }
}