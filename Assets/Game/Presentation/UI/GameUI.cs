using System.Collections.Generic;
using OneMoreTurn.Core;
using OneMoreTurn.Presentation.ViewModels;
using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// Main gameplay UI controller.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private Text _turnText;
        [SerializeField] private Text _atRiskScoreText;
        [SerializeField] private Text _bankedScoreText;
        [SerializeField] private Text _totalScoreText;

        [Header("Risk Meter")]
        [SerializeField] private Slider _riskSlider;
        [SerializeField] private Image _riskFill;
        [SerializeField] private Text _riskText;
        [SerializeField] private Gradient _riskColorGradient;

        [Header("Action Buttons")]
        [SerializeField] private Button _bank25Button;
        [SerializeField] private Button _bank50Button;
        [SerializeField] private Button _pushButton;
        [SerializeField] private Text _pushButtonText;
        [SerializeField] private Button _oneMoreTurnButton;
        [SerializeField] private Button _cashOutButton;

        [Header("Turn Breakdown")]
        [SerializeField] private GameObject _breakdownPanel;
        [SerializeField] private Text _baseGainText;
        [SerializeField] private Text _pushBonusText;
        [SerializeField] private Text _finalGainText;
        [SerializeField] private Text _riskChangeText;

        [Header("Modifiers")]
        [SerializeField] private Transform _modifierContainer;
        [SerializeField] private GameObject _modifierItemPrefab;

        private List<ModifierItemUI> _modifierItems = new List<ModifierItemUI>();

        private void OnEnable()
        {
            // Wire up button events when enabled
            _bank25Button?.onClick.AddListener(() => GameManager.Instance?.Bank25());
            _bank50Button?.onClick.AddListener(() => GameManager.Instance?.Bank50());
            _pushButton?.onClick.AddListener(() => GameManager.Instance?.Push());
            _oneMoreTurnButton?.onClick.AddListener(() => GameManager.Instance?.OneMoreTurn());
            _cashOutButton?.onClick.AddListener(() => GameManager.Instance?.CashOut());
        }

        private void OnDisable()
        {
            // Clean up button listeners
            _bank25Button?.onClick.RemoveAllListeners();
            _bank50Button?.onClick.RemoveAllListeners();
            _pushButton?.onClick.RemoveAllListeners();
            _oneMoreTurnButton?.onClick.RemoveAllListeners();
            _cashOutButton?.onClick.RemoveAllListeners();
        }

        public void UpdateUI(GameViewModel viewModel)
        {
            if (viewModel == null) return;

            // Update turn and scores
            if (_turnText) _turnText.text = $"Turn {viewModel.Turn}";
            if (_atRiskScoreText) _atRiskScoreText.text = $"At Risk: {viewModel.AtRiskScore:N0}";
            if (_bankedScoreText) _bankedScoreText.text = $"Banked: {viewModel.BankedScore:N0}";
            if (_totalScoreText) _totalScoreText.text = $"Total: {viewModel.TotalScore:N0}";

            // Update risk meter
            if (_riskSlider)
            {
                _riskSlider.value = viewModel.Risk;
            }
            if (_riskText)
            {
                _riskText.text = $"{viewModel.Risk * 100:F0}%";
            }
            if (_riskFill && _riskColorGradient != null)
            {
                _riskFill.color = _riskColorGradient.Evaluate(viewModel.Risk);
            }

            // Update button states
            if (_bank25Button) _bank25Button.interactable = viewModel.CanBank;
            if (_bank50Button) _bank50Button.interactable = viewModel.CanBank;
            if (_pushButton)
            {
                _pushButton.interactable = viewModel.CanPush;
                if (_pushButtonText)
                {
                    _pushButtonText.text = $"Push ({viewModel.PushStacksUsed}/{viewModel.PushStacksMax})";
                }
            }
            if (_oneMoreTurnButton) _oneMoreTurnButton.interactable = !viewModel.IsGameOver;
            if (_cashOutButton) _cashOutButton.interactable = !viewModel.IsGameOver;

            // Update turn breakdown
            UpdateBreakdown(viewModel.LastTurn);

            // Update modifiers
            UpdateModifiers(viewModel.Modifiers);
        }

        private void UpdateBreakdown(TurnBreakdownViewModel breakdown)
        {
            if (_breakdownPanel)
            {
                _breakdownPanel.SetActive(breakdown != null);
            }

            if (breakdown == null) return;

            if (_baseGainText) _baseGainText.text = $"Base: +{breakdown.BaseGain}";
            if (_pushBonusText)
            {
                _pushBonusText.gameObject.SetActive(breakdown.PushMultiplier > 1f);
                _pushBonusText.text = $"Push: x{breakdown.PushMultiplier:F1}";
            }
            if (_finalGainText) _finalGainText.text = $"Gain: +{breakdown.FinalGain}";
            if (_riskChangeText)
            {
                _riskChangeText.text = $"Risk: +{breakdown.FinalRiskDelta * 100:F1}%";
            }
        }

        private void UpdateModifiers(IReadOnlyList<ModifierViewModel> modifiers)
        {
            if (_modifierContainer == null || _modifierItemPrefab == null) return;

            // Clear existing items
            foreach (var item in _modifierItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _modifierItems.Clear();

            // Create new items
            foreach (var mod in modifiers)
            {
                var go = Instantiate(_modifierItemPrefab, _modifierContainer);
                var item = go.GetComponent<ModifierItemUI>();
                if (item != null)
                {
                    item.Setup(mod);
                    _modifierItems.Add(item);
                }
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
