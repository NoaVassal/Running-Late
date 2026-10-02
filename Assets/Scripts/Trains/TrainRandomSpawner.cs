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

        [Tooltip(
            "Chance that a train will appear " +
            "on each side of a track segment."
        )]
        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSide =
            0.72f;

        [Tooltip(
            "Chance that a spawned train " +
            "will be the blue ramp train."
        )]
        [SerializeField]
        [Range(0f, 1f)]
        private float rampTrainChance =
            0.55f;

        [Tooltip(
            "0 means a train may appear again " +
            "on the very next track segment."
        )]
        [SerializeField]
        [Min(0)]
        private int emptySegmentsBetweenTrains =
            0;

        [Tooltip(
            "Keep the first track segment " +
            "clear when the run begins."
        )]
        [SerializeField]
        private bool keepFirstSegmentClear =
            true;

        // ==================================================
        // TRAIN POSITION
        // ==================================================

        [Header("Train Position")]

        [Tooltip(
            "Minimum local Z position inside a segment."
        )]
        [SerializeField]
        private float minLocalZ =
            0f;

        [Tooltip(
            "Maximum local Z position inside a segment."
        )]
        [SerializeField]
        private float maxLocalZ =
            0f;

        // ==================================================
        // TRAIN SIZE
        // ==================================================

        [Header("Train Size")]

        [SerializeField]
        private float trainWidth =
            2.1f;

        [Tooltip(
            "All trains use exactly the same height."
        )]
        [SerializeField]
        private float trainHeight =
            2.6f;

        [Tooltip(
            "Track segments are 30 units long. " +
            "A train length of 21 leaves a 9-unit gap " +
            "between trains on consecutive segments."
        )]
        [SerializeField]
        private float trainLength =
            21f;

        [Tooltip(
            "Length of the blue train ramp."
        )]
        [SerializeField]
        private float rampLength =
            8f;

        [Tooltip(
            "Front area of the blue train " +
            "where entering from another lane is allowed."
        )]
        [SerializeField]
        private float boardingZoneLength =
            8f;

        // ==================================================
        // LANE POSITIONS
        // ==================================================

        private const float LeftLaneX =
            -2.5f;

        private const float RightLaneX =
            2.5f;

        // ==================================================
        // RUNTIME
        // ==================================================

        private TrackSegment[]
            trackSegments;

        private float[]
            lastSegmentZ;

        private int leftCooldown =
            0;

        private int rightCooldown =
            0;

        // ==================================================
        // MATERIALS
        // ==================================================

        private Material
            rampTrainMaterial;

        private Material
            blockingTrainMaterial;

        // ==================================================
        // SEGMENT TRAIN PAIRS
        // ==================================================

        private class SegmentTrainPair
        {
            public TrainVehicle left;
            public TrainVehicle right;
        }

        private readonly Dictionary<
            TrackSegment,
            SegmentTrainPair
        > trainPairs =
            new Dictionary<
                TrackSegment,
                SegmentTrainPair
            >();

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
                    "TrainRandomSpawner: " +
                    "No TrackSegments were found.",
                    this
                );

                enabled =
                    false;

                return;
            }

            // The old LeftTrain / RightTrain cubes
            // now become normal ground lanes.
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
                    segment
                        .transform
                        .position
                        .z;

                // When TrackManager recycles a segment,
                // it suddenly jumps forward in Z.
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
        // FIND TRACK SEGMENTS
        // ==================================================

        private void FindAndSortSegments()
        {
            trackSegments =
                Object.FindObjectsByType<
                    TrackSegment
                >(
                    FindObjectsSortMode.None
                );

            System.Array.Sort(
                trackSegments,
                (
                    first,
                    second
                ) =>
                    first
                        .transform
                        .position
                        .z
                        .CompareTo(
                            second
                                .transform
                                .position
                                .z
                        )
            );

            lastSegmentZ =
                new float[
                    trackSegments.Length
                ];
        }

        // ==================================================
        // SAVE SEGMENT POSITIONS
        // ==================================================

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
        // PREPARE THREE GROUND LANES
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

        // ==================================================
        // PREPARE SIDE GROUND
        // ==================================================

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
                Debug.LogWarning(
                    "TrainRandomSpawner: " +
                    objectName +
                    " was not found under " +
                    segment.name +
                    ".",
                    segment
                );

                return;
            }

            // Turn the old train platform
            // into normal ground.
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
                lane.GetComponent<
                    BoxCollider
                >();

            if (collider == null)
            {
                collider =
                    lane.gameObject
                        .AddComponent<
                            BoxCollider
                        >();
            }

            collider.enabled =
                true;

            collider.isTrigger =
                false;
        }

        // ==================================================
        // CREATE MATERIALS
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
                Debug.LogWarning(
                    "TrainRandomSpawner: " +
                    "Could not find a suitable shader."
                );

                return;
            }

            // ----------------------------------------------
            // BLUE RAMP TRAIN
            // ----------------------------------------------

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

            // ----------------------------------------------
            // RED BLOCKING TRAIN
            // ----------------------------------------------

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
        // CREATE TRAIN SLOTS
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

        // ==================================================
        // CREATE ONE TRAIN SLOT
        // ==================================================

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
                root.GetComponent<
                    TrainVehicle
                >();

            if (vehicle == null)
            {
                vehicle =
                    root.AddComponent<
                        TrainVehicle
                    >();
            }

            vehicle.Build(
                trainWidth,
                trainHeight,
                trainLength,
                rampLength,
                boardingZoneLength,
                groundLayer,
                rampTrainMaterial,
                blockingTrainMaterial
            );

            root.SetActive(
                false
            );

            return vehicle;
        }

        // ==================================================
        // INITIAL TRAINS
        // ==================================================

        private void ConfigureInitialTrains()
        {
            leftCooldown =
                0;

            rightCooldown =
                0;

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

                // First segment should be safe
                // when the player starts.
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

        // ==================================================
        // CONFIGURE SEGMENT
        // ==================================================

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

        // ==================================================
        // CONFIGURE TRAIN
        // ==================================================

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

            bool shouldSpawn =
                ShouldSpawnTrain(
                    ref cooldown
                );

            if (!shouldSpawn)
            {
                vehicle
                    .gameObject
                    .SetActive(
                        false
                    );

                return;
            }

            // With Min = 0 and Max = 0,
            // every train is centered in its segment.
            //
            // Track segment = 30
            // Train = 21
            //
            // Therefore:
            //
            // 30 - 21 = 9 units of jump gap
            // between consecutive trains.
            float localZ =
                Random.Range(
                    minLocalZ,
                    maxLocalZ
                );

            TrainVehicle.TrainType type =
                Random.value <
                rampTrainChance
                    ? TrainVehicle
                        .TrainType
                        .Ramp
                    : TrainVehicle
                        .TrainType
                        .Blocking;

            vehicle
                .transform
                .localPosition =
                new Vector3(
                    laneX,
                    0f,
                    localZ
                );

            vehicle
                .transform
                .localRotation =
                Quaternion.identity;

            vehicle
                .transform
                .localScale =
                Vector3.one;

            vehicle.Configure(
                type,
                laneX
            );

            vehicle
                .gameObject
                .SetActive(
                    true
                );
        }

        // ==================================================
        // SPAWN RULE
        // ==================================================

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

        // ==================================================
        // EMPTY SEGMENT
        // ==================================================

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
                pair.left
                    .gameObject
                    .SetActive(
                        false
                    );
            }

            if (pair.right != null)
            {
                pair.right
                    .gameObject
                    .SetActive(
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
            // Wait one frame so TrackManager
            // can restore segment positions first.
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