using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Places rows of coins ahead of the ninja. Some rows switch lane half-way to
    /// encourage movement. Coins are never placed inside an obstacle.
    /// </summary>
    public class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ObstacleSpawner obstacleSpawner;
        [SerializeField] private Coin coinPrefab;

        [Header("Placement")]
        [SerializeField] private float firstCoinZ = 12f;

        [Tooltip("Must be smaller than the ObstacleSpawner's distance, so obstacles are placed first.")]
        [SerializeField] private float spawnAheadDistance = 130f;

        [SerializeField] private float despawnBehindDistance = 10f;
        [SerializeField] private float coinHeight = 1f;
        [SerializeField] private float coinSpacing = 2f;

        [Header("Rows")]
        [SerializeField] private int minCoinsPerRow = 5;
        [SerializeField] private int maxCoinsPerRow = 10;
        [SerializeField] private float minGapBetweenRows = 8f;
        [SerializeField] private float maxGapBetweenRows = 22f;

        [Tooltip("Chance that a row switches lanes part-way through.")]
        [Range(0f, 1f)]
        [SerializeField] private float laneSwitchChance = 0.35f;

        [Tooltip("Free space kept between a coin and an obstacle.")]
        [SerializeField] private float obstacleClearance = 1.5f;

        [SerializeField] private int prewarmCount = 40;

        private PrefabPool<Coin> pool;
        private readonly Queue<Coin> active = new Queue<Coin>();
        private float nextRowZ;

        private void Awake()
        {
            pool = new PrefabPool<Coin>(coinPrefab, transform, prewarmCount);
            nextRowZ = firstCoinZ;
        }

        private void Update()
        {
            float horizon = player.position.z + spawnAheadDistance;
            while (nextRowZ < horizon)
                SpawnRow();

            // Return coins the ninja has passed (collected coins are already hidden).
            float despawnZ = player.position.z - despawnBehindDistance;
            while (active.Count > 0 && active.Peek().transform.position.z < despawnZ)
                pool.Release(active.Dequeue());
        }

        private void SpawnRow()
        {
            var lane = Random.value < 0.5f ? PlayerController.Lane.Left : PlayerController.Lane.Right;
            int count = Random.Range(minCoinsPerRow, maxCoinsPerRow + 1);
            int switchAt = Random.value < laneSwitchChance ? Random.Range(2, count - 1) : -1;

            float z = nextRowZ;
            for (int i = 0; i < count; i++)
            {
                if (i == switchAt) lane = Opposite(lane);

                if (!obstacleSpawner.IsBlocked(lane, z, obstacleClearance))
                {
                    var position = new Vector3(playerController.LaneToX(lane), coinHeight, z);
                    active.Enqueue(pool.Get(position, Quaternion.identity));
                }

                z += coinSpacing;
            }

            nextRowZ = z + Random.Range(minGapBetweenRows, maxGapBetweenRows);
        }

        private static PlayerController.Lane Opposite(PlayerController.Lane lane) =>
            lane == PlayerController.Lane.Left ? PlayerController.Lane.Right : PlayerController.Lane.Left;
    }
}
