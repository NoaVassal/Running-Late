using System;
using UnityEngine;

namespace RunningLate
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            GetReady,
            Running,
            Results,
            GameOver
        }

        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        public GameState State { get; private set; } = GameState.GetReady;
        public float TimeRemaining { get; private set; }
        public bool IsRunning => State == GameState.Running;

        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimerChanged;
        public event Action<string> OnGameOver;
        public event Action<bool, int> OnRunFinished;
        public event Action OnRunReset;

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
            bool startPressed =
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return);

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

            bool passed = finalGrade > config.passingGrade;

            SetState(GameState.Results);
            OnRunFinished?.Invoke(passed, finalGrade);
        }

        public void TriggerGameOver(string reason)
        {
            if (!IsRunning)
                return;

            SetState(GameState.GameOver);
            OnGameOver?.Invoke(reason);
        }

        public void RestartRun()
        {
            TimeRemaining = config.classTimer;

            OnRunReset?.Invoke();
            OnTimerChanged?.Invoke(TimeRemaining);

            SetState(GameState.GetReady);
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
