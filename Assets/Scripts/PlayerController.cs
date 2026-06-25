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

            // Water is a hard barrier (unless bridged) and SOLID BUILDINGS block you too, so layout
            // matters — you route around your factory. Resolve each axis separately so you slide
            // along an obstacle instead of sticking. Escape valve: if you're already standing inside
            // a building (e.g. one was just built on you), don't block movement — let yourself out.
            Vector3 step = (Vector3)(dir.normalized * (speed * Time.deltaTime));
            Vector3 p = transform.position;
            bool insideBuilding = BuildController.SolidBuildingAt(p);
            Vector3 tryX = new Vector3(p.x + step.x, p.y, 0f);
            if (TerrainGrid.Walkable(tryX) && (insideBuilding || !BuildController.SolidBuildingAt(tryX))) p = tryX;
            Vector3 tryY = new Vector3(p.x, p.y + step.y, 0f);
            if (TerrainGrid.Walkable(tryY) && (insideBuilding || !BuildController.SolidBuildingAt(tryY))) p = tryY;
            transform.position = p;
        }
    }
}
