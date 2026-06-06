// Marks the HumanHandArea as a valid drop zone for card reordering.
// Highlights the hand area with a green tint while a card is hovered.
// On drop, CardDragHandler directly calls DropInsideHand() which handles
// re-parenting; HandDropZone only commits the data sync via HandView.
using UnityEngine;
using UnityEngine.UI;

namespace Rami
{
    [RequireComponent(typeof(Image))]
    public class HandDropZone : MonoBehaviour, ICardDropZone
    {
        private Image    _bg;
        private HandView _handView;

        private Color _normalColor;

        private static readonly Color HoverColor = new Color(0.15f, 0.70f, 0.20f, 0.45f);

        private void Awake()
        {
            _bg       = GetComponent<Image>();
            _handView = GetComponent<HandView>();
            if (_handView == null)
                _handView = GetComponentInChildren<HandView>();

            _normalColor = _bg != null ? _bg.color : new Color(0.02f, 0.10f, 0.04f, 0.45f);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // ICardDropZone
        // ──────────────────────────────────────────────────────────────────────────

        public bool CanAcceptDrop(CardView card) => _handView != null;

        public void OnCardDragOver(CardView card)
        {
            if (_bg != null) _bg.color = HoverColor;
        }

        public void OnCardDragEnd(CardView card)
        {
            if (_bg != null) _bg.color = _normalColor;
        }

        public void OnCardDropped(CardView card)
        {
            // CardDragHandler.DropInsideHand() already re-parents the card and calls
            // HandView.OnCardReordered(). This path is reached when CardDragHandler
            // detects HandDropZone as the drop target — it routes through DropInsideHand
            // itself, so here we only need to restore the visual state.
            if (_bg != null) _bg.color = _normalColor;
            _handView?.OnCardReordered();
        }
    }
}
