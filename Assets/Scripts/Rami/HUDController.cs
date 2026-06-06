// Manages the top HUD bar: Menu button, Round info, Target Score, and current turn indicator.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rami
{
    /// <summary>
    /// Top bar controller. Format: [Menu] [Round N] [Target: X pts] [Turn: Player] [Scores]
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Top Bar Elements")]
        [SerializeField] private TextMeshProUGUI _roundLabel;         // "Round 3"
        [SerializeField] private TextMeshProUGUI _targetLabel;        // "Target: 800 pts"
        [SerializeField] private TextMeshProUGUI _turnLabel;          // "Turn: You"
        [SerializeField] private Button          _btnMenu;
        [SerializeField] private Button          _btnScoreboard;

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Wires menu and scoreboard button callbacks.</summary>
        public void Init(System.Action onMenu, System.Action onScoreboard)
        {
            if (_btnMenu != null)
            {
                _btnMenu.onClick.RemoveAllListeners();
                _btnMenu.onClick.AddListener(() => onMenu?.Invoke());
            }

            if (_btnScoreboard != null)
            {
                _btnScoreboard.onClick.RemoveAllListeners();
                _btnScoreboard.onClick.AddListener(() => onScoreboard?.Invoke());
            }
        }

        /// <summary>Updates the round and target score labels.</summary>
        public void SetRoundInfo(int round, int target)
        {
            if (_roundLabel  != null) _roundLabel.text  = $"Round {round}";
            if (_targetLabel != null) _targetLabel.text = $"Target: {target} pts";
        }

        /// <summary>Updates the active-turn player label.</summary>
        public void SetTurnLabel(string playerName)
        {
            if (_turnLabel != null) _turnLabel.text = $"Turn: {playerName}";
        }
    }
}
