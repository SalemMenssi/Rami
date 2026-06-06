// Renders a player's committed public melds on the table.
// Each meld group now shows a point-value badge above it.
// During a Man3a turn the human can tap a meld group to select it as the
// target for adding cards from their revealed hand.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rami
{
    /// <summary>
    /// Manages the visual meld groups for one player's public (committed) melds.
    /// Supports click-to-select a meld group during Man3a add-card interaction.
    /// Each meld group shows its point value as a badge label.
    /// </summary>
    public class PublicMeldZone : MonoBehaviour
    {
        [SerializeField] private GameObject _cardPrefab;

        /// <summary>Optional z-rotation for side-player meld zones (left=90, right=-90).</summary>
        [SerializeField] private float _cardRotationZ = 0f;

        // Colours.
        private static readonly Color HighlightColor = new Color(1f, 0.85f, 0f, 0.40f);
        private static readonly Color NormalColor     = new Color(0f, 0f, 0f, 0f);
        private static readonly Color ValueLabelColor = new Color(1f, 0.95f, 0.4f, 1f);

        // Live copy of this player's meld data.
        private List<List<PlayingCard>> _melds = new();

        // Currently selected meld index (-1 = none).
        private int _selectedMeldIndex = -1;

        // Callback: invoked with the meld index when a group is tapped.
        private System.Action<int> _onMeldSelected;

        // Child GameObjects, one wrapper per meld group (contains value label + card row).
        private readonly List<GameObject> _meldGroupGos = new();

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the zone from the full meld list.
        /// <paramref name="onMeldSelected"/> receives the tapped meld index; pass null when not interactive.
        /// </summary>
        public void Refresh(List<List<PlayingCard>> melds, System.Action<int> onMeldSelected = null)
        {
            _melds             = melds ?? new List<List<PlayingCard>>();
            _onMeldSelected    = onMeldSelected;
            _selectedMeldIndex = -1;
            RebuildVisuals();
        }

        /// <summary>Highlights the meld at <paramref name="index"/> and clears the previous selection.</summary>
        public void SelectMeld(int index)
        {
            _selectedMeldIndex = index;
            ApplyHighlight();
        }

        /// <summary>Clears meld selection highlight.</summary>
        public void ClearSelection()
        {
            _selectedMeldIndex = -1;
            ApplyHighlight();
        }

        /// <summary>Returns the data of the currently selected meld, or null.</summary>
        public List<PlayingCard> GetSelectedMeld() =>
            (_selectedMeldIndex >= 0 && _selectedMeldIndex < _melds.Count)
                ? _melds[_selectedMeldIndex]
                : null;

        public int SelectedMeldIndex => _selectedMeldIndex;

        // ──────────────────────────────────────────────────────────────────────────
        // Visual rebuild
        // ──────────────────────────────────────────────────────────────────────────

        private void RebuildVisuals()
        {
            foreach (var go in _meldGroupGos)
                if (go != null) Destroy(go);
            _meldGroupGos.Clear();

            if (_cardPrefab == null) return;

            for (int i = 0; i < _melds.Count; i++)
            {
                int capturedIndex = i;
                var meld          = _melds[i];
                int meldPts       = RamiRuleValidator.MeldPointValue(meld);

                // Outer wrapper: vertical stack of [value label + card row].
                var wrapGo = new GameObject($"MeldGroup_{i}");
                wrapGo.transform.SetParent(transform, false);

                var wrapRt = wrapGo.AddComponent<RectTransform>();
                wrapRt.sizeDelta = new Vector2(meld.Count * 48f + 8f, 90f);

                var wrapVG = wrapGo.AddComponent<VerticalLayoutGroup>();
                wrapVG.childAlignment     = TextAnchor.UpperCenter;
                wrapVG.spacing            = 2f;
                wrapVG.childControlWidth  = true;
                wrapVG.childControlHeight = false;
                wrapVG.padding            = new RectOffset(0, 0, 2, 2);

                // Transparent button background so the whole wrapper is tappable.
                var wrapImg   = wrapGo.AddComponent<Image>();
                wrapImg.color = NormalColor;
                var wrapBtn   = wrapGo.AddComponent<Button>();
                wrapBtn.targetGraphic = wrapImg;
                wrapBtn.onClick.AddListener(() => OnMeldGroupClicked(capturedIndex));

                // ── Point value label ──────────────────────────────────────────────
                var labelGo = new GameObject("ValueLabel");
                labelGo.transform.SetParent(wrapGo.transform, false);

                var labelRt      = labelGo.AddComponent<RectTransform>();
                labelRt.sizeDelta = new Vector2(0f, 18f);

                var labelLE           = labelGo.AddComponent<LayoutElement>();
                labelLE.preferredHeight = 18f;

                var tmp        = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text       = $"{meldPts} pts";
                tmp.fontSize   = 13f;
                tmp.color      = ValueLabelColor;
                tmp.alignment  = TextAlignmentOptions.Center;
                tmp.fontStyle  = FontStyles.Bold;

                // ── Card row ──────────────────────────────────────────────────────
                var rowGo = new GameObject("CardRow");
                rowGo.transform.SetParent(wrapGo.transform, false);

                var rowRt      = rowGo.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(0f, 68f);

                var rowLE           = rowGo.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 68f;

                var rowHG = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowHG.spacing            = -16f;
                rowHG.childAlignment     = TextAnchor.MiddleCenter;
                rowHG.childControlWidth  = false;
                rowHG.childControlHeight = false;

                foreach (var card in meld)
                {
                    var go  = Instantiate(_cardPrefab, rowGo.transform);
                    var rt2 = go.GetComponent<RectTransform>();
                    if (rt2 != null)
                    {
                        rt2.sizeDelta = new Vector2(44f, 64f);
                        if (!Mathf.Approximately(_cardRotationZ, 0f))
                            rt2.localRotation = Quaternion.Euler(0f, 0f, _cardRotationZ);
                    }

                    var cv = go.GetComponent<CardView>();
                    if (cv != null)
                        cv.Setup(card, faceUp: true, interactable: false, onClick: null);
                    else
                    {
                        var img = go.GetComponent<Image>();
                        if (img != null) img.sprite = CardSpriteLibrary.GetFaceSprite(card);
                    }
                }

                _meldGroupGos.Add(wrapGo);
            }

            ApplyHighlight();
        }

        private void OnMeldGroupClicked(int index)
        {
            if (_onMeldSelected == null) return;
            _selectedMeldIndex = (_selectedMeldIndex == index) ? -1 : index;
            ApplyHighlight();
            _onMeldSelected.Invoke(_selectedMeldIndex);
        }

        private void ApplyHighlight()
        {
            for (int i = 0; i < _meldGroupGos.Count; i++)
            {
                var go = _meldGroupGos[i];
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img != null)
                    img.color = (i == _selectedMeldIndex) ? HighlightColor : NormalColor;
            }
        }
    }
}
