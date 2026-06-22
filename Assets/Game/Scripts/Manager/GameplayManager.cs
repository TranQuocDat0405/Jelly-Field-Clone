using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NFramework;
using Cysharp.Threading.Tasks;

namespace Game.Manager
{
    public enum EGameplayState
    {
        NONE,
        BEGIN,
        PLAYING,
        CHECK,
        LOSE,
        RESULT,
        REVIVE
    }

    public class GameplayManager : SingletonMono<GameplayManager>
    {
        public EGameplayState CurrentState { get; private set; } = EGameplayState.NONE;

        public static event Action<EGameplayState> OnGameplayStateChanged;

        public void ChangeState(EGameplayState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            HandleGameplayStateChanged(state).Forget();
            OnGameplayStateChanged?.Invoke(state);
        }

        private async UniTaskVoid HandleGameplayStateChanged(EGameplayState state)
        {
            switch (state)
            {
                case EGameplayState.NONE:
                    break;

                case EGameplayState.BEGIN:
                    // Perform start-of-level routines (e.g. countdowns)
                    Debug.Log("[GameplayManager] Level Begin");
                    await UniTask.Delay(TimeSpan.FromSeconds(1f)); // Wait 1 second (e.g. for "Ready? Go!")
                    ChangeState(EGameplayState.PLAYING);
                    break;

                case EGameplayState.PLAYING:
                    Debug.Log("[GameplayManager] Level Playing");
                    break;

                case EGameplayState.CHECK:
                    // Check gameplay winning/losing conditions
                    break;

                case EGameplayState.LOSE:
                    Debug.Log("[GameplayManager] Level Lose");
                    // Trigger Lose UI/Popup
                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Open("ResultPopup");
                    }
                    break;

                case EGameplayState.RESULT:
                    Debug.Log("[GameplayManager] Level Result");
                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Open("ResultPopup");
                    }
                    break;

                case EGameplayState.REVIVE:
                    Debug.Log("[GameplayManager] Level Reviving");
                    // Delay or yield to revive logic
                    await UniTask.Yield();
                    ChangeState(EGameplayState.PLAYING);
                    break;
            }
        }

        public void EnterBegin() => ChangeState(EGameplayState.BEGIN);
        public void EnterPlaying() => ChangeState(EGameplayState.PLAYING);
        public void EnterLose() => ChangeState(EGameplayState.LOSE);
        public void EnterResult() => ChangeState(EGameplayState.RESULT);

        public void BackHome()
        {
            if (GameManager.IsSingletonAlive)
            {
                GameManager.I.EnterHome();
            }
        }
    }
}
