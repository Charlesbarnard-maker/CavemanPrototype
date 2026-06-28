using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Core logistics infrastructure: a plank tile placed ON a water cell that makes it
    /// passable for the player AND for belts (not for normal buildings). Bridges are how you
    /// cross rivers/lakes — placement is strategic (chokepoints) and shapes your whole supply
    /// routing. Placed instantly (builders can't walk out onto water to construct one).
    /// </summary>
    public class Bridge : MonoBehaviour
    {
        private Vector2Int _cell;

        public static Bridge Spawn(BuildingDefinition def, Vector3 pos)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            var go = new GameObject("Bridge");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def != null ? def.color : new Color(0.62f, 0.47f, 0.28f);
            sr.sortingOrder = -80; // above terrain (-90), below belts (1) and buildings (5)

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var b = go.AddComponent<Bridge>();
            b._cell = cell;
            TerrainGrid.RegisterBridge(cell);
            return b;
        }

        void OnDestroy() => TerrainGrid.RemoveBridge(_cell);
    }
}
