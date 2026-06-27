using UnityEngine;
using NFramework;
using Game.Data;

namespace Game.Manager
{
    public class LevelManager : SingletonMono<LevelManager>
    {
        [SerializeField] private LevelData[] _levels;

        public int CurrentLevelIndex { get; private set; }

        /// <summary>
        /// Level đang chơi. 20 level đầu đi theo thứ tự thiết kế; từ level 21 trở đi (ENDLESS) thì
        /// bốc ngẫu nhiên từ pool các level có sẵn — XÁC ĐỊNH theo index (cùng index luôn ra cùng level,
        /// ổn định khi retry/revive). UI số level vẫn tăng vô hạn (CurrentLevelIndex + 1).
        /// </summary>
        public LevelData CurrentLevel
        {
            get
            {
                if (_levels == null || _levels.Length == 0) return null;
                if (CurrentLevelIndex < _levels.Length) return _levels[CurrentLevelIndex];
                return EndlessLevelFor(CurrentLevelIndex);
            }
        }

        public int  TotalLevels => _levels != null ? _levels.Length : 0;
        public bool IsEndless   => _levels != null && CurrentLevelIndex >= _levels.Length;

        private void Start()
        {
            CurrentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
            if (CurrentLevelIndex < 0) CurrentLevelIndex = 0; // KHÔNG chặn trên → cho phép endless
        }

        // Bốc 1 level từ pool theo index (xác định). Tránh lặp ngay level liền trước nếu có thể.
        private LevelData EndlessLevelFor(int index)
        {
            int n = _levels.Length;
            if (n == 1) return _levels[0];
            var rng  = new System.Random(index * 9973 + 12345);
            int pick = rng.Next(n);
            // tránh trùng level của index ngay trước đó cho đỡ lặp liên tiếp
            int prevPick = PickIndex(index - 1, n);
            if (pick == prevPick) pick = (pick + 1) % n;
            return _levels[pick];
        }

        private int PickIndex(int index, int n)
        {
            if (index < _levels.Length) return index; // vùng authored
            var rng = new System.Random(index * 9973 + 12345);
            return rng.Next(n);
        }

        public void CompleteLevel()
        {
            CurrentLevelIndex++; // luôn +1 (kể cả endless) → UI số level tăng tiếp
            PlayerPrefs.SetInt("CurrentLevelIndex", CurrentLevelIndex);
            PlayerPrefs.Save();
        }

        public void SetLevel(int index)
        {
            CurrentLevelIndex = Mathf.Max(0, index);
            PlayerPrefs.SetInt("CurrentLevelIndex", CurrentLevelIndex);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            SetLevel(0);
        }
    }
}
