using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// Main menu UI.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _quitButton;

        [Header("Display")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _loadingText;

        private void Start()
        {
            if (_startGameButton)
            {
                _startGameButton.onClick.AddListener(OnStartGame);
                _startGameButton.interactable = false; // Wait for load
            }

            if (_quitButton)
            {
                _quitButton.onClick.AddListener(OnQuit);
            }

            if (_loadingText)
            {
                _loadingText.text = "Loading modifiers...";
                _loadingText.gameObject.SetActive(true);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoadComplete += OnLoadComplete;

                // Check if already loaded
                if (GameManager.Instance.CurrentPhase != GamePhase.Loading)
                {
                    OnLoadComplete();
                }
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoadComplete -= OnLoadComplete;
            }
        }

        private void OnLoadComplete()
        {
            if (_startGameButton)
            {
                _startGameButton.interactable = true;
            }

            if (_loadingText)
            {
                _loadingText.gameObject.SetActive(false);
            }
        }

        private void OnStartGame()
        {
            Hide();
            GameManager.Instance?.StartNewGame();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
