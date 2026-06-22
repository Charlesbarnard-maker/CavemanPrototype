using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring:
    /// camera, player, rocks/trees, crafting, and the HUD. Placeholder art is
    /// tinted squares (rocks) and circles (trees/player). Add this component to
    /// one empty GameObject and press Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private static Sprite _square;
        private static Sprite _circle;

        void Start()
        {
            // --- Item definitions (runtime instances for now; become .asset files later) ---
            var stone = MakeItem("stone", "Stone", new Color(0.55f, 0.55f, 0.62f));
            var wood = MakeItem("wood", "Wood", new Color(0.52f, 0.34f, 0.16f));

            // --- A craftable tool: 3 Wood + 2 Stone -> Stone Axe (gather x2) ---
            var axe = ScriptableObject.CreateInstance<ToolDefinition>();
            axe.displayName = "Stone Axe";
            axe.gatherPower = 2;
            axe.cost = new List<ItemAmount>
            {
                new ItemAmount(wood, 3),
                new ItemAmount(stone, 2),
            };

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
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, circle: true);
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var crafter = player.AddComponent<PlayerCrafter>();
            crafter.recipe = axe;
            crafter.gatherer = gatherer;

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.crafter = crafter;

            // --- Resource nodes: rocks (grey squares), trees (green circles) ---
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-3f, 2f), circle: false);
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-4f, -2f), circle: false);
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(3f, 1.5f), circle: true);
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(4f, -2f), circle: true);
        }

        private static ItemDefinition MakeItem(string id, string name, Color color)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.color = color;
            return item;
        }

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos, bool circle)
        {
            var go = MakeSprite(name, color, pos, 1f, 0, circle);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.yieldPerHit = 1;
            node.maxHits = 5;
            node.respawnSeconds = 8f;
        }

        private static GameObject MakeSprite(string name, Color color, Vector2 pos, float size, int sortingOrder, bool circle)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle ? CircleSprite() : SquareSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        // 1x1 white sprite (1 px-per-unit => 1 world unit). Tinted per-renderer.
        private static Sprite SquareSprite()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _square;
        }

        // A filled white circle, 64px => 1 world unit.
        private static Sprite CircleSprite()
        {
            if (_circle != null) return _circle;
            const int s = 64;
            var tex = new Texture2D(s, s);
            var pixels = new Color[s * s];
            float c = (s - 1) / 2f;
            float r = s / 2f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c, dy = y - c;
                    pixels[y * s + x] = (dx * dx + dy * dy <= r * r) ? Color.white : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _circle;
        }
    }
}
