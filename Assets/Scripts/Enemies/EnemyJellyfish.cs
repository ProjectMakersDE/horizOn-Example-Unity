using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Jellyfish enemy: drops poison zones periodically (available from wave 3).
    /// </summary>
    public class EnemyJellyfish : EnemyBase
    {
        [SerializeField] private GameObject poisonZonePrefab;
        private float _poisonTimer;
        private const float PoisonInterval = 4f;

        protected override void Update()
        {
            base.Update();
            if (!IsAlive) return;

            _poisonTimer -= Time.deltaTime;
            if (_poisonTimer <= 0f)
            {
                _poisonTimer = PoisonInterval;
                if (poisonZonePrefab != null)
                {
                    var zone = Instantiate(poisonZonePrefab, transform.position, Quaternion.identity);
                    Destroy(zone, 5f);
                }
            }
        }

        public override void Initialize(float speed, int hp, int dmg, int xp, Transform target)
        {
            base.Initialize(speed, hp, dmg, xp, target);
            _poisonTimer = PoisonInterval;
        }
    }
}
