using System;
using UnityEngine;

namespace RunningLate
{
    public class ScoreSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("Runtime Score")]
        [SerializeField] private int projectPoints = 0;

        public int ProjectPoints => projectPoints;

        // Final Grade is exactly equal to Project Points.
        public int FinalGrade => projectPoints;

        public event Action<int> OnProjectPointsChanged;

        private void Start()
        {
            ResetScore();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset += ResetScore;
            }

            if (config == null)
            {
                Debug.LogError(
                    "ScoreSystem: GameConfig is not assigned."
                );
            }
        }

        public void AddProjectPoints()
        {
            if (config == null)
                return;

            AddProjectPoints(config.projectPointValue);
        }

        public void AddProjectPoints(int amount)
        {
            if (amount <= 0)
                return;

            projectPoints += amount;

            Debug.Log(
                "Project Points: " +
                projectPoints +
                " | Final Grade: " +
                FinalGrade
            );

            OnProjectPointsChanged?.Invoke(projectPoints);
        }

        public void ResetScore()
        {
            projectPoints = 0;
            OnProjectPointsChanged?.Invoke(projectPoints);
            Debug.Log("Project Points reset to 0.");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -= ResetScore;
            }
        }
    }
}
