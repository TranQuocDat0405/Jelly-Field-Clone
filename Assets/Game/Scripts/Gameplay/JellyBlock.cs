using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay
{
    public class JellyBlock : MonoBehaviour
    {
        [Header("Jelly Properties")]
        public string[,] cellColors = new string[2, 2];

        [Header("Visual References")]
        [SerializeField] private Transform _visualParent;

        private Vector2Int _gridPos;

        // Prefab-mode instances (used when JellyPrefab is assigned)
        private GameObject[,] _jellyInstances = new GameObject[2, 2];

        // Sprite-mode fallback renderers
        private SpriteRenderer[,] _renderers = new SpriteRenderer[2, 2];

        // Set by JellyLauncher.Awake() before any blocks are spawned
        public static GameObject JellyPrefab;

        // Shared sprite cache: generated once per color using matcap shading
        private static Texture2D _matcapTex;
        private static bool _matcapLoaded;
        private static readonly Dictionary<string, Sprite> _jellySprites = new Dictionary<string, Sprite>();

        public Vector2Int GridPos { get => _gridPos; set => _gridPos = value; }

        private void Awake()
        {
            EnsureMatCap();
            if (_visualParent == null)
                _visualParent = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        // ── Matcap / sprite generation ────────────────────────────────────────────

        private static void EnsureMatCap()
        {
            if (_matcapLoaded) return;
            _matcapLoaded = true;

            Texture2D loaded = Resources.Load<Texture2D>("JellyMatCap");
            if (loaded != null)
            {
                try
                {
                    loaded.GetPixel(0, 0); // throws if not readable
                    _matcapTex = loaded;
                    return;
                }
                catch { /* fall through to procedural */ }
            }
            _matcapTex = BuildProceduralMatCap(128);
        }

        private static Texture2D BuildProceduralMatCap(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector3 lightDir = new Vector3(-0.55f, 0.75f, 1.0f).normalized;
            Vector3 viewDir  = Vector3.forward;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float u = (px + 0.5f) / size * 2f - 1f;
                    float v = (py + 0.5f) / size * 2f - 1f;
                    float lenSq = u * u + v * v;

                    float z = Mathf.Sqrt(Mathf.Max(0.001f, 1f - lenSq * 0.72f));
                    Vector3 n = new Vector3(u, v, z).normalized;

                    float diff = Mathf.Max(0f, Vector3.Dot(n, lightDir));
                    Vector3 h  = (lightDir + viewDir).normalized;
                    float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, h)), 20) * 0.85f;

                    // Keep minimum high (0.6) so jelly color stays vivid in dark areas
                    float bright = Mathf.Clamp01(0.60f + diff * 0.35f + spec);
                    tex.SetPixel(px, py, new Color(bright, bright, bright, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Sprite GetJellySprite(string colorId)
        {
            if (_jellySprites.TryGetValue(colorId, out Sprite cached)) return cached;

            EnsureMatCap();
            Color jelly = GetColorFromId(colorId);
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float u = (px + 0.5f) / size * 2f - 1f;
                    float v = (py + 0.5f) / size * 2f - 1f;
                    float lenSq = u * u + v * v;
                    float z = Mathf.Sqrt(Mathf.Max(0.001f, 1f - lenSq * 0.72f));
                    Vector3 n = new Vector3(u, v, z).normalized;

                    float mcU = n.x * 0.5f + 0.5f;
                    float mcV = n.y * 0.5f + 0.5f;
                    Color mc = _matcapTex.GetPixelBilinear(mcU, mcV);

                    // Map matcap [0..1] → shade factor [0.55..1.0] so dark areas stay vivid
                    float shade = 0.55f + mc.r * 0.45f;

                    // White specular on top (additive)
                    Vector3 h = new Vector3(-0.55f, 0.75f, 1.75f).normalized;
                    float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, h)), 18) * 0.55f;

                    Color final = new Color(
                        Mathf.Clamp01(jelly.r * shade + spec),
                        Mathf.Clamp01(jelly.g * shade + spec),
                        Mathf.Clamp01(jelly.b * shade + spec),
                        1f
                    );
                    tex.SetPixel(px, py, final);
                }
            }
            tex.Apply();

            // 64 PPU -> 1.0 world at scale 1; block parent=0.45 -> cell visual=0.45 world
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            _jellySprites[colorId] = sprite;
            return sprite;
        }

        // ── Public color lookup ────────────────────────────────────────────────────

        public static Color GetColorFromId(string id)
        {
            switch (id)
            {
                case "blue":   return new Color(0.20f, 0.55f, 1.00f);
                case "red":    return new Color(1.00f, 0.25f, 0.25f);
                case "yellow": return new Color(1.00f, 0.80f, 0.10f);
                case "green":  return new Color(0.20f, 0.82f, 0.30f);
                case "purple": return new Color(0.70f, 0.25f, 0.95f);
                case "cyan":   return new Color(0.10f, 0.80f, 0.90f);
                case "orange": return new Color(1.00f, 0.55f, 0.05f);
                case "pink":   return new Color(1.00f, 0.40f, 0.70f);
                default:       return Color.clear;
            }
        }

        // ── Init & visuals ─────────────────────────────────────────────────────────

        public void Init(string[,] colors)
        {
            cellColors = (string[,])colors.Clone();
            RefreshVisuals();
            PlayIdleBreathing();
        }

        public void RefreshVisuals()
        {
            if (JellyPrefab != null)
                RefreshWithPrefab();
            else
                RefreshWithSprites();
        }

        // ── Prefab-based visuals ───────────────────────────────────────────────────

        private void RefreshWithPrefab()
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (_jellyInstances[x, y] != null)
                    {
                        DOTween.Kill(_jellyInstances[x, y].transform);
                        Destroy(_jellyInstances[x, y]);
                        _jellyInstances[x, y] = null;
                    }

                    string colorId = cellColors[x, y];
                    if (string.IsNullOrEmpty(colorId)) continue;

                    GameObject jellyGo = Instantiate(JellyPrefab, _visualParent);
                    // Jelly mesh is 1.0 world at scale(1,1,1), centered at pivot.
                    // Scale to 0.5 so each sub-cell = 0.5×0.5, placed at ±0.25 to tile a 1×1 cell.
                    float cx = x == 0 ? -0.25f : 0.25f;
                    float cy = y == 0 ? -0.25f : 0.25f;
                    jellyGo.transform.localPosition = new Vector3(cx, cy, 0f);
                    jellyGo.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
                    jellyGo.transform.localRotation = Quaternion.identity;

                    ColorJellyInstance(jellyGo, GetColorFromId(colorId));
                    _jellyInstances[x, y] = jellyGo;
                }
            }
        }

        private static Shader _jellyShader;

        private static void ColorJellyInstance(GameObject go, Color color)
        {
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            // TCP2 shader from APK is a dummy stub — swap in a real URP/Standard shader
            if (_jellyShader == null)
                _jellyShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

            Material mat = new Material(_jellyShader);
            // Both URP Lit (_BaseColor) and Standard (_Color) — set both
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.70f);
            mat.SetFloat("_Metallic", 0.0f);
            smr.material = mat;
        }

        // ── Sprite-based visuals (fallback) ───────────────────────────────────────

        private void RefreshWithSprites()
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    string colorId  = cellColors[x, y];
                    string childName = $"Cell_{x}_{y}";
                    Transform childTrans = _visualParent.Find(childName);

                    if (childTrans == null)
                    {
                        GameObject childGo = new GameObject(childName);
                        childGo.transform.SetParent(_visualParent);
                        childGo.transform.localPosition = new Vector3(x == 0 ? -0.5f : 0.5f,
                                                                       y == 0 ? -0.5f : 0.5f, 0f);
                        childGo.transform.localScale    = Vector3.one;
                        childGo.transform.localRotation = Quaternion.identity;

                        SpriteRenderer sr = childGo.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = 10;
                        _renderers[x, y] = sr;
                        childTrans = childGo.transform;
                    }
                    else
                    {
                        _renderers[x, y] = childTrans.GetComponent<SpriteRenderer>();
                    }

                    if (string.IsNullOrEmpty(colorId))
                        childTrans.gameObject.SetActive(false);
                    else
                    {
                        childTrans.gameObject.SetActive(true);
                        if (_renderers[x, y] != null)
                            _renderers[x, y].sprite = GetJellySprite(colorId);
                    }
                }
            }
        }

        // ── Elimination / morph ────────────────────────────────────────────────────

        public void ApplyEliminations(HashSet<string> colorsToEliminate)
        {
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    if (cellColors[x, y] != null && colorsToEliminate.Contains(cellColors[x, y]))
                        cellColors[x, y] = null;

            HashSet<string> remaining = new HashSet<string>();
            string firstColor = null;
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    if (cellColors[x, y] != null)
                    {
                        remaining.Add(cellColors[x, y]);
                        firstColor ??= cellColors[x, y];
                    }

            if (remaining.Count == 0)
            {
                // empty - will be destroyed
            }
            else if (remaining.Count == 1)
            {
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        cellColors[x, y] = firstColor;
            }
            else if (remaining.Count == 2)
            {
                // Exactly 2 colors: guarantee a perfect 2+2 split.
                // Flood-fill is NOT used here because it can propagate a freshly-filled
                // cell in the same pass, turning 2+2 into 3+1.
                string[] cols = new string[2];
                int ci = 0;
                foreach (string c in remaining) cols[ci++] = c;
                string c0 = cols[0], c1 = cols[1];

                int c0Count = 0;
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        if (cellColors[x, y] == c0) c0Count++;

                int c0Needed = 2 - c0Count; // nulls to fill with c0; rest get c1
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        if (cellColors[x, y] == null)
                        {
                            if (c0Needed > 0) { cellColors[x, y] = c0; c0Needed--; }
                            else cellColors[x, y] = c1;
                        }

                // Diagonal detection: [0,0]==[1,1] and [0,1]==[1,0] means checkerboard.
                // Rearrange to a clean vertical split so same-color cells are always adjacent.
                if (cellColors[0,0] == cellColors[1,1] &&
                    cellColors[0,1] == cellColors[1,0] &&
                    cellColors[0,0] != cellColors[0,1])
                {
                    string left = cellColors[0,0]; string right = cellColors[0,1];
                    cellColors[0,0] = left;  cellColors[0,1] = left;
                    cellColors[1,0] = right; cellColors[1,1] = right;
                }
            }
            else
            {
                // 3+ colors: flood-fill (each null borrows from the nearest non-null neighbor)
                bool filled = true;
                while (filled)
                {
                    filled = false;
                    for (int x = 0; x < 2; x++)
                        for (int y = 0; y < 2; y++)
                            if (cellColors[x, y] == null)
                            {
                                string adj = FindAdjacentColor(x, y);
                                if (adj != null) { cellColors[x, y] = adj; filled = true; }
                            }
                }
            }

            RefreshVisuals();
        }

        private string FindAdjacentColor(int x, int y)
        {
            if (cellColors[1 - x, y] != null)     return cellColors[1 - x, y];
            if (cellColors[x, 1 - y] != null)     return cellColors[x, 1 - y];
            if (cellColors[1 - x, 1 - y] != null) return cellColors[1 - x, 1 - y];
            return null;
        }

        public bool IsEmpty()
        {
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    if (!string.IsNullOrEmpty(cellColors[x, y])) return false;
            return true;
        }

        // ── Animations ─────────────────────────────────────────────────────────────

        public void PlayIdleBreathing()
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            _visualParent.localScale = Vector3.one;
            _visualParent.DOScale(new Vector3(1.04f, 1.04f, 1f), 1.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void ApplyDragStretch(Vector3 velocity)
        {
            // No drag deformation - block follows finger cleanly
        }

        public void ResetRotationAndScale()
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            _visualParent.rotation   = Quaternion.identity;
            _visualParent.localScale = Vector3.one;
        }

        public void PlayLandingBounce()
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            ResetRotationAndScale();
            _visualParent.DOScale(new Vector3(1.22f, 0.78f, 1f), 0.10f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    _visualParent.DOScale(new Vector3(0.92f, 1.08f, 1f), 0.07f)
                        .SetEase(Ease.InOutQuad)
                        .OnComplete(() =>
                            _visualParent.DOScale(Vector3.one, 0.06f).SetEase(Ease.InQuad)));
        }

        public void PlayMergeAndCollect(Vector3 targetPos, System.Action onComplete)
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            DOTween.Sequence()
                .Append(_visualParent.DOScale(new Vector3(1.3f, 1.3f, 1f), 0.12f).SetEase(Ease.OutBack))
                .Append(transform.DOMove(targetPos, 0.45f).SetEase(Ease.InBack))
                .Join(transform.DOScale(Vector3.zero, 0.45f).SetEase(Ease.InBack))
                .OnComplete(() => { onComplete?.Invoke(); Destroy(gameObject); });
        }

        public Transform GetSubCellTransform(int x, int y)
        {
            if (JellyPrefab != null)
                return _jellyInstances[x, y] != null ? _jellyInstances[x, y].transform : null;
            return _renderers[x, y] != null ? _renderers[x, y].transform : null;
        }

        public UniTask FlashSubCellsAsync(HashSet<string> colorsToEliminate)
        {
            var tasks = new List<UniTask>();
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            {
                if (!colorsToEliminate.Contains(cellColors[x, y])) continue;
                Transform t = GetSubCellTransform(x, y);
                if (t == null) continue;
                var tcs = new UniTaskCompletionSource();
                t.DOKill();
                // Subtle pulse: +8% of current scale, returns to base — never covers adjacent cells
                t.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.12f, 1, 0f)
                    .OnComplete(() => tcs.TrySetResult());
                tasks.Add(tcs.Task);
            }
            return tasks.Count > 0 ? UniTask.WhenAll(tasks) : UniTask.CompletedTask;
        }

        public UniTask PopSubCellsAsync(HashSet<string> colorsToEliminate)
        {
            var tasks = new List<UniTask>();
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            {
                if (!colorsToEliminate.Contains(cellColors[x, y])) continue;
                Transform t = GetSubCellTransform(x, y);
                if (t == null) continue;
                var tcs = new UniTaskCompletionSource();
                t.DOKill();
                t.DOScale(Vector3.zero, 0.14f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => tcs.TrySetResult());
                tasks.Add(tcs.Task);
            }
            return UniTask.WhenAll(tasks);
        }

        public void PlayRefillBounce()
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            _visualParent.localScale = Vector3.one;
            _visualParent.DOPunchScale(new Vector3(0.22f, 0.22f, 0f), 0.32f, 5, 0.45f);
        }

        public void PlayPoof(System.Action onComplete)
        {
            if (_visualParent == null) return;
            _visualParent.DOKill(true);
            _visualParent.DOScale(Vector3.zero, 0.18f)
                .SetEase(Ease.InBack)
                .OnComplete(() => { onComplete?.Invoke(); Destroy(gameObject); });
        }
    }
}
