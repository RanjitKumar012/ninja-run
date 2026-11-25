using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaRunner
{
    /// <summary>The four states the game can be in.</summary>
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Central brain of the game: owns the game state, running speed, distance, coins,
    /// score and high score. Other systems never talk to each other directly for these;
    /// they read values from here and listen to its events (Observer pattern).
    /// </summary>
    [DefaultExecutionOrder(-100)] // initialise before every other script
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Speed & Difficulty")]
        [Tooltip("Running speed (units per second) when a run starts.")]
        [SerializeField] private float startSpeed = 10f;

        [Tooltip("Highest speed the ninja can reach.")]
        [SerializeField] private float maxSpeed = 26f;

        [Tooltip("How much speed is added every second while playing.")]
        [SerializeField] private float speedIncreasePerSecond = 0.22f;

        [Header("Scoring")]
        [Tooltip("Bonus points for every coin collected (distance gives 1 point per metre).")]
        [SerializeField] private int pointsPerCoin = 10;

        private const string HighScoreKey = "NinjaRunner.HighScore";

        // Set before reloading the scene so that "Restart" skips the start screen.
        private static bool skipMenuOnNextLoad;

        private int lastReportedScore = -1;

        // ---------- Events (UI, audio and camera subscribe to these) ----------
        public event Action<GameState> StateChanged;
        public event Action<int> CoinsChanged;
        public event Action<int> ScoreChanged;

        // ---------- Read-only state ----------
        public GameState State { get; private set; } = GameState.Menu;
        public float CurrentSpeed { get; private set; }
        public float Distance { get; private set; }
        public int Coins { get; private set; }
        public int HighScore { get; private set; }
        public bool IsNewHighScore { get; private set; }
        public float StartSpeed => startSpeed;
        public float MaxSpeed => maxSpeed;
        public bool IsPlaying => State == GameState.Playing;

        /// <summary>Total score = metres run + coin bonus.</summary>
        public int Score => Mathf.FloorToInt(Distance) + Coins * pointsPerCoin;

        /// <summary>0 at the start speed, 1 at max speed. Spawners use it to ramp up difficulty.</summary>
        public float Difficulty01 => Mathf.InverseLerp(startSpeed, maxSpeed, CurrentSpeed);

        // Static fields survive between Play sessions when "Enter Play Mode Options" skips
        // domain reload, so reset them explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            skipMenuOnNextLoad = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
            CurrentSpeed = startSpeed;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Start()
        {
            if (skipMenuOnNextLoad)
            {
                skipMenuOnNextLoad = false;
                StartGame();
            }
            else
            {
                SetState(GameState.Menu);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            // Difficulty: speed rises gradually until it reaches maxSpeed.
            CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + speedIncreasePerSecond * Time.deltaTime);
            Distance += CurrentSpeed * Time.deltaTime;

            NotifyScoreIfChanged();
        }

        // ---------- Public API (called by UI buttons, the player, etc.) ----------

        public void StartGame()
        {
            if (State == GameState.Playing) return;

            CurrentSpeed = startSpeed;
            Distance = 0f;
            Coins = 0;
            IsNewHighScore = false;
            Time.timeScale = 1f;

            SetState(GameState.Playing);
            CoinsChanged?.Invoke(Coins);
            NotifyScoreIfChanged(force: true);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) PauseGame();
            else if (State == GameState.Paused) ResumeGame();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f; // freezes movement, physics and animations
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void AddCoin()
        {
            if (!IsPlaying) return;
            Coins++;
            CoinsChanged?.Invoke(Coins);
            NotifyScoreIfChanged();
        }

        /// <summary>Called by the player when it hits an obstacle.</summary>
        public void GameOver()
        {
            if (State != GameState.Playing) return;

            if (Score > HighScore)
            {
                HighScore = Score;
                IsNewHighScore = true;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            SetState(GameState.GameOver);
        }

        /// <summary>Reloads the scene and jumps straight into a new run.</summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            skipMenuOnNextLoad = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ---------- Helpers ----------

        private void SetState(GameState newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }

        private void NotifyScoreIfChanged(bool force = false)
        {
            int score = Score;
            if (!force && score == lastReportedScore) return;
            lastReportedScore = score;
            ScoreChanged?.Invoke(score);
        }
    }
}
