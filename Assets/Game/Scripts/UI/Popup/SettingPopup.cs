using UnityEngine;
using UnityEngine.UI;
using NFramework;

namespace Game.UI
{
    public class SettingPopup : BaseUIView
    {
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Button _closeButton;

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => CloseSelf());

            // Listen to value changes
            if (_musicVolumeSlider != null)
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (_musicToggle != null)
                _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

            if (_vibrationToggle != null)
                _vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (SoundManager.IsSingletonAlive)
            {
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.value = SoundManager.I.MusicVolume;

                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.value = SoundManager.I.SFXVolume;

                if (_musicToggle != null)
                    _musicToggle.isOn = SoundManager.I.MusicStatus;

                if (_sfxToggle != null)
                    _sfxToggle.isOn = SoundManager.I.SFXStatus;
            }

            if (VibrationManager.IsSingletonAlive)
            {
                if (_vibrationToggle != null)
                    _vibrationToggle.isOn = VibrationManager.I.Status;
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (SoundManager.IsSingletonAlive)
                SoundManager.I.MusicVolume = value;
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (SoundManager.IsSingletonAlive)
                SoundManager.I.SFXVolume = value;
        }

        private void OnMusicToggleChanged(bool value)
        {
            if (SoundManager.IsSingletonAlive)
                SoundManager.I.MusicStatus = value;
        }

        private void OnSfxToggleChanged(bool value)
        {
            if (SoundManager.IsSingletonAlive)
                SoundManager.I.SFXStatus = value;
        }

        private void OnVibrationToggleChanged(bool value)
        {
            if (VibrationManager.IsSingletonAlive)
                VibrationManager.I.Status = value;
        }
    }
}
