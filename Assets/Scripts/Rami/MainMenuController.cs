// Main Menu screen controller.
// Flow: Play → Mode panel (Standard / Dwaz / Kbabet)
//        → Target panel (depends on mode) → Launch game.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Rami
{
    public class MainMenuController : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Main menu buttons
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Main Menu")]
        [SerializeField] private Button     _btnPlaySolo;
        [SerializeField] private Button     _btnRules;
        [SerializeField] private Button     _btnSettings;
        [SerializeField] private Button     _btnQuit;

        [SerializeField] private GameObject _rulesPanel;
        [SerializeField] private GameObject _settingsPanel;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Mode selection panel  (shown first when Play is pressed)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Mode Selection Panel")]
        [SerializeField] private GameObject _modePanel;
        [SerializeField] private Button     _btnModeStandard;
        [SerializeField] private Button     _btnModeDwaz;
        [SerializeField] private Button     _btnModeKbabet;
        [SerializeField] private Button     _btnCancelMode;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Standard Rami target panel (600 / 800 / 1000)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Standard Rami — Elimination Target Panel")]
        [SerializeField] private GameObject _eliminationPanel;
        [SerializeField] private Button     _btn600;
        [SerializeField] private Button     _btn800;
        [SerializeField] private Button     _btn1000;
        [SerializeField] private Button     _btnCancelElimination;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Dwaz target panel (21 / 31 / 40)
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Dwaz — Win Target Panel")]
        [SerializeField] private GameObject _dwazPanel;
        [SerializeField] private Button     _btnDwaz21;
        [SerializeField] private Button     _btnDwaz31;
        [SerializeField] private Button     _btnDwaz40;
        [SerializeField] private Button     _btnCancelDwaz;

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector — Kbabet options panel
        // ──────────────────────────────────────────────────────────────────────────
        [Header("Kbabet — Options Panel")]
        [SerializeField] private GameObject _kbabetPanel;
        [SerializeField] private Toggle     _toggleTafdhila;
        [SerializeField] private Button     _btnStartKbabet;
        [SerializeField] private Button     _btnCancelKbabet;

        // ──────────────────────────────────────────────────────────────────────────
        // Legacy static — kept for backwards compatibility with GameManager.Start()
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Elimination target for Standard Rami — GameManager reads this on Start().
        /// Replaced by GameSettings.ChosenTarget for new modes, but kept so existing
        /// code that references MainMenuController.ChosenEliminationTarget still compiles.
        /// </summary>
        public static int ChosenEliminationTarget { get; private set; } = MatchState.DefaultEliminationTarget;

        // ──────────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Debug.Log("[Rami] Main Menu loaded");

            // Main buttons
            _btnPlaySolo.onClick.AddListener(OpenModePanel);
            _btnRules.onClick.AddListener(ToggleRules);
            if (_btnSettings != null) _btnSettings.onClick.AddListener(ToggleSettings);
            if (_btnQuit     != null) _btnQuit.onClick.AddListener(QuitGame);

            if (_rulesPanel    != null) _rulesPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            // Mode panel
            if (_modePanel != null)
            {
                _modePanel.SetActive(false);
                if (_btnModeStandard != null) _btnModeStandard.onClick.AddListener(SelectStandardRami);
                if (_btnModeDwaz     != null) _btnModeDwaz.onClick.AddListener(SelectDwaz);
                if (_btnModeKbabet   != null) _btnModeKbabet.onClick.AddListener(SelectKbabet);
                if (_btnCancelMode   != null) _btnCancelMode.onClick.AddListener(CloseModePanel);
            }

            // Standard Rami target panel
            if (_eliminationPanel != null)
            {
                _eliminationPanel.SetActive(false);
                if (_btn600               != null) _btn600.onClick.AddListener(() => LaunchStandardRami(600));
                if (_btn800               != null) _btn800.onClick.AddListener(() => LaunchStandardRami(800));
                if (_btn1000              != null) _btn1000.onClick.AddListener(() => LaunchStandardRami(1000));
                if (_btnCancelElimination != null) _btnCancelElimination.onClick.AddListener(CloseTargetPanels);
            }

            // Dwaz panel
            if (_dwazPanel != null)
            {
                _dwazPanel.SetActive(false);
                if (_btnDwaz21    != null) _btnDwaz21.onClick.AddListener(() => LaunchDwaz(21));
                if (_btnDwaz31    != null) _btnDwaz31.onClick.AddListener(() => LaunchDwaz(31));
                if (_btnDwaz40    != null) _btnDwaz40.onClick.AddListener(() => LaunchDwaz(40));
                if (_btnCancelDwaz != null) _btnCancelDwaz.onClick.AddListener(CloseTargetPanels);
            }

            // Kbabet panel
            if (_kbabetPanel != null)
            {
                _kbabetPanel.SetActive(false);
                if (_btnStartKbabet  != null) _btnStartKbabet.onClick.AddListener(LaunchKbabet);
                if (_btnCancelKbabet != null) _btnCancelKbabet.onClick.AddListener(CloseTargetPanels);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Mode panel
        // ──────────────────────────────────────────────────────────────────────────

        private void OpenModePanel()
        {
            CloseAllSubPanels();
            if (_modePanel != null)
                _modePanel.SetActive(true);
            else
                // No mode panel in scene — fall back to Standard Rami directly.
                LaunchStandardRami(MatchState.DefaultEliminationTarget);
        }

        private void CloseModePanel()
        {
            if (_modePanel != null) _modePanel.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Mode selections
        // ──────────────────────────────────────────────────────────────────────────

        private void SelectStandardRami()
        {
            CloseModePanel();
            if (_eliminationPanel != null)
                _eliminationPanel.SetActive(true);
            else
                LaunchStandardRami(MatchState.DefaultEliminationTarget);
        }

        private void SelectDwaz()
        {
            CloseModePanel();
            if (_dwazPanel != null)
                _dwazPanel.SetActive(true);
            else
                LaunchDwaz(GameSettings.DefaultDwazTarget);
        }

        private void SelectKbabet()
        {
            CloseModePanel();
            if (_kbabetPanel != null)
                _kbabetPanel.SetActive(true);
            else
                LaunchKbabet();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Launch helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void LaunchStandardRami(int eliminationTarget)
        {
            GameSettings.ChosenMode   = GameMode.StandardRami;
            GameSettings.ChosenTarget = eliminationTarget;
            ChosenEliminationTarget   = eliminationTarget;   // legacy compat
            Debug.Log($"[Rami] Launching Standard Rami — elimination @ {eliminationTarget}");
            SceneManager.LoadScene("Game");
        }

        private void LaunchDwaz(int winTarget)
        {
            GameSettings.ChosenMode   = GameMode.Dwaz;
            GameSettings.ChosenTarget = winTarget;
            Debug.Log($"[Rami] Launching Dwaz — target {winTarget} pts");
            SceneManager.LoadScene("Game");
        }

        private void LaunchKbabet()
        {
            bool tafdhila = _toggleTafdhila != null && _toggleTafdhila.isOn;
            GameSettings.ChosenMode           = GameMode.Kbabet;
            GameSettings.ChosenTarget         = 0;
            GameSettings.KbabetTafdhilaEnabled = tafdhila;
            Debug.Log($"[Rami] Launching Kbabet — Tafdhila: {tafdhila}");
            SceneManager.LoadScene("Game");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Utility
        // ──────────────────────────────────────────────────────────────────────────

        private void CloseTargetPanels()
        {
            if (_eliminationPanel != null) _eliminationPanel.SetActive(false);
            if (_dwazPanel        != null) _dwazPanel.SetActive(false);
            if (_kbabetPanel      != null) _kbabetPanel.SetActive(false);
        }

        private void CloseAllSubPanels()
        {
            CloseModePanel();
            CloseTargetPanels();
            if (_rulesPanel    != null) _rulesPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        private void ToggleRules()
        {
            if (_rulesPanel == null) return;
            bool next = !_rulesPanel.activeSelf;
            CloseAllSubPanels();
            _rulesPanel.SetActive(next);
        }

        private void ToggleSettings()
        {
            if (_settingsPanel == null) return;
            bool next = !_settingsPanel.activeSelf;
            CloseAllSubPanels();
            _settingsPanel.SetActive(next);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
