using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Manager;
using Game.Data;

namespace Game.Gameplay
{
    public class JellyLauncher : MonoBehaviour
    {
        [Header("Launcher Settings")]
        [SerializeField] private JellyGrid  _grid;
        [SerializeField] private GameObject _blockBasePrefab;
        [SerializeField] private Transform[] _spawnSlots;

        [Header("Prefabs")]
        [SerializeField] private GameObject _jellyPrefab;
        [SerializeField] private GameObject _pickupSlotPrefab;

        [Header("Fallback Color Pool (used when no LevelData is loaded)")]
        [SerializeField] private string[] _fallbackColorIds = new string[] { "blue", "red", "yellow", "green", "purple" };

        // ── Runtime state ──────────────────────────────────────────────────────────
        private string[]    _colorIds;           // active palette for this level
        private JellyBlock[] _slots;
        private int          _activeSlotCount;   // how many slots to show (from LevelData)

        // Pickup division weights (read from LevelData or defaults)
        private float _wOne   = 5f;
        private float _wTwo   = 3f;
        private float _wThree = 0f;
        private float _wFour  = 0f;

        private JellyBlock _draggingBlock;
        private int        _draggedSlotIndex = -1;
        private Vector3    _dragOffset;
        private Vector3    _lastMousePos;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            JellyBlock.JellyPrefab = _jellyPrefab;
        }

        private void Start()
        {
            ApplyLevelSettings();
            GeneratePickupSlotVisuals();
            _slots = new JellyBlock[_activeSlotCount];
            ReplenishAllSlots();
        }

        // ── Level data ────────────────────────────────────────────────────────────

        private void ApplyLevelSettings()
        {
            LevelData level = LevelManager.IsSingletonAlive ? LevelManager.I.CurrentLevel : null;

            if (level != null)
            {
                _colorIds       = level.GetColorPool();
                _activeSlotCount = Mathf.Clamp(level.pickupSlots, 1,
                    _spawnSlots != null ? _spawnSlots.Length : 1);
                _wOne   = level.pickupWeightOne;
                _wTwo   = level.pickupWeightTwo;
                _wThree = level.pickupWeightThree;
                _wFour  = level.pickupWeightFour;
            }
            else
            {
                _colorIds        = _fallbackColorIds;
                _activeSlotCount = _spawnSlots != null ? _spawnSlots.Length : 1;
                // keep default weights
            }

            if (_colorIds == null || _colorIds.Length == 0)
                _colorIds = _fallbackColorIds;
        }

        private void GeneratePickupSlotVisuals()
        {
            if (_pickupSlotPrefab == null || _spawnSlots == null) return;
            for (int i = 0; i < _activeSlotCount; i++)
            {
                if (_spawnSlots[i] == null) continue;
                // Don't double-spawn if already present
                if (_spawnSlots[i].childCount == 0)
                {
                    GameObject slotVisual = Instantiate(_pickupSlotPrefab, _spawnSlots[i]);
                    slotVisual.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                    slotVisual.transform.localScale    = new Vector3(1.5f, 1.5f, 1.5f);
                    slotVisual.transform.localRotation = Quaternion.identity;
                }
            }
            // Hide unused slots
            for (int i = _activeSlotCount; i < _spawnSlots.Length; i++)
                if (_spawnSlots[i] != null) _spawnSlots[i].gameObject.SetActive(false);
        }

        // ── Input ─────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_grid != null && _grid.IsProcessing) return;

