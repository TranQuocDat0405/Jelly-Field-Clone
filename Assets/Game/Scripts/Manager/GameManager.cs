using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NFramework;
using Cysharp.Threading.Tasks;
using Game.Data;

namespace Game.Manager
{
    public enum EGameState
    {
        NONE,
        LOADING,
        FIRST,
        HOME,
        INGAME,
        RESET
    }

    public class GameManager : SingletonMono<GameManager>
    {
        [SerializeField] private GameConfig _config;

        public EGameState CurrentState { get; private set; } = EGameState.NONE;

        public static event Action<EGameState> OnGameStateChanged;

        public void ChangeState(EGameState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            HandleGameStateChanged(state).Forget();
            OnGameStateChanged?.Invoke(state);
        }

        private void Start()
        {
            ChangeState(EGameState.LOADING);
        }

        public void RegisterAndLoadSave()
        {
            if (SaveManager.IsSingletonAlive)
            {
                if (UserData.IsSingletonAlive)
                    SaveManager.I.RegisterSaveData(UserData.I);
                
                if (SoundManager.IsSingletonAlive)
                    SaveManager.I.RegisterSaveData(SoundManager.I);
                
                if (VibrationManager.IsSingletonAlive)
                    SaveManager.I.RegisterSaveData(VibrationManager.I);

                SaveManager.I.Load();
            }
            else
            {
                Debug.LogWarning("[GameManager] SaveManager is not alive, cannot register or load save.");
            }
        }

        private async UniTaskVoid HandleGameStateChanged(EGameState state)
        {
            switch (state)
            {
                case EGameState.LOADING:
                {
                    OpenLoadingPopup();
                    
                    // Allow everything to initialize before registering save data
                    await UniTask.Yield();
                    
                    RegisterAndLoadSave();
                    ChangeState(EGameState.FIRST);
                    break;
                }

                case EGameState.FIRST:
                {
                    // Transition to HOME after a short delay (hook ads/leaderboards here if needed)
                    float firstEndTime = Time.realtimeSinceStartup + 0.5f;
                    while (Time.realtimeSinceStartup < firstEndTime)
                        await UniTask.Yield();
                    ChangeState(EGameState.HOME);
                    break;
                }

                case EGameState.HOME:
                {
                    OpenLoadingPopup();
                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Close("GamePlayMenu");
                        UIManager.I.Close("ResultPopup");
                        UIManager.I.Close("WinPopup");
                        UIManager.I.Close("LosePopup");
                    }

                    // Unload additive scene "Game"
                    Scene gameScene = SceneManager.GetSceneByName("Game");
                    if (gameScene.isLoaded)
                    {
                        var unloadOp = SceneManager.UnloadSceneAsync("Game");
                        if (unloadOp != null)
                        {
                            await unloadOp.ToUniTask();
                        }
                    }

                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Open("HomeMenu");
                    }
                    await CloseLoadingPopupAsync();

                    // Play main BGM
                    if (SoundManager.IsSingletonAlive)
                    {
                        SoundManager.I.PlayMusicResource("Audio/Bgm/BGM_Main");
                    }
                    break;
                }

                case EGameState.INGAME:
                {
                    OpenLoadingPopup();
                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Close("HomeMenu");
                    }

                    // Load scene "Game" additively
                    var loadOp = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
                    if (loadOp != null)
                    {
                        await loadOp.ToUniTask();
                        Scene activeScene = SceneManager.GetSceneByName("Game");
                        if (activeScene.IsValid())
                        {
                            SceneManager.SetActiveScene(activeScene);
                        }
                    }

                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Open("GamePlayMenu");
                    }
                    await CloseLoadingPopupAsync();

                    // Play Ingame BGM
                    if (SoundManager.IsSingletonAlive)
                    {
                        SoundManager.I.PlayMusicResource("Audio/Bgm/BGM_Ingame");
                    }

                    // Enter Gameplay State BEGIN
                    if (GameplayManager.IsSingletonAlive)
                    {
                        GameplayManager.I.EnterBegin();
                    }
                    break;
                }

                case EGameState.RESET:
                {
                    OpenLoadingPopup();
                    if (UIManager.IsSingletonAlive)
                    {
                        UIManager.I.Close("GamePlayMenu");
                        UIManager.I.Close("ResultPopup");
                        UIManager.I.Close("WinPopup");
                        UIManager.I.Close("LosePopup");
                    }

                    // Unload scene "Game"
                    Scene gameScene = SceneManager.GetSceneByName("Game");
                    if (gameScene.isLoaded)
                    {
                        var unloadOp = SceneManager.UnloadSceneAsync("Game");
                        if (unloadOp != null)
                        {
                            await unloadOp.ToUniTask();
                        }
                    }

                    // Restart game
                    ChangeState(EGameState.INGAME);
                    break;
                }
            }
        }

        private void OpenLoadingPopup()
        {
            if (!UIManager.IsSingletonAlive) return;
            BaseUIView dummy;
            if (!UIManager.I.IsSpecificViewShown("LoadingPopup", out dummy))
                UIManager.I.Open("LoadingPopup");
        }

        private async UniTask CloseLoadingPopupAsync()
        {
            if (!UIManager.IsSingletonAlive) return;
            BaseUIView view;
            if (UIManager.I.IsSpecificViewShown("LoadingPopup", out view))
            {
                if (view is Game.UI.LoadingPopup lp)
                    await lp.WaitForAnimationAsync();
            }
            UIManager.I.CloseAll("LoadingPopup");
        }

        public void EnterInGame() => ChangeState(EGameState.INGAME);
        public void EnterHome() => ChangeState(EGameState.HOME);
        public void EnterReset() => ChangeState(EGameState.RESET);
    }
}
