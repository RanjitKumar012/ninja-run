using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Third-person camera with two views:
    ///  • Menu view    – stands in front of the ninja so the player sees the character.
    ///  • Gameplay view – behind and above the ninja, looking ahead, ninja in the
    ///                    centre-bottom of the screen.
    /// When the run starts, the camera swings smoothly from the menu view to the
    /// gameplay view. Also shakes briefly when the ninja crashes.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Gameplay View")]
        [Tooltip("Camera position relative to the ninja while running.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.2f, -7f);

        [Tooltip("Point the camera looks at while running, relative to the ninja.")]
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 6f);

        [Tooltip("0 = camera stays centred on the road, 1 = fully follows lane changes.")]
        [Range(0f, 1f)]
        [SerializeField] private float laneFollowAmount = 0.5f;

        [SerializeField] private float sideSmoothTime = 0.2f;

        [Header("Menu View")]
        [Tooltip("Camera position relative to the ninja on the start screen (in front of him).")]
        [SerializeField] private Vector3 menuOffset = new Vector3(1.5f, 1.9f, 3.6f);

        [Tooltip("Point the camera looks at on the start screen, relative to the ninja.")]
        [SerializeField] private Vector3 menuLookOffset = new Vector3(0.25f, 1.15f, 0f);

        [Tooltip("Seconds the camera takes to swing from the menu view to the gameplay view.")]
        [SerializeField] private float transitionDuration = 1.1f;

        [Tooltip("How far the swing curves out to the side / upward, so it goes around the ninja.")]
        [SerializeField] private float swingArcSide = 3.5f;
        [SerializeField] private float swingArcHeight = 1.5f;

        [Header("Crash Shake")]
        [SerializeField] private float shakeDuration = 0.35f;
        [SerializeField] private float shakeMagnitude = 0.25f;

        private float currentX;
        private float xVelocity;
        private float shakeTimeLeft;
        private float blend; // 0 = menu view, 1 = gameplay view

        private void Start()
        {
            if (target != null) currentX = target.position.x * laneFollowAmount;

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                // After "Restart" the game starts immediately, so skip the menu view.
                blend = gm.State == GameState.Menu ? 0f : 1f;
            }

            LateUpdate();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        // LateUpdate runs after the player has moved this frame, which avoids jitter.
        private void LateUpdate()
        {
            if (target == null) return;

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Menu)
                blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime / Mathf.Max(0.01f, transitionDuration));

            // --- Gameplay pose ---
            currentX = Mathf.SmoothDamp(currentX, target.position.x * laneFollowAmount,
                ref xVelocity, sideSmoothTime);
            Vector3 anchor = new Vector3(currentX, target.position.y, target.position.z);
            Vector3 playPos = anchor + offset;
            Vector3 playLook = anchor + lookOffset;

            // --- Menu pose ---
            Vector3 menuPos = target.position + menuOffset;
            Vector3 menuLook = target.position + menuLookOffset;

            // --- Blend: smoothstep easing, curved path around the ninja, always looking at him ---
            float t = blend * blend * (3f - 2f * blend);
            float arc = Mathf.Sin(t * Mathf.PI); // 0 at both ends, 1 half-way
            Vector3 position = Vector3.Lerp(menuPos, playPos, t)
                               + new Vector3(swingArcSide * arc, swingArcHeight * arc, 0f);
            Vector3 lookAt = Vector3.Lerp(menuLook, playLook, t);
            Quaternion rotation = Quaternion.LookRotation(lookAt - position);

            if (shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= Time.deltaTime;
                float strength = shakeMagnitude * (shakeTimeLeft / shakeDuration);
                position += Random.insideUnitSphere * strength;
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        public void Shake() => shakeTimeLeft = shakeDuration;

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.GameOver) Shake();
        }
    }
}
