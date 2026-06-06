// Marks the DraftPanel as a valid drag-drop target.
// When a card is dropped here, stages all currently selected hand cards as a draft meld.
// GameManager reference is resolved at Awake (not Start) to prevent null-ref windows.
using UnityEngine;
using UnityEngine.UI;

namespace Rami
{
    [RequireComponent(typeof(Image))]
    public class DraftDropZone : MonoBehaviour, ICardDropZone
    {
        [SerializeField] private GameManager _gameManager;

        private Image _bg;
        private Color _normalColor;

        private static readonly Color HoverColor = new Color(0.15f, 0.30f, 0.75f, 0.70f);
        private static readonly Color ReadyColor = new Color(0.10f, 0.65f, 0.15f, 0.70f);

        private void Awake()
        {
            _bg          = GetComponent<Image>();
            _normalColor = _bg != null ? _bg.color : new Color(0.05f, 0.05f, 0.18f, 0.92f);

            // Resolve immediately so the reference is never null at runtime.
            if (_gameManager == null)
                _gameManager = FindFirstObjectByType<GameManager>();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // ICardDropZone
        // ──────────────────────────────────────────────────────────────────────────

        public bool CanAcceptDrop(CardView card) => card != null && card.Card != null;

        public void OnCardDragOver(CardView card)
        {
            if (_bg == null) return;
            // Green = dragged card is already selected (multi-card drop ready).
            // Blue  = dragged card not yet selected; single card being explored.
            _bg.color = card != null && card.IsSelected ? ReadyColor : HoverColor;
        }

        public void OnCardDragEnd(CardView card)
        {
            if (_bg != null) _bg.color = _normalColor;
        }

        public void OnCardDropped(CardView card)
        {
            if (_bg != null) _bg.color = _normalColor;
            if (_gameManager == null || card == null || card.Card == null) return;

            var uiCtrl = _gameManager.UIController;
            if (uiCtrl == null) return;

            // Ensure the dragged card is included in the staged group.
            card.SetSelected(true);

            var selected = uiCtrl.GetHumanSelectedCards();
            if (selected.Count >= 3)
                _gameManager.HumanStageMeld(selected);
            else
                uiCtrl.ShowToast("Select ≥ 3 cards before dragging to Draft.");
        }
    }
}
