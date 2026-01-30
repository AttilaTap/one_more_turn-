using System;
using OneMoreTurn.Core;
using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// UI component for a single draft option. The entire card is clickable.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DraftOptionUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _rarityText;
        [SerializeField] private Image _background;

        [Header("Rarity Colors")]
        [SerializeField] private Color _commonColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color _uncommonColor = new Color(0.2f, 0.5f, 0.2f);
        [SerializeField] private Color _rareColor = new Color(0.5f, 0.4f, 0.1f);

        private Button _button;
        private ModifierDefinition _definition;
        private Action<string> _onSelect;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Setup(ModifierDefinition definition, Action<string> onSelect)
        {
            _definition = definition;
            _onSelect = onSelect;

            if (_nameText) _nameText.text = definition.Name;
            if (_descriptionText) _descriptionText.text = definition.Description;

            if (_rarityText)
            {
                _rarityText.text = definition.Rarity.ToString();
            }

            if (_background)
            {
                _background.color = definition.Rarity switch
                {
                    ModifierRarity.Common => _commonColor,
                    ModifierRarity.Uncommon => _uncommonColor,
                    ModifierRarity.Rare => _rareColor,
                    _ => _commonColor
                };
            }

            if (_button)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            _onSelect?.Invoke(_definition.Id);
        }
    }
}
