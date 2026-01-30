using System.Collections.Generic;
using OneMoreTurn.Core;
using OneMoreTurn.Presentation.ViewModels;
using UnityEngine;

namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// Manages UI screen transitions based on game phase.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private MainMenuUI _mainMenuUI;
        [SerializeField] private DraftUI _draftUI;
        [SerializeField] private GameUI _gameUI;
        [SerializeField] private GameOverUI _gameOverUI;

        private void Start()
        {
            // Subscribe to phase changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoadComplete += () => ShowScreen(GamePhase.MainMenu);
                GameManager.Instance.OnDraftStarted += OnDraftStarted;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnGameOver += (_, __) => ShowScreen(GamePhase.GameOver);
            }

            // Hide all screens initially
            HideAllScreens();

            // Show loading state
            if (_mainMenuUI) _mainMenuUI.Show();
        }

        private void OnDraftStarted(List<ModifierDefinition> options, int picksRemaining)
        {
            ShowScreen(GamePhase.Draft);
            if (_draftUI) _draftUI.OnDraftStarted(options, picksRemaining);
        }

        private void OnGameStateChanged(GameViewModel viewModel)
        {
            ShowScreen(GamePhase.Playing);
            if (_gameUI) _gameUI.UpdateUI(viewModel);
        }

        private void ShowScreen(GamePhase phase)
        {
            // Hide all first
            if (_mainMenuUI) _mainMenuUI.Hide();
            if (_draftUI) _draftUI.Hide();
            if (_gameUI) _gameUI.Hide();
            // Note: GameOverUI handles its own show/hide

            // Show the appropriate screen
            switch (phase)
            {
                case GamePhase.MainMenu:
                    if (_mainMenuUI) _mainMenuUI.Show();
                    break;
                case GamePhase.Draft:
                    if (_draftUI) _draftUI.Show();
                    break;
                case GamePhase.Playing:
                    if (_gameUI) _gameUI.Show();
                    break;
                case GamePhase.GameOver:
                    if (_gameUI) _gameUI.Show(); // Keep game visible behind
                    if (_gameOverUI) _gameOverUI.Show();
                    break;
            }
        }

        private void HideAllScreens()
        {
            if (_mainMenuUI) _mainMenuUI.Hide();
            if (_draftUI) _draftUI.Hide();
            if (_gameUI) _gameUI.Hide();
            if (_gameOverUI) _gameOverUI.Hide();
        }
    }
}
