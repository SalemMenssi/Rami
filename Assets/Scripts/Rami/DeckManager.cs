// Builds and manages the draw pile and discard pile.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public class DeckManager
    {
        private readonly List<PlayingCard> _drawPile    = new();
        private readonly List<PlayingCard> _discardPile = new();

        // ──────────────────────────────────────────────────────────────────────────
        // Public state
        // ──────────────────────────────────────────────────────────────────────────

        public int DrawCount    => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;

        /// <summary>Top card of the discard pile, or null if empty.</summary>
        public PlayingCard TopDiscard =>
            _discardPile.Count > 0 ? _discardPile[_discardPile.Count - 1] : null;

        // ──────────────────────────────────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a 108-card Rami deck (2 × 52 normal cards + 4 jokers),
        /// shuffles it, and seeds the discard pile with one face-up card.
        /// </summary>
        public void BuildAndShuffle()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            var suits = new[] { CardSuit.Spades, CardSuit.Hearts, CardSuit.Diamonds, CardSuit.Clubs };
            var ranks = new[]
            {
                CardRank.Ace,  CardRank.Two,   CardRank.Three, CardRank.Four,
                CardRank.Five, CardRank.Six,   CardRank.Seven, CardRank.Eight,
                CardRank.Nine, CardRank.Ten,   CardRank.Jack,  CardRank.Queen, CardRank.King
            };

            // Two full 52-card decks — copyIndex 0 and 1 distinguish the two copies.
            for (int copy = 0; copy < 2; copy++)
                foreach (var suit in suits)
                    foreach (var rank in ranks)
                        _drawPile.Add(new PlayingCard(suit, rank, copy));

            // Four jokers with unique copy indices 0–3.
            for (int j = 0; j < 4; j++)
                _drawPile.Add(new PlayingCard(CardSuit.Joker, CardRank.Joker, j));

            Shuffle(_drawPile);

            int normalCount = _drawPile.Count(c => !c.IsJoker);
            int jokerCount  = _drawPile.Count(c =>  c.IsJoker);
            Debug.Log($"[Rami] Deck built: {_drawPile.Count} cards " +
                      $"({normalCount} normal, {jokerCount} jokers)");

            // Seed discard pile.
            _discardPile.Add(DrawFromDeck());
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Deal
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Deals <paramref name="count"/> cards from the draw pile.</summary>
        public List<PlayingCard> Deal(int count)
        {
            var hand = new List<PlayingCard>(count);
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0) RefillFromDiscard();
                if (_drawPile.Count == 0) break;
                hand.Add(DrawFromDeck());
            }
            return hand;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Draw actions
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Human/bot draws from the draw pile.</summary>
        public PlayingCard DrawFromDeck()
        {
            if (_drawPile.Count == 0) RefillFromDiscard();
            if (_drawPile.Count == 0) return null;

            var card = _drawPile[_drawPile.Count - 1];
            _drawPile.RemoveAt(_drawPile.Count - 1);
            return card;
        }

        /// <summary>Human/bot draws the top discard card.</summary>
        public PlayingCard DrawFromDiscard()
        {
            if (_discardPile.Count == 0) return null;
            var card = _discardPile[_discardPile.Count - 1];
            _discardPile.RemoveAt(_discardPile.Count - 1);
            return card;
        }

        /// <summary>Adds a card to the top of the discard pile.</summary>
        public void AddToDiscard(PlayingCard card)
        {
            _discardPile.Add(card);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void RefillFromDiscard()
        {
            if (_discardPile.Count <= 1) return;

            // Keep the top discard card; shuffle the rest back.
            var top = _discardPile[_discardPile.Count - 1];
            _discardPile.RemoveAt(_discardPile.Count - 1);

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            _discardPile.Add(top);

            Shuffle(_drawPile);
            Debug.Log("[Rami] Draw pile refilled from discard.");
        }

        private static void Shuffle(List<PlayingCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
