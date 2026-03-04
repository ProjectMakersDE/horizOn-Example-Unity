using UnityEngine;
using SeagullStorm.Gameplay.Enemies;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Weapons
{
    public class SeagullScreech : WeaponBase
    {
        private void Awake()
        {
            weaponId = "screech";
        }

        protected override void Fire()
        {
            float radius = GameManager.Instance.Config.weaponScreechRadius * (1f + (level - 1) * 0.1f);
            int dmg = GetDamage();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeHit(dmg);
                }
            }

            AudioManager.Instance?.PlayScreech();
        }
    }
}
