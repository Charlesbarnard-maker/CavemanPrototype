using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Smoothly keeps the camera centred on a target (the player), preserving the
    /// camera's Z so the orthographic view stays correct.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 6f;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, goal, t);
        }
    }
}
