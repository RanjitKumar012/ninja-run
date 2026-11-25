using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Places obstacles ahead of the ninja. Only one lane is ever blocked at a given
    /// spot, so there is always a way through. As the game speeds up, obstacles are
    /// spaced closer together in time (increasing difficulty).
    /// </summary>
    [DefaultExecutionOrder(-10)] // runs before CoinSpawner, so coins can avoid new obstacles
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Obstacle[] obstaclePrefabs;

        [Header("Placement")]
        [Tooltip("Z position of the very first obstacle (gives the player a warm-up).")]
        [SerializeField] private float firstObstacleZ = 45f;

        [Tooltip("How far ahead of the ninja obstacles are created.")]
        [SerializeField] private float spawnAheadDistance = 150f;

        [Tooltip("Obstacles this far behind the ninja go back to the pool.")]
        [SerializeField] private float despawnBehindDistance = 15f;

        [Header("Difficulty")]
        [Tooltip("Seconds of running between obstacles at the start.")]
        [SerializeField] private float startGapSeconds = 1.5f;

        [Tooltip("Seconds of running between obstacles at maximum difficulty.")]
        [SerializeField] private float minGapSeconds = 0.75f;

        [Tooltip("Never place obstacles closer than this (units).")]
        [SerializeField] private float minGapDistance = 11f;

        [SerializeField] private int prewarmPerType = 4;

        private PrefabPool<Obstacle>[] pools;
        private readonly List<Obstacle> active = new List<Obstacle>();
        private float nextSpawnZ;

        private void Awake()
        {
            pools = new PrefabPool<Obstacle>[obstaclePrefabs.Length];
            for (int i = 0; i < obstaclePrefabs.Length; i++)
                pools[i] = new PrefabPool<Obstacle>(obstaclePrefabs[i], transform, prewarmPerType);

            nextSpawnZ = firstObstacleZ;
        }

        private void Update()
        {
            float horizon = player.position.z + spawnAheadDistance;
            while (nextSpawnZ < horizon)
            {
                SpawnObstacle(nextSpawnZ);
                nextSpawnZ += NextGap();
            }

            // Obstacles are spawned in order of z, so the oldest is always first in the list.
            float despawnZ = player.position.z - despawnBehindDistance;
            while (active.Count > 0 && active[0].transform.position.z < despawnZ)
            {
                Obstacle obstacle = active[0];
                active.RemoveAt(0);
                pools[obstacle.PoolIndex].Release(obstacle);
            }
        }

        /// <summary>
        /// True if an obstacle in <paramref name="lane"/> overlaps position z
        /// (plus a safety margin). The CoinSpawner uses this to keep coins out of obstacles.
        /// </summary>
        public bool IsBlocked(PlayerController.Lane lane, float z, float margin)
        {
            foreach (Obstacle o in active)
            {
                if (o.Lane != lane) continue;
                if (Mathf.Abs(o.transform.position.z - z) < o.Length * 0.5f + margin) return true;
            }
            return false;
        }

        private void SpawnObstacle(float z)
        {
            int type = Random.Range(0, obstaclePrefabs.Length);
            var lane = Random.value < 0.5f ? PlayerController.Lane.Left : PlayerController.Lane.Right;

            Vector3 position = new Vector3(playerController.LaneToX(lane), 0f, z);
            Obstacle obstacle = pools[type].Get(position, Quaternion.identity);
            obstacle.Lane = lane;
            obstacle.PoolIndex = type;
            active.Add(obstacle);
        }

        private float NextGap()
        {
            GameManager gm = GameManager.Instance;
            float difficulty = gm != null ? gm.Difficulty01 : 0f;
            float speed = gm != null ? Mathf.Max(gm.CurrentSpeed, gm.StartSpeed) : 10f;

            // Gap measured in seconds of running, so it stays fair at any speed.
            float seconds = Mathf.Lerp(startGapSeconds, minGapSeconds, difficulty);
            float gap = speed * seconds * Random.Range(0.85f, 1.35f);
            return Mathf.Max(minGapDistance, gap);
        }
    }
}
