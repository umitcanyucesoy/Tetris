using UnityEngine;

namespace Game.Scripts.Game.Managers
{
    public class ScoreManager : MonoSingleton<ScoreManager>
    {
        public int currentScore;

        public void ResetScore()
        {
            currentScore = 0;
            UIManager.Instance.SetScore(currentScore);
        }

        public void AddScore(int rowsCleared,int level)
        {
            int score = rowsCleared switch
            {
                1 => 40,
                2 => 100,
                3 => 300,
                4 => 1200,
                _ => 0
            };

            if (score == 0) return;

            int lvl = level;
            currentScore += score * lvl;
            UIManager.Instance.SetScore(currentScore);

            int target = LevelManager.Instance.CurrentLevel.targetScore;
            if (currentScore >= target) GameManager.Instance.TargetScoreReached();
        }
        
        public void AddTSpinScore(int rowsCleared, int level)
        {
            int baseScore = rowsCleared switch
            {
                1 => 200, 
                2 => 400, 
                _ => 0     
            };

            if (baseScore == 0) return;

            currentScore += baseScore * level;
            UIManager.Instance.SetScore(currentScore);

            int target = LevelManager.Instance.CurrentLevel.targetScore;
            if (currentScore >= target) GameManager.Instance.TargetScoreReached();
        }
    }
}