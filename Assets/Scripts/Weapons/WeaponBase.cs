using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Base weapon with auto-fire cooldown.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        public string weaponId;
        public int baseDamage = 20;
        public float cooldown = 1f;
        public int level = 1;

        protected float _timer;

        protected virtual void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Run) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = cooldown;
                Fire();
            }
        }

        protected abstract void Fire();

        public int GetDamage()
        {
            float mult = GameManager.Instance?.GetDamageMultiplier() ?? 1f;
            float levelMult = 1f + (level - 1) * 0.15f;
            return Mathf.RoundToInt(baseDamage * mult * levelMult);
        }

        public void UpgradeLevel()
        {
            level++;
        }
    }
}
