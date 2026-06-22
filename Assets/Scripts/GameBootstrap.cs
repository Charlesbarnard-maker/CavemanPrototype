using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring: camera
    /// (follows player), player, spread-out resource patches, the build system,
    /// and the HUD. Placeholder art is tinted squares/circles. Add this component
    /// to one empty GameObject and press Play.
    ///
    /// Design intent: manual gathering bootstraps you; the real loop is placing
    /// buildings on patches so they gather automatically.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            // --- Items ---
            var stone = MakeItem("stone", "Stone", new Color(0.55f, 0.55f, 0.62f));
            var wood = MakeItem("wood", "Wood", new Color(0.52f, 0.34f, 0.16f));

            // --- Buildings (auto-harvest a nearby patch) ---
            var woodHut = MakeBuilding("Wood Hut", wood, 1, 2.0f, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeBuilding("Stone Pit", stone, 1, 2.5f, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));

            // --- Camera (follows the player) ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.16f);

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, circle: true);
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.buildables = new List<BuildingDefinition> { woodHut, stonePit };

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.builder = builder;

            // --- Resource patches spread across a bigger field ---
            // Trees (wood) clustered to the right, rocks (stone) to the left.
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(4f, 1.5f), circle: true);
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(5.5f, -0.5f), circle: true);
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(3.5f, -2.5f), circle: true);
            SpawnNode("Tree", wood, new Color(0.30f, 0.55f, 0.22f), new Vector2(7f, 2f), circle: true);

            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-4f, 1.5f), circle: false);
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-5.5f, -0.5f), circle: false);
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-3.5f, -2.5f), circle: false);
            SpawnNode("Rock", stone, new Color(0.55f, 0.55f, 0.6f), new Vector2(-7f, 2f), circle: false);
        }

        private static ItemDefinition MakeItem(string id, string name, Color color)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.color = color;
            return item;
        }

        private static BuildingDefinition MakeBuilding(string name, ItemDefinition produces, int output,
            float interval, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.produces = produces;
            def.outputPerCycle = output;
            def.interval = interval;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos, bool circle)
        {
            var go = MakeSprite(name, color, pos, 1f, 0, circle);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.capacity = 30;
            node.regenAmount = 1;
            node.regenInterval = 1.5f;
        }

        private static GameObject MakeSprite(string name, Color color, Vector2 pos, float size, int sortingOrder, bool circle)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle ? PlaceholderArt.Circle() : PlaceholderArt.Square();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
