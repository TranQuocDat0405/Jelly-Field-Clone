using UnityEngine;
using UnityEngine.UI;
using NFramework;
using Game;

namespace Game.UI
{
    public class SettingPopup : BaseUIView
    {
        [SerializeField] private Toggle _soundsToggle;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Button _closeButton;

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
            if (_soundsToggle != null)
                _soundsToggle.onValueChanged.AddListener(OnSoundsToggleChanged);
            if (_vibrationToggle != null)
                _vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        }

        // OnEnable chạy synchronous khi SetActive(true) — trước cả Start và OnOpen
        private void OnEnable() => RefreshUI();

        public override void OnOpen()
        {
            base.OnOpen();
            RefreshUI();
        }

        private void RefreshUI()
        {
            // SetIsOnWithoutNotify đảm bảo không trigger callback dù Start chạy trước hay sau
            if (_soundsToggle != null && SoundManager.IsSingletonAlive)
                _soundsToggle.SetIsOnWithoutNotify(SoundManager.I.MusicStatus && SoundManager.I.SFXStatus);

            if (_vibrationToggle != null && VibrationManager.IsSingletonAlive)
                _vibrationToggle.SetIsOnWithoutNotify(VibrationManager.I.Status);
        }

        private void OnSoundsToggleChanged(bool value)
        {
            if (!SoundManager.IsSingletonAlive) return;
            SoundManager.I.MusicStatus = value;
            SoundManager.I.SFXStatus   = value;
        }

        private void OnVibrationToggleChanged(bool value)
        {
            if (VibrationManager.IsSingletonAlive)
                VibrationManager.I.Status = value;
        }

        private void OnCloseClicked()
        {
            GameSFX.PlayClick();
            CloseSelf();
        }
    }
}
