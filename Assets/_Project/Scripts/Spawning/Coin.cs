using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// A collectable gold coin: spins and bobs, and disappears when collected.
    /// The CoinSpawner returns it to the pool once the player has passed it.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [Tooltip("The visible coin mesh (child), so the trigger collider on the root stays still.")]
        [SerializeField] private Transform visual;

        [SerializeField] private float spinSpeed = 180f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 3f;

        private float bobOffset;

        public bool IsCollectable { get; private set; }

        private void OnEnable()
        {
            IsCollectable = true;
            bobOffset = Random.value * Mathf.PI * 2f; // so coins don't all bob in sync
        }

        private void Update()
        {
            if (visual == null) return;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight);
        }

        public void Collect()
        {
            IsCollectable = false;
            gameObject.SetActive(false);
        }
    }
}
