using UnityEngine;

namespace SeagullStorm
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        private Rigidbody2D _rb;
        private SpriteRenderer _sprite;
        private Vector2 _moveInput;
        private float _baseSpeed = 200f;

        public Vector2 MoveDirection => _moveInput.normalized;
        public Vector2 LastNonZeroDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _sprite = GetComponent<SpriteRenderer>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Run) return;

            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            if (_moveInput.sqrMagnitude > 0.01f)
                LastNonZeroDirection = _moveInput.normalized;

            if (_moveInput.x < 0) _sprite.flipX = true;
            else if (_moveInput.x > 0) _sprite.flipX = false;
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Run) return;

            float speedMult = GameManager.Instance.GetSpeedMultiplier();
            Vector2 velocity = _moveInput.normalized * _baseSpeed * speedMult * Time.fixedDeltaTime;
            _rb.MovePosition(_rb.position + velocity);
            GameManager.Instance.RunState.playerPosition = _rb.position;
        }

        public void TakeDamage(int amount)
        {
            var run = GameManager.Instance.RunState;
            run.playerHP -= amount;
            AudioManager.Instance?.PlayPlayerHit();

            HorizonManager.Instance?.SetCrashCustomKey("hp", run.playerHP.ToString());

            if (run.playerHP <= 0)
            {
                run.playerHP = 0;
                GameManager.Instance.EndRun();
                AudioManager.Instance?.PlayGameOver();
                AudioManager.Instance?.PlayMenuMusic();
                GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }

        public void Heal(float amount)
        {
            var run = GameManager.Instance.RunState;
            run.playerHP = Mathf.Min(run.playerHP + amount, run.playerMaxHP);
        }
    }
}
