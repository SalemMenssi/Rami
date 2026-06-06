// Manages the runtime hand of one player (human or bot).
using System.Collections.Generic;
using System.Linq;

namespace Rami
{
    public class PlayerHand
    {
        private readonly List<PlayingCard> _cards = new();

        public string PlayerName { get; }
        public bool   IsHuman    { get; }
        public int    CardCount  => _cards.Count;

        public IReadOnlyList<PlayingCard> Cards => _cards;

        public PlayerHand(string name, bool isHuman)
        {
            PlayerName = name;
            IsHuman    = isHuman;
        }

        public void AddCard(PlayingCard card)   => _cards.Add(card);
        public void AddCards(IEnumerable<PlayingCard> cards) => _cards.AddRange(cards);

        /// <summary>Clears all cards from the hand (used between rounds).</summary>
        public void ClearCards() => _cards.Clear();

        /// <summary>Removes card by reference and returns true if found.</summary>
        public bool RemoveCard(PlayingCard card) => _cards.Remove(card);

        /// <summary>Removes all matching cards from the hand.</summary>
        public void RemoveCards(IEnumerable<PlayingCard> cards)
        {
            foreach (var c in cards.ToList())
                _cards.Remove(c);
        }

        /// <summary>
        /// Moves a card to a new position in the hand list to match the visual drag-reorder.
        /// Safe no-op if the card is not found.
        /// </summary>
        public void MoveCardToIndex(PlayingCard card, int newIndex)
        {
            int current = _cards.IndexOf(card);
            if (current < 0) return;
            _cards.RemoveAt(current);
            newIndex = System.Math.Clamp(newIndex, 0, _cards.Count);
            _cards.Insert(newIndex, card);
        }

        /// <summary>
        /// Replaces the internal order with the supplied ordered list.
        /// Used by HandView after a drag-reorder to sync data with the visual layout.
        /// </summary>
        public void SetOrder(IList<PlayingCard> ordered)
        {
            _cards.Clear();
            _cards.AddRange(ordered);
        }

        /// <summary>Total unplayed hand penalty (for scoring).</summary>
        public int HandValue() => _cards.Sum(c => c.PointValue());
    }
}
