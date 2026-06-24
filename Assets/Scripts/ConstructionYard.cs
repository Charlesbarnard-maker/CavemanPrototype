using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Construction infrastructure: each yard raises the colony's builder cap by
    /// <see cref="slots"/>. Scaling construction means BUILDING and placing yards
    /// (they cost resources + space) AND having spare population to fill the new
    /// builder jobs — not nudging a slider. This is how building becomes a system
    /// you invest in rather than a fixed cap.
    /// </summary>
    public class ConstructionYard : MonoBehaviour
    {
        public BuildingDefinition def;
        public int slots = 3;

        public static readonly List<ConstructionYard> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Total extra builder capacity from every yard in the colony.</summary>
        public static int TotalSlots
        {
            get { int n = 0; foreach (var y in All) if (y != null) n += y.slots; return n; }
        }

        public static ConstructionYard Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 4;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var y = go.AddComponent<ConstructionYard>();
            y.def = def;
            y.slots = Mathf.Max(1, def.builderSlots);
            return y;
        }
    }
}
