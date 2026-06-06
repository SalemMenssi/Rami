// Manages the bottom action panel's button visibility and interactability.
// Also drives the "Selected: X / 51 pts" meld-value indicator.
// Receives phase updates from GameUIController.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rami
{
    /// <summary>
    /// Encapsulates all bottom-bar button management so GameUIController stays clean.
    /// </summary>
    public class ActionPanelController : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Draw group
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Draw Buttons")]
        [SerializeField] private Button _btnDrawDeck;
        [SerializeField] private Button _btnDrawDiscard;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Action group (post-draw)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Action Buttons")]
        [SerializeField] private Button _btnStageMeld;
        [SerializeField] private Button _btnCancelDraft;
        [SerializeField] private Button _btnDiscard;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Special actions
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Special Action Buttons")]
        [SerializeField] private Button _btnShowMelds;    // Man3a
        [SerializeField] private Button _btnFarcha;
        [SerializeField] private Button _btnAddToMeld;
        [SerializeField] private Button _btnMan3aFarcha;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Meld value display
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Meld Value Display")]
        [SerializeField] private GameObject      _meldValuePanel;
        [SerializeField] private TextMeshProUGUI _selectedValueLabel;   // "Selected: 29 pts"
        [SerializeField] private TextMeshProUGUI _meldStatusLabel;      // "Need 22 more" / "Ready ✓"
        [SerializeField] private Image           _meldStatusBg;         // background tint

        private static readonly Color StatusReadyColor  = new Color(0.1f, 0.65f, 0.15f, 0.85f);
        private static readonly Color StatusNeedColor   = new Color(0.6f, 0.3f, 0.0f, 0.85f);
        private static readonly Color StatusNeutralColor= new Color(0.12f, 0.18f, 0.35f, 0.85f);

        private const int Man3aThreshold = 61;

        // ──────────────────────────────────────────────────────────────────────────
        // Phase API (called from GameUIController)
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets button interactability based on game phase.
        /// drawn=false → only draw buttons active.
        /// drawn=true  → draw disabled; stage/cancel/discard/special active.
        /// </summary>
        public void SetPhase(bool drawn, bool humanHasMan3a)
        {
            SetInteractable(_btnDrawDeck,    !drawn);
            SetInteractable(_btnDrawDiscard, !drawn);
            SetInteractable(_btnStageMeld,   drawn && !humanHasMan3a);
            SetInteractable(_btnDiscard,     drawn);
            SetInteractable(_btnCancelDraft, drawn);
            SetInteractable(_btnShowMelds,   drawn && !humanHasMan3a);
            SetInteractable(_btnFarcha,      drawn && !humanHasMan3a);

            // Man3a-specific buttons are controlled separately by RefreshMan3aButtons.
            if (!humanHasMan3a)
            {
                SetActive(_btnAddToMeld,   false);
                SetActive(_btnMan3aFarcha, false);
            }
        }

        /// <summary>Shows/hides Man3a add-to-meld buttons based on selection state.</summary>
        public void RefreshMan3aButtons(bool hasMeldTarget, bool hasRevealCards)
        {
            SetActive(_btnAddToMeld,   hasMeldTarget && hasRevealCards);
            SetActive(_btnMan3aFarcha, hasMeldTarget);
        }

        /// <summary>Disables all action buttons (e.g. during round-end popup).</summary>
        public void DisableAll()
        {
            SetInteractable(_btnDrawDeck,    false);
            SetInteractable(_btnDrawDiscard, false);
            SetInteractable(_btnStageMeld,   false);
            SetInteractable(_btnCancelDraft, false);
            SetInteractable(_btnDiscard,     false);
            SetInteractable(_btnShowMelds,   false);
            SetInteractable(_btnFarcha,      false);
            SetActive(_btnAddToMeld,   false);
            SetActive(_btnMan3aFarcha, false);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Meld value panel
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Updates the meld-value indicator based on selected cards.</summary>
        public void RefreshMeldValue(List<PlayingCard> selectedCards, bool hasShownMelds)
        {
            if (_meldValuePanel == null) return;

            if (selectedCards == null || selectedCards.Count == 0)
            {
                _meldValuePanel.SetActive(false);
                return;
            }

            _meldValuePanel.SetActive(true);

            int pts = RamiRuleValidator.MeldPointValue(selectedCards);

            if (_selectedValueLabel != null)
                _selectedValueLabel.text = $"Selected: {pts} pts";

            if (hasShownMelds)
            {
                // Already declared Man3a — show neutral status.
                if (_meldStatusLabel != null) _meldStatusLabel.text = "Man3a active";
                SetBgColor(StatusNeutralColor);
            }
            else if (pts >= Man3aThreshold)
            {
                if (_meldStatusLabel != null) _meldStatusLabel.text = "Ready ✓";
                SetBgColor(StatusReadyColor);
            }
            else
            {
                int need = Man3aThreshold - pts;
                if (_meldStatusLabel != null) _meldStatusLabel.text = $"Need {need} more";
                SetBgColor(StatusNeedColor);
            }
        }

        private void SetBgColor(Color c)
        {
            if (_meldStatusBg != null) _meldStatusBg.color = c;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private static void SetInteractable(Button btn, bool value)
        {
            if (btn != null) btn.interactable = value;
        }

        private static void SetActive(Button btn, bool value)
        {
            if (btn != null) btn.gameObject.SetActive(value);
        }
    }
}
