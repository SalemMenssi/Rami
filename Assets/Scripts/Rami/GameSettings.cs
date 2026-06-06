// Static carrier that survives a scene load.
// Set in MainMenuController before loading the Game scene; read by GameManager on Start().
namespace Rami
{
    public static class GameSettings
    {
        // ── Defaults ─────────────────────────────────────────────────────────────
        public const GameMode DefaultMode            = GameMode.StandardRami;
        public const int      DefaultEliminationTarget = 600;   // Standard Rami
        public const int      DefaultDwazTarget        = 31;    // Dwaz
        public const string   DefaultTafdhilaTarget    = "";    // Kbabet: empty = not used yet

        // ── Selection set from the main menu ─────────────────────────────────────

        /// <summary>The game mode chosen by the player.</summary>
        public static GameMode ChosenMode { get; set; } = DefaultMode;

        /// <summary>
        /// Target score for the chosen mode.
        /// Standard Rami: elimination threshold (600/800/1000).
        /// Dwaz: win target (21/31/40).
        /// Kbabet: not used for winning; stored as 0.
        /// </summary>
        public static int ChosenTarget { get; set; } = DefaultEliminationTarget;

        /// <summary>
        /// Kbabet only — whether Tafdhila is enabled for this match.
        /// When true, winning with zero jokers in melds awards 50 pts to a chosen opponent.
        /// </summary>
        public static bool KbabetTafdhilaEnabled { get; set; } = false;
    }
}
