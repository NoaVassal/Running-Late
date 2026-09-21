using UnityEngine;

namespace RunningLate
{
    public class ProjectPointCollectible : MonoBehaviour
    {
        private ScoreSystem scoreSystem;
        private bool collected = false;

        private void Awake()
        {
            scoreSystem =
                Object.FindFirstObjectByType<ScoreSystem>();

            if (scoreSystem == null)
            {
                Debug.LogError(
                    "ProjectPointCollectible: ScoreSystem was not found in the scene."
                );
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (scoreSystem == null)
                return;

            collected = true;

            scoreSystem.AddProjectPoints();

            Debug.Log("Project Point collected!");

            // Temporary behaviour.
            // Later this will be handled by the Object Pool.
            gameObject.SetActive(false);
        }
    }
}
