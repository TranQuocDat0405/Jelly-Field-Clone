using System;
using UnityEngine;
using NFramework;
using Newtonsoft.Json;

namespace Game.Data
{
    public class UserData : SingletonMono<UserData>, ISaveable
    {
        [System.Serializable]
        public class SaveData
        {
            public int coin;
            public int gem;
            public int bestScore;
            public string name = "Player";
        }

        private SaveData _saveData = new SaveData();

        public static event Action OnCoinChanged;
        public static event Action OnGemChanged;
        public static event Action OnBestScoreChanged;
        public static event Action OnNameChanged;
        public static event Action OnDataLoaded;

        public bool DataChanged { get; set; }

        public string SaveKey => "UserData";

        public int Coin
        {
            get => _saveData.coin;
            set
            {
                if (_saveData.coin != value)
                {
                    _saveData.coin = value;
                    DataChanged = true;
                    OnCoinChanged?.Invoke();
                }
            }
        }

        public int Gem
        {
            get => _saveData.gem;
            set
            {
                if (_saveData.gem != value)
                {
                    _saveData.gem = value;
                    DataChanged = true;
                    OnGemChanged?.Invoke();
                }
            }
        }

        public int BestScore
        {
            get => _saveData.bestScore;
            set
            {
                if (_saveData.bestScore != value)
                {
                    _saveData.bestScore = value;
                    DataChanged = true;
                    OnBestScoreChanged?.Invoke();
                }
            }
        }

        public string Name
        {
            get => _saveData.name;
            set
            {
                if (_saveData.name != value)
                {
                    _saveData.name = value;
                    DataChanged = true;
                    OnNameChanged?.Invoke();
                }
            }
        }

        public object GetData()
        {
            return _saveData;
        }

        public void SetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                _saveData = new SaveData();
            }
            else
            {
                try
                {
                    _saveData = JsonConvert.DeserializeObject<SaveData>(data) ?? new SaveData();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserData] SetData error: {e.Message}");
                    _saveData = new SaveData();
                }
            }
        }

        public void OnAllDataLoaded()
        {
            OnDataLoaded?.Invoke();
        }
    }
}
