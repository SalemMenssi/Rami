// Owns all Game scene UI: hands, piles, per-player meld zones, draft panel,
// card preview, action buttons, toasts, round-end popup, match-over overlay,
// options/menu panel, and scoreboard.
// Public meld reveal is Man3a / Farcha only.
// Man3a players can add cards from their revealed hand onto any public meld.
// Sub-controllers: ActionPanelController, HUDController, PlayerStatusBadge (per seat).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Rami
{
    public class GameUIController : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector references — Hand Views
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Hand Views")]
        [SerializeField] private HandView _humanHandView;
        [SerializeField] private HandView _leftBotHandView;
        [SerializeField] private HandView _topBotHandView;
        [SerializeField] private HandView _rightBotHandView;

        // ──────────────────────────────────────────────────────────────────────────
        // Pile displays
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Pile Displays")]
        [SerializeField] private Image           _drawPileImage;
        [SerializeField] private Image           _discardPileImage;
        [SerializeField] private TextMeshProUGUI _drawCountLabel;

        // ──────────────────────────────────────────────────────────────────────────
        // Per-player public meld zones (PublicMeldZone components)
        //   Human     → bottom of screen
        //   Bot Left  → left side
        //   Bot Top   → top
        //   Bot Right → right side
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Personal Public Meld Zones")]
        [SerializeField] private PublicMeldZone _humanMeldZone;
        [SerializeField] private PublicMeldZone _leftBotMeldZone;
        [SerializeField] private PublicMeldZone _topBotMeldZone;
        [SerializeField] private PublicMeldZone _rightBotMeldZone;

        // Man3a badges — one per player (small "✓ Man3a" label shown after Man3a)
        [SerializeField] private GameObject _humanMan3aBadge;
        [SerializeField] private GameObject _leftBotMan3aBadge;
        [SerializeField] private GameObject _topBotMan3aBadge;
        [SerializeField] private GameObject _rightBotMan3aBadge;

        // ──────────────────────────────────────────────────────────────────────────
        // Per-player Man3a hand reveal zones
        //   After a player declares Man3a their remaining hand cards become visible
        //   on the table in front of them.
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Man3a Hand Reveal Zones")]
        [SerializeField] private Man3aHandReveal _humanMan3aReveal;
        [SerializeField] private Man3aHandReveal _leftBotMan3aReveal;
        [SerializeField] private Man3aHandReveal _topBotMan3aReveal;
        [SerializeField] private Man3aHandReveal _rightBotMan3aReveal;

        // ──────────────────────────────────────────────────────────────────────────
        // Draft panel (private staging area — human only)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Draft Meld Panel")]
        [SerializeField] private GameObject      _draftPanel;
        [SerializeField] private Transform        _draftContainer;
        [SerializeField] private TextMeshProUGUI  _draftPointsLabel;

        // ──────────────────────────────────────────────────────────────────────────
        // Card preview panel
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Card Preview")]
        [SerializeField] private GameObject _cardPreviewPanel;
        [SerializeField] private Image      _cardPreviewImage;

        // ──────────────────────────────────────────────────────────────────────────
        // Action buttons
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Action Buttons")]
        [SerializeField] private Button _btnDrawDeck;
        [SerializeField] private Button _btnDrawDiscard;
        [SerializeField] private Button _btnStageMeld;
        [SerializeField] private Button _btnCancelDraft;
        [SerializeField] private Button _btnCommitMelds;   // legacy — hidden immediately
        [SerializeField] private Button _btnDiscard;
        [SerializeField] private Button _btnShowMelds;     // Man3a
        [SerializeField] private Button _btnFarcha;        // Farcha (draft path)

        /// <summary>
        /// Shown only when the human has Man3a and has selected cards from their reveal zone.
        /// Pressing this adds selected cards to the chosen meld target.
        /// </summary>
        [SerializeField] private Button _btnAddToMeld;

        /// <summary>
        /// Shown when all remaining hand cards are mapped to target melds via the Man3a add flow.
        /// Triggers the Man3a Farcha finish.
        /// </summary>
        [SerializeField] private Button _btnMan3aFarcha;

        // ──────────────────────────────────────────────────────────────────────────
        // HUD
        // ──────────────────────────────────────────────────────────────────────────
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI _turnLabel;
        [SerializeField] private TextMeshProUGUI _roundLabel;
        [SerializeField] private TextMeshProUGUI _toastLabel;
        [SerializeField] private GameObject      _toastRoot;

        // ──────────────────────────────────────────────────────────────────────────
        // Options / Menu panel
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Options Panel")]
        [SerializeField] private Button          _btnOptions;
        [SerializeField] private GameObject      _optionsPanel;
        [SerializeField] private Button          _btnOptionsResume;
        [SerializeField] private Button          _btnOptionsMainMenu;
        [SerializeField] private TextMeshProUGUI _optionsRoundLabel;
        [SerializeField] private TextMeshProUGUI _optionsTargetLabel;
        [SerializeField] private Transform       _optionsScoreContainer; // rows of score text
        [SerializeField] private GameObject      _optionsScoreRowPrefab; // TextMeshProUGUI prefab

        // ──────────────────────────────────────────────────────────────────────────
        // Round-end popup
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Round End Panel")]
        [SerializeField] private GameObject      _roundEndPanel;
        [SerializeField] private TextMeshProUGUI _roundEndTitle;
        [SerializeField] private Transform       _roundEndScoreContainer;
        [SerializeField] private GameObject      _roundEndRowPrefab;
        [SerializeField] private Button          _btnRoundEndContinue;

        // ──────────────────────────────────────────────────────────────────────────
        // Match-over overlay
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Match Over Overlay")]
        [SerializeField] private GameObject      _matchOverPanel;
        [SerializeField] private TextMeshProUGUI _matchOverLabel;
        [SerializeField] private Transform       _matchOverScoreContainer;
        [SerializeField] private GameObject      _matchOverRowPrefab;
        [SerializeField] private Button          _btnMatchOverMenu;

        // ──────────────────────────────────────────────────────────────────────────
        // Shared
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Shared")]
        [SerializeField] private GameObject _cardPrefab;

        // ──────────────────────────────────────────────────────────────────────────
        // Sub-controllers (optional — wired in scene)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Sub-Controllers")]
        [SerializeField] private ActionPanelController _actionPanel;
        [SerializeField] private HUDController         _hudController;

        // Per-seat status badges (human + 3 bots).
        [Header("Player Status Badges")]
        [SerializeField] private PlayerStatusBadge _humanStatusBadge;
        [SerializeField] private PlayerStatusBadge _leftBotStatusBadge;
        [SerializeField] private PlayerStatusBadge _topBotStatusBadge;
        [SerializeField] private PlayerStatusBadge _rightBotStatusBadge;

        // ──────────────────────────────────────────────────────────────────────────
        // Runtime state
        // ──────────────────────────────────────────────────────────────────────────
        private GameManager _game;
        private DeckManager _deck;
        private TurnManager _turns;
        private PlayerHand  _humanHand;
        private MatchState  _match;
        private Canvas      _rootCanvas;

        private Coroutine _toastCoroutine;

        // Maps player name → their meld zone component.
        private Dictionary<string, PublicMeldZone> _meldZoneMap;

        // Maps player name → Man3a badge GameObject.
        private Dictionary<string, GameObject> _man3aBadgeMap;

        // Maps player name → Man3a hand reveal component.
        private Dictionary<string, Man3aHandReveal> _man3aRevealMap;

        // Currently shown card in the preview panel.
        private CardView _previewCard;

        // Man3a add-to-meld interaction state.
        // The owner of the currently selected target meld.
        private string _selectedMeldOwner;
        private int    _selectedMeldIndex = -1;

        // Pending Man3a-Farcha placements: cardInstanceId → (ownerName, meldIndex).
        private readonly Dictionary<string, (string ownerName, int meldIndex)> _man3aFarchaPlacements = new();

        // ──────────────────────────────────────────────────────────────────────────
        // Initialisation
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Called by GameManager at the start of each round.</summary>
        public void Init(GameManager game, DeckManager deck, TurnManager turns,
                         PlayerHand humanHand, MatchState match)
        {
            _game      = game;
            _deck      = deck;
            _turns     = turns;
            _humanHand = humanHand;
            _match     = match;
            _rootCanvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();

            _selectedMeldOwner  = null;
            _selectedMeldIndex  = -1;
            _man3aFarchaPlacements.Clear();

            BuildMappings();

            // Human hand — interactive with drag.
            _humanHandView.Init(OnHumanCardClicked, _humanHand, _rootCanvas);
            _leftBotHandView.Init(null);
            _topBotHandView.Init(null);
            _rightBotHandView.Init(null);

            // Action buttons.
            _btnDrawDeck.onClick.RemoveAllListeners();
            _btnDrawDiscard.onClick.RemoveAllListeners();
            _btnStageMeld.onClick.RemoveAllListeners();
            _btnCancelDraft.onClick.RemoveAllListeners();
            _btnDiscard.onClick.RemoveAllListeners();
            _btnShowMelds.onClick.RemoveAllListeners();
            _btnFarcha.onClick.RemoveAllListeners();

            _btnDrawDeck.onClick.AddListener(_game.HumanDrawFromDeck);
            _btnDrawDiscard.onClick.AddListener(_game.HumanDrawFromDiscard);
            _btnStageMeld.onClick.AddListener(OnStageMeldClicked);
            _btnCancelDraft.onClick.AddListener(_game.HumanCancelDraft);
            _btnDiscard.onClick.AddListener(OnDiscardClicked);
            _btnShowMelds.onClick.AddListener(_game.HumanShowMelds);
            _btnFarcha.onClick.AddListener(_game.HumanFarcha);

            // Man3a interaction buttons.
            if (_btnAddToMeld  != null) { _btnAddToMeld.onClick.RemoveAllListeners();  _btnAddToMeld.onClick.AddListener(OnAddToMeldClicked);  _btnAddToMeld.gameObject.SetActive(false); }
            if (_btnMan3aFarcha != null) { _btnMan3aFarcha.onClick.RemoveAllListeners(); _btnMan3aFarcha.onClick.AddListener(OnMan3aFarchaClicked); _btnMan3aFarcha.gameObject.SetActive(false); }

            // Legacy commit — hide.
            if (_btnCommitMelds != null)
            {
                _btnCommitMelds.onClick.RemoveAllListeners();
                _btnCommitMelds.onClick.AddListener(_game.HumanCommitMelds);
                _btnCommitMelds.gameObject.SetActive(false);
            }

            // Options panel.
            if (_btnOptions       != null) { _btnOptions.onClick.RemoveAllListeners();       _btnOptions.onClick.AddListener(OpenOptionsPanel); }
            if (_btnOptionsResume != null) { _btnOptionsResume.onClick.RemoveAllListeners(); _btnOptionsResume.onClick.AddListener(CloseOptionsPanel); }
            if (_btnOptionsMainMenu != null) { _btnOptionsMainMenu.onClick.RemoveAllListeners(); _btnOptionsMainMenu.onClick.AddListener(GoToMainMenu); }
            if (_optionsPanel     != null) _optionsPanel.SetActive(false);

            // Round-end panel.
            if (_roundEndPanel    != null) _roundEndPanel.SetActive(false);
            if (_btnRoundEndContinue != null) _btnRoundEndContinue.onClick.RemoveAllListeners();

            // Match-over panel.
            if (_matchOverPanel   != null) _matchOverPanel.SetActive(false);
            if (_btnMatchOverMenu != null) { _btnMatchOverMenu.onClick.RemoveAllListeners(); _btnMatchOverMenu.onClick.AddListener(GoToMainMenu); }

            // Draft panel.
            if (_draftPanel       != null) _draftPanel.SetActive(false);

            // Toast.
            if (_toastRoot        != null) _toastRoot.SetActive(false);
            else if (_toastLabel  != null) _toastLabel.gameObject.SetActive(false);

            // Card preview.
            if (_cardPreviewPanel != null) _cardPreviewPanel.SetActive(false);

            // Man3a badges — all off.
            foreach (var badge in _man3aBadgeMap.Values)
                if (badge != null) badge.SetActive(false);

            // Man3a reveal zones — all hidden.
            foreach (var reveal in _man3aRevealMap.Values)
                reveal?.Hide();

            // Clear meld zones.
            ClearAllMeldZones();

            // Wire HUDController if present.
            _hudController?.Init(OpenOptionsPanel, OpenOptionsPanel);
            _hudController?.SetRoundInfo(_match.CurrentRound, _match.EliminationTarget);

            // Set player names on status badges.
            if (_turns.Players.Count > 0) _humanStatusBadge?.SetPlayerName(_turns.Players[0].PlayerName);
            if (_turns.Players.Count > 1) _leftBotStatusBadge?.SetPlayerName(_turns.Players[1].PlayerName);
            if (_turns.Players.Count > 2) _topBotStatusBadge?.SetPlayerName(_turns.Players[2].PlayerName);
            if (_turns.Players.Count > 3) _rightBotStatusBadge?.SetPlayerName(_turns.Players[3].PlayerName);
            RefreshStatusBadges();
        }

        private void BuildMappings()
        {
            // Player name → PublicMeldZone component.
            _meldZoneMap = new Dictionary<string, PublicMeldZone>();
            if (_turns.Players.Count > 0 && _humanMeldZone   != null) _meldZoneMap[_turns.Players[0].PlayerName] = _humanMeldZone;
            if (_turns.Players.Count > 1 && _leftBotMeldZone != null) _meldZoneMap[_turns.Players[1].PlayerName] = _leftBotMeldZone;
            if (_turns.Players.Count > 2 && _topBotMeldZone  != null) _meldZoneMap[_turns.Players[2].PlayerName] = _topBotMeldZone;
            if (_turns.Players.Count > 3 && _rightBotMeldZone != null) _meldZoneMap[_turns.Players[3].PlayerName] = _rightBotMeldZone;

            _man3aBadgeMap = new Dictionary<string, GameObject>();
            if (_turns.Players.Count > 0) _man3aBadgeMap[_turns.Players[0].PlayerName] = _humanMan3aBadge;
            if (_turns.Players.Count > 1) _man3aBadgeMap[_turns.Players[1].PlayerName] = _leftBotMan3aBadge;
            if (_turns.Players.Count > 2) _man3aBadgeMap[_turns.Players[2].PlayerName] = _topBotMan3aBadge;
            if (_turns.Players.Count > 3) _man3aBadgeMap[_turns.Players[3].PlayerName] = _rightBotMan3aBadge;

            _man3aRevealMap = new Dictionary<string, Man3aHandReveal>();
            if (_turns.Players.Count > 0) _man3aRevealMap[_turns.Players[0].PlayerName] = _humanMan3aReveal;
            if (_turns.Players.Count > 1) _man3aRevealMap[_turns.Players[1].PlayerName] = _leftBotMan3aReveal;
            if (_turns.Players.Count > 2) _man3aRevealMap[_turns.Players[2].PlayerName] = _topBotMan3aReveal;
            if (_turns.Players.Count > 3) _man3aRevealMap[_turns.Players[3].PlayerName] = _rightBotMan3aReveal;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Refresh
        // ──────────────────────────────────────────────────────────────────────────

        public void RefreshAll()
        {
            RefreshHand();
            RefreshBotHands();
            RefreshDiscard();
            RefreshDrawCount();
        }

        public void RefreshHand()
        {
            _humanHandView.Refresh(_humanHand.Cards, faceUp: true, interactable: true);
            RefreshDrawCount();

            // If Man3a is active, also sync the reveal zone.
            bool humanHasMan3a = _match?.GetRecord(_humanHand?.PlayerName)?.HasShownMelds ?? false;
            if (humanHasMan3a)
                RefreshMan3aReveal(_humanHand.PlayerName, _humanHand.Cards);
        }

        private void RefreshBotHands()
        {
            var players = _turns.Players;
            if (players.Count > 1) _leftBotHandView.Refresh(players[1].Cards,  faceUp: false, interactable: false);
            if (players.Count > 2) _topBotHandView.Refresh(players[2].Cards,   faceUp: false, interactable: false);
            if (players.Count > 3) _rightBotHandView.Refresh(players[3].Cards, faceUp: false, interactable: false);
        }

        public void RefreshDiscard()
        {
            var top = _deck.TopDiscard;
            if (_discardPileImage != null)
            {
                _discardPileImage.sprite = top != null
                    ? CardSpriteLibrary.GetFaceSprite(top)
                    : CardSpriteLibrary.GetBackSprite();
                _discardPileImage.color = top != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private void RefreshDrawCount()
        {
            if (_drawCountLabel != null) _drawCountLabel.text = _deck.DrawCount.ToString();
            if (_drawPileImage  != null) _drawPileImage.sprite = CardSpriteLibrary.GetBackSprite();
        }

        /// <summary>Rebuilds the private draft staging panel.</summary>
        public void RefreshDraftPanel(DraftMeldManager draft)
        {
            if (_draftPanel == null) return;

            bool hasDraft = draft.HasStagedMelds;
            _draftPanel.SetActive(hasDraft);

            if (_draftContainer != null)
            {
                foreach (Transform child in _draftContainer)
                    Destroy(child.gameObject);

                if (hasDraft && _cardPrefab != null)
                {
                    foreach (var meld in draft.StagedMelds)
                    {
                        var groupGo = new GameObject("DraftMeld");
                        groupGo.transform.SetParent(_draftContainer, false);
                        var rt = groupGo.AddComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(meld.Count * 54f + 8f, 80f);
                        var hg = groupGo.AddComponent<HorizontalLayoutGroup>();
                        hg.spacing            = -12f;
                        hg.childAlignment     = TextAnchor.MiddleCenter;
                        hg.childControlWidth  = false;
                        hg.childControlHeight = false;

                        foreach (var card in meld)
                        {
                            var go  = Instantiate(_cardPrefab, groupGo.transform);
                            var rt2 = go.GetComponent<RectTransform>();
                            if (rt2 != null) rt2.sizeDelta = new Vector2(50f, 74f);
                            var cv  = go.GetComponent<CardView>();
                            if (cv != null) cv.Setup(card, faceUp: true, interactable: false, onClick: null);
                        }
                    }
                }
            }

            if (_draftPointsLabel != null && hasDraft)
                _draftPointsLabel.text = $"Draft: {draft.StagedPointValue()} pts";

            if (_btnCancelDraft != null) _btnCancelDraft.interactable = hasDraft;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Per-player public meld zones
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Rebuilds the public meld zone for a specific player.</summary>
        public void RefreshPlayerMelds(string playerName, List<List<PlayingCard>> melds)
        {
            if (!_meldZoneMap.TryGetValue(playerName, out var zone) || zone == null) return;

            // Determine if meld groups should be interactive (human's turn + human has Man3a).
            bool humanHasMeld = _match.GetRecord(_humanHand.PlayerName)?.HasShownMelds ?? false;
            System.Action<int> onSelect = humanHasMeld
                ? (idx) => OnMeldGroupSelected(playerName, idx)
                : (System.Action<int>)null;

            zone.Refresh(melds, onSelect);
        }

        private void ClearAllMeldZones()
        {
            foreach (var zone in _meldZoneMap.Values)
                zone?.Refresh(new List<List<PlayingCard>>(), null);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a hand reveal zones
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or refreshes the face-up hand reveal for a player who has declared Man3a.
        /// Pass the player's current hand cards so the zone stays in sync after adds.
        /// </summary>
        public void RefreshMan3aReveal(string playerName, IReadOnlyList<PlayingCard> handCards)
        {
            if (!_man3aRevealMap.TryGetValue(playerName, out var reveal) || reveal == null) return;

            bool isHuman = playerName == _humanHand.PlayerName;
            // Human reveal is interactive so they can select cards to add to melds.
            System.Action<CardView> onClick = isHuman ? OnMan3aRevealCardClicked : (System.Action<CardView>)null;
            reveal.Refresh(handCards, interactable: isHuman, onCardClicked: onClick);
        }

        /// <summary>Hides the Man3a reveal for a player (e.g. after they empty their hand).</summary>
        public void HideMan3aReveal(string playerName)
        {
            if (_man3aRevealMap.TryGetValue(playerName, out var reveal))
                reveal?.Hide();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a badge
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the Man3a badge for a player.</summary>
        public void UpdateMan3aBadge(string playerName, bool shown)
        {
            if (_man3aBadgeMap.TryGetValue(playerName, out var badge) && badge != null)
                badge.SetActive(shown);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Card preview
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Shows the large preview of the tapped card. Pass null to hide.</summary>
        public void ShowCardPreview(PlayingCard card)
        {
            if (_cardPreviewPanel == null) return;

            if (card == null)
            {
                _cardPreviewPanel.SetActive(false);
                return;
            }

            _cardPreviewPanel.SetActive(true);
            if (_cardPreviewImage != null)
                _cardPreviewImage.sprite = CardSpriteLibrary.GetFaceSprite(card);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Turn / phase control
        // ──────────────────────────────────────────────────────────────────────────

        public void SetTurnIndicator(string playerName, int round)
        {
            if (_turnLabel  != null) _turnLabel.text  = $"Turn: {playerName}";
            if (_roundLabel != null) _roundLabel.text = $"Round {round}";

            // Delegate to HUDController when present.
            _hudController?.SetTurnLabel(playerName);
            _hudController?.SetRoundInfo(round, _match?.EliminationTarget ?? 0);

            RefreshBotHands();
            RefreshStatusBadges();
        }

        /// <summary>
        /// Enables/disables human action buttons based on game phase.
        /// drawn=false → only draw buttons active.
        /// drawn=true  → draw disabled; stage/cancel/discard/Man3a/Farcha active.
        /// When the human has Man3a the Add/Man3aFarcha buttons are managed separately.
        /// </summary>
        public void SetHumanActionPhase(bool drawn)
        {
            bool humanHasMan3a = _match?.GetRecord(_humanHand?.PlayerName)?.HasShownMelds ?? false;

            // Delegate to ActionPanelController when present.
            if (_actionPanel != null)
            {
                _actionPanel.SetPhase(drawn, humanHasMan3a);
            }
            else
            {
                // Fallback: legacy direct button management.
                _btnDrawDeck.interactable    = !drawn;
                _btnDrawDiscard.interactable = !drawn;
                _btnStageMeld.interactable   = drawn && !humanHasMan3a;
                _btnDiscard.interactable     = drawn;
                _btnShowMelds.interactable   = drawn && !humanHasMan3a;
                _btnFarcha.interactable      = drawn && !humanHasMan3a;

                if (_btnCancelDraft != null) _btnCancelDraft.interactable = drawn;
                if (_btnCommitMelds != null) _btnCommitMelds.gameObject.SetActive(false);

                if (!humanHasMan3a)
                {
                    if (_btnAddToMeld   != null) _btnAddToMeld.gameObject.SetActive(false);
                    if (_btnMan3aFarcha != null) _btnMan3aFarcha.gameObject.SetActive(false);
                }
            }

            // Refresh meld value indicator based on current selection.
            RefreshMeldValueDisplay();
        }

        public void ClearSelection() => _humanHandView.ClearAllSelections();

        /// <summary>Returns all cards currently selected in the human hand view.</summary>
        public System.Collections.Generic.List<PlayingCard> GetHumanSelectedCards()
            => _humanHandView.GetSelectedCards();

        // ──────────────────────────────────────────────────────────────────────────
        // Man3a add-to-meld interaction
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the human taps a meld group during a Man3a turn.
        /// Selects that meld as the target for card additions.
        /// </summary>
        private void OnMeldGroupSelected(string ownerName, int meldIndex)
        {
            // Deselect previous meld zone if switching targets.
            if (_selectedMeldOwner != null && _meldZoneMap.TryGetValue(_selectedMeldOwner, out var prevZone))
                prevZone?.ClearSelection();

            if (meldIndex < 0)
            {
                // Deselected.
                _selectedMeldOwner = null;
                _selectedMeldIndex = -1;
            }
            else
            {
                _selectedMeldOwner = ownerName;
                _selectedMeldIndex = meldIndex;
                if (_meldZoneMap.TryGetValue(ownerName, out var zone))
                    zone?.SelectMeld(meldIndex);
            }

            RefreshMan3aActionButtons();
        }

        /// <summary>Called when the human taps a card in their Man3a reveal zone.</summary>
        private void OnMan3aRevealCardClicked(CardView cv)
        {
            cv.SetSelected(!cv.IsSelected);
            RefreshMan3aActionButtons();
        }

        /// <summary>Shows/hides the Add and Man3a-Farcha buttons based on selection state.</summary>
        private void RefreshMan3aActionButtons()
        {
            bool hasMeldTarget   = _selectedMeldIndex >= 0;
            bool hasRevealCards  = _humanMan3aReveal != null &&
                                   _humanMan3aReveal.GetSelectedCards().Count > 0;

            // Delegate to ActionPanelController when present.
            if (_actionPanel != null)
            {
                _actionPanel.RefreshMan3aButtons(hasMeldTarget, hasRevealCards);
            }
            else
            {
                if (_btnAddToMeld  != null)
                    _btnAddToMeld.gameObject.SetActive(hasMeldTarget && hasRevealCards);

                if (_btnMan3aFarcha != null)
                    _btnMan3aFarcha.gameObject.SetActive(hasMeldTarget);
            }
        }

        /// <summary>Executes an add-to-meld action: selected reveal cards → selected meld.</summary>
        private void OnAddToMeldClicked()
        {
            if (_humanMan3aReveal == null) return;
            var cards = _humanMan3aReveal.GetSelectedCards();
            if (cards.Count == 0) { ShowToast("Select cards from your revealed hand."); return; }
            if (_selectedMeldIndex < 0)   { ShowToast("Tap a meld to choose a target."); return; }

            bool ok = _game.HumanAddCardsToPublicMeld(cards, _selectedMeldOwner, _selectedMeldIndex);
            if (ok)
            {
                // Clear selection state.
                _humanMan3aReveal.ClearSelection();
                OnMeldGroupSelected(_selectedMeldOwner, -1);
                if (_btnAddToMeld   != null) _btnAddToMeld.gameObject.SetActive(false);
                if (_btnMan3aFarcha != null) _btnMan3aFarcha.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Confirms all pending Man3a-Farcha placements and ends the round
        /// by placing every remaining card onto melds.
        /// </summary>
        private void OnMan3aFarchaClicked()
        {
            if (_man3aFarchaPlacements.Count == 0)
            {
                ShowToast("Map all hand cards to melds using Add, then confirm.");
                return;
            }
            _game.HumanMan3aFarcha(new Dictionary<string, (string, int)>(_man3aFarchaPlacements));
            _man3aFarchaPlacements.Clear();
        }

        /// <summary>
        /// Called by GameManager after Man3a is declared.
        /// Activates the Man3a reveal zone for the human and makes all meld zones interactive.
        /// </summary>
        public void OnHumanMan3aDeclared()
        {
            // Reveal remaining hand face-up on the table.
            RefreshMan3aReveal(_humanHand.PlayerName, _humanHand.Cards);

            // Re-wire all meld zones to be interactive (for add-to-meld targeting).
            foreach (var kv in _game.PublicMelds)
                RefreshPlayerMelds(kv.Key, kv.Value);

            // Show the Man3a-Farcha confirm button.
            if (_btnMan3aFarcha != null) _btnMan3aFarcha.gameObject.SetActive(true);

            ShowToast("Man3a! Tap a meld then select cards to add.");
        }

        /// <summary>
        /// Called by GameManager when a bot declares Man3a.
        /// Shows the bot's remaining hand cards on the table.
        /// </summary>
        public void OnBotMan3aDeclared(string playerName, IReadOnlyList<PlayingCard> handCards)
        {
            RefreshMan3aReveal(playerName, handCards);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round banner
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Shows a brief "Round N" banner at the start of each round.</summary>
        public void ShowRoundBanner(int round)
        {
            ShowToast($"Round {round} — Good luck!");
            if (_roundLabel != null) _roundLabel.text = $"Round {round}";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button handlers
        // ──────────────────────────────────────────────────────────────────────────

        private void OnHumanCardClicked(CardView cardView)
        {
            _humanHandView.ToggleSelection(cardView);
            // Show large preview of the most recently clicked card.
            bool nowSelected = cardView.IsSelected;
            ShowCardPreview(nowSelected ? cardView.Card : null);
            // Update meld value indicator.
            RefreshMeldValueDisplay();
        }

        private void OnStageMeldClicked()
        {
            var selected = _humanHandView.GetSelectedCards();
            _game.HumanStageMeld(selected);
        }

        private void OnDiscardClicked()
        {
            var card = _humanHandView.GetFirstSelected();
            _game.HumanDiscard(card);
            ShowCardPreview(null);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Meld value display
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Refreshes the meld-value indicator based on current hand selection.</summary>
        public void RefreshMeldValueDisplay()
        {
            if (_actionPanel == null) return;
            var selected     = _humanHandView?.GetSelectedCards() ?? new System.Collections.Generic.List<PlayingCard>();
            bool hasShown    = _match?.GetRecord(_humanHand?.PlayerName)?.HasShownMelds ?? false;
            _actionPanel.RefreshMeldValue(selected, hasShown);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Status badges
        // ──────────────────────────────────────────────────────────────────────────

        private void RefreshStatusBadges()
        {
            if (_match == null || _turns == null) return;

            RefreshSingleBadge(_humanStatusBadge,    0);
            RefreshSingleBadge(_leftBotStatusBadge,  1);
            RefreshSingleBadge(_topBotStatusBadge,   2);
            RefreshSingleBadge(_rightBotStatusBadge, 3);
        }

        private void RefreshSingleBadge(PlayerStatusBadge badge, int playerIndex)
        {
            if (badge == null || _turns.Players.Count <= playerIndex) return;
            var player = _turns.Players[playerIndex];
            var record = _match.GetRecord(player.PlayerName);
            badge.Refresh(record, player.CardCount);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Toast
        // ──────────────────────────────────────────────────────────────────────────

        public void ShowToast(string message)
        {
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            var toastGo = _toastRoot != null ? _toastRoot : _toastLabel?.gameObject;
            if (_toastLabel != null) _toastLabel.text = message;
            if (toastGo    != null) toastGo.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            if (toastGo    != null) toastGo.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Options / Menu panel
        // ──────────────────────────────────────────────────────────────────────────

        private void OpenOptionsPanel()
        {
            if (_optionsPanel == null) return;

            if (_optionsRoundLabel  != null)
                _optionsRoundLabel.text = $"Round: {_match.CurrentRound}";

            if (_optionsTargetLabel != null)
                _optionsTargetLabel.text = $"Elimination target: {_match.EliminationTarget} pts";

            // Rebuild scoreboard rows.
            if (_optionsScoreContainer != null)
            {
                foreach (Transform child in _optionsScoreContainer)
                    Destroy(child.gameObject);

                foreach (var rec in _match.Players)
                {
                    string status = rec.IsEliminated ? " [Eliminated]" : "";
                    string man3a  = rec.HasShownMelds ? " ✓Man3a" : "";
                    string row    = $"{rec.Name}{status}{man3a}: {rec.TotalScore} pts";
                    CreateTextRow(_optionsScoreContainer, _optionsScoreRowPrefab, row);
                }
            }

            _optionsPanel.SetActive(true);
        }

        private void CloseOptionsPanel()
        {
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round-end popup
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Displays the round-end result popup with per-player breakdown.</summary>
        public void ShowRoundEndPanel(
            List<RoundScoreCalculator.RoundResult> results,
            MatchState match,
            bool matchOver,
            System.Action onContinue)
        {
            if (_roundEndPanel == null) { onContinue?.Invoke(); return; }

            SetHumanActionPhase(drawn: false);
            _btnDrawDeck.interactable    = false;
            _btnDrawDiscard.interactable = false;

            if (_roundEndTitle != null)
                _roundEndTitle.text = matchOver ? "Match Over!" : $"Round {match.CurrentRound - 1} Complete";

            if (_roundEndScoreContainer != null)
            {
                foreach (Transform child in _roundEndScoreContainer)
                    Destroy(child.gameObject);

                foreach (var r in results)
                {
                    var rec     = match.GetRecord(r.PlayerName);
                    int total   = rec?.TotalScore ?? 0;
                    string line = $"{r.PlayerName}: {ResultTypeLabel(r.Type)} +{r.RoundScore} → Total: {total}";
                    if (rec?.IsEliminated == true) line += " [Eliminated]";
                    CreateTextRow(_roundEndScoreContainer, _roundEndRowPrefab, line);
                }
            }

            if (_btnRoundEndContinue != null)
            {
                _btnRoundEndContinue.onClick.RemoveAllListeners();
                _btnRoundEndContinue.onClick.AddListener(() =>
                {
                    _roundEndPanel.SetActive(false);
                    onContinue?.Invoke();
                });
                var btnLabel = _btnRoundEndContinue.GetComponentInChildren<TextMeshProUGUI>();
                if (btnLabel != null) btnLabel.text = matchOver ? "Finish" : "Next Round";
            }

            _roundEndPanel.SetActive(true);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Match-over overlay
        // ──────────────────────────────────────────────────────────────────────────

        public void ShowMatchOverScreen(string winner, MatchState match)
        {
            if (_matchOverPanel == null) return;

            string header = winner == "You" ? "You won the match!" : $"{winner} won the match!";
            if (_matchOverLabel != null) _matchOverLabel.text = header;

            if (_matchOverScoreContainer != null)
            {
                foreach (Transform child in _matchOverScoreContainer)
                    Destroy(child.gameObject);

                var sorted = match.Players.OrderBy(p => p.TotalScore).ToList();
                int rank   = 1;
                foreach (var rec in sorted)
                {
                    string status = rec.IsEliminated ? " [Eliminated]" : "";
                    string row    = $"#{rank++} {rec.Name}{status}: {rec.TotalScore} pts";
                    CreateTextRow(_matchOverScoreContainer, _matchOverRowPrefab, row);
                }
            }

            _matchOverPanel.SetActive(true);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Legacy: kept so existing scene references compile
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Legacy winner screen — redirects to match-over flow.</summary>
        public void ShowWinnerScreen(string winner, bool isMan3aa = false, bool isFarcha = false)
        {
            // This is superseded by the round-end → match-over flow.
            // Kept only so any old scene wiring doesn't break.
            Debug.Log($"[UI] ShowWinnerScreen: {winner} (Farcha={isFarcha})");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void CreateTextRow(Transform container, GameObject rowPrefab, string text)
        {
            if (container == null) return;

            if (rowPrefab != null)
            {
                var go  = Instantiate(rowPrefab, container);
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = text;
            }
            else
            {
                // Fallback: create a raw TextMeshProUGUI.
                var go  = new GameObject("ScoreRow");
                go.transform.SetParent(container, false);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text      = text;
                tmp.fontSize  = 24;
                tmp.color     = Color.white;
                var rt        = go.GetComponent<RectTransform>();
                rt.sizeDelta  = new Vector2(600f, 36f);
            }
        }

        private static string ResultTypeLabel(RoundScoreCalculator.ResultType type) => type switch
        {
            RoundScoreCalculator.ResultType.Farcha   => "Farcha",
            RoundScoreCalculator.ResultType.Man3a    => "Man3a leftover",
            RoundScoreCalculator.ResultType.NotShown => "Not shown",
            _                                        => "?"
        };

        // ──────────────────────────────────────────────────────────────────────────
        // Navigation
        // ──────────────────────────────────────────────────────────────────────────

        private void GoToMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
