using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Smooth camera follow during run, static during hub.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        private void LateUpdate()
        {
            if (target == null || GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Hub)
            {
                transform.position = new Vector3(0, 0, -10f);
                return;
            }

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform t)
        {
            target = t;
        }
    }
}
