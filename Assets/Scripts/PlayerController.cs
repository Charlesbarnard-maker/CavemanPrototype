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

        /// <summary>Bought from a Harbour — lets the player sail over WATER (to reach islands). Static so the
        /// avatar + movement can read it; reset on a fresh game in GameBootstrap.</summary>
        public static bool HasBoat;

        // Travel tier per age (index = Colony.Age). On Foot is always free; the rest are MOUNTS you BUY
        // and park at a Garage. HYBRID model: reaching an age auto-grants a small BASELINE speed bump
        // (so travel always improves), but the full tier speed + the mount VISUAL need the bought mount.
        public static readonly (string name, float speed)[] Mounts =
        {
            ("On Foot",   6f),
            ("Horseback", 8.5f),
            ("Ox Cart",   10.5f),
            ("Wagon",     13f),
            ("Motorbike", 16f),
        };
        private int _lastTier = -1;

        // ---- Mount ownership / selection (the "buyable mount + limited garage") ----
        /// <summary>Which mount tiers the player owns (parked). Index 0 = On Foot, always owned.</summary>
        public static readonly bool[] OwnedMount = { true, false, false, false, false };
        /// <summary>The tier currently being ridden (0 = on foot). Drives speed + the avatar visual.</summary>
        public static int ActiveMount;
        /// <summary>Per-tier purchase cost, populated by GameBootstrap (it owns the ItemDefinitions).</summary>
        public static System.Collections.Generic.List<ItemAmount>[] MountCost = new System.Collections.Generic.List<ItemAmount>[5];
        /// <summary>Total parking slots across all built Garages (0 = no garage yet → can't buy mounts).</summary>
        public static int GarageSlots;

        public static int MountTierMax => Mounts.Length - 1;
        /// <summary>Highest mount tier buyable at the given age (you can't buy above your age).</summary>
        public static int MaxTierForAge(int age) => Mathf.Clamp(age, 0, Mounts.Length - 1);
        /// <summary>How many mounts (tiers 1+) are currently owned/parked.</summary>
        public static int OwnedCount()
        {
            int n = 0; for (int i = 1; i < OwnedMount.Length; i++) if (OwnedMount[i]) n++; return n;
        }
        /// <summary>The tier actually being ridden right now: the active one if owned and within your age, else on foot.</summary>
        public static int RidingTier(int age)
            => (ActiveMount >= 1 && ActiveMount < OwnedMount.Length && OwnedMount[ActiveMount] && ActiveMount <= MaxTierForAge(age)) ? ActiveMount : 0;

        public static bool HasGarage => GarageSlots > 0;
        public static bool CanBuy(int tier, int age)
            => tier >= 1 && tier <= MaxTierForAge(age) && !OwnedMount[tier] && HasGarage && OwnedCount() < GarageSlots;

        public static void Buy(int tier)
        {
            if (tier < 1 || tier >= OwnedMount.Length) return;
            OwnedMount[tier] = true; ActiveMount = tier; // hop straight on the new mount
        }
        public static void SetActive(int tier)
        {
            if (tier == 0 || (tier >= 1 && tier < OwnedMount.Length && OwnedMount[tier])) ActiveMount = tier;
        }
        /// <summary>Free a parking slot — release a mount you own (no refund). Drops to on foot if it was active.</summary>
        public static void Release(int tier)
        {
            if (tier < 1 || tier >= OwnedMount.Length) return;
            OwnedMount[tier] = false; if (ActiveMount == tier) ActiveMount = 0;
        }
        /// <summary>Reset on a fresh game (statics persist with domain-reload-off).</summary>
        public static void ResetMounts()
        {
            for (int i = 0; i < OwnedMount.Length; i++) OwnedMount[i] = (i == 0);
            ActiveMount = 0;
        }
        /// <summary>Recompute total parking slots from the live garages (each Garage adds its slots, capped at the mount count).</summary>
        public static void RecomputeGarageSlots()
            => GarageSlots = Garage.BuiltCount > 0 ? Mathf.Min(Mounts.Length - 1, Garage.BuiltCount * 2) : 0;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            int tier = Mathf.Clamp(age, 0, Mounts.Length - 1);
            if (tier != _lastTier)
            {
                if (_lastTier >= 0)
                    Toast.Show($"<color=#ffd24d>🐎 Age up — travel's a little quicker, and the {Mounts[tier].name} is now buyable at a Garage (full speed + the look).</color>");
                _lastTier = tier;
            }

            // HYBRID speed: an age-scaled BASELINE you always get on foot, or the FULL tier speed when
            // riding a bought mount.
            float baseline = Mathf.Lerp(Mounts[0].speed, Mounts[tier].speed, 0.35f);
            int ride = RidingTier(age);
            float speed = ride >= 1 ? Mounts[ride].speed : baseline;
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
            bool boat = HasBoat; // a boat lets you cross water (land stays walkable too)
            Vector3 tryX = new Vector3(p.x + step.x, p.y, 0f);
            if ((boat || TerrainGrid.Walkable(tryX)) && (insideBuilding || !BuildController.SolidBuildingAt(tryX))) p = tryX;
            Vector3 tryY = new Vector3(p.x, p.y + step.y, 0f);
            if ((boat || TerrainGrid.Walkable(tryY)) && (insideBuilding || !BuildController.SolidBuildingAt(tryY))) p = tryY;
            transform.position = p;
        }
    }
}
