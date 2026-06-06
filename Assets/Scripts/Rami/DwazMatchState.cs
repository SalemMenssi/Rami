// Dwaz match state.
// Scoring rules:
//   • Target is a small positive score (21 / 31 / 40) — first player to reach it wins.
//   • When a player wins a round they earn +3 pts.
//   • The player sitting immediately to the right of the winner (next in turn order) earns
//     one Zal3a token (−2 pts each).
//   • Every other player earns +1 pt per joker (Diamond 2 counts as joker) still in hand.
//   • Zal3a tokens cannot be "converted" until the owning player has accumulated enough
//     raw points to cover them: a player's effective score = RawScore − (ZAL3ACount × 2).
//     Zal3a tokens are burned off at −2 each before the player can reach a "pure" target.
//     e.g. target 31, raw=20, zal3a=5 → effective=10, must reach raw=31+(5×2)=41 to win.
//   • Match win condition: first player whose effective score reaches or exceeds the target.
// Diamond 2 as Joker is enforced in DwazRuleValidator.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public class DwazMatchState
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Inner record
        // ─────────────────────────────────────────────────────────────────────────

        public class PlayerRecord
        {
            public string Name      { get; }
            public bool   IsHuman   { get; }

            /// <summary>Accumulated positive score (winner bonuses, joker penalties received).</summary>
            public int RawScore     { get; set; }

            /// <summary>Number of Zal3a tokens the player holds (each represents −2 pts).</summary>
            public int ZAL3ACount   { get; set; }

            /// <summary>
            /// Effective score after Zal3a deduction.
            /// A player can only reach the target once their raw score covers all Zal3as.
            /// </summary>
            public int EffectiveScore => RawScore - ZAL3ACount * 2;

            /// <summary>True if this player has shown melds this round (Man3a / Farcha).</summary>
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

        /// <summary>Target score to win the match (21 / 31 / 40).</summary>
        public int WinTarget { get; private set; }

        public int CurrentRound { get; private set; } = 1;

        public IReadOnlyList<PlayerRecord> Players => _players;

        public bool   MatchOver   { get; private set; }
        public string MatchWinner { get; private set; }

        private readonly List<PlayerRecord> _players = new();

        // ─────────────────────────────────────────────────────────────────────────
        // Setup
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Initialises the match with player hands and the chosen win target.</summary>
        public void Setup(IEnumerable<PlayerHand> hands, int winTarget)
        {
            _players.Clear();
            foreach (var h in hands)
                _players.Add(new PlayerRecord(h.PlayerName, h.IsHuman));

            WinTarget    = winTarget;
            CurrentRound = 1;
            MatchOver    = false;
            MatchWinner  = null;

            Debug.Log($"[Dwaz] Match setup — {_players.Count} players, target {winTarget} pts.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Round helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>All players are always active in Dwaz (no elimination).</summary>
        public bool IsActive(string playerName) => GetRecord(playerName) != null;

        /// <summary>Resets per-round flags.</summary>
        public void ResetRoundFlags()
        {
            foreach (var p in _players)
                p.HasShownMelds = false;
        }

        /// <summary>
        /// Applies the Dwaz round result:
        ///   • Winner: +3 pts.
        ///   • Next player (right-of-winner in <paramref name="orderedNames"/>): +1 Zal3a.
        ///   • Every other player: +1 pt per joker in hand.
        /// Returns true when the match is over.
        /// </summary>
        public bool ApplyRoundResult(DwazRoundResult result, IReadOnlyList<string> orderedNames)
        {
            // Winner +3
            var winnerRec = GetRecord(result.WinnerName);
            if (winnerRec != null)
            {
                winnerRec.RawScore += 3;
                Debug.Log($"[Dwaz] {winnerRec.Name} wins round → RawScore {winnerRec.RawScore}, Effective {winnerRec.EffectiveScore}");
            }

            // Next player gets a Zal3a
            string nextName = NextPlayerName(result.WinnerName, orderedNames);
            var nextRec = nextName != null ? GetRecord(nextName) : null;
            if (nextRec != null)
            {
                nextRec.ZAL3ACount++;
                Debug.Log($"[Dwaz] {nextRec.Name} gets a Zal3a → total {nextRec.ZAL3ACount} (effective {nextRec.EffectiveScore})");
            }

            // All others: +1 per joker in hand
            foreach (var kv in result.JokersInHand)
            {
                if (kv.Key == result.WinnerName) continue;
                var rec = GetRecord(kv.Key);
                if (rec == null) continue;
                rec.RawScore += kv.Value;
                Debug.Log($"[Dwaz] {rec.Name} has {kv.Value} joker(s) → +{kv.Value} pts, RawScore {rec.RawScore}");
            }

            // Check win: first player whose effective score >= WinTarget
            var winner = _players.FirstOrDefault(p => p.EffectiveScore >= WinTarget);
            if (winner != null)
            {
                MatchOver   = true;
                MatchWinner = winner.Name;
                Debug.Log($"[Dwaz] Match over — {MatchWinner} reached {winner.EffectiveScore} pts (target {WinTarget}).");
                return true;
            }

            CurrentRound++;
            ResetRoundFlags();
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────

        public PlayerRecord GetRecord(string name) =>
            _players.FirstOrDefault(p => p.Name == name);

        /// <summary>Returns the name of the player seated after <paramref name="currentName"/> in turn order.</summary>
        private static string NextPlayerName(string currentName, IReadOnlyList<string> orderedNames)
        {
            if (orderedNames == null || orderedNames.Count == 0) return null;
            int idx = -1;
            for (int i = 0; i < orderedNames.Count; i++)
            {
                if (orderedNames[i] == currentName) { idx = i; break; }
            }
            if (idx < 0) return null;
            return orderedNames[(idx + 1) % orderedNames.Count];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Result DTO produced by DwazRoundScoreCalculator
    // ─────────────────────────────────────────────────────────────────────────────

    public class DwazRoundResult
    {
        /// <summary>Name of the player who won the round.</summary>
        public string WinnerName { get; set; }

        /// <summary>Maps playerName → number of jokers (Diamond 2 included) in their hand at round end.</summary>
        public Dictionary<string, int> JokersInHand { get; set; } = new();
    }
}
