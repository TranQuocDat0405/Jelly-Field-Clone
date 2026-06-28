using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Game.Gameplay
{
    public class GoalCounterUI : MonoBehaviour
    {
        private class BadgeView
        {
            public GameObject        go;
            public TextMeshProUGUI   countText;
            public Image             bgImage;
            public string            colorId;
        }

        private readonly List<BadgeView> _badges = new List<BadgeView>();
        private static Sprite _circleSprite;

        private void Start()
        {
            JellyGameManager.OnGoalsUpdated += OnGoalsUpdated;
            if (JellyGameManager.IsSingletonAlive)
            {
                BuildBadges();
                RefreshCounts();
            }
        }

        private void OnDestroy()
        {
            JellyGameManager.OnGoalsUpdated -= OnGoalsUpdated;
        }

        private void OnGoalsUpdated()
        {
            if (!JellyGameManager.IsSingletonAlive) return;
            var gm = JellyGameManager.I;

            // Rebuild if the goal color set has changed (e.g. new level)
            bool needsRebuild = false;
            var goalColors = new HashSet<string>(gm.GetGoalColors());

            if (goalColors.Count != _badges.Count) { needsRebuild = true; }
            else foreach (var b in _badges) if (!goalColors.Contains(b.colorId)) { needsRebuild = true; break; }

            if (needsRebuild) BuildBadges();
            RefreshCounts();
        }

        private void BuildBadges()
        {
            if (!JellyGameManager.IsSingletonAlive) return;
            var gm = JellyGameManager.I;

            // Destroy old badges
            foreach (var b in _badges) if (b.go != null) Destroy(b.go);
            _badges.Clear();

            List<string> goalColors = new List<string>(gm.GetGoalColors());
            int n = goalColors.Count;
            if (n == 0) return;

            // Kích thước thích ứng: to hẳn cho level ít màu (3-4), tự thu lại để KHÔNG tràn khi
            // level có nhiều màu (Level_20 = 7) — hàng badge luôn vừa trong maxRowWidth.
            float gap          = 22f;
            float desiredSize  = 150f;   // cỡ tối đa mong muốn (level ít màu)
            float maxRowWidth  = 980f;   // bề rộng tối đa của cả hàng (canvas units, chừa lề)
            float badgeSize    = Mathf.Min(desiredSize, (maxRowWidth - (n - 1) * gap) / n);
            float totalW       = n * badgeSize + (n - 1) * gap;
            float startX       = -totalW / 2f + badgeSize / 2f;

            for (int i = 0; i < n; i++)
            {
                string colorId = goalColors[i];
                Color  color   = JellyBlock.GetColorFromId(colorId);
                Vector2 pos    = new Vector2(startX + i * (badgeSize + gap), 0f);
                _badges.Add(CreateBadge(colorId, color, pos, badgeSize));
            }
        }

        private BadgeView CreateBadge(string colorId, Color color, Vector2 anchoredPos, float size)
        {
            var go = new GameObject($"Badge_{colorId}");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta      = new Vector2(size, size);
            rt.anchoredPosition = anchoredPos;

            // Outer dark ring
            var ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            var ringRt = ring.AddComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero; ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero; ringRt.offsetMax = Vector2.zero;
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = GetCircleSprite();
            ringImg.color  = new Color(0f, 0f, 0f, 0.35f);

            // Colored fill (slightly inset)
            var fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(5f, 5f); fillRt.offsetMax = new Vector2(-5f, -5f);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = GetCircleSprite();
            fillImg.color  = color;

            // Count label
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment      = TextAlignmentOptions.Center;
            tmp.fontSize       = size * 0.46f;
            tmp.fontStyle      = FontStyles.Bold;
            tmp.color          = Color.white;
            tmp.text           = "?";
            tmp.enableAutoSizing = false;

            return new BadgeView { go = go, countText = tmp, bgImage = fillImg, colorId = colorId };
        }

        private void RefreshCounts()
        {
            if (!JellyGameManager.IsSingletonAlive) return;
            var gm = JellyGameManager.I;
            foreach (var b in _badges)
            {
                int rem = gm.GetRemaining(b.colorId);
                b.countText.text = rem > 0 ? rem.ToString() : "✓"; // ✓
                float a   = rem > 0 ? 1f : 0.45f;
                Color col = b.bgImage.color;
                b.bgImage.color = new Color(col.r, col.g, col.b, a);
            }
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float r = sz / 2f;
            var center = new Vector2(r, r);
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((r - d) / 1.5f)));
                }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
            return _circleSprite;
        }
    }
}
