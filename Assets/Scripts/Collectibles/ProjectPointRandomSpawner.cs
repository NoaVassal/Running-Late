using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    public class ProjectPointRandomSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment = 0.45f;

        [SerializeField]
        private float minLocalZ = -10f;

        [SerializeField]
        private float maxLocalZ = 10f;

        [SerializeField]
        private float minimumDistanceAhead = 30f;

        [Header("Pickup Settings")]

        [SerializeField]
        private float pickupScale = 0.05f;

        [SerializeField]
        private int poolSize = 8;

        private const float LeftLaneX = -2.5f;
        private const float CenterLaneX = 0f;
        private const float RightLaneX = 2.5f;

        private const float BoulevardY = 1.05f;
        private const float TrainY = 2.25f;

        private ProjectPointCollectible template;

        private TrackSegment[] trackSegments;
        private float[] lastSegmentZ;

        private Transform player;

        private readonly List<GameObject> pool =
            new List<GameObject>();

        private readonly Dictionary<TrackSegment, GameObject>
            activePointBySegment =
                new Dictionary<TrackSegment, GameObject>();

        private void Awake()
        {
            player = FindPlayer();

            template =
                Object.FindFirstObjectByType<
                    ProjectPointCollectible
                >();

            if (template == null)
            {
                Debug.LogError(
                    "ProjectPointRandomSpawner: " +
                    "No ProjectPointCollectible " +
                    "was found in the scene."
                );

                enabled = false;
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

        private void Start()
        {
            if (!enabled)
                return;

            if (player == null)
            {
                player = FindPlayer();
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
                    "No TrackSegments were found."
                );

                enabled = false;
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
                        .transform.position.z;
            }

            CreatePool();
            SpawnInitialPoints();

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

                bool wasRecycled =
                    currentZ >
                    lastSegmentZ[i] + 5f;

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
                    (i + 1);

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

            sphere.isTrigger = true;
            sphere.radius = 4.5f;
        }

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

        private bool SpawnPoint(
            TrackSegment segment
        )
        {
            if (player == null)
            {
                player = FindPlayer();
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

            int randomLane =
                Random.Range(0, 3);

            float x;
            float y;

            if (randomLane == 0)
            {
                x = LeftLaneX;
                y = TrainY;
            }
            else if (randomLane == 1)
            {
                x = CenterLaneX;
                y = BoulevardY;
            }
            else
            {
                x = RightLaneX;
                y = TrainY;
            }

            float randomZ =
                Random.Range(
                    safeMinLocalZ,
                    maxLocalZ
                );

            point.transform.SetParent(
                segment.transform,
                false
            );

            point.transform.localPosition =
                new Vector3(
                    x,
                    y,
                    randomZ
                );

            point.transform.localRotation =
                Quaternion.identity;

            PreparePoint(
                point
            );

            point.SetActive(
                true
            );

            activePointBySegment[
                segment
            ] = point;

            return true;
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

        private GameObject GetAvailablePoint()
        {
            for (int i = 0;
                 i < pool.Count;
                 i++)
            {
                GameObject candidate =
                    pool[i];

                bool alreadyAssigned =
                    activePointBySegment
                        .ContainsValue(
                            candidate
                        );

                if (!alreadyAssigned)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ReleasePointFromSegment(
            TrackSegment segment
        )
        {
            if (!activePointBySegment
                    .TryGetValue(
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
                        .transform.position.z;
            }

            SpawnInitialPoints();
        }

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
