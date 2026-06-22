using System;
using UnityEngine;
using UnityEngine.UI;
using NFramework;
using Cysharp.Threading.Tasks;

namespace Game.UI
{
    public class LoadingPopup : BaseUIView
    {
        [SerializeField] private Image _loadingBarFill;
        [SerializeField] private Transform _spinner;
        [SerializeField] private float _spinSpeed = 200f;

        private Action _onCompleteCallback;

        private void Update()
        {
            if (_spinner != null)
            {
                _spinner.Rotate(Vector3.forward, -_spinSpeed * Time.deltaTime);
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (_loadingBarFill != null)
            {
                _loadingBarFill.fillAmount = Mathf.Clamp01(progress);
            }
        }

        public async UniTask SimulateLoadingAsync(float duration, Action onComplete = null)
        {
            _onCompleteCallback = onComplete;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetProgress(elapsed / duration);
                await UniTask.Yield();
            }
            SetProgress(1f);
            
            _onCompleteCallback?.Invoke();
            CloseSelf();
        }

        public override void OnClose()
        {
            base.OnClose();
            _onCompleteCallback = null;
        }
    }
}
