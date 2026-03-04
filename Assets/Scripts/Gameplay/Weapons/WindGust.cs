using UnityEngine;
using SeagullStorm.Gameplay.Enemies;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Weapons
{
    public class WindGust : WeaponBase
    {
        private void Awake()
        {
            weaponId = "gust";
        }

        protected override void Fire()
        {
            float knockback = GameManager.Instance.Config.weaponGustKnockback * (1f + (level - 1) * 0.1f);
            float radius = 80f * (1f + (level - 1) * 0.1f);
            int dmg = GetDamage();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeHit(dmg);

                    Vector2 pushDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    var rb = hit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.AddForce(pushDir * knockback, ForceMode2D.Impulse);
                    }
                }
            }

            AudioManager.Instance?.PlayGust();
        }
    }
}
