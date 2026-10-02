using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Base enemy with health, movement toward player, contact damage.
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        public float moveSpeed = 40f;
        public int maxHP = 30;
        public int damage = 10;
        public int xpReward = 10;

        protected int _currentHP;
        protected Transform _target;
        protected SpriteRenderer _sprite;
        protected Rigidbody2D _rb;

        protected float _damageCooldown;
        private const float DamageInterval = 0.5f;

        public bool IsAlive => _currentHP > 0;

        protected virtual void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }
        }

        public virtual void Initialize(float speed, int hp, int dmg, int xp, Transform target)
        {
            moveSpeed = speed;
            maxHP = hp;
            _currentHP = hp;
            damage = dmg;
            xpReward = xp;
            _target = target;
            _damageCooldown = 0f;
            if (_sprite != null) _sprite.color = Color.white;
            gameObject.SetActive(true);
        }

        protected virtual void Update()
        {
            if (!IsAlive || _target == null || GameManager.Instance?.CurrentState != GameState.Run) return;

            _damageCooldown -= Time.deltaTime;

            Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

            if (_sprite != null)
                _sprite.flipX = dir.x < 0;
        }

        public virtual void TakeHit(int dmg)
        {
            if (!IsAlive) return;
            _currentHP -= dmg;
            CombatFeedback.Hit(transform.position, dmg);
            if (_sprite != null) _sprite.color = GameColors.Orange;
            CancelInvoke(nameof(ClearHitColor));
            Invoke(nameof(ClearHitColor), 0.1f);
            AudioManager.Instance?.PlayEnemyHit();

            if (_currentHP <= 0)
            {
                Die();
            }
        }

        private void ClearHitColor() { if (_sprite != null) _sprite.color = Color.white; }

        protected virtual void Die()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RunState.kills++;

            // Spawn XP pickup
            PickupPool.Instance?.SpawnXP(transform.position, xpReward);

            EnemyPool.Instance?.ReturnEnemy(this);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsAlive || _damageCooldown > 0f) return;

            if (other.CompareTag("Player"))
            {
                _damageCooldown = DamageInterval;
                PlayerController.Instance?.TakeDamage(damage);
                AudioManager.Instance?.PlayEnemyAttack();
            }
        }
    }
}
