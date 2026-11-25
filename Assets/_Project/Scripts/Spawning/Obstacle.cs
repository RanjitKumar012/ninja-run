using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Marks a prefab as an obstacle. Its trigger collider ends the run when the
    /// ninja touches it (see PlayerController.OnTriggerEnter).
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Tooltip("Depth of the obstacle along the road. Used so coins are never placed inside it.")]
        [SerializeField] private float length = 1.2f;

        public float Length => length;

        /// <summary>Lane this instance was spawned in (set by the ObstacleSpawner).</summary>
        public PlayerController.Lane Lane { get; set; }

        /// <summary>Index of the prefab it came from, so it returns to the right pool.</summary>
        public int PoolIndex { get; set; }
    }
}
