using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Game.Manager;
using Game.Data;
using Game;

namespace Game.Gameplay
{
    public class JellyGrid : MonoBehaviour
    {
        [Header("Grid Size (defaults — overridden by LevelData)")]
        public int   width    = 5;
        public int   height   = 6;
        public float slotStep = 1.0f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _blockPrefab; // same prefab as JellyLauncher uses

        [Header("Scene References")]
        [SerializeField] private Transform       _blocksParent;
        [SerializeField] private SpriteRenderer  _gridBackground;

        // ── State ──────────────────────────────────────────────────────────────────
        private JellyBlock[,]      _grid;
        private bool[,]            _activeCells;
        private List<JellyBlock>   _blocksInScene  = new List<JellyBlock>();
        private List<GameObject>   _slotBgObjects  = new List<GameObject>();
        private Sprite             _fallbackSprite;

        // Viền trắng preview khi kéo (lấy từ child "Ghost Highlight" của Cell prefab)
        private readonly Dictionary<Vector2Int, Transform> _cellHighlights = new Dictionary<Vector2Int, Transform>();
        private Vector2Int _previewCell = new Vector2Int(-1, -1);

        public System.Action<string, int> OnJelliesCollected;
        public System.Action              OnMoveCompleted;

        private bool _isProcessing;
        public  bool  IsProcessing => _isProcessing;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Minimal init; full setup happens in SpawnInitialJellies()
            _activeCells = BuildFullActive(width, height);
            _grid        = new JellyBlock[width, height];
        }

        private void Start()
        {
            SpawnInitialJellies();
        }

        // ── Coordinate helpers ────────────────────────────────────────────────────

