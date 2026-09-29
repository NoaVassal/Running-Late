using UnityEngine;

namespace RunningLate
{
    public class TrackSegment : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    "TrackSegment: GameConfig is not assigned.",
                    this
                );

                enabled = false;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            Vector3 position = transform.position;
            position.z -= config.baseScrollSpeed * Time.deltaTime;
            transform.position = position;
        }
    }
}
