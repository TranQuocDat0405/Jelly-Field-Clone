using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;

namespace Game.UI
{
    public class ConfirmPopup : BaseUIView
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _cancelButton;

        private Action _onYes;

        private void Start()
        {
            if (_yesButton != null)
                _yesButton.onClick.AddListener(OnYesClicked);
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        public void Setup(string message, Action onYes)
        {
            if (_messageText != null)
                _messageText.text = message;
            _onYes = onYes;
        }

        private void OnYesClicked()
        {
            GameSFX.PlayClick();
            CloseSelf();
            _onYes?.Invoke();
        }

        private void OnCancelClicked()
        {
            GameSFX.PlayClick();
            CloseSelf();
        }
    }
}
