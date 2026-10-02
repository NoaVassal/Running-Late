using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    [DefaultExecutionOrder(-100)]
    public class ObstacleRandomSpawner : MonoBehaviour
    {
        // ==================================================
        // SPAWN SETTINGS
        // ==================================================

        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment =
            0.80f;

        [SerializeField]
        private float minLocalZ =
            -8f;

        [SerializeField]
        private float maxLocalZ =
            10f;

        [SerializeField]
        private float minimumDistanceAhead =
            36f;

        [SerializeField]
        private float minimumCollectibleDistance =
            4f;

        // ==================================================
        // SAFETY
        // ==================================================

        [Header("Safety")]

        [Tooltip(
            "Minimum distance between hazards " +
            "in the same lane."
        )]
        [SerializeField]
        private float minimumHazardDistance =
            22f;

        [Tooltip(
            "Z range used when checking whether " +
            "all three lanes would become blocked."
        )]
        [SerializeField]
        private float laneBlockWindow =
            6f;

        // ==================================================
        // POOL
        // ==================================================

        [Header("Pool")]

        [SerializeField]
        private int poolSize =
            6;

        // ==================================================
        // JUMP OBSTACLE
        // ==================================================

        [Header("Jump Obstacle")]

        [SerializeField]
        private float jumpObstacleWidth =
            1.25f;

        [SerializeField]
        private float jumpObstacleHeight =
            0.9f;

        [SerializeField]
        private float jumpObstacleDepth =
            0.8f;

        // ==================================================
        // SLIDE OBSTACLE
        // ==================================================

        [Header("Slide Obstacle")]

        [SerializeField]
        private float slideBarWidth =
            1.45f;

        [SerializeField]
        private float slideBarHeight =
            0.45f;

        [SerializeField]
        private float slideBarDepth =
            0.8f;

        [SerializeField]
        private float slideBarCenterHeight =
            1.5f;

        // ==================================================
        // LANES
        // ==================================================

        private const float LeftLaneX =
            -2.5f;

        private const float CenterLaneX =
            0f;

        private const float RightLaneX =
            2.5f;

        // All three ground lanes have
        // their top surface at Y = 0.
        private const float GroundSurfaceY =
            0f;

        // ==================================================
        // RUNTIME
        // ==================================================

        private TrackSegment[] trackSegments;

        private float[] lastSegmentZ;

        private Transform player;

        private GameObject obstacleTemplate;

        private Material jumpMaterial;

        private Material slideMaterial;

        private readonly List<GameObject> pool =
            new List<GameObject>();

        private readonly Dictionary<
            TrackSegment,
            GameObject
        > activeObstacleBySegment =
            new Dictionary<
                TrackSegment,
                GameObject
            >();

        private readonly HashSet<
            TrackSegment
        > pendingSegments =
            new HashSet<
                TrackSegment
            >();

        // ==================================================
        // AWAKE
        // ==================================================

        private void Awake()
        {
            player =
                FindPlayer();

            CreateTemporaryMaterials();

            CreateObstacleTemplate();

            CreatePool();
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
                    "ObstacleRandomSpawner: " +
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

            StartCoroutine(
                SpawnInitialObstaclesAfterFrame()
            );

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
                    HandleSegmentRecycled(
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
        // MATERIALS
        // ==================================================

        private void CreateTemporaryMaterials()
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
                    "ObstacleRandomSpawner: " +
                    "Could not find a suitable shader."
                );

                return;
            }

            jumpMaterial =
                new Material(
                    shader
                );

            jumpMaterial.name =
                "Runtime_JumpObstacle";

            jumpMaterial.color =
                new Color(
                    1f,
                    0.25f,
                    0.1f,
                    1f
                );

            slideMaterial =
                new Material(
                    shader
                );

            slideMaterial.name =
                "Runtime_SlideObstacle";

            slideMaterial.color =
                new Color(
                    0.15f,
                    0.55f,
                    1f,
                    1f
                );
        }

        // ==================================================
        // TEMPLATE
        // ==================================================

        private void CreateObstacleTemplate()
        {
            obstacleTemplate =
                new GameObject(
                    "Obstacle_Template"
                );

            obstacleTemplate.transform.SetParent(
                transform,
                false
            );

            CreateJumpObstacle(
                obstacleTemplate.transform
            );

            CreateSlideObstacle(
                obstacleTemplate.transform
            );

            obstacleTemplate.SetActive(
                false
            );
        }

        // ==================================================
        // JUMP OBSTACLE
        // ==================================================

        private void CreateJumpObstacle(
            Transform root
        )
        {
            GameObject group =
                new GameObject(
                    "JumpBarrier"
                );

            group.transform.SetParent(
                root,
                false
            );

            GameObject body =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            body.name =
                "Body";

            body.transform.SetParent(
                group.transform,
                false
            );

            body.transform.localPosition =
                new Vector3(
                    0f,
                    jumpObstacleHeight /
                    2f,
                    0f
                );

            body.transform.localScale =
                new Vector3(
                    jumpObstacleWidth,
                    jumpObstacleHeight,
                    jumpObstacleDepth
                );

            BoxCollider collider =
                body.GetComponent<
                    BoxCollider
                >();

            if (collider != null)
            {
                collider.isTrigger =
                    true;
            }

            LethalObstacle hazard =
                body.AddComponent<
                    LethalObstacle
                >();

            hazard.SetHazardType(
                LethalObstacle
                    .HazardType
                    .Obstacle
            );

            Renderer renderer =
                body.GetComponent<
                    Renderer
                >();

            if (renderer != null &&
                jumpMaterial != null)
            {
                renderer.sharedMaterial =
                    jumpMaterial;
            }
        }

        // ==================================================
        // SLIDE OBSTACLE
        // ==================================================

        private void CreateSlideObstacle(
            Transform root
        )
        {
            GameObject group =
                new GameObject(
                    "SlideGate"
                );

            group.transform.SetParent(
                root,
                false
            );

            GameObject topBar =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            topBar.name =
                "TopBar";

            topBar.transform.SetParent(
                group.transform,
                false
            );

            topBar.transform.localPosition =
                new Vector3(
                    0f,
                    slideBarCenterHeight,
                    0f
                );

            topBar.transform.localScale =
                new Vector3(
                    slideBarWidth,
                    slideBarHeight,
                    slideBarDepth
                );

            BoxCollider topCollider =
                topBar.GetComponent<
                    BoxCollider
                >();

            if (topCollider != null)
            {
                topCollider.isTrigger =
                    true;
            }

            LethalObstacle hazard =
                topBar.AddComponent<
                    LethalObstacle
                >();

            hazard.SetHazardType(
                LethalObstacle
                    .HazardType
                    .Obstacle
            );

            Renderer renderer =
                topBar.GetComponent<
                    Renderer
                >();

            if (renderer != null &&
                slideMaterial != null)
            {
                renderer.sharedMaterial =
                    slideMaterial;
            }

            CreateSlideGatePost(
                group.transform,
                -0.6f
            );

            CreateSlideGatePost(
                group.transform,
                0.6f
            );
        }

        // ==================================================
        // SLIDE POSTS
        // ==================================================

        private void CreateSlideGatePost(
            Transform parent,
            float x
        )
        {
            GameObject post =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            post.name =
                x < 0f
                    ? "LeftPost"
                    : "RightPost";

            post.transform.SetParent(
                parent,
                false
            );

            post.transform.localPosition =
                new Vector3(
                    x,
                    0.7f,
                    0f
                );

            post.transform.localScale =
                new Vector3(
                    0.12f,
                    1.4f,
                    0.15f
                );

            Collider collider =
                post.GetComponent<
                    Collider
                >();

            if (collider != null)
            {
                collider.enabled =
                    false;
            }

            Renderer renderer =
                post.GetComponent<
                    Renderer
                >();

            if (renderer != null &&
                slideMaterial != null)
            {
                renderer.sharedMaterial =
                    slideMaterial;
            }
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
                GameObject obstacle =
                    Instantiate(
                        obstacleTemplate,
                        transform
                    );

                obstacle.name =
                    "Obstacle_Pooled_" +
                    (
                        i +
                        1
                    );

                obstacle.SetActive(
                    false
                );

                pool.Add(
                    obstacle
                );
            }
        }

        private GameObject GetAvailableObstacle()
        {
            for (int i = 0;
                 i < pool.Count;
                 i++)
            {
                GameObject obstacle =
                    pool[i];

                bool alreadyAssigned =
                    activeObstacleBySegment
                        .ContainsValue(
                            obstacle
                        );

                if (!obstacle.activeSelf &&
                    !alreadyAssigned)
                {
                    return obstacle;
                }
            }

            return null;
        }

        // ==================================================
        // INITIAL SPAWN
        // ==================================================

        private IEnumerator
            SpawnInitialObstaclesAfterFrame()
        {
            // Let trains and collectibles spawn first.
            yield return null;

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrySpawnObstacle(
                    trackSegments[i]
                );
            }
        }

        // ==================================================
        // SEGMENT RECYCLE
        // ==================================================

        private void HandleSegmentRecycled(
            TrackSegment segment
        )
        {
            ReleaseObstacleFromSegment(
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
            // Let TrainRandomSpawner and
            // collectibles choose positions first.
            yield return null;

            TrySpawnObstacle(
                segment
            );

            pendingSegments.Remove(
                segment
            );
        }

        // ==================================================
        // SPAWN
        // ==================================================

        private bool TrySpawnObstacle(
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
                player =
                    FindPlayer();
            }

            if (player == null)
            {
                Debug.LogWarning(
                    "ObstacleRandomSpawner: " +
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

            GameObject obstacle =
                GetAvailableObstacle();

            if (obstacle == null)
            {
                return false;
            }

            const int maxAttempts =
                24;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                // ==================================================
                // CHOOSE LANE
                // ==================================================

                float laneX =
                    GetRandomLaneX();

                // ==================================================
                // CHOOSE Z
                // ==================================================

                float localZ =
                    Random.Range(
                        safeMinLocalZ,
                        maxLocalZ
                    );

                Vector3 candidateWorldPosition =
                    segment.transform.TransformPoint(
                        new Vector3(
                            laneX,
                            GroundSurfaceY,
                            localZ
                        )
                    );

                // ==================================================
                // IMPORTANT:
                // NO OBSTACLE WHERE THERE IS A TRAIN
                // ==================================================

                TrainVehicle train =
                    TrainVehicle
                        .FindTrainAtWorldPosition(
                            candidateWorldPosition.x,
                            candidateWorldPosition.z
                        );

                if (train != null)
                {
                    continue;
                }

                // ==================================================
                // FAIRNESS
                // ==================================================

                if (!IsSpawnPositionFair(
                        segment,
                        laneX,
                        localZ,
                        candidateWorldPosition
                    ))
                {
                    continue;
                }

                // ==================================================
                // PLACE ON GROUND ONLY
                // ==================================================

                PlaceObstacle(
                    obstacle,
                    segment,
                    laneX,
                    localZ
                );

                activeObstacleBySegment[
                    segment
                ] =
                    obstacle;

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
        // FAIR SPAWN
        // ==================================================

        private bool IsSpawnPositionFair(
            TrackSegment segment,
            float laneX,
            float localZ,
            Vector3 candidateWorldPosition
        )
        {
            // ==================================================
            // 1. DON'T SPAWN ON BATTERY / +5
            // ==================================================

            if (SpawnSafetyUtility
                .IsPickupTooClose(
                    segment,
                    laneX,
                    localZ,
                    minimumCollectibleDistance
                ))
            {
                return false;
            }

            // ==================================================
            // 2. DISTANCE FROM OTHER OBSTACLES
            // ==================================================

            if (SpawnSafetyUtility
                .IsHazardTooClose(
                    candidateWorldPosition,
                    laneX,
                    minimumHazardDistance
                ))
            {
                return false;
            }

            // ==================================================
            // 3. DON'T BLOCK ALL THREE LANES
            // ==================================================

            if (SpawnSafetyUtility
                .WouldBlockAllLanes(
                    candidateWorldPosition,
                    laneX,
                    laneBlockWindow
                ))
            {
                return false;
            }

            // ==================================================
            // 4. TRAINS + THIS OBSTACLE MUST
            //    LEAVE AT LEAST ONE FREE LANE
            // ==================================================

            if (WouldObstaclePlusTrainsBlockAllLanes(
                    candidateWorldPosition,
                    laneX
                ))
            {
                return false;
            }

            return true;
        }

        // ==================================================
        // TRAIN FAIRNESS
        // ==================================================

        private bool
            WouldObstaclePlusTrainsBlockAllLanes(
                Vector3 candidateWorldPosition,
                float obstacleLaneX
            )
        {
            bool leftBlocked =
                Mathf.Abs(
                    obstacleLaneX -
                    LeftLaneX
                ) <
                0.75f;

            bool centerBlocked =
                Mathf.Abs(
                    obstacleLaneX -
                    CenterLaneX
                ) <
                0.75f;

            bool rightBlocked =
                Mathf.Abs(
                    obstacleLaneX -
                    RightLaneX
                ) <
                0.75f;

            // ==================================================
            // LEFT TRAIN?
            // ==================================================

            TrainVehicle leftTrain =
                TrainVehicle
                    .FindTrainAtWorldPosition(
                        LeftLaneX,
                        candidateWorldPosition.z
                    );

            if (leftTrain != null)
            {
                leftBlocked =
                    true;
            }

            // ==================================================
            // CENTER
            //
            // Trains currently only use
            // left/right, so center remains
            // based on obstacle hazards.
            // ==================================================

            // ==================================================
            // RIGHT TRAIN?
            // ==================================================

            TrainVehicle rightTrain =
                TrainVehicle
                    .FindTrainAtWorldPosition(
                        RightLaneX,
                        candidateWorldPosition.z
                    );

            if (rightTrain != null)
            {
                rightBlocked =
                    true;
            }

            return
                leftBlocked &&
                centerBlocked &&
                rightBlocked;
        }

        // ==================================================
        // PLACE OBSTACLE
        // ==================================================

        private void PlaceObstacle(
            GameObject obstacle,
            TrackSegment segment,
            float laneX,
            float localZ
        )
        {
            obstacle.transform.SetParent(
                segment.transform,
                false
            );

            // IMPORTANT:
            // Always ground level.
            obstacle.transform.localPosition =
                new Vector3(
                    laneX,
                    GroundSurfaceY,
                    localZ
                );

            obstacle.transform.localRotation =
                Quaternion.identity;

            obstacle.transform.localScale =
                Vector3.one;

            Transform jumpBarrier =
                obstacle.transform.Find(
                    "JumpBarrier"
                );

            Transform slideGate =
                obstacle.transform.Find(
                    "SlideGate"
                );

            bool createJumpObstacle =
                Random.value <
                0.5f;

            if (jumpBarrier != null)
            {
                jumpBarrier.gameObject.SetActive(
                    createJumpObstacle
                );
            }

            if (slideGate != null)
            {
                slideGate.gameObject.SetActive(
                    !createJumpObstacle
                );
            }

            obstacle.SetActive(
                true
            );
        }

        // ==================================================
        // SAFE DISTANCE AHEAD
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
        // RELEASE
        // ==================================================

        private void ReleaseObstacleFromSegment(
            TrackSegment segment
        )
        {
            if (!activeObstacleBySegment
                    .TryGetValue(
                        segment,
                        out GameObject obstacle
                    ))
            {
                return;
            }

            obstacle.SetActive(
                false
            );

            obstacle.transform.SetParent(
                transform,
                false
            );

            activeObstacleBySegment.Remove(
                segment
            );
        }

        private void ReleaseAllObstacles()
        {
            List<TrackSegment> segments =
                new List<TrackSegment>(
                    activeObstacleBySegment.Keys
                );

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                ReleaseObstacleFromSegment(
                    segments[i]
                );
            }

            pendingSegments.Clear();
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

        private IEnumerator
            ResetAfterTrackReset()
        {
            ReleaseAllObstacles();

            // Track, trains and collectibles first.
            yield return null;
            yield return null;

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

            for (int i = 0;
                 i < trackSegments.Length;
                 i++)
            {
                TrySpawnObstacle(
                    trackSegments[i]
                );
            }
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

            if (jumpMaterial != null)
            {
                Destroy(
                    jumpMaterial
                );
            }

            if (slideMaterial != null)
            {
                Destroy(
                    slideMaterial
                );
            }
        }
    }
}