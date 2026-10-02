using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    [DefaultExecutionOrder(-190)]
    public class ProjectPointRandomSpawner : MonoBehaviour
    {
        // ==================================================
        // SPAWN SETTINGS
        // ==================================================

        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment =
            0.45f;

        [SerializeField]
        private float minLocalZ =
            -10f;

        [SerializeField]
        private float maxLocalZ =
            10f;

        [SerializeField]
        private float minimumDistanceAhead =
            30f;

        [Tooltip(
            "Minimum distance from another collectible " +
            "in the same lane."
        )]
        [SerializeField]
        private float minimumCollectibleDistance =
            3f;

        // ==================================================
        // PICKUP SETTINGS
        // ==================================================

        [Header("Pickup Settings")]

        [SerializeField]
        private float pickupScale =
            0.05f;

        [SerializeField]
        private float heightAboveSurface =
            1.05f;

        [SerializeField]
        private int poolSize =
            8;

        // ==================================================
        // SURFACE DETECTION
        // ==================================================

        [Header("Surface Detection")]

        [SerializeField]
        private float surfaceProbeHeight =
            8f;

        [SerializeField]
        private float surfaceProbeDistance =
            20f;

        // ==================================================
        // LANES
        // ==================================================

        private const float LeftLaneX =
            -2.5f;

        private const float CenterLaneX =
            0f;

        private const float RightLaneX =
            2.5f;

        // ==================================================
        // RUNTIME
        // ==================================================

        private ProjectPointCollectible template;

        private TrackSegment[] trackSegments;

        private float[] lastSegmentZ;

        private Transform player;

        private int groundMask;

        private readonly List<GameObject> pool =
            new List<GameObject>();

        private readonly Dictionary<
            TrackSegment,
            GameObject
        > activePointBySegment =
            new Dictionary<
                TrackSegment,
                GameObject
            >();

        // ==================================================
        // AWAKE
        // ==================================================

        private void Awake()
        {
            groundMask =
                LayerMask.GetMask(
                    "Ground"
                );

            if (groundMask == 0)
            {
                Debug.LogError(
                    "ProjectPointRandomSpawner: " +
                    "Ground layer was not found.",
                    this
                );

                enabled =
                    false;

                return;
            }

            player =
                FindPlayer();

            template =
                Object.FindFirstObjectByType<
                    ProjectPointCollectible
                >();

            if (template == null)
            {
                Debug.LogError(
                    "ProjectPointRandomSpawner: " +
                    "No ProjectPointCollectible " +
                    "was found in the scene.",
                    this
                );

                enabled =
                    false;

                return;
            }

            template.transform.SetParent(
                transform
            );

            PreparePoint(
                template.gameObject
            );

            template.gameObject.SetActive(
                false
            );
        }

        // ==================================================
        // START
        // ==================================================

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            if (player == null)
            {
                player =
                    FindPlayer();
            }

            trackSegments =
                Object.FindObjectsByType<
                    TrackSegment
                >(
                    FindObjectsSortMode.None
                );

            if (trackSegments == null ||
                trackSegments.Length == 0)
            {
                Debug.LogError(
                    "ProjectPointRandomSpawner: " +
                    "No TrackSegments were found.",
                    this
                );

                enabled =
                    false;

                return;
            }

            lastSegmentZ =
                new float[
                    trackSegments.Length
                ];

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform
                        .position
                        .z;
            }

            CreatePool();

            SpawnInitialPoints();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset +=
                    HandleRunReset;
            }
        }

        // ==================================================
        // UPDATE
        // ==================================================

        private void Update()
        {
            if (!enabled ||
                trackSegments == null)
            {
                return;
            }

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

                if (segment == null)
                {
                    continue;
                }

                float currentZ =
                    segment.transform.position.z;

                bool wasRecycled =
                    currentZ >
                    lastSegmentZ[i] +
                    5f;

                if (wasRecycled)
                {
                    RefreshSegment(
                        segment
                    );
                }

                lastSegmentZ[i] =
                    currentZ;
            }
        }

        // ==================================================
        // FIND PLAYER
        // ==================================================

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

        // ==================================================
        // POOL
        // ==================================================

        private void CreatePool()
        {
            for (int i = 0;
                 i < poolSize;
                 i++)
            {
                GameObject point =
                    Instantiate(
                        template.gameObject,
                        transform
                    );

                point.name =
                    "ProjectPoint_Pooled_" +
                    (
                        i +
                        1
                    );

                PreparePoint(
                    point
                );

                point.SetActive(
                    false
                );

                pool.Add(
                    point
                );
            }
        }

        // ==================================================
        // PREPARE
        // ==================================================

        private void PreparePoint(
            GameObject point
        )
        {
            point.transform.localScale =
                Vector3.one *
                pickupScale;

            SphereCollider sphere =
                point.GetComponent<
                    SphereCollider
                >();

            if (sphere == null)
            {
                sphere =
                    point.AddComponent<
                        SphereCollider
                    >();
            }

            sphere.isTrigger =
                true;

            sphere.radius =
                4.5f;
        }

        // ==================================================
        // INITIAL SPAWN
        // ==================================================

        private void SpawnInitialPoints()
        {
            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrySpawnPoint(
                    trackSegments[i]
                );
            }
        }

        // ==================================================
        // REFRESH
        // ==================================================

        private void RefreshSegment(
            TrackSegment segment
        )
        {
            ReleasePointFromSegment(
                segment
            );

            TrySpawnPoint(
                segment
            );
        }

        // ==================================================
        // TRY SPAWN
        // ==================================================

        private bool TrySpawnPoint(
            TrackSegment segment
        )
        {
            if (Random.value >
                spawnChancePerSegment)
            {
                return false;
            }

            return SpawnPoint(
                segment
            );
        }

        // ==================================================
        // SPAWN POINT
        // ==================================================

        private bool SpawnPoint(
            TrackSegment segment
        )
        {
            if (player == null)
            {
                player =
                    FindPlayer();
            }

            if (player == null)
            {
                Debug.LogWarning(
                    "ProjectPointRandomSpawner: " +
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

            GameObject point =
                GetAvailablePoint();

            if (point == null)
            {
                return false;
            }

            const int maxAttempts =
                18;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                float laneX =
                    GetRandomLaneX();

                float localZ =
                    Random.Range(
                        safeMinLocalZ,
                        maxLocalZ
                    );

                if (!TryGetSurfaceLocalY(
                        segment,
                        laneX,
                        localZ,
                        out float surfaceLocalY,
                        out bool isRampSurface
                    ))
                {
                    continue;
                }

                // Do not put +5 on the slope itself.
                if (isRampSurface)
                {
                    continue;
                }

                if (IsCollectibleTooClose(
                        segment,
                        laneX,
                        localZ
                    ))
                {
                    continue;
                }

                PlacePoint(
                    point,
                    segment,
                    laneX,
                    surfaceLocalY,
                    localZ
                );

                activePointBySegment[
                    segment
                ] =
                    point;

                return true;
            }

            return false;
        }

        // ==================================================
        // RANDOM LANE
        // ==================================================

        private float GetRandomLaneX()
        {
            int lane =
                Random.Range(
                    0,
                    3
                );

            if (lane == 0)
            {
                return LeftLaneX;
            }

            if (lane == 1)
            {
                return CenterLaneX;
            }

            return RightLaneX;
        }

        // ==================================================
        // SURFACE DETECTION
        // ==================================================

        private bool TryGetSurfaceLocalY(
            TrackSegment segment,
            float laneX,
            float localZ,
            out float surfaceLocalY,
            out bool isRampSurface
        )
        {
            surfaceLocalY =
                0f;

            isRampSurface =
                false;

            Vector3 candidateWorld =
                segment.transform.TransformPoint(
                    new Vector3(
                        laneX,
                        0f,
                        localZ
                    )
                );

            Vector3 rayOrigin =
                candidateWorld +
                Vector3.up *
                surfaceProbeHeight;

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    surfaceProbeDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                ))
            {
                return false;
            }

            isRampSurface =
                hit.collider != null &&
                hit.collider.gameObject.name.StartsWith(
                    "RampSolidStep_"
                );

            Vector3 localHitPoint =
                segment.transform.InverseTransformPoint(
                    hit.point
                );

            surfaceLocalY =
                localHitPoint.y;

            return true;
        }

        // ==================================================
        // COLLECTIBLE DISTANCE
        // ==================================================

        private bool IsCollectibleTooClose(
            TrackSegment segment,
            float laneX,
            float localZ
        )
        {
            BatteryPickup[] batteries =
                segment.GetComponentsInChildren<
                    BatteryPickup
                >(
                    false
                );

            for (int i = 0;
                 i < batteries.Length;
                 i++)
            {
                Vector3 localPosition =
                    segment.transform
                        .InverseTransformPoint(
                            batteries[i]
                                .transform
                                .position
                        );

                if (Mathf.Abs(
                        localPosition.x -
                        laneX
                    ) < 0.75f &&
                    Mathf.Abs(
                        localPosition.z -
                        localZ
                    ) <
                    minimumCollectibleDistance)
                {
                    return true;
                }
            }

            ProjectPointCollectible[] points =
                segment.GetComponentsInChildren<
                    ProjectPointCollectible
                >(
                    false
                );

            for (int i = 0;
                 i < points.Length;
                 i++)
            {
                Vector3 localPosition =
                    segment.transform
                        .InverseTransformPoint(
                            points[i]
                                .transform
                                .position
                        );

                if (Mathf.Abs(
                        localPosition.x -
                        laneX
                    ) < 0.75f &&
                    Mathf.Abs(
                        localPosition.z -
                        localZ
                    ) <
                    minimumCollectibleDistance)
                {
                    return true;
                }
            }

            return false;
        }

        // ==================================================
        // PLACE
        // ==================================================

        private void PlacePoint(
            GameObject point,
            TrackSegment segment,
            float laneX,
            float surfaceLocalY,
            float localZ
        )
        {
            point.transform.SetParent(
                segment.transform,
                false
            );

            point.transform.localPosition =
                new Vector3(
                    laneX,
                    surfaceLocalY +
                    heightAboveSurface,
                    localZ
                );

            point.transform.localRotation =
                Quaternion.identity;

            PreparePoint(
                point
            );

            point.SetActive(
                true
            );
        }

        // ==================================================
        // SAFE DISTANCE
        // ==================================================

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
                    )
                    .z;

            return Mathf.Max(
                minLocalZ,
                requiredLocalZ
            );
        }

        // ==================================================
        // AVAILABLE POINT
        // ==================================================

        private GameObject GetAvailablePoint()
        {
            for (int i = 0;
                 i < pool.Count;
                 i++)
            {
                GameObject candidate =
                    pool[i];

                bool assigned =
                    activePointBySegment
                        .ContainsValue(
                            candidate
                        );

                if (!assigned)
                {
                    return candidate;
                }
            }

            return null;
        }

        // ==================================================
        // RELEASE
        // ==================================================

        private void ReleasePointFromSegment(
            TrackSegment segment
        )
        {
            if (!activePointBySegment.TryGetValue(
                    segment,
                    out GameObject point
                ))
            {
                return;
            }

            point.SetActive(
                false
            );

            point.transform.SetParent(
                transform,
                false
            );

            activePointBySegment.Remove(
                segment
            );
        }

        private void ReleaseAllPoints()
        {
            List<TrackSegment> segments =
                new List<TrackSegment>(
                    activePointBySegment.Keys
                );

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                ReleasePointFromSegment(
                    segments[i]
                );
            }
        }

        // ==================================================
        // RESET
        // ==================================================

        private void HandleRunReset()
        {
            StartCoroutine(
                ResetAfterTrackReset()
            );
        }

        private IEnumerator ResetAfterTrackReset()
        {
            yield return null;

            ReleaseAllPoints();

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform
                        .position
                        .z;
            }

            SpawnInitialPoints();
        }

        // ==================================================
        // CLEANUP
        // ==================================================

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -=
                    HandleRunReset;
            }
        }
    }
}