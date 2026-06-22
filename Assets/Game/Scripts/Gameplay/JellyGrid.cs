using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay
{
    public class JellyGrid : MonoBehaviour
    {
        [Header("Grid Size")]
        public int width = 4;
        public int height = 4;
        public float slotStep = 1.05f;

        [Header("Scene References")]
        [SerializeField] private Transform _blocksParent;
        [SerializeField] private SpriteRenderer _gridBackground;

        private JellyBlock[,] _grid;
        private List<JellyBlock> _blocksInScene = new List<JellyBlock>();
        private List<GameObject> _slotBackgroundObjects = new List<GameObject>();
        private Sprite _slotBackgroundSprite;

        public System.Action<string, int> OnJelliesCollected; // colorId, count
        public System.Action OnMoveCompleted;

        private bool _isProcessing = false;

        public bool IsProcessing => _isProcessing;

        private void Awake()
        {
            _grid = new JellyBlock[width, height];
            
            // Adjust grid background visual to match size with a bit of padding
            if (_gridBackground != null)
            {
                _gridBackground.size = new Vector2(width * slotStep + 0.2f, height * slotStep + 0.2f);
                _gridBackground.transform.localPosition = Vector3.zero;
            }

            CreateVisualGridSlots();
        }

        private void Start()
        {
            SpawnInitialJellies();
        }

        public Vector3 GridToWorld(int gx, int gy)
        {
            float offset = (width - 1) * slotStep * 0.5f;
            return transform.position + new Vector3(gx * slotStep - offset, gy * slotStep - offset, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - transform.position;
            float offset = (width - 1) * slotStep * 0.5f;
            int gx = Mathf.RoundToInt((localPos.x + offset) / slotStep);
            int gy = Mathf.RoundToInt((localPos.y + offset) / slotStep);
            return new Vector2Int(gx, gy);
        }

        public bool IsInBounds(int gx, int gy)
        {
            return gx >= 0 && gx < width && gy >= 0 && gy < height;
        }

        public bool IsOccupied(int gx, int gy)
        {
            if (!IsInBounds(gx, gy)) return true;
            return _grid[gx, gy] != null;
        }

        public bool CanPlaceBlock(int gx, int gy)
        {
            return IsInBounds(gx, gy) && _grid[gx, gy] == null;
        }

        public async UniTask<bool> DropBlockAsync(JellyBlock block, int gx, int gy)
        {
            if (_isProcessing) return false;
            _isProcessing = true;

            block.transform.SetParent(_blocksParent != null ? _blocksParent : transform);
            
            Vector3 endPos = GridToWorld(gx, gy);
            
            // Animate fall/snap
            block.transform.DOKill();
            await block.transform.DOMove(endPos, 0.25f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();

            // Set grid occupation
            _grid[gx, gy] = block;
            _blocksInScene.Add(block);
            block.GridPos = new Vector2Int(gx, gy);

            // Bounce visual
            block.PlayLandingBounce();
            await UniTask.Delay(150);

            // Process merges and morph cascades recursively
            await ProcessPhysicsCycleAsync();

            _isProcessing = false;
            OnMoveCompleted?.Invoke();
            return true;
        }

        private async UniTask ProcessPhysicsCycleAsync()
        {
            bool hasChanged = true;
            int safetyLimit = 50;

            while (hasChanged && safetyLimit-- > 0)
            {
                hasChanged = false;

                // 1. Check for same-colored jelly connections and merge/pop them
                bool merged = await CheckAndMergeJelliesAsync();
                if (merged) hasChanged = true;
            }
        }

        private async UniTask<bool> CheckAndMergeJelliesAsync()
        {
            // Map each block to a set of colors to be eliminated from it
            Dictionary<JellyBlock, HashSet<string>> eliminations = new Dictionary<JellyBlock, HashSet<string>>();

            // 1. Scan horizontal boundaries between (gx, gy) and (gx+1, gy)
            for (int gy = 0; gy < height; gy++)
            {
                for (int gx = 0; gx < width - 1; gx++)
                {
                    JellyBlock leftBlock = _grid[gx, gy];
                    JellyBlock rightBlock = _grid[gx + 1, gy];
                    if (leftBlock == null || rightBlock == null) continue;

                    // TR of left block touches TL of right block
                    string colorTR = leftBlock.cellColors[1, 1];
                    string colorTL = rightBlock.cellColors[0, 1];
                    if (!string.IsNullOrEmpty(colorTR) && colorTR == colorTL)
                    {
                        MarkElimination(leftBlock, colorTR, eliminations);
                        MarkElimination(rightBlock, colorTL, eliminations);
                    }

                    // BR of left block touches BL of right block
                    string colorBR = leftBlock.cellColors[1, 0];
                    string colorBL = rightBlock.cellColors[0, 0];
                    if (!string.IsNullOrEmpty(colorBR) && colorBR == colorBL)
                    {
                        MarkElimination(leftBlock, colorBR, eliminations);
                        MarkElimination(rightBlock, colorBL, eliminations);
                    }
                }
            }

            // 2. Scan vertical boundaries between (gx, gy) and (gx, gy+1)
            for (int gx = 0; gx < width; gx++)
            {
                for (int gy = 0; gy < height - 1; gy++)
                {
                    JellyBlock bottomBlock = _grid[gx, gy];
                    JellyBlock topBlock = _grid[gx, gy + 1];
                    if (bottomBlock == null || topBlock == null) continue;

                    // TL of bottom block touches BL of top block
                    string colorTL = bottomBlock.cellColors[0, 1];
                    string colorBL = topBlock.cellColors[0, 0];
                    if (!string.IsNullOrEmpty(colorTL) && colorTL == colorBL)
                    {
                        MarkElimination(bottomBlock, colorTL, eliminations);
                        MarkElimination(topBlock, colorBL, eliminations);
                    }

                    // TR of bottom block touches BR of top block
                    string colorTR = bottomBlock.cellColors[1, 1];
                    string colorBR = topBlock.cellColors[1, 0];
                    if (!string.IsNullOrEmpty(colorTR) && colorTR == colorBR)
                    {
                        MarkElimination(bottomBlock, colorTR, eliminations);
                        MarkElimination(topBlock, colorBR, eliminations);
                    }
                }
            }

            if (eliminations.Count == 0) return false;

            // 3. Apply eliminations and count scores
            foreach (var kvp in eliminations)
            {
                JellyBlock block = kvp.Key;
                HashSet<string> colorsToElim = kvp.Value;

                // Count how many cells of these colors are eliminated
                foreach (string col in colorsToElim)
                {
                    int matchCount = 0;
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            if (block.cellColors[x, y] == col) matchCount++;
                        }
                    }
                    if (matchCount > 0)
                    {
                        OnJelliesCollected?.Invoke(col, matchCount);
                    }
                }

                // Apply elimination rules inside the block (morphed to single color or empty replaced with adjacent)
                block.ApplyEliminations(colorsToElim);
            }

            // Play animations for eliminated (completely empty) blocks
            List<JellyBlock> toDestroy = new List<JellyBlock>();
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

            foreach (var b in toDestroy)
            {
                b.PlayPoof(() => {});
            }

            // Wait for morphs and poof animations
            await UniTask.Delay(350);
            return true;
        }

        private void MarkElimination(JellyBlock block, string color, Dictionary<JellyBlock, HashSet<string>> eliminations)
        {
            if (!eliminations.ContainsKey(block))
            {
                eliminations[block] = new HashSet<string>();
            }
            eliminations[block].Add(color);
        }

        private Sprite GenerateGridSlotSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            
            // Nice dark background matching the user screenshot
            Color bgColor = new Color(0.20f, 0.18f, 0.24f, 1f);
            Color borderColor = new Color(0.32f, 0.29f, 0.38f, 1f);
            
            float borderRadius = 20f;
            float borderWidth = 8f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0;
                    if (x < borderRadius) dx = borderRadius - x;
                    else if (x > size - 1 - borderRadius) dx = x - (size - 1 - borderRadius);

                    float dy = 0;
                    if (y < borderRadius) dy = borderRadius - y;
                    else if (y > size - 1 - borderRadius) dy = y - (size - 1 - borderRadius);

                    bool isCorner = dx > 0 && dy > 0;
                    float distToCornerCenter = Mathf.Sqrt(dx * dx + dy * dy);

                    if (isCorner && distToCornerCenter > borderRadius)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else
                    {
                        bool isBorder = false;
                        if (isCorner)
                        {
                            isBorder = distToCornerCenter >= borderRadius - borderWidth;
                        }
                        else
                        {
                            isBorder = x < borderWidth || x >= size - borderWidth ||
                                       y < borderWidth || y >= size - borderWidth;
                        }

                        if (isBorder)
                        {
                            tex.SetPixel(x, y, borderColor);
                        }
                        else
                        {
                            tex.SetPixel(x, y, bgColor);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f); // 64 PPU -> size 2.0x2.0
        }

        private void CreateVisualGridSlots()
        {
            foreach (var go in _slotBackgroundObjects)
            {
                if (go != null) Destroy(go);
            }
            _slotBackgroundObjects.Clear();

            if (_slotBackgroundSprite == null)
            {
                _slotBackgroundSprite = GenerateGridSlotSprite();
            }

            for (int gx = 0; gx < width; gx++)
            {
                for (int gy = 0; gy < height; gy++)
                {
                    GameObject go = new GameObject($"SlotBg_{gx}_{gy}");
                    go.transform.SetParent(transform);
                    go.transform.position = GridToWorld(gx, gy);
                    go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _slotBackgroundSprite;
                    sr.sortingOrder = 1;
                    
                    _slotBackgroundObjects.Add(go);
                }
            }
        }

        public void SpawnInitialJellies()
        {
            // Clear board on start: starts empty as requested!
            ResetGrid();
        }

        public void ResetGrid()
        {
            foreach (var block in _blocksInScene)
            {
                if (block != null) Destroy(block.gameObject);
            }
            _blocksInScene.Clear();
            _grid = new JellyBlock[width, height];
            _isProcessing = false;
        }

        public int GetOccupiedCount()
        {
            int count = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_grid[x, y] != null) count++;
                }
            }
            return count;
        }

        public int GetTotalSlots()
        {
            return width * height;
        }
    }
}
