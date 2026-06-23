using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.Manager;

namespace Game.UI
{
    public class LosePopup : BaseUIView
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        private void Start()
        {
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);
            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void OnOpen()
        {
            base.OnOpen();
        }

        private void OnRetryClicked()
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
