using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Controls the player's Animator based on movement and state.
    /// Requires an Animator with bool "IsWalking" and trigger "Hurt"/"Death".
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerController _controller;

        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int HurtTrigger = Animator.StringToHash("Hurt");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_controller == null || _animator == null) return;
            _animator.SetBool(IsWalking, _controller.MoveDirection.sqrMagnitude > 0.01f);
        }

        public void PlayHurt()
        {
            _animator?.SetTrigger(HurtTrigger);
        }

        public void PlayDeath()
        {
            _animator?.SetTrigger(DeathTrigger);
        }
    }
}
