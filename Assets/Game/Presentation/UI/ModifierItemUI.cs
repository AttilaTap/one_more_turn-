using OneMoreTurn.Core;
using OneMoreTurn.Presentation.ViewModels;
using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// UI component for displaying a single modifier.
    /// </summary>
    public class ModifierItemUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _durationText;
        [SerializeField] private Image _rarityBorder;

        [Header("Sacrifice")]
        [SerializeField] private Button _sacrificeRiskButton;
        [SerializeField] private Button _sacrificeScoreButton;
        [SerializeField] private Text _sacrificeRiskText;
        [SerializeField] private Text _sacrificeScoreText;

        [Header("Rarity Colors")]
        [SerializeField] private Color _commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _rareColor = new Color(0.8f, 0.6f, 0.2f);

        private ModifierViewModel _viewModel;

        public void Setup(ModifierViewModel viewModel)
        {
            _viewModel = viewModel;

            if (_nameText) _nameText.text = viewModel.Name;
            if (_descriptionText) _descriptionText.text = viewModel.Description;

            // Duration display
            if (_durationText)
            {
                if (viewModel.TurnsRemaining > 0)
                {
                    _durationText.text = $"{viewModel.TurnsRemaining} turns";
                    _durationText.gameObject.SetActive(true);
                }
                else
                {
                    _durationText.gameObject.SetActive(false);
                }
            }

            // Rarity color
            if (_rarityBorder)
            {
                _rarityBorder.color = viewModel.Rarity switch
                {
                    ModifierRarity.Common => _commonColor,
                    ModifierRarity.Uncommon => _uncommonColor,
                    ModifierRarity.Rare => _rareColor,
                    _ => _commonColor
                };
            }

            // Sacrifice buttons
            if (_sacrificeRiskText)
            {
                _sacrificeRiskText.text = $"-{viewModel.SacrificeRiskReduction * 100:F0}% Risk";
            }
            if (_sacrificeScoreText)
            {
                _sacrificeScoreText.text = $"+{viewModel.SacrificeScoreGain} Score";
            }

            // Wire up sacrifice buttons
            if (_sacrificeRiskButton)
            {
                _sacrificeRiskButton.onClick.RemoveAllListeners();
                _sacrificeRiskButton.onClick.AddListener(OnSacrificeRisk);
            }
            if (_sacrificeScoreButton)
            {
                _sacrificeScoreButton.onClick.RemoveAllListeners();
                _sacrificeScoreButton.onClick.AddListener(OnSacrificeScore);
            }
        }

        private void OnSacrificeRisk()
        {
            GameManager.Instance?.Sacrifice(_viewModel.Id, SacrificeChoice.ReduceRisk);
        }

        private void OnSacrificeScore()
        {
            GameManager.Instance?.Sacrifice(_viewModel.Id, SacrificeChoice.GainScore);
        }
    }
}
