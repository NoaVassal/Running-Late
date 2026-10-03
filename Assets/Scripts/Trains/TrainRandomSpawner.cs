using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    [DefaultExecutionOrder(-300)]
    public class TrainRandomSpawner : MonoBehaviour
    {
        // ==================================================
        // SPAWN SETTINGS
        // ==================================================

        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSide = 0.72f;

        [SerializeField]
        [Range(0f, 1f)]
        private float rampTrainChance = 0.55f;

        [SerializeField]
        [Min(0)]
        private int emptySegmentsBetweenTrains = 0;

        [SerializeField]
        private bool keepFirstSegmentClear = true;

        // ==================================================
        // TRAIN POSITION
        // ==================================================

        [Header("Train Position")]

        [SerializeField]
        private float minLocalZ = 0f;

        [SerializeField]
        private float maxLocalZ = 0f;

        // ==================================================
        // TRAIN SIZE
        // ==================================================

        [Header("Train Size")]

        [SerializeField]
        private float trainWidth = 2.1f;

        [SerializeField]
        private float trainHeight = 2.25f;

        [SerializeField]
        private float trainLength = 21f;

        [SerializeField]
        private float rampLength = 8f;

        [SerializeField]
        private float boardingZoneLength = 8f;

        // ==================================================
        // TRAIN VISUALS
        // ==================================================

        [Header("Train Visuals")]

        [Tooltip("Normal train visual prefab.")]
        [SerializeField]
        private GameObject normalTrainVisualPrefab;

        [Tooltip("Train-with-ramp visual prefab.")]
        [SerializeField]
        private GameObject rampTrainVisualPrefab;

        [Tooltip("Use 0 first. If the train faces backward, use 180.")]
        [SerializeField]
        private float trainVisualYawOffset = 0f;

        [Tooltip("Fine tuning after automatic fitting. Start with 1,1,1.")]
        [SerializeField]
        private Vector3 trainVisualScaleMultiplier =
            Vector3.one;

        [Tooltip("Fine tuning after automatic centering. Start with 0,0,0.")]
        [SerializeField]
        private Vector3 trainVisualPositionOffset =
            Vector3.zero;

        // ==================================================
        // TRAIN MOVEMENT
        // ==================================================

        [Header("Train Movement")]

        [SerializeField]
        [Min(0f)]
        private float approachWorldSpeed = 38f;

        [SerializeField]
        [Min(0f)]
        private float boardableWorldSpeed = 17.5f;

        [SerializeField]
        [Min(0f)]
        private float slowdownStartFrontDistance = 18f;

        [SerializeField]
        [Min(0f)]
        private float boardableFrontDistance = 2.5f;

        // ==================================================
        // LANES
        // ==================================================

        private const float LeftLaneX = -2.5f;
        private const float RightLaneX = 2.5f;

        // ==================================================
        // RUNTIME
        // ==================================================

        private TrackSegment[] trackSegments;
        private float[] lastSegmentZ;

        private int leftCooldown = 0;
        private int rightCooldown = 0;

        private Material rampTrainMaterial;
        private Material blockingTrainMaterial;

        private class SegmentTrainPair
        {
            public TrainVehicle left;
            public TrainVehicle right;
        }

        private readonly Dictionary<TrackSegment, SegmentTrainPair>
            trainPairs =
                new Dictionary<TrackSegment, SegmentTrainPair>();

        // ==================================================
        // AWAKE
        // ==================================================

        private void Awake()
        {
            FindAndSortSegments();

            if (trackSegments == null ||
                trackSegments.Length == 0)
            {
                Debug.LogError(
                    "TrainRandomSpawner: No TrackSegments were found.",
                    this
                );

                enabled = false;
                return;
            }

            PrepareThreeGroundLanes();

            CreateMaterials();

            CreateTrainSlots();

            ConfigureInitialTrains();

            SaveSegmentPositions();
        }

        // ==================================================
        // START
        // ==================================================

        private void Start()
        {
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
                    ConfigureSegment(
                        segment
                    );
                }

                lastSegmentZ[i] =
                    currentZ;
            }
        }

        // ==================================================
        // SEGMENTS
        // ==================================================

        private void FindAndSortSegments()
        {
            trackSegments =
                Object.FindObjectsByType<TrackSegment>(
                    FindObjectsSortMode.None
                );

            System.Array.Sort(
                trackSegments,
                (
                    first,
                    second
                ) =>
                    first.transform.position.z.CompareTo(
                        second.transform.position.z
                    )
            );

            lastSegmentZ =
                new float[
                    trackSegments.Length
                ];
        }

        private void SaveSegmentPositions()
        {
            if (trackSegments == null)
            {
                return;
            }

            if (lastSegmentZ == null ||
                lastSegmentZ.Length !=
                trackSegments.Length)
            {
                lastSegmentZ =
                    new float[
                        trackSegments.Length
                    ];
            }

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                if (trackSegments[i] == null)
                {
                    continue;
                }

                lastSegmentZ[i] =
                    trackSegments[i]
                        .transform
                        .position
                        .z;
            }
        }

        // ==================================================
        // GROUND LANES
        // ==================================================

        private void PrepareThreeGroundLanes()
        {
            int groundLayer =
                LayerMask.NameToLayer(
                    "Ground"
                );

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

                PrepareSideGround(
                    segment,
                    "LeftTrain",
                    LeftLaneX,
                    groundLayer
                );

                PrepareSideGround(
                    segment,
                    "RightTrain",
                    RightLaneX,
                    groundLayer
                );
            }
        }

        private void PrepareSideGround(
            TrackSegment segment,
            string objectName,
            float laneX,
            int groundLayer
        )
        {
            Transform lane =
                segment.transform.Find(
                    objectName
                );

            if (lane == null)
            {
                return;
            }

            lane.localPosition =
                new Vector3(
                    laneX,
                    -0.1f,
                    0f
                );

            lane.localRotation =
                Quaternion.identity;

            lane.localScale =
                new Vector3(
                    2.2f,
                    0.2f,
                    30f
                );

            if (groundLayer >= 0)
            {
                lane.gameObject.layer =
                    groundLayer;
            }

            lane.gameObject.SetActive(
                true
            );

            BoxCollider collider =
                lane.GetComponent<BoxCollider>();

            if (collider == null)
            {
                collider =
                    lane.gameObject.AddComponent<BoxCollider>();
            }

            collider.enabled =
                true;

            collider.isTrigger =
                false;
        }

        // ==================================================
        // MATERIALS
        // ==================================================

        private void CreateMaterials()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit"
                    );
            }

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard"
                    );
            }

            if (shader == null)
            {
                return;
            }

            rampTrainMaterial =
                new Material(
                    shader
                );

            rampTrainMaterial.name =
                "Runtime_BlueRampTrain";

            rampTrainMaterial.color =
                new Color(
                    0.10f,
                    0.50f,
                    0.95f,
                    1f
                );

            blockingTrainMaterial =
                new Material(
                    shader
                );

            blockingTrainMaterial.name =
                "Runtime_RedBlockingTrain";

            blockingTrainMaterial.color =
                new Color(
                    0.90f,
                    0.15f,
                    0.12f,
                    1f
                );
        }

        // ==================================================
        // TRAIN SLOTS
        // ==================================================

        private void CreateTrainSlots()
        {
            int groundLayer =
                LayerMask.NameToLayer(
                    "Ground"
                );

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

                SegmentTrainPair pair =
                    new SegmentTrainPair();

                pair.left =
                    CreateTrainSlot(
                        segment,
                        "LeftTrainVehicle",
                        LeftLaneX,
                        groundLayer
                    );

                pair.right =
                    CreateTrainSlot(
                        segment,
                        "RightTrainVehicle",
                        RightLaneX,
                        groundLayer
                    );

                trainPairs[
                    segment
                ] =
                    pair;
            }
        }

        private TrainVehicle CreateTrainSlot(
            TrackSegment segment,
            string objectName,
            float laneX,
            int groundLayer
        )
        {
            Transform existing =
                segment.transform.Find(
                    objectName
                );

            GameObject root;

            if (existing != null)
            {
                root =
                    existing.gameObject;
            }
            else
            {
                root =
                    new GameObject(
                        objectName
                    );

                root.transform.SetParent(
                    segment.transform,
                    false
                );
            }

            root.transform.localPosition =
                new Vector3(
                    laneX,
                    0f,
                    0f
                );

            root.transform.localRotation =
                Quaternion.identity;

            root.transform.localScale =
                Vector3.one;

            TrainVehicle vehicle =
                root.GetComponent<TrainVehicle>();

            if (vehicle == null)
            {
                vehicle =
                    root.AddComponent<TrainVehicle>();
            }

            vehicle.Build(
                trainWidth,
                trainHeight,
                trainLength,
                rampLength,
                boardingZoneLength,
                groundLayer,
                rampTrainMaterial,
                blockingTrainMaterial,
                normalTrainVisualPrefab,
                rampTrainVisualPrefab,
                trainVisualYawOffset,
                trainVisualScaleMultiplier,
                trainVisualPositionOffset,
                approachWorldSpeed,
                boardableWorldSpeed,
                slowdownStartFrontDistance,
                boardableFrontDistance
            );

            root.SetActive(
                false
            );

            return vehicle;
        }

        // ==================================================
        // CONFIGURE
        // ==================================================

        private void ConfigureInitialTrains()
        {
            leftCooldown = 0;
            rightCooldown = 0;

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

                if (i == 0 &&
                    keepFirstSegmentClear)
                {
                    SetSegmentEmpty(
                        segment
                    );

                    continue;
                }

                ConfigureSegment(
                    segment
                );
            }
        }

        private void ConfigureSegment(
            TrackSegment segment
        )
        {
            if (!trainPairs.TryGetValue(
                    segment,
                    out SegmentTrainPair pair
                ))
            {
                return;
            }

            ConfigureTrain(
                pair.left,
                LeftLaneX,
                ref leftCooldown
            );

            ConfigureTrain(
                pair.right,
                RightLaneX,
                ref rightCooldown
            );
        }

        private void ConfigureTrain(
            TrainVehicle vehicle,
            float laneX,
            ref int cooldown
        )
        {
            if (vehicle == null)
            {
                return;
            }

            if (!ShouldSpawnTrain(
                    ref cooldown
                ))
            {
                vehicle.gameObject.SetActive(
                    false
                );

                return;
            }

            float localZ =
                Random.Range(
                    minLocalZ,
                    maxLocalZ
                );

            TrainVehicle.TrainType type =
                Random.value <
                rampTrainChance
                    ? TrainVehicle.TrainType.Ramp
                    : TrainVehicle.TrainType.Blocking;

            vehicle.transform.localPosition =
                new Vector3(
                    laneX,
                    0f,
                    localZ
                );

            vehicle.transform.localRotation =
                Quaternion.identity;

            vehicle.transform.localScale =
                Vector3.one;

            vehicle.Configure(
                type,
                laneX
            );

            vehicle.gameObject.SetActive(
                true
            );
        }

        private bool ShouldSpawnTrain(
            ref int cooldown
        )
        {
            if (cooldown > 0)
            {
                cooldown--;

                return false;
            }

            bool spawn =
                Random.value <
                spawnChancePerSide;

            if (spawn)
            {
                cooldown =
                    emptySegmentsBetweenTrains;
            }

            return spawn;
        }

        private void SetSegmentEmpty(
            TrackSegment segment
        )
        {
            if (!trainPairs.TryGetValue(
                    segment,
                    out SegmentTrainPair pair
                ))
            {
                return;
            }

            if (pair.left != null)
            {
                pair.left.gameObject.SetActive(
                    false
                );
            }

            if (pair.right != null)
            {
                pair.right.gameObject.SetActive(
                    false
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

            FindAndSortSegments();

            ConfigureInitialTrains();

            SaveSegmentPositions();
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

            if (rampTrainMaterial != null)
            {
                Destroy(
                    rampTrainMaterial
                );
            }

            if (blockingTrainMaterial != null)
            {
                Destroy(
                    blockingTrainMaterial
                );
            }
        }
    }
}
