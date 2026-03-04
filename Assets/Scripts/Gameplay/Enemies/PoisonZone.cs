using UnityEngine;
using SeagullStorm.Gameplay.Player;

namespace SeagullStorm.Gameplay.Enemies
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class PoisonZone : MonoBehaviour
    {
        public int damagePerTick = 5;
        public float tickInterval = 0.5f;

        private float _lastTickTime;

        private void Awake()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") && Time.time - _lastTickTime >= tickInterval)
            {
                _lastTickTime = Time.time;
                PlayerController.Instance?.TakeDamage(damagePerTick);
            }
        }
    }
}
