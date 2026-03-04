using UnityEngine;

namespace SeagullStorm.Gameplay.Enemies
{
    public class JellyfishEnemy : EnemyBase
    {
        [SerializeField] private GameObject poisonZonePrefab;
        private float _poisonDropInterval = 3f;
        private float _lastPoisonDrop;

        private void Update()
        {
            if (Time.time - _lastPoisonDrop >= _poisonDropInterval)
            {
                _lastPoisonDrop = Time.time;
                DropPoisonZone();
            }
        }

        private void DropPoisonZone()
        {
            if (poisonZonePrefab != null)
            {
                var zone = Instantiate(poisonZonePrefab, transform.position, Quaternion.identity);
                Destroy(zone, 3f);
            }
        }
    }
}
