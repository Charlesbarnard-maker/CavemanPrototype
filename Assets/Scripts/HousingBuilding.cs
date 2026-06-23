using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A structure that raises the population cap (Town Hall, House). The Colony
    /// sums every HousingBuilding's capacity to get the total population cap.
    /// </summary>
    public class HousingBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public int houseCapacity = 3;
        [Tooltip("The HQ (Town Hall) — builders are managed from here.")]
        public bool isHQ;

        public static readonly List<HousingBuilding> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static HousingBuilding Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 1.1f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 4;

            go.AddComponent<BoxCollider2D>(); // clickable for demolish

            var h = go.AddComponent<HousingBuilding>();
            h.def = def;
            h.houseCapacity = Mathf.Max(0, def.houseCapacity);
            return h;
        }
    }
}
