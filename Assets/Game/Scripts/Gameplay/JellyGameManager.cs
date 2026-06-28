using System.Collections.Generic;
using UnityEngine;
using NFramework;
using Game.Manager;
using Game.Data;
using Game;

namespace Game.Gameplay
{
    public class JellyGameManager : SingletonMono<JellyGameManager>
    {
        [Header("Grid References")]
        [SerializeField] private JellyGrid     _grid;
        [SerializeField] private JellyLauncher _launcher;

        // ── Goal state ─────────────────────────────────────────────────────────────
        private readonly Dictionary<string, int> _goals     = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _collected = new Dictionary<string, int>();

        // Chặn thắng/thua kích hoạt nhiều lần trong 1 level (nước thắng dọn nhiều cặp → fire nhiều lần
        // → CompleteLevel tăng index nhiều lần → NHẢY BẬC level).
        private bool _resolved;

        public static event System.Action OnGoalsUpdated;

        // ── Public API for goals ───────────────────────────────────────────────────

        public IEnumerable<string> GetGoalColors() => _goals.Keys;

        public int GetGoal(string colorId)
        {
            _goals.TryGetValue(colorId, out int v);
            return v;
        }

        public int GetCollected(string colorId)
        {
            _collected.TryGetValue(colorId, out int v);
            return v;
        }

        public int GetRemaining(string colorId) =>
            Mathf.Max(0, GetGoal(colorId) - GetCollected(colorId));

        public bool AllGoalsMet()
        {
            if (_goals.Count == 0) return false;
            foreach (var kvp in _goals)
                if (kvp.Value - GetCollected(kvp.Key) > 0) return false;
            return true;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_grid != null)
            {
                _grid.OnJelliesCollected += HandleJelliesCollected;
                _grid.OnMoveCompleted    += HandleMoveCompleted;
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
                _grid.OnMoveCompleted    -= HandleMoveCompleted;
            }
            GameplayManager.OnGameplayStateChanged -= HandleGameplayStateChanged;
        }

        // ── Internal ───────────────────────────────────────────────────────────────

        private void ResetGoals()
        {
            _goals.Clear();
            _collected.Clear();
            _resolved = false; // level mới → cho phép thắng/thua kích hoạt lại

            // Load targets from current level
            if (LevelManager.IsSingletonAlive && LevelManager.I.CurrentLevel != null)
            {
                foreach (var t in LevelManager.I.CurrentLevel.targets)
                    if (!string.IsNullOrEmpty(t.colorId) && t.count > 0)
                        _goals[t.colorId] = t.count;
            }

            OnGoalsUpdated?.Invoke();
        }

        private void HandleGameplayStateChanged(EGameplayState state)
        {
            if (state == EGameplayState.BEGIN)
            {
                ResetGoals();
                if (_grid != null)     _grid.SpawnInitialJellies();
                if (_launcher != null) _launcher.ResetLauncher();
            }
        }

        private void HandleJelliesCollected(string colorId, int count)
        {
            if (_collected.ContainsKey(colorId)) _collected[colorId] += count;
            else                                  _collected[colorId]  = count;

            if (UserData.IsSingletonAlive)
            {
                UserData.I.Coin      += count;
                UserData.I.BestScore += count * 10;
            }

            OnGoalsUpdated?.Invoke();

            if (SoundManager.IsSingletonAlive)
                SoundManager.I.PlaySFXResource("Sfx/SFX_Collect");

            if (AllGoalsMet()) TriggerWin();
        }

        private void HandleMoveCompleted()
        {
            if (_grid == null || _launcher == null) return;

            bool gridFull = _grid.GetOccupiedCount() >= _grid.GetTotalSlots();
            if (gridFull) TriggerLose();
        }

        private void TriggerWin()
        {
            if (_resolved) return;   // chỉ thắng 1 lần/level → CompleteLevel chỉ +1
            _resolved = true;
            Debug.Log("[JellyGameManager] Victory!");
            GameSFX.PlayWin();
            if (LevelManager.IsSingletonAlive) LevelManager.I.CompleteLevel();
            if (GameplayManager.IsSingletonAlive) GameplayManager.I.EnterResult();
        }

        private void TriggerLose()
        {
            if (_resolved) return;
            _resolved = true;
            Debug.Log("[JellyGameManager] Defeat!");
            GameSFX.PlayLose();
            if (GameplayManager.IsSingletonAlive) GameplayManager.I.EnterLose();
        }

        public void ForceReset()
        {
            ResetGoals();
            if (_grid != null)     _grid.SpawnInitialJellies();
            if (_launcher != null) _launcher.ResetLauncher();
        }
    }
}
