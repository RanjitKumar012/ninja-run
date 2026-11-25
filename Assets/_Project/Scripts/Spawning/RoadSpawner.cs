using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Creates an endless road from a fixed set of road segments. When a segment is
    /// far enough behind the ninja it is moved to the front of the road and its
    /// decorations are re-randomised (segment recycling: no Instantiate/Destroy while playing).
    /// </summary>
    public class RoadSpawner : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private RoadSegment segmentPrefab;

        [Tooltip("How many segments are kept in front of the ninja.")]
        [SerializeField] private int segmentsAhead = 12;

        [Tooltip("How many segments are kept behind the ninja (visible to the camera).")]
        [SerializeField] private int segmentsBehind = 2;

        private readonly Queue<RoadSegment> segments = new Queue<RoadSegment>();
        private float nextSpawnZ;
        private float segmentLength;

        private void Start()
        {
            segmentLength = segmentPrefab.Length;
            nextSpawnZ = player.position.z - segmentsBehind * segmentLength;

            int total = segmentsAhead + segmentsBehind;
            for (int i = 0; i < total; i++)
            {
                RoadSegment segment = Instantiate(segmentPrefab, transform);
                segment.name = $"RoadSegment_{i:00}";
                PlaceAtFront(segment, randomize: i > segmentsBehind + 1); // keep the start area clean
                segments.Enqueue(segment);
            }
        }

        private void Update()
        {
            // Recycle every segment that has fallen too far behind the ninja.
            while (segments.Count > 0 &&
                   player.position.z - segments.Peek().transform.position.z > (segmentsBehind + 1) * segmentLength)
            {
                RoadSegment oldest = segments.Dequeue();
                PlaceAtFront(oldest, randomize: true);
                segments.Enqueue(oldest);
            }
        }

        private void PlaceAtFront(RoadSegment segment, bool randomize)
        {
            segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);
            if (randomize) segment.Randomize();
            else segment.HideOptionalDecorations();
            nextSpawnZ += segmentLength;
        }
    }
}
