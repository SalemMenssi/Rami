// Displays a player's remaining hand cards face-up on the table after they declare Man3a.
// The cards are shown in front of the player's position, styled like the draft-meld panel
// but laid flat on the table surface. Cards in this zone can be selected by the human
// to add onto any valid public meld (their own or any bot's).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rami
{
    /// <summary>
    /// Attached to a "Man3aReveal" container per player.
    /// Shows remaining hand cards face-up after Man3a is declared.
    /// </summary>
    public class Man3aHandReveal : MonoBehaviour
    {
        [SerializeField] private GameObject _cardPrefab;

        /// <summary>Optional z-rotation for side-player reveals (left=90, right=-90).</summary>
        [SerializeField] private float _cardRotationZ = 0f;

        private readonly List<CardView> _cardViews = new();
        private System.Action<CardView> _onCardClicked;

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the reveal zone from the given hand cards.
        /// Pass <paramref name="interactable"/>=true only for the human's own zone during their turn.
        /// </summary>
        public void Refresh(IReadOnlyList<PlayingCard> cards, bool interactable,
                            System.Action<CardView> onCardClicked = null)
        {
            _onCardClicked = onCardClicked;
            Clear();

            if (cards == null || cards.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            foreach (var card in cards)
            {
                var go = Instantiate(_cardPrefab, transform);
                var rt = go.GetComponent<RectTransform>();
                if (rt != null && !Mathf.Approximately(_cardRotationZ, 0f))
                    rt.localRotation = Quaternion.Euler(0f, 0f, _cardRotationZ);

                var cv = go.GetComponent<CardView>();
                if (cv != null)
                {
                    cv.Setup(card, faceUp: true, interactable: interactable, onClick: onCardClicked);
                    _cardViews.Add(cv);
                }
            }
        }

        /// <summary>Removes all card views without hiding the container.</summary>
        public void Clear()
        {
            foreach (var cv in _cardViews)
                if (cv != null) Destroy(cv.gameObject);
            _cardViews.Clear();
        }

        /// <summary>Hides the entire reveal zone.</summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>Returns all currently selected card views.</summary>
        public List<PlayingCard> GetSelectedCards()
        {
            var result = new List<PlayingCard>();
            foreach (var cv in _cardViews)
                if (cv != null && cv.IsSelected)
                    result.Add(cv.Card);
            return result;
        }

        /// <summary>Deselects all cards.</summary>
        public void ClearSelection()
        {
            foreach (var cv in _cardViews)
                cv?.SetSelected(false);
        }
    }
}
