using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RunningLate
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private CapsuleCollider capsuleCollider;

        [Header("Visual")]
        [SerializeField] private Transform playerVisual;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundMask;

        [Header("Slide Visual Settings")]
        [SerializeField] private float slideVisualScaleY = 0.5f;
        [SerializeField] private float slideVisualOffsetY = -0.5f;

        private const int LeftLane = -1;
        private const int CenterLane = 0;
        private const int RightLane = 1;

        private int currentLane = CenterLane;

        private bool jumpRequested = false;
        private bool isSliding = false;
        private bool jumpInputReady = false;

        private Vector3 startingPosition;
        private Quaternion startingRotation;

        private float originalColliderHeight;
        private Vector3 originalColliderCenter;

        private Vector3 originalVisualScale;
        private Vector3 originalVisualPosition;

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

            rb.constraints |=
                RigidbodyConstraints.FreezeRotation |
                RigidbodyConstraints.FreezePositionZ;

            rb.interpolation = RigidbodyInterpolation.Interpolate;

            startingPosition = transform.position;
            startingRotation = transform.rotation;

            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;

            if (playerVisual != null)
            {
                originalVisualScale = playerVisual.localScale;
                originalVisualPosition = playerVisual.localPosition;
            }

            if (config == null)
            {
                Debug.LogError(
                    "PlayerController: GameConfig is not assigned."
                );
            }

            if (playerVisual == null)
            {
                Debug.LogError(
                    "PlayerController: Player Visual is not assigned."
                );
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset += ResetPlayer;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameManager.GameState newState)
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
            }
        }

        private void Update()
        {
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
                currentLane--;

                if (currentLane < LeftLane)
                {
                    currentLane = LeftLane;
                }
            }

            if (keyboard.dKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame)
            {
                currentLane++;

                if (currentLane > RightLane)
                {
                    currentLane = RightLane;
                }
            }
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
                IsGrounded() &&
                !isSliding)
            {
                slideCoroutine = StartCoroutine(SlideRoutine());
            }
        }

        private void MoveBetweenLanes()
        {
            float targetX =
                startingPosition.x +
                currentLane * config.laneWidth;

            Vector3 currentPosition = rb.position;

            float newX = Mathf.MoveTowards(
                currentPosition.x,
                targetX,
                config.laneChangeSpeed *
                Time.fixedDeltaTime
            );

            Vector3 newPosition = new Vector3(
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
        }

        private bool IsGrounded()
        {
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

            float originalBottom =
                originalColliderCenter.y -
                originalColliderHeight / 2f;

            float newCenterY =
                originalBottom +
                newHeight / 2f;

            capsuleCollider.height =
                newHeight;

            capsuleCollider.center =
                new Vector3(
                    originalColliderCenter.x,
                    newCenterY,
                    originalColliderCenter.z
                );

            if (playerVisual != null)
            {
                playerVisual.localScale =
                    new Vector3(
                        originalVisualScale.x,
                        originalVisualScale.y *
                        slideVisualScaleY,
                        originalVisualScale.z
                    );

                playerVisual.localPosition =
                    originalVisualPosition +
                    Vector3.up *
                    slideVisualOffsetY;
            }

            yield return new WaitForSeconds(
                config.slideDuration
            );

            EndSlide();
        }

        private void EndSlide()
        {
            RestoreCollider();
            RestoreVisual();

            isSliding = false;
            slideCoroutine = null;
        }

        private void RestoreCollider()
        {
            capsuleCollider.height =
                originalColliderHeight;

            capsuleCollider.center =
                originalColliderCenter;
        }

        private void RestoreVisual()
        {
            if (playerVisual == null)
            {
                return;
            }

            playerVisual.localScale =
                originalVisualScale;

            playerVisual.localPosition =
                originalVisualPosition;
        }

        private void ResetPlayer()
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }

            isSliding = false;
            jumpRequested = false;
            jumpInputReady = false;
            currentLane = CenterLane;

            RestoreCollider();
            RestoreVisual();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = startingPosition;
            rb.rotation = startingRotation;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -= ResetPlayer;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
    }
}
