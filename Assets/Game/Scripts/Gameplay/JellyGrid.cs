using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay
{
    public class JellyGrid : MonoBehaviour
    {
        [Header("Grid Size")]
        public int width = 5;
        public int height = 6;
        public float slotStep = 1.0f;

        [Header("Prefabs")]
        [SerializeField] private GameObject _cellPrefab;

        [Header("Scene References")]
        [SerializeField] private Transform _blocksParent;
        [SerializeField] private SpriteRenderer _gridBackground;

        private JellyBlock[,] _grid;
        private List<JellyBlock> _blocksInScene = new List<JellyBlock>();
        private List<GameObject> _slotBackgroundObjects = new List<GameObject>();
        private Sprite _slotBackgroundSprite;

        public System.Action<string, int> OnJelliesCollected;
        public System.Action OnMoveCompleted;

        private bool _isProcessing = false;
        public bool IsProcessing => _isProcessing;

        private void Awake()
        {
            _grid = new JellyBlock[width, height];

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
            float xOffset = (width - 1) * slotStep * 0.5f;
            float yOffset = (height - 1) * slotStep * 0.5f;
            return transform.position + new Vector3(gx * slotStep - xOffset, gy * slotStep - yOffset, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - transform.position;
            float xOffset = (width - 1) * slotStep * 0.5f;
            float yOffset = (height - 1) * slotStep * 0.5f;
            int gx = Mathf.RoundToInt((localPos.x + xOffset) / slotStep);
            int gy = Mathf.RoundToInt((localPos.y + yOffset) / slotStep);
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
            int safetyLimit = 50;

            while (hasChanged && safetyLimit-- > 0)
            {
                hasChanged = false;
                bool merged = await CheckAndMergeJelliesAsync();
                if (merged) hasChanged = true;
            }
        }

        private async UniTask<bool> CheckAndMergeJelliesAsync()
        {
            Dictionary<JellyBlock, HashSet<string>> eliminations = new Dictionary<JellyBlock, HashSet<string>>();

            // Horizontal boundaries: right sub-cells of left block touch left sub-cells of right block
            for (int gy = 0; gy < height; gy++)
            {
                for (int gx = 0; gx < width - 1; gx++)
                {
                    JellyBlock left = _grid[gx, gy];
                    JellyBlock right = _grid[gx + 1, gy];
                    if (left == null || right == null) continue;

                    if (!string.IsNullOrEmpty(left.cellColors[1, 1]) && left.cellColors[1, 1] == right.cellColors[0, 1])
                    {
                        MarkElimination(left, left.cellColors[1, 1], eliminations);
                        MarkElimination(right, right.cellColors[0, 1], eliminations);
                    }
                    if (!string.IsNullOrEmpty(left.cellColors[1, 0]) && left.cellColors[1, 0] == right.cellColors[0, 0])
                    {
                        MarkElimination(left, left.cellColors[1, 0], eliminations);
                        MarkElimination(right, right.cellColors[0, 0], eliminations);
                    }
                }
            }

            // Vertical boundaries: top sub-cells of bottom block touch bottom sub-cells of top block
            for (int gx = 0; gx < width; gx++)
            {
                for (int gy = 0; gy < height - 1; gy++)
                {
                    JellyBlock bottom = _grid[gx, gy];
                    JellyBlock top = _grid[gx, gy + 1];
                    if (bottom == null || top == null) continue;

                    if (!string.IsNullOrEmpty(bottom.cellColors[0, 1]) && bottom.cellColors[0, 1] == top.cellColors[0, 0])
                    {
                        MarkElimination(bottom, bottom.cellColors[0, 1], eliminations);
                        MarkElimination(top, top.cellColors[0, 0], eliminations);
                    }
                    if (!string.IsNullOrEmpty(bottom.cellColors[1, 1]) && bottom.cellColors[1, 1] == top.cellColors[1, 0])
                    {
                        MarkElimination(bottom, bottom.cellColors[1, 1], eliminations);
                        MarkElimination(top, top.cellColors[1, 0], eliminations);
                    }
                }
            }

            if (eliminations.Count == 0) return false;

            foreach (var kvp in eliminations)
            {
                JellyBlock block = kvp.Key;
                foreach (string col in kvp.Value)
                {
                    // 1 point per block that loses this color, regardless of sub-cell count
                    OnJelliesCollected?.Invoke(col, 1);
                }
                block.ApplyEliminations(kvp.Value);
            }

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
                b.PlayPoof(() => { });

            await UniTask.Delay(300);
            return true;
        }

        private void MarkElimination(JellyBlock block, string color, Dictionary<JellyBlock, HashSet<string>> elim)
        {
            if (!elim.ContainsKey(block)) elim[block] = new HashSet<string>();
            elim[block].Add(color);
        }

        private Sprite GenerateGridSlotSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color bgColor = new Color(0.15f, 0.13f, 0.20f, 1f);
            Color borderColor = new Color(0.28f, 0.25f, 0.35f, 1f);
            float borderRadius = 18f;
            float borderWidth = 7f;

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
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (isCorner && dist > borderRadius)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else
                    {
                        bool isBorder = isCorner
                            ? dist >= borderRadius - borderWidth
                            : x < borderWidth || x >= size - borderWidth || y < borderWidth || y >= size - borderWidth;
                        tex.SetPixel(x, y, isBorder ? borderColor : bgColor);
                    }
                }
            }
            tex.Apply();
            // 64 PPU -> sprite is 2.0 world; at scale 0.45 -> 0.9 world visual
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private void CreateVisualGridSlots()
        {
            foreach (var go in _slotBackgroundObjects)
                if (go != null) Destroy(go);
            _slotBackgroundObjects.Clear();

            for (int gx = 0; gx < width; gx++)
            {
                for (int gy = 0; gy < height; gy++)
                {
                    GameObject go;
                    if (_cellPrefab != null)
                    {
                        go = Instantiate(_cellPrefab, transform);
                        go.name = $"SlotBg_{gx}_{gy}";
                        go.transform.position = GridToWorld(gx, gy);
                        // Cell.prefab BoxCollider2D m_Size=(1,1) → exactly 1×1 world at scale 1
                        go.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        if (_slotBackgroundSprite == null)
                            _slotBackgroundSprite = GenerateGridSlotSprite();

                        go = new GameObject($"SlotBg_{gx}_{gy}");
                        go.transform.SetParent(transform);
                        go.transform.position = GridToWorld(gx, gy);
                        go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = _slotBackgroundSprite;
                        sr.sortingOrder = 1;
                    }

                    _slotBackgroundObjects.Add(go);
                }
            }
        }

        public void SpawnInitialJellies()
        {
            ResetGrid();
        }

        public void ResetGrid()
        {
            foreach (var block in _blocksInScene)
                if (block != null) Destroy(block.gameObject);
            _blocksInScene.Clear();
            _grid = new JellyBlock[width, height];
            _isProcessing = false;
        }

        public int GetOccupiedCount()
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (_grid[x, y] != null) count++;
            return count;
        }

        public int GetTotalSlots() => width * height;
    }
}
