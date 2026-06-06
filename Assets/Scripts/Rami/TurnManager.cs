// Manages the turn order and current-player state.
// Supports skipping eliminated players via the MatchState reference.
using System;
using System.Collections.Generic;

namespace Rami
{
    public class TurnManager
    {
        private readonly List<PlayerHand> _players = new();
        private int _currentIndex;

        private MatchState _match;  // optional; if set, eliminated players are skipped

        public PlayerHand CurrentPlayer => _players[_currentIndex];
        public int         PlayerCount   => _players.Count;

        public event Action<PlayerHand> OnTurnChanged;

        // ──────────────────────────────────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────────────────────────────────

        public void SetPlayers(IEnumerable<PlayerHand> players, int startIndex = 0)
        {
            _players.Clear();
            _players.AddRange(players);
            _currentIndex = startIndex;
        }

        public void SetMatchState(MatchState match) => _match = match;

        public IReadOnlyList<PlayerHand> Players => _players;

        // ──────────────────────────────────────────────────────────────────────────
        // Turn control
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Advances to the next active (non-eliminated) player and fires the event.</summary>
        public void NextTurn()
        {
            int attempts = 0;
            do
            {
                _currentIndex = (_currentIndex + 1) % _players.Count;
                attempts++;
                if (attempts > _players.Count) return; // all eliminated — safety guard
            }
            while (_match != null && !_match.IsActive(_players[_currentIndex].PlayerName));

            UnityEngine.Debug.Log($"[Rami] Turn started: {CurrentPlayer.PlayerName}");
            OnTurnChanged?.Invoke(CurrentPlayer);
        }

        /// <summary>Fires the turn event for the current player without advancing.</summary>
        public void StartCurrentTurn()
        {
            UnityEngine.Debug.Log($"[Rami] Turn started: {CurrentPlayer.PlayerName}");
            OnTurnChanged?.Invoke(CurrentPlayer);
        }

        /// <summary>Resets to a specific player index and fires the event.</summary>
        public void ResetToIndex(int index)
        {
            _currentIndex = index;
            // Skip eliminated players.
            int attempts = 0;
            while (_match != null && !_match.IsActive(_players[_currentIndex].PlayerName))
            {
                _currentIndex = (_currentIndex + 1) % _players.Count;
                if (++attempts > _players.Count) break;
            }
        }
    }
}
