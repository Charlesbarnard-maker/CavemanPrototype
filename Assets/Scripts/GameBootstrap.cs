using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring: camera
    /// (follows player), player, spread-out resource patches, the build system,
    /// and the HUD. Add this component to one empty GameObject and press Play.
    ///
    /// Loop: gather manually to bootstrap -> place a collector near a patch (its
    /// worker walks out, chops, and hauls back) -> place matching storage next to
    /// the collector -> stockpile grows hands-free -> the pool funds more buildings.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            // --- Items ---
            var stone = MakeItem("stone", "Stone", new Color(0.55f, 0.55f, 0.62f));
            var wood = MakeItem("wood", "Wood", new Color(0.52f, 0.34f, 0.16f));

            // --- Buildings ---
            var woodHut = MakeCollector("Wood Hut", wood, 1, 2.0f, 12, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeCollector("Stone Pit", stone, 1, 2.5f, 12, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));
            var woodStore = MakeStorage("Wood Warehouse", wood, 100, new Color(0.62f, 0.40f, 0.20f),
                new ItemAmount(wood, 8));
            var stoneStore = MakeStorage("Stone Storage", stone, 100, new Color(0.40f, 0.43f, 0.50f),
                new ItemAmount(wood, 8));

            // --- Camera (follows the player) ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.16f);

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, PlaceholderArt.Circle());
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f; // matches the worker commute range
            builder.buildables = new List<BuildingDefinition> { woodHut, stonePit, woodStore, stoneStore };

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.builder = builder;

            // --- Resource patches: trees (green triangles) right, rocks (grey hexagons) left ---
            var treeColor = new Color(0.27f, 0.55f, 0.22f);
            SpawnNode("Tree", wood, treeColor, new Vector2(5f, 2.5f), 1.3f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(7f, 1f), 1.2f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(6f, -1.5f), 1.4f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(8.5f, 2.2f), 1.1f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(4.5f, -2.8f), 1.3f, PlaceholderArt.Triangle());

            var rockColor = new Color(0.55f, 0.55f, 0.6f);
            SpawnNode("Rock", stone, rockColor, new Vector2(-5f, 2.5f), 1.2f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-7f, 1f), 1.1f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-6f, -1.5f), 1.3f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-8.5f, 2.2f), 1.0f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-4.5f, -2.8f), 1.2f, PlaceholderArt.Hexagon());
        }

        private static ItemDefinition MakeItem(string id, string name, Color color)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.color = color;
            return item;
        }

        private static BuildingDefinition MakeCollector(string name, ItemDefinition item, int output,
            float interval, int capacity, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Collector;
            def.item = item;
            def.outputPerCycle = output;
            def.interval = interval;
            def.capacity = capacity;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static BuildingDefinition MakeStorage(string name, ItemDefinition item, int capacity,
            Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Storage;
            def.item = item;
            def.capacity = capacity;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos, float size, Sprite sprite)
        {
            var go = MakeSprite(name, color, pos, size, 0, sprite);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.capacity = 30;
            node.regenAmount = 1;
            node.regenInterval = 1.5f;
        }

        private static GameObject MakeSprite(string name, Color color, Vector2 pos, float size, int sortingOrder, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
