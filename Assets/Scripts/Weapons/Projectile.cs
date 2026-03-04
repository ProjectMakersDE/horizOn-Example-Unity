using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Generic projectile: moves in a direction, deals damage on collision, returns to pool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour
    {
        private Vector2 _direction;
        private float _speed = 400f;
        private int _damage;
        private float _lifetime;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        public void Initialize(Vector2 direction, float speed, int damage, float lifetime = 2f)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _lifetime = lifetime;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                ProjectilePool.Instance?.ReturnProjectile(this);
                return;
            }

            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeHit(_damage);
                ProjectilePool.Instance?.ReturnProjectile(this);
            }
        }
    }
}
