using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// ScriptableObject defining weapon stats template.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "SeagullStorm/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponId;
        public int baseDamage = 20;
        public float cooldown = 1f;
        public Sprite icon;
        public string displayName;
        public string description;
    }
}
