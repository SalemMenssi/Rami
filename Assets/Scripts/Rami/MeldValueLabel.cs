// Displays the computed point value of a meld group as a small label badge
// positioned above the meld card group. Instantiated by PublicMeldZone.
using UnityEngine;
using TMPro;

namespace Rami
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MeldValueLabel : MonoBehaviour
    {
        private TextMeshProUGUI _label;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        /// <summary>Sets the displayed value text.</summary>
        public void SetValue(int points)
        {
            if (_label != null)
                _label.text = $"{points} pts";
        }
    }
}
