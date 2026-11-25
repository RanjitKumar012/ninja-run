using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Procedural running animation for the placeholder ninja built from primitives:
    /// swings arms and legs, bobs the body, flutters the headband, and plays a
    /// "fall over" pose on game over. Swap this for an Animator + a rigged model
    /// (e.g. from Mixamo) later without touching any gameplay code.
    /// </summary>
    public class NinjaAnimator : MonoBehaviour
    {
        [Header("Body Parts (pivots)")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform headbandTails;

        [Header("Run Cycle")]
        [Tooltip("Steps per second at the starting speed.")]
        [SerializeField] private float baseStepRate = 1.6f;
        [SerializeField] private float legSwingAngle = 45f;
        [SerializeField] private float armSwingAngle = 55f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float forwardLeanAngle = 12f;

        [Header("Game Over")]
        [SerializeField] private float fallAngle = -80f;
        [SerializeField] private float fallSpeed = 6f;

        private float phase;
        private Vector3 bodyStartPos;

        private void Awake()
        {
            if (body != null) bodyStartPos = body.localPosition;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            GameState state = gm != null ? gm.State : GameState.Menu;

            switch (state)
            {
                case GameState.Playing:
                    float speedFactor = gm.CurrentSpeed / Mathf.Max(0.01f, gm.StartSpeed);
                    phase += Time.deltaTime * baseStepRate * speedFactor * Mathf.PI * 2f;
                    PoseRunning(Mathf.Sin(phase));
                    break;

                case GameState.GameOver:
                    PoseFallen();
                    break;

                case GameState.Menu:
                    PoseIdle();
                    break;

                // Paused: Time.timeScale = 0, so the pose simply freezes.
            }
        }

        private void PoseRunning(float swing)
        {
            SetPitch(leftLeg, swing * legSwingAngle);
            SetPitch(rightLeg, -swing * legSwingAngle);
            SetPitch(leftArm, -swing * armSwingAngle);
            SetPitch(rightArm, swing * armSwingAngle);

            if (body != null)
            {
                body.localPosition = bodyStartPos + Vector3.up * (Mathf.Abs(Mathf.Cos(phase)) * bobHeight);
                body.localRotation = Quaternion.Euler(forwardLeanAngle, 0f, 0f);
            }

            if (headbandTails != null)
                headbandTails.localRotation = Quaternion.Euler(10f + Mathf.Sin(phase * 3f) * 12f, 0f, 0f);
        }

        private void PoseIdle()
        {
            float breathe = Mathf.Sin(Time.time * 2f);
            SetPitch(leftLeg, 0f);
            SetPitch(rightLeg, 0f);
            SetPitch(leftArm, 8f + breathe * 3f);
            SetPitch(rightArm, 8f + breathe * 3f);

            if (body != null)
            {
                body.localPosition = bodyStartPos + Vector3.up * (breathe * 0.015f);
                body.localRotation = Quaternion.identity;
            }

            if (headbandTails != null)
                headbandTails.localRotation = Quaternion.Euler(-55f + breathe * 3f, 0f, 0f);
        }

        private void PoseFallen()
        {
            // This object's pivot is at the feet, so rotating it tips the whole ninja backwards.
            Quaternion target = Quaternion.Euler(fallAngle, 0f, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target,
                1f - Mathf.Exp(-fallSpeed * Time.deltaTime));
        }

        private static void SetPitch(Transform t, float angle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
