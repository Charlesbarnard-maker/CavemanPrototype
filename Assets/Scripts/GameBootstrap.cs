using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring:
    /// camera, player, a few rocks/trees, and the HUD. Placeholder art is just
    /// tinted squares. Add this component to one empty GameObject and press Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private static Sprite _square;

        void Start()
        {
            // --- Item definitions (runtime instances for now; become .asset files later) ---
            var stone = MakeItem("stone", "Stone", new Color(0.55f, 0.55f, 0.62f));
            var wood = MakeItem("wood", "Wood", new Color(0.52f, 0.34f, 0.16f));

            // --- Camera ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.16f);

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, sortingOrder: 10);
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;

            // --- Resource nodes ---
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-3f, 2f));
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-4f, -2f));
            SpawnNode("Tree", wood, new Color(0.45f, 0.30f, 0.15f), new Vector2(3f, 1.5f));
            SpawnNode("Tree", wood, new Color(0.45f, 0.30f, 0.15f), new Vector2(4f, -2f));
        }

        private static ItemDefinition MakeItem(string id, string name, Color color)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.color = color;
            return item;
        }

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos)
        {
            var go = MakeSprite(name, color, pos, 1f, sortingOrder: 0);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.yieldPerHit = 1;
            node.hitsRemaining = 5;
        }

        private static GameObject MakeSprite(string name, Color color, Vector2 pos, float size, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SquareSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        // A shared 1x1 white sprite (1 pixel-per-unit => 1 world unit). Tinted per-renderer.
        private static Sprite SquareSprite()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _square;
        }
    }
}
