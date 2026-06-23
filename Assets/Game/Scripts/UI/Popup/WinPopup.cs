using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.Manager;

namespace Game.UI
{
    public class WinPopup : BaseUIView
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _homeButton;

        private void Start()
        {
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            if (_titleText != null)
            {
                // CompleteLevel already ran before this popup opened, so CurrentLevelIndex
                // is the next level's index. The completed level number = that index (1-based).
                int completedLvl = LevelManager.IsSingletonAlive ? LevelManager.I.CurrentLevelIndex : 1;
                _titleText.text = $"Level {completedLvl} Complete!";
            }
        }

        private void OnNextLevelClicked()
        {
            if (GameManager.IsSingletonAlive)
                GameManager.I.EnterReset();
        }

        private void OnHomeClicked()
        {
            if (GameplayManager.IsSingletonAlive)
                GameplayManager.I.BackHome();
        }
    }
}
