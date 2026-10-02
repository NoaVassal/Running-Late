using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RunningLate
{
    public class PlayerController : MonoBehaviour
    {
        // ==================================================
        // CONFIGURATION
        // ==================================================

        [Header("Configuration")]
        [SerializeField]
        private GameConfig config;

        // ==================================================
        // REFERENCES
        // ==================================================

        [Header("References")]

        [SerializeField]
        private Rigidbody rb;

        [SerializeField]
        private CapsuleCollider capsuleCollider;

        [SerializeField]
        private Animator animator;

        // ==================================================
        // GROUND DETECTION
        // ==================================================

        [Header("Ground Detection")]

        [SerializeField]
        private LayerMask groundMask;

        // ==================================================
        // JUMP FEEL
        // ==================================================

        [Header("Jump Feel")]

        [SerializeField]
        private float upwardGravityMultiplier =
            1.05f;

        [SerializeField]
        private float fallGravityMultiplier =
            3.5f;

        [SerializeField]
        private float maxUpwardSpeed =
            6.5f;

        [SerializeField]
        private float trainJumpMaxUpwardSpeed =
            4.6f;

        [SerializeField]
        private float maxFallSpeed =
            20f;

        [SerializeField]
        private float jumpToSlideDownSpeed =
            9f;

        // ==================================================
        // JUMP BUFFER
        // ==================================================

        [Header("Jump Buffer")]

        [SerializeField]
        private float jumpBufferDuration =
            0.18f;

        // ==================================================
        // TRAIN EXIT
        // ==================================================

        [Header("Train Exit Feel")]

        [SerializeField]
        private float trainExitLaneSpeedMultiplier =
            2.2f;

        [SerializeField]
        private float trainExitDownSpeed =
            8.5f;

        [SerializeField]
        private float trainExitDuration =
            1.2f;

        [SerializeField]
        private float trainExitClearanceMargin =
            0.02f;

        // ==================================================
        // TRAIN HIT FEEL
        // ==================================================

        [Header("Train Hit Feel")]

        [Tooltip(
            "How far the player moves slightly INTO " +
            "the train during the impact."
        )]
        [SerializeField]
        private float trainSidePushDistance =
            0.20f;

        [Tooltip(
            "How quickly the player bumps toward the train."
        )]
        [SerializeField]
        private float trainSidePushInDuration =
            0.06f;

        [Tooltip(
            "How quickly the player returns " +
            "to the center of the lane."
        )]
        [SerializeField]
        private float trainSideReturnDuration =
            0.12f;

        // ==================================================
        // LANES
        // ==================================================

        private const int LeftLane =
            -1;

        private const int CenterLane =
            0;

        private const int RightLane =
            1;

        private int currentLane =
            CenterLane;

        private int previousLane =
            CenterLane;

        // ==================================================
        // ANIMATOR
        // ==================================================

        private static readonly int MoveSpeedHash =
            Animator.StringToHash(
                "MoveSpeed"
            );

        private static readonly int GroundedHash =
            Animator.StringToHash(
                "Grounded"
            );

        private static readonly int JumpHash =
            Animator.StringToHash(
                "Jump"
            );

        private static readonly int SlideHash =
            Animator.StringToHash(
                "Slide"
            );

        // ==================================================
        // GENERAL STATE
        // ==================================================

        private bool isSliding =
            false;

        private bool jumpInputReady =
            false;

        private bool jumpStartedFromTrain =
            false;

        private bool isTrainHitStunned =
            false;

        private float jumpBufferTimer =
            0f;

        // ==================================================
        // TRAIN EXIT STATE
        // ==================================================

        private bool isSteppingOffTrain =
            false;

        private bool hasClearedTrainEdge =
            false;

        private float trainExitTimer =
            0f;

        private float trainExitDirection =
            0f;

        private TrainVehicle exitingTrain;

        // ==================================================
        // START STATE
        // ==================================================

        private Vector3 startingPosition;

        private Quaternion startingRotation;

        // ==================================================
        // COLLIDER
        // ==================================================

        private float originalColliderHeight;

        private Vector3 originalColliderCenter;

        // ==================================================
        // COROUTINES
        // ==================================================

        private Coroutine slideCoroutine;

        private Coroutine trainHitCoroutine;

        // ==================================================
        // AWAKE
        // ==================================================

        private void Awake()
        {
            if (rb == null)
            {
                rb =
                    GetComponent<Rigidbody>();
            }

            if (capsuleCollider == null)
            {
                capsuleCollider =
                    GetComponent<CapsuleCollider>();
            }

            if (animator == null)
            {
                animator =
                    GetComponentInChildren<Animator>();
            }

            if (rb == null)
            {
                Debug.LogError(
                    "PlayerController: Rigidbody not found.",
                    this
                );

                enabled =
                    false;

                return;
            }

            if (capsuleCollider == null)
            {
                Debug.LogError(
                    "PlayerController: CapsuleCollider not found.",
                    this
                );

                enabled =
                    false;

                return;
            }

            if (config == null)
            {
                Debug.LogError(
                    "PlayerController: GameConfig is not assigned.",
                    this
                );
            }

            if (animator != null)
            {
                animator.ResetTrigger(
                    JumpHash
                );

                animator.ResetTrigger(
                    SlideHash
                );

                animator.SetFloat(
                    MoveSpeedHash,
                    0f
                );
            }

            rb.constraints |=
                RigidbodyConstraints.FreezeRotation |
                RigidbodyConstraints.FreezePositionZ;

            rb.interpolation =
                RigidbodyInterpolation.Interpolate;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            rb.useGravity =
                true;

            startingPosition =
                transform.position;

            startingRotation =
                transform.rotation;

            originalColliderHeight =
                capsuleCollider.height;

            originalColliderCenter =
                capsuleCollider.center;
        }

        // ==================================================
        // START
        // ==================================================

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset +=
                    ResetPlayer;

                GameManager.Instance.OnGameStateChanged +=
                    HandleGameStateChanged;
            }
        }

        // ==================================================
        // UPDATE
        // ==================================================

        private void Update()
        {
            UpdateAnimatorParameters();

            RefreshSurfaceState();

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            ReadLaneInput();

            ReadJumpInput();

            ReadSlideInput();
        }

        // ==================================================
        // FIXED UPDATE
        // ==================================================

        private void FixedUpdate()
        {
            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning ||
                config == null)
            {
                return;
            }

            UpdateJumpBuffer();

            MoveBetweenLanes();

            UpdateTrainExit();

            TryBufferedJump();

            ApplyBetterJumpGravity();

            ApplyTrainExitFall();
        }

        // ==================================================
        // GAME STATE
        // ==================================================

        private void HandleGameStateChanged(
            GameManager.GameState newState
        )
        {
            if (newState ==
                GameManager.GameState.Running)
            {
                Keyboard keyboard =
                    Keyboard.current;

                jumpInputReady =
                    keyboard != null &&
                    !keyboard.spaceKey.isPressed &&
                    !keyboard.wKey.isPressed &&
                    !keyboard.upArrowKey.isPressed;
            }
            else
            {
                jumpInputReady =
                    false;

                jumpBufferTimer =
                    0f;

                jumpStartedFromTrain =
                    false;

                CancelSlide();

                CancelTrainHitReaction();

                CancelTrainExit();
            }
        }

        // ==================================================
        // SURFACE STATE
        // ==================================================

        private void RefreshSurfaceState()
        {
            if (!IsGrounded())
            {
                return;
            }

            if (rb.linearVelocity.y >
                0.1f)
            {
                return;
            }

            if (!isSteppingOffTrain)
            {
                jumpStartedFromTrain =
                    false;
            }
        }

        // ==================================================
        // LANE INPUT
        // ==================================================

        private void ReadLaneInput()
        {
            if (isTrainHitStunned)
            {
                return;
            }

            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame)
            {
                TryChangeLane(
                    -1
                );
            }

            if (keyboard.dKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame)
            {
                TryChangeLane(
                    1
                );
            }
        }

        // ==================================================
        // CHANGE LANE
        // ==================================================

        private void TryChangeLane(
            int direction
        )
        {
            if (isTrainHitStunned)
            {
                return;
            }

            int targetLane =
                Mathf.Clamp(
                    currentLane +
                    direction,
                    LeftLane,
                    RightLane
                );

            if (targetLane ==
                currentLane)
            {
                return;
            }

            TrainVehicle standingTrain =
                GetStandingTrain();

            float targetX =
                startingPosition.x +
                targetLane *
                config.laneWidth;

            TrainVehicle targetTrain =
                TrainVehicle.FindTrainAtWorldPosition(
                    targetX,
                    transform.position.z
                );

            // ==================================================
            // FREE TARGET LANE
            // ==================================================

            if (targetTrain == null)
            {
                previousLane =
                    currentLane;

                currentLane =
                    targetLane;

                if (standingTrain != null)
                {
                    BeginTrainExit(
                        standingTrain,
                        targetX
                    );
                }

                return;
            }

            // ==================================================
            // TRAIN -> TRAIN WHILE AIRBORNE
            // ==================================================

            if (!IsGrounded() &&
                jumpStartedFromTrain)
            {
                previousLane =
                    currentLane;

                currentLane =
                    targetLane;

                return;
            }

            // ==================================================
            // BLUE RAMP
            // ==================================================

            if (targetTrain.Type ==
                    TrainVehicle.TrainType.Ramp &&
                targetTrain.IsRampBoardingWorldZ(
                    transform.position.z
                ))
            {
                previousLane =
                    currentLane;

                currentLane =
                    targetLane;

                return;
            }

            // ==================================================
            // TRAIN SIDE HIT
            // ==================================================

            previousLane =
                currentLane;

            targetTrain.RegisterSideHit(
                this
            );
        }

        // ==================================================
        // MOVE BETWEEN LANES
        // ==================================================

        private void MoveBetweenLanes()
        {
            if (isTrainHitStunned)
            {
                return;
            }

            float targetX =
                startingPosition.x +
                currentLane *
                config.laneWidth;

            Vector3 position =
                rb.position;

            float laneSpeed =
                config.laneChangeSpeed;

            if (isSteppingOffTrain)
            {
                laneSpeed *=
                    trainExitLaneSpeedMultiplier;
            }

            float newX =
                Mathf.MoveTowards(
                    position.x,
                    targetX,
                    laneSpeed *
                    Time.fixedDeltaTime
                );

            rb.MovePosition(
                new Vector3(
                    newX,
                    position.y,
                    position.z
                )
            );
        }

        // ==================================================
        // BEGIN TRAIN EXIT
        // ==================================================

        private void BeginTrainExit(
            TrainVehicle train,
            float targetX
        )
        {
            exitingTrain =
                train;

            isSteppingOffTrain =
                true;

            hasClearedTrainEdge =
                false;

            trainExitTimer =
                trainExitDuration;

            jumpBufferTimer =
                0f;

            jumpStartedFromTrain =
                false;

            trainExitDirection =
                Mathf.Sign(
                    targetX -
                    train.transform.position.x
                );

            if (Mathf.Approximately(
                    trainExitDirection,
                    0f
                ))
            {
                trainExitDirection =
                    1f;
            }
        }

        // ==================================================
        // IS EXITING THIS TRAIN?
        // ==================================================

        public bool IsExitingFromTrain(
            TrainVehicle train
        )
        {
            if (!isSteppingOffTrain)
            {
                return false;
            }

            if (train == null)
            {
                return false;
            }

            return
                exitingTrain ==
                train;
        }

        // ==================================================
        // UPDATE TRAIN EXIT
        // ==================================================

        private void UpdateTrainExit()
        {
            if (!isSteppingOffTrain)
            {
                return;
            }

            trainExitTimer -=
                Time.fixedDeltaTime;

            if (!hasClearedTrainEdge)
            {
                if (HasClearedTrainSide())
                {
                    hasClearedTrainEdge =
                        true;

                    ForceTrainExitDown();
                }
            }
            else
            {
                if (IsGrounded() &&
                    GetStandingTrain() == null)
                {
                    CancelTrainExit();

                    return;
                }
            }

            if (trainExitTimer <= 0f)
            {
                CancelTrainExit();
            }
        }

        // ==================================================
        // HAS CLEARED TRAIN SIDE?
        // ==================================================

        private bool HasClearedTrainSide()
        {
            if (exitingTrain == null ||
                capsuleCollider == null)
            {
                return true;
            }

            Collider[] trainColliders =
                exitingTrain.GetComponentsInChildren<Collider>(
                    true
                );

            bool foundSolidCollider =
                false;

            float trainMinX =
                float.PositiveInfinity;

            float trainMaxX =
                float.NegativeInfinity;

            for (int i = 0;
                 i < trainColliders.Length;
                 i++)
            {
                Collider trainCollider =
                    trainColliders[i];

                if (trainCollider == null)
                {
                    continue;
                }

                if (!trainCollider.enabled)
                {
                    continue;
                }

                if (trainCollider.isTrigger)
                {
                    continue;
                }

                if (!trainCollider
                        .gameObject
                        .activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds =
                    trainCollider.bounds;

                trainMinX =
                    Mathf.Min(
                        trainMinX,
                        bounds.min.x
                    );

                trainMaxX =
                    Mathf.Max(
                        trainMaxX,
                        bounds.max.x
                    );

                foundSolidCollider =
                    true;
            }

            if (!foundSolidCollider)
            {
                return true;
            }

            Bounds playerBounds =
                capsuleCollider.bounds;

            if (trainExitDirection < 0f)
            {
                return
                    playerBounds.max.x <=
                    trainMinX -
                    trainExitClearanceMargin;
            }

            return
                playerBounds.min.x >=
                trainMaxX +
                trainExitClearanceMargin;
        }

        // ==================================================
        // TRAIN EXIT FALL
        // ==================================================

        private void ApplyTrainExitFall()
        {
            if (!isSteppingOffTrain ||
                !hasClearedTrainEdge)
            {
                return;
            }

            ForceTrainExitDown();
        }

        private void ForceTrainExitDown()
        {
            Vector3 velocity =
                rb.linearVelocity;

            if (velocity.y >
                -trainExitDownSpeed)
            {
                velocity.y =
                    -trainExitDownSpeed;

                rb.linearVelocity =
                    velocity;
            }
        }

        // ==================================================
        // CANCEL TRAIN EXIT
        // ==================================================

        private void CancelTrainExit()
        {
            isSteppingOffTrain =
                false;

            hasClearedTrainEdge =
                false;

            exitingTrain =
                null;

            trainExitTimer =
                0f;

            trainExitDirection =
                0f;
        }

        // ==================================================
        // JUMP INPUT
        // ==================================================

        private void ReadJumpInput()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (isSteppingOffTrain)
            {
                return;
            }

            if (!jumpInputReady)
            {
                bool stillHeld =
                    keyboard.spaceKey.isPressed ||
                    keyboard.wKey.isPressed ||
                    keyboard.upArrowKey.isPressed;

                if (!stillHeld)
                {
                    jumpInputReady =
                        true;
                }

                return;
            }

            bool jumpPressed =
                keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame;

            if (!jumpPressed)
            {
                return;
            }

            if (isSliding &&
                IsGrounded())
            {
                CancelSlide();
            }

            jumpBufferTimer =
                jumpBufferDuration;
        }

        // ==================================================
        // JUMP BUFFER
        // ==================================================

        private void UpdateJumpBuffer()
        {
            if (jumpBufferTimer <= 0f)
            {
                return;
            }

            jumpBufferTimer -=
                Time.fixedDeltaTime;

            if (jumpBufferTimer < 0f)
            {
                jumpBufferTimer =
                    0f;
            }
        }

        // ==================================================
        // TRY JUMP
        // ==================================================

        private void TryBufferedJump()
        {
            if (jumpBufferTimer <= 0f)
            {
                return;
            }

            if (!IsGrounded())
            {
                return;
            }

            if (isSliding)
            {
                CancelSlide();
            }

            PerformJump();

            jumpBufferTimer =
                0f;
        }

        // ==================================================
        // PERFORM JUMP
        // ==================================================

        private void PerformJump()
        {
            if (!IsGrounded())
            {
                return;
            }

            TrainVehicle standingTrain =
                GetStandingTrain();

            jumpStartedFromTrain =
                standingTrain != null;

            Vector3 velocity =
                rb.linearVelocity;

            if (velocity.y < 0f)
            {
                velocity.y =
                    0f;

                rb.linearVelocity =
                    velocity;
            }

            rb.AddForce(
                Vector3.up *
                config.jumpForce,
                ForceMode.Impulse
            );

            velocity =
                rb.linearVelocity;

            float upwardLimit =
                jumpStartedFromTrain
                    ? trainJumpMaxUpwardSpeed
                    : maxUpwardSpeed;

            if (velocity.y >
                upwardLimit)
            {
                velocity.y =
                    upwardLimit;

                rb.linearVelocity =
                    velocity;
            }

            if (animator != null)
            {
                animator.ResetTrigger(
                    SlideHash
                );

                animator.ResetTrigger(
                    JumpHash
                );

                animator.SetTrigger(
                    JumpHash
                );
            }
        }

        // ==================================================
        // BETTER GRAVITY
        // ==================================================

        private void ApplyBetterJumpGravity()
        {
            if (hasClearedTrainEdge)
            {
                return;
            }

            if (IsGrounded())
            {
                return;
            }

            if (isSliding)
            {
                return;
            }

            Vector3 velocity =
                rb.linearVelocity;

            if (velocity.y > 0f)
            {
                rb.AddForce(
                    Physics.gravity *
                    (
                        upwardGravityMultiplier -
                        1f
                    ),
                    ForceMode.Acceleration
                );

                velocity =
                    rb.linearVelocity;

                float upwardLimit =
                    jumpStartedFromTrain
                        ? trainJumpMaxUpwardSpeed
                        : maxUpwardSpeed;

                if (velocity.y >
                    upwardLimit)
                {
                    velocity.y =
                        upwardLimit;

                    rb.linearVelocity =
                        velocity;
                }
            }
            else if (velocity.y < 0f)
            {
                rb.AddForce(
                    Physics.gravity *
                    (
                        fallGravityMultiplier -
                        1f
                    ),
                    ForceMode.Acceleration
                );

                velocity =
                    rb.linearVelocity;

                if (velocity.y <
                    -maxFallSpeed)
                {
                    velocity.y =
                        -maxFallSpeed;

                    rb.linearVelocity =
                        velocity;
                }
            }
        }

        // ==================================================
        // SLIDE INPUT
        // ==================================================

        private void ReadSlideInput()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null ||
                config == null)
            {
                return;
            }

            if (isSteppingOffTrain)
            {
                return;
            }

            bool slidePressed =
                keyboard.sKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame;

            if (!slidePressed)
            {
                return;
            }

            if (isSliding ||
                slideCoroutine != null)
            {
                return;
            }

            jumpBufferTimer =
                0f;

            StartSlide(
                !IsGrounded()
            );
        }

        // ==================================================
        // START SLIDE
        // ==================================================

        private void StartSlide(
            bool startedInAir
        )
        {
            if (slideCoroutine != null)
            {
                return;
            }

            if (animator != null)
            {
                animator.ResetTrigger(
                    JumpHash
                );

                animator.ResetTrigger(
                    SlideHash
                );

                animator.SetTrigger(
                    SlideHash
                );
            }

            slideCoroutine =
                StartCoroutine(
                    SlideRoutine(
                        startedInAir
                    )
                );
        }

        // ==================================================
        // SLIDE ROUTINE
        // ==================================================

        private IEnumerator SlideRoutine(
            bool startedInAir
        )
        {
            isSliding =
                true;

            ApplySlideCollider();

            if (startedInAir)
            {
                ForceFastDrop();

                while (
                    GameManager.Instance != null &&
                    GameManager.Instance.IsRunning &&
                    !IsGrounded()
                )
                {
                    ForceFastDrop();

                    yield return
                        new WaitForFixedUpdate();
                }
            }

            float timer =
                0f;

            while (
                timer <
                config.slideDuration &&
                GameManager.Instance != null &&
                GameManager.Instance.IsRunning
            )
            {
                timer +=
                    Time.deltaTime;

                yield return null;
            }

            EndSlide();
        }

        // ==================================================
        // SLIDE COLLIDER
        // ==================================================

        private void ApplySlideCollider()
        {
            float newHeight =
                originalColliderHeight *
                0.5f;

            float newCenterY =
                originalColliderCenter.y -
                (
                    originalColliderHeight -
                    newHeight
                ) /
                2f;

            capsuleCollider.height =
                newHeight;

            capsuleCollider.center =
                new Vector3(
                    originalColliderCenter.x,
                    newCenterY,
                    originalColliderCenter.z
                );
        }

        // ==================================================
        // FAST DROP
        // ==================================================

        private void ForceFastDrop()
        {
            Vector3 velocity =
                rb.linearVelocity;

            velocity.y =
                -jumpToSlideDownSpeed;

            rb.linearVelocity =
                velocity;
        }

        // ==================================================
        // END SLIDE
        // ==================================================

        private void EndSlide()
        {
            RestoreCollider();

            isSliding =
                false;

            slideCoroutine =
                null;

            if (animator != null)
            {
                animator.ResetTrigger(
                    SlideHash
                );
            }
        }

        // ==================================================
        // CANCEL SLIDE
        // ==================================================

        private void CancelSlide()
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(
                    slideCoroutine
                );

                slideCoroutine =
                    null;
            }

            RestoreCollider();

            isSliding =
                false;

            if (animator != null)
            {
                animator.ResetTrigger(
                    SlideHash
                );
            }
        }

        // ==================================================
        // RESTORE COLLIDER
        // ==================================================

        private void RestoreCollider()
        {
            if (capsuleCollider == null)
            {
                return;
            }

            capsuleCollider.height =
                originalColliderHeight;

            capsuleCollider.center =
                originalColliderCenter;
        }

        // ==================================================
        // GROUNDED
        // ==================================================

        private bool IsGrounded()
        {
            if (capsuleCollider == null)
            {
                return false;
            }

            float rayDistance =
                capsuleCollider
                    .bounds
                    .extents
                    .y +
                0.15f;

            return Physics.Raycast(
                capsuleCollider.bounds.center,
                Vector3.down,
                rayDistance,
                groundMask
            );
        }

        // ==================================================
        // STANDING TRAIN
        // ==================================================

        private TrainVehicle GetStandingTrain()
        {
            if (capsuleCollider == null)
            {
                return null;
            }

            float rayDistance =
                capsuleCollider
                    .bounds
                    .extents
                    .y +
                0.35f;

            if (!Physics.Raycast(
                    capsuleCollider.bounds.center,
                    Vector3.down,
                    out RaycastHit hit,
                    rayDistance,
                    groundMask
                ))
            {
                return null;
            }

            return
                hit.collider
                    .GetComponentInParent<TrainVehicle>();
        }

        // ==================================================
        // TRAIN TARGET SUPPORT
        // ==================================================

        public bool IsTargetingLane(
            float laneX
        )
        {
            if (config == null)
            {
                return false;
            }

            float targetX =
                startingPosition.x +
                currentLane *
                config.laneWidth;

            return
                Mathf.Abs(
                    targetX -
                    laneX
                ) <
                0.5f;
        }

        // ==================================================
        // TRAIN SIDE HIT
        // ==================================================

        public void HandleTrainSideHit(
            float trainLaneX
        )
        {
            CancelTrainExit();

            currentLane =
                previousLane;

            jumpBufferTimer =
                0f;

            jumpStartedFromTrain =
                false;

            if (trainHitCoroutine != null)
            {
                StopCoroutine(
                    trainHitCoroutine
                );
            }

            trainHitCoroutine =
                StartCoroutine(
                    TrainSideHitRoutine(
                        trainLaneX
                    )
                );
        }

        // ==================================================
        // TRAIN SIDE IMPACT
        // ==================================================

        private IEnumerator TrainSideHitRoutine(
            float trainLaneX
        )
        {
            isTrainHitStunned =
                true;

            float safeX =
                startingPosition.x +
                currentLane *
                config.laneWidth;

            // ==================================================
            // IMPORTANT CHANGE:
            //
            // Direction is now TOWARD the train.
            // ==================================================

            float directionTowardTrain =
                Mathf.Sign(
                    trainLaneX -
                    safeX
                );

            if (Mathf.Approximately(
                    directionTowardTrain,
                    0f
                ))
            {
                directionTowardTrain =
                    1f;
            }

            float startX =
                rb.position.x;

            float impactX =
                safeX +
                directionTowardTrain *
                trainSidePushDistance;

            // ==================================================
            // PHASE 1:
            // SMALL MOVEMENT INTO TRAIN
            // ==================================================

            float timer =
                0f;

            while (
                timer <
                trainSidePushInDuration
            )
            {
                timer +=
                    Time.fixedDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        trainSidePushInDuration
                    );

                float easedT =
                    1f -
                    (
                        1f - t
                    ) *
                    (
                        1f - t
                    );

                Vector3 position =
                    rb.position;

                position.x =
                    Mathf.Lerp(
                        startX,
                        impactX,
                        easedT
                    );

                rb.MovePosition(
                    position
                );

                yield return
                    new WaitForFixedUpdate();
            }

            // ==================================================
            // PHASE 2:
            // RETURN TO LANE CENTER
            // ==================================================

            timer =
                0f;

            float returnStartX =
                rb.position.x;

            while (
                timer <
                trainSideReturnDuration
            )
            {
                timer +=
                    Time.fixedDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        trainSideReturnDuration
                    );

                float smoothT =
                    t *
                    t *
                    (
                        3f -
                        2f *
                        t
                    );

                Vector3 position =
                    rb.position;

                position.x =
                    Mathf.Lerp(
                        returnStartX,
                        safeX,
                        smoothT
                    );

                rb.MovePosition(
                    position
                );

                yield return
                    new WaitForFixedUpdate();
            }

            // ==================================================
            // FINISH
            // ==================================================

            Vector3 finalPosition =
                rb.position;

            finalPosition.x =
                safeX;

            rb.MovePosition(
                finalPosition
            );

            Vector3 velocity =
                rb.linearVelocity;

            velocity.x =
                0f;

            rb.linearVelocity =
                velocity;

            isTrainHitStunned =
                false;

            trainHitCoroutine =
                null;
        }

        // ==================================================
        // CANCEL HIT
        // ==================================================

        private void CancelTrainHitReaction()
        {
            if (trainHitCoroutine != null)
            {
                StopCoroutine(
                    trainHitCoroutine
                );

                trainHitCoroutine =
                    null;
            }

            isTrainHitStunned =
                false;
        }

        // ==================================================
        // ANIMATOR
        // ==================================================

        private void UpdateAnimatorParameters()
        {
            if (animator == null)
            {
                return;
            }

            bool running =
                GameManager.Instance != null &&
                GameManager.Instance.IsRunning;

            animator.SetFloat(
                MoveSpeedHash,
                running &&
                config != null
                    ? config.baseScrollSpeed
                    : 0f
            );

            animator.SetBool(
                GroundedHash,
                IsGrounded()
            );
        }

        // ==================================================
        // RESET
        // ==================================================

        private void ResetPlayer()
        {
            CancelSlide();

            CancelTrainHitReaction();

            CancelTrainExit();

            currentLane =
                CenterLane;

            previousLane =
                CenterLane;

            jumpBufferTimer =
                0f;

            jumpInputReady =
                false;

            jumpStartedFromTrain =
                false;

            rb.useGravity =
                true;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.position =
                startingPosition;

            rb.rotation =
                startingRotation;

            if (animator != null)
            {
                animator.ResetTrigger(
                    JumpHash
                );

                animator.ResetTrigger(
                    SlideHash
                );

                animator.SetFloat(
                    MoveSpeedHash,
                    0f
                );
            }
        }

        // ==================================================
        // DISABLE
        // ==================================================

        private void OnDisable()
        {
            CancelSlide();

            CancelTrainHitReaction();

            CancelTrainExit();
        }

        // ==================================================
        // CLEANUP
        // ==================================================

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -=
                    ResetPlayer;

                GameManager.Instance.OnGameStateChanged -=
                    HandleGameStateChanged;
            }
        }
    }
}