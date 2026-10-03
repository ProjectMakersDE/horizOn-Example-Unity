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
        private float _baseSpeed = 145f;
        private float _invulnerableUntil;
        private void OnEnable() { _invulnerableUntil = 0; _moveInput = Vector2.zero; if (_sprite != null) _sprite.color = Color.white; }

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

            float speedMult = GameManager.Instance.GetSpeedMultiplier() * GameManager.Instance.RunState.moveSpeedMultiplier;
            Vector2 velocity = _moveInput.normalized * _baseSpeed * speedMult * Time.fixedDeltaTime;
            Vector2 next = _rb.position + velocity;
            next.x = Mathf.Clamp(next.x, -480f, 480f);
            next.y = Mathf.Clamp(next.y, -350f, 350f);
            _rb.MovePosition(next);
            GameManager.Instance.RunState.playerPosition = _rb.position;
        }

        public void TakeDamage(int amount)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Run || Time.time < _invulnerableUntil) return;
            _invulnerableUntil = Time.time + 0.65f;
            var run = GameManager.Instance.RunState;
            run.playerHP -= amount;
            GetComponent<SeagullCutoutRig>()?.PlayHurt();
            CombatFeedback.Hit(transform.position, amount, true);
            StartCoroutine(HitFlash());
            AudioManager.Instance?.PlayPlayerHit();

            HorizonManager.Instance?.SetCrashCustomKey("hp", run.playerHP.ToString());

            if (run.playerHP <= 0)
            {
                run.playerHP = 0;
                GameManager.Instance.EndRun();

            }
        }

        private System.Collections.IEnumerator HitFlash()
        {
            for (int i = 0; i < 5; i++)
            {
                _sprite.color = i % 2 == 0 ? GameColors.Orange : Color.white;
                yield return new WaitForSeconds(0.1f);
            }
            _sprite.color = Color.white;
        }

        public void Heal(float amount)
        {
            var run = GameManager.Instance.RunState;
            run.playerHP = Mathf.Min(run.playerHP + amount, run.playerMaxHP);
        }
    }
}
