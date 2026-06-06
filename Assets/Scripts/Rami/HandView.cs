// Manages a player's hand of cards with fully manual positioning.
// HorizontalLayoutGroup is disabled at runtime — all positions are calculated
// and smoothly lerped here so drag-to-reorder is stable and gap animation works.
// Bot hands (no PlayerHand / no drag) are also positioned manually for consistency.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rami
{
    public class HandView : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector
        // ──────────────────────────────────────────────────────────────────────────
        [SerializeField] private GameObject _cardPrefab;

        [Tooltip("Z-axis rotation applied to every card (for side bot hands).")]
        [SerializeField] private float _cardRotationZ = 0f;

        [Tooltip("Card width in pixels at reference resolution.")]
        [SerializeField] private float _cardWidth = 80f;

        [Tooltip("Card height in pixels at reference resolution.")]
        [SerializeField] private float _cardHeight = 112f;

        [Tooltip("Spacing between card centres when hand has ≤10 cards.")]
        [SerializeField] private float _maxSpacing = -12f;

        [Tooltip("Spacing between card centres when hand has 16+ cards.")]
        [SerializeField] private float _minSpacing = -40f;

        [Tooltip("Speed at which non-dragging cards slide to their target positions.")]
        [SerializeField] [Range(8f, 40f)] private float _slideLerpSpeed = 22f;

        // ──────────────────────────────────────────────────────────────────────────
        // Private — hand state
        // ──────────────────────────────────────────────────────────────────────────

        // Logical order of all cards including the one being dragged (at its virtual slot).
        private readonly List<CardView> _cardViews = new();

        // Per-card target anchoredPosition (relative to hand container centre).
        private readonly Dictionary<CardView, Vector2> _targets = new();

        private System.Action<CardView> _onCardClicked;
        private PlayerHand              _playerHand;
        private Canvas                  _rootCanvas;

        private RectTransform          _rt;
        private HorizontalLayoutGroup  _layoutGroup;  // disabled at runtime
        private HandInsertionIndicator _indicator;

        // ── Drag state ────────────────────────────────────────────────────────────
        private CardView _dragging;
        private int      _dragInsertIndex = -1;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rt          = GetComponent<RectTransform>();
            _layoutGroup = GetComponent<HorizontalLayoutGroup>();

            // Disable the built-in layout group permanently.
            // We manage all child positions ourselves so animations are never overwritten.
            if (_layoutGroup != null) _layoutGroup.enabled = false;
        }

        private void Update()
        {
            // Slide every non-dragging card toward its target position each frame.
            float factor = Mathf.Clamp01(Time.unscaledDeltaTime * _slideLerpSpeed);
            foreach (var cv in _cardViews)
            {
                if (cv == null || cv == _dragging) continue;
                if (!_targets.TryGetValue(cv, out var target)) continue;

                var cvRt = cv.GetComponent<RectTransform>();
                if (cvRt == null) continue;

                cvRt.anchoredPosition = Vector2.Lerp(cvRt.anchoredPosition, target, factor);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Initialisation & refresh
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Binds event handlers and optional drag support (human hand only).</summary>
        public void Init(System.Action<CardView> onCardClicked,
                         PlayerHand playerHand = null,
                         Canvas rootCanvas     = null)
        {
            _onCardClicked = onCardClicked;
            _playerHand    = playerHand;
            _rootCanvas    = rootCanvas;
        }

        /// <summary>Rebuilds the entire hand display from scratch.</summary>
        public void Refresh(IReadOnlyList<PlayingCard> cards, bool faceUp, bool interactable)
        {
            foreach (var cv in _cardViews)
                if (cv != null) Destroy(cv.gameObject);
            _cardViews.Clear();
            _targets.Clear();
            _dragging        = null;
            _dragInsertIndex = -1;

            if (cards == null) return;

            bool enableDrag = interactable && _playerHand != null;
            Canvas canvas   = _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();

            foreach (var card in cards)
            {
                var go    = Instantiate(_cardPrefab, transform);
                var cardRt = go.GetComponent<RectTransform>();
                CentreAnchor(cardRt);

                if (!Mathf.Approximately(_cardRotationZ, 0f))
                    cardRt.localRotation = Quaternion.Euler(0f, 0f, _cardRotationZ);

                var cv = go.GetComponent<CardView>();
                cv.Setup(card, faceUp, interactable, _onCardClicked);
                _cardViews.Add(cv);

                if (enableDrag)
                {
                    var drag = go.GetComponent<CardDragHandler>();
                    if (drag == null) drag = go.AddComponent<CardDragHandler>();
                    drag.SetCanvas(canvas);
                    drag.SetHandView(this);
                }
            }

            EnsureIndicator();
            RecalculateTargets();
            SnapAllToTargets();     // instant placement on first show
        }

        /// <summary>Overrides the card rotation (e.g. on layout change).</summary>
        public void SetRotation(float rotationZ) => _cardRotationZ = rotationZ;

        // ──────────────────────────────────────────────────────────────────────────
        // Drag API — called by CardDragHandler
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Returns the logical index of <paramref name="card"/> in the hand list.</summary>
        public int GetCardIndex(CardView card) => _cardViews.IndexOf(card);

        /// <summary>
        /// Called once when a drag starts.
        /// Records the dragging card so Update() skips it and opens its gap in the layout.
        /// </summary>
        public void OnDragBegin(CardView card)
        {
            _dragging        = card;
            _dragInsertIndex = _cardViews.IndexOf(card);
            RecalculateTargets();
        }

        /// <summary>
        /// Called every drag frame when the pointer is inside the hand.
        /// Moves the card to its new logical slot and animates other cards to open/close the gap.
        /// </summary>
        public void OnDragUpdate(CardView card, Vector2 screenPos)
        {
            int newIndex = ComputeInsertionIndex(screenPos);
            if (newIndex == _dragInsertIndex) return;

            // Move card to the new logical position in the list.
            _cardViews.Remove(card);
            _cardViews.Insert(Mathf.Clamp(newIndex, 0, _cardViews.Count), card);
            _dragInsertIndex = _cardViews.IndexOf(card);

            // Recalculate — other cards will lerp to fill / open the gap in Update().
            RecalculateTargets();
            PlaceIndicatorAt(_dragInsertIndex);
        }

        /// <summary>
        /// Commits the drop inside the hand.
        /// Re-parents the card, sets its position to <paramref name="dragWorldPos"/>,
        /// and lets Update() lerp it smoothly to its layout target.
        /// </summary>
        public void CommitDrop(CardView card, Vector3 dragWorldPos)
        {
            _dragging        = null;
            _dragInsertIndex = -1;
            HideInsertionPointer();

            int targetIdx = _cardViews.IndexOf(card);
            if (targetIdx < 0)
            {
                _cardViews.Add(card);
                targetIdx = _cardViews.Count - 1;
            }

            ReparentCard(card, targetIdx, dragWorldPos);
            RecalculateTargets();
            // card.anchoredPosition is now at dragWorldPos in parent-local space;
            // Update() lerps it to _targets[card] automatically.
            SyncDataOrder();
        }

        /// <summary>
        /// Cancels the drag and returns the card to <paramref name="originalIndex"/>.
        /// Re-parents the card and lets Update() lerp it back smoothly.
        /// </summary>
        public void CancelDrop(CardView card, int originalIndex, Vector3 dragWorldPos)
        {
            _dragging        = null;
            _dragInsertIndex = -1;
            HideInsertionPointer();

            // Restore logical order.
            _cardViews.Remove(card);
            _cardViews.Insert(Mathf.Clamp(originalIndex, 0, _cardViews.Count), card);

            int restoredIdx = _cardViews.IndexOf(card);
            ReparentCard(card, restoredIdx, dragWorldPos);
            RecalculateTargets();
            SyncDataOrder();
        }

        /// <summary>
        /// Returns true when <paramref name="screenPos"/> is inside this hand's screen rect.
        /// </summary>
        public bool IsPointerInsideHand(Vector2 screenPos)
        {
            if (_rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_rt, screenPos, GetCamera());
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Insertion pointer
        // ──────────────────────────────────────────────────────────────────────────

        public void HideInsertionPointer() => _indicator?.Hide();

        private void EnsureIndicator()
        {
            if (_indicator != null) return;
            _indicator = HandInsertionIndicator.Create(transform, _cardHeight);
        }

        private void PlaceIndicatorAt(int insertIndex)
        {
            EnsureIndicator();
            _indicator.transform.SetAsLastSibling();

            // The dragging card occupies a virtual slot at insertIndex in _targets.
            // Its target X is exactly where the indicator belongs.
            float localX = _targets.TryGetValue(_dragging, out var t) ? t.x : SlotX(insertIndex);
            _indicator.PlaceAt(localX);
            _indicator.Show();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Position calculation
        // ──────────────────────────────────────────────────────────────────────────

        private float CurrentSpacing()
        {
            float pct = Mathf.InverseLerp(10f, 16f, _cardViews.Count);
            return Mathf.Lerp(_maxSpacing, _minSpacing, pct);
        }

        private float CardStep() => _cardWidth + CurrentSpacing();

        /// <summary>X (local) of slot <paramref name="index"/> assuming current card count.</summary>
        private float SlotX(int index)
        {
            int count = _cardViews.Count;
            if (count == 0) return 0f;
            float step      = CardStep();
            float startX    = -(count - 1) * step * 0.5f;
            return startX + index * step;
        }

        /// <summary>Writes the target anchoredPosition for every card in the logical list.</summary>
        private void RecalculateTargets()
        {
            int count = _cardViews.Count;
            if (count == 0) return;

            float step   = CardStep();
            float startX = -(count - 1) * step * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var cv = _cardViews[i];
                if (cv == null) continue;
                _targets[cv] = new Vector2(startX + i * step, 0f);
            }
        }

        /// <summary>Teleports every card to its target instantly (used on Refresh).</summary>
        private void SnapAllToTargets()
        {
            foreach (var cv in _cardViews)
            {
                if (cv == null) continue;
                var cvRt = cv.GetComponent<RectTransform>();
                if (cvRt == null) continue;
                if (_targets.TryGetValue(cv, out var target))
                    cvRt.anchoredPosition = target;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Insertion index
        // ──────────────────────────────────────────────────────────────────────────

        private int ComputeInsertionIndex(Vector2 screenPos)
        {
            Camera cam = GetCamera();

            for (int i = 0; i < _cardViews.Count; i++)
            {
                var cv = _cardViews[i];
                if (cv == null || cv == _dragging) continue;

                var cvRt = cv.GetComponent<RectTransform>();
                if (cvRt == null) continue;

                // Compare against the card's current visual position (mid-lerp),
                // giving responsive index tracking even while cards are animating.
                Vector3 cardScreen = RectTransformUtility.WorldToScreenPoint(cam, cvRt.position);
                if (screenPos.x < cardScreen.x)
                    return i;
            }
            return _cardViews.Count;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Re-parent helper
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Re-parents <paramref name="card"/> into this hand container at <paramref name="index"/>,
        /// then overrides its anchoredPosition to correspond to <paramref name="dragWorldPos"/>
        /// so the lerp in Update() starts from the visual drag position.
        /// </summary>
        private void ReparentCard(CardView card, int index, Vector3 dragWorldPos)
        {
            var cardRt = card.GetComponent<RectTransform>();

            // Reparent without world-position preservation so Unity does not try to
            // compute a complex offset from the canvas root to the hand container.
            card.transform.SetParent(transform, worldPositionStays: false);
            CentreAnchor(cardRt);

            // Apply sibling indices for every card in logical order so that
            // render depth matches the hand layout: left cards are lower siblings,
            // right cards are higher siblings (rendered on top of their left neighbours).
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var cv = _cardViews[i];
                if (cv != null)
                    cv.transform.SetSiblingIndex(i);
            }

            // Keep the indicator always on top of all cards.
            _indicator?.transform.SetAsLastSibling();

            // Now override the anchoredPosition to match the drag world-position.
            // Setting _rect.position (world space) lets Unity convert it correctly
            // into the new parent's local coordinate system.
            if (cardRt != null)
                cardRt.position = dragWorldPos;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Data sync
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Pushes the current visual order to the underlying PlayerHand model.</summary>
        private void SyncDataOrder()
        {
            if (_playerHand == null) return;

            var newOrder = new List<PlayingCard>();
            foreach (var cv in _cardViews)
                if (cv != null && cv.Card != null)
                    newOrder.Add(cv.Card);

            _playerHand.SetOrder(newOrder);
        }

        /// <summary>Compatibility shim called by HandDropZone.</summary>
        public void OnCardReordered() => SyncDataOrder();

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private Camera GetCamera()
        {
            return _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _rootCanvas.worldCamera : null;
        }

        /// <summary>
        /// Sets anchor min/max and pivot to centre (0.5, 0.5) so that anchoredPosition
        /// is always relative to the parent's centre — independent of Canvas scaling.
        /// </summary>
        private static void CentreAnchor(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Selection helpers (unchanged)
        // ──────────────────────────────────────────────────────────────────────────

        public List<PlayingCard> GetSelectedCards()
        {
            var result = new List<PlayingCard>();
            foreach (var cv in _cardViews)
                if (cv != null && cv.IsSelected) result.Add(cv.Card);
            return result;
        }

        public PlayingCard GetFirstSelected()
        {
            foreach (var cv in _cardViews)
                if (cv != null && cv.IsSelected) return cv.Card;
            return null;
        }

        public void ClearAllSelections()
        {
            foreach (var cv in _cardViews) cv?.SetSelected(false);
        }

        public bool ToggleSelection(CardView target)
        {
            bool nowSelected = !target.IsSelected;
            target.SetSelected(nowSelected);
            return nowSelected;
        }

        public void MarkStagedCards(HashSet<string> stagedInstanceIds)
        {
            foreach (var cv in _cardViews)
            {
                if (cv == null) continue;
                cv.SetDimmed(stagedInstanceIds.Contains(cv.Card.InstanceId));
            }
        }

        public void ClearDimmed()
        {
            foreach (var cv in _cardViews) cv?.SetDimmed(false);
        }
    }
}
