using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunningLate
{
    public class TrainVehicle : MonoBehaviour
    {
        public enum TrainType
        {
            Ramp,
            Blocking
        }

        // ==================================================
        // ACTIVE TRAINS
        // ==================================================

        private static readonly List<TrainVehicle> activeTrains =
            new List<TrainVehicle>();

        // ==================================================
        // TYPE / SIZE
        // ==================================================

        [SerializeField]
        private TrainType trainType;

        private float laneX;
        private float trainWidth;
        private float trainHeight;
        private float trainLength;
        private float rampLength;
        private float boardingZoneLength;

        // ==================================================
        // RAMP ENTRY
        // ==================================================

        [Header("Ramp Entry")]

        [SerializeField]
        private float groundRampEntryLength = 1.6f;

        [SerializeField]
        private float rampEntryFrontTolerance = 0.45f;

        // ==================================================
        // INDEPENDENT TRAIN MOVEMENT
        // ==================================================

        private float approachWorldSpeed = 38f;
        private float boardableWorldSpeed = 17.5f;
        private float slowdownStartFrontDistance = 18f;
        private float boardableFrontDistance = 2.5f;

        private float independentWorldZ = 0f;
        private bool independentMovementInitialized = false;

        private Transform playerTransform;

        // ==================================================
        // SAME-LANE TRAIN SPACING
        // ==================================================

        // Adjacent track segments are 30 units long and each train is
        // 21 units long, so the natural visual gap is 9 units.
        // Independent approach movement used to let a rear train catch
        // a slower train near the player. This minimum gap prevents
        // trains in the same lane from joining into one long train.
        private const float MinimumSameLaneTrainGap = 9f;

        private const float SameLaneTolerance = 0.20f;

        // ==================================================
        // VISUAL PREFABS
        // ==================================================

        private GameObject normalTrainVisualPrefab;
        private GameObject rampTrainVisualPrefab;

        private GameObject visualModel;
        private GameObject currentVisualSourcePrefab;

        private float visualYawOffset = 0f;
        private Vector3 visualScaleMultiplier = Vector3.one;
        private Vector3 visualPositionOffset = Vector3.zero;

        // ==================================================
        // PHYSICS OBJECTS
        // ==================================================

        private GameObject body;
        private Renderer bodyRenderer;

        private GameObject rampSupportsRoot;

        private GameObject frontHitZone;
        private GameObject leftSideHitZone;
        private GameObject rightSideHitZone;

        // ==================================================
        // MATERIALS
        // ==================================================

        private Material rampMaterial;
        private Material blockingMaterial;

        // ==================================================
        // SIDE HIT
        // ==================================================

        private int sideHitCount = 0;
        private float lastSideHitTime = -100f;

        private const float SideHitCooldown = 0.20f;

        [Header("Side Hit")]

        [SerializeField]
        private float secondHitGameOverDelay = 0.22f;

        private bool secondHitPending = false;

        // ==================================================
        // RAMP PHYSICS
        // ==================================================

        [Header("Ramp Physics")]

        [SerializeField]
        [Range(16, 48)]
        private int rampPhysicsSteps = 32;

        [SerializeField]
        private float rampColliderOverlap = 0.12f;

        // ==================================================
        // PUBLIC
        // ==================================================

        public float LaneX => laneX;
        public float RoofHeight => trainHeight;
        public TrainType Type => trainType;

        // ==================================================
        // UNITY
        // ==================================================

        private void OnEnable()
        {
            if (!activeTrains.Contains(this))
            {
                activeTrains.Add(this);
            }

            secondHitPending = false;
        }

        private void OnDisable()
        {
            activeTrains.Remove(this);

            StopAllCoroutines();

            secondHitPending = false;
        }

        private void OnDestroy()
        {
            activeTrains.Remove(this);
        }

        private void LateUpdate()
        {
            UpdateIndependentMovement();
        }

        // ==================================================
        // INDEPENDENT MOVEMENT
        // ==================================================

        private void UpdateIndependentMovement()
        {
            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            if (!independentMovementInitialized)
            {
                independentWorldZ = transform.position.z;
                independentMovementInitialized = true;
            }

            if (playerTransform == null)
            {
                PlayerController player =
                    Object.FindFirstObjectByType<PlayerController>();

                if (player == null)
                {
                    return;
                }

                playerTransform = player.transform;
            }

            float trainFrontWorldZ =
                independentWorldZ -
                trainLength / 2f;

            float frontDistanceToPlayer =
                trainFrontWorldZ -
                playerTransform.position.z;

            float worldSpeed;

            if (frontDistanceToPlayer >=
                slowdownStartFrontDistance)
            {
                worldSpeed =
                    approachWorldSpeed;
            }
            else if (frontDistanceToPlayer <=
                     boardableFrontDistance)
            {
                worldSpeed =
                    boardableWorldSpeed;
            }
            else
            {
                float t =
                    Mathf.InverseLerp(
                        boardableFrontDistance,
                        slowdownStartFrontDistance,
                        frontDistanceToPlayer
                    );

                t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );

                worldSpeed =
                    Mathf.Lerp(
                        boardableWorldSpeed,
                        approachWorldSpeed,
                        t
                    );
            }

            float proposedWorldZ =
                independentWorldZ -
                worldSpeed *
                Time.deltaTime;

            independentWorldZ =
                KeepSafeDistanceFromTrainAhead(
                    proposedWorldZ
                );

            Vector3 worldPosition =
                transform.position;

            worldPosition.z =
                independentWorldZ;

            transform.position =
                worldPosition;
        }

        // ==================================================
        // SAME-LANE SEPARATION
        // ==================================================

        private float KeepSafeDistanceFromTrainAhead(
            float proposedWorldZ
        )
        {
            float currentWorldZ =
                independentWorldZ;

            bool foundTrainAhead =
                false;

            float minimumAllowedWorldZ =
                float.NegativeInfinity;

            for (int i = 0;
                 i < activeTrains.Count;
                 i++)
            {
                TrainVehicle other =
                    activeTrains[i];

                if (other == null ||
                    other == this ||
                    !other.isActiveAndEnabled)
                {
                    continue;
                }

                if (Mathf.Abs(
                        other.laneX -
                        laneX
                    ) >
                    SameLaneTolerance)
                {
                    continue;
                }

                float otherWorldZ =
                    other.transform.position.z;

                // Trains move toward smaller world-Z values.
                // Therefore a train with a smaller Z is ahead.
                if (otherWorldZ >=
                    currentWorldZ -
                    0.01f)
                {
                    continue;
                }

                float requiredCenterDistance =
                    trainLength /
                    2f +
                    other.trainLength /
                    2f +
                    MinimumSameLaneTrainGap;

                float allowedWorldZ =
                    otherWorldZ +
                    requiredCenterDistance;

                if (!foundTrainAhead ||
                    allowedWorldZ >
                    minimumAllowedWorldZ)
                {
                    minimumAllowedWorldZ =
                        allowedWorldZ;

                    foundTrainAhead =
                        true;
                }
            }

            if (!foundTrainAhead)
            {
                return
                    proposedWorldZ;
            }

            if (proposedWorldZ >=
                minimumAllowedWorldZ)
            {
                return
                    proposedWorldZ;
            }

            // If trains are already closer than the minimum gap,
            // do not teleport the rear train backward.
            // Hold it in place until the train ahead opens the gap.
            if (currentWorldZ <
                minimumAllowedWorldZ)
            {
                return
                    currentWorldZ;
            }

            return
                minimumAllowedWorldZ;
        }

        // ==================================================
        // BUILD
        // ==================================================

        public void Build(
            float width,
            float height,
            float length,
            float newRampLength,
            float newBoardingZoneLength,
            int groundLayer,
            Material blueMaterial,
            Material redMaterial,
            GameObject newNormalTrainVisualPrefab,
            GameObject newRampTrainVisualPrefab,
            float newVisualYawOffset,
            Vector3 newVisualScaleMultiplier,
            Vector3 newVisualPositionOffset,
            float newApproachWorldSpeed,
            float newBoardableWorldSpeed,
            float newSlowdownStartFrontDistance,
            float newBoardableFrontDistance
        )
        {
            if (body != null)
            {
                return;
            }

            trainWidth = width;
            trainHeight = height;
            trainLength = length;

            rampLength =
                Mathf.Clamp(
                    newRampLength,
                    2f,
                    trainLength - 2f
                );

            boardingZoneLength =
                newBoardingZoneLength;

            rampMaterial =
                blueMaterial;

            blockingMaterial =
                redMaterial;

            normalTrainVisualPrefab =
                newNormalTrainVisualPrefab;

            rampTrainVisualPrefab =
                newRampTrainVisualPrefab;

            visualYawOffset =
                newVisualYawOffset;

            visualScaleMultiplier =
                newVisualScaleMultiplier;

            visualPositionOffset =
                newVisualPositionOffset;

            approachWorldSpeed =
                Mathf.Max(
                    0f,
                    newApproachWorldSpeed
                );

            boardableWorldSpeed =
                Mathf.Max(
                    0f,
                    newBoardableWorldSpeed
                );

            slowdownStartFrontDistance =
                Mathf.Max(
                    0f,
                    newSlowdownStartFrontDistance
                );

            boardableFrontDistance =
                Mathf.Clamp(
                    newBoardableFrontDistance,
                    0f,
                    slowdownStartFrontDistance
                );

            CreatePhysicsBody(
                groundLayer
            );

            CreateSolidRampPhysics(
                groundLayer
            );

            frontHitZone =
                CreateHitZone(
                    "FrontHitZone",
                    TrainHitZone.ZoneType.Front
                );

            leftSideHitZone =
                CreateHitZone(
                    "LeftSideHitZone",
                    TrainHitZone.ZoneType.Side
                );

            rightSideHitZone =
                CreateHitZone(
                    "RightSideHitZone",
                    TrainHitZone.ZoneType.Side
                );
        }

        // ==================================================
        // PHYSICS BODY
        // ==================================================

        private void CreatePhysicsBody(
            int groundLayer
        )
        {
            body =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            body.name =
                "TrainBodyPhysics";

            body.transform.SetParent(
                transform,
                false
            );

            body.layer =
                groundLayer;

            bodyRenderer =
                body.GetComponent<Renderer>();

            if (bodyRenderer != null)
            {
                bodyRenderer.enabled =
                    false;
            }
        }

        // ==================================================
        // SOLID RAMP PHYSICS
        // ==================================================

        private void CreateSolidRampPhysics(
            int groundLayer
        )
        {
            rampSupportsRoot =
                new GameObject(
                    "RampSolidPhysics"
                );

            rampSupportsRoot.transform.SetParent(
                transform,
                false
            );

            int steps =
                Mathf.Max(
                    16,
                    rampPhysicsSteps
                );

            float stepDepth =
                rampLength /
                steps;

            float rampStartZ =
                -trainLength /
                2f;

            for (int i = 0;
                 i < steps;
                 i++)
            {
                float progress =
                    (i + 1f) /
                    steps;

                float stepHeight =
                    trainHeight *
                    progress;

                GameObject step =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube
                    );

                step.name =
                    "RampSolidStep_" +
                    (i + 1);

                step.transform.SetParent(
                    rampSupportsRoot.transform,
                    false
                );

                step.layer =
                    groundLayer;

                float stepZ =
                    rampStartZ +
                    stepDepth *
                    (i + 0.5f);

                step.transform.localPosition =
                    new Vector3(
                        0f,
                        stepHeight / 2f,
                        stepZ
                    );

                step.transform.localRotation =
                    Quaternion.identity;

                step.transform.localScale =
                    new Vector3(
                        trainWidth + 0.12f,
                        stepHeight,
                        stepDepth +
                        rampColliderOverlap
                    );

                Renderer renderer =
                    step.GetComponent<Renderer>();

                if (renderer != null)
                {
                    renderer.enabled =
                        false;
                }

                BoxCollider collider =
                    step.GetComponent<BoxCollider>();

                if (collider != null)
                {
                    collider.enabled =
                        true;

                    collider.isTrigger =
                        false;
                }
            }
        }

        // ==================================================
        // VISUAL MODEL
        // ==================================================

        private void EnsureVisualModel()
        {
            GameObject desiredPrefab =
                trainType == TrainType.Ramp &&
                rampTrainVisualPrefab != null
                    ? rampTrainVisualPrefab
                    : normalTrainVisualPrefab;

            if (desiredPrefab == null)
            {
                if (visualModel != null)
                {
                    Destroy(
                        visualModel
                    );

                    visualModel =
                        null;

                    currentVisualSourcePrefab =
                        null;
                }

                return;
            }

            if (visualModel != null &&
                currentVisualSourcePrefab ==
                desiredPrefab)
            {
                ConfigureVisualModelTransform();
                return;
            }

            if (visualModel != null)
            {
                Destroy(
                    visualModel
                );
            }

            visualModel =
                Instantiate(
                    desiredPrefab,
                    transform
                );

            visualModel.name =
                desiredPrefab.name +
                "_RuntimeVisual";

            currentVisualSourcePrefab =
                desiredPrefab;

            DisableVisualPhysics(
                visualModel
            );

            ConfigureVisualModelTransform();
        }

        private void DisableVisualPhysics(
            GameObject root
        )
        {
            Collider[] colliders =
                root.GetComponentsInChildren<Collider>(
                    true
                );

            for (int i = 0;
                 i < colliders.Length;
                 i++)
            {
                colliders[i].enabled =
                    false;
            }

            Rigidbody[] rigidbodies =
                root.GetComponentsInChildren<Rigidbody>(
                    true
                );

            for (int i = 0;
                 i < rigidbodies.Length;
                 i++)
            {
                rigidbodies[i].isKinematic =
                    true;

                rigidbodies[i].detectCollisions =
                    false;

                rigidbodies[i].useGravity =
                    false;
            }
        }

        private void ConfigureVisualModelTransform()
        {
            if (visualModel == null)
            {
                return;
            }

            visualModel.SetActive(
                true
            );

            visualModel.transform.localPosition =
                Vector3.zero;

            visualModel.transform.localRotation =
                Quaternion.identity;

            visualModel.transform.localScale =
                Vector3.one;

            Bounds initialBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visualModel.transform,
                    transform,
                    out initialBounds
                ))
            {
                return;
            }

            float autoYaw =
                initialBounds.size.x >
                initialBounds.size.z
                    ? 90f
                    : 0f;

            visualModel.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    autoYaw +
                    visualYawOffset,
                    0f
                );

            Bounds rotatedBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visualModel.transform,
                    transform,
                    out rotatedBounds
                ))
            {
                return;
            }

            float scaleX =
                SafeScale(
                    trainWidth,
                    rotatedBounds.size.x
                );

            float scaleY =
                SafeScale(
                    trainHeight,
                    rotatedBounds.size.y
                );

            float scaleZ =
                SafeScale(
                    trainLength,
                    rotatedBounds.size.z
                );

            visualModel.transform.localScale =
                new Vector3(
                    scaleX *
                    Mathf.Max(
                        0.01f,
                        visualScaleMultiplier.x
                    ),
                    scaleY *
                    Mathf.Max(
                        0.01f,
                        visualScaleMultiplier.y
                    ),
                    scaleZ *
                    Mathf.Max(
                        0.01f,
                        visualScaleMultiplier.z
                    )
                );

            Bounds fittedBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visualModel.transform,
                    transform,
                    out fittedBounds
                ))
            {
                return;
            }

            Vector3 correction =
                new Vector3(
                    -fittedBounds.center.x,
                    -fittedBounds.min.y,
                    -fittedBounds.center.z
                );

            visualModel.transform.localPosition =
                correction +
                visualPositionOffset;
        }

        private float SafeScale(
            float target,
            float current
        )
        {
            if (current <= 0.0001f)
            {
                return 1f;
            }

            return
                target /
                current;
        }

        private bool TryGetRendererBoundsRelativeTo(
            Transform visualRoot,
            Transform reference,
            out Bounds combinedBounds
        )
        {
            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(
                    true
                );

            bool hasBounds =
                false;

            combinedBounds =
                new Bounds();

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                Renderer renderer =
                    renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Bounds localBounds =
                    renderer.localBounds;

                Vector3 center =
                    localBounds.center;

                Vector3 extents =
                    localBounds.extents;

                for (int x = -1;
                     x <= 1;
                     x += 2)
                {
                    for (int y = -1;
                         y <= 1;
                         y += 2)
                    {
                        for (int z = -1;
                             z <= 1;
                             z += 2)
                        {
                            Vector3 rendererLocalCorner =
                                center +
                                Vector3.Scale(
                                    extents,
                                    new Vector3(
                                        x,
                                        y,
                                        z
                                    )
                                );

                            Vector3 worldCorner =
                                renderer.transform.TransformPoint(
                                    rendererLocalCorner
                                );

                            Vector3 referenceLocalCorner =
                                reference.InverseTransformPoint(
                                    worldCorner
                                );

                            if (!hasBounds)
                            {
                                combinedBounds =
                                    new Bounds(
                                        referenceLocalCorner,
                                        Vector3.zero
                                    );

                                hasBounds =
                                    true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(
                                    referenceLocalCorner
                                );
                            }
                        }
                    }
                }
            }

            return
                hasBounds;
        }

        // ==================================================
        // HIT ZONES
        // ==================================================

        private GameObject CreateHitZone(
            string objectName,
            TrainHitZone.ZoneType type
        )
        {
            GameObject zone =
                new GameObject(
                    objectName
                );

            zone.transform.SetParent(
                transform,
                false
            );

            BoxCollider collider =
                zone.AddComponent<BoxCollider>();

            collider.isTrigger =
                true;

            TrainHitZone hitZone =
                zone.AddComponent<TrainHitZone>();

            hitZone.Initialize(
                this,
                type
            );

            return zone;
        }

        // ==================================================
        // CONFIGURE
        // ==================================================

        public void Configure(
            TrainType newType,
            float newLaneX
        )
        {
            trainType =
                newType;

            laneX =
                newLaneX;

            sideHitCount =
                0;

            lastSideHitTime =
                -100f;

            secondHitPending =
                false;

            independentWorldZ =
                transform.position.z;

            independentMovementInitialized =
                true;

            if (trainType ==
                TrainType.Ramp)
            {
                ConfigureRampTrain();
            }
            else
            {
                ConfigureBlockingTrain();
            }

            ConfigureSideZones();

            EnsureVisualModel();
        }

        // ==================================================
        // RAMP TRAIN
        // ==================================================

        private void ConfigureRampTrain()
        {
            float bodyLength =
                trainLength -
                rampLength;

            body.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight / 2f,
                    rampLength / 2f
                );

            body.transform.localRotation =
                Quaternion.identity;

            body.transform.localScale =
                new Vector3(
                    trainWidth,
                    trainHeight,
                    bodyLength
                );

            if (rampSupportsRoot != null)
            {
                rampSupportsRoot.SetActive(
                    true
                );
            }

            frontHitZone.SetActive(
                false
            );
        }

        // ==================================================
        // BLOCKING TRAIN
        // ==================================================

        private void ConfigureBlockingTrain()
        {
            body.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight / 2f,
                    0f
                );

            body.transform.localRotation =
                Quaternion.identity;

            body.transform.localScale =
                new Vector3(
                    trainWidth,
                    trainHeight,
                    trainLength
                );

            if (rampSupportsRoot != null)
            {
                rampSupportsRoot.SetActive(
                    false
                );
            }

            frontHitZone.SetActive(
                true
            );

            frontHitZone.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight / 2f,
                    -trainLength / 2f -
                    0.15f
                );

            frontHitZone.transform.localRotation =
                Quaternion.identity;

            BoxCollider frontCollider =
                frontHitZone.GetComponent<BoxCollider>();

            frontCollider.size =
                new Vector3(
                    trainWidth * 0.95f,
                    trainHeight,
                    0.5f
                );
        }

        // ==================================================
        // SIDE HIT ZONES
        // ==================================================

        private void ConfigureSideZones()
        {
            float trainStartZ =
                -trainLength /
                2f;

            float zoneStart;

            if (trainType ==
                TrainType.Ramp)
            {
                zoneStart =
                    trainStartZ +
                    groundRampEntryLength;
            }
            else
            {
                zoneStart =
                    trainStartZ +
                    0.35f;
            }

            float zoneEnd =
                trainLength /
                2f;

            float zoneLength =
                zoneEnd -
                zoneStart;

            float zoneCenterZ =
                (
                    zoneStart +
                    zoneEnd
                ) /
                2f;

            float sideX =
                trainWidth /
                2f +
                0.15f;

            ConfigureSideZone(
                leftSideHitZone,
                -sideX,
                zoneCenterZ,
                zoneLength
            );

            ConfigureSideZone(
                rightSideHitZone,
                sideX,
                zoneCenterZ,
                zoneLength
            );
        }

        private void ConfigureSideZone(
            GameObject zone,
            float x,
            float z,
            float depth
        )
        {
            zone.transform.localPosition =
                new Vector3(
                    x,
                    trainHeight / 2f,
                    z
                );

            zone.transform.localRotation =
                Quaternion.identity;

            BoxCollider collider =
                zone.GetComponent<BoxCollider>();

            collider.size =
                new Vector3(
                    0.45f,
                    trainHeight + 1f,
                    depth
                );

            collider.isTrigger =
                true;
        }

        // ==================================================
        // FRONT HIT
        // ==================================================

        public void RegisterFrontHit(
            PlayerController player
        )
        {
            if (trainType !=
                TrainType.Blocking)
            {
                return;
            }

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            GameManager.Instance.TriggerGameOver(
                "Hit train front"
            );
        }

        // ==================================================
        // SIDE HIT
        // ==================================================

        public void RegisterSideHit(
            PlayerController player
        )
        {
            if (player == null)
            {
                return;
            }

            if (player.IsExitingFromTrain(this))
            {
                return;
            }

            if (secondHitPending)
            {
                return;
            }

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            if (Time.time -
                lastSideHitTime <
                SideHitCooldown)
            {
                return;
            }

            lastSideHitTime =
                Time.time;

            sideHitCount++;

            Debug.Log(
                "TRAIN SIDE HIT #" +
                sideHitCount
            );

            if (sideHitCount == 1)
            {
                player.HandleTrainSideHit(
                    laneX
                );

                return;
            }

            secondHitPending =
                true;

            player.HandleTrainSideHit(
                laneX
            );

            StartCoroutine(
                SecondSideHitGameOver()
            );
        }

        private IEnumerator SecondSideHitGameOver()
        {
            yield return
                new WaitForSeconds(
                    secondHitGameOverDelay
                );

            if (GameManager.Instance == null)
            {
                yield break;
            }

            if (!GameManager.Instance.IsRunning)
            {
                yield break;
            }

            GameManager.Instance.TriggerGameOver(
                "Hit train side twice"
            );
        }

        // ==================================================
        // POSITION HELPERS
        // ==================================================

        public bool ContainsWorldZ(
            float worldZ
        )
        {
            return
                Mathf.Abs(
                    worldZ -
                    transform.position.z
                ) <=
                trainLength /
                2f;
        }

        public bool IsRampBoardingWorldZ(
            float worldZ
        )
        {
            if (trainType !=
                TrainType.Ramp)
            {
                return false;
            }

            float relativeZ =
                worldZ -
                transform.position.z;

            float rampFront =
                -trainLength /
                2f;

            float start =
                rampFront -
                rampEntryFrontTolerance;

            float end =
                rampFront +
                groundRampEntryLength;

            return
                relativeZ >= start &&
                relativeZ <= end;
        }

        public static TrainVehicle FindTrainAtWorldPosition(
            float laneWorldX,
            float worldZ
        )
        {
            for (int i = 0;
                 i < activeTrains.Count;
                 i++)
            {
                TrainVehicle train =
                    activeTrains[i];

                if (train == null ||
                    !train.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bool sameLane =
                    Mathf.Abs(
                        train.transform.position.x -
                        laneWorldX
                    ) <
                    0.8f;

                if (!sameLane)
                {
                    continue;
                }

                if (!train.ContainsWorldZ(
                        worldZ
                    ))
                {
                    continue;
                }

                return train;
            }

            return null;
        }
    }
}
