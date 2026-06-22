using UnityEngine;
using UnityEngine.UI;
using NFramework;
using Game.Manager;

namespace Game.UI
{
    public class HomeMenu : BaseUIView
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _shopButton;

        private void Start()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_shopButton != null)
                _shopButton.onClick.AddListener(OnShopClicked);
        }

        private void OnPlayClicked()
        {
            if (GameManager.IsSingletonAlive)
            {
                GameManager.I.EnterInGame();
            }
        }

        private void OnSettingsClicked()
        {
            if (UIManager.IsSingletonAlive)
            {
                UIManager.I.Open("SettingPopup");
            }
        }

        private void OnShopClicked()
        {
            Debug.Log("[HomeMenu] Shop Button Clicked!");
        }
    }
}