        public Vector3 GridToWorld(int gx, int gy)
        {
            float xOffset = (width  - 1) * slotStep * 0.5f;
            float yOffset = (height - 1) * slotStep * 0.5f;
            return transform.position + new Vector3(gx * slotStep - xOffset,
                                                    gy * slotStep - yOffset, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - transform.position;
            float xOffset = (width  - 1) * slotStep * 0.5f;
            float yOffset = (height - 1) * slotStep * 0.5f;
            int gx = Mathf.RoundToInt((local.x + xOffset) / slotStep);
            int gy = Mathf.RoundToInt((local.y + yOffset) / slotStep);
            return new Vector2Int(gx, gy);
        }

        public bool IsInBounds(int gx, int gy) =>
            gx >= 0 && gx < width && gy >= 0 && gy < height;

        public bool IsOccupied(int gx, int gy)
        {
            if (!IsInBounds(gx, gy)) return true;
            return _grid[gx, gy] != null;
        }

        public bool CanPlaceBlock(int gx, int gy)
        {
            if (!IsInBounds(gx, gy))          return false;
            if (!_activeCells[gx, gy])         return false; // hole
            return _grid[gx, gy] == null;
        }

        // ── Placement preview (viền trắng khi kéo) ──────────────────────────────────

        public void ShowPlacementPreview(int gx, int gy)
        {
            var cell = new Vector2Int(gx, gy);
            if (cell == _previewCell) return;
            ClearPlacementPreview();
            if (!CanPlaceBlock(gx, gy)) return;
            if (_cellHighlights.TryGetValue(cell, out var hl) && hl != null)
            {
                hl.gameObject.SetActive(true);
                _previewCell = cell;
            }
        }

        public void ClearPlacementPreview()
        {
            if (_previewCell.x >= 0 &&
                _cellHighlights.TryGetValue(_previewCell, out var hl) && hl != null)
                hl.gameObject.SetActive(false);
            _previewCell = new Vector2Int(-1, -1);
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        // ── Drop & physics ────────────────────────────────────────────────────────

        public async UniTask<bool> DropBlockAsync(JellyBlock block, int gx, int gy)
        {
            if (_isProcessing) return false;
            _isProcessing = true;

            block.transform.SetParent(_blocksParent != null ? _blocksParent : transform);
            Vector3 endPos = GridToWorld(gx, gy);

            block.transform.DOKill();
            await block.transform.DOMove(endPos, 0.2f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();

            _grid[gx, gy] = block;
            _blocksInScene.Add(block);
            block.GridPos = new Vector2Int(gx, gy);

            block.PlayLandingBounce();
            await UniTask.Delay(120);
            await ProcessPhysicsCycleAsync();

            _isProcessing = false;
            OnMoveCompleted?.Invoke();
            return true;
        }

        private async UniTask ProcessPhysicsCycleAsync()
        {
            bool hasChanged = true;
            int  safety     = 50;
            while (hasChanged && safety-- > 0)
            {
                hasChanged = false;
                if (await CheckAndMergeJelliesAsync()) hasChanged = true;
            }
        }

        private async UniTask<bool> CheckAndMergeJelliesAsync()
        {
            var eliminations = new Dictionary<JellyBlock, HashSet<string>>();
            // Điểm merge chung (world) theo (khối, màu) = mép giữa 2 khối khớp → 2 khối gộp về 1 chỗ.
            var mergePts = new Dictionary<JellyBlock, Dictionary<string, Vector3>>();

            // Horizontal: right sub-cells of left block ↔ left sub-cells of right block
            for (int gy = 0; gy < height; gy++)
            for (int gx = 0; gx < width - 1; gx++)
            {
                JellyBlock left  = _grid[gx,     gy];
                JellyBlock right = _grid[gx + 1, gy];
                if (left == null || right == null) continue;
                Vector3 border = (GridToWorld(gx, gy) + GridToWorld(gx + 1, gy)) * 0.5f;

                if (Match(left.cellColors[1, 1], right.cellColors[0, 1]))
                { Mark(left, left.cellColors[1, 1], eliminations); Mark(right, right.cellColors[0, 1], eliminations);
                  MarkMerge(left, left.cellColors[1, 1], border, mergePts); MarkMerge(right, right.cellColors[0, 1], border, mergePts); }
                if (Match(left.cellColors[1, 0], right.cellColors[0, 0]))
                { Mark(left, left.cellColors[1, 0], eliminations); Mark(right, right.cellColors[0, 0], eliminations);
                  MarkMerge(left, left.cellColors[1, 0], border, mergePts); MarkMerge(right, right.cellColors[0, 0], border, mergePts); }
            }

            // Vertical: top sub-cells of bottom block ↔ bottom sub-cells of top block
            for (int gx = 0; gx < width; gx++)
            for (int gy = 0; gy < height - 1; gy++)
            {
                JellyBlock bottom = _grid[gx, gy];
                JellyBlock top    = _grid[gx, gy + 1];
                if (bottom == null || top == null) continue;
                Vector3 border = (GridToWorld(gx, gy) + GridToWorld(gx, gy + 1)) * 0.5f;

                if (Match(bottom.cellColors[0, 1], top.cellColors[0, 0]))
                { Mark(bottom, bottom.cellColors[0, 1], eliminations); Mark(top, top.cellColors[0, 0], eliminations);
                  MarkMerge(bottom, bottom.cellColors[0, 1], border, mergePts); MarkMerge(top, top.cellColors[0, 0], border, mergePts); }
                if (Match(bottom.cellColors[1, 1], top.cellColors[1, 0]))
                { Mark(bottom, bottom.cellColors[1, 1], eliminations); Mark(top, top.cellColors[1, 0], eliminations);
                  MarkMerge(bottom, bottom.cellColors[1, 1], border, mergePts); MarkMerge(top, top.cellColors[1, 0], border, mergePts); }
            }

            if (eliminations.Count == 0) return false;

            // Step 1: Flash matching sub-cells so player sees what's about to pop
            var flashTasks = new List<UniTask>();
            foreach (var kvp in eliminations) flashTasks.Add(kvp.Key.FlashSubCellsAsync(kvp.Value));
            await UniTask.WhenAll(flashTasks);

            await UniTask.Delay(80);
            GameSFX.PlayMerge();

            // Step 2: Pop matching sub-cells — gộp về mép chung giữa 2 khối rồi thu nhỏ biến mất
            var popTasks = new List<UniTask>();
            foreach (var kvp in eliminations)
            {
                mergePts.TryGetValue(kvp.Key, out var mp);
                popTasks.Add(kvp.Key.PopSubCellsAsync(kvp.Value, mp));
            }
            await UniTask.WhenAll(popTasks);

            await UniTask.Delay(100);

            // Step 3: Apply data changes and fire goal events
            foreach (var kvp in eliminations)
            {
                foreach (string col in kvp.Value)
                    OnJelliesCollected?.Invoke(col, 1); // 1 point per block-color pair
                kvp.Key.ApplyEliminations(kvp.Value);
            }

            // Step 4: Bounce surviving blocks to show the color spreading
            foreach (var kvp in eliminations)
                if (!kvp.Key.IsEmpty()) kvp.Key.PlayRefillBounce();

            await UniTask.Delay(80);

            // Step 5: Poof empty blocks
            var toDestroy = new List<JellyBlock>();
            foreach (var kvp in eliminations)
            {
                JellyBlock block = kvp.Key;
                if (block.IsEmpty())
                {
                    _grid[block.GridPos.x, block.GridPos.y] = null;
                    _blocksInScene.Remove(block);
                    toDestroy.Add(block);
                }
            }
            foreach (var b in toDestroy) b.PlayPoof(() => { });

            await UniTask.Delay(280);
            return true;
        }

        private void SilentCleanupBoard()
        {
            bool changed = true;
            int safety = 50;
            while (changed && safety-- > 0)
            {
                var eliminations = new Dictionary<JellyBlock, HashSet<string>>();

                for (int gy = 0; gy < height; gy++)
                for (int gx = 0; gx < width - 1; gx++)
                {
                    JellyBlock left = _grid[gx, gy], right = _grid[gx + 1, gy];
                    if (left == null || right == null) continue;
                    if (Match(left.cellColors[1,1], right.cellColors[0,1])) { Mark(left, left.cellColors[1,1], eliminations); Mark(right, right.cellColors[0,1], eliminations); }
                    if (Match(left.cellColors[1,0], right.cellColors[0,0])) { Mark(left, left.cellColors[1,0], eliminations); Mark(right, right.cellColors[0,0], eliminations); }
                }
                for (int gx = 0; gx < width; gx++)
                for (int gy = 0; gy < height - 1; gy++)
                {
                    JellyBlock bottom = _grid[gx, gy], top = _grid[gx, gy + 1];
                    if (bottom == null || top == null) continue;
                    if (Match(bottom.cellColors[0,1], top.cellColors[0,0])) { Mark(bottom, bottom.cellColors[0,1], eliminations); Mark(top, top.cellColors[0,0], eliminations); }
                    if (Match(bottom.cellColors[1,1], top.cellColors[1,0])) { Mark(bottom, bottom.cellColors[1,1], eliminations); Mark(top, top.cellColors[1,0], eliminations); }
                }

                if (eliminations.Count == 0) { changed = false; break; }

                foreach (var kvp in eliminations) kvp.Key.ApplyEliminations(kvp.Value);

                var toDestroy = new List<JellyBlock>();
                foreach (var kvp in eliminations)
                {
                    if (kvp.Key.IsEmpty())
                    {
                        _grid[kvp.Key.GridPos.x, kvp.Key.GridPos.y] = null;
                        _blocksInScene.Remove(kvp.Key);
                        toDestroy.Add(kvp.Key);
                    }
                }
                foreach (var b in toDestroy) Destroy(b.gameObject);
            }
        }

        private static bool Match(string a, string b) =>
            !string.IsNullOrEmpty(a) && a == b;

        private static void Mark(JellyBlock block, string color,
                                  Dictionary<JellyBlock, HashSet<string>> elim)
        {
            if (!elim.ContainsKey(block)) elim[block] = new HashSet<string>();
            elim[block].Add(color);
        }

        private static void MarkMerge(JellyBlock block, string color, Vector3 worldPoint,
                                      Dictionary<JellyBlock, Dictionary<string, Vector3>> mp)
        {
            if (!mp.ContainsKey(block)) mp[block] = new Dictionary<string, Vector3>();
            mp[block][color] = worldPoint; // mép chung giữa 2 khối khớp màu
        }

        // ── Board setup ───────────────────────────────────────────────────────────

        public void SpawnInitialJellies()
        {
            // 1. Load level settings
            LevelData level = LevelManager.IsSingletonAlive ? LevelManager.I.CurrentLevel : null;
            if (level != null)
            {
                width  = level.gridWidth;
                height = level.gridHeight;
                _activeCells = level.ParseBoard();
            }
            else
            {
                _activeCells = BuildFullActive(width, height);
            }

            // 2. Clear blocks
            foreach (var b in _blocksInScene) if (b != null) Destroy(b.gameObject);
            _blocksInScene.Clear();
            _grid        = new JellyBlock[width, height];
            _isProcessing = false;

            // 3. Grid background
            if (_gridBackground != null)
                _gridBackground.size = new Vector2(width * slotStep + 0.2f,
                                                   height * slotStep + 0.2f);

            // 4. Visual slot backgrounds
            CreateVisualGridSlots();

            // 5. Fill board
            if (level != null)
            {
                FillBoardWithLevel(level);
                SilentCleanupBoard(); // resolve pre-existing matches before first move
            }
        }

        public void ResetGrid()
        {
            foreach (var b in _blocksInScene) if (b != null) Destroy(b.gameObject);
            _blocksInScene.Clear();
            _grid        = new JellyBlock[width, height];
            _isProcessing = false;
        }

        // ── Visual slots ──────────────────────────────────────────────────────────

        private void CreateVisualGridSlots()
        {
            foreach (var go in _slotBgObjects) if (go != null) Destroy(go);
            _slotBgObjects.Clear();
            _cellHighlights.Clear();
            _previewCell = new Vector2Int(-1, -1);

            for (int gx = 0; gx < width; gx++)
            for (int gy = 0; gy < height; gy++)
            {
                if (!_activeCells[gx, gy]) continue; // skip holes

                GameObject go;
                if (_cellPrefab != null)
                {
                    go = Instantiate(_cellPrefab, transform);
                    go.name = $"SlotBg_{gx}_{gy}";
                    go.transform.position   = GridToWorld(gx, gy);
                    go.transform.localScale = Vector3.one;

                    // Bắt sprite "Ghost Highlight" (viền trắng) để bật khi kéo block tới ô này.
                    // Đặt lại đúng tâm ô + đồng phẳng mặt ô (z hơi về phía camera) để không lệch dưới
                    // camera nghiêng.
                    Transform hl = FindDeep(go.transform, "Ghost Highlight");
                    if (hl != null)
                    {
                        Vector3 cw = GridToWorld(gx, gy);
                        hl.position = new Vector3(cw.x, cw.y, -0.3f);
                        hl.gameObject.SetActive(false);
                        _cellHighlights[new Vector2Int(gx, gy)] = hl;
                    }
                }
                else
                {
                    if (_fallbackSprite == null) _fallbackSprite = GenerateFallbackSprite();
                    go = new GameObject($"SlotBg_{gx}_{gy}");
                    go.transform.SetParent(transform);
                    go.transform.position   = GridToWorld(gx, gy);
                    go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite       = _fallbackSprite;
                    sr.sortingOrder = 1;
                }
                _slotBgObjects.Add(go);
            }
        }

        // ── Board fill ────────────────────────────────────────────────────────────

        private void FillBoardWithLevel(LevelData level)
        {
            string[] palette = level.GetColorPool();
            var      rng     = new System.Random(level.fillSeed);

            float totalW = level.weightOne + level.weightTwo +
                           level.weightThree + level.weightFour;
            if (totalW <= 0f) totalW = 1f;

            // Build pre-placed lookup
            var preplaced = new HashSet<Vector2Int>();
            if (level.placedJellies != null)
                foreach (var pj in level.placedJellies)
                    preplaced.Add(new Vector2Int(pj.gridX, pj.gridY));

            for (int gx = 0; gx < width; gx++)
            for (int gy = 0; gy < height; gy++)
            {
                if (!_activeCells[gx, gy]) continue;
                if (preplaced.Contains(new Vector2Int(gx, gy))) continue;
                if (rng.NextDouble() > level.fillRate) continue;

                int parts = PickParts(level.weightOne, level.weightTwo,
                                      level.weightThree, level.weightFour,
                                      totalW, rng);
                string[,] colors = GenerateColors(palette, parts, rng);
                SpawnJellyOnGrid(gx, gy, colors);
            }

            // Pre-placed jellies
            if (level.placedJellies != null)
            {
                foreach (var pj in level.placedJellies)
                {
                    if (!IsInBounds(pj.gridX, pj.gridY)) continue;
                    if (!_activeCells[pj.gridX, pj.gridY]) continue;
                    string colorId = pj.colorIndex < palette.Length ? palette[pj.colorIndex] : palette[0];
                    string[,] colors = new string[2, 2];
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) colors[x, y] = colorId;
                    SpawnJellyOnGrid(pj.gridX, pj.gridY, colors);
                }
            }
        }

        private void SpawnJellyOnGrid(int gx, int gy, string[,] colors)
        {
            GameObject go;
            Transform  parent = _blocksParent != null ? _blocksParent : transform;

            if (_blockPrefab != null)
            {
                go = Instantiate(_blockPrefab, parent);
            }
            else
            {
                go = new GameObject("JellyBlock_Init");
                go.transform.SetParent(parent);
                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform);
                vis.transform.localPosition = Vector3.zero;
                go.AddComponent<JellyBlock>();
            }

            go.transform.position   = GridToWorld(gx, gy);
            go.transform.localScale = Vector3.one;

            var block = go.GetComponent<JellyBlock>();
            if (block == null) { Destroy(go); return; }

            block.Init(colors);
            block.GridPos    = new Vector2Int(gx, gy);
            _grid[gx, gy]   = block;
            _blocksInScene.Add(block);
        }

        // ── Color generation ──────────────────────────────────────────────────────

        private static int PickParts(float w1, float w2, float w3, float w4,
                                     float total, System.Random rng)
        {
            double r = rng.NextDouble() * total;
            r -= w1; if (r <= 0) return 1;
            r -= w2; if (r <= 0) return 2;
            r -= w3; if (r <= 0) return 3;
            return 4;
        }

        private static string[,] GenerateColors(string[] palette, int parts, System.Random rng)
        {
            string[,] c = new string[2, 2];
            int n = palette.Length;

            switch (parts)
            {
                case 1:
                {
                    string col = palette[rng.Next(n)];
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) c[x, y] = col;
                    break;
                }
                case 2:
                {
                    string c1 = palette[rng.Next(n)];
                    string c2 = Distinct(palette, c1, rng);
                    if (rng.NextDouble() < 0.5)
                    { c[0,0]=c1; c[0,1]=c1; c[1,0]=c2; c[1,1]=c2; } // vertical
                    else
                    { c[0,0]=c1; c[1,0]=c1; c[0,1]=c2; c[1,1]=c2; } // horizontal
                    break;
                }
                case 3:
                {
                    string c1 = palette[rng.Next(n)];
                    string c2 = Distinct(palette, c1, rng);
                    string c3 = Distinct(palette, c1, rng);
                    for (int t = 0; t < 10 && c3 == c2; t++) c3 = Distinct(palette, c1, rng);
                    // c1 gets 2 adjacent cells (never diagonal)
                    switch (rng.Next(4))
                    {
                        case 0: c[0,1]=c1; c[1,1]=c1; c[0,0]=c2; c[1,0]=c3; break; // top row
                        case 1: c[0,0]=c1; c[0,1]=c1; c[1,0]=c2; c[1,1]=c3; break; // left col
                        case 2: c[1,0]=c1; c[1,1]=c1; c[0,0]=c2; c[0,1]=c3; break; // right col
                        default:c[0,0]=c1; c[1,0]=c1; c[0,1]=c2; c[1,1]=c3; break; // bottom row
                    }
                    break;
                }
                default: // 4
                {
                    for (int x = 0; x < 2; x++)
                        for (int y = 0; y < 2; y++)
                            c[x, y] = palette[rng.Next(n)];
                    break;
                }
            }
            return c;
        }

