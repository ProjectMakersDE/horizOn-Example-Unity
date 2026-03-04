using UnityEngine;

namespace SeagullStorm
{
    public class SeagullScreech : WeaponBase
    {
        private float _radius = 80f;

        private void Start()
        {
            weaponId = "screech";
            var config = GameManager.Instance?.Config;
            if (config != null)
            {
                baseDamage = config.WeaponScreechDamage;
                cooldown = config.WeaponScreechCooldown;
                _radius = config.WeaponScreechRadius;
            }
        }

        protected override void Fire()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            AudioManager.Instance?.PlayScreech();

            float scaledRadius = _radius + (level - 1) * 10f;
            var hits = Physics2D.OverlapCircleAll(player.transform.position, scaledRadius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeHit(GetDamage());
                }
            }
        }
    }
}
