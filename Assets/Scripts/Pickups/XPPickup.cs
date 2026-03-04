using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// XP shell pickup with magnet attraction.
    /// </summary>
    public class XPPickup : MonoBehaviour
    {
        public int xpValue = 10;
        private Transform _target;
        private float _magnetSpeed = 300f;
        private bool _attracted;

        public void Initialize(int value)
        {
            xpValue = value;
            _attracted = false;
            _target = PlayerController.Instance?.transform;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_target == null) return;

            float pickupRadius = GameManager.Instance?.GetPickupRadius() ?? 50f;
            float dist = Vector2.Distance(transform.position, _target.position);

            if (dist < pickupRadius)
                _attracted = true;

            if (_attracted)
            {
                Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(dir * _magnetSpeed * Time.deltaTime);

                if (dist < 10f)
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            if (GameManager.Instance != null)
            {
                var run = GameManager.Instance.RunState;
                run.xpCollected += xpValue;
                run.xpCurrent += xpValue;

                // Check level up
                if (run.xpCurrent >= run.xpToNextLevel)
                {
                    run.xpCurrent -= run.xpToNextLevel;
                    run.level++;
                    var config = GameManager.Instance.Config;
                    run.xpToNextLevel = 20f * Mathf.Pow(config?.XpLevelCurve ?? 1.4f, run.level - 1);

                    LevelUpManager.Instance?.TriggerLevelUp();
                }
            }

            AudioManager.Instance?.PlayPickupXp();
            PickupPool.Instance?.ReturnPickup(this);
        }
    }
}
