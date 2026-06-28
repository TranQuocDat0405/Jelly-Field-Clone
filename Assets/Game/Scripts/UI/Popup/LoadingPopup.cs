using UnityEngine;
using UnityEngine.UI;
using NFramework;
using Cysharp.Threading.Tasks;

namespace Game.UI
{
    public class LoadingPopup : BaseUIView
    {
        [SerializeField] private Image _loadingBarFill;
        [SerializeField] private float _fillDuration = 2f;

        private bool _animationDone;

        public override void OnOpen()
        {
            base.OnOpen();
            _animationDone = false;
            SetProgress(0f);
            AnimateAsync().Forget();
        }

        private async UniTaskVoid AnimateAsync()
        {
            float elapsed = 0f;
            while (elapsed < _fillDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(elapsed / _fillDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            SetProgress(1f);
            _animationDone = true;
        }

        public async UniTask WaitForAnimationAsync()
        {
            while (!_animationDone)
                await UniTask.Yield(PlayerLoopTiming.Update);
        }

        public void SetProgress(float progress)
        {
            if (_loadingBarFill != null)
                _loadingBarFill.fillAmount = Mathf.Clamp01(progress);
        }

        public override void OnClose()
        {
            base.OnClose();
            _animationDone = false;
        }
    }
}
