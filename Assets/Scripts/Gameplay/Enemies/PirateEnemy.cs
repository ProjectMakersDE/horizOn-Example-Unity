using UnityEngine;
using SeagullStorm.Gameplay.Player;

namespace SeagullStorm.Gameplay.Enemies
{
    public class PirateEnemy : EnemyBase
    {
        private float _evadeTimer;
        private float _evadeCooldown = 2f;
        private float _evadeDuration = 0.3f;
        private Vector2 _evadeDirection;
        private bool _isEvading;

        private void Update()
        {
            if (_isEvading) return;

            _evadeTimer += Time.deltaTime;
            if (_evadeTimer >= _evadeCooldown)
            {
                _evadeTimer = 0f;
                StartEvade();
            }
        }

        private void StartEvade()
        {
            if (PlayerController.Instance == null) return;

            Vector2 toPlayer = ((Vector2)PlayerController.Instance.transform.position - (Vector2)transform.position).normalized;
            _evadeDirection = Vector2.Perpendicular(toPlayer) * (Random.value > 0.5f ? 1f : -1f);
            _isEvading = true;
            Invoke(nameof(StopEvade), _evadeDuration);
        }

        private void StopEvade()
        {
            _isEvading = false;
        }

        private void FixedUpdate()
        {
            if (_isEvading)
            {
                var rb = GetComponent<Rigidbody2D>();
                rb.MovePosition(rb.position + _evadeDirection * moveSpeed * 2f * Time.fixedDeltaTime);
            }
        }
    }
}
