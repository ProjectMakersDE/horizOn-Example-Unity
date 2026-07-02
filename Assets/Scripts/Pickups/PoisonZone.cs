using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Poison zone dropped by jellyfish: damages the player while they stay inside.
    /// </summary>
    public class PoisonZone : MonoBehaviour
    {
        [SerializeField] private int damagePerTick = 5;
        [SerializeField] private float tickInterval = 1f;

        private float _cooldown;

        private void OnEnable()
        {
            _cooldown = 0f;
        }

        private void Update()
        {
            _cooldown -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_cooldown > 0f) return;

            if (other.CompareTag("Player"))
            {
                _cooldown = tickInterval;
                PlayerController.Instance?.TakeDamage(damagePerTick);
            }
        }
    }
}
