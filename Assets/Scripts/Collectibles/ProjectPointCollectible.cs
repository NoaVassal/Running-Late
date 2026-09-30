using UnityEngine;

namespace RunningLate
{
    public class ProjectPointCollectible : MonoBehaviour
    {
        private ScoreSystem scoreSystem;
        private bool collected = false;

        private void Awake()
        {
            FindScoreSystem();
        }

        private void OnEnable()
        {
            // מאפשר למחזר את הנקודה שוב ושוב.
            collected = false;

            if (scoreSystem == null)
            {
                FindScoreSystem();
            }
        }

        private void FindScoreSystem()
        {
            scoreSystem =
                Object.FindFirstObjectByType<ScoreSystem>();

            if (scoreSystem == null)
            {
                Debug.LogError(
                    "ProjectPointCollectible: ScoreSystem was not found."
                );
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            if (scoreSystem == null)
                return;

            collected = true;

            scoreSystem.AddProjectPoints();

            Debug.Log(
                "Project Point collected! Grade: " +
                scoreSystem.FinalGrade
            );

            // מחזיר את האובייקט ל-Pool.
            gameObject.SetActive(false);
        }
    }
}