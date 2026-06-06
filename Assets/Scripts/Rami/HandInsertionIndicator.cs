// Visual insertion pointer shown between hand cards during a drag-to-reorder.
// Rendered as a thin vertical glowing bar in gold/yellow.
// Created and destroyed dynamically by HandView — no prefab required.
using UnityEngine;
using UnityEngine.UI;

namespace Rami
{
    public class HandInsertionIndicator : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────────────────────────────────
        private const float BarWidth      = 6f;
        private const float GlowWidth     = 14f;
        private const float HeightPadding = 8f;   // extra px above/below card height

        private static readonly Color BarColor  = new Color(1.00f, 0.82f, 0.10f, 1.00f);   // gold
        private static readonly Color GlowColor = new Color(1.00f, 0.82f, 0.10f, 0.30f);   // soft halo

        // ──────────────────────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────────────────────
        private RectTransform _rt;
        private Image         _bar;
        private Image         _glow;

        // ──────────────────────────────────────────────────────────────────────────
        // Factory
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the indicator as a child of <paramref name="parent"/> and hides it immediately.
        /// </summary>
        public static HandInsertionIndicator Create(Transform parent, float cardHeight)
        {
            var go = new GameObject("HandInsertionIndicator", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var indicator = go.AddComponent<HandInsertionIndicator>();
            indicator.Build(cardHeight);
            indicator.gameObject.SetActive(false);
            return indicator;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Build
        // ──────────────────────────────────────────────────────────────────────────

        private void Build(float cardHeight)
        {
            _rt = GetComponent<RectTransform>();

            float totalHeight = cardHeight + HeightPadding;

            // Outer glow layer (behind the bar, wider).
            var glowGo = new GameObject("Glow", typeof(RectTransform));
            glowGo.transform.SetParent(transform, false);
            _glow          = glowGo.AddComponent<Image>();
            _glow.color    = GlowColor;
            var glowRt     = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.pivot     = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(GlowWidth, totalHeight);
            glowRt.anchoredPosition = Vector2.zero;

            // Core bar.
            var barGo = new GameObject("Bar", typeof(RectTransform));
            barGo.transform.SetParent(transform, false);
            _bar          = barGo.AddComponent<Image>();
            _bar.color    = BarColor;
            var barRt     = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0.5f);
            barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.pivot     = new Vector2(0.5f, 0.5f);
            barRt.sizeDelta = new Vector2(BarWidth, totalHeight);
            barRt.anchoredPosition = Vector2.zero;

            // Root rect — zero-width so it doesn't participate in layout width, but full height.
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot     = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = new Vector2(0f, totalHeight);

            // Pointer events must not block drags on cards behind it.
            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable   = false;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Positions the indicator at the given world-space X within the hand container.
        /// The Y is taken from the container's own anchored centre.
        /// </summary>
        public void PlaceAt(float localX)
        {
            if (_rt == null) return;
            _rt.anchoredPosition = new Vector2(localX, 0f);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
