using UnityEngine;

namespace SeagullStorm.Gameplay.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        private Transform _target;

        private void LateUpdate()
        {
            if (_target == null)
            {
                var player = PlayerController.Instance;
                if (player != null) _target = player.transform;
                else return;
            }

            Vector3 desired = _target.position + offset;
            Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.position = smoothed;
        }
    }
}
