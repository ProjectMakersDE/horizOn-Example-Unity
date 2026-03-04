using UnityEngine;

namespace SeagullStorm
{
    public class WindGust : WeaponBase
    {
        private float _knockback = 60f;

        private void Start()
        {
            weaponId = "gust";
            var config = GameManager.Instance?.Config;
            if (config != null)
            {
                baseDamage = config.WeaponGustDamage;
                cooldown = config.WeaponGustCooldown;
                _knockback = config.WeaponGustKnockback;
            }
        }

        protected override void Fire()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            AudioManager.Instance?.PlayGust();

            float radius = 100f + (level - 1) * 15f;
            var hits = Physics2D.OverlapCircleAll(player.transform.position, radius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeHit(GetDamage());

                    var rb = hit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 pushDir = ((Vector2)hit.transform.position - (Vector2)player.transform.position).normalized;
                        rb.AddForce(pushDir * _knockback, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}
