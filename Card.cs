using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace PairReader
{
    public class Cards
    {
        public static List<Card> CardList { get; set; }
        public static void Load()
        {
            using (WebClient client = new WebClient())
            {
                string jsonString = 
                    client.DownloadString(@"https://api.hearthstonejson.com/v1/latest/enUS/cards.json");
                CardList = JsonSerializer.Deserialize<List<Card>>(jsonString);
            }
        }

        public static Card FindByNameCollectible(string cardName)
        {
            return CardList.Find(x => x.name == cardName && x.collectible == true);
        }

        public static Card FindByNameNotHeroCollectible(string cardName)
        {
            return CardList.Find(x => x.name == cardName && x.collectible == true && x.set != "HERO_SKINS");
        }

        public static Card FindByName(string cardName)
        {
            return CardList.Find(x => x.name == cardName);
        }

        internal static Card Find(string cardId)
        {
            return CardList.Find(x => x.id == cardId);
        }

    }
    public class Card 
    { 
#pragma warning disable IDE1006 // Naming Styles
        public string artist { get; set; }
        public string cardClass { get; set; }
        public bool collectible { get; set; }
        public int cost { get; set; }
        public int dbfId { get; set; }
        public string flavor { get; set; }
        public string id { get; set; }
        public List<string> mechanics { get; set; }
        public string name { get; set; }
        public string rarity { get; set; }
        public List<string> referencedTags { get; set; }
        public string set { get; set; }
        public string text { get; set; }
        public string type { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}