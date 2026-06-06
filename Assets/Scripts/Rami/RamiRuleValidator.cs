// Validates Standard Rami melds (sequences and sets).
// Jokers are universal wildcards: they fill any missing rank in a sequence
// or any missing suit in a set.
using System.Collections.Generic;
using System.Linq;

namespace Rami
{
    public static class RamiRuleValidator
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="cards"/> form a valid meld (set or sequence).
        /// Jokers are treated as universal wildcards.
        /// Populates <paramref name="reason"/> with the rejection message when invalid.
        /// </summary>
        public static bool IsValidMeld(IList<PlayingCard> cards, out string reason)
        {
            reason = string.Empty;

            if (cards == null || cards.Count < 3)
            {
                reason = "Need at least 3 cards.";
                return false;
            }

            if (IsSet(cards))      return true;
            if (IsSequence(cards)) return true;

            reason = "Not a valid set or sequence.";
            return false;
        }

        /// <summary>
        /// Calculates the point value of a meld, including the inferred value of jokers.
        /// Joker value = average non-joker card value in the meld, or 10 if all jokers.
        /// </summary>
        public static int MeldPointValue(IList<PlayingCard> cards)
        {
            var normals = cards.Where(c => !c.IsJoker).ToList();
            int normalSum = normals.Sum(c => c.PointValue());
            int jokerCount = cards.Count - normals.Count;

            if (jokerCount == 0) return normalSum;
            if (normals.Count == 0) return jokerCount * 10;

            // Joker takes average value of normal cards in this meld for scoring.
            int avgNormal = normalSum / normals.Count;
            return normalSum + jokerCount * avgNormal;
        }

        /// <summary>Finds all valid melds inside <paramref name="hand"/> (may overlap).</summary>
        public static List<List<PlayingCard>> FindValidMelds(IList<PlayingCard> hand)
        {
            var result = new List<List<PlayingCard>>();
            var cards  = hand.ToList();

            for (int size = 3; size <= cards.Count; size++)
                foreach (var combo in Combinations(cards, size))
                    if (IsValidMeld(combo, out _))
                        result.Add(combo);

            return result;
        }

