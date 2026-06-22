using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.Data;
using Game.Manager;

namespace Game.UI
{
    public class ResultPopup : BaseUIView
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _bestScoreText;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _homeButton;

        private void Start()
        {
            if (_replayButton != null)
                _replayButton.onClick.AddListener(OnReplayClicked);

            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            
            if (UserData.IsSingletonAlive)
            {
                _bestScoreText.text = $"Best Score: {UserData.I.BestScore}";
                _scoreText.text = $"Score: {UserData.I.BestScore}"; // Fallback to best score for demo
            }
        }

        private void OnReplayClicked()
        {
            if (GameManager.IsSingletonAlive)
            {
                GameManager.I.EnterReset();
            }
        }

        private void OnHomeClicked()
        {
            if (GameplayManager.IsSingletonAlive)
            {
                GameplayManager.I.BackHome();
            }
        }
    }
}
