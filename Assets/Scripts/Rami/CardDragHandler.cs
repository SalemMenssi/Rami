// Drag-to-reorder handler for hand cards.
// Delegates all position management to HandView — this class only handles
// pointer events, card visual state, and drop zone detection.
// Works with both mouse and touch via Unity EventSystem.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rami
{
    [RequireComponent(typeof(CardView))]
    public class CardDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────────────────────────────────
        private const float LiftOffsetY = 40f;   // px: upward nudge when picked up
        private const float DragScaleUp = 1.08f; // scale factor while dragging
        private const float DragAlpha   = 0.5f;  // alpha while dragging

        // ──────────────────────────────────────────────────────────────────────────
        // State
        // ──────────────────────────────────────────────────────────────────────────
        private Canvas        _canvas;
        private CanvasGroup   _canvasGroup;
        private RectTransform _rect;
        private CardView      _cardView;
        private HandView      _handView;

        // Saved at drag-start — never mutated during the drag.
        private Transform _originalParent;
        private int       _originalIndex;
        private bool      _isDragging;

        // Drop-zone detection — rebuilt once per drag to avoid per-frame FindObjectsByType.
        private readonly List<RaycastResult> _raycastResults = new();
        private static readonly List<ICardDropZone> _allDropZones = new();
        private ICardDropZone _lastHoveredZone;

        // ──────────────────────────────────────────────────────────────────────────
        // Wiring
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rect        = GetComponent<RectTransform>();
            _cardView    = GetComponent<CardView>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Called by HandView after instantiation.</summary>
        public void SetCanvas(Canvas canvas)     => _canvas   = canvas;

        /// <summary>Called by HandView after instantiation.</summary>
        public void SetHandView(HandView handView) => _handView = handView;

        // ──────────────────────────────────────────────────────────────────────────
        // Drag lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            // Save true origin — restored verbatim on cancel.
            _originalParent = transform.parent;
            _originalIndex  = _handView != null
                ? _handView.GetCardIndex(_cardView)
                : transform.GetSiblingIndex();

            // Notify HandView to open a gap at this card's current slot.
            _handView?.OnDragBegin(_cardView);

            // Move to canvas root so the card renders above everything.
            transform.SetParent(_canvas.transform, worldPositionStays: true);
            transform.SetAsLastSibling();

            // Visual state.
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha          = DragAlpha;
            _rect.localScale            = Vector3.one * DragScaleUp;

            float sf = ScaleFactor();
            _rect.anchoredPosition += new Vector2(0f, LiftOffsetY / sf);

            // Cache drop zones once — FindObjectsByType is expensive per-frame.
            _allDropZones.Clear();
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is ICardDropZone zone) _allDropZones.Add(zone);

            _lastHoveredZone = null;
            _isDragging      = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _canvas == null) return;

            // Follow the pointer.
            _rect.anchoredPosition += eventData.delta / ScaleFactor();

            // Drive insertion index / gap animation in HandView.
            if (_handView != null)
            {
                if (_handView.IsPointerInsideHand(eventData.position))
                    _handView.OnDragUpdate(_cardView, eventData.position);
                else
                    _handView.HideInsertionPointer();
            }

            // Zone hover highlight — only fires when the hovered zone changes.
            ICardDropZone hovered = FindDropZoneUnderPointer(eventData);
            if (hovered != _lastHoveredZone)
            {
                _lastHoveredZone?.OnCardDragEnd(_cardView);
                hovered?.OnCardDragOver(_cardView);
                _lastHoveredZone = hovered;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Restore visual state unconditionally.
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha          = 1f;
            _rect.localScale            = Vector3.one;

            _lastHoveredZone?.OnCardDragEnd(_cardView);
            _lastHoveredZone = null;
            _allDropZones.Clear();
            _handView?.HideInsertionPointer();

            // Record world position BEFORE any reparenting happens.
            Vector3 worldPos = _rect.position;

            ICardDropZone dropZone  = FindDropZoneUnderPointer(eventData);
            bool isHandZone         = dropZone is HandDropZone;
            bool insideHand         = _handView != null
                                    && _handView.IsPointerInsideHand(eventData.position);

            if (dropZone != null && !isHandZone && dropZone.CanAcceptDrop(_cardView))
            {
                // ── External zone (draft, meld, discard, …) ──────────────────────
                dropZone.OnCardDropped(_cardView);
            }
            else if (insideHand || isHandZone)
            {
                // ── In-hand reorder ───────────────────────────────────────────────
                // CommitDrop reparents and position-overrides so Update() lerps it in.
                _handView?.CommitDrop(_cardView, worldPos);
            }
            else
            {
                // ── No valid zone — return to original slot ───────────────────────
                if (_handView != null)
                    _handView.CancelDrop(_cardView, _originalIndex, worldPos);
                else
                    RestoreFallback();
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void RestoreFallback()
        {
            // Safety: if somehow HandView is missing, re-parent back to original.
            if (_originalParent != null)
                transform.SetParent(_originalParent, worldPositionStays: false);
        }

        private ICardDropZone FindDropZoneUnderPointer(PointerEventData eventData)
        {
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            foreach (var r in _raycastResults)
            {
                var zone = r.gameObject.GetComponentInParent<ICardDropZone>();
                if (zone != null) return zone;
            }
            return null;
        }

        private float ScaleFactor() =>
            _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
    }
}
