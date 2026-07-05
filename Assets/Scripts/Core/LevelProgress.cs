using UnityEngine;

namespace BrokenAnchor.Core
{
    public static class LevelProgress
    {
        private const string HighestUnlockedLevelKey = "BrokenAnchor.HighestUnlockedLevel";

        public static int HighestUnlockedLevel
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1));
            private set
            {
                PlayerPrefs.SetInt(HighestUnlockedLevelKey, Mathf.Max(1, value));
                PlayerPrefs.Save();
            }
        }

        public static void MarkLevelCompleted(int completedLevelId, int maxLevelId)
        {
            if (completedLevelId < HighestUnlockedLevel)
            {
                return;
            }

            HighestUnlockedLevel = Mathf.Min(maxLevelId, completedLevelId + 1);
        }

        public static void UnlockAll(int maxLevelId)
        {
            HighestUnlockedLevel = Mathf.Max(1, maxLevelId);
        }
    }
}
