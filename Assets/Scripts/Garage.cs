using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>The player's MOUNT GARAGE. A placed building whose only job is to be selectable so its
    /// panel (in <see cref="InventoryHud"/>) lets you BUY age-gated mounts and PARK / switch which one you
    /// ride. The mount state itself (ownership, the active ride, the hybrid speed) lives statically in
    /// <see cref="PlayerController"/>; this component is just the in-world hook + a tiny "limited slots"
    /// owner. Cosmetic, like <see cref="Battery"/> — no ports, no power.</summary>
    public class Garage : MonoBehaviour
    {
        public BuildingDefinition def;

        /// <summary>How many mounts THIS garage can hold parked (the "limited" in a limited garage). The
        /// game-wide cap is the max over all built garages — see <see cref="PlayerController.GarageSlots"/>.</summary>
        public int slots = 2;

        public static int BuiltCount;   // how many garages exist (0 = can't buy mounts yet)
        public static readonly List<Garage> All = new();

        void OnEnable()  { if (!All.Contains(this)) All.Add(this); BuiltCount++; PlayerController.RecomputeGarageSlots(); }
        void OnDisable() { All.Remove(this); BuiltCount = Mathf.Max(0, BuiltCount - 1); PlayerController.RecomputeGarageSlots(); }

        public static Garage Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var g = go.AddComponent<Garage>();
            g.def = def;
            g.slots = Mathf.Max(1, def.capacity > 0 ? def.capacity : 2); // reuse `capacity` as parking slots
            return g;
        }
    }
}
