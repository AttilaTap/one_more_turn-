using System;
using System.Collections.Generic;
using OneMoreTurn.Core;
using OneMoreTurn.Presentation.Services;
using OneMoreTurn.Presentation.ViewModels;
using UnityEngine;

namespace OneMoreTurn.Presentation
{
    /// <summary>
    /// Main game orchestrator. Manages game state and UI flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int _defaultSeed = -1; // -1 = random

        // Core systems
        private ModifierRegistry _registry;
        private TurnResolver _resolver;
        private ModifierLoader _loader;

        // Game state
        private RunState _currentState;
        private TurnResult _lastTurnResult;
        private GamePhase _phase = GamePhase.Loading;

        // Draft state
        private List<ModifierDefinition> _draftOptions;
        private List<ModifierDefinition> _selectedModifiers;

        // Events for UI to subscribe to
        public event Action<GameViewModel> OnGameStateChanged;
        public event Action<List<ModifierDefinition>, int> OnDraftStarted; // options, picks required
        public event Action<GameOverReason, long> OnGameOver; // reason, final score
        public event Action OnLoadComplete;

        public GamePhase CurrentPhase => _phase;
        public ModifierRegistry Registry => _registry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        private void Start()
        {
            LoadModifiers();
        }

        private void InitializeSystems()
        {
            _registry = new ModifierRegistry();
            _resolver = new TurnResolver(_registry);
            _loader = new ModifierLoader(_registry);
        }

        private void LoadModifiers()
        {
            _phase = GamePhase.Loading;

            var result = _loader.LoadFromStreamingAssets("Modifiers");
            result.LogToConsole();

            if (result.HasErrors)
            {
                Debug.LogError("Failed to load modifiers. Check console for details.");
                return;
            }

            Debug.Log($"Loaded {_registry.Count} modifiers");
            _phase = GamePhase.MainMenu;
            OnLoadComplete?.Invoke();
        }

        /// <summary>
        /// Start a new game with optional seed.
        /// </summary>
        public void StartNewGame(int? seed = null)
        {
            int useSeed = seed ?? (_defaultSeed >= 0 ? _defaultSeed : UnityEngine.Random.Range(0, int.MaxValue));
            Debug.Log($"Starting new game with seed: {useSeed}");

            _selectedModifiers = new List<ModifierDefinition>();
            StartDraft(useSeed);
        }

        private void StartDraft(int seed)
        {
            _phase = GamePhase.Draft;

            // Get 5 random modifiers for draft
            _draftOptions = GetRandomModifiersForDraft(5, seed);

            OnDraftStarted?.Invoke(_draftOptions, 3);
        }

        private List<ModifierDefinition> GetRandomModifiersForDraft(int count, int seed)
        {
            var rng = new SeededRandom(seed);
            var allModifiers = new List<ModifierDefinition>(_registry.GetAll());
            var result = new List<ModifierDefinition>();

            // Shuffle using seeded RNG
            for (int i = allModifiers.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = allModifiers[i];
                allModifiers[i] = allModifiers[j];
                allModifiers[j] = temp;
            }

            // Take first 'count' modifiers
            for (int i = 0; i < count && i < allModifiers.Count; i++)
            {
                result.Add(allModifiers[i]);
            }

            return result;
        }

        /// <summary>
        /// Called by draft UI when player selects a modifier.
        /// </summary>
        public void SelectDraftModifier(string modifierId)
        {
            if (_phase != GamePhase.Draft) return;

            var modifier = _draftOptions.Find(m => m.Id == modifierId);
            if (modifier == null) return;

            _selectedModifiers.Add(modifier);
            _draftOptions.Remove(modifier);

            if (_selectedModifiers.Count >= 3)
            {
                StartGameWithModifiers();
            }
            else
            {
                OnDraftStarted?.Invoke(_draftOptions, 3 - _selectedModifiers.Count);
            }
        }

        private void StartGameWithModifiers()
        {
            int seed = _defaultSeed >= 0 ? _defaultSeed : UnityEngine.Random.Range(0, int.MaxValue);

            var startingModifiers = new List<ModifierInstance>();
            foreach (var def in _selectedModifiers)
            {
                startingModifiers.Add(ModifierInstance.FromDefinition(def));
            }

            _currentState = RunState.NewRun(seed, startingModifiers);
            _lastTurnResult = null;
            _phase = GamePhase.Playing;

            NotifyStateChanged();
        }

        /// <summary>
        /// Player action: Bank 25% of at-risk score.
        /// </summary>
        public void Bank25()
        {
            if (_phase != GamePhase.Playing) return;

            var result = _resolver.Bank(_currentState, 0.25f);
            if (result.Success)
            {
                _currentState = result.NewState;
                NotifyStateChanged();
            }
            else
            {
                Debug.LogWarning($"Bank failed: {result.FailureReason}");
            }
        }

        /// <summary>
        /// Player action: Bank 50% of at-risk score.
        /// </summary>
        public void Bank50()
        {
            if (_phase != GamePhase.Playing) return;

            var result = _resolver.Bank(_currentState, 0.5f);
            if (result.Success)
            {
                _currentState = result.NewState;
                NotifyStateChanged();
            }
            else
            {
                Debug.LogWarning($"Bank failed: {result.FailureReason}");
            }
        }

        /// <summary>
        /// Player action: Push (add risk for gain bonus).
        /// </summary>
        public void Push()
        {
            if (_phase != GamePhase.Playing) return;

            var result = _resolver.Push(_currentState);
            if (result.Success)
            {
                _currentState = result.NewState;
                NotifyStateChanged();
            }
            else
            {
                Debug.LogWarning($"Push failed: {result.FailureReason}");
            }
        }

        /// <summary>
        /// Player action: Sacrifice a modifier.
        /// </summary>
        public void Sacrifice(string modifierId, SacrificeChoice choice)
        {
            if (_phase != GamePhase.Playing) return;

            var result = _resolver.Sacrifice(_currentState, modifierId, choice);
            if (result.Success)
            {
                _currentState = result.NewState;
                NotifyStateChanged();
            }
            else
            {
                Debug.LogWarning($"Sacrifice failed: {result.FailureReason}");
            }
        }

        /// <summary>
        /// Player action: One More Turn.
        /// </summary>
        public void OneMoreTurn()
        {
            if (_phase != GamePhase.Playing) return;

            var (newState, turnResult) = _resolver.ResolveTurn(_currentState);
            _currentState = newState;
            _lastTurnResult = turnResult;

            if (_currentState.IsGameOver)
            {
                EndGame();
            }
            else
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Player action: Cash Out.
        /// </summary>
        public void CashOut()
        {
            if (_phase != GamePhase.Playing) return;

            _currentState = _resolver.CashOut(_currentState);
            EndGame();
        }

        private void EndGame()
        {
            _phase = GamePhase.GameOver;
            OnGameOver?.Invoke(_currentState.EndReason, _currentState.TotalScore);
        }

        private void NotifyStateChanged()
        {
            var viewModel = new GameViewModel(_currentState, _lastTurnResult, _registry);
            OnGameStateChanged?.Invoke(viewModel);
        }

        /// <summary>
        /// Return to main menu / restart.
        /// </summary>
        public void ReturnToMenu()
        {
            _phase = GamePhase.MainMenu;
            _currentState = null;
            _lastTurnResult = null;
        }
    }

    public enum GamePhase
    {
        Loading,
        MainMenu,
        Draft,
        Playing,
        GameOver
    }
}
