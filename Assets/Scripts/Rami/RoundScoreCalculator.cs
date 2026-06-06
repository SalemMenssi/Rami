// Calculates per-player round scores following Standard Rami rules.
// Farcha winner → 0 pts.
// Man3a shown → leftover hand value (cards NOT in public melds).
// Not shown    → 100 pts flat.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public static class RoundScoreCalculator
    {
        /// <summary>
        /// Describes a single player's round result.
        /// </summary>
        public class RoundResult
        {
            public string PlayerName  { get; set; }
            public ResultType Type    { get; set; }
            public int  HandLeftover  { get; set; }   // value of cards still in hand
            public int  RoundScore    { get; set; }   // points added this round
        }

        public enum ResultType
        {
            Farcha,
            Man3a,
            NotShown
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Main calculation
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes round scores for all players.
        /// <paramref name="farchaWinner"/> is the name of the player who did Farcha, or null if the round
        /// ended via draw-pile exhaustion or Man3a chain.
        /// <paramref name="shownMeldOwners"/> maps playerName → bool indicating Man3a status.
        /// <paramref name="hands"/> are the current (remaining) cards in each player's hand.
        /// </summary>
        public static List<RoundResult> Calculate(
            IEnumerable<PlayerHand>             hands,
            string                              farchaWinner,
            IReadOnlyDictionary<string, bool>   shownMeldOwners,
            MatchState                          match)
        {
            var results = new List<RoundResult>();

            foreach (var hand in hands)
            {
                // Skip eliminated players — they don't score this round.
                if (!match.IsActive(hand.PlayerName))
                {
                    results.Add(new RoundResult
                    {
                        PlayerName   = hand.PlayerName,
                        Type         = ResultType.NotShown,
                        HandLeftover = 0,
                        RoundScore   = 0
                    });
                    continue;
                }

                bool isFarchaWinner = hand.PlayerName == farchaWinner;
                bool hasShownMelds  = shownMeldOwners.TryGetValue(hand.PlayerName, out var shown) && shown;

                RoundResult result;

                if (isFarchaWinner)
                {
                    result = new RoundResult
                    {
                        PlayerName   = hand.PlayerName,
                        Type         = ResultType.Farcha,
                        HandLeftover = 0,
                        RoundScore   = 0
                    };
                }
                else if (hasShownMelds)
                {
                    // Man3a: score = value of cards still in hand (not in public melds).
                    int leftover = hand.HandValue();
                    result = new RoundResult
                    {
                        PlayerName   = hand.PlayerName,
                        Type         = ResultType.Man3a,
                        HandLeftover = leftover,
                        RoundScore   = leftover
                    };
                }
                else
                {
                    // Not shown at all → flat 100 penalty.
                    result = new RoundResult
                    {
                        PlayerName   = hand.PlayerName,
                        Type         = ResultType.NotShown,
                        HandLeftover = hand.HandValue(),
                        RoundScore   = 100
                    };
                }

                Debug.Log($"[Score] {result.PlayerName}: {result.Type} → +{result.RoundScore}");
                results.Add(result);
            }

            return results;
        }

        /// <summary>Converts the result list to a simple name→score dictionary for MatchState.</summary>
        public static Dictionary<string, int> ToScoreDictionary(IEnumerable<RoundResult> results)
        {
            var dict = new Dictionary<string, int>();
            foreach (var r in results)
                dict[r.PlayerName] = r.RoundScore;
            return dict;
        }
    }
}
