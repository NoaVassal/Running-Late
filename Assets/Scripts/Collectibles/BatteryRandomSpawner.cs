using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    public class BatteryRandomSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment = 0.50f;

        [SerializeField]
        private float minLocalZ = -10f;

        [SerializeField]
        private float maxLocalZ = 10f;

        [SerializeField]
        private float minimumDistanceAhead = 30f;

        [Header("Pickup Settings")]

        [SerializeField]
        private float pickupScale = 0.035f;

        [SerializeField]
        private int poolSize = 6;

        private const float LeftLaneX = -2.5f;
        private const float CenterLaneX = 0f;
        private const float RightLaneX = 2.5f;

        private const float BoulevardY = 1.05f;
        private const float TrainY = 2.25f;

        private BatteryPickup template;

        private TrackSegment[] trackSegments;
        private float[] lastSegmentZ;

        private Transform player;

        private readonly List<GameObject> pool =
            new List<GameObject>();

        private readonly Dictionary<TrackSegment, GameObject>
            activeBatteryBySegment =
                new Dictionary<TrackSegment, GameObject>();

        private void Awake()
        {
            player = FindPlayer();

            template =
                Object.FindFirstObjectByType<BatteryPickup>();

            if (template == null)
            {
                Debug.LogError(
                    "BatteryRandomSpawner: " +
                    "No BatteryPickup was found in the scene."
                );

                enabled = false;
                return;
            }

            template.transform.SetParent(
                transform
            );

            PrepareBattery(
                template.gameObject
            );

            template.gameObject.SetActive(false);
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
                Object.FindObjectsByType<TrackSegment>(
                    FindObjectsSortMode.None
                );

            if (trackSegments == null ||
                trackSegments.Length == 0)
            {
                Debug.LogError(
                    "BatteryRandomSpawner: " +
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

            CreatePool();
            SpawnInitialBatteries();

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
                GameObject battery =
                    Instantiate(
                        template.gameObject,
                        transform
                    );

                battery.name =
                    "Battery_Pooled_" +
                    (i + 1);

                PrepareBattery(
                    battery
                );

                battery.SetActive(
                    false
                );

                pool.Add(
                    battery
                );
            }
        }

        private void PrepareBattery(
            GameObject battery
        )
        {
            battery.transform.localScale =
                Vector3.one *
                pickupScale;

            SphereCollider sphere =
                battery.GetComponent<
                    SphereCollider
                >();

            if (sphere == null)
            {
                sphere =
                    battery.AddComponent<
                        SphereCollider
                    >();
            }

            sphere.isTrigger = true;
            sphere.radius = 4.5f;
        }

        private void SpawnInitialBatteries()
        {
            int spawnedCount = 0;

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                if (TrySpawnBattery(
                        trackSegments[i]
                    ))
                {
                    spawnedCount++;
                }
            }

            if (spawnedCount == 0)
            {
                for (int i = 0;
                     i < trackSegments.Length;
                     i++)
                {
                    if (SpawnBattery(
                            trackSegments[i]
                        ))
                    {
                        break;
                    }
                }
            }
        }

        private void RefreshSegment(
            TrackSegment segment
        )
        {
            ReleaseBatteryFromSegment(
                segment
            );

            TrySpawnBattery(
                segment
            );
        }

        private bool TrySpawnBattery(
            TrackSegment segment
        )
        {
            if (Random.value >
                spawnChancePerSegment)
            {
                return false;
            }

            return SpawnBattery(
                segment
            );
        }

        private bool SpawnBattery(
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
                    "BatteryRandomSpawner: " +
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
                // המקטע כבר קרוב מדי לשחקנית.
                // לא מייצרים עליו Battery.
                return false;
            }

            GameObject battery =
                GetAvailableBattery();

            if (battery == null)
            {
                return false;
            }

            int randomLane =
                Random.Range(0, 3);

            float x;
            float y;

            if (randomLane == 0)
            {
                // Left Train
                x = LeftLaneX;
                y = TrainY;
            }
            else if (randomLane == 1)
            {
                // Boulevard
                x = CenterLaneX;
                y = BoulevardY;
            }
            else
            {
                // Right Train
                x = RightLaneX;
                y = TrainY;
            }

            float randomZ =
                Random.Range(
                    safeMinLocalZ,
                    maxLocalZ
                );

            battery.transform.SetParent(
                segment.transform,
                false
            );

            battery.transform.localPosition =
                new Vector3(
                    x,
                    y,
                    randomZ
                );

            battery.transform.localRotation =
                Quaternion.identity;

            PrepareBattery(
                battery
            );

            battery.SetActive(
                true
            );

            activeBatteryBySegment[
                segment
            ] = battery;

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

        private GameObject GetAvailableBattery()
        {
            for (int i = 0;
                 i < pool.Count;
                 i++)
            {
                GameObject candidate =
                    pool[i];

                bool alreadyAssigned =
                    activeBatteryBySegment
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

        private void ReleaseBatteryFromSegment(
            TrackSegment segment
        )
        {
            if (!activeBatteryBySegment
                    .TryGetValue(
                        segment,
                        out GameObject battery
                    ))
            {
                return;
            }

            battery.SetActive(
                false
            );

            battery.transform.SetParent(
                transform,
                false
            );

            activeBatteryBySegment.Remove(
                segment
            );
        }

        private void ReleaseAllBatteries()
        {
            List<TrackSegment> segments =
                new List<TrackSegment>(
                    activeBatteryBySegment.Keys
                );

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                ReleaseBatteryFromSegment(
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

            ReleaseAllBatteries();

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform.position.z;
            }

            SpawnInitialBatteries();
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