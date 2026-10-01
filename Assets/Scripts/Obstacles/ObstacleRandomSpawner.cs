using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    public class ObstacleRandomSpawner : MonoBehaviour
    {
        // ==================================================
        // SPAWN SETTINGS
        // ==================================================

        [Header("Spawn Settings")]

        [SerializeField]
        [Range(0f, 1f)]
        private float spawnChancePerSegment = 0.65f;

        [SerializeField]
        private float minLocalZ = -8f;

        [SerializeField]
        private float maxLocalZ = 10f;

        [SerializeField]
        private float minimumDistanceAhead = 36f;

        [SerializeField]
        private float minimumCollectibleDistance = 4f;

        // ==================================================
        // SAFETY
        // ==================================================

        [Header("Safety")]

        [Tooltip(
            "Minimum distance between hazards " +
            "in the same lane."
        )]
        [SerializeField]
        private float minimumHazardDistance = 6f;

        [Tooltip(
            "Z range used when checking whether " +
            "all three lanes would become blocked."
        )]
        [SerializeField]
        private float laneBlockWindow = 5f;

        // ==================================================
        // POOL
        // ==================================================

        [Header("Pool")]

        [SerializeField]
        private int poolSize = 6;

        // ==================================================
        // JUMP OBSTACLE
        // ==================================================

        [Header("Jump Obstacle")]

        [SerializeField]
        private float jumpObstacleWidth = 1.25f;

        [SerializeField]
        private float jumpObstacleHeight = 0.9f;

        [SerializeField]
        private float jumpObstacleDepth = 0.8f;

        // ==================================================
        // SLIDE OBSTACLE
        // ==================================================

        [Header("Slide Obstacle")]

        [SerializeField]
        private float slideBarWidth = 1.45f;

        [SerializeField]
        private float slideBarHeight = 0.45f;

        [SerializeField]
        private float slideBarDepth = 0.8f;

        [SerializeField]
        private float slideBarCenterHeight = 1.5f;

        // ==================================================
        // LANES
        // ==================================================

        private const float LeftLaneX = -2.5f;
        private const float CenterLaneX = 0f;
        private const float RightLaneX = 2.5f;

        // Current prototype surface heights.
        private const float BoulevardSurfaceY = 0f;
        private const float TrainSurfaceY = 1.2f;

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

        private readonly HashSet<TrackSegment>
            pendingSegments =
                new HashSet<TrackSegment>();

        // ==================================================
        // UNITY
        // ==================================================

        private void Awake()
        {
            player = FindPlayer();

            CreateTemporaryMaterials();

            CreateObstacleTemplate();

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
                    "ObstacleRandomSpawner: " +
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

            StartCoroutine(
                SpawnInitialObstaclesAfterFrame()
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

                // When TrackManager recycles a segment,
                // its Z suddenly jumps forward.
                bool wasRecycled =
                    currentZ >
                    lastSegmentZ[i] + 5f;

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
        // TEMPORARY MATERIALS
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

            // Jump obstacle material.
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

            // Slide obstacle material.
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
                    jumpObstacleHeight / 2f,
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
                collider.isTrigger = true;
            }

            LethalObstacle jumpHazard =
                body.AddComponent<
                    LethalObstacle
                >();

            jumpHazard.SetHazardType(
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

            // ----------------------------------------------
            // TOP LETHAL BAR
            // ----------------------------------------------

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

            LethalObstacle slideHazard =
                topBar.AddComponent<
                    LethalObstacle
                >();

            slideHazard.SetHazardType(
                LethalObstacle
                    .HazardType
                    .Obstacle
            );

            Renderer topRenderer =
                topBar.GetComponent<
                    Renderer
                >();

            if (topRenderer != null &&
                slideMaterial != null)
            {
                topRenderer.sharedMaterial =
                    slideMaterial;
            }

            // ----------------------------------------------
            // VISUAL POSTS
            // ----------------------------------------------

            CreateSlideGatePost(
                group.transform,
                -0.6f
            );

            CreateSlideGatePost(
                group.transform,
                0.6f
            );
        }

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
                // Posts are visual only.
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
                    (i + 1);

                obstacle.SetActive(
                    false
                );

                pool.Add(
                    obstacle
                );
            }
        }

        private GameObject
            GetAvailableObstacle()
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
            // Let Battery and Project Point
            // spawners choose their locations first.
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
            // Let collectibles respawn first.
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

            // No valid spawn room.
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

            const int maxAttempts = 12;

            for (int attempt = 0;
                 attempt < maxAttempts;
                 attempt++)
            {
                // ------------------------------------------
                // CHOOSE RANDOM LANE
                // ------------------------------------------

                int lane =
                    Random.Range(
                        0,
                        3
                    );

                float laneX;
                float surfaceY;

                if (lane == 0)
                {
                    // Left train.
                    laneX =
                        LeftLaneX;

                    surfaceY =
                        TrainSurfaceY;
                }
                else if (lane == 1)
                {
                    // Boulevard.
                    laneX =
                        CenterLaneX;

                    surfaceY =
                        BoulevardSurfaceY;
                }
                else
                {
                    // Right train.
                    laneX =
                        RightLaneX;

                    surfaceY =
                        TrainSurfaceY;
                }

                // ------------------------------------------
                // CHOOSE RANDOM Z
                // ------------------------------------------

                float localZ =
                    Random.Range(
                        safeMinLocalZ,
                        maxLocalZ
                    );

                // ------------------------------------------
                // FAIRNESS CHECK
                // ------------------------------------------

                if (!IsSpawnPositionFair(
                        segment,
                        laneX,
                        localZ
                    ))
                {
                    continue;
                }

                // ------------------------------------------
                // PLACE
                // ------------------------------------------

                PlaceObstacle(
                    obstacle,
                    segment,
                    laneX,
                    surfaceY,
                    localZ
                );

                activeObstacleBySegment[
                    segment
                ] = obstacle;

                return true;
            }

            // No fair position was found.
            return false;
        }

        // ==================================================
        // FAIR SPAWNING
        // ==================================================

        private bool IsSpawnPositionFair(
            TrackSegment segment,
            float laneX,
            float localZ
        )
        {
            // ----------------------------------------------
            // 1. DO NOT SPAWN ON BATTERY / +5
            // ----------------------------------------------

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

            // Convert candidate position
            // into world coordinates.
            Vector3 candidateWorldPosition =
                segment.transform.TransformPoint(
                    new Vector3(
                        laneX,
                        0f,
                        localZ
                    )
                );

            // ----------------------------------------------
            // 2. KEEP DISTANCE FROM OTHER HAZARDS
            // ----------------------------------------------

            if (SpawnSafetyUtility
                .IsHazardTooClose(
                    candidateWorldPosition,
                    laneX,
                    minimumHazardDistance
                ))
            {
                return false;
            }

            // ----------------------------------------------
            // 3. NEVER BLOCK ALL THREE LANES
            // ----------------------------------------------

            if (SpawnSafetyUtility
                .WouldBlockAllLanes(
                    candidateWorldPosition,
                    laneX,
                    laneBlockWindow
                ))
            {
                return false;
            }

            return true;
        }

        // ==================================================
        // PLACE OBSTACLE
        // ==================================================

        private void PlaceObstacle(
            GameObject obstacle,
            TrackSegment segment,
            float laneX,
            float surfaceY,
            float localZ
        )
        {
            obstacle.transform.SetParent(
                segment.transform,
                false
            );

            obstacle.transform.localPosition =
                new Vector3(
                    laneX,
                    surfaceY,
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
                Random.value < 0.5f;

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

            Debug.Log(
                createJumpObstacle
                    ? "Jump obstacle spawned."
                    : "Slide obstacle spawned."
            );
        }

        // ==================================================
        // SAFE DISTANCE AHEAD
        // ==================================================

        private float
            CalculateSafeMinimumLocalZ(
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

            // Give TrackManager, Battery and
            // Project Point spawners time to reset.
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