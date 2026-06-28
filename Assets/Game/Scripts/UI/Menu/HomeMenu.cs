using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.Manager;
using Game;

namespace Game.UI
{
    public class HomeMenu : BaseUIView
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private GameObject _hardBadge;

        private void Start()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_shopButton != null)
                _shopButton.onClick.AddListener(OnShopClicked);

            GameManager.OnGameStateChanged += OnGameStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnEnable()
        {
            Refresh();
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Refresh();
        }

        private void OnGameStateChanged(EGameState state)
        {
            if (state == EGameState.HOME) Refresh();
        }

        private void Refresh()
        {
            // LevelManager nằm ở Game.unity (additive) → chỉ alive khi đang INGAME.
            // Khi ở HOME, đọc trực tiếp từ PlayerPrefs (CompleteLevel() luôn save đồng bộ).
            int index = LevelManager.IsSingletonAlive
                ? LevelManager.I.CurrentLevelIndex
                : UnityEngine.PlayerPrefs.GetInt("CurrentLevelIndex", 0);

            if (_levelText != null)
                _levelText.text = (index + 1).ToString();

            if (_hardBadge != null)
            {
                bool isHard = LevelManager.IsSingletonAlive
                    && LevelManager.I.CurrentLevel != null
                    && LevelManager.I.CurrentLevel.isHard;
                _hardBadge.SetActive(isHard);
            }
        }

        private void OnPlayClicked()
        {
            GameSFX.PlayClick();
            if (GameManager.IsSingletonAlive)
                GameManager.I.EnterInGame();
        }

        private void OnSettingsClicked()
        {
            GameSFX.PlayClick();
            if (UIManager.IsSingletonAlive)
                UIManager.I.Open("SettingPopup");
        }

        private void OnShopClicked()
        {
            Debug.Log("[HomeMenu] Shop Button Clicked!");
        }
    }
}
