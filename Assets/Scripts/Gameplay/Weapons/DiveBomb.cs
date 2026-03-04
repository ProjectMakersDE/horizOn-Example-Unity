using System.Collections;
using UnityEngine;
using SeagullStorm.Gameplay.Player;
using SeagullStorm.Gameplay.Enemies;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Weapons
{
    public class DiveBomb : WeaponBase
    {
        private bool _isDiving;

        private void Awake()
        {
            weaponId = "dive";
        }

        protected override void Update()
        {
            if (_isDiving) return;
            base.Update();
        }

        protected override void Fire()
        {
            if (PlayerController.Instance == null || _isDiving) return;
            StartCoroutine(DiveCoroutine());
        }

        private IEnumerator DiveCoroutine()
        {
            _isDiving = true;
            Vector2 direction = PlayerController.Instance.LastNonZeroDirection;
            float range = GameManager.Instance.Config.weaponDiveRange * (1f + (level - 1) * 0.1f);
            int dmg = GetDamage();

            Vector2 startPos = transform.position;
            Vector2 endPos = startPos + direction * range;
            float duration = 0.2f;
            float t = 0f;

            AudioManager.Instance?.PlayDive();

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = t / duration;
                Vector2 pos = Vector2.Lerp(startPos, endPos, progress);

                Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 16f);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeHit(dmg);
                    }
                }

                yield return null;
            }

            _isDiving = false;
        }
    }
}
