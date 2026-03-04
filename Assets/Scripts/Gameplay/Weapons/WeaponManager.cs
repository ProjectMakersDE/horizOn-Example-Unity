using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm.Gameplay.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        [SerializeField] private GameObject featherThrowPrefab;
        [SerializeField] private GameObject screechPrefab;
        [SerializeField] private GameObject diveBombPrefab;
        [SerializeField] private GameObject windGustPrefab;

        private readonly Dictionary<string, WeaponBase> _activeWeapons = new Dictionary<string, WeaponBase>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            AddWeapon("feather");
        }

        public void AddWeapon(string weaponId)
        {
            if (_activeWeapons.ContainsKey(weaponId))
            {
                _activeWeapons[weaponId].UpgradeLevel();
                return;
            }

            GameObject prefab = GetPrefab(weaponId);
            if (prefab == null) return;

            var weaponObj = Instantiate(prefab, transform);
            var weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                ApplyConfig(weapon);
                _activeWeapons[weaponId] = weapon;
                Managers.GameManager.Instance.CurrentRun.activeWeapons.Add(weaponId);
            }
        }

        public void UpgradeWeapon(string weaponId)
        {
            if (_activeWeapons.TryGetValue(weaponId, out var weapon))
            {
                weapon.UpgradeLevel();
            }
        }

        public bool HasWeapon(string weaponId)
        {
            return _activeWeapons.ContainsKey(weaponId);
        }

        private GameObject GetPrefab(string weaponId)
        {
            switch (weaponId)
            {
                case "feather": return featherThrowPrefab;
                case "screech": return screechPrefab;
                case "dive": return diveBombPrefab;
                case "gust": return windGustPrefab;
                default: return null;
            }
        }

        private void ApplyConfig(WeaponBase weapon)
        {
            var config = Managers.GameManager.Instance.Config;
            switch (weapon.weaponId)
            {
                case "feather":
                    weapon.baseDamage = config.weaponFeatherDamage;
                    weapon.cooldown = config.weaponFeatherCooldown;
                    break;
                case "screech":
                    weapon.baseDamage = config.weaponScreechDamage;
                    weapon.cooldown = config.weaponScreechCooldown;
                    break;
                case "dive":
                    weapon.baseDamage = config.weaponDiveDamage;
                    weapon.cooldown = config.weaponDiveCooldown;
                    break;
                case "gust":
                    weapon.baseDamage = config.weaponGustDamage;
                    weapon.cooldown = config.weaponGustCooldown;
                    break;
            }
        }
    }
}
