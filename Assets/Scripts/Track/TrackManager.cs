using UnityEngine;

namespace RunningLate
{
    public class TrackManager : MonoBehaviour
    {
        private const int SegmentCount = 3;

        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField]
        private TrackSegment[] trackSegments =
            new TrackSegment[SegmentCount];

        private Vector3 initialFirstSegmentPosition;
        private bool isInitialized;

        private void Awake()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            initialFirstSegmentPosition =
                trackSegments[0].transform.position;

            ResetSegmentPositions();
            isInitialized = true;
        }

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "TrackManager: GameManager was not found in the scene.",
                    this
                );
                return;
            }

            GameManager.Instance.OnRunReset += ResetSegmentPositions;
        }

        private void LateUpdate()
        {
            if (!isInitialized ||
                GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            float halfSegmentLength =
                config.trackSegmentLength * 0.5f;

            for (int i = 0; i < trackSegments.Length; i++)
            {
                Transform segmentTransform =
                    trackSegments[i].transform;

                float segmentFrontEdge =
                    segmentTransform.position.z + halfSegmentLength;

                if (segmentFrontEdge <= player.position.z)
                {
                    MoveSegmentToFront(segmentTransform);
                }
            }
        }

        private void MoveSegmentToFront(Transform segmentTransform)
        {
            float frontmostZ = trackSegments[0].transform.position.z;

            for (int i = 1; i < trackSegments.Length; i++)
            {
                frontmostZ = Mathf.Max(
                    frontmostZ,
                    trackSegments[i].transform.position.z
                );
            }

            Vector3 position = segmentTransform.position;
            position.z = frontmostZ + config.trackSegmentLength;
            segmentTransform.position = position;
        }

        private void ResetSegmentPositions()
        {
            if (trackSegments == null ||
                trackSegments.Length != SegmentCount)
            {
                return;
            }

            for (int i = 0; i < trackSegments.Length; i++)
            {
                if (trackSegments[i] == null)
                {
                    return;
                }

                trackSegments[i].transform.position =
                    initialFirstSegmentPosition +
                    Vector3.forward *
                    (config.trackSegmentLength * i);
            }
        }

        private bool ValidateSetup()
        {
            if (config == null)
            {
                Debug.LogError(
                    "TrackManager: GameConfig is not assigned.",
                    this
                );
                return false;
            }

            if (player == null)
            {
                Debug.LogError(
                    "TrackManager: Player is not assigned.",
                    this
                );
                return false;
            }

            if (trackSegments == null ||
                trackSegments.Length != SegmentCount)
            {
                Debug.LogError(
                    "TrackManager: Exactly 3 track segments are required.",
                    this
                );
                return false;
            }

            for (int i = 0; i < trackSegments.Length; i++)
            {
                if (trackSegments[i] == null)
                {
                    Debug.LogError(
                        "TrackManager: Every track segment must be assigned.",
                        this
                    );
                    return false;
                }

                for (int j = i + 1; j < trackSegments.Length; j++)
                {
                    if (trackSegments[i] == trackSegments[j])
                    {
                        Debug.LogError(
                            "TrackManager: Track segment references must be unique.",
                            this
                        );
                        return false;
                    }
                }
            }

            return true;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -= ResetSegmentPositions;
            }
        }
    }
}
