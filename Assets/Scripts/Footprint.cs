using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Grid-footprint maths for multi-cell buildings. A building's transform sits at
    /// its CENTER (which is a half-integer for even sizes); its occupied cells run
    /// from the bottom-left "anchor" cell. These helpers convert between center,
    /// anchor and the full cell list so placement, occupancy and belt I/O all agree.
    /// </summary>
    public static class Footprint
    {
        /// <summary>Bottom-left cell of a (w,h) building whose centre is at `center`.</summary>
        public static Vector2Int Anchor(Vector3 center, int w, int h) => new Vector2Int(
            Mathf.RoundToInt(center.x - (w - 1) * 0.5f),
            Mathf.RoundToInt(center.y - (h - 1) * 0.5f));

        /// <summary>World-space centre of a (w,h) building anchored at bottom-left `anchor`.</summary>
        public static Vector3 Center(Vector2Int anchor, int w, int h) => new Vector3(
            anchor.x + (w - 1) * 0.5f,
            anchor.y + (h - 1) * 0.5f,
            0f);

        /// <summary>Every grid cell covered by a (w,h) building centred at `center`.</summary>
        public static List<Vector2Int> Cells(Vector3 center, int w, int h)
        {
            var a = Anchor(center, w, h);
            var cells = new List<Vector2Int>(Mathf.Max(1, w * h));
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    cells.Add(new Vector2Int(a.x + i, a.y + j));
            return cells;
        }

        /// <summary>Snap a free world point to the nearest valid centre for a (w,h) building.</summary>
        public static Vector3 SnapCenter(Vector3 world, int w, int h) => Center(Anchor(world, w, h), w, h);
    }
}
