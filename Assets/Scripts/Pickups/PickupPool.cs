using UnityEngine;
using UnityEngine.Pool;

namespace SeagullStorm
{
    /// <summary>
    /// Object pool for XP pickups.
    /// </summary>
    public class PickupPool : MonoBehaviour
    {
        public static PickupPool Instance { get; private set; }

        [SerializeField] private XPPickup xpPrefab;

        private ObjectPool<XPPickup> _pool;

        private void Awake()
        {
            Instance = this;
            _pool = new ObjectPool<XPPickup>(
                createFunc: () =>
                {
                    if (xpPrefab == null) return null;
                    var p = Instantiate(xpPrefab, transform);
                    p.gameObject.SetActive(false);
                    return p;
                },
                actionOnGet: p => { },
                actionOnRelease: p => { if (p != null) p.gameObject.SetActive(false); },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                defaultCapacity: 50,
                maxSize: 100
            );
        }

        public void SpawnXP(Vector3 position, int value)
        {
            var pickup = _pool?.Get();
            if (pickup != null)
            {
                pickup.transform.position = position;
                pickup.Initialize(value);
            }
        }

        public void ReturnPickup(XPPickup pickup)
        {
            _pool?.Release(pickup);
        }

        public void ReturnAll()
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }
    }
}
