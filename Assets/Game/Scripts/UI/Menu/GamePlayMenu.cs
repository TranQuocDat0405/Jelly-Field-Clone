using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.Data;
using Game.Manager;

namespace Game.UI
{
    public class GamePlayMenu : BaseUIView
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _boosterButton;

        private void Start()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnPauseClicked);

            if (_boosterButton != null)
                _boosterButton.onClick.AddListener(OnBoosterClicked);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            
            // Subscribe to UserData events to update score and coin displays
            UserData.OnBestScoreChanged += UpdateScoreDisplay;
            UserData.OnCoinChanged += UpdateCoinDisplay;

            UpdateScoreDisplay();
            UpdateCoinDisplay();
        }

        public override void OnClose()
        {
            base.OnClose();
            
            // Unsubscribe to avoid memory leaks
            UserData.OnBestScoreChanged -= UpdateScoreDisplay;
            UserData.OnCoinChanged -= UpdateCoinDisplay;
        }

        private void UpdateScoreDisplay()
        {
            if (_scoreText != null && UserData.IsSingletonAlive)
            {
                _scoreText.text = $"Best Score: {UserData.I.BestScore}";
            }
        }

        private void UpdateCoinDisplay()
        {
            if (_coinText != null && UserData.IsSingletonAlive)
            {
                _coinText.text = $"Coins: {UserData.I.Coin}";
            }
        }

        private void OnPauseClicked()
        {
            if (UIManager.IsSingletonAlive)
            {
                UIManager.I.Open("SettingPopup");
            }
        }

        private void OnBoosterClicked()
        {
            Debug.Log("[GamePlayMenu] Booster Button Clicked!");
            if (UserData.IsSingletonAlive)
            {
                UserData.I.Coin += 5; // Example booster side effect
            }
        }
    }
}
