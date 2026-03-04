using System.Collections;
using UnityEngine;

namespace SeagullStorm
{
    public class DiveBomb : WeaponBase
    {
        private float _range = 120f;
        private bool _isDiving;

        private void Start()
        {
            weaponId = "dive";
            var config = GameManager.Instance?.Config;
            if (config != null)
            {
                baseDamage = config.WeaponDiveDamage;
                cooldown = config.WeaponDiveCooldown;
                _range = config.WeaponDiveRange;
            }
        }

        protected override void Fire()
        {
            if (_isDiving) return;
            var player = PlayerController.Instance;
            if (player == null) return;

            AudioManager.Instance?.PlayDive();
            StartCoroutine(DiveCoroutine(player));
        }

        private IEnumerator DiveCoroutine(PlayerController player)
        {
            _isDiving = true;
            Vector2 dir = player.LastNonZeroDirection;
            Vector2 start = player.transform.position;
            Vector2 end = start + dir * (_range + (level - 1) * 20f);
            float t = 0f;
            float duration = 0.2f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                player.transform.position = Vector2.Lerp(start, end, p);

                var hits = Physics2D.OverlapCircleAll(player.transform.position, 20f);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null && enemy.IsAlive)
                        enemy.TakeHit(GetDamage());
                }

                yield return null;
            }

            _isDiving = false;
        }
    }
}
