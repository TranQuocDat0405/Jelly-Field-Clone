using NFramework;

namespace Game
{
    public static class GameSFX
    {
        public static void PlayClick()       => Play("Sfx/SFX_Click");
        public static void PlayPickup()      => Play("Sfx/SFX_Pickup");
        public static void PlayPlace()       => Play("Sfx/SFX_Place");
        public static void PlayFailedPlace() => Play("Sfx/SFX_FailedPlace");
        public static void PlayMerge()       => Play("Sfx/SFX_Merge");
        public static void PlayWin()         => Play("Sfx/SFX_Win");
        public static void PlayLose()        => Play("Sfx/SFX_Lose");

        private static void Play(string path)
        {
            if (SoundManager.IsSingletonAlive)
                SoundManager.I.PlaySFXResource(path);
        }
    }
}
