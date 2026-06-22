using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string gameName = "Jelly Field Clone";
        public string gameVersion = "1.0.0";
    }
}
