// Runtime card model — pure C#, no ScriptableObject dependency.
using System;

namespace Rami
{
    [Serializable]
    public class PlayingCard
    {
        public CardSuit Suit      { get; }
        public CardRank Rank      { get; }

        /// <summary>
        /// Distinguishes the two physical copies of every normal card (0 = first deck, 1 = second deck).
        /// Jokers use values 0–3 for their four instances.
        /// </summary>
        public int CopyIndex { get; }

        /// <summary>
        /// Unique string key for this physical card instance, e.g. "Spades_Ace_0" / "Spades_Ace_1" / "Joker_2".
        /// Use this for selection, equality, and display — never compare by Suit+Rank alone.
        /// </summary>
        public string InstanceId { get; }

        public bool IsJoker => Suit == CardSuit.Joker;

        public PlayingCard(CardSuit suit, CardRank rank, int copyIndex = 0)
        {
            Suit      = suit;
            Rank      = rank;
            CopyIndex = copyIndex;
            InstanceId = IsJoker
                ? $"Joker_{copyIndex}"
                : $"{suit}_{rank}_{copyIndex}";
        }

        /// <summary>Point value used for scoring (unplayed hand penalty).</summary>
        public int PointValue()
        {
            if (IsJoker)              return 50;
            if (Rank == CardRank.Ace) return 11;
            if ((int)Rank >= 10)      return 10;
            return (int)Rank;
        }

        public override string ToString() =>
            IsJoker ? $"Joker[{CopyIndex}]" : $"{Rank} of {Suit} [{CopyIndex}]";
    }
}
