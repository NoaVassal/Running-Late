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

        private static readonly List<TrainVehicle>
            activeTrains =
                new List<TrainVehicle>();

        // ==================================================
        // TYPE
        // ==================================================

        [SerializeField]
        private TrainType trainType;

        // ==================================================
        // TRAIN DATA
        // ==================================================

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
        private float groundRampEntryLength =
            1.6f;

        [SerializeField]
        private float rampEntryFrontTolerance =
            0.45f;

        // ==================================================
        // OBJECTS
        // ==================================================

        private GameObject body;

        private GameObject rampVisual;

        private GameObject rampSupportsRoot;

        private GameObject frontHitZone;

        private GameObject leftSideHitZone;

        private GameObject rightSideHitZone;

        // ==================================================
        // RENDERERS
        // ==================================================

        private Renderer bodyRenderer;

        private Renderer rampRenderer;

        // ==================================================
        // MATERIALS
        // ==================================================

        private Material rampMaterial;

        private Material blockingMaterial;

        // ==================================================
        // SIDE HIT
        // ==================================================

        private int sideHitCount =
            0;

        private float lastSideHitTime =
            -100f;

        private const float
            SideHitCooldown = 0.20f;

        [Header("Side Hit")]

        [SerializeField]
        private float secondHitGameOverDelay =
            0.22f;

        private bool secondHitPending =
            false;

        // ==================================================
        // RAMP PHYSICS
        // ==================================================

        [Header("Ramp Physics")]

        [SerializeField]
        [Range(16, 48)]
        private int rampPhysicsSteps =
            32;

        [SerializeField]
        private float rampColliderOverlap =
            0.12f;

        // ==================================================
        // PUBLIC
        // ==================================================

        public float LaneX =>
            laneX;

        public float RoofHeight =>
            trainHeight;

        public TrainType Type =>
            trainType;

        // ==================================================
        // UNITY
        // ==================================================

        private void OnEnable()
        {
            if (!activeTrains.Contains(this))
            {
                activeTrains.Add(
                    this
                );
            }

            secondHitPending =
                false;
        }

        private void OnDisable()
        {
            activeTrains.Remove(
                this
            );

            StopAllCoroutines();

            secondHitPending =
                false;
        }

        private void OnDestroy()
        {
            activeTrains.Remove(
                this
            );
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
            Material redMaterial
        )
        {
            if (body != null)
            {
                return;
            }

            trainWidth =
                width;

            trainHeight =
                height;

            trainLength =
                length;

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

            // ==================================================
            // BODY
            // ==================================================

            body =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            body.name =
                "TrainBody";

            body.transform.SetParent(
                transform,
                false
            );

            body.layer =
                groundLayer;

            bodyRenderer =
                body.GetComponent<Renderer>();

            // ==================================================
            // VISIBLE RAMP
            // ==================================================

            rampVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            rampVisual.name =
                "TrainRampVisual";

            rampVisual.transform.SetParent(
                transform,
                false
            );

            rampVisual.layer =
                groundLayer;

            rampRenderer =
                rampVisual.GetComponent<Renderer>();

            BoxCollider rampVisualCollider =
                rampVisual.GetComponent<BoxCollider>();

            if (rampVisualCollider != null)
            {
                rampVisualCollider.enabled =
                    false;
            }

            // ==================================================
            // SOLID RAMP
            // ==================================================

            rampSupportsRoot =
                new GameObject(
                    "RampSolidPhysics"
                );

            rampSupportsRoot
                .transform
                .SetParent(
                    transform,
                    false
                );

            BuildSolidRampPhysics(
                groundLayer
            );

            // ==================================================
            // HIT ZONES
            // ==================================================

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
        // BUILD SOLID RAMP PHYSICS
        // ==================================================

        private void BuildSolidRampPhysics(
            int groundLayer
        )
        {
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
                    (
                        i +
                        1f
                    ) /
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
                    (
                        i +
                        1
                    );

                step.transform.SetParent(
                    rampSupportsRoot.transform,
                    false
                );

                step.layer =
                    groundLayer;

                float stepZ =
                    rampStartZ +
                    stepDepth *
                    (
                        i +
                        0.5f
                    );

                step.transform.localPosition =
                    new Vector3(
                        0f,
                        stepHeight /
                        2f,
                        stepZ
                    );

                step.transform.localRotation =
                    Quaternion.identity;

                step.transform.localScale =
                    new Vector3(
                        trainWidth +
                        0.12f,
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
        // CREATE HIT ZONE
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
        }

        // ==================================================
        // BLUE TRAIN
        // ==================================================

        private void ConfigureRampTrain()
        {
            float bodyLength =
                trainLength -
                rampLength;

            body.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight /
                    2f,
                    rampLength /
                    2f
                );

            body.transform.localRotation =
                Quaternion.identity;

            body.transform.localScale =
                new Vector3(
                    trainWidth,
                    trainHeight,
                    bodyLength
                );

            if (bodyRenderer != null)
            {
                bodyRenderer.sharedMaterial =
                    rampMaterial;
            }

            // ==================================================
            // RAMP VISUAL
            // ==================================================

            float slopeLength =
                Mathf.Sqrt(
                    rampLength *
                    rampLength +
                    trainHeight *
                    trainHeight
                );

            float slopeAngle =
                Mathf.Atan2(
                    trainHeight,
                    rampLength
                ) *
                Mathf.Rad2Deg;

            rampVisual.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight /
                    2f,
                    -trainLength /
                    2f +
                    rampLength /
                    2f
                );

            rampVisual.transform.localRotation =
                Quaternion.Euler(
                    -slopeAngle,
                    0f,
                    0f
                );

            rampVisual.transform.localScale =
                new Vector3(
                    trainWidth,
                    0.18f,
                    slopeLength
                );

            rampVisual.SetActive(
                true
            );

            if (rampRenderer != null)
            {
                rampRenderer.sharedMaterial =
                    rampMaterial;
            }

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
        // RED TRAIN
        // ==================================================

        private void ConfigureBlockingTrain()
        {
            body.transform.localPosition =
                new Vector3(
                    0f,
                    trainHeight /
                    2f,
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

            if (bodyRenderer != null)
            {
                bodyRenderer.sharedMaterial =
                    blockingMaterial;
            }

            rampVisual.SetActive(
                false
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

            frontHitZone
                .transform
                .localPosition =
                new Vector3(
                    0f,
                    trainHeight /
                    2f,
                    -trainLength /
                    2f -
                    0.15f
                );

            frontHitZone
                .transform
                .localRotation =
                Quaternion.identity;

            BoxCollider frontCollider =
                frontHitZone
                    .GetComponent<BoxCollider>();

            frontCollider.size =
                new Vector3(
                    trainWidth *
                    0.95f,
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

        // ==================================================
        // CONFIGURE SIDE ZONE
        // ==================================================

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
                    trainHeight /
                    2f,
                    z
                );

            zone.transform.localRotation =
                Quaternion.identity;

            BoxCollider collider =
                zone.GetComponent<BoxCollider>();

            collider.size =
                new Vector3(
                    0.45f,
                    trainHeight +
                    1f,
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

            GameManager.Instance
                .TriggerGameOver(
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

            // ==================================================
            // IMPORTANT FIX
            //
            // If the player is intentionally walking
            // OFF THIS train, the side trigger must NOT
            // push her back onto the roof.
            // ==================================================

            if (player.IsExitingFromTrain(
                    this
                ))
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

            // ==================================================
            // FIRST SIDE HIT
            // ==================================================

            if (sideHitCount == 1)
            {
                player.HandleTrainSideHit(
                    laneX
                );

                return;
            }

            // ==================================================
            // SECOND SIDE HIT
            // ==================================================

            secondHitPending =
                true;

            player.HandleTrainSideHit(
                laneX
            );

            StartCoroutine(
                SecondSideHitGameOver()
            );
        }

        // ==================================================
        // SECOND SIDE HIT GAME OVER
        // ==================================================

        private IEnumerator
            SecondSideHitGameOver()
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

            GameManager.Instance
                .TriggerGameOver(
                    "Hit train side twice"
                );
        }

        // ==================================================
        // CONTAINS WORLD Z
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

        // ==================================================
        // BLUE RAMP BOARDING
        // ==================================================

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

        // ==================================================
        // FIND TRAIN
        // ==================================================

        public static TrainVehicle
            FindTrainAtWorldPosition(
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