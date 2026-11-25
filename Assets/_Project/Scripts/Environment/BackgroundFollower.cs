using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Keeps far-away scenery (mountains, clouds) at a fixed distance in front of the
    /// ninja, so the horizon is always visible however far the ninja runs.
    /// </summary>
    public class BackgroundFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = transform.position;
            p.z = target.position.z;
            transform.position = p;
        }
    }
}
