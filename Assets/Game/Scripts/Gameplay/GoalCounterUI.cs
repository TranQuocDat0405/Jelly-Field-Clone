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
            public GameObject go;
            public TextMeshProUGUI countText;
            public Image bgImage;
            public string colorId;
        }

        private readonly List<BadgeView> _badges = new List<BadgeView>();
        private JellyGameManager _gm;
        private static Sprite _circleSprite;

        private void Start()
        {
            _gm = JellyGameManager.IsSingletonAlive ? JellyGameManager.I : null;
            if (_gm == null) return;

            BuildBadges();
            JellyGameManager.OnGoalsUpdated += RefreshCounts;
            RefreshCounts();
        }

        private void OnDestroy()
        {
            JellyGameManager.OnGoalsUpdated -= RefreshCounts;
        }

        private void BuildBadges()
        {
            var defs = new (string id, Color col, int target)[]
            {
                ("blue",   new Color(0.20f, 0.55f, 1.00f), _gm.blueGoal),
                ("red",    new Color(1.00f, 0.25f, 0.25f), _gm.redGoal),
                ("yellow", new Color(1.00f, 0.80f, 0.10f), _gm.yellowGoal),
                ("green",  new Color(0.20f, 0.82f, 0.30f), _gm.greenGoal),
                ("purple", new Color(0.70f, 0.25f, 0.95f), _gm.purpleGoal),
            };

            var active = new List<(string, Color)>();
            foreach (var d in defs)
                if (d.target > 0) active.Add((d.id, d.col));

            int n = active.Count;
            if (n == 0) return;

            float badgeSize = 80f;
            float gap = 18f;
            float totalW = n * badgeSize + (n - 1) * gap;
            float startX = -totalW / 2f + badgeSize / 2f;

            for (int i = 0; i < n; i++)
            {
                var (id, col) = active[i];
                Vector2 pos = new Vector2(startX + i * (badgeSize + gap), 0f);
                _badges.Add(CreateBadge(id, col, pos, badgeSize));
            }
        }

        private BadgeView CreateBadge(string colorId, Color color, Vector2 anchoredPos, float size)
        {
            var go = new GameObject($"Badge_{colorId}");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = anchoredPos;

            // Outer dark ring
            var ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            var ringRt = ring.AddComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero;
            ringRt.offsetMax = Vector2.zero;
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = GetCircleSprite();
            ringImg.color = new Color(0f, 0f, 0f, 0.35f);

            // Colored fill (slightly inset)
            var fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(5f, 5f);
            fillRt.offsetMax = new Vector2(-5f, -5f);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = GetCircleSprite();
            fillImg.color = color;

            // Count label
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 38;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.text = "?";
            tmp.enableAutoSizing = false;

            return new BadgeView { go = go, countText = tmp, bgImage = fillImg, colorId = colorId };
        }

        private void RefreshCounts()
        {
            if (_gm == null) return;
            foreach (var b in _badges)
            {
                int rem = GetRemaining(b.colorId);
                b.countText.text = rem > 0 ? rem.ToString() : "✓";
                float a = rem > 0 ? 1f : 0.45f;
                var c = b.bgImage.color;
                b.bgImage.color = new Color(c.r, c.g, c.b, a);
            }
        }

        private int GetRemaining(string id)
        {
            switch (id)
            {
                case "blue":   return _gm.BlueRemaining;
                case "red":    return _gm.RedRemaining;
                case "yellow": return _gm.YellowRemaining;
                case "green":  return _gm.GreenRemaining;
                case "purple": return _gm.PurpleRemaining;
                default:       return 0;
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
                    float alpha = Mathf.Clamp01((r - d) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
            return _circleSprite;
        }
    }
}
