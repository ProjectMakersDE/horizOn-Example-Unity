using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Exposes player stats derived from upgrades and config for easy access.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public float Speed => 200f * (GameManager.Instance?.GetSpeedMultiplier() ?? 1f);
        public float DamageMultiplier => GameManager.Instance?.GetDamageMultiplier() ?? 1f;
        public float MaxHP => GameManager.Instance?.GetPlayerMaxHP() ?? 100f;
        public float PickupRadius => GameManager.Instance?.GetPickupRadius() ?? 50f;
    }
}
