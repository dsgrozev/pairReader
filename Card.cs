using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace PairReader
{
    public class Cards
    {
        public static List<Card> CardList { get; set; }
        internal static void Load()
        {
            using (WebClient client = new WebClient())
            {
                string jsonString = 
                    client.DownloadString(@"https://api.hearthstonejson.com/v1/latest/enUS/cards.json");
                CardList = JsonSerializer.Deserialize<List<Card>>(jsonString);
            }
        }

        internal static Card Find(string cardId)
        {
            if (CardList.Exists(x => x.id == cardId))
            {
                return CardList.Find(x => x.id == cardId);
            }

            return null;
        }

    }
    public class Card 
    { 
#pragma warning disable IDE1006 // Naming Styles
        public string artist { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string cardClass { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public bool collectible { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public int cost { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public int dbfId { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string flavor { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public List<string> mechanics { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string name { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string rarity { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public List<string> referencedTags { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string set { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string text { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public string type { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}