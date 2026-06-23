using UnityEngine;
using NFramework;
using Game.Data;

namespace Game.Manager
{
    public class LevelManager : SingletonMono<LevelManager>
    {
        [SerializeField] private LevelData[] _levels;

        public int        CurrentLevelIndex { get; private set; }
        public LevelData  CurrentLevel      => _levels != null && CurrentLevelIndex < _levels.Length
                                               ? _levels[CurrentLevelIndex] : null;
        public int        TotalLevels       => _levels != null ? _levels.Length : 0;

        private void Start()
        {
            CurrentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
            CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, Mathf.Max(0, TotalLevels - 1));
        }

        public void CompleteLevel()
        {
            if (CurrentLevelIndex < TotalLevels - 1)
                CurrentLevelIndex++;
            // If last level, stay on it (can be extended to loop or show credits)
            PlayerPrefs.SetInt("CurrentLevelIndex", CurrentLevelIndex);
            PlayerPrefs.Save();
        }

        public void SetLevel(int index)
        {
            CurrentLevelIndex = Mathf.Clamp(index, 0, Mathf.Max(0, TotalLevels - 1));
            PlayerPrefs.SetInt("CurrentLevelIndex", CurrentLevelIndex);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            SetLevel(0);
        }
    }
}
