// Displays the current status badge for one player seat.
// Status: NotOpen | Man3a | Farcha | Eliminated
// Colour-coded: default=grey, Man3a=purple, Farcha=gold, Eliminated=dark red.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rami
{
    public class PlayerStatusBadge : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _statusLabel;
        [SerializeField] private Image           _statusBackground;
        [SerializeField] private TextMeshProUGUI _cardCountLabel;   // e.g. "14 cards"

        // Status colours matching the spec.
        private static readonly Color ColourNotOpen    = new Color(0.35f, 0.35f, 0.40f, 0.90f);
        private static readonly Color ColourMan3a      = new Color(0.45f, 0.1f,  0.70f, 0.90f);
        private static readonly Color ColourFarcha     = new Color(0.75f, 0.60f, 0.0f,  0.90f);
        private static readonly Color ColourEliminated = new Color(0.40f, 0.05f, 0.05f, 0.90f);

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Sets the player name label.</summary>
        public void SetPlayerName(string name)
        {
            if (_nameLabel != null) _nameLabel.text = name;
        }

        /// <summary>Updates status badge from player record and hand count.</summary>
        public void Refresh(MatchState.PlayerRecord record, int cardCount)
        {
            if (record == null) return;

            string statusText;
            Color  statusColour;

            if (record.IsEliminated)
            {
                statusText   = "Eliminated";
                statusColour = ColourEliminated;
            }
            else if (record.HasShownMelds)
            {
                statusText   = "Man3a ✓";
                statusColour = ColourMan3a;
            }
            else
            {
                statusText   = "Not Open";
                statusColour = ColourNotOpen;
            }

            if (_statusLabel      != null) _statusLabel.text       = statusText;
            if (_statusBackground != null) _statusBackground.color = statusColour;
            if (_cardCountLabel   != null) _cardCountLabel.text     = $"{cardCount} cards";
        }

        /// <summary>Shows or hides a Farcha badge overlay (for the winning player).</summary>
        public void ShowFarcha(bool shown)
        {
            if (!shown) return;
            if (_statusLabel      != null) _statusLabel.text       = "Farcha! ★";
            if (_statusBackground != null) _statusBackground.color = ColourFarcha;
        }
    }
}