        private static string Distinct(string[] palette, string exclude, System.Random rng)
        {
            if (palette.Length == 1) return palette[0];
            string result = palette[rng.Next(palette.Length)];
            for (int i = 0; i < 20 && result == exclude; i++)
                result = palette[rng.Next(palette.Length)];
            return result;
        }

        // ── Queries ───────────────────────────────────────────────────────────────

        public int GetOccupiedCount()
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (_grid[x, y] != null) count++;
            return count;
        }

        public int GetTotalSlots()
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (_activeCells[x, y]) count++;
            return count;
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        private static bool[,] BuildFullActive(int w, int h)
        {
            bool[,] a = new bool[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) a[x, y] = true;
            return a;
        }

        private static Sprite GenerateFallbackSprite()
        {
            int     size        = 128;
            var     tex         = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color   bg          = new Color(0.15f, 0.13f, 0.20f, 1f);
            Color   border      = new Color(0.28f, 0.25f, 0.35f, 1f);
            Color   transparent = new Color(0, 0, 0, 0);
            float   radius      = 18f;
            float   bw          = 7f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x < radius ? radius - x : x > size - 1 - radius ? x - (size - 1 - radius) : 0f;
                float dy = y < radius ? radius - y : y > size - 1 - radius ? y - (size - 1 - radius) : 0f;
                bool  corner = dx > 0 && dy > 0;
                float dist   = Mathf.Sqrt(dx * dx + dy * dy);

                if (corner && dist > radius) tex.SetPixel(x, y, transparent);
                else
                {
                    bool isBorder = corner ? dist >= radius - bw
                                           : x < bw || x >= size - bw || y < bw || y >= size - bw;
                    tex.SetPixel(x, y, isBorder ? border : bg);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
