using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Plays background music and sound effects. It only *listens* to gameplay events
    /// (coin collected, lane changed, state changed), so gameplay scripts do not need
    /// to know that audio exists.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip laneSwitchClip;
        [SerializeField] private AudioClip crashClip;
        [SerializeField] private AudioClip clickClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;

        [Header("Event Sources")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CoinCollector coinCollector;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (musicSource != null && musicClip != null)
            {
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }

            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
            if (player != null) player.LaneChanged += OnLaneChanged;
            if (coinCollector != null) coinCollector.CoinCollected += OnCoinCollected;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
            if (player != null) player.LaneChanged -= OnLaneChanged;
            if (coinCollector != null) coinCollector.CoinCollected -= OnCoinCollected;
        }

        public void PlayClick() => PlaySfx(clickClip, 0.7f);

        private void OnCoinCollected(Coin coin) => PlaySfx(coinClip, 0.8f);

        private void OnLaneChanged(PlayerController.Lane lane) => PlaySfx(laneSwitchClip, 0.6f);

        private void OnStateChanged(GameState state)
        {
            if (musicSource == null) return;

            switch (state)
            {
                case GameState.Paused:
                    musicSource.Pause();
                    break;
                case GameState.Playing:
                    musicSource.volume = musicVolume;
                    musicSource.UnPause();
                    break;
                case GameState.GameOver:
                    PlaySfx(crashClip, 1f);
                    musicSource.volume = musicVolume * 0.35f;
                    break;
            }
        }

        private void PlaySfx(AudioClip clip, float volumeScale)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }
    }
}
