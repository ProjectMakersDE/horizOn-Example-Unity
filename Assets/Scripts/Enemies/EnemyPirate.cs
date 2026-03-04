using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Pirate seagull enemy: fast and evasive, strafes perpendicular to player (available from wave 5).
    /// </summary>
    public class EnemyPirate : EnemyBase
    {
        private float _strafeTimer;
        private float _strafeDir = 1f;
        private const float StrafeInterval = 1.5f;

        protected override void Update()
        {
            if (!IsAlive || _target == null) return;

            _damageCooldown -= Time.deltaTime;
            _strafeTimer -= Time.deltaTime;
            if (_strafeTimer <= 0f)
            {
                _strafeTimer = StrafeInterval;
                _strafeDir *= -1f;
            }

            Vector2 toPlayer = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x) * _strafeDir;
            Vector2 moveDir = (toPlayer * 0.7f + perpendicular * 0.3f).normalized;

            transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);

            if (_sprite != null)
                _sprite.flipX = moveDir.x < 0;
        }

        public override void Initialize(float speed, int hp, int dmg, int xp, Transform target)
        {
            base.Initialize(speed, hp, dmg, xp, target);
            _strafeTimer = StrafeInterval;
            _strafeDir = Random.value > 0.5f ? 1f : -1f;
        }
    }
}
