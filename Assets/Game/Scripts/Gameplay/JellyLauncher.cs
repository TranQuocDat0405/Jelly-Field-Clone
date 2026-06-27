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

        // Khi kéo, nhấc block về phía camera (z âm) + lên trên (y) để nó vẽ rõ TRÊN các block board,
        // không bị chồng/hòa màu dưới camera nghiêng. Thả ra mới chiếu xuống board (z=0) để đặt.
        private const float DRAG_LIFT_Z = -1.6f;
        private const float DRAG_LIFT_Y = 1.2f; // nhấc block cao hơn ngón tay để không bị che

        private Camera _cam;
        private Camera Cam
        {
            get
            {
                if (_cam != null) return _cam;
                // Camera.main có thể trỏ nhầm camera scene khác → ưu tiên camera cùng scene (đang render board)
                foreach (var c in Camera.allCameras)
                    if (c.gameObject.scene == gameObject.scene) { _cam = c; break; }
                if (_cam == null) _cam = Camera.main;
                return _cam;
            }
        }

        // Chiếu con trỏ lên mặt phẳng board (z=0) — đúng cho cả camera nghiêng (ortho/perspective).
        private bool TryGetBoardPoint(out Vector3 world)
        {
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);
            Plane board = new Plane(Vector3.forward, Vector3.zero); // z = 0
            if (board.Raycast(ray, out float enter))
            {
                world = ray.GetPoint(enter);
                world.z = 0f;
                return true;
            }
            world = Vector3.zero;
            return false;
        }

        // Chiếu MỘT điểm thế giới (vd tâm block đã nhấc) xuống mặt board z=0 theo hướng nhìn camera.
        // → ô đích = nơi block ĐANG HIỆN trên màn hình, không phụ thuộc vị trí chuột/độ nhấc.
        private Vector3 ProjectToBoard(Vector3 worldPoint)
        {
            Vector3 fwd = Cam.transform.forward;
            if (Mathf.Abs(fwd.z) < 1e-4f) { worldPoint.z = 0f; return worldPoint; }
            float t = -worldPoint.z / fwd.z;
            Vector3 p = worldPoint + t * fwd;
            p.z = 0f;
            return p;
        }

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
                    // Đế vừa khít khối (như video), không quá to
                    slotVisual.transform.localScale    = new Vector3(1.0f, 1.0f, 1.0f);
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
            if (!TryGetBoardPoint(out Vector3 mouseWorld)) return;

            for (int i = 0; i < _activeSlotCount; i++)
            {
                if (_slots[i] == null || _spawnSlots[i] == null) continue;
                // So khoảng cách trên mặt board (x,y); slot ở z≈0 nên chiếu z=0 là hợp lệ.
                Vector2 a = new Vector2(mouseWorld.x, mouseWorld.y);
                Vector2 b = new Vector2(_spawnSlots[i].position.x, _spawnSlots[i].position.y);
                if (Vector2.Distance(a, b) < 1.2f)
                {
                    _draggingBlock    = _slots[i];
                    _draggedSlotIndex = i;
                    // Giữ offset chỗ ngón tay nắm vào block (trên mặt board) → đặt theo vị trí block.
                    Vector3 blockOnBoard = _draggingBlock.transform.position; blockOnBoard.z = 0f;
                    _dragOffset       = blockOnBoard - mouseWorld;
                    _lastMousePos     = mouseWorld;

                    _draggingBlock.transform.SetParent(null);
                    _draggingBlock.transform.DOKill();
                    // Nhấc lên về phía camera để nổi rõ trên board, kèm phóng nhẹ
                    _draggingBlock.transform.DOScale(Vector3.one * 1.1f, 0.1f).SetEase(Ease.OutBack);
                    break;
                }
            }
        }

        private void UpdateDrag()
        {
            if (!TryGetBoardPoint(out Vector3 mouseWorld)) return;
            _lastMousePos = mouseWorld;

            // Vị trí block trên mặt board = điểm chạm + offset nắm. NHẤC về phía camera (z âm) + lên (y)
            // → block nổi rõ TRÊN ngón tay và trên các block đã đặt, không bị che/hòa màu.
            Vector3 boardPos = mouseWorld + _dragOffset; boardPos.z = 0f;
            Vector3 liftedPos = boardPos;
            liftedPos.y += DRAG_LIFT_Y;
            liftedPos.z  = DRAG_LIFT_Z;
            _draggingBlock.transform.position = liftedPos;

            // Ô đích = chiếu TÂM BLOCK (đang nhấc) xuống board theo hướng camera → đúng nơi block hiện trên
            // màn hình, không theo vị trí chuột.
            Vector3 proj = ProjectToBoard(liftedPos);
            Vector2Int gc = _grid.WorldToGrid(proj);
            _grid.ShowPlacementPreview(gc.x, gc.y);
        }

        private void EndDrag()
        {
            _grid.ClearPlacementPreview();
            _draggingBlock.ResetRotationAndScale();
            _draggingBlock.transform.DOScale(Vector3.one, 0.05f);

            // Ô đặt = chiếu TÂM BLOCK (đang nhấc) xuống board theo hướng camera (giống lúc preview)
            // → đặt đúng ô mà block đang hiện trên màn hình.
            Vector3 proj = ProjectToBoard(_draggingBlock.transform.position);
            Vector2Int gridCoords = _grid.WorldToGrid(proj);

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
