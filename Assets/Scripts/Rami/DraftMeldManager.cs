// Manages the human player's private draft melds.
// Cards are moved from hand → draft staging area.
// They are NEVER committed to the public meld table directly.
// Public reveal happens only through Man3a (≥61 pts) or Farcha (all 14 arranged).
using System.Collections.Generic;
using System.Linq;

namespace Rami
{
    /// <summary>
    /// Owns the private staging area for the human player's melds.
    /// Cards remain hidden from opponents until Man3a or Farcha is declared.
    /// </summary>
    public class DraftMeldManager
    {
        // Each staged meld is a mutable list so the player can rearrange cards.
        private readonly List<List<PlayingCard>> _stagedMelds = new();

        // ──────────────────────────────────────────────────────────────────────────
        // Read
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>All currently staged (private) melds.</summary>
        public IReadOnlyList<IReadOnlyList<PlayingCard>> StagedMelds => _stagedMelds;

        /// <summary>All cards currently locked inside staged melds.</summary>
        public List<PlayingCard> AllStagedCards()
            => _stagedMelds.SelectMany(m => m).ToList();

        public bool HasStagedMelds => _stagedMelds.Count > 0;

        /// <summary>Total point value of all cards in staged melds (joker value is estimated).</summary>
        public int StagedPointValue()
            => _stagedMelds.Sum(m => RamiRuleValidator.MeldPointValue(m));

        // ──────────────────────────────────────────────────────────────────────────
        // Staging
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Validates and stages a meld. Returns false with a reason if the meld is invalid.
        /// Does NOT remove cards from the hand — caller handles that.
        /// </summary>
        public bool StageMeld(IList<PlayingCard> cards, out string reason)
        {
            if (!RamiRuleValidator.IsValidMeld(cards, out reason))
                return false;

            _stagedMelds.Add(new List<PlayingCard>(cards));
            reason = string.Empty;
            return true;
        }

        /// <summary>Removes the staged meld at <paramref name="index"/> and returns its cards.</summary>
        public List<PlayingCard> UnstageMeld(int index)
        {
            if (index < 0 || index >= _stagedMelds.Count) return new List<PlayingCard>();
            var cards = _stagedMelds[index];
            _stagedMelds.RemoveAt(index);
            return cards;
        }

        /// <summary>Cancels all staged melds and returns every card to the caller for re-adding to hand.</summary>
        public List<PlayingCard> CancelAll()
        {
            var all = AllStagedCards();
            _stagedMelds.Clear();
            return all;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a threshold check
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the staged melds collectively meet the Man3a requirement (≥ 61 pts).
        /// <paramref name="totalPoints"/> receives the calculated value regardless of result.
        /// </summary>
        public bool MeetsMan3aThreshold(out int totalPoints)
        {
            totalPoints = StagedPointValue();
            return totalPoints >= 61;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Reveal (used only by Man3a / Farcha paths in GameManager)
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drains and returns all staged melds so the caller can register them publicly.
        /// Clears the staging area. Call only from Man3a or Farcha code paths.
        /// </summary>
        public List<List<PlayingCard>> RevealDraft()
        {
            var result = new List<List<PlayingCard>>(_stagedMelds.Select(m => new List<PlayingCard>(m)));
            _stagedMelds.Clear();
            return result;
        }
    }
}
