using UnityEngine;

namespace SeagullStorm.Gameplay.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        public string weaponId;
        public int baseDamage;
        public float cooldown;
        public int level = 1;

        protected float _lastFireTime;

        protected virtual void Update()
        {
            if (Time.time - _lastFireTime >= cooldown)
            {
                _lastFireTime = Time.time;
                Fire();
            }
        }

        protected abstract void Fire();

        public virtual void UpgradeLevel()
        {
            level++;
        }

        protected int GetDamage()
        {
            float multiplier = Managers.GameManager.Instance.GetUpgradeMultiplier("damage");
            return Mathf.RoundToInt(baseDamage * multiplier * (1f + (level - 1) * 0.15f));
        }
    }
}
