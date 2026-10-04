using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RunningLate
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            GetReady,
            Running,
            EndingVideo,
            Results,
            GameOver
        }

        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        public GameState State { get; private set; } = GameState.GetReady;
        public GameState PendingFinalState { get; private set; } =
            GameState.GetReady;
        public float TimeRemaining { get; private set; }
        public bool IsRunning => State == GameState.Running;

        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimerChanged;
        public event Action<string> OnGameOver;
        public event Action<bool, int> OnRunFinished;
        public event Action OnRunReset;

        private bool hasPendingEndingVideo;
        private bool pendingPassed;
        private int pendingFinalGrade;
        private string pendingGameOverReason = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (config == null)
            {
                Debug.LogError(
                    "GameManager: GameConfig is not assigned in the Inspector."
                );

                enabled = false;
                return;
            }

            TimeRemaining = config.classTimer;
        }

        private void Start()
        {
            SetState(GameState.GetReady);
            OnTimerChanged?.Invoke(TimeRemaining);
        }

        private void Update()
        {
            HandleStateInput();

            if (!IsRunning)
                return;

            UpdateTimer();
        }

        private void HandleStateInput()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            bool startPressed =
                keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame;

            if (State == GameState.GetReady && startPressed)
            {
                StartRun();
                return;
            }

            if ((State == GameState.Results ||
                 State == GameState.GameOver) &&
                startPressed)
            {
                RestartRun();
            }
        }

        private void UpdateTimer()
        {
            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining < 0f)
                TimeRemaining = 0f;

            OnTimerChanged?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
            {
                TriggerGameOver("Timer reached zero");
            }
        }

        public void StartRun()
        {
            TimeRemaining = config.classTimer;
            OnTimerChanged?.Invoke(TimeRemaining);
            SetState(GameState.Running);
        }

        public void FinishRun(int finalGrade)
        {
            if (!IsRunning)
                return;

            pendingPassed =
                finalGrade >= config.passingGrade;

            pendingFinalGrade = finalGrade;
            pendingGameOverReason = "";
            PendingFinalState = GameState.Results;
            hasPendingEndingVideo = true;

            SetState(GameState.EndingVideo);
        }

        public void TriggerGameOver(string reason)
        {
            if (!IsRunning)
                return;

            pendingPassed = false;
            pendingFinalGrade = 0;
            pendingGameOverReason = reason;
            PendingFinalState = GameState.GameOver;
            hasPendingEndingVideo = true;

            SetState(GameState.EndingVideo);
        }

        public void CompleteEndingVideo()
        {
            if (State != GameState.EndingVideo ||
                !hasPendingEndingVideo)
            {
                return;
            }

            GameState finalState = PendingFinalState;
            bool didPass = pendingPassed;
            int finalGrade = pendingFinalGrade;
            string gameOverReason = pendingGameOverReason;

            ClearPendingEndingVideo();
            SetState(finalState);

            if (finalState == GameState.Results)
            {
                OnRunFinished?.Invoke(
                    didPass,
                    finalGrade
                );
            }
            else if (finalState == GameState.GameOver)
            {
                OnGameOver?.Invoke(gameOverReason);
            }
        }

        public void RestartRun()
        {
            TimeRemaining = config.classTimer;
            ClearPendingEndingVideo();

            OnRunReset?.Invoke();
            OnTimerChanged?.Invoke(TimeRemaining);

            SetState(GameState.GetReady);
        }

        private void ClearPendingEndingVideo()
        {
            hasPendingEndingVideo = false;
            pendingPassed = false;
            pendingFinalGrade = 0;
            pendingGameOverReason = "";
            PendingFinalState = GameState.GetReady;
        }

        private void SetState(GameState newState)
        {
            State = newState;
            OnGameStateChanged?.Invoke(State);
            Debug.Log("Game State: " + State);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
