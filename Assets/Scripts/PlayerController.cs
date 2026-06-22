using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Top-down WASD/arrow movement. Uses the new Input System's direct device
    /// polling (no .inputactions asset needed for this MVP).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2 dir = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1;

            transform.position += (Vector3)(dir.normalized * (moveSpeed * Time.deltaTime));
        }
    }
}
