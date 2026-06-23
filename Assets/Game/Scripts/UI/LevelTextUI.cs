using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Manager;

namespace Game.UI
{
    public class LevelTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private void Start()
        {
            if (_label == null) Bootstrap();
            GameplayManager.OnGameplayStateChanged += OnStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            GameplayManager.OnGameplayStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(EGameplayState state)
        {
            if (state == EGameplayState.BEGIN || state == EGameplayState.PLAYING)
                Refresh();
        }

        private void Refresh()
        {
            if (_label == null) return;
            int lvl = LevelManager.IsSingletonAlive ? LevelManager.I.CurrentLevelIndex + 1 : 1;
            _label.text = $"Level {lvl}";
        }

        private void Bootstrap()
        {
            // Auto-create a World Space Canvas + TMP label centered above the grid
            var canvasGo = new GameObject("LevelTextCanvas");
            var rt = canvasGo.AddComponent<RectTransform>();
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvasGo.AddComponent<CanvasScaler>();

            // Position above goal counter (goal ~y=4.2; camera top=5.0; place at y=4.75 → visible)
            canvasGo.transform.position = new Vector3(0f, 4.75f, 0f);
            rt.sizeDelta = new Vector2(400f, 50f);

            var textGo = new GameObject("LevelLabel");
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 30;
            _label.fontStyle = FontStyles.Bold;
            _label.color = Color.white;
        }
    }
}
