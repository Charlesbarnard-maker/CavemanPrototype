using UnityEngine;

namespace Caveman
{
    /// <summary>The procedural placeholder shape a visual falls back to when no external sprite is
    /// assigned — i.e. the current code-generated art (see PlaceholderArt).</summary>
    public enum PlaceholderShape { None, Square, Circle, Triangle, Hexagon, Conveyor, Tree, Rock, Splitter, Merger }

    /// <summary>
    /// A reference to a visual: an external sprite NAME (resolved centrally via <see cref="SpriteDatabase"/>
    /// from Resources/art/&lt;name&gt;) plus the procedural FALLBACK to use until a real sprite exists.
    /// Every building / resource node / belt / item carries one. Nothing is loaded here — all resolution
    /// (and the future sprite-pack swap) is centralised in SpriteDatabase, so gameplay never touches art.
    /// </summary>
    [System.Serializable]
    public class SpriteDefinition
    {
        public string name = "";                                  // external sprite key; empty = none yet → fallback
        public PlaceholderShape fallback = PlaceholderShape.Square;

        public SpriteDefinition() { }
        public SpriteDefinition(string name, PlaceholderShape fallback) { this.name = name; this.fallback = fallback; }

        /// <summary>A fallback-only definition (no external sprite assigned yet).</summary>
        public static SpriteDefinition Of(PlaceholderShape fallback) => new SpriteDefinition("", fallback);
    }
}
