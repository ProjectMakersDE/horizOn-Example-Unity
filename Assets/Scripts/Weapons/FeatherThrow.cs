using UnityEngine;

namespace SeagullStorm
{
    public class FeatherThrow : WeaponBase
    {
        private int _projectileCount = 1;
        private float _projectileSpeed = 400f;

        private void Start()
        {
            weaponId = "feather";
            var config = GameManager.Instance?.Config;
            if (config != null)
            {
                baseDamage = config.WeaponFeatherDamage;
                cooldown = config.WeaponFeatherCooldown;
                _projectileCount = config.WeaponFeatherProjectiles;
            }
        }

        protected override void Fire()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 dir = player.LastNonZeroDirection;
            int count = _projectileCount + (level - 1);
            float spread = 15f;

            AudioManager.Instance?.PlayFeather();

            for (int i = 0; i < count; i++)
            {
                float angle = 0f;
                if (count > 1)
                    angle = spread * (i - (count - 1) / 2f);

                Vector2 shootDir = Quaternion.Euler(0, 0, angle) * dir;
                ProjectilePool.Instance?.GetProjectile(player.transform.position, shootDir, _projectileSpeed, GetDamage());
            }
        }
    }
}
