using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Top-down WASD/arrow movement. Travel speed scales with the age you've reached
    /// (On Foot → Horseback → … → Motorbike) so getting around stops being a slog as
    /// you progress; hold Shift to sprint. Uses direct device polling (no asset).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 6f; // Stone-age base (also the fallback)

        // Travel tier per age (index = Colony.Age). Faster travel is itself a
        // progression reward — later ages add real mounts/vehicles.
        private static readonly (string name, float speed)[] Mounts =
        {
            ("On Foot",   6f),
            ("Horseback", 8.5f),
            ("Ox Cart",   10.5f),
            ("Wagon",     13f),
            ("Motorbike", 16f),
        };
        private int _lastTier = -1;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            int tier = Mathf.Clamp(age, 0, Mounts.Length - 1);
            if (tier != _lastTier)
            {
                if (_lastTier >= 0)
                    Toast.Show($"<color=#ffd24d>🐎 Faster travel unlocked: {Mounts[tier].name}!</color>");
                _lastTier = tier;
            }

            float speed = Mounts[tier].speed;
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) speed *= 1.5f; // sprint

            Vector2 dir = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1;

            transform.position += (Vector3)(dir.normalized * (speed * Time.deltaTime));
        }
    }
}
