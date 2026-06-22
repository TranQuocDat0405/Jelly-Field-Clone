using System.Collections.Generic;
using UnityEngine;
using NFramework;
using Game.Manager;
using Game.Data;

namespace Game.Gameplay
{
    public class JellyGameManager : SingletonMono<JellyGameManager>
    {
        [Header("Grid References")]
        [SerializeField] private JellyGrid _grid;
        [SerializeField] private JellyLauncher _launcher;

        [Header("Goals")]
        public int blueGoal = 10;
        public int redGoal = 10;
        public int yellowGoal = 10;
        public int greenGoal = 5;
        public int purpleGoal = 5;

        private int _blueCollected = 0;
        private int _redCollected = 0;
        private int _yellowCollected = 0;
        private int _greenCollected = 0;
        private int _purpleCollected = 0;

        public int BlueRemaining => Mathf.Max(0, blueGoal - _blueCollected);
        public int RedRemaining => Mathf.Max(0, redGoal - _redCollected);
        public int YellowRemaining => Mathf.Max(0, yellowGoal - _yellowCollected);
        public int GreenRemaining => Mathf.Max(0, greenGoal - _greenCollected);
        public int PurpleRemaining => Mathf.Max(0, purpleGoal - _purpleCollected);

        public static event System.Action OnGoalsUpdated;

        private void Start()
        {
            if (_grid != null)
            {
                _grid.OnJelliesCollected += HandleJelliesCollected;
                _grid.OnMoveCompleted += HandleMoveCompleted;
            }

            GameplayManager.OnGameplayStateChanged += HandleGameplayStateChanged;
            ResetGoals();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_grid != null)
            {
                _grid.OnJelliesCollected -= HandleJelliesCollected;
                _grid.OnMoveCompleted -= HandleMoveCompleted;
            }
            GameplayManager.OnGameplayStateChanged -= HandleGameplayStateChanged;
        }

        private void ResetGoals()
        {
            _blueCollected = 0;
            _redCollected = 0;
            _yellowCollected = 0;
            _greenCollected = 0;
            _purpleCollected = 0;
            OnGoalsUpdated?.Invoke();
        }

        private void HandleGameplayStateChanged(EGameplayState state)
        {
            if (state == EGameplayState.BEGIN)
            {
                ResetGoals();
                if (_grid != null) _grid.SpawnInitialJellies();
                if (_launcher != null) _launcher.ResetLauncher();
            }
        }

        private void HandleJelliesCollected(string colorId, int count)
        {
            if (colorId == "blue") _blueCollected += count;
            else if (colorId == "red") _redCollected += count;
            else if (colorId == "yellow") _yellowCollected += count;
            else if (colorId == "green") _greenCollected += count;
            else if (colorId == "purple") _purpleCollected += count;

            if (UserData.IsSingletonAlive)
            {
                UserData.I.Coin += count;
                UserData.I.BestScore += count * 10;
            }

            OnGoalsUpdated?.Invoke();

            if (SoundManager.IsSingletonAlive)
            {
                SoundManager.I.PlaySFXResource("Audio/Sfx/SFX_Collect");
            }

            // Win condition check: all goals collected
            if (BlueRemaining == 0 && RedRemaining == 0 && YellowRemaining == 0 &&
                GreenRemaining == 0 && PurpleRemaining == 0)
            {
                TriggerWin();
            }
        }

        private void HandleMoveCompleted()
        {
            // Lose condition check: board is full
            if (_grid != null && _launcher != null)
            {
                if (_grid.GetOccupiedCount() >= _grid.GetTotalSlots())
                {
                    TriggerLose();
                }
            }
        }

        private void TriggerWin()
        {
            Debug.Log("[JellyGameManager] level Victory!");
            if (GameplayManager.IsSingletonAlive)
            {
                GameplayManager.I.EnterResult();
            }
        }

        private void TriggerLose()
        {
            Debug.Log("[JellyGameManager] Level Defeat!");
            if (GameplayManager.IsSingletonAlive)
            {
                GameplayManager.I.EnterLose();
            }
        }

        public void ForceReset()
        {
            ResetGoals();
            if (_grid != null) _grid.SpawnInitialJellies();
            if (_launcher != null) _launcher.ResetLauncher();
        }
    }
}
