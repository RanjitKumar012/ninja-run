using UnityEngine;
using UnityEngine.UI;

namespace NinjaRunner
{
    /// <summary>
    /// The UI is designed for a 1080 x 1920 portrait phone. This keeps it fully on screen
    /// in any orientation: in portrait it scales to the screen width, in landscape
    /// (PC / editor / WebGL) it scales to the screen height.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        private CanvasScaler scaler;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) Apply();
        }

        private void Apply()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            bool landscape = Screen.width > Screen.height;
            scaler.matchWidthOrHeight = landscape ? 1f : 0f;
        }
    }
}
