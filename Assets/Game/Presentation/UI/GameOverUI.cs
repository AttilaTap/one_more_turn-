using OneMoreTurn.Core;
using UnityEngine;
using UnityEngine.UI;


namespace OneMoreTurn.Presentation.UI
{
    /// <summary>
    /// UI for game over screen.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Image _backgroundImage;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Colors")]
        [SerializeField] private Color _bustColor = new Color(0.5f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color _cashOutColor = new Color(0.1f, 0.4f, 0.1f, 0.9f);

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += OnGameOver;
            }

            if (_playAgainButton)
            {
                _playAgainButton.onClick.AddListener(OnPlayAgain);
            }

            if (_mainMenuButton)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenu);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= OnGameOver;
            }
        }

        private void OnGameOver(GameOverReason reason, long finalScore)
        {
            Show();

            bool isBust = reason == GameOverReason.Bust;

            if (_titleText)
            {
                _titleText.text = isBust ? "BUST!" : "CASHED OUT!";
            }

            if (_messageText)
            {
                _messageText.text = isBust
                    ? "You pushed too far and lost it all..."
                    : "Smart move! You secured your winnings.";
            }

            if (_scoreText)
            {
                _scoreText.text = $"Final Score: {finalScore:N0}";
            }

            if (_backgroundImage)
            {
                _backgroundImage.color = isBust ? _bustColor : _cashOutColor;
            }
        }

        private void OnPlayAgain()
        {
            Hide();
            GameManager.Instance?.StartNewGame();
        }

        private void OnMainMenu()
        {
            Hide();
            GameManager.Instance?.ReturnToMenu();
            // TODO: Show main menu UI
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
