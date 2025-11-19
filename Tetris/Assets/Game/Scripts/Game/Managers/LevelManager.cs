using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Game.Managers
{
    public class LevelManager : MonoSingleton<LevelManager>
    {
        [Serializable]
        public struct LevelData
        {
            public int level;
            public int targetScore;
        }

        public List<LevelData> levels = new();
        public int currentLevel = 0;
        public LevelData CurrentLevel => levels[currentLevel];

        public bool NextLevelIndex()
        {
            if (currentLevel >= levels.Count - 1) return false;
            currentLevel++;
            return true;
        }
    }
}