using UnityEngine;

namespace RunningLate
{
    public class LethalObstacle : MonoBehaviour
    {
        public enum HazardType
        {
            Obstacle,
            Gap
        }

        [SerializeField]
        private HazardType hazardType =
            HazardType.Obstacle;

        private bool alreadyHit = false;

        public void SetHazardType(
            HazardType type
        )
        {
            hazardType = type;
        }

        private void Awake()
        {
            Collider obstacleCollider =
                GetComponent<Collider>();

            if (obstacleCollider != null)
            {
                obstacleCollider.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            alreadyHit = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (alreadyHit)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (GameManager.Instance == null)
                return;

            if (!GameManager.Instance.IsRunning)
                return;

            alreadyHit = true;

            string reason =
                hazardType ==
                HazardType.Gap
                    ? "Hit gap"
                    : "Hit obstacle";

            Debug.Log(
                reason == "Hit gap"
                    ? "TRAIN GAP HIT -> GAME OVER"
                    : "OBSTACLE HIT -> GAME OVER"
            );

            GameManager.Instance.TriggerGameOver(
                reason
            );
        }
    }
}