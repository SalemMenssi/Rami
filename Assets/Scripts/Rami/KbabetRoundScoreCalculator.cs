// Calculates per-player round scores for Kbabet mode.
// Winner (player who went out) → 0 pts.
// Others → sum of card values remaining in hand.
// Jokers NOT in any combination count at their face value (50 pts).
// Tafdhila eligibility: winner had zero jokers across all their submitted melds.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public static class KbabetRoundScoreCalculator
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Result DTO
        // ─────────────────────────────────────────────────────────────────────────

        public class KbabetRoundResult
        {
            public string PlayerName        { get; set; }
            public int    RoundScore        { get; set; }
            public int    HandValue         { get; set; }
            public bool   IsWinner          { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Main calculation
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes round scores for all players.
        /// <paramref name="winnerName"/>: the player who went out first.
        /// <paramref name="hands"/>: remaining cards in each player's hand.
        /// <paramref name="winnerMelds"/>: the melds the winner submitted (used to check Tafdhila eligibility).
        /// <paramref name="tafdhilaEligible"/>: set to true if the winner used zero jokers in their melds.
        /// </summary>
        public static List<KbabetRoundResult> Calculate(
            string                               winnerName,
            IEnumerable<PlayerHand>              hands,
            IList<List<PlayingCard>>             winnerMelds,
            out bool                             tafdhilaEligible)
        {
            tafdhilaEligible = WinnerUsedNoJokersInMelds(winnerMelds);

            var results = new List<KbabetRoundResult>();

            foreach (var hand in hands)
            {
                bool isWinner = hand.PlayerName == winnerName;
                int  score    = isWinner ? 0 : hand.HandValue();

                results.Add(new KbabetRoundResult
                {
                    PlayerName = hand.PlayerName,
                    IsWinner   = isWinner,
                    HandValue  = score,
                    RoundScore = score
                });

                Debug.Log($"[Kbabet] {hand.PlayerName}: {(isWinner ? "winner" : $"hand={score}")} → +{score}");
            }

            return results;
        }

        /// <summary>Converts result list to a name→score dictionary for KbabetMatchState.</summary>
        public static Dictionary<string, int> ToScoreDictionary(IEnumerable<KbabetRoundResult> results)
        {
            return results.ToDictionary(r => r.PlayerName, r => r.RoundScore);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Tafdhila check
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when none of the submitted melds contain a joker card.
        /// Jokers held outside melds (still in hand) do not disqualify Tafdhila.
        /// </summary>
        private static bool WinnerUsedNoJokersInMelds(IList<List<PlayingCard>> melds)
        {
            if (melds == null || melds.Count == 0) return false;
            return melds.All(meld => meld.All(c => !c.IsJoker));
        }
    }
}
