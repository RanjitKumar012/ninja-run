using System;
using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Moves the ninja: runs forward automatically and switches smoothly between
    /// exactly two lanes. Uses a kinematic Rigidbody so trigger colliders
    /// (coins and obstacles) are detected reliably.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        public enum Lane
        {
            Left = 0,
            Right = 1
        }

        [Header("Lanes")]
        [Tooltip("Distance of each lane from the centre of the road.")]
        [SerializeField] private float laneOffset = 1.5f;

        [Tooltip("Lane the ninja starts in.")]
        [SerializeField] private Lane startLane = Lane.Left;

        [Tooltip("Roughly how long (seconds) a lane switch takes. Smaller = snappier.")]
        [SerializeField] private float laneSwitchTime = 0.12f;

        [Header("Visuals")]
        [Tooltip("Child object that holds the ninja model (it leans while switching lanes).")]
        [SerializeField] private Transform model;

        [Tooltip("Lean angle (degrees) per unit of sideways speed.")]
        [SerializeField] private float leanPerUnitSpeed = 1.6f;

        [SerializeField] private float maxLeanAngle = 18f;

        private Rigidbody rb;
        private float targetX;
        private float sidewaysVelocity;
        private float currentLean;

        /// <summary>Raised whenever the player changes lane (used for sound effects).</summary>
        public event Action<Lane> LaneChanged;

        public Lane CurrentLane { get; private set; }
        public bool IsDead { get; private set; }

        private bool CanControl =>
            !IsDead && GameManager.Instance != null && GameManager.Instance.IsPlaying;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;   // we move it ourselves; physics only reports triggers
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth camera follow

            CurrentLane = startLane;
            targetX = LaneToX(startLane);

            Vector3 p = transform.position;
            p.x = targetX;
            transform.position = p;
        }

        private void Update()
        {
            if (CanControl)
            {
                if (InputHelper.LeftPressed()) MoveLeft();
                if (InputHelper.RightPressed()) MoveRight();
            }

            UpdateLean();
        }

        private void FixedUpdate()
        {
            if (!CanControl) return;

            Vector3 pos = rb.position;

            // 1) Automatic forward running.
            pos.z += GameManager.Instance.CurrentSpeed * Time.fixedDeltaTime;

            // 2) Smooth sideways movement toward the target lane.
            pos.x = Mathf.SmoothDamp(pos.x, targetX, ref sidewaysVelocity, laneSwitchTime,
                Mathf.Infinity, Time.fixedDeltaTime);

            rb.MovePosition(pos);
        }

        // ---------- Public controls (keyboard and on-screen buttons call these) ----------

        public void MoveLeft() => SetLane(Lane.Left);

        public void MoveRight() => SetLane(Lane.Right);

        /// <summary>World X position of a lane.</summary>
        public float LaneToX(Lane lane) => lane == Lane.Left ? -laneOffset : laneOffset;

        // ---------- Collisions ----------

        private void OnTriggerEnter(Collider other)
        {
            if (IsDead || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            if (other.GetComponentInParent<Obstacle>() != null)
            {
                IsDead = true;
                GameManager.Instance.GameOver();
            }
        }

        // ---------- Helpers ----------

        private void SetLane(Lane lane)
        {
            // Only two lanes exist, so the player can never leave the road.
            if (!CanControl || lane == CurrentLane) return;

            CurrentLane = lane;
            targetX = LaneToX(lane);
            LaneChanged?.Invoke(lane);
        }

        private void UpdateLean()
        {
            if (model == null) return;

            float targetLean = IsDead
                ? 0f
                : Mathf.Clamp(-sidewaysVelocity * leanPerUnitSpeed, -maxLeanAngle, maxLeanAngle);

            currentLean = Mathf.Lerp(currentLean, targetLean, 1f - Mathf.Exp(-15f * Time.deltaTime));
            model.localRotation = Quaternion.Euler(0f, 0f, currentLean);
        }
    }
}
