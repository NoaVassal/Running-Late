using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    public class TrainGapRandomSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment = 0.35f;

        [SerializeField]
        private float minLocalZ = -8f;

        [SerializeField]
        private float maxLocalZ = 10f;

        [SerializeField]
        private float minimumDistanceAhead = 36f;

        [SerializeField]
        private float minimumDistanceFromOtherObjects = 5f;

        [Header("Gap Size")]

        [SerializeField]
        private float gapWidth = 2f;

        [SerializeField]
        private float gapLength = 3f;

        [SerializeField]
        private float triggerHeight = 0.30f;

        [Header("Pool")]

        [SerializeField]
        private int poolSize = 5;

        // Our current lane positions.
        private const float LeftTrainX = -2.5f;
        private const float RightTrainX = 2.5f;

        // Top of the temporary train platforms.
        private const float TrainSurfaceY = 1.2f;

        private Transform player;

        private TrackSegment[] trackSegments;
        private float[] lastSegmentZ;

        private GameObject gapTemplate;
        private Material gapMaterial;

        private readonly List<GameObject> pool =
            new List<GameObject>();

        private readonly Dictionary<TrackSegment, GameObject>
            activeGapBySegment =
                new Dictionary<TrackSegment, GameObject>();

        private readonly HashSet<TrackSegment>
            pendingSegments =
                new HashSet<TrackSegment>();

        private void Awake()
        {
            player = FindPlayer();

            CreateGapMaterial();
            CreateGapTemplate();
            CreatePool();
        }

        private void Start()
        {
            trackSegments =
                Object.FindObjectsByType<TrackSegment>(
                    FindObjectsSortMode.None
                );

            if (trackSegments == null ||
                trackSegments.Length == 0)
            {
                Debug.LogError(
                    "TrainGapRandomSpawner: " +
                    "No TrackSegments were found."
                );

                enabled = false;
                return;
            }

            lastSegmentZ =
                new float[trackSegments.Length];

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform.position.z;
            }

            StartCoroutine(
                SpawnInitialGaps()
            );

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset +=
                    HandleRunReset;
            }
        }

        private void Update()
        {
            if (!enabled)
                return;

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrackSegment segment =
                    trackSegments[i];

                float currentZ =
                    segment.transform.position.z;

                bool recycled =
                    currentZ >
                    lastSegmentZ[i] + 5f;

                if (recycled)
                {
                    HandleSegmentRecycled(
                        segment
                    );
                }

                lastSegmentZ[i] =
                    currentZ;
            }
        }

        // --------------------------------------------------
        // PLAYER
        // --------------------------------------------------

        private Transform FindPlayer()
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (playerObject != null)
            {
                return playerObject.transform;
            }

            PlayerController controller =
                Object.FindFirstObjectByType<
                    PlayerController
                >();

            if (controller != null)
            {
                return controller.transform;
            }

            return null;
        }

        // --------------------------------------------------
        // TEMPORARY GAP VISUAL
        // --------------------------------------------------

        private void CreateGapMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    );
            }

            if (shader == null)
            {
                shader =
                    Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogWarning(
                    "TrainGapRandomSpawner: " +
                    "No suitable shader found."
                );

                return;
            }

            gapMaterial =
                new Material(shader);

            gapMaterial.name =
                "Runtime_TrainGap";

            gapMaterial.color =
                new Color(
                    0.01f,
                    0.01f,
                    0.01f,
                    1f
                );
        }

        private void CreateGapTemplate()
        {
            gapTemplate =
                new GameObject(
                    "TrainGap_Template"
                );

            gapTemplate.transform.SetParent(
                transform,
                false
            );

            // Black visual rectangle.
            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            visual.name =
                "GapVisual";

            visual.transform.SetParent(
                gapTemplate.transform,
                false
            );

            visual.transform.localPosition =
                new Vector3(
                    0f,
                    0.015f,
                    0f
                );

            visual.transform.localScale =
                new Vector3(
                    gapWidth,
                    0.03f,
                    gapLength
                );

            Collider visualCollider =
                visual.GetComponent<Collider>();

            if (visualCollider != null)
            {
                visualCollider.enabled = false;
            }

            Renderer renderer =
                visual.GetComponent<Renderer>();

            if (renderer != null &&
                gapMaterial != null)
            {
                renderer.sharedMaterial =
                    gapMaterial;
            }

            // Invisible lethal trigger.
            GameObject trigger =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            trigger.name =
                "GapTrigger";

            trigger.transform.SetParent(
                gapTemplate.transform,
                false
            );

            trigger.transform.localPosition =
                new Vector3(
                    0f,
                    triggerHeight / 2f,
                    0f
                );

            trigger.transform.localScale =
                new Vector3(
                    gapWidth,
                    triggerHeight,
                    gapLength
                );

            Renderer triggerRenderer =
                trigger.GetComponent<Renderer>();

            if (triggerRenderer != null)
            {
                triggerRenderer.enabled = false;
            }

            BoxCollider triggerCollider =
                trigger.GetComponent<BoxCollider>();

            triggerCollider.isTrigger = true;

            trigger.AddComponent<
                LethalObstacle
            >();

            gapTemplate.SetActive(
                false
            );
        }

        // --------------------------------------------------
        // POOL
        // --------------------------------------------------

        private void CreatePool()
        {
            for (int i = 0;
                 i < poolSize;
                 i++)
            {
                GameObject gap =
                    Instantiate(
                        gapTemplate,
                        transform
                    );

                gap.name =
                    "TrainGap_Pooled_" +
                    (i + 1);

                gap.SetActive(
                    false
                );

                pool.Add(
                    gap
                );
            }
        }

        private GameObject GetAvailableGap()
        {
            for (int i = 0;
                 i < pool.Count;
                 i++)
            {
                GameObject gap =
                    pool[i];

                if (!gap.activeSelf &&
                    !activeGapBySegment
                        .ContainsValue(gap))
                {
                    return gap;
                }
            }

            return null;
        }

        // --------------------------------------------------
        // SPAWNING
        // --------------------------------------------------

        private IEnumerator SpawnInitialGaps()
        {
            // Give collectibles and normal obstacles
            // time to spawn first.
            yield return null;
            yield return null;

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrySpawnGap(
                    trackSegments[i]
                );
            }
        }

        private void HandleSegmentRecycled(
            TrackSegment segment
        )
        {
            ReleaseGapFromSegment(
                segment
            );

            if (pendingSegments.Contains(
                    segment
                ))
            {
                return;
            }

            pendingSegments.Add(
                segment
            );

            StartCoroutine(
                SpawnAfterRecycle(
                    segment
                )
            );
        }

        private IEnumerator SpawnAfterRecycle(
            TrackSegment segment
        )
        {
            // Let Battery, Points and normal
            // obstacles choose their positions first.
            yield return null;
            yield return null;

            TrySpawnGap(
                segment
            );

            pendingSegments.Remove(
                segment
            );
        }

        private bool TrySpawnGap(
            TrackSegment segment
        )
        {
            if (Random.value >
                spawnChancePerSegment)
            {
                return false;
            }

            if (player == null)
            {
                player = FindPlayer();
            }

            if (player == null)
            {
                Debug.LogWarning(
                    "TrainGapRandomSpawner: " +
                    "Player was not found."
                );

                return false;
            }

            float safeMinLocalZ =
                CalculateSafeMinimumLocalZ(
                    segment
                );

            if (safeMinLocalZ >
                maxLocalZ)
            {
                return false;
            }

            GameObject gap =
                GetAvailableGap();

            if (gap == null)
            {
                return false;
            }

            const int maxAttempts = 12;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                // Only left or right train.
                bool useLeftTrain =
                    Random.value < 0.5f;

                float laneX =
                    useLeftTrain
                        ? LeftTrainX
                        : RightTrainX;

                float localZ =
                    Random.Range(
                        safeMinLocalZ,
                        maxLocalZ
                    );

                if (PositionIsBlocked(
                        segment,
                        laneX,
                        localZ
                    ))
                {
                    continue;
                }

                PlaceGap(
                    gap,
                    segment,
                    laneX,
                    localZ
                );

                activeGapBySegment[
                    segment
                ] = gap;

                return true;
            }

            return false;
        }

        private void PlaceGap(
            GameObject gap,
            TrackSegment segment,
            float laneX,
            float localZ
        )
        {
            gap.transform.SetParent(
                segment.transform,
                false
            );

            gap.transform.localPosition =
                new Vector3(
                    laneX,
                    TrainSurfaceY,
                    localZ
                );

            gap.transform.localRotation =
                Quaternion.identity;

            gap.transform.localScale =
                Vector3.one;

            gap.SetActive(
                true
            );

            Debug.Log(
                "Train gap spawned on " +
                (laneX < 0f
                    ? "LEFT TRAIN"
                    : "RIGHT TRAIN")
            );
        }

        private float CalculateSafeMinimumLocalZ(
            TrackSegment segment
        )
        {
            float minimumWorldZ =
                player.position.z +
                minimumDistanceAhead;

            Vector3 minimumWorldPosition =
                new Vector3(
                    segment.transform.position.x,
                    segment.transform.position.y,
                    minimumWorldZ
                );

            float requiredLocalZ =
                segment.transform
                    .InverseTransformPoint(
                        minimumWorldPosition
                    ).z;

            return Mathf.Max(
                minLocalZ,
                requiredLocalZ
            );
        }

        // --------------------------------------------------
        // AVOID OTHER OBJECTS
        // --------------------------------------------------

        private bool PositionIsBlocked(
            TrackSegment segment,
            float laneX,
            float localZ
        )
        {
            BatteryPickup[] batteries =
                segment.GetComponentsInChildren<
                    BatteryPickup
                >(false);

            for (int i = 0;
                 i < batteries.Length;
                 i++)
            {
                if (IsTooClose(
                        segment,
                        batteries[i].transform,
                        laneX,
                        localZ
                    ))
                {
                    return true;
                }
            }

            ProjectPointCollectible[] points =
                segment.GetComponentsInChildren<
                    ProjectPointCollectible
                >(false);

            for (int i = 0;
                 i < points.Length;
                 i++)
            {
                if (IsTooClose(
                        segment,
                        points[i].transform,
                        laneX,
                        localZ
                    ))
                {
                    return true;
                }
            }

            // Also keep away from the existing
            // lethal obstacles.
            LethalObstacle[] obstacles =
                segment.GetComponentsInChildren<
                    LethalObstacle
                >(false);

            for (int i = 0;
                 i < obstacles.Length;
                 i++)
            {
                if (IsTooClose(
                        segment,
                        obstacles[i].transform,
                        laneX,
                        localZ
                    ))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTooClose(
            TrackSegment segment,
            Transform other,
            float laneX,
            float localZ
        )
        {
            Vector3 localPosition =
                segment.transform
                    .InverseTransformPoint(
                        other.position
                    );

            bool sameLane =
                Mathf.Abs(
                    localPosition.x -
                    laneX
                ) < 0.75f;

            bool tooCloseInZ =
                Mathf.Abs(
                    localPosition.z -
                    localZ
                ) <
                minimumDistanceFromOtherObjects;

            return
                sameLane &&
                tooCloseInZ;
        }

        // --------------------------------------------------
        // RELEASE / RESET
        // --------------------------------------------------

        private void ReleaseGapFromSegment(
            TrackSegment segment
        )
        {
            if (!activeGapBySegment
                    .TryGetValue(
                        segment,
                        out GameObject gap
                    ))
            {
                return;
            }

            gap.SetActive(
                false
            );

            gap.transform.SetParent(
                transform,
                false
            );

            activeGapBySegment.Remove(
                segment
            );
        }

        private void ReleaseAllGaps()
        {
            List<TrackSegment> segments =
                new List<TrackSegment>(
                    activeGapBySegment.Keys
                );

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                ReleaseGapFromSegment(
                    segments[i]
                );
            }

            pendingSegments.Clear();
        }

        private void HandleRunReset()
        {
            StartCoroutine(
                ResetAfterTrackReset()
            );
        }

        private IEnumerator ResetAfterTrackReset()
        {
            ReleaseAllGaps();

            yield return null;
            yield return null;

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform.position.z;
            }

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrySpawnGap(
                    trackSegments[i]
                );
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -=
                    HandleRunReset;
            }

            if (gapMaterial != null)
            {
                Destroy(
                    gapMaterial
                );
            }
        }
    }
}