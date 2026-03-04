using UnityEngine;
using SeagullStorm.Gameplay.Player;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Pickups
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class XPPickup : MonoBehaviour
    {
        private int _xpValue;
        private static GameObject _prefab;
        private bool _attracted;
        private float _attractSpeed = 400f;

        public static void SetPrefab(GameObject prefab)
        {
            _prefab = prefab;
        }

        public static void Spawn(Vector2 position, int xpValue)
        {
            if (_prefab == null) return;

            var obj = Instantiate(_prefab, position, Quaternion.identity);
            var pickup = obj.GetComponent<XPPickup>();
            if (pickup != null)
            {
                pickup._xpValue = xpValue;
            }
        }

        private void Awake()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
        }

        private void Update()
        {
            if (PlayerController.Instance == null) return;

            float pickupRadius = GameManager.Instance.GetPickupRadius();
            float dist = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);

            if (dist <= pickupRadius)
            {
                _attracted = true;
            }

            if (_attracted)
            {
                Vector2 dir = ((Vector2)PlayerController.Instance.transform.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(dir * _attractSpeed * Time.deltaTime);

                if (dist < 8f)
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            var run = GameManager.Instance.CurrentRun;
            run.xpCollected += _xpValue;
            run.xpCurrent += _xpValue;

            AudioManager.Instance?.PlayPickupXp();

            // Check for level up
            if (run.xpCurrent >= run.xpToNextLevel)
            {
                run.xpCurrent -= run.xpToNextLevel;
                run.currentLevel++;
                run.xpToNextLevel = Mathf.RoundToInt(run.xpToNextLevel * GameManager.Instance.Config.xpLevelCurve);
                RunManager.Instance?.OnLevelUp();
            }

            Destroy(gameObject);
        }
    }
}
