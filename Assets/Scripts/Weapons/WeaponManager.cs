using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Manages active weapons on the player. Weapons are added as components.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        private Dictionary<string, WeaponBase> _weapons = new Dictionary<string, WeaponBase>();

        private void Awake()
        {
            Instance = this;
        }

        public void InitializeDefaultWeapon(GameObject player)
        {
            if (!_weapons.ContainsKey("feather"))
            {
                AddWeaponToPlayer(player, "feather");
            }
        }

        public void AddWeapon(string weaponId)
        {
            var player = PlayerController.Instance?.gameObject;
            if (player == null) return;
            AddWeaponToPlayer(player, weaponId);
        }

        private void AddWeaponToPlayer(GameObject player, string weaponId)
        {
            if (_weapons.ContainsKey(weaponId)) return;

            WeaponBase weapon = null;
            switch (weaponId)
            {
                case "feather": weapon = player.AddComponent<FeatherThrow>(); break;
                case "screech": weapon = player.AddComponent<SeagullScreech>(); break;
                case "dive": weapon = player.AddComponent<DiveBomb>(); break;
                case "gust": weapon = player.AddComponent<WindGust>(); break;
            }

            if (weapon != null)
            {
                weapon.weaponId = weaponId;
                _weapons[weaponId] = weapon;
            }
        }

        public void UpgradeWeapon(string weaponId)
        {
            if (_weapons.TryGetValue(weaponId, out var weapon))
            {
                weapon.UpgradeLevel();
            }
        }

        public bool HasWeapon(string weaponId)
        {
            return _weapons.ContainsKey(weaponId);
        }

        public void ClearWeapons()
        {
            foreach (var kvp in _weapons)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _weapons.Clear();
        }
    }
}
