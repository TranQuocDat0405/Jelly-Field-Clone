using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Game.Gameplay
{
    public class JellyBlock : MonoBehaviour
    {
        [Header("Jelly Properties")]
        public string[,] cellColors = new string[2, 2]; // [x, y] -> colorId

        [Header("Visual References")]
        [SerializeField] private Transform _visualParent;

        private Vector2Int _gridPos;
        private SpriteRenderer[,] _renderers = new SpriteRenderer[2, 2];
        private Sprite _whitePixelSprite;

        public Vector2Int GridPos
        {
            get => _gridPos;
            set => _gridPos = value;
        }

        private void Awake()
        {
            GenerateWhiteSprite();
            if (_visualParent == null)
            {
                if (transform.childCount > 0)
                    _visualParent = transform.GetChild(0);
                else
                    _visualParent = transform;
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

        public static Color GetColorFromId(string id)
        {
            switch (id)
            {
                case "blue": return new Color(0.2f, 0.6f, 1f);
                case "red": return new Color(1f, 0.3f, 0.3f);
                case "yellow": return new Color(1f, 0.8f, 0.2f);
                case "green": return new Color(0.3f, 0.8f, 0.3f);
                case "purple": return new Color(0.7f, 0.3f, 0.9f);
                case "cyan": return new Color(0.2f, 0.8f, 0.9f);
                default: return Color.clear;
            }
        }

        public void Init(string[,] colors)
        {
            cellColors = (string[,])colors.Clone();
            RefreshVisuals();
            PlayIdleBreathing();
        }

        public void RefreshVisuals()
        {
            GenerateWhiteSprite();

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    string colorId = cellColors[x, y];
                    string childName = $"Cell_{x}_{y}";
                    Transform childTrans = _visualParent.Find(childName);
                    
                    if (childTrans == null)
                    {
                        GameObject childGo = new GameObject(childName);
                        childGo.transform.SetParent(_visualParent);
                        childGo.transform.localPosition = new Vector3(x == 0 ? -0.5f : 0.5f, y == 0 ? -0.5f : 0.5f, 0f);
                        childGo.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                        childGo.transform.localRotation = Quaternion.identity;
                        
                        SpriteRenderer sr = childGo.AddComponent<SpriteRenderer>();
                        sr.sprite = _whitePixelSprite;
                        sr.sortingOrder = 10;
                        _renderers[x, y] = sr;
                        childTrans = childGo.transform;
                    }
                    else
                    {
                        _renderers[x, y] = childTrans.GetComponent<SpriteRenderer>();
                    }

                    if (string.IsNullOrEmpty(colorId))
                    {
                        childTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        childTrans.gameObject.SetActive(true);
                        if (_renderers[x, y] != null)
                        {
                            _renderers[x, y].color = GetColorFromId(colorId);
                        }
                    }
                }
            }
        }

        public void ApplyEliminations(HashSet<string> colorsToEliminate)
        {
            // Clear cells that match eliminated colors
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (cellColors[x, y] != null && colorsToEliminate.Contains(cellColors[x, y]))
                    {
                        cellColors[x, y] = null;
                    }
                }
            }

            // Check remaining colors
            HashSet<string> remainingColors = new HashSet<string>();
            string firstRemainingColor = null;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (cellColors[x, y] != null)
                    {
                        remainingColors.Add(cellColors[x, y]);
                        if (firstRemainingColor == null)
                        {
                            firstRemainingColor = cellColors[x, y];
                        }
                    }
                }
            }

            if (remainingColors.Count == 0)
            {
                // Block is completely empty, it will be destroyed
            }
            else if (remainingColors.Count == 1)
            {
                // Only 1 color left -> morph block into 1 single solid color occupying all 4 cells!
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        cellColors[x, y] = firstRemainingColor;
                    }
                }
            }
            else
            {
                // 2 or more colors remaining -> replace empty cells with the color of an adjacent cell in the block
                bool filledAny = true;
                while (filledAny)
                {
                    filledAny = false;
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            if (cellColors[x, y] == null)
                            {
                                string adjacentColor = FindAdjacentColorInBlock(x, y);
                                if (adjacentColor != null)
                                {
                                    cellColors[x, y] = adjacentColor;
                                    filledAny = true;
                                }
                            }
                        }
                    }
                }
            }

            RefreshVisuals();
        }

        private string FindAdjacentColorInBlock(int x, int y)
        {
            // orthogonal neighbors
            if (cellColors[1 - x, y] != null) return cellColors[1 - x, y];
            if (cellColors[x, 1 - y] != null) return cellColors[x, 1 - y];
            // diagonal neighbor
            if (cellColors[1 - x, 1 - y] != null) return cellColors[1 - x, 1 - y];
            return null;
        }

        public bool IsEmpty()
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (!string.IsNullOrEmpty(cellColors[x, y])) return false;
                }
            }
            return true;
        }

        public void PlayIdleBreathing()
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            _visualParent.DOScale(new Vector3(1.03f, 0.97f, 1f), 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void ApplyDragStretch(Vector3 velocity)
        {
            if (_visualParent == null) return;
            
            float speed = velocity.magnitude;
            if (speed < 0.05f)
            {
                _visualParent.localScale = Vector3.one;
                return;
            }

            float stretch = Mathf.Min(speed * 0.02f, 0.25f);
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            _visualParent.rotation = Quaternion.Euler(0, 0, angle);
            _visualParent.localScale = new Vector3(1f + stretch, 1f - stretch, 1f);
        }

        public void ResetRotationAndScale()
        {
            if (_visualParent == null) return;
            _visualParent.rotation = Quaternion.identity;
            _visualParent.localScale = Vector3.one;
        }

        public void PlayLandingBounce()
        {
            if (_visualParent == null) return;

            _visualParent.DOKill(true);
            ResetRotationAndScale();

            _visualParent.DOScale(new Vector3(1.25f, 0.75f, 1f), 0.12f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _visualParent.DOScale(new Vector3(0.9f, 1.1f, 1f), 0.08f)
                        .SetEase(Ease.InOutQuad)
                        .OnComplete(() =>
                        {
                            _visualParent.DOScale(Vector3.one, 0.06f)
                                .SetEase(Ease.InQuad);
                        });
                });
        }

        public void PlayMergeAndCollect(Vector3 targetPos, System.Action onComplete)
        {
            if (_visualParent == null) return;

            _visualParent.DOKill(true);
            
            Sequence seq = DOTween.Sequence();
            seq.Append(_visualParent.DOScale(new Vector3(1.3f, 1.3f, 1f), 0.15f).SetEase(Ease.OutBack));
            seq.Append(transform.DOMove(targetPos, 0.5f).SetEase(Ease.InBack));
            seq.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            });
        }

        public void PlayPoof(System.Action onComplete)
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);

            _visualParent.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                    Destroy(gameObject);
                });
        }
    }
}
