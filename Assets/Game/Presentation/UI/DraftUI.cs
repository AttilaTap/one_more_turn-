using System.Collections.Generic;
using OneMoreTurn.Core;
using OneMoreTurn.Presentation.ViewModels;
using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// UI for the modifier draft phase.
    /// </summary>
    public class DraftUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _instructionText;

        [Header("Options")]
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private GameObject _draftOptionPrefab;

        private List<DraftOptionUI> _options = new List<DraftOptionUI>();

        /// <summary>
        /// Called by UIManager when draft starts.
        /// </summary>
        public void OnDraftStarted(List<ModifierDefinition> options, int picksRemaining)
        {
            Debug.Log($"[DraftUI] OnDraftStarted called with {options?.Count ?? 0} options, {picksRemaining} picks remaining");
            Show();
            UpdateUI(options, picksRemaining);
        }

        private void UpdateUI(List<ModifierDefinition> options, int picksRemaining)
        {
            if (_titleText) _titleText.text = "Choose Your Modifiers";
            if (_instructionText) _instructionText.text = $"Select {picksRemaining} more modifier{(picksRemaining != 1 ? "s" : "")}";

            // Clear existing options
            foreach (var option in _options)
            {
                if (option != null && option.gameObject != null)
                {
                    Destroy(option.gameObject);
                }
            }
            _options.Clear();

            // Create new options
            Debug.Log($"[DraftUI] Container: {_optionsContainer != null}, Prefab: {_draftOptionPrefab != null}");
            if (_optionsContainer != null && _draftOptionPrefab != null)
            {
                foreach (var mod in options)
                {
                    Debug.Log($"[DraftUI] Creating option for: {mod.Name}");
                    var go = Instantiate(_draftOptionPrefab, _optionsContainer);
                    var option = go.GetComponent<DraftOptionUI>();
                    if (option != null)
                    {
                        option.Setup(mod, OnModifierSelected);
                        _options.Add(option);
                    }
                }
                Debug.Log($"[DraftUI] Created {_options.Count} options");
            }
            else
            {
                Debug.LogError("[DraftUI] Container or Prefab is null!");
            }
        }

        private void OnModifierSelected(string modifierId)
        {
            GameManager.Instance?.SelectDraftModifier(modifierId);

            // If all picks made, draft phase will end and this UI will be hidden
            if (GameManager.Instance?.CurrentPhase != GamePhase.Draft)
            {
                Hide();
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
