using UnityEngine;
using SeagullStorm.Gameplay.Player;
using SeagullStorm.Gameplay.Pickups;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyBase : MonoBehaviour
    {
        public float moveSpeed = 40f;
        public int maxHP = 30;
        public int damage = 10;
        public int xpReward = 10;
        public string enemyType = "crab";

        private int _currentHP;
        private Rigidbody2D _rb;
        private float _attackCooldown = 1f;
        private float _lastAttackTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            _currentHP = maxHP;
        }

        private void FixedUpdate()
        {
            if (PlayerController.Instance == null) return;

            Vector2 direction = ((Vector2)PlayerController.Instance.transform.position - _rb.position).normalized;
            _rb.MovePosition(_rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && Time.time - _lastAttackTime >= _attackCooldown)
            {
                _lastAttackTime = Time.time;
                PlayerController.Instance.TakeDamage(damage);
                AudioManager.Instance?.PlayEnemyAttack();
            }
        }

        public void TakeHit(int damageAmount)
        {
            _currentHP -= damageAmount;
            AudioManager.Instance?.PlayEnemyHit();

            if (_currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            var run = GameManager.Instance.CurrentRun;
            run.kills++;
            run.currentScore += xpReward;

            // Spawn XP pickup
            XPPickup.Spawn(transform.position, xpReward);

            gameObject.SetActive(false);
        }
    }
}
