// Rami Card Enums — clean standalone definitions, no dependency on broken SOs.
namespace Rami
{
    public enum CardSuit
    {
        Spades   = 0,
        Hearts   = 1,
        Diamonds = 2,
        Clubs    = 3,
        Joker    = 4
    }

    /// <summary>Rank values follow card index in the sprite map (Ace=1 … King=13).</summary>
    public enum CardRank
    {
        Ace   = 1,
        Two   = 2,
        Three = 3,
        Four  = 4,
        Five  = 5,
        Six   = 6,
        Seven = 7,
        Eight = 8,
        Nine  = 9,
        Ten   = 10,
        Jack  = 11,
        Queen = 12,
        King  = 13,
        Joker = 14
    }
}
