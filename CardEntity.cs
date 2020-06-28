using System;
using System.Xml.Linq;

namespace PairReader
{
    internal class CardEntity : IComparable<CardEntity>
    {
        internal string CardId { get; set; }
        internal string EntityId { get; set; }
        internal string Name { get; private set; }
        internal CardType CardType { get; private set; }
        internal Player Owner { get; private set; }
        public int Turn { get; internal set; } = 0;

        internal static void CreateCardEntity(XElement xElement, Game game)
        {
            XAttribute cardIdAttr = xElement.Attribute("cardID");
            if (cardIdAttr == null)
            {
                return;
            }
            string cardId = cardIdAttr.Value;
            string entityId = xElement.FirstAttribute.Value;

            XElement child = xElement.FirstNode as XElement;
            while (child.Attribute("tag").Value != "50")
            {
                child = child.NextNode as XElement;
            }
            string heroId = child.Attribute("value").Value;
            Player owner = game.First;
            if (heroId == "2")
            {
                owner = game.Second;
            }
            Card card = Cards.Find(cardId);
            if (card == null || !card.collectible)
            {
                return;
            }
            CardType cardType = (CardType)Enum.Parse(typeof(CardType), card.type);
            if (cardType == CardType.HERO)
            {
                owner.SetHero(card.cardClass);
            }
            if (cardType == CardType.SPELL && card.mechanics != null &&
                (card.mechanics.Contains("SECRET") ||
                card.mechanics.Contains("QUEST") ||
                card.mechanics.Contains("SIDEQUEST")))
            {
                cardType = CardType.SECRET;
            }

            //if (int.Parse(entityId) > 64)

            if (!card.collectible)
            {
                return;
            }

            if (card.cardClass != "NEUTRAL" && card.cardClass != owner.HeroClass.ToString())
            {
                return;
            }

            CardEntity entity = new CardEntity(cardId, entityId, card.name, cardType, owner);
            if (game.Cards.Exists(x => x.EntityId == entityId))
            {
                var oldSecret = game.Cards.Find(x => x.EntityId == entityId);
                if (oldSecret.Name == null)
                {
                    int turn = oldSecret.Turn;
                    game.Cards.Remove(oldSecret);
                    entity.Turn = turn;
                }
                else
                {
                    return;
                }
            }
            game.Cards.Add(entity);
        }

        int IComparable<CardEntity>.CompareTo(CardEntity other)
        {
            return this.Turn.CompareTo(other.Turn);
        }

        public CardEntity(string cardId, string entityId, string name, CardType cardType, Player owner)
        {
            CardId = cardId;
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            Name = name;
            CardType = cardType;
            Owner = owner;
        }
    }
}