            if (Input.GetMouseButtonDown(0))
                TryStartDrag();
            else if (Input.GetMouseButton(0) && _draggingBlock != null)
                UpdateDrag();
            else if (Input.GetMouseButtonUp(0) && _draggingBlock != null)
                EndDrag();
        }

        private void TryStartDrag()
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            for (int i = 0; i < _activeSlotCount; i++)
            {
                if (_slots[i] == null || _spawnSlots[i] == null) continue;
                if (Vector3.Distance(mouseWorld, _spawnSlots[i].position) < 1.0f)
                {
                    _draggingBlock    = _slots[i];
                    _draggedSlotIndex = i;
                    _dragOffset       = _draggingBlock.transform.position - mouseWorld;
                    _lastMousePos     = mouseWorld;

                    _draggingBlock.transform.SetParent(null);
                    _draggingBlock.transform.DOKill();
                    _draggingBlock.transform.DOScale(Vector3.one, 0.08f);
                    break;
                }
            }
        }

        private void UpdateDrag()
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            _lastMousePos = mouseWorld;

            Vector3 liftedPos = mouseWorld + _dragOffset;
            liftedPos.y += 0.4f;
            _draggingBlock.transform.position = liftedPos;
        }

        private void EndDrag()
        {
            _draggingBlock.ResetRotationAndScale();
            _draggingBlock.transform.DOScale(Vector3.one, 0.05f);

            Vector3 blockPos = _draggingBlock.transform.position;
            blockPos.y -= 0.4f;
            Vector2Int gridCoords = _grid.WorldToGrid(blockPos);

            int gx = gridCoords.x;
            int gy = gridCoords.y;

            if (_grid.CanPlaceBlock(gx, gy))
            {
                JellyBlock placed = _draggingBlock;
                int slotIdx = _draggedSlotIndex;
                _slots[slotIdx] = null;
                _grid.DropBlockAsync(placed, gx, gy).Forget();
                SpawnBlockInSlot(slotIdx);
            }
            else
            {
                int slotIdx = _draggedSlotIndex;
                JellyBlock block = _draggingBlock;
                block.transform.SetParent(_spawnSlots[slotIdx]);
                block.transform.DOMove(_spawnSlots[slotIdx].position, 0.25f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => block.PlayLandingBounce());
            }

            _draggingBlock    = null;
            _draggedSlotIndex = -1;
        }

        // ── Slot management ───────────────────────────────────────────────────────

        private void ReplenishAllSlots()
        {
            for (int i = 0; i < _activeSlotCount; i++)
            {
                if (_slots[i] != null) Destroy(_slots[i].gameObject);
                SpawnBlockInSlot(i);
            }
        }

        private void SpawnBlockInSlot(int slotIndex)
        {
            if (_spawnSlots == null || slotIndex >= _activeSlotCount ||
                slotIndex >= _spawnSlots.Length || _spawnSlots[slotIndex] == null) return;

            GameObject go;
            if (_blockBasePrefab != null)
            {
                go = Instantiate(_blockBasePrefab, _spawnSlots[slotIndex]);
            }
            else
            {
                go = new GameObject("JellyBlock_Dynamic", typeof(JellyBlock));
                go.transform.SetParent(_spawnSlots[slotIndex]);
                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform);
                vis.transform.localPosition = Vector3.zero;
            }

            go.transform.position   = _spawnSlots[slotIndex].position;
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

            var block  = go.GetComponent<JellyBlock>();
            var colors = GeneratePickupColors();
            block.Init(colors);
            _slots[slotIndex] = block;
            block.PlayIdleBreathing();
        }

        // ── Color generation ──────────────────────────────────────────────────────

        private string[,] GeneratePickupColors()
        {
            float total = _wOne + _wTwo + _wThree + _wFour;
            if (total <= 0f) total = 1f;

            float r   = Random.value * total;
            int   parts;
            r -= _wOne;   if (r <= 0) parts = 1;
            else { r -= _wTwo;  if (r <= 0) parts = 2;
            else { r -= _wThree; if (r <= 0) parts = 3;
            else parts = 4; } }

            return GenerateColors(_colorIds, parts);
        }

        private static string[,] GenerateColors(string[] palette, int parts)
        {
            string[,] c = new string[2, 2];
            int n = palette.Length;

            switch (parts)
            {
                case 1:
                {
                    string col = palette[Random.Range(0, n)];
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) c[x, y] = col;
                    break;
                }
                case 2:
                {
                    string c1 = palette[Random.Range(0, n)];
                    string c2 = Distinct(palette, c1);
                    if (Random.value < 0.5f)
                    { c[0,0]=c1; c[0,1]=c1; c[1,0]=c2; c[1,1]=c2; }
                    else
                    { c[0,0]=c1; c[1,0]=c1; c[0,1]=c2; c[1,1]=c2; }
                    break;
                }
                case 3:
                {
                    string c1 = palette[Random.Range(0, n)];
                    string c2 = Distinct(palette, c1);
                    string c3 = Distinct(palette, c1);
                    for (int t = 0; t < 10 && c3 == c2; t++) c3 = Distinct(palette, c1);
                    switch (Random.Range(0, 4))
                    {
                        case 0: c[0,1]=c1; c[1,1]=c1; c[0,0]=c2; c[1,0]=c3; break; // top row
                        case 1: c[0,0]=c1; c[0,1]=c1; c[1,0]=c2; c[1,1]=c3; break; // left col
                        case 2: c[1,0]=c1; c[1,1]=c1; c[0,0]=c2; c[0,1]=c3; break; // right col
                        default:c[0,0]=c1; c[1,0]=c1; c[0,1]=c2; c[1,1]=c3; break; // bottom row
                    }
                    break;
                }
                default:
                {
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) c[x,y] = palette[Random.Range(0, n)];
                    break;
                }
            }
            return c;
        }

        private static string Distinct(string[] palette, string exclude)
        {
            if (palette.Length == 1) return palette[0];
            string result = palette[Random.Range(0, palette.Length)];
            for (int i = 0; i < 20 && result == exclude; i++)
                result = palette[Random.Range(0, palette.Length)];
            return result;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void ClearSlots()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) { Destroy(_slots[i].gameObject); _slots[i] = null; }
            }
        }

        public void ResetLauncher()
        {
            ClearSlots();
            ApplyLevelSettings();
            _slots = new JellyBlock[_activeSlotCount];
            ReplenishAllSlots();
        }

        public int GetLauncherBlocksCount()
        {
            if (_slots == null) return 0;
            int count = 0;
            foreach (var s in _slots) if (s != null) count++;
            return count;
        }
    }
}
