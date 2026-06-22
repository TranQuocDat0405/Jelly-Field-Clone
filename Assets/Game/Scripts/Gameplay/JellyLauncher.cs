using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay
{
    public class JellyLauncher : MonoBehaviour
    {
        [Header("Launcher Settings")]
        [SerializeField] private JellyGrid _grid;
        [SerializeField] private GameObject _blockBasePrefab;
        [SerializeField] private Transform[] _spawnSlots; // Spawns blocks in these slot transforms

        [Header("Block Spawning Pool")]
        [SerializeField] private string[] _colorIds = new string[] { "blue", "red", "yellow", "green", "purple" };

        private JellyBlock[] _slots;
        private JellyBlock _draggingBlock = null;
        private int _draggedSlotIndex = -1;
        private Vector3 _dragOffset;
        private Vector3 _lastMousePos;
        private Vector3 _dragVelocity;

        private Sprite _whitePixelSprite;

        private void Start()
        {
            _slots = new JellyBlock[_spawnSlots.Length];
            GenerateWhiteSprite();
            
            bool hasBlocks = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) hasBlocks = true;
            }
            if (!hasBlocks)
            {
                ReplenishAllSlots();
            }
        }

        private void GenerateWhiteSprite()
        {
            if (_whitePixelSprite != null) return;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void Update()
        {
            if (_grid != null && _grid.IsProcessing) return;

            if (Input.GetMouseButtonDown(0))
            {
                TryStartDrag();
            }
            else if (Input.GetMouseButton(0) && _draggingBlock != null)
            {
                UpdateDrag();
            }
            else if (Input.GetMouseButtonUp(0) && _draggingBlock != null)
            {
                EndDrag();
            }
        }

        private void TryStartDrag()
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            for (int i = 0; i < _spawnSlots.Length; i++)
            {
                if (_slots[i] == null) continue;

                float dist = Vector3.Distance(mouseWorld, _spawnSlots[i].position);
                if (dist < 1.0f) // Drag radius
                {
                    _draggingBlock = _slots[i];
                    _draggedSlotIndex = i;
                    _dragOffset = _draggingBlock.transform.position - mouseWorld;
                    _lastMousePos = mouseWorld;
                    
                    _draggingBlock.transform.SetParent(null);
                    
                    _draggingBlock.transform.DOKill();
                    _draggingBlock.transform.DOScale(new Vector3(0.45f * 1.15f, 0.45f * 1.15f, 1f), 0.1f);
                    break;
                }
            }
        }

        private void UpdateDrag()
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector3 delta = mouseWorld - _lastMousePos;
            _dragVelocity = delta / Time.deltaTime;
            _lastMousePos = mouseWorld;

            _draggingBlock.transform.position = mouseWorld + _dragOffset;
            _draggingBlock.ApplyDragStretch(_dragVelocity);
        }

        private void EndDrag()
        {
            _draggingBlock.ResetRotationAndScale();
            _draggingBlock.transform.DOScale(new Vector3(0.45f, 0.45f, 1f), 0.05f);

            Vector3 blockPos = _draggingBlock.transform.position;
            Vector2Int gridCoords = _grid.WorldToGrid(blockPos);

            int gx = gridCoords.x;
            int gy = gridCoords.y;

            if (_grid.CanPlaceBlock(gx, gy))
            {
                JellyBlock placedBlock = _draggingBlock;
                _slots[_draggedSlotIndex] = null;
                
                _grid.DropBlockAsync(placedBlock, gx, gy).Forget();
                SpawnBlockInSlot(_draggedSlotIndex);
            }
            else
            {
                int slotIndex = _draggedSlotIndex;
                JellyBlock block = _draggingBlock;
                
                block.transform.SetParent(_spawnSlots[slotIndex]);
                block.transform.DOMove(_spawnSlots[slotIndex].position, 0.25f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        block.PlayLandingBounce();
                    });
            }

            _draggingBlock = null;
            _draggedSlotIndex = -1;
        }

        private void ReplenishAllSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) Destroy(_slots[i].gameObject);
                SpawnBlockInSlot(i);
            }
        }

        private void SpawnBlockInSlot(int slotIndex)
        {
            if (_spawnSlots == null || slotIndex >= _spawnSlots.Length || _spawnSlots[slotIndex] == null) return;

            GenerateWhiteSprite();

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

            go.transform.position = _spawnSlots[slotIndex].position;
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(new Vector3(0.45f, 0.45f, 1f), 0.25f).SetEase(Ease.OutBack);

            var block = go.GetComponent<JellyBlock>();
            
            // Randomize color configurations:
            // - 1 color: 40%
            // - 2 colors (split): 40%
            // - 4 colors (split): 20%
            string[,] colors = new string[2, 2];
            float r = Random.value;
            if (r < 0.4f)
            {
                int cIndex = Random.Range(0, _colorIds.Length);
                string col = _colorIds[cIndex];
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        colors[x, y] = col;
            }
            else if (r < 0.8f)
            {
                int c1 = Random.Range(0, _colorIds.Length);
                int c2 = Random.Range(0, _colorIds.Length);
                while (c2 == c1) c2 = Random.Range(0, _colorIds.Length);
                
                string col1 = _colorIds[c1];
                string col2 = _colorIds[c2];
                
                if (Random.value < 0.5f)
                {
                    // Vertical split (left = col1, right = col2)
                    colors[0, 0] = col1;
                    colors[0, 1] = col1;
                    colors[1, 0] = col2;
                    colors[1, 1] = col2;
                }
                else
                {
                    // Horizontal split (bottom = col1, top = col2)
                    colors[0, 0] = col1;
                    colors[1, 0] = col1;
                    colors[0, 1] = col2;
                    colors[1, 1] = col2;
                }
            }
            else
            {
                List<string> availableColors = new List<string>(_colorIds);
                // Simple shuffle
                for (int idx = 0; idx < availableColors.Count; idx++)
                {
                    int swapIdx = Random.Range(idx, availableColors.Count);
                    string tmp = availableColors[idx];
                    availableColors[idx] = availableColors[swapIdx];
                    availableColors[swapIdx] = tmp;
                }
                colors[0, 0] = availableColors[0];
                colors[1, 0] = availableColors[1];
                colors[0, 1] = availableColors[2];
                colors[1, 1] = availableColors[3];
            }

            block.Init(colors);
            _slots[slotIndex] = block;
            block.PlayIdleBreathing();
        }

        public void ClearSlots()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    Destroy(_slots[i].gameObject);
                    _slots[i] = null;
                }
            }
        }

        public void ResetLauncher()
        {
            ClearSlots();
            ReplenishAllSlots();
        }

        public int GetLauncherBlocksCount()
        {
            if (_slots == null) return 0;
            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) count++;
            }
            return count;
        }
    }
}
