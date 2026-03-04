using UnityEngine;
using UnityEngine.Pool;

namespace SeagullStorm
{
    /// <summary>
    /// Object pool for projectiles.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [SerializeField] private Projectile projectilePrefab;

        private ObjectPool<Projectile> _pool;

        private void Awake()
        {
            Instance = this;
            _pool = new ObjectPool<Projectile>(
                createFunc: () =>
                {
                    if (projectilePrefab == null) return null;
                    var p = Instantiate(projectilePrefab, transform);
                    p.gameObject.SetActive(false);
                    return p;
                },
                actionOnGet: p => { },
                actionOnRelease: p => { if (p != null) p.gameObject.SetActive(false); },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                defaultCapacity: 30,
                maxSize: 60
            );
        }

        public Projectile GetProjectile(Vector3 position, Vector2 direction, float speed, int damage)
        {
            var p = _pool?.Get();
            if (p != null)
            {
                p.transform.position = position;
                p.Initialize(direction, speed, damage);
            }
            return p;
        }

        public void ReturnProjectile(Projectile p)
        {
            _pool?.Release(p);
        }

        public void ReturnAll()
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }
    }
}
