using UnityEngine;
using UnityEngine.UI;
using NFramework;
using Game.Manager;

namespace Game.UI
{
    public class GamePlayMenu : BaseUIView
    {
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _retryButton;

        private void Start()
        {
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);
        }

        private void OnSettingsClicked()
        {
            GameSFX.PlayClick();
            if (UIManager.IsSingletonAlive)
                UIManager.I.Open("SettingPopup");
        }

        private void OnHomeClicked()
        {
            GameSFX.PlayClick();
            if (!UIManager.IsSingletonAlive) return;
            var popup = UIManager.I.Open<ConfirmPopup>("ConfirmPopup");
            popup?.Setup("Go to\nHome Menu?", () =>
            {
                if (GameManager.IsSingletonAlive)
                    GameManager.I.EnterHome();
            });
        }

        private void OnRetryClicked()
        {
            GameSFX.PlayClick();
            if (!UIManager.IsSingletonAlive) return;
            var popup = UIManager.I.Open<ConfirmPopup>("ConfirmPopup");
            popup?.Setup("Retry\nthis level?", () =>
            {
                if (GameManager.IsSingletonAlive)
                    GameManager.I.EnterReset();
            });
        }
    }
}
