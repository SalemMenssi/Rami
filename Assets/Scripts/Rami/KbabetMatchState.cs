// Kbabet match state.
// Scoring rules:
//   • No elimination: the match continues for a configured number of rounds,
//     or until all players agree to stop. The player with the LOWEST total score wins.
//   • Each round the winner (lowest hand) scores 0. Others score their hand value.
//   • Tafdhila (optional): if the round winner held zero jokers in their melds (jokers
//     may exist in hand as long as they are NOT part of any submitted combination),
//     they may assign a 50-point penalty to any one opponent. The winner may claim
//     as many Tafdhilas in a single match as they earn; there is no cap.
//   • Jokers not in any combination are treated as normal cards for hand-value scoring.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public class KbabetMatchState
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Inner record
        // ─────────────────────────────────────────────────────────────────────────

        public class PlayerRecord
        {
            public string Name       { get; }
            public bool   IsHuman    { get; }
            public int    TotalScore { get; set; }

            /// <summary>True if this player has shown melds this round.</summary>
            public bool HasShownMelds { get; set; }

            public PlayerRecord(string name, bool isHuman)
            {
                Name    = name;
                IsHuman = isHuman;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────────────────────────────────

        public int CurrentRound { get; private set; } = 1;

        public IReadOnlyList<PlayerRecord> Players => _players;

        /// <summary>Whether Tafdhila is active for this match.</summary>
        public bool TafdhilaEnabled { get; private set; }

        public bool   MatchOver   { get; private set; }
        public string MatchWinner { get; private set; }

        private readonly List<PlayerRecord> _players = new();

        // ─────────────────────────────────────────────────────────────────────────
        // Setup
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Initialises the match.</summary>
        public void Setup(IEnumerable<PlayerHand> hands, bool tafdhilaEnabled)
        {
            _players.Clear();
            foreach (var h in hands)
                _players.Add(new PlayerRecord(h.PlayerName, h.IsHuman));

            TafdhilaEnabled = tafdhilaEnabled;
            CurrentRound    = 1;
            MatchOver       = false;
            MatchWinner     = null;

            Debug.Log($"[Kbabet] Match setup — {_players.Count} players, Tafdhila: {tafdhilaEnabled}.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Round helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>All Kbabet players are always active.</summary>
        public bool IsActive(string playerName) => GetRecord(playerName) != null;

        /// <summary>Resets per-round flags.</summary>
        public void ResetRoundFlags()
        {
            foreach (var p in _players)
                p.HasShownMelds = false;
        }

        /// <summary>
        /// Applies a Kbabet round result.
        /// <paramref name="roundScores"/>: playerName → round penalty (0 for winner).
        /// <paramref name="tafdhilaTargetName"/>: the opponent who receives the +50 Tafdhila penalty,
        ///   or null if Tafdhila was not used / not available this round.
        /// </summary>
        public void ApplyRoundScores(
            Dictionary<string, int> roundScores,
            string tafdhilaTargetName = null)
        {
            foreach (var kv in roundScores)
            {
                var rec = GetRecord(kv.Key);
                if (rec == null) continue;
                rec.TotalScore += kv.Value;
                Debug.Log($"[Kbabet] {kv.Key} +{kv.Value} → total {rec.TotalScore}");
            }

            if (TafdhilaEnabled && !string.IsNullOrEmpty(tafdhilaTargetName))
            {
                const int TafdhilaPenalty = 50;
                var target = GetRecord(tafdhilaTargetName);
                if (target != null)
                {
                    target.TotalScore += TafdhilaPenalty;
                    Debug.Log($"[Kbabet] Tafdhila! {tafdhilaTargetName} +{TafdhilaPenalty} pts.");
                }
            }

            CurrentRound++;
            ResetRoundFlags();
        }

        /// <summary>
        /// Explicitly ends the match. The player with the lowest TotalScore wins.
        /// Call this when the session ends (all players agreed to stop).
        /// </summary>
        public void EndMatch()
        {
            MatchOver   = true;
            MatchWinner = _players.OrderBy(p => p.TotalScore).First().Name;
            Debug.Log($"[Kbabet] Match ended — winner (lowest score): {MatchWinner}");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────

        public PlayerRecord GetRecord(string name) =>
            _players.FirstOrDefault(p => p.Name == name);

        /// <summary>The current leader (lowest score player).</summary>
        public string CurrentLeader =>
            _players.OrderBy(p => p.TotalScore).First().Name;
    }
}
