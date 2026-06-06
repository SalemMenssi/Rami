// UI component that displays a single playing card.
// Handles click-to-select, hover preview, and visual state (selected, dimmed, face-up/down).
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rami
{
    [RequireComponent(typeof(Image))]
    public class CardView : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector
        // ──────────────────────────────────────────────────────────────────────────
        [SerializeField] private Image        _cardImage;
        [SerializeField] private Image        _selectionOutline;   // child Image used as highlight
        [SerializeField] private RectTransform _rect;

        // ──────────────────────────────────────────────────────────────────────────
        // Private state
        // ──────────────────────────────────────────────────────────────────────────
        private PlayingCard _card;
        private bool        _isSelected;
        private bool        _isFaceUp;
        private bool        _isInteractable;

        private System.Action<CardView> _onClick;

        // Pixel offset: card lifts upward when selected (applied on the RectTransform).
        private const float SelectedLiftY = 24f;

        // ──────────────────────────────────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_cardImage == null) _cardImage = GetComponent<Image>();
            if (_rect      == null) _rect      = GetComponent<RectTransform>();
        }

        /// <summary>Binds the card data and display mode to this view.</summary>
        public void Setup(PlayingCard card, bool faceUp, bool interactable,
                          System.Action<CardView> onClick)
        {
            _card           = card;
            _isFaceUp       = faceUp;
            _isInteractable = interactable;
            _onClick        = onClick;
            _isSelected     = false;

            RefreshSprite();
            SetSelectedVisual(false);
        }

        public PlayingCard Card       => _card;
        public bool        IsSelected => _isSelected;

        // ──────────────────────────────────────────────────────────────────────────
        // Selection
        // ──────────────────────────────────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            SetSelectedVisual(selected);
        }

        private void SetSelectedVisual(bool selected)
        {
            if (_selectionOutline != null)
                _selectionOutline.gameObject.SetActive(selected);

            // Lift the card vertically via local position so the layout group pivot is unchanged.
            if (_rect != null)
            {
                Vector3 pos = _rect.localPosition;
                pos.y = selected ? SelectedLiftY : 0f;
                _rect.localPosition = pos;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Pointer events
        // ──────────────────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            _onClick?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Slight scale-up on hover for feedback (only on interactive cards).
            if (!_isInteractable) return;
            transform.localScale = Vector3.one * 1.04f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Visuals
        // ──────────────────────────────────────────────────────────────────────────

        private void RefreshSprite()
        {
            if (_cardImage == null) return;
            _cardImage.sprite = _isFaceUp
                ? CardSpriteLibrary.GetFaceSprite(_card)
                : CardSpriteLibrary.GetBackSprite();
        }

        /// <summary>Flips the card face-up or face-down.</summary>
        public void SetFaceUp(bool faceUp)
        {
            _isFaceUp = faceUp;
            RefreshSprite();
        }

        /// <summary>Dims the card to indicate it is locked in a staged draft meld.</summary>
        public void SetDimmed(bool dimmed)
        {
            if (_cardImage != null)
                _cardImage.color = dimmed ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
        }
    }
}