        /// <summary>
        /// Greedy search for the largest set of non-overlapping valid melds covering as many
        /// cards as possible. Used by Man3a and Farcha validation.
        /// Each card instance is used in at most one meld (matched by InstanceId).
        /// </summary>
        public static List<List<PlayingCard>> FindNonOverlappingMelds(IList<PlayingCard> hand)
        {
            var result    = new List<List<PlayingCard>>();
            var available = new HashSet<string>(hand.Select(c => c.InstanceId));
            var remaining = hand.ToList();

            // Prefer longer melds first so more cards are covered.
            var allMelds = FindValidMelds(remaining)
                .OrderByDescending(m => m.Count)
                .ThenByDescending(m => MeldPointValue(m))
                .ToList();

            foreach (var meld in allMelds)
            {
                if (meld.All(c => available.Contains(c.InstanceId)))
                {
                    result.Add(meld);
                    foreach (var c in meld)
                        available.Remove(c.InstanceId);
                }
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Meld detection — joker-aware
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Set: 3–4 cards of the same rank, all different suits.
        /// Jokers fill missing suits.
        /// Duplicate identical normal cards (same suit AND rank) are not allowed.
        /// </summary>
        private static bool IsSet(IList<PlayingCard> cards)
        {
            if (cards.Count < 3 || cards.Count > 4) return false;

            var normals = cards.Where(c => !c.IsJoker).ToList();
            int jokerCount = cards.Count - normals.Count;

            if (normals.Count == 0) return false; // pure jokers cannot form a meaningful set

            // All non-jokers must share the same rank.
            var rank = normals[0].Rank;
            if (normals.Any(c => c.Rank != rank)) return false;

            // Collect the suits used by normal cards.
            var usedSuits = normals.Select(c => c.Suit).ToList();

            // Duplicate suits among normal cards → invalid.
            if (usedSuits.Distinct().Count() != usedSuits.Count) return false;

            // Jokers fill the remaining distinct suit slots (max 4 suits total in standard Rami).
            // The total card count already constrains the meld to 3–4, which is valid.
            return true;
        }

        /// <summary>
        /// Sequence: 3+ cards of the same suit with consecutive ranks.
        /// Ace is low by default (A=1). Special exception: Q-K-A is valid (Ace high after King).
        /// Jokers fill missing consecutive ranks.
        /// Duplicate normal cards (same suit AND rank) are not allowed.
        /// </summary>
        private static bool IsSequence(IList<PlayingCard> cards)
        {
            if (cards.Count < 3) return false;

            var normals = cards.Where(c => !c.IsJoker).ToList();
            int jokerCount = cards.Count - normals.Count;

            if (normals.Count == 0) return false; // pure jokers don't form a sequence

            // All non-jokers must share the same suit.
            var suit = normals[0].Suit;
            if (normals.Any(c => c.Suit != suit)) return false;

            // No duplicate ranks among normal cards.
            var normalRanks = normals.Select(c => (int)c.Rank).OrderBy(r => r).ToList();
            if (normalRanks.Distinct().Count() != normalRanks.Count) return false;

            // ── Ace-high special case: Q(12)-K(13)-A(1) and joker expansions of that sequence.
            // Remap Ace to 14 when all normal ranks are among {12, 13, 1} and we have an Ace.
            bool hasAce = normals.Any(c => c.Rank == CardRank.Ace);
            if (hasAce)
            {
                // Build a candidate rank list treating Ace as 14.
                var highRanks = normals.Select(c => c.Rank == CardRank.Ace ? 14 : (int)c.Rank)
                                       .OrderBy(r => r).ToList();
                // Only treat Ace-as-high if all ranks are ≥ 12 (Q, K, A-high).
                if (highRanks.All(r => r >= 12))
                {
                    if (CheckSequenceSpan(highRanks, jokerCount, minBound: 12, maxBound: 14))
                        return true;
                }
            }

            // ── Standard Ace-low sequence.
            return CheckSequenceSpan(normalRanks, jokerCount, minBound: 1, maxBound: 13);
        }

        /// <summary>
        /// Checks whether <paramref name="sortedRanks"/> plus <paramref name="jokerCount"/> wildcards
        /// form a valid consecutive span within [<paramref name="minBound"/>, <paramref name="maxBound"/>].
        /// </summary>
        private static bool CheckSequenceSpan(
            List<int> sortedRanks, int jokerCount, int minBound, int maxBound)
        {
            int minRank = sortedRanks.First();
            int maxRank = sortedRanks.Last();
            int span    = maxRank - minRank + 1;

            int gapsInside = span - sortedRanks.Count;
            if (gapsInside > jokerCount) return false;

            int remainingJokers = jokerCount - gapsInside;
            int totalSlots      = span + remainingJokers;

            // Total card count (normals + jokers) must exactly match slots.
            int totalCards = sortedRanks.Count + jokerCount;
            if (totalSlots != totalCards) return false;

            int lowestPossible  = minRank - remainingJokers;
            int highestPossible = maxRank + remainingJokers;
            return lowestPossible >= minBound && highestPossible <= maxBound;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a add-to-meld helper
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="additions"/> can be appended to <paramref name="existingMeld"/>
        /// while keeping the combined group a valid meld (set or sequence).
        /// The original meld's combination type is preserved.
        /// </summary>
        public static bool CanAddCardsToMeld(
            IList<PlayingCard> existingMeld,
            IList<PlayingCard> additions,
            out string reason)
        {
            reason = string.Empty;

            if (additions == null || additions.Count == 0)
            {
                reason = "No cards to add.";
                return false;
            }

            var combined = existingMeld.Concat(additions).ToList();
            return IsValidMeld(combined, out reason);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Combination helper
        // ──────────────────────────────────────────────────────────────────────────

        private static IEnumerable<List<PlayingCard>> Combinations(List<PlayingCard> list, int size)
        {
            if (size == 0) { yield return new List<PlayingCard>(); yield break; }
            for (int i = 0; i <= list.Count - size; i++)
                foreach (var rest in Combinations(list.GetRange(i + 1, list.Count - i - 1), size - 1))
                {
                    var combo = new List<PlayingCard> { list[i] };
                    combo.AddRange(rest);
                    yield return combo;
                }
        }
    }
}
