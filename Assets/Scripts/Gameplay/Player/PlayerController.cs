using UnityEngine;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private float _baseSpeed = 200f;
        private Vector2 _moveInput;

        public Vector2 MoveDirection => _moveInput.normalized;
        public Vector2 LastNonZeroDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                LastNonZeroDirection = _moveInput.normalized;
            }

            // Flip sprite based on direction
            if (_moveInput.x < 0)
                _spriteRenderer.flipX = true;
            else if (_moveInput.x > 0)
                _spriteRenderer.flipX = false;
        }

        private void FixedUpdate()
        {
            float speedMultiplier = GameManager.Instance.GetUpgradeMultiplier("speed");
            Vector2 velocity = _moveInput.normalized * _baseSpeed * speedMultiplier * Time.fixedDeltaTime;
            _rb.MovePosition(_rb.position + velocity);

            GameManager.Instance.CurrentRun.playerPosition = _rb.position;
        }

        public void TakeDamage(int amount)
        {
            var run = GameManager.Instance.CurrentRun;
            run.playerHP -= amount;

            AudioManager.Instance?.PlayPlayerHit();

            if (run.playerHP <= 0)
            {
                run.playerHP = 0;
                RunManager.Instance?.OnPlayerDeath();
            }
        }

        public void Heal(int amount)
        {
            var run = GameManager.Instance.CurrentRun;
            run.playerHP = Mathf.Min(run.playerHP + amount, run.playerMaxHP);
        }
    }
}
