using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NinjaRunner
{
    /// <summary>
    /// Shows the right screen for each game state (Start, HUD, Pause, Game Over),
    /// keeps the coin and score counters up to date, and contains the methods that
    /// the UI buttons call (connected in each Button / EventTrigger in the Inspector).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("HUD")]
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Start Screen")]
        [SerializeField] private TMP_Text menuBestText;

        [Header("Game Over Screen")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalCoinsText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private GameObject newBestLabel;

        [Tooltip("Short pause after the crash before the Game Over screen appears.")]
        [SerializeField] private float gameOverDelay = 0.8f;

        [Header("References")]
        [SerializeField] private PlayerController player;

        private GameManager gm;
        private Coroutine gameOverRoutine;

        private void Start()
        {
            gm = GameManager.Instance;
            gm.StateChanged += OnStateChanged;
            gm.CoinsChanged += UpdateCoins;
            gm.ScoreChanged += UpdateScore;

            OnStateChanged(gm.State);
            UpdateCoins(gm.Coins);
            UpdateScore(gm.Score);
        }

        private void OnDestroy()
        {
            if (gm == null) return;
            gm.StateChanged -= OnStateChanged;
            gm.CoinsChanged -= UpdateCoins;
            gm.ScoreChanged -= UpdateScore;
        }

        private void Update()
        {
            // Keyboard shortcuts for the menus (on mobile, the buttons do the same).
            if (InputHelper.PausePressed()) gm.TogglePause();

            if (InputHelper.ConfirmPressed())
            {
                switch (gm.State)
                {
                    case GameState.Menu: OnPlayButton(); break;
                    case GameState.Paused: OnResumeButton(); break;
                    case GameState.GameOver:
                        if (gameOverPanel.activeSelf) OnRestartButton();
                        break;
                }
            }
        }

        // ---------- Button handlers (assigned in the Inspector) ----------

        public void OnPlayButton()
        {
            ButtonFeedback();
            gm.StartGame();
        }

        public void OnPauseButton()
        {
            ButtonFeedback();
            gm.TogglePause();
        }

        public void OnResumeButton()
        {
            ButtonFeedback();
            gm.ResumeGame();
        }

        public void OnRestartButton()
        {
            ButtonFeedback();
            gm.RestartGame();
        }

        public void OnLeftButton() => player.MoveLeft();

        public void OnRightButton() => player.MoveRight();

        // ---------- State → screens ----------

        private void OnStateChanged(GameState state)
        {
            startPanel.SetActive(state == GameState.Menu);
            hudPanel.SetActive(state == GameState.Playing || state == GameState.Paused);
            pausePanel.SetActive(state == GameState.Paused);

            if (state == GameState.Menu)
                menuBestText.text = $"BEST  {gm.HighScore}";

            if (gameOverRoutine != null)
            {
                StopCoroutine(gameOverRoutine);
                gameOverRoutine = null;
            }

            if (state == GameState.GameOver)
                gameOverRoutine = StartCoroutine(ShowGameOverAfterDelay());
            else
                gameOverPanel.SetActive(false);
        }

        private IEnumerator ShowGameOverAfterDelay()
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);

            hudPanel.SetActive(false);
            finalScoreText.text = $"SCORE  {gm.Score}";
            finalCoinsText.text = $"COINS  {gm.Coins}";
            bestScoreText.text = $"BEST  {gm.HighScore}";
            newBestLabel.SetActive(gm.IsNewHighScore);
            gameOverPanel.SetActive(true);
            gameOverRoutine = null;
        }

        private void UpdateCoins(int coins) => coinText.text = coins.ToString();

        private void UpdateScore(int score) => scoreText.text = score.ToString();

        private void ButtonFeedback()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

            // Deselect the button, otherwise Space/Enter would "click" it again.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
