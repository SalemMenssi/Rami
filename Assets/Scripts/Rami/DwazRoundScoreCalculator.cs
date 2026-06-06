// Produces the DwazRoundResult consumed by DwazMatchState.ApplyRoundResult.
// Diamond 2 counts as a joker for hand-penalty scoring.
using System.Collections.Generic;
using UnityEngine;

namespace Rami
{
    public static class DwazRoundScoreCalculator
    {
        /// <summary>
        /// Counts jokers in a hand for Dwaz scoring.
        /// Both standard joker cards AND the Diamond 2 count as jokers.
        /// </summary>
        public static int CountDwazJokers(IEnumerable<PlayingCard> hand)
        {
            int count = 0;
            foreach (var c in hand)
            {
                if (c.IsJoker) count++;
                else if (c.Suit == CardSuit.Diamonds && c.Rank == CardRank.Two) count++;
            }
            return count;
        }

        /// <summary>
        /// Builds a <see cref="DwazRoundResult"/> from the current hand states.
        /// <paramref name="winnerName"/> is the player who won the round.
        /// <paramref name="hands"/> are all player hands at round end.
        /// </summary>
        public static DwazRoundResult Calculate(
            string winnerName,
            IEnumerable<PlayerHand> hands)
        {
            var result = new DwazRoundResult
            {
                WinnerName   = winnerName,
                JokersInHand = new Dictionary<string, int>()
            };

            foreach (var hand in hands)
            {
                int jokers = CountDwazJokers(hand.Cards);
                result.JokersInHand[hand.PlayerName] = jokers;
                Debug.Log($"[Dwaz] {hand.PlayerName} has {jokers} Dwaz joker(s) in hand at round end.");
            }

            return result;
        }
    }
}
