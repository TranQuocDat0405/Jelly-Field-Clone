using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "Level_01", menuName = "Game/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth  = 5;
        public int gridHeight = 6;
        [TextArea(3, 15)]
        [Tooltip("Rows top→bottom. '0' = active cell, '-' = hole. Columns separated by commas.\nExample (3×3):\n0,0,0\n0,-,0\n0,0,0")]
        public string boardLayout;

        [Header("Pre-placed Jellies")]
        public PlacedJelly[] placedJellies = new PlacedJelly[0];

        [Header("Board Fill (initial state)")]
        public int   fillSeed      = 42;
        [Range(0f, 1f)]
        public float fillRate      = 0.7f;
        public float weightOne     = 5f;   // all 4 sub-cells same color
        public float weightTwo     = 3f;   // 2+2 split
        public float weightThree   = 0f;   // 2+1+1
        public float weightFour    = 0f;   // 1+1+1+1

        [Header("Pickup Block Divisions")]
        public float pickupWeightOne   = 5f;
        public float pickupWeightTwo   = 3f;
        public float pickupWeightThree = 0f;
        public float pickupWeightFour  = 0f;

        [Header("Settings")]
        [Tooltip("How many colors appear in this level (uses first N from the palette)")]
        public int maxColors   = 5;
        [Tooltip("Number of pickup slots shown to the player")]
        public int pickupSlots = 2;

        [Header("Targets (colors to clear to win)")]
        public ColorTarget[] targets;

        // ── Fixed color palette (index 0-7) ──────────────────────────────────────
        public static readonly string[] Palette =
            { "blue", "red", "yellow", "green", "purple", "cyan", "orange", "pink" };

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns activeCells[gx, gy] — true if the cell exists in this level.</summary>
        public bool[,] ParseBoard()
        {
            bool[,] result = new bool[gridWidth, gridHeight];

            if (string.IsNullOrWhiteSpace(boardLayout))
            {
                // Default: everything active
                for (int x = 0; x < gridWidth; x++)
                    for (int y = 0; y < gridHeight; y++)
                        result[x, y] = true;
                return result;
            }

            string[] rows = boardLayout.Split('\n');
            for (int row = 0; row < rows.Length && row < gridHeight; row++)
            {
                int gy = gridHeight - 1 - row; // file top row → highest gy
                string[] cols = rows[row].Split(',');
                for (int col = 0; col < cols.Length && col < gridWidth; col++)
                {
                    string cell = cols[col].Trim().ToLower();
                    result[col, gy] = cell != "-" && cell.Length > 0;
                }
            }
            return result;
        }

        /// <summary>Returns the color IDs active in this level (first maxColors from Palette).</summary>
        public string[] GetColorPool()
        {
            int n = Mathf.Clamp(maxColors, 1, Palette.Length);
            string[] pool = new string[n];
            System.Array.Copy(Palette, pool, n);
            return pool;
        }

        /// <summary>Utility: build an all-active boardLayout string for a given size.</summary>
        public static string BuildFullLayout(int w, int h)
        {
            var sb = new System.Text.StringBuilder();
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    if (col > 0) sb.Append(',');
                    sb.Append('0');
                }
                if (row < h - 1) sb.Append('\n');
            }
            return sb.ToString();
        }
    }

    [System.Serializable]
    public class PlacedJelly
    {
        [Tooltip("Grid column (0 = leftmost)")]
        public int gridX;
        [Tooltip("Grid row (0 = bottom)")]
        public int gridY;
        [Tooltip("Index into LevelData.Palette (0=blue, 1=red, 2=yellow, 3=green, 4=purple …)")]
        public int colorIndex;
    }

    [System.Serializable]
    public class ColorTarget
    {
        [Tooltip("Color ID string: blue, red, yellow, green, purple, cyan, orange, pink")]
        public string colorId;
        [Tooltip("How many of this color must be cleared to win")]
        public int count;
    }
}
