using UnityEngine;
using SeagullStorm.Gameplay.Enemies;

namespace SeagullStorm.Gameplay.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FeatherProjectile : MonoBehaviour
    {
        private Vector2 _direction;
        private int _damage;
        private float _speed = 300f;
        private float _lifetime = 2f;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        public void Initialize(Vector2 direction, int damage)
        {
            _direction = direction.normalized;
            _damage = damage;
            Destroy(gameObject, _lifetime);

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void FixedUpdate()
        {
            _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeHit(_damage);
                Destroy(gameObject);
            }
        }
    }
}
