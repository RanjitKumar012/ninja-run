using System;
using UnityEngine;

namespace NinjaRunner
{
    /// <summary>
    /// Lives on the player. When the player's collider touches a coin's trigger,
    /// the coin is collected and the GameManager adds it to the counter.
    /// </summary>
    public class CoinCollector : MonoBehaviour
    {
        /// <summary>Raised after a coin is collected (the AudioManager plays a sound).</summary>
        public event Action<Coin> CoinCollected;

        private void OnTriggerEnter(Collider other)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;

            Coin coin = other.GetComponentInParent<Coin>();
            if (coin == null || !coin.IsCollectable) return;

            coin.Collect();
            gm.AddCoin();
            CoinCollected?.Invoke(coin);
        }
    }
}
