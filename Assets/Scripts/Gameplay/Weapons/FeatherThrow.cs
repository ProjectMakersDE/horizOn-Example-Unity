using UnityEngine;
using SeagullStorm.Gameplay.Player;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Weapons
{
    public class FeatherThrow : WeaponBase
    {
        [SerializeField] private GameObject projectilePrefab;

        private void Awake()
        {
            weaponId = "feather";
        }

        protected override void Fire()
        {
            if (PlayerController.Instance == null) return;

            Vector2 direction = PlayerController.Instance.LastNonZeroDirection;
            int projectileCount = Mathf.Max(1, GameManager.Instance.Config.weaponFeatherProjectiles + (level - 1));

            for (int i = 0; i < projectileCount; i++)
            {
                float angleOffset = 0f;
                if (projectileCount > 1)
                {
                    angleOffset = (i - (projectileCount - 1) / 2f) * 15f;
                }

                Vector2 dir = Quaternion.Euler(0, 0, angleOffset) * direction;
                SpawnProjectile(dir);
            }

            AudioManager.Instance?.PlayFeather();
        }

        private void SpawnProjectile(Vector2 direction)
        {
            if (projectilePrefab == null) return;

            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var featherProj = proj.GetComponent<FeatherProjectile>();
            if (featherProj != null)
            {
                featherProj.Initialize(direction, GetDamage());
            }
        }
    }
}
