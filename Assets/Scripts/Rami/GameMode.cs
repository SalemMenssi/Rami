// Enumerates all supported game modes.
namespace Rami
{
    public enum GameMode
    {
        /// <summary>Classic Rami — elimination by reaching a point cap (600/800/1000).</summary>
        StandardRami,

        /// <summary>
        /// Hardcore mode: Diamond 2 is joker, win only with a card given by the next player,
        /// winner earns +3 pts, next player earns a Zal3a (-2), jokers in hand score extra.
        /// First to reach the target (21/31/40) wins.
        /// </summary>
        Dwaz,

        /// <summary>
        /// Normal Rummy: minimum points wins.
        /// Optional Tafdhila: win with zero jokers in melds → give 50 pts to any one player.
        /// </summary>
        Kbabet,
    }
}
