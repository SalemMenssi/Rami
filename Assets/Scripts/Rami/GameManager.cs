// Central game controller — handles Standard Rami, Dwaz and Kbabet game modes.
// Mode is read from GameSettings on Start(). Each mode owns its own match-state object;
// round scoring and win detection are delegated to the appropriate calculator/state.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rami
{
    public class GameManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector references
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Controllers")]
        [SerializeField] private GameUIController    _uiController;
        [SerializeField] private BotPlayerController _botController;

        // ──────────────────────────────────────────────────────────────────────────
        // Runtime state
        // ──────────────────────────────────────────────────────────────────────────
        private DeckManager      _deck;
        private TurnManager      _turns;
        private PlayerHand       _humanPlayer;
        private PlayerHand       _leftBot;
        private PlayerHand       _topBot;
        private PlayerHand       _rightBot;
        private DraftMeldManager _draft;

        // Standard Rami match state (null when playing Dwaz or Kbabet).
        private MatchState       _match;

        // Dwaz match state (null when not in Dwaz mode).
        private DwazMatchState   _dwazMatch;

        // Kbabet match state (null when not in Kbabet mode).
        private KbabetMatchState _kbabetMatch;

        // Active game mode — set once in Start().
        private GameMode _gameMode;

        // Per-player public melds — keyed by player name.
        private readonly Dictionary<string, List<List<PlayingCard>>> _publicMelds = new();

        // Whether the human has drawn this turn.
        private bool _humanHasDrawn;

        // Whether a round/game-over sequence is in progress.
        private bool _roundOver;

        // Tracks which player did Farcha to end the round.
        private string _farchaWinnerName;

        // ──────────────────────────────────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────────────────────────────────
        private const int HandSize       = 14;
        private const int TotalPlayers   = 4;
        private const int Man3aThreshold = 61;

        // ──────────────────────────────────────────────────────────────────────────
        // Initialisation — called externally with the chosen elimination target
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Entry point from the pre-game lobby / main-menu.
        /// <paramref name="eliminationTarget"/> is the user-chosen cap (600 / 800 / 1000).
        /// Legacy overload — assumes StandardRami mode.
        /// </summary>
        public void StartMatch(int eliminationTarget)
        {
            _gameMode = GameMode.StandardRami;
            _match    = new MatchState();
            StartCoroutine(InitRound(eliminationTarget, firstRound: true));
        }

        /// <summary>Mode-aware entry point — reads from <see cref="GameSettings"/>.</summary>
        public void StartMatchFromSettings()
        {
            _gameMode = GameSettings.ChosenMode;

            switch (_gameMode)
            {
                case GameMode.Dwaz:
                    _dwazMatch = new DwazMatchState();
                    _match     = null;
                    StartCoroutine(InitRound(GameSettings.ChosenTarget, firstRound: true));
                    break;

                case GameMode.Kbabet:
                    _kbabetMatch = new KbabetMatchState();
                    _match       = null;
                    StartCoroutine(InitRound(0, firstRound: true));
                    break;

                default:
                    _match = new MatchState();
                    StartCoroutine(InitRound(GameSettings.ChosenTarget, firstRound: true));
                    break;
            }
        }

        private void Start()
        {
            // Use mode-aware launch. Falls back to Standard Rami defaults if the menu
            // was skipped (direct play from Editor without going through MainMenu).
            if (GameSettings.ChosenMode == GameMode.StandardRami &&
                GameSettings.ChosenTarget == GameSettings.DefaultEliminationTarget)
            {
                // Honour the legacy static field for backwards compatibility.
                GameSettings.ChosenTarget = MainMenuController.ChosenEliminationTarget;
            }

            StartMatchFromSettings();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round initialisation
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator InitRound(int modeTarget, bool firstRound)
        {
            _roundOver        = false;
            _humanHasDrawn    = false;
            _farchaWinnerName = null;
            _publicMelds.Clear();

            _deck  = new DeckManager();
            _draft = new DraftMeldManager();
            _deck.BuildAndShuffle();

            if (firstRound)
            {
                _humanPlayer = new PlayerHand("You",       isHuman: true);
                _leftBot     = new PlayerHand("Bot Left",  isHuman: false);
                _topBot      = new PlayerHand("Bot Top",   isHuman: false);
                _rightBot    = new PlayerHand("Bot Right", isHuman: false);
            }
            else
            {
                _humanPlayer.ClearCards();
                _leftBot.ClearCards();
                _topBot.ClearCards();
                _rightBot.ClearCards();
            }

            var allPlayers = new[] { _humanPlayer, _leftBot, _topBot, _rightBot };

            // Initialise mode-specific match state on the first round.
            if (firstRound)
            {
                switch (_gameMode)
                {
                    case GameMode.Dwaz:
                        _dwazMatch.Setup(allPlayers, modeTarget);
                        break;
                    case GameMode.Kbabet:
                        _kbabetMatch.Setup(allPlayers, GameSettings.KbabetTafdhilaEnabled);
                        break;
                    default:
                        _match.Setup(allPlayers, modeTarget);
                        break;
                }
            }
            else
            {
                // Reset per-round flags on subsequent rounds.
                switch (_gameMode)
                {
                    case GameMode.Dwaz:   _dwazMatch.ResetRoundFlags();   break;
                    case GameMode.Kbabet: _kbabetMatch.ResetRoundFlags(); break;
                    default:              _match.ResetRoundFlags();        break;
                }
            }

            // Deal cards to all players (Dwaz / Kbabet have no elimination).
            foreach (var p in allPlayers)
            {
                bool active = _gameMode == GameMode.StandardRami
                    ? _match.IsActive(p.PlayerName)
                    : true;
                if (!active) continue;
                p.AddCards(_deck.Deal(HandSize));
                Debug.Log($"[{_gameMode}] Round {CurrentRound} — dealt {HandSize} cards to {p.PlayerName}");
            }

            // Initialise public-meld containers.
            foreach (var p in allPlayers)
                _publicMelds[p.PlayerName] = new List<List<PlayingCard>>();

            // Wire turn manager — Standard Rami uses MatchState for elimination skipping;
            // other modes pass null (no elimination).
            _turns = new TurnManager();
            if (_gameMode == GameMode.StandardRami)
                _turns.SetMatchState(_match);
            _turns.SetPlayers(allPlayers, startIndex: 0);
            _turns.OnTurnChanged += HandleTurnChanged;

            _botController.Init(this);
            _uiController.Init(this, _deck, _turns, _humanPlayer, UiMatchState());
            _uiController.RefreshAll();
            _uiController.ShowRoundBanner(CurrentRound);

            yield return new WaitForSeconds(1.2f);

            _turns.StartCurrentTurn();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Turn routing
        // ──────────────────────────────────────────────────────────────────────────

        private void HandleTurnChanged(PlayerHand player)
        {
            if (_roundOver) return;

            // Skip eliminated players (belt-and-suspenders guard).
            if (!_match.IsActive(player.PlayerName))
            {
                _turns.NextTurn();
                return;
            }

            _uiController.SetTurnIndicator(player.PlayerName, _match.CurrentRound);

            if (player.IsHuman)
            {
                _humanHasDrawn = false;
                _uiController.SetHumanActionPhase(drawn: false);
            }
            else
            {
                _uiController.SetHumanActionPhase(drawn: false);
                StartCoroutine(_botController.RunTurn(player, _deck));
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Human draw actions
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Human draws from the deck.</summary>
        public void HumanDrawFromDeck()
        {
            if (_humanHasDrawn) { _uiController.ShowToast("You already drew this turn."); return; }

            var card = _deck.DrawFromDeck();
            if (card == null) { _uiController.ShowToast("Draw pile is empty!"); return; }

            _humanPlayer.AddCard(card);
            _humanHasDrawn = true;
            Debug.Log($"[Rami] Human drew {card}");

            _uiController.RefreshHand();
            _uiController.SetHumanActionPhase(drawn: true);
        }

        /// <summary>Human draws the top discard card. Requires Man3a to have been passed (61 rule).</summary>
        public void HumanDrawFromDiscard()
        {
            if (_humanHasDrawn) { _uiController.ShowToast("You already drew this turn."); return; }

            if (!HumanHasShownMelds)
            {
                _uiController.ShowToast("You must show melds (Man3a) before drawing from the discard pile.");
                return;
            }

            if (_deck.TopDiscard == null) { _uiController.ShowToast("Discard pile is empty!"); return; }

            var card = _deck.DrawFromDiscard();
            _humanPlayer.AddCard(card);
            _humanHasDrawn = true;
            Debug.Log($"[Rami] Human drew discard: {card}");

            _uiController.RefreshHand();
            _uiController.RefreshDiscard();
            _uiController.SetHumanActionPhase(drawn: true);
        }

        private bool HumanHasShownMelds =>
            _match.GetRecord(_humanPlayer.PlayerName)?.HasShownMelds ?? false;

        // ──────────────────────────────────────────────────────────────────────────
        // Draft meld actions
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Stages the currently selected cards as a private draft meld.</summary>
        public void HumanStageMeld(List<PlayingCard> selected)
        {
            if (!_humanHasDrawn) { _uiController.ShowToast("Draw a card first!"); return; }
            if (selected.Count < 3) { _uiController.ShowToast("Select at least 3 cards."); return; }

            if (!_draft.StageMeld(selected, out string reason))
            {
                _uiController.ShowToast($"Invalid meld: {reason}");
                return;
            }

            _humanPlayer.RemoveCards(selected);
            Debug.Log($"[Rami] Staged draft meld: {string.Join(", ", selected)}");

            _uiController.RefreshHand();
            _uiController.RefreshDraftPanel(_draft);
            _uiController.ClearSelection();
        }

        /// <summary>Returns all staged draft cards back to the human hand.</summary>
        public void HumanCancelDraft()
        {
            var returned = _draft.CancelAll();
            foreach (var c in returned) _humanPlayer.AddCard(c);

            Debug.Log($"[Rami] Draft cancelled — {returned.Count} cards returned to hand.");
            _uiController.RefreshHand();
            _uiController.RefreshDraftPanel(_draft);
        }

        /// <summary>Guard — melds may only be revealed via Man3a or Farcha.</summary>
        public void HumanCommitMelds()
        {
            _uiController.ShowToast("Use Man3a or Farcha to reveal melds.");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a — reveal melds publicly when total ≥ 61 pts
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Human publicly reveals draft melds (Man3a).
        /// All staged melds must be valid and total ≥ 61 points.
        /// Unlocks discard-pile drawing for future turns.
        /// </summary>
        public void HumanShowMelds()
        {
            if (!_humanHasDrawn) { _uiController.ShowToast("Draw a card first!"); return; }
            if (!_draft.HasStagedMelds) { _uiController.ShowToast("Arrange cards into draft melds first."); return; }

            foreach (var meld in _draft.StagedMelds)
            {
                if (!RamiRuleValidator.IsValidMeld(meld.ToList(), out string meldReason))
                {
                    _uiController.ShowToast($"Invalid meld: {meldReason}");
                    return;
                }
            }

            if (!_draft.MeetsMan3aThreshold(out int pts))
            {
                _uiController.ShowToast($"Man3a requires ≥ {Man3aThreshold} pts (yours: {pts}).");
                return;
            }

            var revealed = _draft.RevealDraft();
            foreach (var meld in revealed)
                RegisterPublicMeld(_humanPlayer, meld);

            var rec = _match.GetRecord(_humanPlayer.PlayerName);
            if (rec != null) rec.HasShownMelds = true;

            Debug.Log($"[Rami] Man3a! Human revealed melds — {pts} pts.");
            _uiController.RefreshDraftPanel(_draft);
            _uiController.ShowToast("Man3a! Melds shown.");
            _uiController.RefreshPlayerMelds(_humanPlayer.PlayerName, _publicMelds[_humanPlayer.PlayerName]);
            _uiController.UpdateMan3aBadge(_humanPlayer.PlayerName, shown: true);

            // Activate Man3a reveal zone and interactive meld targeting.
            _uiController.OnHumanMan3aDeclared();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Farcha — all cards in valid melds + 1 final discard → end round
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Farcha: human has all remaining cards in valid draft melds plus exactly 1 card to discard.
        /// Valid without prior Man3a. Ends the round immediately.
        /// </summary>
        public void HumanFarcha()
        {
            if (!_humanHasDrawn) { _uiController.ShowToast("Draw a card first!"); return; }

            int cardsInHand  = _humanPlayer.CardCount;
            int cardsInDraft = _draft.AllStagedCards().Count;
            int total        = cardsInHand + cardsInDraft;

            if (total < HandSize)
            {
                _uiController.ShowToast("Farcha requires all cards arranged in melds with 1 left to discard.");
                return;
            }

            if (cardsInHand != 1)
            {
                _uiController.ShowToast($"Farcha: leave exactly 1 card in hand to discard (you have {cardsInHand}).");
                return;
            }

            if (!_draft.HasStagedMelds)
            {
                _uiController.ShowToast("Farcha: arrange all other cards into draft melds first.");
                return;
            }

            foreach (var meld in _draft.StagedMelds)
            {
                if (!RamiRuleValidator.IsValidMeld(meld.ToList(), out string meldReason))
                {
                    _uiController.ShowToast($"Farcha invalid meld: {meldReason}");
                    return;
                }
            }

            // Reveal all draft melds publicly.
            var revealed = _draft.RevealDraft();
            foreach (var meld in revealed)
                RegisterPublicMeld(_humanPlayer, meld);

            // Discard the final card.
            var finalCard = _humanPlayer.Cards[0];
            _humanPlayer.RemoveCard(finalCard);
            _deck.AddToDiscard(finalCard);

            Debug.Log($"[Rami] Farcha! Human discards {finalCard}.");
            _uiController.RefreshHand();
            _uiController.RefreshDiscard();
            _uiController.RefreshPlayerMelds(_humanPlayer.PlayerName, _publicMelds[_humanPlayer.PlayerName]);

            EndRound(farchaWinnerName: _humanPlayer.PlayerName);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a — add cards from revealed hand to any public meld
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Human (with Man3a) adds <paramref name="cardsToAdd"/> to the meld at
        /// <paramref name="meldIndex"/> in <paramref name="ownerName"/>'s public zone.
        /// Validates the combined meld, then commits the change.
        /// Returns true on success.
        /// </summary>
        public bool HumanAddCardsToPublicMeld(
            List<PlayingCard> cardsToAdd,
            string ownerName,
            int meldIndex)
        {
            if (!HumanHasShownMelds)
            {
                _uiController.ShowToast("Declare Man3a first.");
                return false;
            }

            if (cardsToAdd == null || cardsToAdd.Count == 0)
            {
                _uiController.ShowToast("Select cards to add.");
                return false;
            }

            if (!_publicMelds.TryGetValue(ownerName, out var ownerMelds) ||
                meldIndex < 0 || meldIndex >= ownerMelds.Count)
            {
                _uiController.ShowToast("Invalid meld target.");
                return false;
            }

            var existingMeld = ownerMelds[meldIndex];

            if (!RamiRuleValidator.CanAddCardsToMeld(existingMeld, cardsToAdd, out string reason))
            {
                _uiController.ShowToast($"Cannot add: {reason}");
                return false;
            }

            // Verify all cards actually belong to the human's hand (or their own Man3a reveal).
            foreach (var c in cardsToAdd)
            {
                if (!_humanPlayer.RemoveCard(c))
                {
                    _uiController.ShowToast("Card not in hand.");
                    // Roll back already-removed cards.
                    foreach (var already in cardsToAdd.TakeWhile(x => x.InstanceId != c.InstanceId))
                        _humanPlayer.AddCard(already);
                    return false;
                }
            }

            // Commit: extend the meld.
            existingMeld.AddRange(cardsToAdd);
            _uiController.RefreshPlayerMelds(ownerName, ownerMelds);
            _uiController.RefreshHand();
            _uiController.RefreshMan3aReveal(_humanPlayer.PlayerName, _humanPlayer.Cards);

            Debug.Log($"[Rami] Human added {cardsToAdd.Count} card(s) to {ownerName}'s meld {meldIndex}.");

            // Check if human emptied their hand — round win via Man3a path.
            if (_humanPlayer.CardCount == 0)
            {
                Debug.Log("[Rami] Human cleared hand via Man3a add — round over.");
                EndRound(farchaWinnerName: null);
            }

            return true;
        }

        /// <summary>
        /// Farcha via Man3a: human places ALL remaining hand cards onto existing public melds,
        /// each card extending a valid meld without breaking it.
        /// <paramref name="placements"/> maps each card's InstanceId → (ownerName, meldIndex).
        /// </summary>
        public void HumanMan3aFarcha(Dictionary<string, (string ownerName, int meldIndex)> placements)
        {
            if (!HumanHasShownMelds)
            {
                _uiController.ShowToast("Declare Man3a first.");
                return;
            }

            if (_humanPlayer.CardCount == 0)
            {
                _uiController.ShowToast("Hand is already empty.");
                return;
            }

            // Group placements by target meld so we can validate each combined result once.
            // Key: ownerName + meldIndex, Value: cards being added.
            var groups = new Dictionary<string, (string owner, int idx, List<PlayingCard> cards)>();

            foreach (var kv in placements)
            {
                string mapKey = $"{kv.Value.ownerName}_{kv.Value.meldIndex}";
                if (!groups.TryGetValue(mapKey, out var entry))
                {
                    entry = (kv.Value.ownerName, kv.Value.meldIndex, new List<PlayingCard>());
                    groups[mapKey] = entry;
                }

                // Find the card in hand.
                var card = _humanPlayer.Cards.FirstOrDefault(c => c.InstanceId == kv.Key);
                if (card == null)
                {
                    _uiController.ShowToast($"Card {kv.Key} not in hand.");
                    return;
                }
                entry.cards.Add(card);
                groups[mapKey] = entry;
            }

            // Ensure every hand card is covered by a placement.
            if (placements.Count != _humanPlayer.CardCount)
            {
                _uiController.ShowToast($"All {_humanPlayer.CardCount} hand card(s) must be placed to declare Farcha.");
                return;
            }

            // Validate each target meld with additions before committing.
            foreach (var group in groups.Values)
            {
                if (!_publicMelds.TryGetValue(group.owner, out var ownerMelds) ||
                    group.idx < 0 || group.idx >= ownerMelds.Count)
                {
                    _uiController.ShowToast("Invalid meld target.");
                    return;
                }

                if (!RamiRuleValidator.CanAddCardsToMeld(ownerMelds[group.idx], group.cards, out string reason))
                {
                    _uiController.ShowToast($"Invalid placement: {reason}");
                    return;
                }
            }

            // All valid — commit.
            foreach (var group in groups.Values)
            {
                var meld = _publicMelds[group.owner][group.idx];
                foreach (var c in group.cards)
                {
                    _humanPlayer.RemoveCard(c);
                    meld.Add(c);
                }
                _uiController.RefreshPlayerMelds(group.owner, _publicMelds[group.owner]);
            }

            _uiController.RefreshHand();
            Debug.Log("[Rami] Human Man3a Farcha — all cards placed on melds.");
            EndRound(farchaWinnerName: _humanPlayer.PlayerName);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Human discard — ends the turn
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Human discards one selected card to end their turn.</summary>
        public void HumanDiscard(PlayingCard card)
        {
            if (!_humanHasDrawn) { _uiController.ShowToast("Draw a card first!"); return; }
            if (card == null)     { _uiController.ShowToast("Select a card to discard."); return; }

            if (!_humanPlayer.RemoveCard(card))
            {
                _uiController.ShowToast("Card not in hand.");
                return;
            }

            _deck.AddToDiscard(card);
            Debug.Log($"[Rami] Human discarded {card}");

            _uiController.RefreshHand();
            _uiController.RefreshDiscard();
            _uiController.ClearSelection();

            // Check empty-hand win (Man3a path — emptied hand by discarding last card).
            if (_humanPlayer.CardCount == 0 && HumanHasShownMelds)
            {
                EndRound(farchaWinnerName: null);
                return;
            }

            _turns.NextTurn();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Bot callbacks
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Called by BotPlayerController after a bot completes its turn.</summary>
        public void OnBotTurnFinished(PlayerHand bot)
        {
            if (_roundOver) return;
            _uiController.RefreshDiscard();
            _turns.NextTurn();
        }

        /// <summary>Called when a bot successfully performs Farcha.</summary>
        public void OnBotFarcha(PlayerHand bot)
        {
            if (_roundOver) return;
            _uiController.RefreshPlayerMelds(bot.PlayerName, _publicMelds[bot.PlayerName]);
            _uiController.RefreshDiscard();
            EndRound(farchaWinnerName: bot.PlayerName);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Shared meld registration (Man3a / Farcha / bot paths)
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a meld to the player's personal public meld zone and refreshes the UI.
        /// Also marks the player as having shown melds (for scoring).
        /// </summary>
        public void RegisterPublicMeld(PlayerHand player, List<PlayingCard> meld)
        {
            if (!_publicMelds.ContainsKey(player.PlayerName))
                _publicMelds[player.PlayerName] = new List<List<PlayingCard>>();

            _publicMelds[player.PlayerName].Add(meld);
            _uiController.RefreshPlayerMelds(player.PlayerName, _publicMelds[player.PlayerName]);
        }

        /// <summary>Marks a bot as Man3a shown (called from BotPlayerController).</summary>
        public void MarkBotShownMelds(PlayerHand bot)
        {
            var rec = _match.GetRecord(bot.PlayerName);
            if (rec != null) rec.HasShownMelds = true;
            _uiController.UpdateMan3aBadge(bot.PlayerName, shown: true);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round end
        // ──────────────────────────────────────────────────────────────────────────

        private void EndRound(string farchaWinnerName)
        {
            if (_roundOver) return;
            _roundOver        = true;
            _farchaWinnerName = farchaWinnerName;

            switch (_gameMode)
            {
                case GameMode.Dwaz:   EndRoundDwaz(farchaWinnerName);   break;
                case GameMode.Kbabet: EndRoundKbabet(farchaWinnerName); break;
                default:              EndRoundStandard(farchaWinnerName); break;
            }
        }

        private void EndRoundStandard(string farchaWinnerName)
        {
            var shownMap = new Dictionary<string, bool>();
            foreach (var p in _turns.Players)
            {
                var rec = _match.GetRecord(p.PlayerName);
                shownMap[p.PlayerName] = rec?.HasShownMelds ?? false;
            }

            var roundResults = RoundScoreCalculator.Calculate(
                _turns.Players, farchaWinnerName, shownMap, _match);

            var scoreDict  = RoundScoreCalculator.ToScoreDictionary(roundResults);
            bool matchOver = _match.ApplyRoundScores(scoreDict);

            Debug.Log($"[Rami] Round {_match.CurrentRound - (matchOver ? 0 : 1)} ended. " +
                      $"Farcha: {farchaWinnerName ?? "none"}. Match over: {matchOver}");

            _uiController.ShowRoundEndPanel(
                roundResults,
                _match,
                matchOver,
                onContinue: matchOver
                    ? (System.Action)ShowMatchOverScreen
                    : StartNextRound);
        }

        private void EndRoundDwaz(string winnerName)
        {
            var allPlayers = _turns.Players;

            var dwazResult = DwazRoundScoreCalculator.Calculate(winnerName, allPlayers);
            var orderedNames = allPlayers.Select(p => p.PlayerName).ToList();
            bool matchOver = _dwazMatch.ApplyRoundResult(dwazResult, orderedNames);

            Debug.Log($"[Dwaz] Round {_dwazMatch.CurrentRound - (matchOver ? 0 : 1)} ended. " +
                      $"Winner: {winnerName}. Match over: {matchOver}");

            // Reuse the standard round-end panel with a compatibility MatchState wrapper.
            // Build a temporary MatchState to pass to the existing UI (scores won't be used
            // for elimination — only for display).
            var displayMatch = BuildDwazDisplayMatch();
            var displayResults = BuildDwazDisplayResults(dwazResult, allPlayers);

            _uiController.ShowRoundEndPanel(
                displayResults,
                displayMatch,
                matchOver,
                onContinue: matchOver
                    ? (System.Action)ShowMatchOverScreen
                    : StartNextRound);
        }

        private void EndRoundKbabet(string winnerName)
        {
            var allPlayers    = _turns.Players;
            var winnerMelds   = _publicMelds.TryGetValue(winnerName, out var wm) ? wm : new List<List<PlayingCard>>();

            var kResults = KbabetRoundScoreCalculator.Calculate(
                winnerName, allPlayers, winnerMelds, out bool tafdhilaEligible);

            var scoreDict = KbabetRoundScoreCalculator.ToScoreDictionary(kResults);

            // Tafdhila: if eligible (won with no jokers in melds), human can choose a target.
            // For now, apply automatically to the highest-score opponent.
            // A future UI hook can let the human choose the target interactively.
            string tafdhilaTarget = null;
            if (tafdhilaEligible && _kbabetMatch.TafdhilaEnabled && winnerName == _humanPlayer.PlayerName)
            {
                tafdhilaTarget = _turns.Players
                    .Where(p => p.PlayerName != winnerName)
                    .Select(p => _kbabetMatch.GetRecord(p.PlayerName))
                    .OrderByDescending(r => r.TotalScore)
                    .FirstOrDefault()?.Name;
                Debug.Log($"[Kbabet] Tafdhila eligible → targeting {tafdhilaTarget}");
            }

            _kbabetMatch.ApplyRoundScores(scoreDict, tafdhilaTarget);

            Debug.Log($"[Kbabet] Round {_kbabetMatch.CurrentRound - 1} ended. Winner: {winnerName}.");

            var displayMatch   = BuildKbabetDisplayMatch();
            var displayResults = BuildKbabetDisplayResults(kResults);

            // Kbabet has no automatic match-end — show Continue every round.
            _uiController.ShowRoundEndPanel(
                displayResults,
                displayMatch,
                matchOver: false,
                onContinue: StartNextRound);
        }

        private void StartNextRound()
        {
            int target = _gameMode switch
            {
                GameMode.Dwaz   => _dwazMatch.WinTarget,
                GameMode.Kbabet => 0,
                _               => _match.EliminationTarget
            };
            StartCoroutine(InitRound(target, firstRound: false));
        }

        private void ShowMatchOverScreen()
        {
            string winner = _gameMode switch
            {
                GameMode.Dwaz   => _dwazMatch.MatchWinner,
                GameMode.Kbabet => _kbabetMatch.MatchWinner,
                _               => _match.MatchWinner
            };
            _uiController.ShowMatchOverScreen(winner, UiMatchState());
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Display-state adapters — convert Dwaz / Kbabet state into the MatchState
        // shape expected by the existing UI for round-end and scoreboard display.
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a lightweight MatchState whose TotalScore reflects Dwaz effective scores,
        /// so the existing scoreboard UI shows meaningful numbers.
        /// </summary>
        private MatchState BuildDwazDisplayMatch()
        {
            var display = new MatchState();
            display.Setup(_turns.Players, _dwazMatch.WinTarget);
            foreach (var rec in _dwazMatch.Players)
            {
                var dr = display.GetRecord(rec.Name);
                if (dr != null) dr.TotalScore = rec.EffectiveScore;
            }
            return display;
        }

        private List<RoundScoreCalculator.RoundResult> BuildDwazDisplayResults(
            DwazRoundResult dwazResult,
            IReadOnlyList<PlayerHand> players)
        {
            var list = new List<RoundScoreCalculator.RoundResult>();
            foreach (var p in players)
            {
                bool isWinner = p.PlayerName == dwazResult.WinnerName;
                int  score    = isWinner ? 3 : dwazResult.JokersInHand.GetValueOrDefault(p.PlayerName, 0);
                list.Add(new RoundScoreCalculator.RoundResult
                {
                    PlayerName   = p.PlayerName,
                    Type         = isWinner ? RoundScoreCalculator.ResultType.Farcha : RoundScoreCalculator.ResultType.Man3a,
                    HandLeftover = score,
                    RoundScore   = score
                });
            }
            return list;
        }

        private MatchState BuildKbabetDisplayMatch()
        {
            var display = new MatchState();
            display.Setup(_turns.Players, 0);
            foreach (var rec in _kbabetMatch.Players)
            {
                var dr = display.GetRecord(rec.Name);
                if (dr != null) dr.TotalScore = rec.TotalScore;
            }
            return display;
        }

        private List<RoundScoreCalculator.RoundResult> BuildKbabetDisplayResults(
            List<KbabetRoundScoreCalculator.KbabetRoundResult> kResults)
        {
            return kResults.Select(kr => new RoundScoreCalculator.RoundResult
            {
                PlayerName   = kr.PlayerName,
                Type         = kr.IsWinner ? RoundScoreCalculator.ResultType.Farcha : RoundScoreCalculator.ResultType.Man3a,
                HandLeftover = kr.HandValue,
                RoundScore   = kr.RoundScore
            }).ToList();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round / mode convenience
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Current round number, regardless of active mode.</summary>
        private int CurrentRound => _gameMode switch
        {
            GameMode.Dwaz   => _dwazMatch?.CurrentRound ?? 1,
            GameMode.Kbabet => _kbabetMatch?.CurrentRound ?? 1,
            _               => _match?.CurrentRound ?? 1
        };

        /// <summary>Whether the active player is considered active in the current mode.</summary>
        private bool IsPlayerActive(string name) => _gameMode switch
        {
            GameMode.Dwaz   => _dwazMatch.IsActive(name),
            GameMode.Kbabet => _kbabetMatch.IsActive(name),
            _               => _match.IsActive(name)
        };

        /// <summary>
        /// Returns a <see cref="MatchState"/> that the UI can safely read every round.
        /// For Standard Rami this is the real match state.
        /// For Dwaz / Kbabet a lightweight display state is built from the mode-specific state
        /// so that UI code that reads CurrentRound, EliminationTarget, TotalScore, etc. never
        /// receives a null reference.
        /// </summary>
        private MatchState UiMatchState()
        {
            if (_gameMode == GameMode.StandardRami)
                return _match;

            return _gameMode == GameMode.Dwaz
                ? BuildDwazDisplayMatch()
                : BuildKbabetDisplayMatch();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Public accessors for UI
        // ──────────────────────────────────────────────────────────────────────────

        public MatchState       Match        => _match;
        public DwazMatchState   DwazMatch    => _dwazMatch;
        public KbabetMatchState KbabetMatch  => _kbabetMatch;
        public GameMode         ActiveMode   => _gameMode;
        public DeckManager      Deck         => _deck;
        public TurnManager      Turns        => _turns;
        public PlayerHand       HumanPlayer  => _humanPlayer;
        public GameUIController UIController => _uiController;

        public IReadOnlyDictionary<string, List<List<PlayingCard>>> PublicMelds => _publicMelds;
    }
}
