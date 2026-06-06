// Holds cross-round match state: accumulated scores, elimination tracking, round number.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    /// <summary>
    /// Stores match-level data that persists across rounds.
    /// Passed around by reference so all systems share one authoritative instance.
    /// </summary>
    public class MatchState
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Constants
        // ─────────────────────────────────────────────────────────────────────────

        public const int DefaultEliminationTarget = 600;

        // ─────────────────────────────────────────────────────────────────────────
        // Per-player record
        // ─────────────────────────────────────────────────────────────────────────

        public class PlayerRecord
        {
            public string Name           { get; }
            public bool   IsHuman        { get; }
            public int    TotalScore     { get; set; }
            public bool   IsEliminated   { get; set; }

            /// <summary>True if this player has declared Man3a in the current round.</summary>
            public bool   HasShownMelds  { get; set; }

            public PlayerRecord(string name, bool isHuman)
            {
                Name    = name;
                IsHuman = isHuman;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────────────────────────────────

        public int                    EliminationTarget { get; private set; }
        public int                    CurrentRound      { get; private set; } = 1;
        public IReadOnlyList<PlayerRecord> Players       => _players;
        public bool                   MatchOver         { get; private set; }
        public string                 MatchWinner        { get; private set; }

        private readonly List<PlayerRecord> _players = new();

        // ─────────────────────────────────────────────────────────────────────────
        // Setup
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Initialises match with player names and chosen elimination target.</summary>
        public void Setup(IEnumerable<PlayerHand> hands, int eliminationTarget)
        {
            _players.Clear();
            foreach (var h in hands)
                _players.Add(new PlayerRecord(h.PlayerName, h.IsHuman));

            EliminationTarget = eliminationTarget;
            CurrentRound      = 1;
            MatchOver         = false;
            MatchWinner       = null;

            Debug.Log($"[Match] Setup — {_players.Count} players, elimination @ {eliminationTarget} pts");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Round helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Returns true if this player may still act (not eliminated).</summary>
        public bool IsActive(string playerName)
        {
            var rec = GetRecord(playerName);
            return rec != null && !rec.IsEliminated;
        }

        /// <summary>Resets per-round flags on all active players.</summary>
        public void ResetRoundFlags()
        {
            foreach (var p in _players)
                p.HasShownMelds = false;
        }

        /// <summary>
        /// Applies round scores, checks eliminations, advances round counter.
        /// Returns true when the match has ended.
        /// </summary>
        public bool ApplyRoundScores(Dictionary<string, int> roundScores)
        {
            foreach (var kv in roundScores)
            {
                var rec = GetRecord(kv.Key);
                if (rec == null || rec.IsEliminated) continue;
                rec.TotalScore += kv.Value;
                Debug.Log($"[Match] {kv.Key} round score +{kv.Value} → total {rec.TotalScore}");
            }

            // Eliminate players at or above the target.
            foreach (var rec in _players)
            {
                if (!rec.IsEliminated && rec.TotalScore >= EliminationTarget)
                {
                    rec.IsEliminated = true;
                    Debug.Log($"[Match] {rec.Name} ELIMINATED at {rec.TotalScore} pts");
                }
            }

            // Determine if match is over.
            var active = _players.Where(p => !p.IsEliminated).ToList();
            if (active.Count <= 1)
            {
                MatchOver   = true;
                MatchWinner = active.Count == 1
                    ? active[0].Name
                    : _players.OrderBy(p => p.TotalScore).First().Name; // all eliminated — lowest wins
                Debug.Log($"[Match] Match over — winner: {MatchWinner}");
                return true;
            }

            CurrentRound++;
            ResetRoundFlags();
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Convenience
        // ─────────────────────────────────────────────────────────────────────────

        public PlayerRecord GetRecord(string name) =>
            _players.FirstOrDefault(p => p.Name == name);

        public int ActivePlayerCount =>
            _players.Count(p => !p.IsEliminated);
    }
}
