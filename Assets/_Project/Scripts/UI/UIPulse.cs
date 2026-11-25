using UnityEngine;
using UnityEngine.UI;

namespace NinjaRunner
{
    /// <summary>
    /// Gently pulses a UI element (size and transparency), e.g. the "TAP TO PLAY" text.
    /// Uses unscaled time so it also animates while the game is paused.
    /// </summary>
    public class UIPulse : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private float scaleAmount = 0.06f;
        [Range(0f, 1f)] [SerializeField] private float minAlpha = 0.45f;

        private Graphic graphic;
        private Color baseColor;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            if (graphic != null) baseColor = graphic.color;
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f; // 0..1
            transform.localScale = Vector3.one * (1f + wave * scaleAmount);

            if (graphic != null)
            {
                Color c = baseColor;
                c.a = baseColor.a * Mathf.Lerp(minAlpha, 1f, wave);
                graphic.color = c;
            }
        }
    }
}
