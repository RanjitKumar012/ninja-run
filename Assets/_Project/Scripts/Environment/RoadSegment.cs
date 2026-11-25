using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// One 20 m piece of the stone road with its scenery (lanterns, cherry trees,
    /// optional torii gate and pagodas). Re-randomised each time it is recycled,
    /// so the world never looks exactly the same twice.
    /// </summary>
    public class RoadSegment : MonoBehaviour
    {
        [Tooltip("Length of this segment along the road (Z axis).")]
        [SerializeField] private float length = 20f;

        [Header("Optional Decorations")]
        [SerializeField] private GameObject[] toriiGates;
        [Range(0f, 1f)] [SerializeField] private float toriiChance = 0.35f;

        [SerializeField] private GameObject[] pagodas;
        [Range(0f, 1f)] [SerializeField] private float pagodaChance = 0.3f;

        [Header("Variation")]
        [Tooltip("Objects (e.g. cherry trees) that get a random size and rotation.")]
        [SerializeField] private Transform[] randomizedProps;
        [SerializeField] private Vector2 propScaleRange = new Vector2(0.8f, 1.3f);

        public float Length => length;

        public void Randomize()
        {
            SetRandomActive(toriiGates, toriiChance);
            SetRandomActive(pagodas, pagodaChance);

            foreach (Transform prop in randomizedProps)
            {
                if (prop == null) continue;
                prop.localScale = Vector3.one * Random.Range(propScaleRange.x, propScaleRange.y);
                prop.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        public void HideOptionalDecorations()
        {
            SetRandomActive(toriiGates, 0f);
            SetRandomActive(pagodas, 0f);
        }

        private static void SetRandomActive(GameObject[] objects, float chance)
        {
            if (objects == null) return;
            foreach (GameObject go in objects)
                if (go != null) go.SetActive(Random.value < chance);
        }
    }
}
