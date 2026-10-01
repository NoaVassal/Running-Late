using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RunningLate
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private CapsuleCollider capsuleCollider;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundMask;

        private const int LeftLane = -1;
        private const int CenterLane = 0;
        private const int RightLane = 1;

        private static readonly int MoveSpeedHash =
            Animator.StringToHash("MoveSpeed");

        private static readonly int GroundedHash =
            Animator.StringToHash("Grounded");

        private static readonly int JumpHash =
            Animator.StringToHash("Jump");

        private static readonly int SlideHash =
            Animator.StringToHash("Slide");

        private int currentLane = CenterLane;

        [SerializeField] private Animator animator;

        private bool jumpRequested = false;
        private bool isSliding = false;
        private bool jumpInputReady = false;

        private Vector3 startingPosition;
        private Quaternion startingRotation;

        private float originalColliderHeight;
        private Vector3 originalColliderCenter;

        private Coroutine slideCoroutine;

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (capsuleCollider == null)
            {
                capsuleCollider = GetComponent<CapsuleCollider>();
            }

            if (capsuleCollider == null)
            {
                Debug.LogError(
                    "PlayerController: CapsuleCollider was not found.",
                    this
                );

                enabled = false;
                return;
            }

            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogWarning(
                    "PlayerController: Animator was not found in the Player hierarchy.",
                    this
                );
            }
            else
            {
                animator.ResetTrigger(JumpHash);
                animator.SetFloat(MoveSpeedHash, 0f);
            }

            rb.constraints |=
                RigidbodyConstraints.FreezeRotation |
                RigidbodyConstraints.FreezePositionZ;

            rb.interpolation =
                RigidbodyInterpolation.Interpolate;

            startingPosition = transform.position;
            startingRotation = transform.rotation;

            originalColliderHeight =
                capsuleCollider.height;

            originalColliderCenter =
                capsuleCollider.center;

            if (config == null)
            {
                Debug.LogError(
                    "PlayerController: GameConfig is not assigned."
                );
            }
        }

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

        private void HandleGameStateChanged(
            GameManager.GameState newState
        )
        {
            if (newState == GameManager.GameState.Running)
            {
                Keyboard keyboard = Keyboard.current;

                jumpInputReady =
                    keyboard != null &&
                    !keyboard.spaceKey.isPressed &&
                    !keyboard.wKey.isPressed &&
                    !keyboard.upArrowKey.isPressed;
            }
            else
            {
                jumpInputReady = false;
                CancelSlide();

                if (animator != null)
                {
                    animator.ResetTrigger(JumpHash);
                }
            }
        }

        private void Update()
        {
            UpdateAnimatorParameters();

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            ReadLaneInput();
            ReadJumpInput();
            ReadSlideInput();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning ||
                config == null)
            {
                return;
            }

            MoveBetweenLanes();
            HandleJump();
        }

        private void ReadLaneInput()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame)
            {
                TryChangeLane(-1);
            }

            if (keyboard.dKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame)
            {
                TryChangeLane(1);
            }
        }

        private void TryChangeLane(int direction)
        {
            int targetLane =
                Mathf.Clamp(
                    currentLane + direction,
                    LeftLane,
                    RightLane
                );

            if (targetLane == currentLane)
            {
                return;
            }

            bool leavingBoulevardForTrain =
                currentLane == CenterLane &&
                targetLane != CenterLane;

            if (leavingBoulevardForTrain)
            {
                bool jumpIsActive =
                    jumpRequested ||
                    !IsGrounded() ||
                    rb.linearVelocity.y > 0.1f ||
                    IsJumpKeyPressedThisFrame();

                if (!jumpIsActive)
                {
                    return;
                }
            }

            currentLane = targetLane;
        }

        private bool IsJumpKeyPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return false;
            }

            return
                keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame;
        }

        private void ReadJumpInput()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (!jumpInputReady)
            {
                bool jumpKeyStillHeld =
                    keyboard.spaceKey.isPressed ||
                    keyboard.wKey.isPressed ||
                    keyboard.upArrowKey.isPressed;

                if (!jumpKeyStillHeld)
                {
                    jumpInputReady = true;
                }

                return;
            }

            bool jumpPressed =
                keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame;

            if (jumpPressed &&
                IsGrounded() &&
                !isSliding)
            {
                jumpRequested = true;
            }
        }

        private void ReadSlideInput()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            bool slidePressed =
                keyboard.sKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame;

            if (slidePressed &&
                config != null &&
                IsGrounded() &&
                !isSliding &&
                slideCoroutine == null)
            {
                slideCoroutine =
                    StartCoroutine(
                        SlideRoutine()
                    );
            }
        }

        private void MoveBetweenLanes()
        {
            float targetX =
                startingPosition.x +
                currentLane * config.laneWidth;

            Vector3 currentPosition =
                rb.position;

            float newX =
                Mathf.MoveTowards(
                    currentPosition.x,
                    targetX,
                    config.laneChangeSpeed *
                    Time.fixedDeltaTime
                );

            Vector3 newPosition =
                new Vector3(
                    newX,
                    currentPosition.y,
                    currentPosition.z
                );

            rb.MovePosition(newPosition);
        }

        private void HandleJump()
        {
            if (!jumpRequested)
            {
                return;
            }

            jumpRequested = false;

            if (!IsGrounded())
            {
                return;
            }

            rb.AddForce(
                Vector3.up * config.jumpForce,
                ForceMode.Impulse
            );

            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(JumpHash);
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (animator == null)
            {
                return;
            }

            bool gameplayIsRunning =
                GameManager.Instance != null &&
                GameManager.Instance.IsRunning;

            float moveSpeed =
                gameplayIsRunning && config != null
                    ? config.baseScrollSpeed
                    : 0f;

            animator.SetFloat(MoveSpeedHash, moveSpeed);
            animator.SetBool(GroundedHash, IsGrounded());
        }

        private bool IsGrounded()
        {
            if (capsuleCollider == null)
            {
                return false;
            }

            float rayDistance =
                capsuleCollider.bounds.extents.y +
                0.15f;

            return Physics.Raycast(
                capsuleCollider.bounds.center,
                Vector3.down,
                rayDistance,
                groundMask
            );
        }

        private IEnumerator SlideRoutine()
        {
            isSliding = true;

            float newHeight =
                originalColliderHeight * 0.5f;

            float newCenterY =
                originalColliderCenter.y -
                (originalColliderHeight - newHeight) / 2f;

            capsuleCollider.height =
                newHeight;

            capsuleCollider.center =
                new Vector3(
                    originalColliderCenter.x,
                    newCenterY,
                    originalColliderCenter.z
                );

            if (animator != null)
            {
                animator.ResetTrigger(SlideHash);
                animator.SetTrigger(SlideHash);
            }

            yield return new WaitForSeconds(
                config.slideDuration
            );

            EndSlide();
        }

        private void EndSlide()
        {
            RestoreCollider();

            isSliding = false;
            slideCoroutine = null;

            if (animator != null)
            {
                animator.ResetTrigger(SlideHash);
            }
        }

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

        private void CancelSlide()
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }

            RestoreCollider();
            isSliding = false;

            if (animator != null)
            {
                animator.ResetTrigger(SlideHash);
            }
        }

        private void ResetPlayer()
        {
            CancelSlide();

            jumpRequested = false;
            jumpInputReady = false;

            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetFloat(MoveSpeedHash, 0f);
            }

            currentLane = CenterLane;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.position =
                startingPosition;

            rb.rotation =
                startingRotation;
        }

        private void OnDisable()
        {
            CancelSlide();
        }

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
