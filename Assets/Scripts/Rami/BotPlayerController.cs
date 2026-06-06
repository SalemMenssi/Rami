// AI controller for bot players in Standard Rami.
// Bots follow the same rules as the human:
//   - Internal draft melds only (never show melds freely).
//   - Man3a: reveal if total ≥ 61 pts from valid non-overlapping melds.
//   - Farcha: finish with all cards in melds + 1 discard (no 61 requirement).
//   - Jokers are treated as universal wildcards by RamiRuleValidator.
//   - If neither Man3a nor Farcha is possible, draw → optionally arrange → discard → end turn.
//   - Eliminated bots do not take turns.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public class BotPlayerController : MonoBehaviour
    {
        private GameManager _game;

        private const int Man3aThreshold = 61;

        /// <summary>Wires the bot to the central GameManager.</summary>
        public void Init(GameManager game) => _game = game;

        /// <summary>Runs a full bot turn as a coroutine.</summary>
        public IEnumerator RunTurn(PlayerHand bot, DeckManager deck)
        {
            // Verify this bot is still active in the match.
            if (!_game.Match.IsActive(bot.PlayerName)) yield break;

            // 1. Brief pause so turns feel natural.
            yield return new WaitForSeconds(Random.Range(0.6f, 1.1f));

            // 2. Draw from deck.
            var drawn = deck.DrawFromDeck();
            if (drawn != null)
            {
                bot.AddCard(drawn);
                Debug.Log($"[Rami] {bot.PlayerName} drew {drawn}");
            }

            yield return new WaitForSeconds(0.3f);

            // 3. Try Farcha first (wins even without 61).
            if (TryBotFarcha(bot, deck))
            {
                Debug.Log($"[Rami] {bot.PlayerName} called Farcha!");
                yield break; // OnBotFarcha called inside TryBotFarcha
            }

            // 4. Try Man3a (reveal melds if ≥ 61 pts).
            TryBotMan3a(bot);

            yield return new WaitForSeconds(0.3f);

            // 5. Discard the highest-value card remaining in hand.
            DiscardHighest(bot, deck);

            yield return new WaitForSeconds(0.2f);

            // 6. Signal end of turn.
            _game.OnBotTurnFinished(bot);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Farcha attempt
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true and ends the round if the bot can arrange all cards into valid melds
        /// with exactly one card left to discard.
        /// </summary>
        private bool TryBotFarcha(PlayerHand bot, DeckManager deck)
        {
            var hand = bot.Cards.ToList();
            if (hand.Count < 2) return false;

            // Try each card as the potential discard; check if the rest form valid melds.
            foreach (var candidate in hand)
            {
                var remaining = hand.Where(c => c.InstanceId != candidate.InstanceId).ToList();
                var melds     = RamiRuleValidator.FindNonOverlappingMelds(remaining);
                int covered   = melds.Sum(m => m.Count);

                if (covered == remaining.Count && remaining.Count > 0)
                {
                    // All other cards covered — Farcha valid.
                    foreach (var meld in melds)
                    {
                        bot.RemoveCards(meld);
                        _game.RegisterPublicMeld(bot, meld);
                    }

                    bot.RemoveCard(candidate);
                    deck.AddToDiscard(candidate);

                    Debug.Log($"[Rami] {bot.PlayerName} Farcha — discards {candidate}");
                    _game.OnBotFarcha(bot);
                    return true;
                }
            }

            return false;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a attempt
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reveals melds publicly if the bot can form non-overlapping valid melds totalling ≥ 61 pts.
        /// Only the covered cards are removed; uncovered cards remain for discarding.
        /// After Man3a the bot's remaining hand is shown face-up on the table.
        /// </summary>
        private void TryBotMan3a(PlayerHand bot)
        {
            // If already shown, do not re-show.
            if (_game.Match.GetRecord(bot.PlayerName)?.HasShownMelds == true) return;

            var hand  = bot.Cards.ToList();
            var melds = RamiRuleValidator.FindNonOverlappingMelds(hand);
            if (melds.Count == 0) return;

            int pts = melds.Sum(m => RamiRuleValidator.MeldPointValue(m));
            if (pts < Man3aThreshold) return;

            foreach (var meld in melds)
            {
                bot.RemoveCards(meld);
                _game.RegisterPublicMeld(bot, meld);
            }

            _game.MarkBotShownMelds(bot);
            // Show remaining hand cards face-up on the bot's table area.
            _game.UIController.OnBotMan3aDeclared(bot.PlayerName, bot.Cards);
            Debug.Log($"[Rami] {bot.PlayerName} Man3a — {pts} pts revealed.");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Discard
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Discards the highest-point card remaining in the bot's hand.</summary>
        private void DiscardHighest(PlayerHand bot, DeckManager deck)
        {
            if (bot.CardCount == 0) return;

            var discard = bot.Cards
                .OrderByDescending(c => c.PointValue())
                .First();

            bot.RemoveCard(discard);
            deck.AddToDiscard(discard);
            Debug.Log($"[Rami] {bot.PlayerName} discarded {discard}");
        }
    }
}
